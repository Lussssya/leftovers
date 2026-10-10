"""Split the original LEFTOVERS fridge without third-party dependencies.

Run from any directory with Python 3. Produces a new FBX; never edits the source.
The component assignments below are specific to Fridge_001.fbx.
"""
import copy
import hashlib
from pathlib import Path
import struct
import zlib

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/Models/Fridge/Fridge_001.fbx'
OUTPUT = SOURCE.with_name('Fridge_Separated.fbx')
FORMATS = dict(Y='h', C='?', I='i', F='f', D='d', L='q',
               d='d', f='f', i='i', l='q', b='?')


def read_fbx(data):
    assert data[:23] == b'Kaydara FBX Binary  \x00\x1a\x00'
    assert struct.unpack_from('<I', data, 23)[0] == 7400

    def read_node(pos):
        end, count, _, length = struct.unpack_from('<IIIB', data, pos)
        pos += 13
        if not end:
            return None, pos
        name = data[pos:pos + length].decode()
        pos += length
        props = []
        for _ in range(count):
            kind = chr(data[pos])
            pos += 1
            if kind in 'YCIFDL':
                fmt = '<' + FORMATS[kind]
                value = struct.unpack_from(fmt, data, pos)[0]
                pos += struct.calcsize(fmt)
            elif kind in 'SR':
                size = struct.unpack_from('<I', data, pos)[0]
                pos += 4
                value = data[pos:pos + size]
                pos += size
            else:
                count_array, encoding, size = struct.unpack_from('<III', data, pos)
                pos += 12
                raw = data[pos:pos + size]
                pos += size
                if encoding:
                    raw = zlib.decompress(raw)
                value = list(struct.unpack('<' + str(count_array) + FORMATS[kind], raw))
            props.append((kind, value))
        children = []
        while pos < end - 13:
            child, pos = read_node(pos)
            if child:
                children.append(child)
        return [name, props, children], end

    nodes, pos = [], 27
    while True:
        node, pos = read_node(pos)
        if node is None:
            break
        nodes.append(node)
    return nodes, data[pos:]


def write_node(node, offset):
    name, props, children = node
    encoded = bytearray()
    for kind, value in props:
        encoded.extend(kind.encode())
        if kind in 'YCIFDL':
            encoded.extend(struct.pack('<' + FORMATS[kind], value))
        elif kind in 'SR':
            encoded.extend(struct.pack('<I', len(value)) + value)
        else:
            raw = struct.pack('<' + str(len(value)) + FORMATS[kind], *value)
            packed = zlib.compress(raw)
            encoded.extend(struct.pack('<III', len(value), 1, len(packed)) + packed)
    content = name.encode() + encoded
    for child in children:
        content += write_node(child, offset + 13 + len(content))
    if children or not props:
        content += bytes(13)
    return struct.pack('<IIIB', offset + 13 + len(content), len(props),
                       len(encoded), len(name)) + content


def child(node, name):
    return next(n for n in node[2] if n[0] == name)


def triples(values):
    return list(zip(values[::3], values[1::3], values[2::3]))


def convert(v):
    # Source is Z-up, front -X. New model is Y-up, front -Z, in meters.
    return (v[1] * .4, v[2] * .4, v[0] * .4)


