from pathlib import Path
import json
import math
import os
import struct
import sys
import wave
import numpy as np
from PIL import Image
from prepare_fridge import read_fbx, child


ROOT = Path(__file__).resolve().parents[1]
WORK = Path(os.environ['TEMP']) / 'leftovers-microwave-assets'
OUT = ROOT / 'Assets/Models/Microwave/Downloaded'
OUT.mkdir(parents=True, exist_ok=True)


def vectors(array, names):
    return [dict(zip(names, map(float, row))) for row in array]


def mesh_data(name, positions, normals, uv, triangles):
    return dict(name=name, vertices=vectors(positions, 'xyz'),
                normals=vectors(normals, 'xyz'), uv=vectors(uv, 'xy'),
                triangles=np.asarray(triangles).flatten().astype(int).tolist())


def read_collada():
    import xml.etree.ElementTree as ET
    ns = {'c': 'http://www.collada.org/2005/11/COLLADASchema'}
    tree = ET.parse(WORK / 'microwave-source/model/model.dae')
    parts = []
    names = ['Door', 'Turntable', 'Oven']
    door_pivot = np.array([.33, .631407 * .33, .6784 * .33])
    angle = math.atan2(.823, .567)
    rotation = np.array([[math.cos(-angle), 0, math.sin(-angle)],
                         [0, 1, 0],
                         [-math.sin(-angle), 0, math.cos(-angle)]])
    for geometry, name in zip(tree.findall('.//c:geometry', ns), names):
        sources = {}
        for source in geometry.findall('c:mesh/c:source', ns):
            stride = int(source.find('c:technique_common/c:accessor', ns).attrib['stride'])
            sources[source.attrib['id']] = np.fromstring(source.find('c:float_array', ns).text, sep=' ').reshape(-1, stride)
        vertices_source = geometry.find('c:mesh/c:vertices/c:input', ns).attrib['source'][1:]
        poly = geometry.find('c:mesh/c:polylist', ns)
        inputs = poly.findall('c:input', ns)
        stride = max(int(i.attrib.get('offset', 0)) for i in inputs) + 1
        indices = np.fromstring(poly.find('c:p', ns).text, sep=' ', dtype=int).reshape(-1, stride)
        counts = np.fromstring(poly.find('c:vcount', ns).text, sep=' ', dtype=int)
        positions = []
        normals = []
        uv = []
        for index in indices:
            for source in inputs:
                offset = int(source.attrib.get('offset', 0))
                semantic = source.attrib['semantic']
                source_id = source.attrib['source'][1:]
                if semantic == 'VERTEX':
                    positions.append(sources[vertices_source][index[offset]])
                elif semantic == 'NORMAL':
                    normals.append(sources[source_id][index[offset]])
                elif semantic == 'TEXCOORD':
                    uv.append(sources[source_id][index[offset]][:2])
        source_positions = np.array(positions)
        positions = source_positions[:, [2, 1, 0]] * .33
        positions[:, 1] += .631407 * .33
        normals = np.array(normals)[:, [2, 1, 0]]
        if name == 'Door':
            positions = (positions - door_pivot) @ rotation.T
            normals = normals @ rotation.T
        if name == 'Turntable':
            center = np.array([.183545 * .33, -.405065 * .33 + .631407 * .33, -.02489 * .33])
            positions -= center
        triangles = []
        offset = 0
        for count in counts:
            face = source_positions[offset:offset + count]
            remove = name == 'Oven' and np.all((face[:, 0] > .67) & (face[:, 1] > .30) & (face[:, 2] < -.70))
            if not remove:
                for corner in range(1, count - 1):
                    triangles.append([offset, offset + corner + 1, offset + corner])
            offset += count
        parts.append(mesh_data(name, positions, normals, uv, triangles))
        print(name, 'bounds', positions.min(0).round(4), positions.max(0).round(4), 'triangles', len(triangles))
    return parts, door_pivot.tolist(), center.tolist()


