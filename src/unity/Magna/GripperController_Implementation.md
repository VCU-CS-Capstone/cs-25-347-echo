# Gripper Controller Implementation for Unity

This document outlines the implementation of a C# script for controlling an OnRobot gripper (RG2/RG6) in Unity, based on the Python reference implementation.

## Overview

The implementation provides a Unity MonoBehaviour component that can:
- Connect to an OnRobot gripper via Modbus TCP
- Open and close the gripper
- Move the gripper to specific positions
- Check the gripper status
- Wait for operations to complete

## Library Setup

To use this implementation, you'll need to import the EasyModbusTCP library:

1. Download the EasyModbusTCP library from [EasyModbusTCP on GitHub](https://github.com/rossmann-engineering/EasyModbusTCP.NET) or via NuGet
2. Import the DLL into your Unity project:
   - Create a `Plugins` folder in your Assets directory if it doesn't exist
   - Place the EasyModbusTCP.dll in the Plugins folder

## GripperController Script

Create a new C# script named `GripperController.cs` with the following content:

```csharp
using UnityEngine;
using EasyModbus; // The Modbus TCP library
using System.Collections;

public class GripperController : MonoBehaviour
{
    [Header("Gripper Configuration")]
    [SerializeField] private string gripperType = "rg6"; // "rg2" or "rg6"
    [SerializeField] private string ipAddress = "192.168.1.1";
    [SerializeField] private int port = 502;
    [SerializeField] private int defaultForce = 400; // Default force in 1/10 Newtons
    
    // Internal variables
    private ModbusClient client;
    private int maxWidth;
    private int maxForce;
    private bool isInitialized = false;
    
    // Constants
    private const int GRIP_WITH_OFFSET_COMMAND = 16;
    private const int STATUS_REGISTER_ADDRESS = 268;
    private const int BUSY_FLAG_BIT = 0;
    
    /// <summary>
    /// Initialize the gripper parameters based on type
    /// </summary>
    private void Awake()
    {
        // Set max width and force based on gripper type
        if (gripperType.ToLower() == "rg2")
        {
            maxWidth = 1100; // 110mm in 1/10 mm
            maxForce = 400;  // 40N in 1/10 N
        }
        else if (gripperType.ToLower() == "rg6")
        {
            maxWidth = 1600; // 160mm in 1/10 mm
            maxForce = 1200; // 120N in 1/10 N
        }
        else
        {
            Debug.LogError("Invalid gripper type. Please specify either 'rg2' or 'rg6'.");
            return;
        }
        
        // Initialize the ModbusClient
        client = new ModbusClient(ipAddress, port);
        client.UnitIdentifier = 65; // Same as in Python code
        client.ConnectionTimeout = 1000; // 1 second timeout
        
        isInitialized = true;
        Debug.Log($"GripperController initialized with {gripperType} gripper");
    }
    
    /// <summary>
    /// Ensure connection is closed when the component is disabled
    /// </summary>
    private void OnDisable()
    {
        CloseConnection();
    }
    
    /// <summary>
    /// Opens the connection to the gripper
    /// </summary>
    /// <returns>True if connection was successful</returns>
    private bool OpenConnection()
    {
        if (!isInitialized)
        {
            Debug.LogError("GripperController not initialized properly");
            return false;
        }
        
        try
        {
            if (!client.Connected)
            {
                client.Connect();
                Debug.Log("Connected to gripper");
            }
            return client.Connected;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to connect to gripper: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Closes the connection to the gripper
    /// </summary>
    private void CloseConnection()
    {
        if (client != null && client.Connected)
        {
            client.Disconnect();
            Debug.Log("Disconnected from gripper");
        }
    }
    
    /// <summary>
    /// Checks if the gripper is currently busy (moving)
    /// </summary>
    /// <returns>True if the gripper is busy</returns>
    public bool IsGripperBusy()
    {
        if (!client.Connected && !OpenConnection())
        {
            return false;
        }
        
        try
        {
            // Read status register (address 268)
            int[] result = client.ReadHoldingRegisters(STATUS_REGISTER_ADDRESS, 1);
            
            // Check the busy flag (bit 0)
            bool isBusy = (result[0] & (1 << BUSY_FLAG_BIT)) != 0;
            
            if (isBusy)
            {
                Debug.Log("Gripper is busy");
            }
            
            return isBusy;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to check gripper status: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Opens the gripper fully
    /// </summary>
    /// <param name="force">Force to apply in 1/10 Newtons</param>
    /// <returns>True if the command was sent successfully</returns>
    public bool OpenGripper(int force = -1)
    {
        if (force < 0) force = defaultForce;
        
        if (!client.Connected && !OpenConnection())
        {
            return false;
        }
        
        try
        {
            // Limit force to max value
            force = Mathf.Min(force, maxForce);
            
            // Write to registers: [force, max_width, command]
            int[] parameters = new int[] { force, maxWidth, GRIP_WITH_OFFSET_COMMAND };
            client.WriteMultipleRegisters(0, parameters);
            
            Debug.Log("Started opening gripper");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to open gripper: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Closes the gripper fully
    /// </summary>
    /// <param name="force">Force to apply in 1/10 Newtons</param>
    /// <returns>True if the command was sent successfully</returns>
    public bool CloseGripper(int force = -1)
    {
        if (force < 0) force = defaultForce;
        
        if (!client.Connected && !OpenConnection())
        {
            return false;
        }
        
        try
        {
            // Limit force to max value
            force = Mathf.Min(force, maxForce);
            
            // Write to registers: [force, 0 (fully closed), command]
            int[] parameters = new int[] { force, 0, GRIP_WITH_OFFSET_COMMAND };
            client.WriteMultipleRegisters(0, parameters);
            
            Debug.Log("Started closing gripper");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to close gripper: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Moves the gripper to a specific width
    /// </summary>
    /// <param name="width">Target width in 1/10 millimeters</param>
    /// <param name="force">Force to apply in 1/10 Newtons</param>
    /// <returns>True if the command was sent successfully</returns>
    public bool MoveGripper(int width, int force = -1)
    {
        if (force < 0) force = defaultForce;
        
        if (!client.Connected && !OpenConnection())
        {
            return false;
        }
        
        try
        {
            // Limit width and force to max values
            width = Mathf.Clamp(width, 0, maxWidth);
            force = Mathf.Min(force, maxForce);
            
            // Write to registers: [force, width, command]
            int[] parameters = new int[] { force, width, GRIP_WITH_OFFSET_COMMAND };
            client.WriteMultipleRegisters(0, parameters);
            
            Debug.Log($"Started moving gripper to width {width/10.0f}mm");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to move gripper: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Coroutine that opens the gripper and waits for completion
    /// </summary>
    /// <param name="force">Force to apply in 1/10 Newtons</param>
    /// <returns>IEnumerator for coroutine</returns>
    public IEnumerator OpenGripperAndWait(int force = -1)
    {
        if (!OpenGripper(force))
        {
            yield break;
        }
        
        // Wait for the gripper to finish moving
        yield return new WaitForSeconds(0.5f); // Initial delay
        
        while (IsGripperBusy())
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.Log("Gripper fully opened");
    }
    
    /// <summary>
    /// Coroutine that closes the gripper and waits for completion
    /// </summary>
    /// <param name="force">Force to apply in 1/10 Newtons</param>
    /// <returns>IEnumerator for coroutine</returns>
    public IEnumerator CloseGripperAndWait(int force = -1)
    {
        if (!CloseGripper(force))
        {
            yield break;
        }
        
        // Wait for the gripper to finish moving
        yield return new WaitForSeconds(0.5f); // Initial delay
        
        while (IsGripperBusy())
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.Log("Gripper fully closed");
    }
    
    /// <summary>
    /// Coroutine that moves the gripper to a specific width and waits for completion
    /// </summary>
    /// <param name="width">Target width in 1/10 millimeters</param>
    /// <param name="force">Force to apply in 1/10 Newtons</param>
    /// <returns>IEnumerator for coroutine</returns>
    public IEnumerator MoveGripperAndWait(int width, int force = -1)
    {
        if (!MoveGripper(width, force))
        {
            yield break;
        }
        
        // Wait for the gripper to finish moving
        yield return new WaitForSeconds(0.5f); // Initial delay
        
        while (IsGripperBusy())
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.Log($"Gripper moved to width {width/10.0f}mm");
    }
}
```

## Usage Example

Here's an example of how to use the GripperController from another script:

```csharp
using UnityEngine;
using System.Collections;

public class GripperAnimation : MonoBehaviour
{
    [SerializeField] private GripperController gripperController;
    
    // Example method to demonstrate gripper usage
    public void DemonstrateGripper()
    {
        StartCoroutine(GripperDemonstration());
    }
    
    private IEnumerator GripperDemonstration()
    {
        // Open the gripper fully and wait for completion
        yield return StartCoroutine(gripperController.OpenGripperAndWait());
        
        // Wait for 1 second
        yield return new WaitForSeconds(1.0f);
        
        // Close the gripper fully and wait for completion
        yield return StartCoroutine(gripperController.CloseGripperAndWait());
        
        // Wait for 1 second
        yield return new WaitForSeconds(1.0f);
        
        // Move the gripper to middle position (800 = 80mm in 1/10mm)
        yield return StartCoroutine(gripperController.MoveGripperAndWait(800));
        
        Debug.Log("Gripper demonstration completed");
    }
}
```

## Implementation Notes

1. **Error Handling**: The script includes robust error handling to manage connection issues and command failures.

2. **Coroutines**: The implementation includes coroutines that wait for operations to complete, which is useful for animation sequencing.

3. **Configuration**: The script allows configuration through the Unity Inspector, making it easy to adjust settings without code changes.

4. **Documentation**: Each method is documented with XML comments for clarity.

5. **Cleanup**: The script ensures the connection is properly closed when the component is disabled.

6. **Units**: Note that width and force values are in 1/10 units (1/10 mm and 1/10 N) to match the gripper's internal representation.

## Setup in Unity

1. Create a new C# script named `GripperController.cs` and paste the code above
2. Attach the script to a GameObject in your scene
3. Configure the gripper settings in the Inspector:
   - Gripper Type: "rg2" or "rg6"
   - IP Address: The IP address of the gripper
   - Port: The port number (default: 502)
   - Default Force: The default force to use (in 1/10 Newtons)
4. Reference this GripperController from your animation scripts

## Comparison with Python Implementation

This C# implementation closely follows the Python reference implementation with these key differences:

1. Uses Unity's MonoBehaviour for lifecycle management
2. Adds coroutines for waiting for operations to complete
3. Provides more robust error handling
4. Includes Unity-specific logging and configuration