def build():
    original = SOURCE.read_bytes()
    assert hashlib.sha256(original).hexdigest() == '89e8df18a322dbc3b27dbc0bd778893ce17f84f529b2a6bcba00407ecc560d5d', 'Source changed: re-inspect component assignments before rebuilding.'
    nodes, footer = read_fbx(original)
    objects = next(n for n in nodes if n[0] == 'Objects')
    geometry = child(objects, 'Geometry')
    model = child(objects, 'Model')
    material = child(objects, 'Material')
    verts = triples(child(geometry, 'Vertices')[1][0][1])
    indices = child(geometry, 'PolygonVertexIndex')[1][0][1]
    faces, face = [], []
    for index in indices:
        face.append(index if index >= 0 else -index - 1)
        if index < 0:
            faces.append(face)
            face = []
    parent = list(range(len(verts)))

    def root(i):
        while parent[i] != i:
            i = parent[i]
        return i

    for face in faces:
        for i in face[1:]:
            parent[root(i)] = root(face[0])
    groups = {}
    for i in range(len(verts)):
        groups.setdefault(root(i), []).append(i)
    groups = list(groups.values())
    assert len(verts) == 560 and len(faces) == 504 and len(groups) == 17
    normals = triples(child(child(geometry, 'LayerElementNormal'), 'Normals')[1][0][1])
    uv_indices = child(child(geometry, 'LayerElementUV'), 'UVIndex')[1][0][1]
    assert len(normals) == len(indices) == len(uv_indices)
    assignments = [('Body', range(11), (0, 0, 0)),
                   ('UpperDoor', [11, 12, 13], convert((-1.072, -.718, 2.15))),
                   ('LowerDoor', [14, 15, 16], convert((-1.072, -.718, .02)))]

    def make_model(ident, name, kind, pivot):
        m = copy.deepcopy(model)
        m[1] = [('L', ident), ('S', name.encode() + b'\x00\x01Model'), ('S', kind)]
        for p in child(m, 'Properties70')[2]:
            key = p[1][0][1]
            if key in (b'Lcl Translation', b'Lcl Rotation', b'Lcl Scaling'):
                values = pivot if key == b'Lcl Translation' else ((1, 1, 1) if key == b'Lcl Scaling' else (0, 0, 0))
                p[1][4:] = [('D', v) for v in values]
        return m

    root_id = 100000
    new_objects = [make_model(root_id, 'Fridge_Separated', b'Null', (0, 0, 0)), material]
    connections = [['C', [('S', b'OO'), ('L', root_id), ('L', 0)], []]]
    emitted, rebuilt = [], []
    for part, (name, component_ids, pivot) in enumerate(assignments):
        selected = sorted(i for k in component_ids for i in groups[k])
        remap = {old: new for new, old in enumerate(selected)}
        local = [tuple(a - b for a, b in zip(convert(verts[i]), pivot)) for i in selected]
        g = copy.deepcopy(geometry)
        gid, mid = 100010 + part, 100020 + part
        g[1] = [('L', gid), ('S', name.encode() + b'\x00\x01Geometry'), ('S', b'Mesh')]
        child(g, 'Vertices')[1] = [('d', [v for point in local for v in point])]
        poly, norm, uv, corner = [], [], [], 0
        for fi, face in enumerate(faces):
            if face[0] in remap:
                assert all(i in remap for i in face)
                poly.extend([remap[i] for i in face[:-1]] + [-remap[face[-1]] - 1])
                for nx, ny, nz in normals[corner:corner + len(face)]:
                    norm.extend((ny, nz, nx))
                uv.extend(uv_indices[corner:corner + len(face)])
                emitted.append(fi)
            corner += len(face)
        child(g, 'PolygonVertexIndex')[1] = [('i', poly)]
        child(child(g, 'LayerElementNormal'), 'Normals')[1] = [('d', norm)]
        child(child(g, 'LayerElementUV'), 'UVIndex')[1] = [('i', uv)]
        g[2] = [n for n in g[2] if n[0] != 'Edges']
        new_objects.extend([g, make_model(mid, name, b'Mesh', pivot)])
        for source, target in [(gid, mid), (mid, root_id), (material[1][0][1], mid)]:
            connections.append(['C', [('S', b'OO'), ('L', source), ('L', target)], []])
        rebuilt.extend((i, tuple(a + b for a, b in zip(p, pivot))) for i, p in zip(selected, local))
        print(name, len(selected), 'vertices; hinge/local origin', pivot)
    assert sorted(emitted) == list(range(len(faces)))
    assert len(rebuilt) == len(verts)
    assert all(max(abs(a - b) for a, b in zip(point, convert(verts[i]))) < 1e-12 for i, point in rebuilt)
    objects[2] = new_objects
    next(n for n in nodes if n[0] == 'Connections')[2] = connections
    settings = child(next(n for n in nodes if n[0] == 'GlobalSettings'), 'Properties70')
    for p in settings[2]:
        if p[1][0][1] in (b'UnitScaleFactor', b'OriginalUnitScaleFactor'):
            p[1][-1] = ('D', 100.0)
    definitions = next(n for n in nodes if n[0] == 'Definitions')
    counts = {b'Model': 4, b'Geometry': 3, b'Material': 1}
    for n in definitions[2]:
        if n[0] == 'ObjectType' and n[1][0][1] in counts:
            child(n, 'Count')[1] = [('I', counts[n[1][0][1]])]
    child(definitions, 'Count')[1] = [('I', sum(child(n, 'Count')[1][0][1] for n in definitions[2] if n[0] == 'ObjectType'))]
    result = original[:27]
    for n in nodes:
        result += write_node(n, len(result))
    result += bytes(13) + footer
    parsed, _ = read_fbx(result)
    parsed_objects = next(n for n in parsed if n[0] == 'Objects')[2]
    assert len(parsed_objects) == 8
    assert parsed_objects == new_objects, 'FBX serialization round-trip failed'
    OUTPUT.write_bytes(result)
    assert SOURCE.read_bytes() == original
    print('Validated: 560 vertices, 504 polygons, normals and UV corner assignments preserved.')
    print('Source SHA256:', hashlib.sha256(original).hexdigest())
    print('Wrote', OUTPUT)


if __name__ == '__main__':
    build()
