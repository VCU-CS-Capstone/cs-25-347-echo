using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class CollisionTracker : MonoBehaviour
{
    private bool touchingRed;
    private bool touchingYellow;
    private bool touchingGreen;
    private TcpClient client;
    private NetworkStream stream;
    private string picoIp = "192.168.0.3";
    private int picoPort = 80;


    public AnotherClass spawner;
    public UpdatedMove Robot;
    public PacketSender packetSender;

    // Reference to the RobotStateEvent ScriptableObject
    public RobotStateEvent stateEvent;

    void Start()
    {
       // packetSender = GetComponent<PacketSender>();
        if (packetSender == null)
        {
            Debug.LogError("PacketSender component not found!");
        }
        ConnectToPico();
    }

    void ConnectToPico()
    {
        client = new TcpClient();
        bool connected = false;
        while (!connected)
        {
            try
            {
                Debug.Log("Connecting to the Raspberry Pi Pico W...");
                client.Connect(picoIp, picoPort);
                stream = client.GetStream();
                Debug.Log("Connected to the Raspberry Pi Pico W.");
                connected = true;
            }
            catch (SocketException)
            {
                Debug.LogWarning("Failed to connect to the Raspberry Pi Pico W. Retrying in 1 second...");
                Thread.Sleep(1000);
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred: {e.Message}");
                break;
            }
        }
    }

    void SendCommand(string command)
    {
        if (client != null && client.Connected)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(command);
                stream.Write(data, 0, data.Length);
                Debug.Log($"Command \"{command}\" sent successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred while sending the command: {e.Message}");
                ConnectToPico(); // Attempt to reconnect
            }
        }
        else
        {
            Debug.LogError("Not connected to the server. Attempting to reconnect...");
            ConnectToPico();
        }
    }

    void OnDestroy()
    {
        if (client != null)
        {
            client.Close();
            Debug.Log("Disconnected from the Raspberry Pi Pico W.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        string tag = other.gameObject.tag;

        if (tag == "RedSphere")
        {
            touchingRed = true;
        }
        else if (tag == "YellowSphere")
        {
            touchingYellow = true;
        }
        else if (tag == "GreenSphere")
        {
            touchingGreen = true;
        }

        CheckCombinations();
    }

    void CheckCombinations()
    {
        if (touchingRed)
        {
            Robot.setPause(true);          
            return;
        }
        if (touchingYellow)
        {
            Robot.setPause(false);
            Robot.setSlow(3);
            return;
        }
        if (touchingGreen)
        {
            Robot.setSlow(1);
            return;
        }

       
    }
}
