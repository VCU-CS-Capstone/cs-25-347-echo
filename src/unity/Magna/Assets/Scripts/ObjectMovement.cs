using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed in units per second")]
    public float moveSpeed = 5.0f;

    [Tooltip("Whether to use local or world space for movement")]
    public bool useLocalSpace = true;

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