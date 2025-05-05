using UnityEngine;
using System.Collections;

// Note: You'll need to import the EasyModbus library
// Add the EasyModbusTCP.dll to your Assets/Plugins folder
// If using Unity Package Manager, install it via NuGet

/// <summary>
/// Controls an OnRobot RG2 or RG6 gripper using Modbus TCP communication via the EasyModbus library.
/// Provides methods for opening, closing, setting width, and checking status.
/// </summary>
/// <remarks>
/// Requires the EasyModbusTCP library (DLL) to be present in the project (e.g., in Assets/Plugins).
/// Ensure the gripper's IP address and Modbus port (usually 502) are correctly configured.
/// </remarks>
public class GripperController : MonoBehaviour
{
    [Header("Gripper Configuration")]
    [Tooltip("Type of the OnRobot gripper ('rg2' or 'rg6'). Determines max width/force.")]
    [SerializeField] private string gripperType = "rg6"; // "rg2" or "rg6"
    [Tooltip("IP address of the gripper's Modbus TCP server.")]
    [SerializeField] private string ipAddress = "192.168.1.1";
    [Tooltip("Port number for Modbus TCP communication (default is 502).")]
    [SerializeField] private int port = 502;
    [Tooltip("Default force applied during gripper actions if not specified (in 1/10 Newtons).")]
    [SerializeField] private int defaultForce = 400; // Default force in 1/10 Newtons

    // Internal variables
    private EasyModbus.ModbusClient client;
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
        client = new EasyModbus.ModbusClient(ipAddress, port);
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
    /// Checks if the gripper is currently busy executing a command (moving).
    /// Reads the status register via Modbus and checks the busy flag.
    /// </summary>
    /// <returns>True if the gripper is busy, false otherwise or if communication fails.</returns>
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
    /// <param name="force">Force to apply during the open action (in 1/10 Newtons). Uses defaultForce if negative.</param>
    /// <returns>True if the command was successfully sent via Modbus, false otherwise.</returns>
    public bool OpenGripper(int force = -1)
    {
        if (force < 0) force = defaultForce; // Use default if not specified

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
    /// <param name="force">Force to apply during the close action (in 1/10 Newtons). Uses defaultForce if negative.</param>
    /// <returns>True if the command was successfully sent via Modbus, false otherwise.</returns>
    public bool CloseGripper(int force = -1)
    {
        if (force < 0) force = defaultForce; // Use default if not specified

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
    /// <param name="width">Target width for the gripper jaws (in 1/10 millimeters). Clamped between 0 and max width.</param>
    /// <param name="force">Force to apply during the move action (in 1/10 Newtons). Uses defaultForce if negative.</param>
    /// <returns>True if the command was successfully sent via Modbus, false otherwise.</returns>
    public bool MoveGripper(int width, int force = -1)
    {
        if (force < 0) force = defaultForce; // Use default if not specified

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

            Debug.Log($"Started moving gripper to width {width / 10.0f}mm");
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
    /// <param name="force">Force to apply during the open action (in 1/10 Newtons). Uses defaultForce if negative.</param>
    /// <returns>IEnumerator for use in StartCoroutine.</returns>
    public IEnumerator OpenGripperAndWait(int force = -1)
    {
        if (!OpenGripper(force)) // Attempt to send the command
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
    /// <param name="force">Force to apply during the close action (in 1/10 Newtons). Uses defaultForce if negative.</param>
    /// <returns>IEnumerator for use in StartCoroutine.</returns>
    public IEnumerator CloseGripperAndWait(int force = -1)
    {
        if (!CloseGripper(force)) // Attempt to send the command
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
    /// <param name="width">Target width for the gripper jaws (in 1/10 millimeters).</param>
    /// <param name="force">Force to apply during the move action (in 1/10 Newtons). Uses defaultForce if negative.</param>
    /// <returns>IEnumerator for use in StartCoroutine.</returns>
    public IEnumerator MoveGripperAndWait(int width, int force = -1)
    {
        if (!MoveGripper(width, force)) // Attempt to send the command
        {
            yield break;
        }

        // Wait for the gripper to finish moving
        yield return new WaitForSeconds(0.5f); // Initial delay

        while (IsGripperBusy())
        {
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log($"Gripper moved to width {width / 10.0f}mm");
    }
}