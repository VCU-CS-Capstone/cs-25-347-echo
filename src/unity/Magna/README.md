# ABB Robot Control with Unity using EGM

This README outlines the process for setting up a Unity project that can communicate with an ABB robot using Externally Guided Motion (EGM). This setup allows for real-time control of ABB robots from a Unity application.

### :warning: Warning

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

```
MODULE EGMCommunication
 ! Identifier for EGM process
 VAR egmident egm_id;
 ! Current state of EGM process on controller
 VAR egmstate egm_state;
 ! Convergence criteria for translation and rotation (in degrees)
 CONST egm_minmax egm_minmax_translation := [-1, 1];
 CONST egm_minmax egm_minmax_rotation := [-1, 1];
 
 ! Correction frame for path correction
 LOCAL CONST pose egm_correction_frame := [[0, 0, 0], [1, 0, 0, 0]];
 LOCAL CONST pose egm_sensor_frame := [[0, 0, 0], [1, 0, 0, 0]];
 
 ! Main function
 PROC main()
 EGM_POSE_MOVEMENT;
 ENDPROC
 
 ! EGM function used to move the robot to a specific position using a pose target.
 PROC EGM_POSE_MOVEMENT()
 ! Check if no EGM setup is active.
 IF egm_state = EGM_STATE_DISCONNECTED THEN
 TPWrite "EGM State: Preparing controller for EGM communication.";
 ENDIF
 
 WHILE TRUE DO 
 ! Register a new EGM id.
 EGMGetId egm_id;
 ! Get current state of egm_id.
 egm_state := EGMGetState(egm_id)
 ! Setup the EGM communication.
 ! Make sure the external device name being used is the same specified in 
 ! the controller communication tab.
 IF egm_state <= EGM_STATE_CONNECTED THEN
 EGMSetupUC ROB_1, egm_id, "default", "UnityApp", \Pose; 
 ENDIF
 
 ! De-serializes the message sent by the external device.
 EGMActPose egm_id\Tool:=tool0, 
 egm_correction_frame,
 EGM_FRAME_BASE,
 egm_sensor_frame,
 EGM_FRAME_BASE
 \x:=egm_minmax_translation
 \y:=egm_minmax_translation
 \z:=egm_minmax_translation
 \rx:=egm_minmax_rotation
 \ry:=egm_minmax_rotation
 \rz:=egm_minmax_rotation
 \LpFilter:= 16
 \MaxSpeedDeviation:=100; 
 
 ! Performs a movement based on the pose target sent by the external device.
 EGMRunPose egm_id, EGM_STOP_HOLD \x \y \z \rx \ry \rz \CondTime:=1\RampInTime:=0;
 
 ! (Debugging) Checks if robot is listening for external commands.
 IF egm_state = EGM_STATE_CONNECTED THEN
 TPWrite "EGM State: Waiting for movement request.";
 ENDIF
 
 ! (Debugging) Checks if the robot received an external command and is moving.
 IF egm_state = EGM_STATE_RUNNING THEN
 TPWrite "EGM State: Movement request received. Robot is moving.";
 ENDIF
 
 ! Reset EGM communication.
 IF egm_state <= EGM_STATE_CONNECTED THEN
 EGMReset egm_id;
 ENDIF
 ENDWHILE
 
 ! (Debugging) Checks if external devices are available.
 ERROR
 IF ERRNO = ERR_UDPUC_COMM THEN
 TPWrite "EGM Warning: Robot is not detecting any external devices.";
 TRYNEXT;
 ENDIF
 ENDPROC
ENDMODULE
```

**Important**: Replace "UnityApp" in `EGMSetupUC ROB_1, egm_id, "default", "UnityApp", \Pose;` with the name you gave to your UDP Unicast Device.

## 4. How to run this application

Import this project folder using Unity Hub and open the project using Unity 2021.3.24f1. Click on the play button available on the top of the Unity interface to run the program. Attach compatible CSV Files to move script.

If your virtual controller is not running EGM already, please refer to our [tutorial](https://github.com/vcuse/egm-for-abb-robots/blob/main/EGM-Preparing-your-robot.pdf) on how to setup your ABB robot for EGM communication.

## 5. What files in this project are related to EGM?

If you are here just to check how we implemented EGM code that runs in Unity, the [Scripts](https://github.com/vcuse/egm-for-abb-robots/tree/main/Unity-Example/Assets/Scripts) folder is what you need. Inside of it you will find:

- [EgmCommunication.cs](https://github.com/vcuse/egm-for-abb-robots/blob/main/Unity-Example/Assets/Scripts/EgmCommunication.cs) This file contains all the implementation used to receive messages from the ABB robot and to submit new joint values to it. Notice that in order to make it work in Unity, we attach this file to an empty object in our _SampleScene_ called _EgmCommunicator_, and fill the necessary scene components in the inspector of this empty object.
- [Egm.cs](https://github.com/vcuse/egm-for-abb-robots/blob/main/WPF-Example/Egm.cs) This file contains the Abb.Egm library used in [EgmCommunication.cs](https://github.com/vcuse/egm-for-abb-robots/blob/main/Unity-Example/Assets/Scripts/EgmCommunication.cs) to write messages in EGM format. Notice that this file is generated automatically. To create your own version of this file, refer to the EGM manual provided by ABB (Section 3.2 - Building an EGM sensor communication endpoint) or follow our [tutorial](https://github.com/vcuse/egm-for-abb-robots/blob/main/EGM-Preparing-your-robot.pdf).
- [UDPCOMM.cs](Assets/Scripts/UDPCOMM.cs) This file implements UDP communication with ABB robots using the EGM protocol. It handles sending and receiving messages to control robot position and orientation in cartesian coordinates. The class manages robot state tracking, displays position information, and provides methods for creating and sending EGM-formatted messages. _Author: Miles Popiela_ - Credit for developing this UDP communication implementation for ABB robot control.

## 6. Notes from the author

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
