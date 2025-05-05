# ABB Robot Control with Unity using EGM

This README outlines the process for setting up a Unity project that can communicate with an ABB robot using Externally Guided Motion (EGM). This setup allows for real-time control of ABB robots from a Unity application.

## :warning: Warning

The organization and authors of this repository are not liable for any consequential damage or injury that any code or information available in this repository may produce to you or others. The code available in this repository should be used only for reading purposes as different robots and settings may act differently during program execution. Use the code and information available here at your own risk, and always make sure you are following all the safety procedures recommended by your robot manufacturer. Robots can be dangerous if used inappropriately, be careful!

## Prerequisites

- Unity **2021.3.24f1**
- ABB RobotStudio
- Nuitrack SDK (latest version)
- Visual Studio (for C# development)
- Access to an ABB robot controller (physical or virtual)

## 1. Installing Unity

1. Download Unity Hub from the [Unity website](https://unity.com/download)
2. Install Unity Hub and open it
3. Go to "Installs" tab and click "Add"
4. Select version **2021.3.24f1** (important for compatibility)
5. Make sure to include the following modules:
   - Microsoft Visual Studio Community
   - Universal Windows Platform Build Support
   - Windows Build Support (IL2CPP)

## 2. Setting up Nuitrack

Nuitrack is a middleware that provides skeleton tracking capabilities which can be used for gesture-based robot control.

1. Download the latest Nuitrack SDK from [GitHub](https://github.com/3DiVi/nuitrack-sdk)
2. Install the SDK by running the installer
3. Activate your Nuitrack license (trial or full)
4. Import the Nuitrack SDK into your Unity project:
   - In Unity, go to Assets → Import Package → Custom Package
   - Navigate to where you installed the Nuitrack SDK and select the Unity package
   - Import all assets

## 3. Configuring the ABB Robot Controller

For the robot to receive external commands, you need to configure the robot controller to enable EGM functionality.

### Step 1: Enabling EGM on your controller

1. Open RobotStudio and connect to your robot controller
2. Right-click on your robot controller in the Controller tab
3. Click on "Change Options..."
4. Find "Engineering Tools" in the system options menu
5. Select the "EGM" checkbox
6. Click OK to confirm the change
7. Restart the controller

### Step 2: Setting up UDP Unicast Device

1. In RobotStudio, click on the "Configuration" dropdown menu on the Controller tab
2. Select "Communication"
3. With the Communication window opened, select "UDP Unicast Device"
4. Right-click on a blank space in the UDP Unicast Device window
5. Select "New UDP Unicast Device..."
6. Fill in the details:
   - Name: Enter a name for your device (e.g., "UnityApp")
   - Type: UDPUC
   - Remote Address: IP address of the computer running Unity
   - Remote Port Number: Port that Unity will use (e.g., 6510)
   - Local Port Number: Port on the robot controller (e.g., 6510)
7. Click OK and restart the controller

### Step 3: Setting up RAPID code

Create a new RAPID module on your robot controller with the following code:

Refer to the file `src/abb/EGMCommunication.mod` for the required RAPID code.

## 4. Project Architecture

This application controls an ABB robot using Externally Guided Motion (EGM) based on task sequences defined within Unity and potentially influenced by real-time skeleton tracking data.

- **Task Initiation**: Robot tasks are defined using `TaskSequenceSO` Scriptable Objects. These sequences are initiated and managed through the `TaskProgrammer` component, configured within the Unity Editor inspector.
- **EGM Communication**: The `UDPCOMM.cs` script is the primary component responsible for handling the low-level UDP communication with the ABB robot controller via the EGM protocol. It sends target poses or joint values to the robot based on the active task or manual input.
- **Manual Control**: The `ObjectMovement.cs` script provides a way to manually control the robot's target position in the Unity scene using keyboard inputs (WASDQE). This target can then be sent to the robot via the EGM communication layer.
- **Skeleton Tracking & Path Rerouting**: Nuitrack SDK is integrated for real-time human skeleton tracking. This tracking data is intended to be used for dynamically rerouting the robot's path based on the user's position or gestures, adding a layer of human-robot interaction.

## 5. How to run this application

Import this project folder using Unity Hub and open the project using Unity 2021.3.24f1. Click on the play button available on the top of the Unity interface to run the program. Attach compatible CSV Files to move script.

If your virtual controller is not running EGM already, please refer to our [tutorial](https://github.com/vcuse/egm-for-abb-robots/blob/main/EGM-Preparing-your-robot.pdf) on how to setup your ABB robot for EGM communication.

## 6. What files in this project are related to EGM and Core Logic?

If you are here just to check how we implemented EGM code and core logic that runs in Unity, the [Scripts](Assets/Scripts) folder is what you need. Inside of it you will find:

- [EgmCommunication.cs](Assets/Scripts/EgmCommunication.cs): Contains base implementation details for EGM message handling, potentially used by `UDPCOMM.cs`. It might handle receiving status messages from the robot. (Note: Based on user feedback, `UDPCOMM.cs` is primary, so this script's exact role might need further clarification within its own documentation).
- [Egm.cs](Assets/Scripts/Egm.cs): Contains the Abb.Egm library (protobuf definitions) used to structure messages in the EGM format. This file is typically generated from ABB specifications.
- [UDPCOMM.cs](Assets/Scripts/UDPCOMM.cs): The primary script responsible for implementing the UDP communication link with the ABB robot using the EGM protocol. It handles sending target poses (from `TaskProgrammer` or `ObjectMovement.cs`) and receiving robot status. _Author: Miles Popiela_ - Credit for developing this UDP communication implementation.
- [TaskProgrammer.cs](Assets/Scripts/TaskProgrammer.cs): Manages the execution of predefined robot task sequences defined in `TaskSequenceSO` assets, initiated via the Unity Editor.
- [TaskSequenceSO.cs](Assets/Scripts/TaskSequenceSO.cs): A ScriptableObject definition used to create assets that hold sequences of robot poses or actions.
- [ObjectMovement.cs](Assets/Scripts/ObjectMovement.cs): Allows for manual control of the target robot pose within the Unity scene using keyboard inputs (WASDQE), providing an alternative input method to predefined task sequences.
- [GripperController.cs](Assets/Scripts/GripperController.cs): Manages communication with an OnRobot RG2/RG6 gripper via Modbus TCP, allowing for opening, closing, and setting specific widths.
- [GripperAnimation.cs](Assets/Scripts/GripperAnimation.cs): Provides example usage and keyboard controls for the `GripperController`, demonstrating how to trigger gripper actions.
- [QuarterSphereConstraint.cs](Assets/Scripts/QuarterSphereConstraint.cs): Constrains the movement of a target GameObject within a defined quarter-ellipsoid boundary, useful for defining reachable workspace limits.
- Nuitrack-related scripts (within `Assets/NuitrackSDK/`): Handle skeleton tracking input for dynamic path adjustments based on user movement.

## 7. Notes from the author

If you plan to create your own Unity application, don't forget to import Egm.cs to your project and install Google.Protobuf and Google.Protobuf.Tools **in your Unity project** (this is a requirement for Egm.cs). Don't use the NuGet Manager of Visual Studio for this type of application as it will not install it correctly inside the Unity project. I recommend [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) as the alternative to install both libraries.

Please don't forget to allow Unity to receive and submit messages over the network in your firewall. Keep in mind that ABB updates EGM in almost every RobotWare version, so please use this project as a reference but be aware that newer implementations might differ from it (for better).

## Safety Considerations

**IMPORTANT:** When working with robots, safety is paramount. Always observe these safety guidelines:

- Always use a virtual controller on a secondary machine if you plan to test any code
- Using a real robot may lead to unexpected behaviors, which can be dangerous
- Get familiar with the concept first before trying anything on a real robot
- Always follow all safety procedures recommended by your robot manufacturer
- Ensure emergency stop buttons are accessible when testing with real hardware
- Never enter the robot's working area while it is powered on without proper safety measures

## Troubleshooting

- **No connection to robot**: Check firewall settings and ensure UDP communication is allowed
- **Robot not responding to commands**: Verify the RAPID code is running and EGM is properly configured
- **Error messages in RobotStudio**: Check the EGM application manual for specific error codes

## Additional Resources

- [ABB EGM Application Manual](https://library.e.abb.com/public/f05090fae99a4d0ba2ee332e50865791/3HAC073318%20AM%20Externally%20Guided%20Motion%20RW7-en.pdf)
- [GitHub Repository with Examples](https://github.com/vcuse/egm-for-abb-robots/)
- [Nuitrack SDK Documentation](https://github.com/3DiVi/nuitrack-sdk)

## Disclaimer

This document is not supported, sponsored, or approved by ABB. Always refer to the official EGM Application Manual for consistent information.
