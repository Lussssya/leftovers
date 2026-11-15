from pathlib import Path
import math
import re
import struct
import uuid


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Models/Products'
SCENE = ROOT / 'Assets/Scenes/Kitchen.unity'
SCRIPT_GUID = '2f440ee97d2d3439cb0fb5f1422e8d1c'
SOURCES = {
    'Ketchup': 'bottle-ketchup',
    'Tomato': 'tomato',
    'Cheese': 'cheese-cut',
    'Egg': 'egg',
    'Toast': 'bread',
    'Lettuce': 'lettuce',
}


def guid(name):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'leftovers/product-models/' + name).hex


def native_meta(path, file_id):
    path.with_name(path.name + '.meta').write_text(
        'fileFormatVersion: 2\nguid: ' + guid(path.name) + '\n'
        'NativeFormatImporter:\n  externalObjects: {}\n'
        f'  mainObjectFileID: {file_id}\n  userData: \n'
        '  assetBundleName: \n  assetBundleVariant: \n')


def vector(text):
    return tuple(float(value) for value in re.findall(r'[xyz]: ([^,}]+)', text))


def yaml_vector(values):
    return '{' + ', '.join(f'{axis}: {value:.9g}' for axis, value in zip('xyz', values)) + '}'


def read_obj(path):
    vertices, normals, uv, faces = [], [], [], []
    for line in path.read_text().splitlines():
        parts = line.split()
        if not parts:
            continue
        if parts[0] == 'v':
            vertices.append(tuple(map(float, parts[1:4])))
        elif parts[0] == 'vn':
            normals.append(tuple(map(float, parts[1:4])))
        elif parts[0] == 'vt':
            uv.append(tuple(map(float, parts[1:3])))
        elif parts[0] == 'f':
            references = [tuple(int(n) - 1 for n in part.split('/')) for part in parts[1:]]
            for i in range(1, len(references) - 1):
                faces.append((references[0], references[i], references[i + 1]))
    return vertices, normals, uv, faces


def create_mesh(name, scale):
    source = Path(__file__).parent / 'Source' / (SOURCES[name] + '.obj')
    vertices, normals, uv, faces = read_obj(source)
    minimum = [min(v[k] for v in vertices) for k in range(3)]
    maximum = [max(v[k] for v in vertices) for k in range(3)]
    center = [(a + b) / 2 for a, b in zip(minimum, maximum)]
    size = [b - a for a, b in zip(minimum, maximum)]
    fit = min(scale[k] / size[k] for k in range(3))
    packed = bytearray()
    points = []
    indices = []
    for face in faces:
        start = len(points)
        a, b, c = [vertices[reference[0]] for reference in face]
        u = [b[k] - a[k] for k in range(3)]
        v = [c[k] - a[k] for k in range(3)]
        face_normal = (u[1]*v[2] - u[2]*v[1], u[2]*v[0] - u[0]*v[2], u[0]*v[1] - u[1]*v[0])
        for vertex_index, uv_index, normal_index in face:
            vertex = vertices[vertex_index]
            point = [(vertex[k] - center[k]) * fit / scale[k] for k in range(3)]
            point[1] = (vertex[1] - minimum[1]) * fit / scale[1] - 0.5
            normal = [normals[normal_index][k] * scale[k] for k in range(3)]
            if sum(normals[normal_index][k] * face_normal[k] for k in range(3)) < 0:
                normal = [-value for value in normal]
            length = math.sqrt(sum(n*n for n in normal))
            normal = [n / length for n in normal]
            points.append(point)
            packed.extend(struct.pack('<8f', *point, *normal, *uv[uv_index]))
        indices.extend((start, start + 1, start + 2))
    bounds_min = [min(p[k] for p in points) for k in range(3)]
    bounds_max = [max(p[k] for p in points) for k in range(3)]
    bounds_center = yaml_vector([(a + b) / 2 for a, b in zip(bounds_min, bounds_max)])
    bounds_extent = yaml_vector([(b - a) / 2 for a, b in zip(bounds_min, bounds_max)])
    template = (ROOT / 'Assets/Models/FlowRoom/Board 92.asset').read_text()
    template = re.sub(r'm_Name: .*', 'm_Name: ' + name, template, count=1)
    template = re.sub(r'indexCount: \d+', f'indexCount: {len(indices)}', template)
    template = re.sub(r'vertexCount: \d+', f'vertexCount: {len(points)}', template)
    template = re.sub(r'm_VertexCount: \d+', f'm_VertexCount: {len(points)}', template)
    template = re.sub(r'm_Center: \{[^}]+\}', 'm_Center: ' + bounds_center, template)
    template = re.sub(r'm_Extent: \{[^}]+\}', 'm_Extent: ' + bounds_extent, template)
    template = re.sub(r'm_IndexBuffer: [0-9a-f]+', 'm_IndexBuffer: ' + struct.pack('<' + 'H'*len(indices), *indices).hex(), template)
    template = re.sub(r'm_DataSize: \d+', f'm_DataSize: {len(packed)}', template)
    template = re.sub(r'_typelessdata: [0-9a-f]+', '_typelessdata: ' + packed.hex(), template)
    channels = []
    for i in range(14):
        offset, dimension = {0: (0, 3), 1: (12, 3), 4: (24, 2)}.get(i, (0, 0))
        channels.append(f'    - stream: 0\n      offset: {offset}\n      format: 0\n      dimension: {dimension}\n')
    template = re.sub(r'    m_Channels:\n.*?(?=    m_DataSize:)', '    m_Channels:\n' + ''.join(channels), template, flags=re.S)
    path = OUT / (name + '.asset')
    path.write_text(template)
    native_meta(path, 4300000)
    print(name, len(faces), 'triangles, world size', [round((b-a)*scale[k], 4) for k, (a,b) in enumerate(zip(bounds_min,bounds_max))])
    return guid(path.name)


