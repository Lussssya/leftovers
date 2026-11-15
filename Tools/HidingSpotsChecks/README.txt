Run these checks in a separate copy of the Unity project.
The checks move products and open containers in memory. They do not save those changes.

Copy CheckHidingSpots.cs into Assets, outside an Editor folder.
Start Unity 2022.3.62f3 with these arguments:

-batchmode -projectPath <project-copy> -executeMethod CheckHidingSpots.Run -logFile <log-path>

For Environment.unity, use CheckHidingSpots.RunEnvironment as the executeMethod value.

Do not add -quit. The checks exit Unity with code 0 after success or code 1 after a failure.
The copy's root folder receives hiding-spots-results.txt and three images of the hiding spots.

The Kitchen checks cover all seven existing products at all three spots.
The Environment checks cover Cheese and Ketchup at all three spots with the existing first-person controller.
They test space, support height, solid overlaps, blocked pickup through closed containers,
visible models, mouse targeting, walking routes, and the current camera limits.
They also test drawer movement, random selection, duplicate products, and basket collection.

The Kitchen checks disable TodoCanvas in memory because its existing font material throws an exception.
Environment has no TodoCanvas and does not need this step.
They do not test the todo-list UI or physical keyboard input.

For a manual test, open Assets/Scenes/Environment.unity and enter Play mode.
Read the product request. Search the fridge, upper cabinet, and drawer beneath the microwave.
Open containers with the existing mouse controls. Aim at the requested product and press E.
Collect both requested products. Restart Play mode to run another search.
