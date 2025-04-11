using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Constrains a GameObject's position to stay within boundaries defined in TaskProgrammer.
/// Attach this component to any GameObject that needs to be constrained within the same boundaries
/// as defined in the TaskProgrammer.
/// </summary>
public class BoundaryConstraint : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the TaskProgrammer that defines the boundaries. Will auto-find if not set.")]
    [SerializeField] private TaskProgrammer taskProgrammer;

    [Header("Behavior Settings")]
    [Tooltip("If true, the object's position will be constrained every frame")]
    public bool constrainContinuously = true;

    [Tooltip("If true, shows debug messages when the object is constrained")]
    public bool showDebugMessages = false;

    private Vector3 lastConstrainedPosition;
    private bool wasConstrained = false;
    private bool hasValidReference = false;

    private void Start()
    {
        // Find TaskProgrammer if not assigned
        if (taskProgrammer == null)
        {
            // Try to use the static instance first
            if (TaskProgrammer.Instance != null)
            {
                taskProgrammer = TaskProgrammer.Instance;
                hasValidReference = true;
            }
            else
            {
                // Fall back to finding in scene
                taskProgrammer = FindObjectOfType<TaskProgrammer>();
                if (taskProgrammer != null)
                {
                    hasValidReference = true;
                    Debug.Log($"BoundaryConstraint on '{gameObject.name}' auto-found TaskProgrammer reference.");
                }
                else
                {
                    Debug.LogError($"BoundaryConstraint on '{gameObject.name}' could not find a TaskProgrammer in the scene. Constraints will not be applied.");
                    hasValidReference = false;
                    return;
                }
            }
        }
        else
        {
            hasValidReference = true;
        }

        // Ensure the object starts within boundaries
        ConstrainPosition();
        lastConstrainedPosition = transform.position;
    }

    private void Update()
    {
        if (constrainContinuously && hasValidReference)
        {
            ConstrainPosition();
        }
    }

    /// <summary>
    /// Constrains the object's position to stay within the boundaries defined in TaskProgrammer
    /// </summary>
    public void ConstrainPosition()
    {
        if (!hasValidReference || taskProgrammer == null)
        {
            // Try to re-find the reference
            if (TaskProgrammer.Instance != null)
            {
                taskProgrammer = TaskProgrammer.Instance;
                hasValidReference = true;
            }
            else
            {
                taskProgrammer = FindObjectOfType<TaskProgrammer>();
                hasValidReference = (taskProgrammer != null);
            }

            if (!hasValidReference)
            {
                Debug.LogWarning($"BoundaryConstraint on '{gameObject.name}' has no TaskProgrammer reference. Cannot constrain position.");
                return;
            }
        }

        Vector3 currentPosition = transform.position;
        Vector3 constrainedPosition = currentPosition;

        // Get boundary values from TaskProgrammer
        Vector3 boundaryMin = taskProgrammer.GetBoundaryMin();
        Vector3 boundaryMax = taskProgrammer.GetBoundaryMax();

        // Clamp to boundaries
        constrainedPosition.x = Mathf.Clamp(constrainedPosition.x, boundaryMin.x, boundaryMax.x);
        constrainedPosition.y = Mathf.Clamp(constrainedPosition.y, boundaryMin.y, boundaryMax.y);
        constrainedPosition.z = Mathf.Clamp(constrainedPosition.z, boundaryMin.z, boundaryMax.z);

        // Check if position was actually constrained
        bool isConstrained = constrainedPosition != currentPosition;

        // Only update position and log if there was a change
        if (isConstrained)
        {
            transform.position = constrainedPosition;
            
            if (showDebugMessages)
            {
                Debug.Log($"Object '{gameObject.name}' constrained from {currentPosition} to {constrainedPosition}");
            }

            wasConstrained = true;
            lastConstrainedPosition = constrainedPosition;
        }
        else if (wasConstrained && showDebugMessages)
        {
            // Only log once when returning to unconstrained movement
            Debug.Log($"Object '{gameObject.name}' is now moving freely within boundaries");
            wasConstrained = false;
        }
    }

    /// <summary>
    /// Returns true if the object is currently at a boundary edge
    /// </summary>
    public bool IsAtBoundary()
    {
        if (!hasValidReference || taskProgrammer == null)
            return false;

        Vector3 pos = transform.position;
        Vector3 boundaryMin = taskProgrammer.GetBoundaryMin();
        Vector3 boundaryMax = taskProgrammer.GetBoundaryMax();

        return Mathf.Approximately(pos.x, boundaryMin.x) || Mathf.Approximately(pos.x, boundaryMax.x) ||
               Mathf.Approximately(pos.y, boundaryMin.y) || Mathf.Approximately(pos.y, boundaryMax.y) ||
               Mathf.Approximately(pos.z, boundaryMin.z) || Mathf.Approximately(pos.z, boundaryMax.z);
    }

    /// <summary>
    /// Returns the distance to the nearest boundary
    /// </summary>
    public float DistanceToNearestBoundary()
    {
        if (!hasValidReference || taskProgrammer == null)
            return float.MaxValue;

        Vector3 pos = transform.position;
        Vector3 boundaryMin = taskProgrammer.GetBoundaryMin();
        Vector3 boundaryMax = taskProgrammer.GetBoundaryMax();
        
        float distX = Mathf.Min(Mathf.Abs(pos.x - boundaryMin.x), Mathf.Abs(pos.x - boundaryMax.x));
        float distY = Mathf.Min(Mathf.Abs(pos.y - boundaryMin.y), Mathf.Abs(pos.y - boundaryMax.y));
        float distZ = Mathf.Min(Mathf.Abs(pos.z - boundaryMin.z), Mathf.Abs(pos.z - boundaryMax.z));
        
        return Mathf.Min(distX, distY, distZ);
    }

#if UNITY_EDITOR // Only compile Gizmo code in the editor
    /// <summary>
    /// Draws a wireframe box in the Scene view to visualize the movement boundaries
    /// when the GameObject is selected.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Try to get TaskProgrammer reference if needed
        if (taskProgrammer == null)
        {
            if (Application.isPlaying && TaskProgrammer.Instance != null)
            {
                taskProgrammer = TaskProgrammer.Instance;
            }
            else
            {
                taskProgrammer = FindObjectOfType<TaskProgrammer>();
                if (taskProgrammer == null)
                    return; // Can't draw without boundaries
            }
        }

        // Get boundary values from TaskProgrammer
        Vector3 boundaryMin = taskProgrammer.GetBoundaryMin();
        Vector3 boundaryMax = taskProgrammer.GetBoundaryMax();

        // Ensure min is actually less than max for sensible drawing
        Vector3 actualMin = Vector3.Min(boundaryMin, boundaryMax);
        Vector3 actualMax = Vector3.Max(boundaryMin, boundaryMax);

        Vector3 center = (actualMin + actualMax) / 2f;
        Vector3 size = actualMax - actualMin;

        // Prevent drawing a zero-size box which causes errors/warnings
        if (size.x > 0.001f && size.y > 0.001f && size.z > 0.001f)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f); // Green, semi-transparent
            Gizmos.DrawWireCube(center, size);
        }
        else
        {
            // Optionally draw a small indicator at the origin if size is invalid
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.1f); // Indicate issue at object origin
            Debug.LogWarning("BoundaryConstraint boundary size is zero or negative on one or more axes. Gizmo cannot be drawn correctly.", this);
        }
    }
#endif // UNITY_EDITOR
}