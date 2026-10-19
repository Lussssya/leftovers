Microwave opening/closing, slam, and completion recordings supplied by the user in Downloads on October 17, 2026. Original filenames: microwave open and close.mp3, Microwave slam.mp3, microwave beep.mp3. Opening is the first event; closing is the second. The start cue is a 140 ms excerpt of the first completion tone. Cooking motor/fan hum is an original synthesized loop. Original recordings remain unchanged.

October 19 sound pass: opening, closing, and slam recordings were rebuilt with synthesized latch transients, damped metallic resonance, and low body impacts. Movement is a new quiet friction loop. Generated locally with Tools/Audio/build_microwave_sounds.py; original source recordings and existing asset GUIDs preserved. Beeps and cooking hum are unchanged.

Further polish: movement loop low-pass filtered and reduced; slam has two synthesized, short metallic latch/tray rattles. See Tools/Audio/refine_microwave_sounds.py.

Gentle close shortened to a 160 ms latch, with the resonant low-frequency body and late tail removed; 45 ms fade prevents a cutoff click. See Tools/Audio/trim_gentle_close.py.

Normal-play correction: supplied start/end beeps low-pass filtered at 2.4 kHz with reduced gain and softened edges. Heating loop regenerated as quiet 100/150/200 Hz motor plus band-limited fan. Runtime sources remain positional, with reduced volumes and shorter distance reach. Generator: Tools/Audio/soften_microwave_motor.py.

Natural endings pass: opening re-extracted from supplied recording with a longer tail; gentle close rebuilt as an original short mechanical latch without the source's heavy closing impact. Slam/start/end cues tapered to silence. Heating audio now ramps over 0.25 seconds; microwave hinge fades more slowly. Fridge keeps its original fade speed. Tools/Audio/microwave_natural_tails.py reproduces the clip changes.
