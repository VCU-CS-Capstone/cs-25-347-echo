using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json; // Make sure Newtonsoft.Json is in your project
using System.Threading; // For running the listener on a separate thread

public class fromIsaac : MonoBehaviour
{
    public int UDP_PORT = 5005; // Should match the sender's port

    private UdpClient udpListener;
    private Thread listenerThread;
    private bool isListening = false;

    // Volatile to ensure that the value is read from main memory
    // and not from CPU cache when accessed by different threads.
    private  Vector3 receivedPosition;
    private  Vector3 receivedRotation;
    private volatile bool newDataReceived = false;

    // Optional: Assign a GameObject here if you want to move it
    // based on the received data.
    public GameObject targetObject;

    void Start()
    {
        try
        {
            udpListener = new UdpClient(UDP_PORT);
            Debug.Log($"UDP Listener started on port {UDP_PORT}");

            listenerThread = new Thread(new ThreadStart(ListenForMessages));
            listenerThread.IsBackground = true; // Ensure thread closes when application quits
            listenerThread.Start();
            isListening = true;
            Debug.Log("is listen");
        }
        catch (SocketException e)
        {
            Debug.LogError($"SocketException in Start: {e.Message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error starting UDP listener: {e.Message}");
        }
    }

    private void ListenForMessages()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, UDP_PORT);

        try
        {
            while (isListening)
            {
                // Blocks until a message returns on this socket from a remote host.
                byte[] receivedBytes = udpListener.Receive(ref remoteEndPoint);
                string jsonMessage = Encoding.UTF8.GetString(receivedBytes);

                // Deserialize the JSON message back into a float array
                float[] positionAndRotationArray = JsonConvert.DeserializeObject<float[]>(jsonMessage);

                if (positionAndRotationArray != null && positionAndRotationArray.Length == 6)
                {
                    // Update the volatile fields. These will be read by the main thread in Update().
                    receivedPosition = new Vector3(positionAndRotationArray[0], positionAndRotationArray[1], positionAndRotationArray[2]);
                    receivedRotation = new Vector3(positionAndRotationArray[3], positionAndRotationArray[4], positionAndRotationArray[5]);
                    newDataReceived = true; // Signal that new data is available

                    // Log to console (optional, can be verbose)
                     Debug.Log($"Received from {remoteEndPoint}: Position({receivedPosition.x}, {receivedPosition.y}, {receivedPosition.z}), Rotation({receivedRotation.x}, {receivedRotation.y}, {receivedRotation.z})");
                }
                else
                {
                    Debug.LogWarning("Received malformed data or incorrect array length.");
                }
            }
        }
        catch (SocketException e)
        {
            // This can happen if the socket is closed while Receive is blocking
            if (isListening) // Only log if we weren't expecting it to close
            {
                Debug.LogError($"SocketException in ListenerThread: {e.Message}");
            }
        }
        catch (JsonException e)
        {
            Debug.LogError($"JsonException: Could not deserialize message. {e.Message}. Received: {Encoding.UTF8.GetString(udpListener.Receive(ref remoteEndPoint))}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ListenerThread: {e.Message}");
        }
        finally
        {
            Debug.Log("Listener thread finished.");
        }
    }

    void Update()
    {
        // Check if new data has been received from the listener thread
        if (newDataReceived)
        {
            // Process the received data in the main thread
            Debug.Log($"Processing new data: Position({receivedPosition.x}, {receivedPosition.y}, {receivedPosition.z}), Rotation({receivedRotation.x}, {receivedRotation.y}, {receivedRotation.z})");

            // Example: Apply the received transform to a target GameObject
            if (targetObject != null)
            {
                targetObject.transform.position = receivedPosition;
                targetObject.transform.eulerAngles = receivedRotation;
            }

            newDataReceived = false; // Reset the flag
        }
    }

    private void OnApplicationQuit()
    {
        StopListening();
    }

    private void OnDestroy()
    {
        StopListening();
    }

    private void StopListening()
    {
        isListening = false; // Signal the thread to stop

        if (udpListener != null)
        {
            udpListener.Close(); // This will cause the blocking Receive to throw a SocketException and exit the loop
            udpListener = null;
            Debug.Log("UDP Listener closed.");
        }

        if (listenerThread != null && listenerThread.IsAlive)
        {
            listenerThread.Join(500); // Wait for the thread to finish (with a timeout)
            if (listenerThread.IsAlive)
            {
                // Optionally force abort if it doesn't stop gracefully, though generally not recommended
                // listenerThread.Abort();
                Debug.LogWarning("Listener thread did not terminate gracefully.");
            }
            listenerThread = null;
        }
    }
}