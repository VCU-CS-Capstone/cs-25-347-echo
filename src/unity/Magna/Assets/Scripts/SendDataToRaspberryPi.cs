using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class SendDataToRaspberryPi : MonoBehaviour
{
    // Replace with the IP address of the Raspberry Pi
    private string raspberryPiIpAddress = "192.168.0.50";

    // Replace with the port number that the Raspberry Pi is listening on
    private int portNumber = 8000;

    // The socket that will be used to send data to the Raspberry Pi
    private Socket socket;

    int frame = 0;

    void Start()
    {
        // Create a socket and connect to the Raspberry Pi
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect(new IPEndPoint(IPAddress.Parse(raspberryPiIpAddress), portNumber));
    }

    void Update()
    {
        if(frame%50 == 0){
        // Get the current values of Rx, Ry, Rz, and y
        string Rx = this.transform.eulerAngles.x.ToString("F2");
        string Ry = this.transform.eulerAngles.y.ToString("F2");
        float z = -this.transform.eulerAngles.z;
        string Rz = z.ToString("F2");
        string y = this.transform.position.y.ToString("F4");

        string dat = Rx + ", " + Ry + ", " + Rz+ ", " + y;
        
        //Debug.Log(dat);
        // Convert the values to a byte array
        byte[] data =  System.Text.Encoding.UTF8.GetBytes(dat);
        //byte[] data = System.BitConverter.GetBytes(Ry);
        //byte[] data = System.BitConverter.GetBytes(Rz);
        //byte[] data = System.BitConverter.GetBytes(y);

        // Send the data to the Raspberry Pi
        socket.Send(data);
        
        }
        frame+=1;
    }
}