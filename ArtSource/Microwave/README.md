# Polished microwave

Editable Blender source: Microwave_Polished.blend. FBX export is included.
The active Blender scene contains the completed model. Rotate the door hinge
around its local Z axis to open; the turntable has its own pivot.
The model has 7,362 triangles across 45 mesh parts.

The Unity prefab uses native meshes converted from this Blender geometry in
Assets/Models/Microwave/Polished. Unity dimensions are 0.66 m wide before the
existing scene scale. Front is +Z, vertical is Y; the door pivot remains separate.
The original supplied sandwich and plate are reused.

MicrowaveDisplay selects a tile from RedTimerAtlas.png on a flat screen plane.
The keypad is decorative surface artwork. Door closing starts cooking as before.
The perforated window is a thin, two-sided translucent texture layer.

Open Assets/Scenes/Microwave Fridge Flow.unity and press Play. Open the fridge,
drag the sandwich plate into the open microwave, release, and close the door.
Check 00:05 counts down in red, the tray spins, and zero beeps and lights up.
Open within the final two seconds to cook early; open cooked food to see steam.
Try gentle closes, slams, and holding the plate in the door's path.

Existing sound, delay, heat, light, shake, collision, and steam settings are retained.
A soft fill light in the flow scene shows the enamel and matches the fridge style.

Tools/Blender contains the texture generator, Blender modeling script and Unity
installation script. The installation script is a one-time migration for a
fresh copy of the previous prefab, not a tool to rerun on the finished prefab.
Checks/results.txt records the successful isolated Unity Play-mode run.
