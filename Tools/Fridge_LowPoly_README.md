# Low-poly fridge with independent doors

Use `Assets/Models/Fridge/Fridge_LowPoly_Rigged.fbx` in Unity 2022.3.62f3.
The source `Fridge.fbx`, previous fridge assets, Kitchen scene, packages, and
project settings are unchanged.

## Added assets

- `Fridge_LowPoly_Rigged.fbx`: Body, UpperDoor, and LowerDoor under one root.
- `Fridge_LowPoly_Atlas.png`: the artist's embedded 512 x 512 color atlas,
  extracted without modification.
- `Fridge_LowPoly.mat`: Standard material using that atlas. The FBX importer
  explicitly maps its Atlas material to this asset.
- A Unity `.meta` file for each asset, preserving material and texture links.
- `Tools/prepare_lowpoly_fridge.py`: reproducible preparation utility using the
  existing `prepare_fridge.py` FBX reader/writer.

Each door includes its panel, trim, and handle. The cabinet and shelves remain
in Body. The hinge origins follow the artist's original hinge line. Both
origins share a point at the gap between doors; rotating around vertical Y
still turns each door around its full hinge edge.

The copy is grounded at Y=0, Y-up, faces +Z, and is 1.95 meters tall at root
scale (1, 1, 1). Source proportions are retained. This is a deliberate prototype
size. Do not copy the old fridge's large scale or corrective rotation.

## Unity test

1. Open a temporary scene and drag Fridge_LowPoly_Rigged.fbx into it.
2. Set the root position and rotation to zero and scale to (1, 1, 1).
3. Expand the instance in the Hierarchy. Select UpperDoor.
4. Set local Rotation Y to -45, then -90, then -110 degrees. It should swing
   outward with its handle attached. Body and LowerDoor should stay still.
5. Set Y back to 0 to close it. Repeat for LowerDoor.
6. Open both doors and inspect the shelves, interior, and textured surfaces.
7. Check the Console for errors. Discard the temporary scene when finished.

These are Editor transform checks. This asset has hinge pivots, not an Animator
or skeleton. Player interaction, physics joints, colliders, angle limits, and
slam-noise gameplay are not included in this asset-preparation step.

## Verification

The utility preserves all 635 source vertices and 574 source polygons, the UV
assignments, and normals. It checks every polygon is assigned exactly once,
closed geometry matches the transformed source, and the FBX binary round-trip
preserves its object data. The source fingerprint prevents accidental reuse
with an incompatible model.

An isolated Unity 2022.3.62f3 project imported three textured meshes (1,186
triangles) and checked the 1.95-meter height, material mapping, fixed hinge
positions, constant vertex distances from each hinge, and return to the closed
pose. The outward-opening test uses local Y=-90. No tests modify Kitchen.

Rebuild the FBX/PNG with `python -B Tools/prepare_lowpoly_fridge.py`.
Keep the supplied `.meta` files and material; rebuilding does not replace them.
