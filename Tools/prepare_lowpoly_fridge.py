"""Prepare Fridge.fbx as a separate, textured model with two hinge pivots.

Uses the FBX reader/writer from prepare_fridge.py. No third-party dependencies.
Run with Python 3 from any directory. Original assets and scenes are untouched.
"""
import copy
import hashlib
import math
from pathlib import Path
from prepare_fridge import read_fbx, write_node, child, triples

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / 'Assets/Models/Fridge'
SOURCE = FOLDER / 'Fridge.fbx'
OUTPUT = FOLDER / 'Fridge_LowPoly_Rigged.fbx'
TEXTURE = FOLDER / 'Fridge_LowPoly_Atlas.png'
HASH = '1a024a098153e1c0227a55856a26a01bfd0d509cdc39c14458f066d829dc86c4'


def faces_of(g):
    faces, face = [], []
    for index in child(g, 'PolygonVertexIndex')[1][0][1]:
        face.append(index if index >= 0 else -index - 1)
        if index < 0:
            faces.append(face)
            face = []
    return faces


def build():
    original = SOURCE.read_bytes()
    assert hashlib.sha256(original).hexdigest() == HASH, 'Source changed; inspect before rebuilding.'
    nodes, footer = read_fbx(original)
    objects = next(n for n in nodes if n[0] == 'Objects')
    geometries = [n for n in objects[2] if n[0] == 'Geometry']
    models = [n for n in objects[2] if n[0] == 'Model']
    material = child(objects, 'Material')
    textures = [child(v, 'Content')[1][0][1] for v in objects[2]
                if v[0] == 'Video' and any(n[0] == 'Content' for n in v[2])]
    assert len(textures) == 1 and textures[0].startswith(b'\x89PNG')

    # Bake source object transforms (FBX centimeters) to Y-up meters.
    def properties(model):
        return {p[1][0][1]: [v for _, v in p[1][4:]] for p in child(model, 'Properties70')[2]}

    def rotate(v, model):
        angles = properties(model).get(b'Lcl Rotation', [0, 0, 0])
        assert abs(angles[1]) < 1e-9 and abs(angles[2]) < 1e-9
        a = math.radians(angles[0]); x, y, z = v
        return x, y * math.cos(a) - z * math.sin(a), y * math.sin(a) + z * math.cos(a)

    def world(v, model):
        p = properties(model)
        v = rotate([a * b for a, b in zip(v, p.get(b'Lcl Scaling', [1, 1, 1]))], model)
        return tuple((a + b) / 100 for a, b in zip(v, p.get(b'Lcl Translation', [0, 0, 0])))

    world_vertices = [[world(v, m) for v in triples(child(g, 'Vertices')[1][0][1])]
                      for g, m in zip(geometries, models)]
    bottom = min(v[1] for vs in world_vertices for v in vs)
    top = max(v[1] for vs in world_vertices for v in vs)
    scale = 1.95 / (top - bottom)

    def normalized(v):
        return v[0] * scale, (v[1] - bottom) * scale, v[2] * scale

    door_vertices = triples(child(geometries[1], 'Vertices')[1][0][1])
    upper = [i for i, v in enumerate(door_vertices) if v[1] > 0]
    lower = [i for i, v in enumerate(door_vertices) if v[1] < 0]
    assert len(upper) == len(lower) == 219
    # The artist's door origin already lies on the right-hand hinge edge.
    hinge = normalized(world((0, 0, 0), models[1]))
    parts = [('Body', 0, list(range(len(world_vertices[0]))), (0, 0, 0)),
             ('UpperDoor', 1, upper, hinge),
             ('LowerDoor', 1, lower, hinge)]

    def make_model(ident, name, kind, pivot):
        m = copy.deepcopy(models[0])
        m[1] = [('L', ident), ('S', name.encode() + b'\x00\x01Model'), ('S', kind)]
        p = child(m, 'Properties70')
        p[2] = [n for n in p[2] if n[1][0][1] not in (b'Lcl Rotation', b'Lcl Scaling', b'Lcl Translation')]
        for key, vals in [(b'Lcl Translation', pivot), (b'Lcl Rotation', (0, 0, 0)), (b'Lcl Scaling', (1, 1, 1))]:
            p[2].append(['P', [('S', key), ('S', key), ('S', b''), ('S', b'A')] + [('D', x) for x in vals], []])
        return m

    root_id = 100000
    new_objects = [make_model(root_id, 'Fridge_LowPoly_Rigged', b'Null', (0, 0, 0)), material]
    connections = [['C', [('S', b'OO'), ('L', root_id), ('L', 0)], []]]
    emitted = [[], []]
    total_vertices = 0
    for part, (name, gi, selected, pivot) in enumerate(parts):
        source = geometries[gi]
        g = copy.deepcopy(source)
        gid, mid = 100010 + part, 100020 + part
        g[1] = [('L', gid), ('S', name.encode() + b'\x00\x01Geometry'), ('S', b'Mesh')]
        remap = {old: new for new, old in enumerate(selected)}
        local = [tuple(a - b for a, b in zip(normalized(world_vertices[gi][i]), pivot)) for i in selected]
        for i, v in zip(selected, local):
            assert max(abs(a + b - c) for a, b, c in zip(v, pivot, normalized(world_vertices[gi][i]))) < 1e-12
        child(g, 'Vertices')[1] = [('d', [x for v in local for x in v])]
        original_normals = triples(child(child(source, 'LayerElementNormal'), 'Normals')[1][0][1])
        original_uv = child(child(source, 'LayerElementUV'), 'UVIndex')[1][0][1]
        indices, normals, uv, offset = [], [], [], 0
        for fi, face in enumerate(faces_of(source)):
            if face[0] in remap:
                assert all(i in remap for i in face), 'A polygon crosses the door split.'
                indices.extend([remap[i] for i in face[:-1]] + [-remap[face[-1]] - 1])
                normals.extend(x for n in original_normals[offset:offset + len(face)] for x in rotate(n, models[gi]))
                uv.extend(original_uv[offset:offset + len(face)])
                emitted[gi].append(fi)
            offset += len(face)
        child(g, 'PolygonVertexIndex')[1] = [('i', indices)]
        child(child(g, 'LayerElementNormal'), 'Normals')[1] = [('d', normals)]
        child(child(g, 'LayerElementUV'), 'UVIndex')[1] = [('i', uv)]
        g[2] = [n for n in g[2] if n[0] != 'Edges']
        new_objects.extend([g, make_model(mid, name, b'Mesh', pivot)])
        for a, b in [(gid, mid), (mid, root_id), (material[1][0][1], mid)]:
            connections.append(['C', [('S', b'OO'), ('L', a), ('L', b)], []])
        total_vertices += len(selected)
        print(name, len(selected), 'vertices;', len(faces_of(g)), 'polygons; pivot', pivot)
    for gi in range(2):
        assert sorted(emitted[gi]) == list(range(len(faces_of(geometries[gi]))))
    assert total_vertices == 635
    # Keep embedded texture data and links as well as providing a portable PNG.
    new_objects.extend(n for n in objects[2] if n[0] in ('Texture', 'Video'))
    old_connections = next(n for n in nodes if n[0] == 'Connections')
    extra_ids = {n[1][0][1] for n in new_objects if n[0] in ('Texture', 'Video')}
    connections.extend(n for n in old_connections[2] if n[1][1][1] in extra_ids)
    for n in new_objects:
        if n[0] in ('Texture', 'Video'):
            for p in n[2]:
                if p[0] in ('FileName', 'Filename', 'RelativeFilename'):
                    p[1] = [('S', TEXTURE.name.encode())]
                if p[0] == 'Properties70':
                    for prop in p[2]:
                        if prop[1][0][1] == b'Path':
                            prop[1][-1] = ('S', TEXTURE.name.encode())
    objects[2] = new_objects
    old_connections[2] = connections
    for p in child(next(n for n in nodes if n[0] == 'GlobalSettings'), 'Properties70')[2]:
        if p[1][0][1] in (b'UnitScaleFactor', b'OriginalUnitScaleFactor'):
            p[1][-1] = ('D', 100.0)
    definitions = next(n for n in nodes if n[0] == 'Definitions')
    for n in definitions[2]:
        if n[0] == 'ObjectType' and n[1][0][1] in (b'Model', b'Geometry'):
            child(n, 'Count')[1] = [('I', 4 if n[1][0][1] == b'Model' else 3)]
    child(definitions, 'Count')[1] = [('I', sum(child(n, 'Count')[1][0][1] for n in definitions[2] if n[0] == 'ObjectType'))]
    result = original[:27]
    for n in nodes:
        result += write_node(n, len(result))
    result += bytes(13) + footer
    parsed, _ = read_fbx(result)
    assert next(n for n in parsed if n[0] == 'Objects')[2] == new_objects
    OUTPUT.write_bytes(result)
    TEXTURE.write_bytes(textures[0])
    assert SOURCE.read_bytes() == original
    print('Validated 635 vertices and 574 polygons, UVs, normals, closed shape, and FBX round-trip.')


if __name__ == '__main__':
    build()
