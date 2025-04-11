using UnityEngine;

/* EGM */
using Abb.Egm;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using System;

public class UDPCOMM : MonoBehaviour
{
    [Header("Connection Settings")]
    /* UDP port where EGM communication should happen (specified in RobotStudio) */
    public static int port = 6511;
    /* UDP client used to send messages from computer to robot */
    private UdpClient server = null;
    /* Endpoint used to store the network address of the ABB robot.
     * Make sure your robot is available on your local network. The easiest option
     * is to connect your computer to the management port of the robot controller
     * using a network cable. */
    private IPEndPoint robotAddress;

    public string robotIpAddress = "192.168.0.4";
    
    [Header("Noise Configuration")]
    [Tooltip("Enable or disable position noise")]
    public bool enableNoise = true;
    
    [Tooltip("Amount of noise to add (±) in mm")]
    public float noiseAmount = 1f;

    /* Variable used to count the number of messages sent */
    private uint sequenceNumber = 0;
    public GameObject target; // The object whose position is sent to the robot
    public GameObject follower; // The object whose position is set by the robot's initial message
    /* Robot cartesian position and rotation values */
    double x, y, z, rx, ry, rz;
    double xc, yc, zc;
    double cz; //initialize variables above
    double cx;
    double cy;
    double crx;
    double cry;
    double crz;
    Vector3 angles;

    public GameObject joint1;
    public GameObject joint2;
    public GameObject joint3;
    public GameObject joint4;
    public GameObject joint5;
    public GameObject joint6;

    /* Current state of EGM communication (disconnected, connected or running) */
    string egmState = "Undefined";

    /* Flag to track if we've logged the initial position */
    private bool initialPositionLogged = false;

    /* Connection status tracking */
    private bool isConnectionEstablished = false;

    // Public properties to expose connection status
    public bool IsConnectionEstablished => isConnectionEstablished;
    public string EgmState => egmState;

    // Property to check if EGM is in RUNNING state
    public bool IsEgmRunning => egmState == "RUNNING";

    /* (Unity) Start is called before the first frame update */
    void Start()
    {
        /* Initializes EGM connection with robot */
        startcom();
    }

    /* (Unity) FixedUpdate is called once per fixed frame */
    void FixedUpdate()
    {
        // Read position and rotation from the follower GameObject
        if (follower != null)
        {
            cz = -follower.transform.position.z;
            cx = follower.transform.position.x;
            cy = follower.transform.position.y;

            // Send follower's pose data to the robot
            CubeMove(cx, cy, cz, -follower.transform.eulerAngles.z - 180, follower.transform.eulerAngles.x, -follower.transform.eulerAngles.y - 180);
        }
        else
        {
            Debug.LogWarning("Follower GameObject not assigned in UDPCOMM script. Cannot send position to robot.");
        }
    }

    public void startcom()
    {
        Debug.Log("Connecting");

        server = new UdpClient(port);
        Debug.Log("SERVER CREATED");
        robotAddress = new IPEndPoint(IPAddress.Parse(robotIpAddress), port);

        // Set connection as established once startcom is completed
        isConnectionEstablished = true;
        Debug.Log("EGM connection established");

        UpdateValues();
    }

    private void UpdateValues()
    {
        byte[] bytes = null;
        /* Receives the messages sent by the robot in as a byte array */
        try
        {
            bytes = server.Receive(ref robotAddress);

        }
        catch (SocketException e)
        {
            Debug.Log(e);
        }
        if (bytes != null)
        {
            /* De-serializes the byte array using the EGM protocol */
            EgmRobot message = EgmRobot.Parser.ParseFrom(bytes);

            ParseCurrentPositionFromMessage(message);
        }
    }

    private void UpdateJointsValues()
    {
        byte[] bytes = null;
        /* Receives the messages sent by the robot in as a byte array */
        try
        {
            bytes = server.Receive(ref robotAddress);

        }
        catch (SocketException e)
        {
            Debug.Log(e);
        }
        if (bytes != null)
        {
            /* De-serializes the byte array using the EGM protocol */
            EgmRobot message = EgmRobot.Parser.ParseFrom(bytes);

            ParseCurrentJointsPositionFromMessage(message);
        }
    }

    private void ParseCurrentJointsPositionFromMessage(EgmRobot message)
    {
        joint1.transform.localEulerAngles = new Vector3(0, 0, -(float)message.FeedBack.Joints.Joints[0]);
        joint2.transform.localEulerAngles = new Vector3(0, -(float)message.FeedBack.Joints.Joints[1], 0);
        joint3.transform.localEulerAngles = new Vector3(0, -(float)message.FeedBack.Joints.Joints[2], 0);
        joint4.transform.localEulerAngles = new Vector3(-(float)message.FeedBack.Joints.Joints[3], 0, 0);
        joint5.transform.localEulerAngles = new Vector3(0, -(float)message.FeedBack.Joints.Joints[4], 0);
        joint6.transform.localEulerAngles = new Vector3(-(float)message.FeedBack.Joints.Joints[5], 0, 0);
    }

