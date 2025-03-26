# Avatar Setup for Nuitrack Skeleton

This README explains how to use the updated `AvatarSetupManager` and `AvatarSetupExample` scripts to wrap a 3D model onto the Nuitrack skeleton.

## Overview

The `AvatarSetupManager` script helps set up a 3D model to follow the skeleton tracking data from Nuitrack. Due to access restrictions in the NuitrackAvatar component, the workflow has been updated to:

1. Create the necessary GameObject hierarchy
2. Add the NuitrackAvatar component
3. Find and identify bones in your 3D model
4. Guide you through manually assigning bones in the Unity Inspector

## How to Use

### 1. Prepare Your 3D Model

Your 3D model should:
- Be in a T-pose (arms extended horizontally)
- Have a proper rigging/skeleton with named bones
- Be imported into your Unity project as a prefab

### 2. Create a Test Scene

The easiest way to test the avatar setup is to create a new scene:

1. Create a new scene in Unity
2. Add a GameObject named "AvatarSetupTester"
3. Add the `AvatarSetupExample` component to this GameObject
4. Drag your 3D model prefab into the "Model Prefab" field in the Inspector

### 3. Run the Scene

1. Make sure a Nuitrack-compatible sensor is connected
2. Enter Play mode in Unity
3. The script will:
   - Initialize Nuitrack if it's not already initialized
   - Create an AvatarRoot GameObject with the NuitrackAvatar component
   - Instantiate your 3D model as a child
   - Find and identify bones in your model
   - Print instructions in the console for manual bone assignment

### 4. Manual Configuration

While the scene is running:

1. Select the "AvatarRoot" GameObject in the Hierarchy
2. In the Inspector, find the NuitrackAvatar component
3. Configure the tracking settings:
   - Set "Smooth Move" to 0.5 (or your preferred value)
   - Enable "Alignment Bone Length" if you want the model to match user proportions
   - Enable "Align Straight Legs" to prevent leg deformation
   - Set "Hide Distance" to 1.5 (or your preferred value)
4. Assign the bones:
   - For each bone field (waist, torso, head, etc.), drag the corresponding bone from your model's hierarchy
   - The console will show a list of bones found in your model to help you identify them
5. Once all settings and bones are configured, the avatar will follow the Nuitrack skeleton when detected

## Troubleshooting

### Model doesn't move:
- Check if all required bones are assigned in the Inspector
- Ensure Nuitrack is properly initialized and detecting skeletons
- Check the console for any error messages

### Movements are jittery:
- Increase the "Smoothing Factor" (closer to 1) in the AvatarSetupManager
- Check if the sensor has a clear view of you

### Limbs bend unnaturally:
- Enable the "Align Straight Legs" option in the AvatarSetupManager
- Check if bone mappings are correct

### Model appears in the wrong position:
- Adjust the position of the AvatarRoot GameObject
- Check if the "Root Joint" is properly mapped

### Model proportions look wrong during movement:
- Enable "Adjust Bone Length" in the AvatarSetupManager to match the model's proportions to the user

## Advanced Usage

### Multiple Users

To track multiple users, you can create multiple instances of the AvatarSetupManager and assign different user IDs to each:

```csharp
// Find the NuitrackAvatar component
NuitrackAvatar avatar = FindObjectOfType<NuitrackAvatar>();
// Set the user ID for tracking
avatar.ControllerUserId = userId;
```

### Using in an Existing Scene

To use the avatar setup in an existing scene (like RobotControll.unity):

1. Add the AvatarSetupExample component to a GameObject in your scene
2. Assign your 3D model prefab to the "Model Prefab" field
3. Run the scene and follow the manual bone assignment steps
4. Once configured, the avatar will appear and follow the Nuitrack skeleton

### UI Integration

The AvatarSetupExample script includes optional UI integration:

- Status Text: Displays tracking status
- Create Avatar Button: Manually triggers avatar creation

To use these features, create UI elements and assign them in the Inspector.
