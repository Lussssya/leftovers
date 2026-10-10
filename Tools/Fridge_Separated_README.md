# Separated fridge model

Added `Assets/Models/Fridge/Fridge_Separated.fbx` and its Unity `.meta` file.
The original `Fridge_001.fbx`, its importer settings, and Kitchen scene are unchanged.

## Structure

- Fridge_Separated: identity root transform.
- Body: cabinet shell, fixed hinge hardware, and feet.
- UpperDoor: upper panel, trim/seal, and handle.
- LowerDoor: lower panel, trim/seal, and handle.

The new model is Y-up, faces -Z, and is approximately 1.95 meters tall at
scale (1, 1, 1). This is a deliberate prototype size, not a size measured from
the artist's reference. Do not copy the old scene instance's 121.246 scale or
rotation onto this asset. The two door origins sit along the existing left
hinge hardware; positive local Y rotation opens them outward.

All 560 source vertices and 504 polygons are retained, including the original
UV assignments, normals, and plain material. No rig, textures, animation clips,
colliders, physics, gameplay scripts, or interior shelf props were added.
The source model's proportions are retained; the upper compartment is larger.

## Test in Unity

1. Open the project with Unity 2022.3.62f3 and wait for import to finish.
2. Check the Console for import errors.
3. Create a temporary empty scene without saving changes to Kitchen.
4. Drag Fridge_Separated.fbx from Assets/Models/Fridge into the scene.
5. Set its root position/rotation to zero and scale to (1, 1, 1).
6. Expand the model in the Hierarchy. Select UpperDoor. Set its local Y rotation
   to 90, then 110. Its handle and trim should move with it while the body and
   lower door stay still. Reset rotation to (0, 0, 0).
7. Repeat for LowerDoor, then open both. Inspect the hinges closely for overlap
   before choosing final collision shapes and angle limits.
8. Reset both rotations to zero. Confirm the closed model has no missing faces
   or detached handles. Check front, back, and interior views.
9. Discard the temporary scene when finished, or save it under a new name.

These are manual Editor transform checks, not Play Mode interaction. A later
step can make a prefab with colliders and door-control components after import
and pivot placement are approved through testing.

## Rebuilding and verification

Run `python Tools/prepare_fridge.py` with Python 3; no extra packages are needed.
The utility checks the source fingerprint, splits known disconnected components,
preserves per-corner normal/UV data, and verifies that all polygons are assigned
exactly once. It checks that closed geometry reconstructs the original shape
after the documented coordinate/size conversion, then round-trips the output
through its FBX reader. It never modifies the source FBX or the Unity scene.

Geometry was previewed closed and with both doors rotated 100 degrees. Unity
import/Play Mode has not been verified: the required Editor version is not
installed on the inspected machine.
