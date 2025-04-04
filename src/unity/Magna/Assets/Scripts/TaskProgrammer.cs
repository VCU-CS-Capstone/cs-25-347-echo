using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// TODO: Fix TaskProgrammer starting too soon
// TODO: Set boundary restrictions
// TODO: Redo task order implementation (gripper actions in relation to arm movement)

public class TaskProgrammer : MonoBehaviour
{
    [System.Serializable]
    public class ProgrammedTask
    {
        [Header("Position")]
        public Vector3 targetPosition;

        [Header("Gripper Actions")]
        public bool openGripper;
        public bool closeGripper;

        [Header("Timing")]
        [Tooltip("Delay in seconds after completing this task")]
        public float delayAfterAction = 0.5f;
    }

    [System.Serializable]
    private class TaskSaveData
    {
        public List<ProgrammedTask> tasks = new List<ProgrammedTask>();
    }

    [Header("References")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private GripperController gripperController;
    [SerializeField] private NuitrackSDK.Tutorials.FirstProject.NativeAvatar nativeAvatar;
    [SerializeField] private UDPCOMM udpCommComponent; // Reference to the UDPCOMM component

    [Header("UI References")]
    [SerializeField] private Button savePositionButton;

    [Header("Task Sequence")]
    [SerializeField] private List<ProgrammedTask> tasks = new List<ProgrammedTask>();

    [Header("Execution Settings")]
    [SerializeField] private bool executeOnStart = false;
    [SerializeField] private bool repeatSequence = false;
    [SerializeField] private float baseSpeed = 2.0f;
    [SerializeField] private float minSpeed = 0.5f;
    [SerializeField] private float maxSpeed = 5.0f;
    [SerializeField] private float distanceScalingFactor = 1.0f;
    [SerializeField] private bool useJointDistanceScaling = true;
    [SerializeField]
    [Tooltip("Distance threshold in meters. Speed is only adjusted when joints are closer than this")]
    private float jointDistanceThreshold = 1.5f;
    [SerializeField]
    [Tooltip("Speed profile curve (Time: 0=Start, 1=End; Value: Speed Multiplier)")]
    private AnimationCurve speedProfile = AnimationCurve.EaseInOut(0f, 0.1f, 1f, 0.1f); // Default: Start/end slow

    [Header("Potential Field Settings")]
    [SerializeField] private bool usePotentialField = true;
    [SerializeField] private float attractionStrength = 1.0f;
    [SerializeField]
    [Tooltip("Higher values create stronger attraction at longer distances")]
    private float attractionFalloff = 0.5f;
    [SerializeField] private float repulsionStrength = 2.0f;
    [SerializeField]
    [Tooltip("Higher values create stronger repulsion at closer distances")]
    private float repulsionFalloff = 2.0f;
    [SerializeField]
    [Tooltip("Maximum distance at which joints create repulsive forces")]
    private float repulsionRadius = 1.0f;

    [Header("Save Settings")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private string saveFileName = "task_sequence.json";

    [Header("Connection Settings")]
    [SerializeField] private float connectionMaxWaitTime = 60f; // Maximum time to wait for connection in seconds
    [SerializeField]
    [Tooltip("Delay in seconds after connection is established before starting task execution")]
    private float startDelayAfterConnection = 2.0f; // Default 2-second delay after connection
    [Header("Boundary Settings")]
    [Tooltip("Minimum boundary corner (world space)")]
    public Vector3 boundaryMin = new Vector3(-5f, 0f, -5f); // Example default
    [Tooltip("Maximum boundary corner (world space)")]
    public Vector3 boundaryMax = new Vector3(5f, 5f, 5f);   // Example default


    private bool isExecuting = false;
    private bool isUdpConnected = false; // Flag to track if UDP connection is established
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
    private void Start()
    {
        Debug.Log("TaskProgrammer Start method called");

        // Set up the save position button click event
        if (savePositionButton != null)
        {
            Debug.Log("Setting up savePositionButton click event");
            savePositionButton.onClick.AddListener(SaveCurrentPositionAsTask);
        }
        else
        {
            Debug.LogError("Save position button reference is missing! Button will not function.");
        }

        // Validate references
        if (targetObject == null)
        {
            Debug.LogError("Target object reference is missing!");
            targetObject = gameObject; // Default to self if missing
        }

        if (gripperController == null)
        {
            Debug.LogError("GripperController reference is missing!");
            gripperController = FindObjectOfType<GripperController>();

            if (gripperController == null)
            {
                Debug.LogError("Could not find GripperController in the scene!");
            }
        }

        if (nativeAvatar == null && useJointDistanceScaling)
        {
            Debug.LogWarning("NativeAvatar reference is missing but joint distance scaling is enabled!");
            nativeAvatar = FindObjectOfType<NuitrackSDK.Tutorials.FirstProject.NativeAvatar>();

            if (nativeAvatar == null)
            {
                Debug.LogWarning("Could not find NativeAvatar in the scene. Joint distance scaling will be disabled.");
                useJointDistanceScaling = false;
            }
        }

        // No need to check for a separate joint distance target since we're using targetObject for that purpose
        if (targetObject == null && useJointDistanceScaling)
        {
            Debug.LogWarning("Target object is missing but joint distance scaling is enabled!");
            useJointDistanceScaling = false;
        }

        // Find UDPCOMM component if not assigned
        if (udpCommComponent == null)
        {
            Debug.Log("UDPCOMM reference is missing. Attempting to find it in the scene.");
            udpCommComponent = FindObjectOfType<UDPCOMM>();

            if (udpCommComponent == null)
            {
                Debug.LogError("Could not find UDPCOMM in the scene! Tasks will not execute until UDPCOMM is available.");
            }
        }

        // Load saved tasks
        LoadTasks();

        // Start a coroutine to wait for UDPCOMM connection before executing tasks
        StartCoroutine(WaitForUdpConnection());
    }

    /// <summary>
    /// Coroutine that waits for the UDPCOMM connection to be established before executing tasks
    /// </summary>
    private IEnumerator WaitForUdpConnection()
    {
        Debug.Log("Waiting for UDPCOMM to establish connection...");

        // Initial delay to give UDPCOMM time to start
        yield return new WaitForSeconds(1.0f);

        // Wait until UDPCOMM component is available
        float componentWaitTime = 0f;
        float componentMaxWaitTime = 10f; // Max time to wait for component to be found

        while (udpCommComponent == null && componentWaitTime < componentMaxWaitTime)
        {
            udpCommComponent = FindObjectOfType<UDPCOMM>();
            componentWaitTime += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        if (udpCommComponent == null)
        {
            Debug.LogError("UDPCOMM component not found after " + componentMaxWaitTime + " seconds. Cannot execute tasks.");
            yield break; // Exit the coroutine
        }

        // Wait for the connection to be established
        float connectionWaitTime = 0f;
        bool connectionLoggedOnce = false;

        Debug.Log("Waiting for EGM connection...");

        while (connectionWaitTime < connectionMaxWaitTime)
        {
            // Check if connection is established
            if (udpCommComponent.IsConnectionEstablished)
            {
                Debug.Log("UDPCOMM connection established.");
                isUdpConnected = true;
                break;
            }

            // Log status periodically (only once every 5 seconds to avoid log spam)
            if (!connectionLoggedOnce || Mathf.FloorToInt(connectionWaitTime) % 5 == 0)
            {
                connectionLoggedOnce = true;
                Debug.Log($"Waiting for EGM connection... Status: Connection={udpCommComponent.IsConnectionEstablished}");
            }

            connectionWaitTime += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        if (!isUdpConnected)
        {
            Debug.LogWarning($"Timed out waiting for UDPCOMM connection after {connectionMaxWaitTime} seconds.");
            Debug.LogWarning($"Connection status: IsConnectionEstablished={udpCommComponent.IsConnectionEstablished}");

            // Do not proceed with execution if connection failed
            Debug.LogError("Task execution aborted due to communication failure with robot.");
            yield break; // Exit without executing tasks
        }

        // Add delay after connection is established before starting task execution
        if (startDelayAfterConnection > 0)
        {
            Debug.Log($"Connection established. Waiting for {startDelayAfterConnection} seconds before starting task execution...");
            yield return new WaitForSeconds(startDelayAfterConnection);
            Debug.Log("Delay completed. Ready to execute tasks.");
        }
        else
        {
            Debug.Log("Connection established. Ready to execute tasks immediately (no delay configured).");
        }

        // Execute tasks if configured to do so on start
        if (executeOnStart && tasks.Count > 0)
        {
            Debug.Log("Auto-executing tasks as configured...");
            ExecuteTasks();
        }
    }

    /// <summary>
    /// Executes the programmed task sequence
    /// </summary>
    public void ExecuteTasks()
    {
        if (tasks.Count == 0)
        {
            Debug.LogWarning("No tasks to execute!");
            return;
        }

        // Simplified connection checking
        if (udpCommComponent == null || !udpCommComponent.IsConnectionEstablished)
        {
            Debug.LogWarning("Cannot execute tasks: UDPCOMM connection not established.");
            Debug.LogWarning($"Connection status: {(udpCommComponent == null ? "UDPCOMM not found" : $"Connection={udpCommComponent.IsConnectionEstablished}")}");

            // Set executeOnStart to true so that tasks will execute once the connection is established
            executeOnStart = true;

            // Start the WaitForUdpConnection coroutine if it's not already running
            if (udpCommComponent == null)
            {
                udpCommComponent = FindObjectOfType<UDPCOMM>();
            }

            // Start the coroutine to wait for the connection
            StartCoroutine(WaitForUdpConnection());
            return;
        }

        if (!isExecuting)
        {
            isExecuting = true;
            StartCoroutine(ExecuteTaskSequence());
        }
        else
        {
            Debug.LogWarning("Task sequence is already executing!");
        }
    }

    /// <summary>
    /// Stops the current task execution
    /// </summary>
    public void StopExecution()
    {
        StopAllCoroutines();
        isExecuting = false;
        Debug.Log("Task execution stopped");
    }

    /// <summary>
    /// Coroutine that executes the task sequence
    /// </summary>
    private IEnumerator ExecuteTaskSequence()
    {
        Debug.Log("Starting task sequence execution");

        do
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                ProgrammedTask task = tasks[i];
                Debug.Log($"Executing task {i + 1}/{tasks.Count}");

                // Move to position
                yield return StartCoroutine(MoveToPosition(task.targetPosition));
                Debug.Log($"Reached position: {task.targetPosition}");

                // Handle gripper actions
                if (task.openGripper)
                {
                    Debug.Log("Opening gripper");
                    yield return StartCoroutine(gripperController.OpenGripperAndWait());
                }

                if (task.closeGripper)
                {
                    Debug.Log("Closing gripper");
                    yield return StartCoroutine(gripperController.CloseGripperAndWait());
                }

                // Wait for specified delay
                if (task.delayAfterAction > 0)
                {
                    Debug.Log($"Waiting for {task.delayAfterAction} seconds");
                    yield return new WaitForSeconds(task.delayAfterAction);
                }
            }

            Debug.Log("Task sequence completed");

            if (repeatSequence)
            {
                Debug.Log("Repeating task sequence");
            }

        } while (repeatSequence);

        isExecuting = false;
    }

    /// <summary>
    /// Coroutine that moves the target object to the specified position
    /// </summary>
    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        Vector3 startPosition = targetObject.transform.position;
        float totalDistance = Vector3.Distance(startPosition, targetPosition);

        // Handle very short movements to avoid division by zero or weird behavior
        if (totalDistance < 0.001f)
        {
            targetObject.transform.position = targetPosition;
            yield break; // Exit if already at the target
        }

        // Move towards target position over time
        while (Vector3.Distance(targetObject.transform.position, targetPosition) > 0.001f)
        {
            // Calculate base speed for this frame (considering joint distance scaling)
            float frameMaxSpeed = baseSpeed;
            if (useJointDistanceScaling && nativeAvatar != null)
            {
                // Joint distance scaling acts as an upper limit based on proximity
                frameMaxSpeed = CalculateSpeedBasedOnJointDistances();
            }

            // Calculate progress (0.0 to 1.0) along the path
            float remainingDistance = Vector3.Distance(targetObject.transform.position, targetPosition);
            float progress = Mathf.Clamp01(1.0f - (remainingDistance / totalDistance));

            // Evaluate the speed profile curve based on progress
            // The curve's value acts as a multiplier for the frameMaxSpeed
            float speedMultiplier = speedProfile.Evaluate(progress);

            // Calculate final speed for this frame
            float currentSpeed = frameMaxSpeed * speedMultiplier;

            // Clamp the final speed between min and max absolute limits
            currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

            // --- Calculate potential next position ---
            Vector3 potentialNextPosition;
            if (usePotentialField && nativeAvatar != null)
            {
                // Calculate movement direction using potential field
                Vector3 moveDirection = CalculatePotentialFieldForce(targetPosition);

                // Calculate position delta based on force and speed
                if (moveDirection.magnitude > 0.001f)
                {
                    potentialNextPosition = targetObject.transform.position + moveDirection.normalized * currentSpeed * Time.deltaTime;
                }
                else
                {
                    // If potential field force is zero, stay put for this frame relative to potential field logic
                    potentialNextPosition = targetObject.transform.position;
                }
            }
            else
            {
                // Traditional direct movement calculation
                potentialNextPosition = Vector3.MoveTowards(
                    targetObject.transform.position,
                    targetPosition,
                    currentSpeed * Time.deltaTime
                );
            }

            // --- Clamp the potential position within boundaries ---
            potentialNextPosition.x = Mathf.Clamp(potentialNextPosition.x, boundaryMin.x, boundaryMax.x);
            potentialNextPosition.y = Mathf.Clamp(potentialNextPosition.y, boundaryMin.y, boundaryMax.y);
            potentialNextPosition.z = Mathf.Clamp(potentialNextPosition.z, boundaryMin.z, boundaryMax.z);

            // --- Apply the clamped position ---
            targetObject.transform.position = potentialNextPosition;

            yield return null;
        }

        // Ensure exact position at the end, clamped to boundaries
        Vector3 finalClampedPosition = targetPosition;
        finalClampedPosition.x = Mathf.Clamp(finalClampedPosition.x, boundaryMin.x, boundaryMax.x);
        finalClampedPosition.y = Mathf.Clamp(finalClampedPosition.y, boundaryMin.y, boundaryMax.y);
        finalClampedPosition.z = Mathf.Clamp(finalClampedPosition.z, boundaryMin.z, boundaryMax.z);
        targetObject.transform.position = finalClampedPosition;
    }

    /// <summary>
    /// Calculates the combined force vector using the potential field method.
    /// Combines attractive force to target with repulsive forces from all joints.
    /// </summary>
    private Vector3 CalculatePotentialFieldForce(Vector3 targetPosition)
    {
        if (nativeAvatar == null || nativeAvatar.CreatedJoint == null)
        {
            // If no joints to avoid, just move directly to target
            return targetPosition - targetObject.transform.position;
        }

        // Calculate attractive force towards target
        Vector3 toTarget = targetPosition - targetObject.transform.position;
        float distanceToTarget = toTarget.magnitude;

        // Attractive force decreases with distance based on falloff parameter
        // Higher attractionFalloff means stronger attraction at longer distances
        float attractionMagnitude = attractionStrength;
        if (distanceToTarget > 0.001f)
        {
            attractionMagnitude = attractionStrength / Mathf.Pow(distanceToTarget, attractionFalloff);
        }

        Vector3 attractiveForce = toTarget.normalized * attractionMagnitude;

        // Calculate repulsive forces from all joints
        Vector3 repulsiveForce = Vector3.zero;

        foreach (GameObject joint in nativeAvatar.CreatedJoint)
        {
            if (joint != null && joint.activeSelf)
            {
                Vector3 toJoint = targetObject.transform.position - joint.transform.position;
                float distanceToJoint = toJoint.magnitude;

                // Only apply repulsion within the specified radius
                if (distanceToJoint < repulsionRadius)
                {
                    // Repulsive force increases as distance decreases
                    // Higher repulsionFalloff means stronger repulsion at closer distances
                    float repulsionMagnitude = repulsionStrength *
                        Mathf.Pow((repulsionRadius - distanceToJoint) / repulsionRadius, repulsionFalloff);

                    // Add repulsive force from this joint
                    repulsiveForce += toJoint.normalized * repulsionMagnitude;
                }
            }
        }

        // Combine forces
        Vector3 totalForce = attractiveForce + repulsiveForce;

        // Debug visualization
        Debug.DrawRay(targetObject.transform.position, attractiveForce, Color.green);
        Debug.DrawRay(targetObject.transform.position, repulsiveForce, Color.red);
        Debug.DrawRay(targetObject.transform.position, totalForce, Color.blue);

        return totalForce;
    }

    /// <summary>
    /// Calculates movement speed based on the distance of the closest joint to the target object.
    /// Only adjusts speed if the closest joint is within the distance threshold.
    /// Uses a non-linear curve for speed adjustment appropriate for collaborative robots.
    /// </summary>
    private float CalculateSpeedBasedOnJointDistances()
    {
        if (nativeAvatar == null || nativeAvatar.CreatedJoint == null)
        {
            return baseSpeed;
        }

        float minDistance = float.MaxValue;
        bool foundActiveJoint = false;

        // Find the closest active joint to the target object
        foreach (GameObject joint in nativeAvatar.CreatedJoint)
        {
            if (joint != null && joint.activeSelf)
            {
                float distance = Vector3.Distance(joint.transform.position, targetObject.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    foundActiveJoint = true;
                }
            }
        }

        // If no active joints, return base speed
        if (!foundActiveJoint)
        {
            return baseSpeed;
        }

        // If distance is greater than threshold, use base speed
        if (minDistance >= jointDistanceThreshold)
        {
            return baseSpeed;
        }

        // Calculate normalized distance (0 to 1) within the threshold
        float normalizedDistance = minDistance / jointDistanceThreshold;

        // Apply quadratic curve for more natural deceleration
        // This creates a curve that:
        // - Starts at baseSpeed when distance = threshold
        // - Decreases more rapidly as distance approaches zero
        // - Reaches minSpeed when distance = 0
        float speedRange = baseSpeed - minSpeed;
        float speedFactor = normalizedDistance * normalizedDistance; // Quadratic curve
        float scaledSpeed = minSpeed + (speedFactor * speedRange);

        // Apply additional scaling factor if needed
        if (distanceScalingFactor != 1.0f)
        {
            // Apply scaling factor while maintaining the curve shape
            float adjustedSpeed = baseSpeed - ((baseSpeed - scaledSpeed) * distanceScalingFactor);
            scaledSpeed = adjustedSpeed;
        }

        // Clamp speed between min and max values
        return Mathf.Clamp(scaledSpeed, minSpeed, maxSpeed);
    }

    /// <summary>
    /// Adds a new task to the sequence at runtime
    /// </summary>
    public void AddTask(Vector3 position, bool openGripper, bool closeGripper, float delay = 0.5f)
    {
        ProgrammedTask newTask = new ProgrammedTask
        {
            targetPosition = position,
            openGripper = openGripper,
            closeGripper = closeGripper,
            delayAfterAction = delay
        };

        tasks.Add(newTask);
        Debug.Log($"Added new task: Position={position}, Open={openGripper}, Close={closeGripper}, Delay={delay}");

        if (autoSave)
        {
            SaveTasks();
        }
    }

    /// <summary>
    /// Clears all tasks from the sequence
    /// </summary>
    public void ClearTasks()
    {
        tasks.Clear();
        Debug.Log("All tasks cleared");

        if (autoSave)
        {
            SaveTasks();
        }
    }

    /// <summary>
    /// Sets whether the sequence should repeat
    /// </summary>
    public void SetRepeat(bool repeat)
    {
        repeatSequence = repeat;
        Debug.Log($"Repeat sequence set to: {repeat}");
    }

    /// <summary>
    /// Gets the current task count
    /// </summary>
    public int GetTaskCount()
    {
        return tasks.Count;
    }

    /// <summary>
    /// Checks if tasks are currently executing
    /// </summary>
    public bool IsExecuting()
    {
        return isExecuting;
    }

    /// <summary>
    /// Checks if the UDPCOMM connection is established
    /// </summary>
    public bool IsUdpConnected()
    {
        if (udpCommComponent == null)
        {
            return false;
        }
        return isUdpConnected && udpCommComponent.IsConnectionEstablished;
    }

    /// <summary>
    /// Saves the current position of the target object as a new task
    /// This method is intended to be called by a UI button
    /// </summary>
    public void SaveCurrentPositionAsTask()
    {
        Debug.Log("SaveCurrentPositionAsTask method called");

        if (targetObject == null)
        {
            Debug.LogError("Cannot save position: Target object is missing!");
            return;
        }

        // Get current position of the target object
        Vector3 currentPosition = targetObject.transform.position;
        Debug.Log($"Current position retrieved: {currentPosition}");

        // Add a new task with the current position only (no gripper actions)
        AddTask(currentPosition, false, false);

        Debug.Log($"Saved current position as task: {currentPosition}");
        Debug.Log("SaveCurrentPositionAsTask method completed successfully");
    }

    /// <summary>
    /// Saves the current task list to a JSON file
    /// </summary>
    public void SaveTasks()
    {
        try
        {
            TaskSaveData saveData = new TaskSaveData
            {
                tasks = tasks
            };

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SaveFilePath, json);

            Debug.Log($"Tasks saved to: {SaveFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving tasks: {e.Message}");
        }
    }

