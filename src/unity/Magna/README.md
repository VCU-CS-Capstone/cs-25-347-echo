# Magna - Unity Application for ABB Robot Control

## Warning
The organization and authors of this repository are not liable for any consequential damage or injury that any code or information available in this repository may produce to you or others. The code available in this repository should be used only for reading purposes as different robots and settings may act different during program execution. Use the code and information available here at your own risk, and always make sure you are following all the safety procedures recommended by your robot manufacturer. Robots can be dangerous if used inappropriately, be careful!

## Requirements
- Unity 2021.3.24f1
- An ABB robot controller running EGM to serve as your EGM client

## Setup Instructions
1. Clone this repository
2. Open Unity Hub and add this project
3. Open the project with Unity 2021.3.24f1
4. Ensure your firewall allows Unity to send/receive network traffic
5. If your ABB controller is not running EGM already, follow the [EGM setup tutorial](https://github.com/vcuse/egm-for-abb-robots/blob/main/EGM-Preparing-your-robot.pdf)

## Running the Application
1. Open the project in Unity
2. Load the "RobotControll" scene in the Scenes folder
3. Click the play button to run the program
4. You can attach compatible CSV animation files to the UpdatedMove script

## Network Configuration
- Default EGM port: 6510
- Default UDPCOMM port: 6511
- Default RaspberryPi port: 8000

## Key Scripts
- `Scripts/EgmCommunication.cs`: Contains the implementation for communicating with the ABB robot via EGM
- `Scripts/Egm.cs`: Contains the Abb.Egm library for formatting EGM messages
- `Scripts/UpdatedMove.cs`: Handles robot movement based on CSV animation files
- `Scripts/SendDataToRaspberryPi.cs`: Communication with Raspberry Pi

## Dependencies
This project uses NuGetForUnity to manage the following dependencies:
- Google.Protobuf (3.21.7)
- Google.Protobuf.Tools (3.21.7)

## Developer Notes
If you're creating your own Unity application using EGM:
1. Import Egm.cs to your project
2. Install Google.Protobuf and Google.Protobuf.Tools using NuGetForUnity
3. Allow Unity to communicate over the network in your firewall settings
4. Be aware that ABB updates EGM in almost every RobotWare version