# Fridge interaction

Hold the left mouse button on either handle and drag to open or close. Release
to let the door coast. Escape releases safely. The inside handle area stays
available when an open door hides the outside handle. There are no dots,
arrows, grip labels, noise readouts, or other fridge UI.

Gentle closes make a soft sound. Slams make a louder impact and briefly shake
the fridge. A thin ring of small low-poly puffs escapes from just behind the closing
door's edges, spreads outward, and fades within a third of a second. Gentle closes emit no puffs.
Existing door sensitivity, slam thresholds, and scene overrides
are preserved.

## Inspector settings

- On UpperDoor and LowerDoor, FridgeDoor has a Slam Volume slider from 0 to 2.
  A value of 1 keeps the old maximum loudness at the midpoint; 2 doubles the
  playback gain. Existing saved volume values are preserved.
  This adjusts only the slam recording, leaving detection and gameplay noise
  events unchanged. Set both doors if you want matching volumes.
- On Fridge_LowPoly_Rigged, FridgeSlamEffect controls Shake Angle (0.6 degrees)
  and Shake Seconds (0.25). The model returns exactly to its resting rotation.
- Inside hum has Closed Volume (0.035), Open Volume (0.14), Full Volume Angle
  (35 degrees), and Fade Speed (0.3). Either door exposes the same interior hum;
  opening both does not double it.

## Test in Unity

1. Let Unity finish compiling, then enter Play mode in Kitchen or FridgeInteraction.
2. Slowly open and close each door: soft close, no shake, no overlays.
3. Close each door quickly: a slam, short shake, and thin white puffs around
   that door's perimeter. The other door should not emit particles.
4. Adjust each door's Slam Volume slider. Zero mutes that slam recording while
   keeping the shake; opening, handling, gentle-close, and hum sounds remain.
5. Open either door and listen for the hum increasing. Close both to quiet it.

Changes made to sliders during Play mode revert when Play mode ends. Set the
final values outside Play mode to keep them.

The scripts use plain C# and preserve existing serialized field names. The
hinges are controlled directly for mouse response; they do not yet stop against
arbitrary props. Noise events are available for later gameplay integration.
Audio credits are in Assets/Audio/Fridge/CREDITS.md.

Tools/FridgePrototypeTools.cs is a historical disposable-project builder. It
recreates old defaults; do not run it against the working project or tuned prefab.
