using UnityEngine;

/// <summary>
/// Makes the GameObject this script is attached to smoothly follow a target Transform.
/// </summary>
public class SmoothFollow : MonoBehaviour
{
    /// <summary>
    /// The target Transform to follow.
    /// </summary>
    public Transform target;

    /// <summary>
    /// Approximate time for the follower to reach the target.
    /// A smaller value will make the follower move faster.
    /// </summary>
    [Tooltip("Approximate time for the follower to reach the target. A smaller value will make the follower move faster.")]
    public float smoothTime = 0.3f;

    // Private variable to store the current velocity, used by SmoothDamp
    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        // Ensure we have a target to follow
        if (target == null)
        {
            Debug.LogWarning("SmoothFollow script needs a target Transform assigned.", this);
            return;
        }

        // Calculate the desired position (same position as the target)
        Vector3 targetPosition = target.position;

        // Smoothly move the follower towards the target position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}