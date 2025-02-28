using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* EGM */
using Abb.Egm;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using System.ComponentModel;
using System;
using TMPro;

public class EgmCommunication : MonoBehaviour
{
    /* UDP port where EGM communication should happen (specified in RobotStudio) */
    public static int port = 6510;
    
    /* IP address of the robot controller - can be configured in the Unity Inspector */
    public string robotIpAddress = "192.168.0.4";
    
    /* UDP client used to send messages from computer to robot */
    private UdpClient server = null;
    
    /* Endpoint used to store the network address of the ABB robot.
     * Make sure your robot is available on your local network. The easiest option
     * is to connect your computer to the management port of the robot controller
     * using a network cable. */
    private IPEndPoint robotAddress;
    
    /* Variable used to count the number of messages sent */
    private uint sequenceNumber = 0;

    /* Robot joints values (in degrees) */
    /* If you are using a robot with 7 degrees of freedom (e.g., YuMi), 
       please adapt this code. */
    private double j1, j2, j3, j4, j5, j6;
    
    /* Current state of EGM communication (disconnected, connected or running) */
    private string egmState = "Undefined";

    /* UI elements */
    public Slider j1Slider, j2Slider, j3Slider, j4Slider, j5Slider, j6Slider;
    public TextMeshProUGUI egmStateText;
    
    /* Connection status */
    private bool isConnected = false;

    /* (Unity) Start is called before the first frame update */
    void Start()
    {
        /* Initialize EGM connection with robot */
        CreateConnection();

        /* Set up listeners for UI sliders to send joint values */
        if (j1Slider != null) j1Slider.onValueChanged.AddListener(delegate { SendJointsMessageToRobot(j1Slider.value, j2, j3, j4, j5, j6); });
        if (j2Slider != null) j2Slider.onValueChanged.AddListener(delegate { SendJointsMessageToRobot(j1, j2Slider.value, j3, j4, j5, j6); });
        if (j3Slider != null) j3Slider.onValueChanged.AddListener(delegate { SendJointsMessageToRobot(j1, j2, j3Slider.value, j4, j5, j6); });
        if (j4Slider != null) j4Slider.onValueChanged.AddListener(delegate { SendJointsMessageToRobot(j1, j2, j3, j4Slider.value, j5, j6); });
        if (j5Slider != null) j5Slider.onValueChanged.AddListener(delegate { SendJointsMessageToRobot(j1, j2, j3, j4, j5Slider.value, j6); });
        if (j6Slider != null) j6Slider.onValueChanged.AddListener(delegate { SendJointsMessageToRobot(j1, j2, j3, j4, j5, j6Slider.value); });

        /* Initialize sliders with robot's joint values */
        StartCoroutine(InitializeConnection());
    }

    /* (Unity) Update is called once per frame */
    void Update()
    {
        if (egmStateText != null)
            egmStateText.text = "EGM State: " + egmState;
    }

    /* (Unity) OnApplicationQuit is called when the program is closed */
    void OnApplicationQuit()
    {
        if (server != null)
        {
            server.Close();
        }
    }

    private void CreateConnection()
    {
        try 
        {
            server = new UdpClient(port);
            robotAddress = new IPEndPoint(IPAddress.Parse(robotIpAddress), port);
            Debug.Log("UDP server created on port " + port);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to create UDP connection: " + e.Message);
        }
    }

