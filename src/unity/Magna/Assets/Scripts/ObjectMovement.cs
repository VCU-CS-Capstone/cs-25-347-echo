using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows manual movement of the GameObject this script is attached to using keyboard input (WASDQE).
/// Typically used to control the target object for the robot.
/// </summary>
public class ObjectMovement : MonoBehaviour
{
    /// <summary>
    /// The speed at which the object moves, in Unity units per second.
    /// </summary>
    [Header("Movement Settings")]
    [Tooltip("Movement speed in units per second")]
    public float moveSpeed = 5.0f;

    /// <summary>
    /// If true, movement (WASD) is relative to the object's local orientation (forward/backward/left/right).
    /// If false, movement is relative to the world axes (X/Z). QE movement is always along the world Y-axis.
    /// </summary>
    [Tooltip("Whether to use local or world space for movement")]
    public bool useLocalSpace = true;

    /// <summary>
    /// (Unity) Called once per frame. Handles keyboard input and applies movement to the transform.
    /// </summary>
    private void Update()
    {
        // Get input values
        float horizontalInput = Input.GetAxis("Horizontal"); // A and D keys
        float verticalInput = Input.GetAxis("Vertical");     // W and S keys
        
        // Get Q and E input for up/down movement
        float upDownInput = 0f;
        if (Input.GetKey(KeyCode.Q))
            upDownInput -= 1.0f;
        if (Input.GetKey(KeyCode.E))
            upDownInput += 1.0f;

        // Calculate movement direction
        Vector3 movementDirection = new Vector3(horizontalInput, upDownInput, verticalInput);
        
        // Apply movement based on space setting
        if (useLocalSpace)
        {
            // Move relative to object's orientation
            transform.Translate(movementDirection * moveSpeed * Time.deltaTime, Space.Self);
        }
        else
        {
            // Move in world space
            transform.Translate(movementDirection * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}