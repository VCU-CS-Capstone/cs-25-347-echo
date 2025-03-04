# OnRobot Gripper Control for Unity

This package provides C# scripts for controlling OnRobot RG2/RG6 grippers in Unity. It allows you to open, close, and move the gripper to specific positions.

## Overview

The implementation consists of two main scripts:

1. **GripperController.cs**: Core functionality for communicating with the gripper via Modbus TCP
2. **GripperAnimation.cs**: Example script demonstrating how to use the GripperController

## Dependencies

This implementation requires the EasyModbusTCP library:

1. Download the EasyModbusTCP library:
   - GitHub: [https://github.com/rossmann-engineering/EasyModbusTCP.NET](https://github.com/rossmann-engineering/EasyModbusTCP.NET)
   - NuGet: `EasyModbusTCP.NET`

2. Import the library into your Unity project:
   - Create a `Plugins` folder in your Assets directory if it doesn't exist
   - Place the EasyModbusTCP.dll in the Plugins folder

## Setup Instructions

### 1. Import the Scripts

Make sure the following scripts are in your project:
- `Assets/Scripts/GripperController.cs`
- `Assets/Scripts/GripperAnimation.cs`

### 2. Set Up the Gripper Controller

1. Create an empty GameObject in your scene (e.g., "GripperController")
2. Add the GripperController component to the GameObject
3. Configure the gripper settings in the Inspector:
   - **Gripper Type**: "rg2" or "rg6"
   - **IP Address**: The IP address of the gripper (default: 192.168.1.1)
   - **Port**: The port number (default: 502)
   - **Default Force**: The default force to use (in 1/10 Newtons)

### 3. Set Up the Animation Script (Optional)

1. Create another GameObject or use the same one
2. Add the GripperAnimation component to the GameObject
3. Drag the GameObject with the GripperController component into the "Gripper Controller" field

## Usage Examples

### Basic Usage

```csharp
// Reference to the GripperController
public GripperController gripperController;

// Open the gripper
gripperController.OpenGripper();

// Close the gripper
gripperController.CloseGripper();

// Move the gripper to a specific width (in 1/10 mm)
gripperController.MoveGripper(800); // 80mm
```

### Using Coroutines to Wait for Completion

```csharp
// Open the gripper and wait for completion
yield return StartCoroutine(gripperController.OpenGripperAndWait());

// Close the gripper and wait for completion
yield return StartCoroutine(gripperController.CloseGripperAndWait());

// Move the gripper to a specific width and wait for completion
yield return StartCoroutine(gripperController.MoveGripperAndWait(800)); // 80mm
```

### Example Animation Sequence

```csharp
private IEnumerator AnimateGripper()
{
    // Open the gripper
    yield return StartCoroutine(gripperController.OpenGripperAndWait());
    
    // Wait for 1 second
    yield return new WaitForSeconds(1.0f);
    
    // Close the gripper
    yield return StartCoroutine(gripperController.CloseGripperAndWait());
    
    // Wait for 1 second
    yield return new WaitForSeconds(1.0f);
    
    // Move to middle position
    yield return StartCoroutine(gripperController.MoveGripperAndWait(800));
}
```

## Troubleshooting

### Connection Issues

If you're having trouble connecting to the gripper:

1. Verify the IP address and port are correct
2. Check that the gripper is powered on and connected to the network
3. Ensure there are no firewalls blocking the connection
4. Try pinging the gripper from your computer to verify network connectivity

### Unity Console Errors

- **ModbusClient not found**: Make sure the EasyModbusTCP.dll is properly imported into your project
- **Connection timeout**: Check network connectivity and gripper power
- **Invalid gripper type**: Make sure to specify either "rg2" or "rg6" in the Inspector

## Notes

- Width and force values are in 1/10 units (1/10 mm and 1/10 N) to match the gripper's internal representation
- The gripper will not accept new commands while it's busy (moving)
- The connection is automatically closed when the GripperController component is disabled

## Advanced Usage

For more advanced usage, you can modify the GripperController.cs script to add additional functionality:

- Reading the current gripper width
- Setting fingertip offsets
- Implementing custom error handling
- Adding support for additional gripper models

Refer to the OnRobot documentation for more details on the Modbus TCP interface.