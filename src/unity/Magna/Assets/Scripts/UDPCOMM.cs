using UnityEngine;

/* EGM */
using Abb.Egm;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using System;

/// <summary>
/// Handles UDP communication with an ABB robot using the Externally Guided Motion (EGM) protocol.
/// Sends target pose commands and receives robot feedback (position and joint angles).
/// </summary>
public class UDPCOMM : MonoBehaviour
{
    [Header("Connection Settings")]
    /// <summary>
    /// UDP port for EGM communication. Must match the port configured in RobotStudio for the EGM UDPUC device.
    /// </summary>
    public static int port = 6511;
    /* UDP client used to send messages from computer to robot */
    private UdpClient server = null;
    /* Endpoint used to store the network address of the ABB robot.
     * Make sure your robot is available on your local network. The easiest option
     * is to connect your computer to the management port of the robot controller
     * using a network cable. */
    private IPEndPoint robotAddress;

    /// <summary>
    /// IP address of the ABB robot controller.
    /// </summary>
    public string robotIpAddress = "192.168.0.4";

    [Header("Noise Configuration")]
    /// <summary>
    /// If true, adds a small amount of random noise to the target position before sending.
    /// This can help ensure the robot controller always registers a change in position data.
    /// </summary>
    [Tooltip("Enable or disable position noise")]
    public bool enableNoise = true;

    /// <summary>
    /// The maximum amount of random noise (positive or negative) added to each position axis (X, Y, Z) if noise is enabled. Units are in millimeters (mm).
    /// </summary>
    [Tooltip("Amount of noise to add (±) in mm")]
    public float noiseAmount = 1f;

    /* Variable used to count the number of messages sent */
    private uint sequenceNumber = 0;
    /// <summary>
    /// The target GameObject in the Unity scene. The position and rotation of this object are sent to the robot as the target pose.
    /// </summary>
    public GameObject target; // The object whose position is sent to the robot
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

    /// <summary>GameObject representing Joint 1 of the robot model in Unity for visualization.</summary>
    public GameObject joint1;
    /// <summary>GameObject representing Joint 2 of the robot model in Unity for visualization.</summary>
    public GameObject joint2;
    /// <summary>GameObject representing Joint 3 of the robot model in Unity for visualization.</summary>
    public GameObject joint3;
    /// <summary>GameObject representing Joint 4 of the robot model in Unity for visualization.</summary>
    public GameObject joint4;
    /// <summary>GameObject representing Joint 5 of the robot model in Unity for visualization.</summary>
    public GameObject joint5;
    /// <summary>GameObject representing Joint 6 of the robot model in Unity for visualization.</summary>
    public GameObject joint6;

    /* Connection status tracking */
    private bool isConnectionEstablished = false;

    /// <summary>
    /// Gets a value indicating whether the UDP connection to the robot has been initialized.
    /// </summary>
    public bool IsConnectionEstablished => isConnectionEstablished;

    /// <summary>
    /// (Unity) Called before the first frame update. Initializes the EGM connection.
    /// </summary>
    void Start()
    {
        /* Initializes EGM connection with robot */
        startcom();
    }

    /// <summary>
    /// (Unity) Called once per fixed frame. Reads the target GameObject's pose and sends it to the robot.
    /// </summary>
    void FixedUpdate()
    {
        // Read position and rotation from the target GameObject
        if (target != null)
        {
            cz = -target.transform.position.z;
            cx = target.transform.position.x;
            cy = target.transform.position.y;

            // Send target's pose data to the robot
            CubeMove(cx, cy, cz, -target.transform.eulerAngles.z - 180, target.transform.eulerAngles.x, -target.transform.eulerAngles.y - 180);
        }
        else
        {
            Debug.LogWarning("Target GameObject not assigned in UDPCOMM script. Cannot send position to robot.");
        }
    }

    /// <summary>
    /// Initializes the UDP client and establishes the endpoint for communicating with the robot controller.
    /// Sets the <see cref="isConnectionEstablished"/> flag upon successful initialization.
    /// </summary>
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
    /// <summary>
    /// Processes the target pose from Unity coordinates, converts it to the robot's coordinate system (including scaling),
    /// sends the target pose to the robot via EGM, and updates the visualized robot joints based on feedback.
    /// </summary>
    /// <remarks>
    /// This method performs coordinate transformations:
    /// - Unity X maps to Robot Y
    /// - Unity Y maps to Robot Z
    /// - Unity Z maps to Robot -X
    /// It also scales the position values by 1000 (Unity meters to robot millimeters).
    /// Rotational values might also undergo transformation based on axis alignment.
    /// </remarks>
    /// <param name="xx">Target X position in Unity coordinates (meters).</param>
    /// <param name="yy">Target Y position in Unity coordinates (meters).</param>
    /// <param name="zz">Target Z position in Unity coordinates (meters).</param>
    /// <param name="rrx">Target X rotation (Euler angle) in Unity coordinates (degrees).</param>
    /// <param name="rry">Target Y rotation (Euler angle) in Unity coordinates (degrees).</param>
    /// <param name="rrz">Target Z rotation (Euler angle) in Unity coordinates (degrees).</param>
    public void CubeMove(double xx, double yy, double zz, double rrx, double rry, double rrz)
    {
        // Coordinate transformation and scaling (Unity meters to Robot mm)
        // Unity X -> Robot Y
        // Unity Y -> Robot Z
        // Unity Z -> Robot -X
        y = (xx * 1000);
        x = (-zz * 1000); // Note the negation for Z to -X mapping
        z = (yy * 1000);//zC;
        rx = rrx;
        ry = rry;
        rz = rrz;
        //Debug.Log("x: " + x + "\ny: " + y + "\nz: " + z + "\nrx: " + rx + "\nry: " + ry + "\nrz: " + rz);
        SendPoseMessageToRobot(x, y, z, rx, ry, rz);
        UpdateJointsValues();
    }
}
