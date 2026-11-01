# Microwave test

Open Assets/Scenes/Kitchen.unity in Unity 2022.3.62f3. The microwave and cabinet are on the left, the fridge is on the right, and
the camera is centered. The sandwich plate starts on a shelf inside the fridge.

## Controls

1. Hold the left mouse button on the microwave handle and drag, just like the
   fridge. Open the door widely before loading or removing the plate.
2. Open the fridge first. Hold the left mouse button on the sandwich plate, drag it out and over the open
   turntable, and release. It snaps into place. Releasing near the open fridge shelf returns
   it to its resting spot. Releasing elsewhere drops the plate with gravity.
   Escape also drops the plate. Solid doors and walls stop dragging.
3. Close the door. The plate and sandwich spin and the physical display counts
   down from 00:05. The inside light stays off while cooking.
4. Open with more than two seconds left: cooking and spinning pause. Close to
   resume the same food's remaining cooking time.
5. Open with two seconds or less left: the sandwich becomes cooked, the cavity
   vents steam, and the sandwich starts steaming. You can take the hot plate
   out of the microwave; its steam follows it.
6. Let the timer reach zero: the motor stops, the microwave beeps, and the
   inside light turns on. Opening releases the chamber steam.
7. Remove the food and close the empty microwave: the light turns off and it
   stays idle. Stop and restart Play mode for a fresh raw sandwich.

The light comes on 0.12 seconds after opening and switches off on closure.
Gentle and slammed closes use the same hinge and impact-speed detection as
the fridge, with the user's recorded microwave opening, closing, and slam clips.
Closing loaded uncooked food plays the closing clip first, then starts cooking
after 0.22 seconds with one quieter short beep. The full four-beep
completion recording plays once when the timer reaches zero. Closing empty
or already cooked food does not beep or start the motor. A slam also shakes the microwave.

## Tuning

On Microwave_Interactive / Microwave: Cooking Seconds, Cooked Window,
Light Delay, Plate Speed, Steam Seconds, and Steam Rate (32 particles/second). Start Delay separates closure from the beep.
Cooking sound / AudioSource controls the fuller motor/fan hum; Volume defaults
to 0.7. The start cue is a 0.14-second excerpt of the first completion tone.

On Microwave model / Microwave door / FridgeDoor: the existing hinge settings
and Slam Volume (0 to 2). The class name is retained so both appliances share
the working door code.

On Microwave model / FridgeSlamEffect: Shake Angle and Shake Seconds.

Chamber steam and Food steam use the same faceted puff mesh and material as
the fridge, with upward movement, fading, and 3D collision against the door.
The microwave has a matching enamel handle and dark translucent glass with
a fine hexagonal screen. DoorCollision checks the swept door movement before
applying it. Plate and sandwich colliders stop held objects moving through
solid surfaces; a blocked door cannot start cooking.

## Assets and code

- Assets/Prefabs/Microwave_Interactive.prefab: complete appliance.
- Assets/Prefabs/Sandwich_Test.prefab: ceramic plate, sandwich, cooking state,
  and food steam.
- Assets/Models/Microwave/Downloaded: converted geometry and original textures
  from microwave-oven.zip, plates.zip, and sandvich.zip. Units are metres; Y is
  up and +Z is the closed appliance's front. Door, shell, and turntable are
  separate. CREDITS.md and SOURCE_FILES.json identify the source files.
- Older generated assets are retained for existing counter, timer and lamp
  references; the microwave, plate, and sandwich use the downloaded geometry.
- Assets/Audio/Microwave: the user's supplied opening/closing, slam, and
  completion recordings, plus a cropped start cue and a fuller synthesized
  motor/fan loop. The combined recording is split with opening first.
- Microwave.cs: door state, timer, cooking, delayed light, and chamber steam.
- MicrowaveFood.cs: persistent heat/cooked state and food steam.
- MicrowaveDisplay.cs: simple seven-segment mesh digits on the appliance.
- MouseFoodInteractor.cs: mouse dragging and plate placement.

The current appliance, plate, and sandwich use the models supplied by the user
in Downloads. The digital display and existing gameplay components are reused.

Tools/BuildMicrowave.cs is the reproducible asset builder used in a disposable
copy of the project. Do not run it against the working project: it regenerates
the test assets and initial scene arrangement.

## Verification

Tools/DoorCorrectionChecks holds the latest isolated Play-mode check scripts.
They cover all three doors opening and closing freely, stopping against scene
obstacles, resuming after removal, picking food through the open fridge,
transferring the plate into the microwave, delayed start audio, cooking and
completion, low-poly steam configuration, held food stopping at the closed
door, the door stopping against food, and dropped food landing on the floor.
Rendered previews verify the handle, hexagonal window, and left/right layout.
The existing fridge hinge and slam settings are preserved.

Tools/FixApplianceDoors.cs applies these prefab and scene corrections once in
an isolated project. It is not an incremental tool; do not run it again on the
working project because it creates the added components and assets.

Tools/prepare_downloaded_microwave.py converts the supplied source geometry and
recordings; Tools/UseDownloadedMicrowave.cs updates the prefab visuals in a
disposable Unity copy. They preserve the existing prefab and component identities.
