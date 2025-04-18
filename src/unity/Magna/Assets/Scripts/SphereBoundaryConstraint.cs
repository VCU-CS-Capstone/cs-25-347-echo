using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereBoundaryConstraint : MonoBehaviour
{
    public Transform sphereCenter; // Assign the center of your boundary sphere
    public float boundaryRadius = 0.75f; // Match this to your boundary sphere's radius
    
    // LateUpdate runs after all Update methods
    void LateUpdate()
    {
        if (sphereCenter == null)
        {
            // Try to find the CollisionBoundary object if not assigned
            GameObject boundaryObj = GameObject.Find("CollisionBoundary");
            if (boundaryObj != null)
            {
                sphereCenter = boundaryObj.transform;
                SphereCollider boundaryCollider = boundaryObj.GetComponent<SphereCollider>();
                if (boundaryCollider != null)
                {
                    boundaryRadius = boundaryCollider.radius;
                }
            }
            else
            {
                Debug.LogError("Sphere center not assigned and CollisionBoundary not found!");
                return;
            }
        }
        
        // Calculate distance from center
        Vector3 toCenter = transform.position - sphereCenter.position;
        float distance = toCenter.magnitude;
        
        // If outside boundary, move back to boundary
        if (distance > boundaryRadius)
        {
            // Normalize and scale to boundary radius
            Vector3 clampedPosition = sphereCenter.position + toCenter.normalized * boundaryRadius;
            
            // Apply the corrected position
            transform.position = clampedPosition;
        }
    }
}