    private IEnumerator InitializeConnection()
    {
        yield return new WaitForSeconds(1.0f); // Wait for connection to stabilize
        
        try
        {
            if (server != null)
            {
                UpdateSlidersWithJointValues();
                isConnected = true;
                Debug.Log("Connection initialized successfully");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to initialize connection: " + e.Message);
        }
    }

    private void UpdateSlidersWithJointValues()
    {
        /* Receives the messages sent by the robot as a byte array */
        try
        {
            server.Client.ReceiveTimeout = 2000; // 2 second timeout
            byte[] bytes = server.Receive(ref robotAddress);

            if (bytes != null)
            {
                /* De-serializes the byte array using the EGM protocol */
                EgmRobot message = EgmRobot.Parser.ParseFrom(bytes);
                ParseJointValuesFromMessage(message);
                
                /* Update UI sliders with current joint values */
                if (j1Slider != null) j1Slider.value = (float)j1;
                if (j2Slider != null) j2Slider.value = (float)j2;
                if (j3Slider != null) j3Slider.value = (float)j3;
                if (j4Slider != null) j4Slider.value = (float)j4;
                if (j5Slider != null) j5Slider.value = (float)j5;
                if (j6Slider != null) j6Slider.value = (float)j6;
            }
        }
        catch (SocketException e)
        {
            Debug.LogWarning("Socket timeout while waiting for robot data: " + e.Message);
        }
        catch (Exception e)
        {
            Debug.LogError("Error updating sliders: " + e.Message);
        }
    }

    private void ParseJointValuesFromMessage(EgmRobot message)
    {
        /* Parse the current robot position and EGM state from message
           received from robot and update the related variables */

        /* Checks if header is valid */
        if (message.Header.HasSeqno && message.Header.HasTm)
        {
            j1 = message.FeedBack.Joints.Joints[0];
            j2 = message.FeedBack.Joints.Joints[1];
            j3 = message.FeedBack.Joints.Joints[2];
            j4 = message.FeedBack.Joints.Joints[3];
            j5 = message.FeedBack.Joints.Joints[4];
            j6 = message.FeedBack.Joints.Joints[5];
            egmState = message.MciState.State.ToString();
        }
        else
        {
            Debug.LogWarning("The message received from robot has an invalid header.");
        }
    }

    private void SendJointsMessageToRobot(double j1, double j2, double j3, double j4, double j5, double j6)
    {
        /* Send message containing new positions to robot in EGM format.
         * This is the primary method used to move the robot in joint coordinates. */

        /* Warning: If you are planning to manipulate an ABB robot with Hololens, this implementation
         * will not work. Hololens runs under Universal Windows Platform (UWP), which at the present
         * moment does not work with UdpClient class. DatagramSocket should be used instead. */

        if (!isConnected)
        {
            Debug.LogWarning("Cannot send message - not connected to robot");
            return;
        }

        try
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                EgmSensor message = new EgmSensor();
                /* Prepare a new message in EGM format */
                CreateJointsMessage(message, j1, j2, j3, j4, j5, j6);

                message.WriteTo(memoryStream);

                /* Send the message as a byte array over the network to the robot */
                int bytesSent = server.Send(memoryStream.ToArray(), (int)memoryStream.Length, robotAddress);

                if (bytesSent < 0)
                {
                    Debug.LogWarning("No message was sent to robot.");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending message to robot: " + e.Message);
        }
    }

    private void CreateJointsMessage(EgmSensor message, double j1, double j2, double j3, double j4, double j5, double j6)
    {
        /* Create a message in EGM format specifying a new joint configuration 
           for the ABB robot. The message contains a header with general
           information and a body with the planned joint configuration.

           Notice that in order for this code to work, your robot must be running a EGM client 
           in RAPID containing EGMActJoint and EGMRunJoint methods.

           See one example here: https://github.com/vcuse/egm-for-abb-robots/blob/main/EGMJointCommunication.modx */

        EgmHeader hdr = new EgmHeader();
        hdr.Seqno = sequenceNumber++;
        hdr.Tm = (uint)DateTime.Now.Ticks;
        hdr.Mtype = EgmHeader.Types.MessageType.MsgtypeCorrection;

        message.Header = hdr;
        EgmPlanned plannedTrajectory = new EgmPlanned();
        EgmJoints jointsConfiguration = new EgmJoints();

        jointsConfiguration.Joints.Add(j1);
        jointsConfiguration.Joints.Add(j2);
        jointsConfiguration.Joints.Add(j3);
        jointsConfiguration.Joints.Add(j4);
        jointsConfiguration.Joints.Add(j5);
        jointsConfiguration.Joints.Add(j6);

        plannedTrajectory.Joints = jointsConfiguration;
        message.Planned = plannedTrajectory;
    }
    
    // Public method to allow manual reconnection
    public void Reconnect()
    {
        if (server != null)
        {
            server.Close();
        }
        
        CreateConnection();
        StartCoroutine(InitializeConnection());
    }
}