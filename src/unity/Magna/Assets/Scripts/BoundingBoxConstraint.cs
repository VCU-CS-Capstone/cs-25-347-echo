using UnityEngine;

/// <summary>
/// Constrains an object's position to stay within a defined bounding box.
/// Attach this script to the TCP (Tool Center Point) of the robot arm.
/// </summary>
public class BoundingBoxConstraint : MonoBehaviour
{
    [Header("Bounding Box Settings")]
    [Tooltip("Minimum position on X axis")]
    public float minX = -0.5f;
    
    [Tooltip("Maximum position on X axis")]
    public float maxX = 0.5f;
    
    [Tooltip("Minimum position on Y axis")]
    public float minY = 0f;
    
    [Tooltip("Maximum position on Y axis")]
    public float maxY = 1f;
    
    [Tooltip("Minimum position on Z axis")]
    public float minZ = -0.5f;
    
    [Tooltip("Maximum position on Z axis")]
    public float maxZ = 0.5f;
    
    [Header("Visualization")]
    [Tooltip("Whether to show the bounding box in the scene view")]
    public bool showBounds = true;
    
    [Tooltip("Color of the bounding box wireframe")]
    public Color boundsColor = new Color(1f, 0.5f, 0f, 0.5f); // Orange with 50% transparency
    
    [Header("Behavior")]
    [Tooltip("Whether to apply constraints in local or world space")]
    public bool useLocalSpace = false;
    
    [Tooltip("Whether to log when position is constrained")]
    public bool logConstraints = false;
    
    // Reference to the original position before constraints
    private Vector3 originalPosition;
    
    private void LateUpdate()
    {
        // Store the original position for logging
        originalPosition = useLocalSpace ? transform.localPosition : transform.position;
        
        // Apply constraints
        ApplyConstraints();
    }
    
    /// <summary>
    /// Applies position constraints to keep the object within bounds
    /// </summary>
    private void ApplyConstraints()
    {
        if (useLocalSpace)
        {
            // Apply constraints in local space
            Vector3 constrainedPosition = transform.localPosition;
            
            constrainedPosition.x = Mathf.Clamp(constrainedPosition.x, minX, maxX);
            constrainedPosition.y = Mathf.Clamp(constrainedPosition.y, minY, maxY);
            constrainedPosition.z = Mathf.Clamp(constrainedPosition.z, minZ, maxZ);
            
            // Only update if position changed
            if (constrainedPosition != transform.localPosition)
            {
                transform.localPosition = constrainedPosition;
                
                if (logConstraints)
                {
                    Debug.Log($"Position constrained from {originalPosition} to {constrainedPosition} (local space)");
                }
            }
        }
        else
        {
            // Apply constraints in world space
            Vector3 constrainedPosition = transform.position;
            
            constrainedPosition.x = Mathf.Clamp(constrainedPosition.x, minX, maxX);
            constrainedPosition.y = Mathf.Clamp(constrainedPosition.y, minY, maxY);
            constrainedPosition.z = Mathf.Clamp(constrainedPosition.z, minZ, maxZ);
            
            // Only update if position changed
            if (constrainedPosition != transform.position)
            {
                transform.position = constrainedPosition;
                
                if (logConstraints)
                {
                    Debug.Log($"Position constrained from {originalPosition} to {constrainedPosition} (world space)");
                }
            }
        }
    }
    
    /// <summary>
    /// Draws the bounding box in the scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showBounds)
            return;
        
        Gizmos.color = boundsColor;
        
        Vector3 center;
        Vector3 size;
        
        if (useLocalSpace)
        {
            // Calculate center and size in local space
            center = transform.parent != null 
                ? transform.parent.TransformPoint(new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, (minZ + maxZ) / 2f))
                : new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, (minZ + maxZ) / 2f);
                
            // For size, we need to account for rotation
            Vector3 min = transform.parent != null 
                ? transform.parent.TransformPoint(new Vector3(minX, minY, minZ))
                : new Vector3(minX, minY, minZ);
                
            Vector3 max = transform.parent != null 
                ? transform.parent.TransformPoint(new Vector3(maxX, maxY, maxZ))
                : new Vector3(maxX, maxY, maxZ);
                
            size = max - min;
        }
        else
        {
            // Calculate center and size in world space
            center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, (minZ + maxZ) / 2f);
            size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
        }
        
        // Draw wireframe cube
        Gizmos.DrawWireCube(center, size);
    }
    
    /// <summary>
    /// Checks if a position is within the defined bounds
    /// </summary>
    /// <param name="position">The position to check</param>
    /// <param name="useLocal">Whether to check in local or world space</param>
    /// <returns>True if the position is within bounds</returns>
    public bool IsWithinBounds(Vector3 position, bool useLocal = false)
    {
        if (useLocal)
        {
            // Convert to local space if needed
            if (!useLocalSpace)
            {
                position = transform.InverseTransformPoint(position);
            }
            
            return position.x >= minX && position.x <= maxX &&
                   position.y >= minY && position.y <= maxY &&
                   position.z >= minZ && position.z <= maxZ;
        }
        else
        {
            // Convert to world space if needed
            if (useLocalSpace)
            {
                position = transform.TransformPoint(position);
            }
            
            return position.x >= minX && position.x <= maxX &&
                   position.y >= minY && position.y <= maxY &&
                   position.z >= minZ && position.z <= maxZ;
        }
    }
    
    /// <summary>
    /// Sets the bounds of the constraint box
    /// </summary>
    public void SetBounds(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
    {
        this.minX = minX;
        this.maxX = maxX;
        this.minY = minY;
        this.maxY = maxY;
        this.minZ = minZ;
        this.maxZ = maxZ;
    }
}