def read_plate():
    nodes, _ = read_fbx((WORK / 'plates/source/Plates.fbx').read_bytes())
    objects = next(node for node in nodes if node[0] == 'Objects')
    geometries = [node for node in objects[2] if node[0] == 'Geometry']
    geometry = min(geometries, key=lambda node: len(child(node, 'Vertices')[1][0][1]))
    vertices = np.array(child(geometry, 'Vertices')[1][0][1]).reshape(-1, 3)
    indices = child(geometry, 'PolygonVertexIndex')[1][0][1]
    normals = np.array(child(child(geometry, 'LayerElementNormal'), 'Normals')[1][0][1]).reshape(-1, 3)
    positions = []
    mapped_normals = []
    triangles = []
    face = []
    for i, index in enumerate(indices):
        vertex = index
        if vertex < 0:
            vertex = -vertex - 1
        point = vertices[vertex]
        positions.append([point[0], point[2], -point[1]])
        normal = normals[i]
        mapped_normals.append([normal[0], normal[2], -normal[1]])
        face.append(i)
        if index < 0:
            for corner in range(1, len(face) - 1):
                triangles.append([face[0], face[corner], face[corner + 1]])
            face = []
    positions = np.array(positions) * (.248 / 3.131759)
    positions[:, 1] -= positions[:, 1].min()
    print('Plate', len(positions), 'vertices', len(triangles), 'triangles')
    return mesh_data('CeramicPlate', positions, mapped_normals, np.zeros((len(positions), 2)), triangles)


def read_sandwich():
    raw = (WORK / 'sandvich/source/SANDVICH.glb').read_bytes()
    length = struct.unpack_from('<I', raw, 12)[0]
    document = json.loads(raw[20:20 + length])
    binary = raw[28 + length:]

    def accessor(index):
        data = document['accessors'][index]
        view = document['bufferViews'][data['bufferView']]
        dtype = {5126: '<f4', 5123: '<u2', 5125: '<u4', 5121: 'u1'}[data['componentType']]
        components = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}[data['type']]
        offset = view.get('byteOffset', 0) + data.get('byteOffset', 0)
        return np.frombuffer(binary, dtype, data['count'] * components, offset).reshape(-1, components)

    positions = []
    normals = []
    uv = []
    triangles = []
    count = 0
    for mesh_index in [1, 2]:
        primitive = document['meshes'][mesh_index]['primitives'][0]
        attributes = primitive['attributes']
        p = accessor(attributes['POSITION'])
        positions.extend(p)
        normals.extend(accessor(attributes['NORMAL']))
        coordinates = accessor(attributes['TEXCOORD_0']).copy()
        coordinates[:, 1] = 1 - coordinates[:, 1]
        uv.extend(coordinates)
        triangles.extend(accessor(primitive['indices']).reshape(-1, 3) + count)
        count += len(p)
    positions = np.array(positions)
    scale = .19 / max(np.ptp(positions[:, 0]), np.ptp(positions[:, 2]))
    positions[:, 0] -= (positions[:, 0].max() + positions[:, 0].min()) / 2
    positions[:, 2] -= (positions[:, 2].max() + positions[:, 2].min()) / 2
    positions[:, 1] -= positions[:, 1].min()
    positions *= scale
    image = document['images'][0]
    view = document['bufferViews'][image['bufferView']]
    start = view.get('byteOffset', 0)
    (OUT / 'Sandwich_Albedo.png').write_bytes(binary[start:start + view['byteLength']])
    print('Sandwich', len(positions), 'vertices', len(triangles), 'triangles', 'height', positions[:, 1].max())
    return mesh_data('Sandwich', positions, normals, uv, triangles)


