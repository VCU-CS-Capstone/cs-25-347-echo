# Using Unity to define joints position in ABB robots

### :warning: Warning

The organization and authors of this repository are not liable for any consequential damage or injury that any code or information available in this repository may produce to you or others. The code available in this repository should be used only for reading purposes as different robots and settings may act differently during program execution. Use the code and information available here at your own risk, and always make sure you are following all the safety procedures recommended by your robot manufacturer. Robots can be dangerous if used inappropriately, be careful!

## Requirements

In order to run this project, you need:

- Unity (Version 2021.3.24f1)
- An ABB robot controller running EGM to serve as your EGM client

## How to run this application?

Import this project folder using Unity Hub and open the project using Unity 2021.3.24f1. Click on the play button available on the top of the Unity interface to run the program. Attach compatible CSV Files to move script.

If your virtual controller is not running EGM already, please refer to our [tutorial](https://github.com/vcuse/egm-for-abb-robots/blob/main/EGM-Preparing-your-robot.pdf) on how to setup your ABB robot for EGM communication.

## What files in this project are related to EGM?

If you are here just to check how we implemented EGM code that runs in Unity, the [Scripts](https://github.com/vcuse/egm-for-abb-robots/tree/main/Unity-Example/Assets/Scripts) folder is what you need. Inside of it you will find:

- [EgmCommunication.cs](https://github.com/vcuse/egm-for-abb-robots/blob/main/Unity-Example/Assets/Scripts/EgmCommunication.cs) This file contains all the implementation used to receive messages from the ABB robot and to submit new joint values to it. Notice that in order to make it work in Unity, we attach this file to an empty object in our _SampleScene_ called _EgmCommunicator_, and fill the necessary scene components in the inspector of this empty object.
- [Egm.cs](https://github.com/vcuse/egm-for-abb-robots/blob/main/WPF-Example/Egm.cs) This file contains the Abb.Egm library used in [EgmCommunication.cs](https://github.com/vcuse/egm-for-abb-robots/blob/main/Unity-Example/Assets/Scripts/EgmCommunication.cs) to write messages in EGM format. Notice that this file is generated automatically. To create your own version of this file, refer to the EGM manual provided by ABB (Section 3.2 - Building an EGM sensor communication endpoint) or follow our [tutorial](https://github.com/vcuse/egm-for-abb-robots/blob/main/EGM-Preparing-your-robot.pdf).
- [UDPCOMM.cs](Assets/Scripts/UDPCOMM.cs) This file implements UDP communication with ABB robots using the EGM protocol. It handles sending and receiving messages to control robot position and orientation in cartesian coordinates. The class manages robot state tracking, displays position information, and provides methods for creating and sending EGM-formatted messages. _Author: Miles Popiela_ - Credit for developing this UDP communication implementation for ABB robot control.

## Notes from the author

If you plan to create your own Unity application, don't forget to import Egm.cs to your project and install Google.Protobuf and Google.Protobuf.Tools **in your Unity project** (this is a requirement for Egm.cs). Don't use the NuGet Manager of Visual Studio for this type of application as it will not install it correctly inside the Unity project. I recommend [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) as the alternative to install both libraries.

Please don't forget to allow Unity to receive and submit messages over the network in your firewall. Keep in mind that ABB updates EGM in almost every RobotWare version, so please use this project as a reference but be aware that newer implementations might differ from it (for better).
