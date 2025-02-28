using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class SendDataToRaspberryPi : MonoBehaviour
{
    // IP address of the Raspberry Pi - can be configured in the Unity Inspector
    public string raspberryPiIpAddress = "192.168.0.50";

    // Port number that the Raspberry Pi is listening on
    public int portNumber = 8000;

    // The socket that will be used to send data to the Raspberry Pi
    private Socket socket;

    // How often to send data (in frames)
    public int sendFrequency = 50;
    private int frameCounter = 0;

    void Start()
    {
        // Create a socket and connect to the Raspberry Pi
        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(new IPEndPoint(IPAddress.Parse(raspberryPiIpAddress), portNumber));
            Debug.Log("Socket connected to Raspberry Pi at " + raspberryPiIpAddress);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to connect to Raspberry Pi: " + e.Message);
        }
    }

    void Update()
    {
        if (frameCounter % sendFrequency == 0)
        {
            SendPositionData();
        }
        frameCounter++;
    }

    private void SendPositionData()
    {
        try
        {
            // Get the current values of Rx, Ry, Rz, and y
            string Rx = this.transform.eulerAngles.x.ToString("F2");
            string Ry = this.transform.eulerAngles.y.ToString("F2");
            float z = -this.transform.eulerAngles.z;
            string Rz = z.ToString("F2");
            string y = this.transform.position.y.ToString("F4");

            string dataToSend = Rx + ", " + Ry + ", " + Rz + ", " + y;
            
            // Convert the values to a byte array
            byte[] data = System.Text.Encoding.UTF8.GetBytes(dataToSend);

            // Send the data to the Raspberry Pi
            socket.Send(data);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error sending data to Raspberry Pi: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        if (socket != null && socket.Connected)
        {
            socket.Close();
        }
    }
}