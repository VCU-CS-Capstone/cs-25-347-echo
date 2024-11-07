using UnityEngine;

public class EventResponseHandler : MonoBehaviour
{
    public int state = 0;
    public RobotStateEvent stateEvent; // Reference to the ScriptableObject that raises events
    public PacketSender packetSender;  // Reference to the PacketSender component

    private void OnEnable()
    {
        // Subscribe to the event
        stateEvent.OnStateChange += HandleStateChange;
    }

    private void OnDisable()
    {
        // Unsubscribe from the event
        stateEvent.OnStateChange -= HandleStateChange;
    }

    // This method is called whenever the event is raised
    private void HandleStateChange(string newState)
    {
        switch (newState)
        {
            case "Stopped":
                Debug.Log("Event 'Stopped' detected, sending 'red' command.");
                packetSender.SendR();
                break;
            case "Slowed":
                Debug.Log("Event 'Slowed' detected, sending 'yellow' command.");
                packetSender.SendY();
                break;
            case "Normal":
                Debug.Log("Event 'Normal' detected, sending 'green' command.");
                packetSender.SendG();
                break;
        }
    }
}

