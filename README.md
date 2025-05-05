# ECHO - Enhanced Collaboration for Human-Robot Operations

## VCU College of Engineering

## Project Description

This project will refine the sensing and control systems of industrial robots, enabling real-time adjustments to movement, position, and speed based on the human user’s proximity, orientation, and actions. This system will rely on real-time location data, velocity, and trajectory tracking to enable robots to dynamically respond to human presence, more effectively collaborate, and enhance both safety and efficiency. The solution will integrate a turn-based interaction protocol, allowing robots and humans to collaborate on tasks like constructing a tower of alternating red and blue blocks. Vision systems will classify and locate blocks in real-time, while task-planning algorithms optimize the sequence of actions, minimizing unnecessary movements. To ensure smooth collaboration, visual and auditory cues, as well as proximity sensors, will coordinate turns and maintain safe working zones. Additionally, the system will feature an emergency stop mechanism and a user-friendly interface, ensuring clear communication and real-time feedback between the robot and human operator.

| Folder               | Description                                                                                                                         |
| -------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| Documentation        | all documentation the project team has created to describe the architecture, design, installation, and configuration of the project |
| Notes and Research   | Relevant helpful information to understand the tools and techniques used in the project                                             |
| Project Deliverables | Folder that contains final pdf versions of all Fall and Spring Major Deliverables                                                   |
| Status Reports       | Project management documentation - weekly reports, milestones, etc.                                                                 |
| scr                  | Source code - create as many subdirectories as needed                                                                               |

## Project Team

- *Shawn Brixey* - *VCU College of Engineering* - Faculty Advisor
- *Tamer Nadeem* - *VCU College of Engineering* - Faculty Advisor
- *Miles Popiela* - *VCU College of Engineering* - Mentor
- *Ian Richards* - *Computer Science* - Student Team Member
- *Ekta Shethna* - *Computer Science* - Student Team Member
- *Gianni Bautista* - *Computer Science* - Student Team Member
- *Samuel Sarzaba* - *Computer Science* - Student Team Member


## 🛠️ Setup Instructions

### 1. RobotStudio Setup

1. **Connect to the Magnaforma Wi-Fi Router**  
   Ensure your computer is connected to the **Magnaforma** Wi-Fi router. This connects your PC, the robot, and the gripper together.

2. **Identify IP Addresses**  
   Use **Wireshark** or a similar network utility to find the IP addresses of:  
   - Your machine (PC)  
   - The ABB robot  
   - The gripper  

3. **Connect to Controller**  
   Open **RobotStudio**, then connect to the controller.

4. **Create a UDP Unicast Device**  
   In **RobotStudio Configuration**:
   - Create a **new UDP unicast device**
   - Set your machine’s IP address
   - Assign a unique device name (e.g., `MyPC_UDP`)

5. **Reference Device in RAPID Code**  
   Ensure the **RAPID** code references your custom device name.  
   _⚠️ If the RAPID code does **not** reference your device, the communication will fail._

6. **Close RobotStudio**  
   After verification, close RobotStudio.

📚 Reference: [EGM for ABB Robots](https://github.com/lsurobotics/egm-for-abb-robots)

---

### 2. Unity Setup

1. **Open Unity Project**  
   Launch the Unity project included in this repo.

2. **Check Robot Mode**  
   The robot must be in **Auto mode** before running the Unity application.

---

### 3. Nuitrack Human Detection

1. **Activate Nuitrack**  
   - The SDK should already be embedded in the Unity project.
   - Launch the **Nuitrack** application.
   - Enter the license key (you may need to purchase a new one).

2. **Run the Project**  
   Once activated, run the Unity project. Human detection should now work as expected.

---

## ⚠️ Troubleshooting

### Common Issues

- **Network Issues**  
  Ensure all devices (PC, robot, and gripper) are connected to the **Magnaforma router** (usually on the table next to the robot).

- **Incorrect IP Addresses in Unity**  
  Some Unity scripts may contain hardcoded IPs. **Update them to match your current setup**.

- **Robot Lock-Up**  
  If the robot locks:
  - Stop the Unity program
  - **Reset the robot manually** using the teach pendant or by jogging it

### General Advice

- Errors in Unity will appear in the console. **Copy and search** them online or ask AI tools for help.
- This project has many moving parts, so issues can arise at multiple layers. Stay patient and methodical in debugging.

---

## 🧠 Final Notes

We've documented the most common problems, but edge cases may still occur. Don’t hesitate to explore solutions and modify code as needed.

Good luck! 🍀
