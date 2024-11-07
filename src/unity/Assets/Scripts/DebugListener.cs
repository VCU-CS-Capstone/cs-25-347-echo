using UnityEngine;
using System.IO.Ports;
using System;

public class DebugLogListener : MonoBehaviour
{
    SerialPort sp;

    void Start()
    {
        try
        {
            // Initialize Serial Port
            sp = new SerialPort("COM5", 9600); // Adjust the COM port and baud rate as necessary
            sp.Open(); // Attempt to open the serial port
            Application.logMessageReceived += HandleLog;
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.LogError("Access Denied when trying to open the serial port: " + ex.Message);
        }
        catch (System.IO.IOException ex)
        {
            Debug.LogError("IO Exception - Check if the COM port is correct and not in use: " + ex.Message);
        }
        catch (Exception ex)
        {
            Debug.LogError("General exception occurred when opening serial port: " + ex.Message);
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Check for a specific message in the log
        if (logString.Contains("red_LED") || logString.Contains("yellow_LED") || logString.Contains("green_LED"))
        {
            ClearSerialBuffers(); // Reset serial buffer to ensure only the latest command is processed

            // Send the appropriate command based on the log message
            if (logString.Contains("red_LED"))
            {
                sp.WriteLine("red_stop"); // Command for red LED
            }
            else if (logString.Contains("yellow_LED"))
            {
                sp.WriteLine("yellow_slow"); // Command for yellow LED
            }
            else if (logString.Contains("green_LED"))
            {
                sp.WriteLine("green_normal"); // Command for green LED
            }
        }
    }

    void ClearSerialBuffers()
    {
        if (sp != null && sp.IsOpen)
        {
            sp.DiscardOutBuffer();
            sp.DiscardInBuffer();
        }
    }

    void OnDisable()
    {
        // Unsubscribe from the log message event when the script is disabled/destroyed
        Application.logMessageReceived -= HandleLog;

        // Close the serial port
        if (sp != null && sp.IsOpen)
        {
            sp.Close();
        }
    }
}
