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

namespace communication
{
    public class UDPCOMM : MonoBehaviour
    {

        /* UDP port where EGM communication should happen (specified in RobotStudio) */
        public static int port = 6511;
        /* UDP client used to send messages from computer to robot */
        private UdpClient server = null;
        /* Endpoint used to store the network address of the ABB robot.
         * Make sure your robot is available on your local network. The easiest option
         * is to connect your computer to the management port of the robot controller
         * using a network cable. */
        private IPEndPoint robotAddress;
        
        /* Variable used to count the number of messages sent */
        private uint sequenceNumber = 0;
        public GameObject cube;
        public GameObject Text;
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

        /* Current state of EGM communication (disconnected, connected or running) */
        string egmState = "Undefined";


        public TextMeshProUGUI egmStateText;

        /* (Unity) Start is called before the first frame update */
        void Start()
        {
            /* Initializes EGM connection with robot */
            //CreateConnection();
            startcom();
            

        }

        /* (Unity) Update is called once per frame */
        void Update()
        {
            cz = -cube.transform.position.z; //initialize variables above
            cx = cube.transform.position.x;
            cy = cube.transform.position.y;

            cubeMove(cx, cy, cz, (-cube.transform.eulerAngles.z - 180), cube.transform.eulerAngles.x, (-cube.transform.eulerAngles.y - 180));
        
            //Updates EGMState
            //egmStateText.text = "EGM State: " + egmState;
        }



        public void startcom()
        {
            Debug.Log("Connecting");
            
            server = new UdpClient(6511);
            Debug.Log("SERVERCREATED");
            robotAddress = new IPEndPoint(IPAddress.Parse("192.168.125.1"), port);

            UpdateValues();
        }


        private void UpdateValues()
        {
            byte[] bytes = null;
            /* Receives the messages sent by the robot in as a byte array */
            try
            {
                bytes = server.Receive(ref robotAddress);
                Text.SetActive(false);
                Debug.Log("Connected");
            }
            catch (SocketException e){
                Debug.Log(e);
            }
            if (bytes != null)
            {
                /* De-serializes the byte array using the EGM protocol */
                EgmRobot message = EgmRobot.Parser.ParseFrom(bytes);

                ParseCurrentPositionFromMessage(message);
            }


        }
        private void ParseCurrentPositionFromMessage(EgmRobot message)
        {
            /* Parse the current robot position and EGM state from message
               received from robot and update the related variables
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
                Debug.Log(egmState);
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

            using (MemoryStream memoryStream = new MemoryStream())
            {
                EgmSensor message = new EgmSensor();
                /* Prepare a new message in EGM format */
                CreatePoseMessage(message, zx, zy, zz, zrx, zry, zrz);

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

            EgmHeader hdr = new EgmHeader();
            hdr.Seqno = sequenceNumber++;
            hdr.Tm = (uint)DateTime.Now.Ticks;
            hdr.Mtype = EgmHeader.Types.MessageType.MsgtypeCorrection;

            message.Header = hdr;
            EgmPlanned planned_trajectory = new EgmPlanned();
            EgmPose cartesian_pos = new EgmPose();
            EgmCartesian tcp_p = new EgmCartesian();
            EgmEuler ea_p = new EgmEuler();

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
        public void cubeMove(double xx, double yy, double zz, double rrx, double rry, double rrz)
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
            y = (xx * 1000) + yc;//yC + deviation;
            x = (zz * 1000) + xc;//xC;
            z = (yy * 1000) + zc;//zC;
            rx = rrx;
            ry = rry;
            rz = rrz;
            //Debug.Log("x: " + x + "\ny: " + y + "\nz: " + z + "\nrx: " + rx + "\nry: " + ry + "\nrz: " + rz);
            SendPoseMessageToRobot(x,y,z,rx,ry,rz);
        }
    }
}