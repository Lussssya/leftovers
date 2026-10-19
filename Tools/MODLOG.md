# Microwave asset integration, October 17, 2026

Target reference: the existing fridge uses metre-scale geometry, Y up, +Z at
the front, separate door pivots, opaque baked colour, and simple readable shapes.

The supplied microwave has 640 body triangles after removing the top dial,
158 door triangles, and 439 turntable triangles. Its original textures are
preserved at 2048 pixels, with a separate transparent window material.
The supplied shallow ceramic plate and complete sandwich replace the earlier
generated geometry. The sandwich keeps its original embedded colour texture.

The supplied door is open in the source file. Its geometry is rotated into a
closed pose about a separate left hinge, preserving UVs and surface normals.
The gameplay hinge, collider, handles, turntable and existing prefab identities
remain the integration points. The timer covers the original top dial position.

Recorded opening, closing, slam and completion cues are converted to mono
48 kHz PCM WAV. A 140 ms excerpt of the first completion tone is the start cue.
The motor/fan loop is fuller and louder. Steam uses a soft-alpha 16-frame wisp
atlas, elongated billboards, overlapping plumes, upward force and door collisions.

Play-mode checks and rendered views verify placement, surface appearance,
readable digits, audio transitions, cooking state, and steam deflection. Exact
source filenames and hashes are in the Downloaded model folder.


## Door and layout corrections

Added a matching minimal enamel handle and corrected the closed door position
by 0.0233 metres. Restored smoked glass with original surface colour and a fine
hexagonal screen. Moved the cabinet/microwave left, fridge right, centered the
camera, and placed the sandwich on an existing fridge shelf.

Closure now happens before the quieter start beep (0.22 second delay). Both
steam systems use the fridge's faceted puff mesh and material. Doors check
their swept path against objects; held plate and sandwich movement casts
against solid colliders. Dropped food uses gravity instead of teleporting.
Existing fridge hinge, slam, shake, audio and other tuned fields are preserved.

An isolated Unity Play-mode run passed the checks recorded in
Tools/DoorCorrectionChecks/results.txt. Closed/open scene renders and a window
closeup were inspected. The verified prefabs, scene and added assets were then
copied back without running tools in the user's live Editor.
