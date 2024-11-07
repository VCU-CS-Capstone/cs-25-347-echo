using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Green_go : MonoBehaviour
{
    private Rigidbody rb;
    public UpdatedMove Robot;
    // Start is called before the first frame update
    void Start()
    {
        //Robot = GetComponent<UpdatedMove>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Called when the GameObject collides with another collider
    void OnTriggerEnter(Collider other)
    {
        // Check if the collision is with the specific object you want
        if (other.gameObject.CompareTag("Capsule"))
        {
            // Slow the robot
            GoRobot();
        }
    }

    

    // Method to bring the robot back to normal speed
    void GoRobot()
    {   
        Robot.setSlow(1);
    }
}