    private void ParseCurrentPositionFromMessage(EgmRobot message)
    {
        /* Parse the current robot position and EGM state from message
           received from robot and update the related variables */
        /* Checks if header is valid */
        if (message.Header.HasSeqno && message.Header.HasTm)
        {
            x = message.FeedBack.Cartesian.Pos.X;
            y = message.FeedBack.Cartesian.Pos.Y;
            z = message.FeedBack.Cartesian.Pos.Z;
            xc = x;
            yc = y;
            zc = z;
            rx = message.FeedBack.Cartesian.Euler.X;
            ry = message.FeedBack.Cartesian.Euler.Y;
            rz = message.FeedBack.Cartesian.Euler.Z;
            egmState = message.MciState.State.ToString();
            // Calculate the new position based on received robot data
            Vector3 newPosition = new Vector3((float)y / 1000, (float)z / 1000, (float)-x / 1000);

            // Set the target's position
            if (target != null)
            {
                target.transform.position = newPosition;
            }
            else
            {
                Debug.LogWarning("Target GameObject not assigned in UDPCOMM script.");
            }

            // Set the follower's position
            if (follower != null)
            {
                follower.transform.position = newPosition;
            }
            else
            {
                Debug.LogWarning("Follower GameObject not assigned in UDPCOMM script.");
            }

            // Log the initial position if not already logged
            if (!initialPositionLogged)
            {
                initialPositionLogged = true;
                Debug.Log("Initial robot position - X:" + x + ", Y:" + y + ", Z:" + z +
                          ", RX:" + rx + ", RY:" + ry + ", RZ:" + rz);
                Debug.Log("Initial EGM state: " + egmState);
            }
        }
        else
        {
            Console.WriteLine("The message received from robot is invalid.");
        }
    }

    private void SendPoseMessageToRobot(double zx, double zy, double zz, double zrx, double zry, double zrz)
    {
        /* Send message containing new positions to robot in EGM format.
         * This is the primary method used to move the robot in cartesian coordinates. */

        /* Warning: If you are planning to manipulate an ABB robot with Hololens, this implementation
         * will not work. Hololens runs under Universal Windows Platform (UWP), which at the present
         * moment does not work with UdpClient class. DatagramSocket should be used instead. */

        // Add configurable random noise to position values to ensure new data is always sent
        // Since position values are already multiplied by 1000 in CubeMove, we need larger noise values
        double noiseX = zx;
        double noiseY = zy;
        double noiseZ = zz;
        
        // Apply noise only if enabled
        if (enableNoise)
        {
            noiseX += UnityEngine.Random.Range(-noiseAmount, noiseAmount);
            noiseY += UnityEngine.Random.Range(-noiseAmount, noiseAmount);
            noiseZ += UnityEngine.Random.Range(-noiseAmount, noiseAmount);
        }

        using (MemoryStream memoryStream = new())
        {
            EgmSensor message = new();
            /* Prepare a new message in EGM format */
            CreatePoseMessage(message, noiseX, noiseY, noiseZ, zrx, zry, zrz);

            message.WriteTo(memoryStream);

            /* Send the message as a byte array over the network to the robot */
            int bytesSent = server.Send(memoryStream.ToArray(), (int)memoryStream.Length, robotAddress);

            if (bytesSent < 0)
            {
                Console.WriteLine("No message was sent to robot.");
            }
        }
    }

    private void CreatePoseMessage(EgmSensor message, double zx, double zy, double zz, double zrx, double zry, double zrz)
    {
        /* Create a message in EGM format specifying a new location to where
           the ABB robot should move to. The message contains a header with general
           information and a body with the planned trajectory.

           Notice that in order for this code to work, your robot must be running a EGM client 
           in RAPID containing EGMActPose and EGMRunPose methods.

           See one example here: https://github.com/vcuse/egm-for-abb-robots/blob/main/EGMPoseCommunication.modx */

        EgmHeader hdr = new()
        {
            Seqno = sequenceNumber++,
            Tm = (uint)DateTime.Now.Ticks,
            Mtype = EgmHeader.Types.MessageType.MsgtypeCorrection
        };

        message.Header = hdr;
        EgmPlanned planned_trajectory = new();
        EgmPose cartesian_pos = new();
        EgmCartesian tcp_p = new();
        EgmEuler ea_p = new();

        /* Translation values */
        tcp_p.X = zx;
        tcp_p.Y = zy;
        tcp_p.Z = zz;

        /* Rotation values (in Euler angles) */
        ea_p.X = zrx;
        ea_p.Y = zry;
        ea_p.Z = zrz;

        cartesian_pos.Pos = tcp_p;
        cartesian_pos.Euler = ea_p;

        planned_trajectory.Cartesian = cartesian_pos;
        message.Planned = planned_trajectory;
        //Debug.Log("MSG MADE");

    }
    public void CubeMove(double xx, double yy, double zz, double rrx, double rry, double rrz)
    /*

    Summary: Retrieves x,y, and z data of cube location, and transcribes this information into coordinates to send
    to robot controller. Once the coordinates are set, SendUDPMessage() is utilized to build and send a UDP packet 
    that contains these coordinates to the robot controller.
    Inputs: 
        - x: X position of cube
        - y: Y position of cube
        - z: Z position of cube
        - xx: X rotational position of cube
        - yy: Y rotational position of cube
        - zz: Z rotational position of cube

    */
    {
        y = (xx * 1000);//yC + deviation;
        x = (zz * 1000);//xC;
        z = (yy * 1000);//zC;
        rx = rrx;
        ry = rry;
        rz = rrz;
        //Debug.Log("x: " + x + "\ny: " + y + "\nz: " + z + "\nrx: " + rx + "\nry: " + ry + "\nrz: " + rz);
        SendPoseMessageToRobot(x, y, z, rx, ry, rz);
        UpdateJointsValues();
    }
}
