**The Impact of Actuated Tangible Objects on Group Awareness in Remote
Collaboration**

Run the simulator version on a single computer (Windows/Mac)
- Install Unity 2021.3.38
- Load the scene in Assets/Scenes
- Skip the calibration step by hitting the esc key (Using real TOIO devices, the projection surface does not match exactly with the side of the mat, to address this problem, we need to calibrate the projection with mat coordinates. )
- The TOIOs need to be placed in their starting box to begin the level (or hit space key)
- Drag the TOIOs using mouse right click to push the crates of their color to the targets (glowing spheres)
- Push 's' to undo the last crate movement for the green TOIO and 'd' for the blue one. Because we need to rewind to a consistent state, every movement done by the other TOIO after the one being undone will be undo too.
- Push 'enter' to restart from the beginning of the level
- The TOIO will move to their last known good position after a couple of seconds if dragged to an unreachable spot
- Hit space to abort and go to the next level
