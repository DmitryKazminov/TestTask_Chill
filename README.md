# TestTask_Chill

A small Unity prototype where you can walk around in third person, hop into a car, drive around, and follow simple mission routes.

## What's included

### Character
Standard third-person controls. You can freely explore the level, look around with the mouse, and move through the environment.

### Vehicle
Walk up to the car and press **F** to get in.  
Once inside, you control the vehicle — acceleration, braking, and steering.  
There are inspector parameters for engine power, braking force, and speed limits. The car is relatively stable and can usually recover after flipping.

### Enter / Exit vehicle
The system is set up so you don’t need to rebuild anything manually.  
Get in — you drive. Get out — you’re back on foot. The camera switches automatically as well.

### Minimap
A simple minimap in the corner of the screen. Helps you stay oriented whether you’re walking or driving.

### Missions & Navigation
Basic route system.  
The player walks or drives toward the next point. Glowing arrows appear in front and guide you along the path.  
When you reach a marker, the route switches to the next segment.

### Camera
Third-person camera with separate settings for on-foot and in-vehicle modes.

## How to play

1. Open the scene and press Play.
2. Walk around the level.
3. Approach the car and press **F**.
4. Drive around.
5. Press **F** again to exit.
6. Follow the arrows to the route markers.

## Main scripts

| File | Responsibility |
|------|----------------|
| `CarController.cs` | Vehicle physics and controls |
| `VehicleEnterExit.cs` | Entering and exiting the car |
| `UnifiedPlayerEntity.cs` | Player state (on foot / in vehicle) |
| `ThirdPersonCamera.cs` | Camera logic |
| `MinimapFollow.cs` | Minimap tracking |
| `RouteNavigationController.cs` | Navigation arrows |
| `PathDrawer.cs` | Bezier path drawing |
| `MissionManager.cs` | Mission and checkpoint logic |
| `RouteMarker.cs` | Route markers |

## Summary

This is a working prototype that already supports:
- walking and driving;
- switching between character and vehicle;
- minimap;
- route following with directional arrows.

It’s a solid base that can be extended with more mechanics and levels.
