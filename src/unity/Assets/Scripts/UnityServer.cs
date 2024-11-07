using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

public class UnityServer : MonoBehaviour
{
    public float[] xPosArray;
    public float[] yPosArray;
    public float[] zPosArray;
    private UdpClient udpServer;
    private int port = 12345;
    
    void Start()
    {
        udpServer = new UdpClient(port);
        udpServer.BeginReceive(ReceiveData, null);
        Debug.Log("UDP server is listening on port " + port);
    }

    private void ReceiveData(System.IAsyncResult result)
    {
        IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.Any, port);
        byte[] receivedBytes = udpServer.EndReceive(result, ref clientEndpoint);
        string receivedData = Encoding.UTF8.GetString(receivedBytes);

        receivedData = receivedData.Trim('[', ']');
        string[] arrays = receivedData.Split(';');

        if (arrays.Length == 3) {
            xPosArray = parseFloatArray(arrays[0]);
            yPosArray = parseFloatArray(arrays[1]);
            zPosArray = parseFloatArray(arrays[2]);
        }
        Debug.Log("xPos: " + string.Join(", ", xPosArray));
        Debug.Log("yPos: " + string.Join(", ", yPosArray));
        Debug.Log("zPos: " + string.Join(", ", zPosArray));


        // Continue listening for more data
        udpServer.BeginReceive(ReceiveData, null);
    }

    void OnApplicationQuit()
    {
        // Close the UDP server when the application quits
        if (udpServer != null)
        {
            udpServer.Close();
        }
    }

    float[] parseFloatArray(string array) {
        string[] elements = array.Split(" ");
        float[] result = new float[elements.Length];
        for (int i = 0; i < elements.Length; i++) {
            float.TryParse(elements[i], out result[i]);
        }
        return result;
    }
}

