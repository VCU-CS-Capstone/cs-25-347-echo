using System;
using System.Text;
using System.Net.Sockets;
using UnityEngine;

public class PacketSender : MonoBehaviour
{
    private const string targetHost = "192.168.0.3";
    private const int targetPort = 88;

    public void Start()
    {

    }
    // Start is called before the first frame update
    public void SendG()
    {
        string colorPacket = "POST /updatesettings HTTP/1.1\r\n" +
                             "Host: 192.168.0.20:88\r\n" +
                             "Connection: keep-alive\r\n" +
                             "Content-Length: 34\r\n" +
                             "Accept: application/json, text/plain, */*\r\n" +
                             "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36\r\n" +
                             "Content-Type: application/json\r\n" +
                             "Origin: http://192.168.0.20:88\r\n" +
                             "Referer: http://192.168.0.20:88/\r\n" +
                             "Accept-Encoding: gzip, deflate\r\n" +
                             "Accept-Language: en-US,en;q=0.9\r\n" +
                             "\r\n" +
                             "{\"groups\":{\"main\":{\"function\":0}}}";

        string movementPacket = "POST /updatesettings HTTP/1.1\r\n" +
                                "Host: 192.168.0.20:88\r\n" +
                                "Connection: keep-alive\r\n" +
                                "Content-Length: 45\r\n" +
                                "Accept: application/json, text/plain, */*\r\n" +
                                "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36\r\n" +
                                "Content-Type: application/json\r\n" +
                                "Origin: http://192.168.0.20:88\r\n" +
                                "Referer: http://192.168.0.20:88/\r\n" +
                                "Accept-Encoding: gzip, deflate\r\n" +
                                "Accept-Language: en-US,en;q=0.9\r\n" +
                                "\r\n" +
                                "{\"groups\":{\"main\":{\"palette\":1713995854033}}}";

        SendPacket(colorPacket);
        SendPacket(movementPacket);
    }

    public void SendY()
    {
        string colorPacket = "POST /updatesettings HTTP/1.1\r\n" +
                             "Host: 192.168.0.20:88\r\n" +
                             "Connection: keep-alive\r\n" +
                             "Content-Length: 34\r\n" +
                             "Accept: application/json, text/plain, */*\r\n" +
                             "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36\r\n" +
                             "Content-Type: application/json\r\n" +
                             "Origin: http://192.168.0.20:88\r\n" +
                             "Referer: http://192.168.0.20:88/\r\n" +
                             "Accept-Encoding: gzip, deflate\r\n" +
                             "Accept-Language: en-US,en;q=0.9\r\n" +
                             "\r\n" +
                             "{\"groups\":{\"main\":{\"function\":1713995592248}}}";

        string movementPacket = "POST /updatesettings HTTP/1.1\r\n" +
                                "Host: 192.168.0.20:88\r\n" +
                                "Connection: keep-alive\r\n" +
                                "Content-Length: 45\r\n" +
                                "Accept: application/json, text/plain, */*\r\n" +
                                "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36\r\n" +
                                "Content-Type: application/json\r\n" +
                                "Origin: http://192.168.0.20:88\r\n" +
                                "Referer: http://192.168.0.20:88/\r\n" +
                                "Accept-Encoding: gzip, deflate\r\n" +
                                "Accept-Language: en-US,en;q=0.9\r\n" +
                                "\r\n" +
                                "{\"groups\":{\"main\":{\"palette\":1713995224958}}}";

        SendPacket(colorPacket);
        SendPacket(movementPacket);
    }

    public void SendR()
    {
        string colorPacket = "POST /updatesettings HTTP/1.1\r\n" +
                             "Host: 192.168.0.20:88\r\n" +
                             "Connection: keep-alive\r\n" +
                             "Content-Length: 34\r\n" +
                             "Accept: application/json, text/plain, */*\r\n" +
                             "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36\r\n" +
                             "Content-Type: application/json\r\n" +
                             "Origin: http://192.168.0.20:88\r\n" +
                             "Referer: http://192.168.0.20:88/\r\n" +
                             "Accept-Encoding: gzip, deflate\r\n" +
                             "Accept-Language: en-US,en;q=0.9\r\n" +
                             "\r\n" +
                             "{\"groups\":{\"main\":{\"function\":160}}}";

        string movementPacket = "POST /updatesettings HTTP/1.1\r\n" +
                                "Host: 192.168.0.20:88\r\n" +
                                "Connection: keep-alive\r\n" +
                                "Content-Length: 45\r\n" +
                                "Accept: application/json, text/plain, */*\r\n" +
                                "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36\r\n" +
                                "Content-Type: application/json\r\n" +
                                "Origin: http://192.168.0.20:88\r\n" +
                                "Referer: http://192.168.0.20:88/\r\n" +
                                "Accept-Encoding: gzip, deflate\r\n" +
                                "Accept-Language: en-US,en;q=0.9\r\n" +
                                "\r\n" +
                                "{\"groups\":{\"main\":{\"palette\":1713993460952}}}";

        SendPacket(colorPacket);
        SendPacket(movementPacket);
    }


    private void SendPacket(string packet)
    {
        try
        {
            using (TcpClient client = new TcpClient(targetHost, targetPort))
            {
                Byte[] data = Encoding.ASCII.GetBytes(packet);
                NetworkStream stream = client.GetStream();

                stream.Write(data, 0, data.Length);

                // Read the response
                Byte[] responseData = new Byte[4096];
                Int32 bytes = stream.Read(responseData, 0, responseData.Length);
                String response = Encoding.ASCII.GetString(responseData, 0, bytes);
                Debug.Log("Received: " + response);

                stream.Close();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Socket error: " + e);
        }
    }
}
