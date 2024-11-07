using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedSphere : MonoBehaviour
{
    private Rigidbody rb;
    public CollisionManager manager;
    public RobotStateEvent stateEvent;
    public PacketSender packetSender;
    // Start is called before the first frame update
    void Start()
    {
        //manager = GetComponent<CollisionManager>();
        rb = GetComponent<Rigidbody>();
    }


    // Called when the GameObject collides with another collider
    void OnTriggerEnter(Collider other)
    {
        // Check if the collision is with the specific object you want
        if (other.gameObject.CompareTag("Capsule"))
        {
            Debug.Log("Collided");
            manager.AddtoBuffer("red");
        }
    }
}