    /// <summary>
    /// Loads tasks from a JSON file
    /// </summary>
    public void LoadTasks()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                TaskSaveData saveData = JsonUtility.FromJson<TaskSaveData>(json);

                if (saveData != null && saveData.tasks != null)
                {
                    tasks = saveData.tasks;
                    Debug.Log($"Loaded {tasks.Count} tasks from: {SaveFilePath}");
                }
            }
            else
            {
                Debug.Log("No saved tasks found. Starting with empty task list.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading tasks: {e.Message}");
        }
    }

#if UNITY_EDITOR // Only compile Gizmo code in the editor
    /// <summary>
    /// Draws a wireframe box in the Scene view to visualize the movement boundaries
    /// when the GameObject is selected.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Ensure min is actually less than max for sensible drawing
        // This handles cases where user might swap min/max values in inspector
        Vector3 actualMin = Vector3.Min(boundaryMin, boundaryMax);
        Vector3 actualMax = Vector3.Max(boundaryMin, boundaryMax);

        Vector3 center = (actualMin + actualMax) / 2f;
        Vector3 size = actualMax - actualMin;

        // Prevent drawing a zero-size box which causes errors/warnings
        if (size.x > 0.001f && size.y > 0.001f && size.z > 0.001f)
        {
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f); // Yellow, semi-transparent
            Gizmos.DrawWireCube(center, size);
        }
        else
        {
            // Optionally draw a small indicator at the origin if size is invalid
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.1f); // Indicate issue at object origin
            Debug.LogWarning("TaskProgrammer boundary size is zero or negative on one or more axes. Gizmo cannot be drawn correctly.", this);
        }
    }
#endif // UNITY_EDITOR
}
