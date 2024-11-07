using UnityEngine;
using System.Collections.Generic;  // This namespace is required for using List<>
using System.Net;
using System.Net.Sockets;
using System.Text;

public class CollisionManager : MonoBehaviour
{
    public PacketSender packetSender;
    private List<string> buffer = new List<string>();
    private int frame = 0;
    private string sigZone;

    public void AddtoBuffer(string color)
    {
        buffer.Add(color);
    }

    void Update()
    {
        frame++;

        if (frame == 20)
        {
            sigZone = "green";  // Assume green unless a higher priority color is detected

            // Print the entire buffer before processing
            Debug.Log("Buffer contents before processing: " + ListToString(buffer));

            bool foundRed = false;
            bool foundYellow = false;
            foreach (string color in buffer)
            {
                Debug.Log("Processing color: " + color);
                if (color == "red")
                {
                    foundRed = true;  // Mark that red is found
                }
                else if (color == "yellow")
                {
                    foundYellow = true;  // Mark that yellow is found
                }
            }

            // Determine the highest priority color to send
            if (foundRed)
            {
                sigZone = "red";
                packetSender.SendR();  // Send the red signal immediately
            }
            else if (foundYellow)
            {
                sigZone = "yellow";
                packetSender.SendY();  // Send the yellow signal
            }
            else
            {
                sigZone = "green";
                packetSender.SendG();  // Send the green signal as fallback
            }

            SendUDP(sigZone);

            // Print the final decision on sigZone
            Debug.Log("Final sigZone: " + sigZone);

            // Clear the buffer array
            buffer.Clear();
            frame = 0; // Reset frame count
        }
    }

    // Helper method to convert list to string
    private string ListToString(List<string> list)
    {
        return string.Join(", ", list);
    }

    // Method to send UDP message to Pico W
    private void SendUDP(string message)
    {
        UdpClient client = new UdpClient();

        // Update your IP to the IP of the haptic feedback device you use
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("192.168.0.8"), 80);
        byte[] data = Encoding.UTF8.GetBytes(message);
        client.Send(data, data.Length, endPoint);
        client.Close();
    }
}

