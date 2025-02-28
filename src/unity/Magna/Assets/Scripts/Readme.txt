Author @ Miles Popiela

This repository contains a collection of scripts and modules related to robot communication and control. Below is a brief description of each:

EGM - Main Communication API

This script serves as the main interface for robot communication. It defines the core API that facilitates various communication tasks related to robot control.
EgmCommunication - Joint Value Communication Script

This script is focused on communicating joint values between the robot and the controlling interface.
Note: This script is currently unfinished and may not be fully functional.
SendDataToRaspberryPi - Location Data Communication

This script is responsible for sending location data directly to a Raspberry Pi setup.
The Raspberry Pi uses this data to update LED indications, showcasing robot location or state.
This script is part of a separate project that deals specifically with Raspberry Pi LED indications based on the robot's position or state.
UDPCOMM - Robot TCP Location Communication

The core function of this script is to communicate the location of the Tool Center Point (TCP) of the robot.
It ensures that the robot's TCP is accurately communicated and tracked.
UpdatedMove - Robot Animation Playback via CSV

This module reads from a CSV file and plays back pre-recorded robot movements or animations.
It serves to reproduce specific robot maneuvers or demonstrations using data stored in CSV format.