def material():
    texture_meta = OUT / 'Colormap.png.meta'
    if not texture_meta.exists():
        texture_meta.write_text('fileFormatVersion: 2\nguid: ' + guid('Colormap.png') + '\n'
            'TextureImporter:\n  externalObjects: {}\n  serializedVersion: 12\n'
            '  mipmaps:\n    enableMipMap: 1\n    sRGBTexture: 1\n'
            '  textureSettings:\n    serializedVersion: 2\n    filterMode: 1\n'
            '    aniso: 1\n    mipBias: 0\n    wrapU: 1\n    wrapV: 1\n    wrapW: 1\n'
            '  maxTextureSize: 512\n  textureCompression: 0\n'
            '  textureType: 0\n  textureShape: 1\n  userData: \n'
            '  assetBundleName: \n  assetBundleVariant: \n')
    template = (ROOT / 'Assets/Materials/ProductPlaceholders/ToastPlaceholder.mat').read_text()
    template = template.replace('m_Name: ToastPlaceholder', 'm_Name: Product colors')
    template = template.replace('m_EnableInstancingVariants: 0', 'm_EnableInstancingVariants: 1')
    template = template.replace('_Color: {r: 0.65, g: 0.34, b: 0.13, a: 1}', '_Color: {r: 1, g: 1, b: 1, a: 1}')
    template = template.replace('_Glossiness: 0.5', '_Glossiness: 0.15')
    template = template.replace('- _MainTex:\n        m_Texture: {fileID: 0}',
        '- _MainTex:\n        m_Texture: {fileID: 2800000, guid: ' + guid('Colormap.png') + ', type: 3}')
    path = OUT / 'ProductColors.mat'
    path.write_text(template)
    native_meta(path, 2100000)
    return guid(path.name)


def run():
    original = SCENE.read_text()
    matches = list(re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=--- !u!|\Z)', original, re.S))
    documents = {int(m[2]): (int(m[1]), m[3]) for m in matches}
    replacements = {}
    shared_material = material()
    products = 0
    for _, (kind, body) in documents.items():
        if kind != 114 or 'guid: ' + SCRIPT_GUID not in body:
            continue
        name = re.search(r'displayName: (.*)', body)[1]
        if name not in SOURCES:
            raise RuntimeError('Unknown product: ' + name)
        go_id = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)[1]
        components = [(i, t, b) for i, (t, b) in documents.items() if f'm_GameObject: {{fileID: {go_id}}}' in b]
        transform = next(b for i, t, b in components if t == 4)
        scale = vector(re.search(r'm_LocalScale: (.*)', transform)[1])
        mesh_guid = create_mesh(name, scale)
        for component_id, component_type, component in components:
            if component_type == 33:
                old = re.search(r'm_Mesh: (.*)', component)[1]
                expected = '{fileID: 4300000, guid: ' + mesh_guid + ', type: 2}'
                if 'fileID: 10202,' not in old and old != expected:
                    raise RuntimeError('Product already has a custom model: ' + name)
                replacements[component_id] = re.sub(r'm_Mesh: .*', 'm_Mesh: ' + expected, component)
            elif component_type == 23:
                replacements[component_id] = re.sub(r'(m_Materials:\n  - )\{[^}]+\}',
                    r'\g<1>{fileID: 2100000, guid: ' + shared_material + ', type: 2}', component)
        products += 1
    if products != 7:
        raise RuntimeError('Expected seven product instances, found ' + str(products))
    updated = original
    for match in reversed(matches):
        replacement = replacements.get(int(match[2]))
        if replacement is not None:
            updated = updated[:match.start(3)] + replacement + updated[match.end(3):]
    SCENE.write_text(updated)
    print('Replaced', products, 'product cubes. Scene changes:', len(replacements), 'mesh/material references.')


if __name__ == '__main__':
    run()