def textures():
    source = WORK / 'microwave-oven/textures'
    albedo = Image.open(source / 'MicrowaveOven_albedo.jpeg').convert('RGB').resize((2048, 2048), Image.Resampling.LANCZOS)
    albedo.save(OUT / 'Microwave_Albedo.png')
    glass = albedo.convert('RGBA')
    glass.putalpha(Image.open(source / 'MicrowaveOven_opacity.jpeg').convert('L').resize((2048, 2048), Image.Resampling.LANCZOS))
    glass.save(OUT / 'Microwave_Glass.png')
    Image.open(source / 'MicrowaveOven_opacity.jpeg').convert('L').resize((2048, 2048), Image.Resampling.LANCZOS).save(OUT / 'Microwave_Opacity.png')
    Image.open(source / 'MicrowaveOven_normal.png').resize((2048, 2048), Image.Resampling.LANCZOS).save(OUT / 'Microwave_Normal.png')
    metallic = np.asarray(Image.open(source / 'MicrowaveOven_metallic.jpeg').convert('L').resize((2048, 2048)), dtype=float)
    roughness = np.asarray(Image.open(source / 'MicrowaveOven_roughness.jpeg').convert('L').resize((2048, 2048)), dtype=float)
    packed = np.zeros((2048, 2048, 4), dtype=np.uint8)
    packed[:, :, :3] = (metallic * .25).astype(np.uint8)[:, :, None]
    packed[:, :, 3] = ((255 - roughness) * .6).astype(np.uint8)
    Image.fromarray(packed).save(OUT / 'Microwave_MetalSmooth.png')


def audio():
    folder = ROOT / 'Assets/Audio/Microwave'
    sr = 48000

    def source(name):
        with wave.open(str(WORK / (name + '.wav'))) as stream:
            return np.frombuffer(stream.readframes(stream.getnframes()), '<i2').astype(float) / 32768

    def save(name, samples, peak, fade=True):
        samples = samples.copy()
        samples -= samples.mean()
        samples *= peak / max(abs(samples).max(), 1e-9)
        if fade:
            samples[:240] *= np.linspace(0, 1, 240)
            samples[-min(960, len(samples)):] *= np.linspace(1, 0, min(960, len(samples)))
        with wave.open(str(folder / (name + '.wav')), 'wb') as stream:
            stream.setparams((1, 2, sr, 0, 'NONE', 'not compressed'))
            stream.writeframes((np.clip(samples, -.99, .99) * 32767).astype('<i2').tobytes())
        print(name, round(len(samples) / sr, 3), 'seconds')

    combined = source('open-close')
    end = source('beep')
    save('Microwave_Open', combined[int(.35 * sr):int(.86 * sr)], .65)
    save('Microwave_Close', combined[int(4.13 * sr):int(4.66 * sr)], .78)
    save('Microwave_Slam', source('slam')[int(.025 * sr):int(.54 * sr)], .96)
    save('Microwave_Beep', end[int(.035 * sr):int(3.68 * sr)], .74)
    save('Microwave_Start', end[int(.07 * sr):int(.21 * sr)], .64)
    rng = np.random.default_rng(34)
    time = np.arange(sr * 2) / sr
    frequencies = np.fft.rfftfreq(len(time), 1 / sr)
    spectrum = np.fft.rfft(rng.normal(0, 1, len(time)))
    spectrum *= np.exp(-frequencies / 850) * (1 - np.exp(-frequencies / 95))
    fan = np.fft.irfft(spectrum, len(time))
    fan /= max(np.std(fan), 1e-9)
    hum = .32 * np.sin(2 * np.pi * 60 * time) + .17 * np.sin(2 * np.pi * 120 * time)
    hum += .07 * np.sin(2 * np.pi * 180 * time) + .035 * np.sin(2 * np.pi * 300 * time)
    hum += fan * .035
    save('Microwave_Cooking', hum, .72, False)
    movement = combined[int(.42 * sr):int(.64 * sr)]
    n = int(.025 * sr)
    movement[:n] = movement[-n:] * np.linspace(1, 0, n) + movement[:n] * np.linspace(0, 1, n)
    save('Microwave_Movement', movement[:-n], .15, False)
    (folder / 'CREDITS.md').write_text('Microwave opening/closing, slam, and completion recordings supplied by the user in Downloads on October 17, 2026. Original filenames: microwave open and close.mp3, Microwave slam.mp3, microwave beep.mp3. Opening is the first event; closing is the second. The start cue is a 140 ms excerpt of the first completion tone. Cooking motor/fan hum is an original synthesized loop. Original recordings remain unchanged.\n')


if __name__ == '__main__':
    parts, hinge, turntable = read_collada()
    parts.append(read_plate())
    parts.append(read_sandwich())
    (OUT / 'PreparedMeshes.json').write_text(json.dumps(dict(meshes=parts, hinge=hinge, turntable=turntable), separators=(',', ':')))
    textures()
    audio()
