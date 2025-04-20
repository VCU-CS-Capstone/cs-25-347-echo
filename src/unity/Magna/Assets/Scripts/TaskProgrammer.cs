using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Enum to define the type of task
public enum TaskType
{
    Movement,
    Gripper
}

// Enum to define the specific gripper action
public enum GripperActionType
{
    Open,
    Close
}

// Base class for all tasks
[System.Serializable]
public abstract class BaseTask
{
    public TaskType taskType; // To identify the task type

    [Header("Timing")]
    [Tooltip("Delay in seconds after completing this task")]
    public float delayAfterAction = 0.5f;

    // Constructor to set the task type
    protected BaseTask(TaskType type)
    {
        taskType = type;
    }
}

// Derived class for movement tasks
[System.Serializable]
public class MovementTask : BaseTask
{
    [Header("Movement")]
    public Vector3 targetPosition;

    // Default constructor needed for serialization
    public MovementTask() : base(TaskType.Movement) { }

    // Constructor for creating movement tasks programmatically
    public MovementTask(Vector3 position, float delay = 0.5f) : base(TaskType.Movement)
    {
        targetPosition = position;
        delayAfterAction = delay;
    }
}

// Derived class for gripper tasks
[System.Serializable]
public class GripperTask : BaseTask
{
    [Header("Gripper Action")]
    public GripperActionType actionType;

    // Default constructor needed for serialization
    public GripperTask() : base(TaskType.Gripper) { }

    // Constructor for creating gripper tasks programmatically
    public GripperTask(GripperActionType action, float delay = 0.5f) : base(TaskType.Gripper)
    {
        actionType = action;
        delayAfterAction = delay;
    }
}


public class TaskProgrammer : MonoBehaviour
{
    [System.Serializable]
    private class TaskSaveData
    {
        [SerializeReference] // Required for serializing derived types
        public List<BaseTask> tasks = new List<BaseTask>();
    }

    [Header("References")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private GripperController gripperController;
    [SerializeField] private NuitrackSDK.Tutorials.FirstProject.NativeAvatar nativeAvatar;
    [SerializeField] private UDPCOMM udpCommComponent; // Reference to the UDPCOMM component

    [Header("Task Sequence")]
    [Tooltip("The Task Sequence asset currently assigned to this programmer.")]
    [SerializeField] private TaskSequenceSO activeTaskSequence;
    
    [Header("Execution Settings")]
    [SerializeField] private bool executeOnStart = false;
    [SerializeField] private bool repeatSequence = false;

    [Header("Movement Speed")]
    [SerializeField] private float movementSpeed = 2.0f; // Base speed, might be adjusted by forces

    [Header("Obstacle Avoidance")]
    private float minDistanceToObstacle = 0.3f; // Minimum distance to maintain from obstacles
    private float repulsionStrength = 10.0f;     // How strongly to push away from obstacles
    private float attractionStrength = 1.0f;    // How strongly to pull towards the target

    // Save Settings removed - Handled by ScriptableObject persistence

    [Header("Connection Settings")]
    private const float connectionMaxWaitTime = 60f; // Maximum time to wait for connection in seconds (fixed)
    [SerializeField]
    [Tooltip("Delay in seconds after connection is established before starting task execution")]
    private float startDelayAfterConnection = 2.0f; // Default 2-second delay after connection

    private bool isExecuting = false;
    private bool isUdpConnected = false; // Flag to track if UDP connection is established
    // SaveFilePath removed - Handled by ScriptableObject persistence
    
    // Public accessors
    public GameObject GetTargetObject() => targetObject;
    public TaskSequenceSO GetActiveSequence() => activeTaskSequence; // Getter for Editor
    
    // Static instance for easy access from other scripts
    public static TaskProgrammer Instance { get; private set; }
    private void Awake()
    {
        // Set up singleton instance
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple TaskProgrammer instances detected. Only using the first one.");
        }
    }

    private void Start()
    {
        Debug.Log("TaskProgrammer Start method called");

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

        if (nativeAvatar == null)
        {
            Debug.LogWarning("NativeAvatar reference is missing!");
            nativeAvatar = FindObjectOfType<NuitrackSDK.Tutorials.FirstProject.NativeAvatar>();
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

        // LoadTasks() call removed - Handled by ScriptableObject assignment

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

        // Execute tasks if configured to do so on start and a sequence is assigned
        if (executeOnStart && activeTaskSequence != null && activeTaskSequence.tasks.Count > 0)
        {
            Debug.Log($"Auto-executing tasks from sequence '{activeTaskSequence.name}' as configured...");
            ExecuteTasks();
        }
    }

    /// <summary>
    /// Executes the programmed task sequence
    /// </summary>
    public void ExecuteTasks()
    {
        // --- ADD NULL CHECK ---
        if (activeTaskSequence == null)
        {
            Debug.LogWarning("Cannot execute: No active task sequence assigned!");
            return;
        }
        // --- MODIFY LIST CHECK ---
        if (activeTaskSequence.tasks.Count == 0)
        {
            Debug.LogWarning($"No tasks in the active sequence '{activeTaskSequence.name}' to execute!");
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
            // --- Pass the sequence to the coroutine ---
            StartCoroutine(ExecuteTaskSequenceCoroutine(activeTaskSequence));
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
    /// Coroutine that executes the task sequence from the provided ScriptableObject
    /// </summary>
    // --- RENAME and ADD PARAMETER ---
    private IEnumerator ExecuteTaskSequenceCoroutine(TaskSequenceSO sequenceToExecute)
    {
        Debug.Log($"Starting execution of sequence: {sequenceToExecute.name}");
        List<BaseTask> tasksToRun = sequenceToExecute.tasks; // Get list from SO

        do
        {
            // --- Use tasksToRun ---
            for (int i = 0; i < tasksToRun.Count; i++)
            {
                BaseTask task = tasksToRun[i];
                Debug.Log($"Executing task {i + 1}/{tasksToRun.Count} (Type: {task.taskType}) from sequence {sequenceToExecute.name}");

                // Execute action based on task type
                switch (task.taskType)
                {
                    case TaskType.Movement:
                        MovementTask moveTask = task as MovementTask;
                        if (moveTask != null)
                        {
                            Debug.Log($"Moving to position: {moveTask.targetPosition}");
                            yield return StartCoroutine(MoveToPosition(moveTask.targetPosition));
                            Debug.Log($"Reached position: {moveTask.targetPosition}");
                        }
                        else
                        {
                            Debug.LogError($"Task {i + 1} is Movement type but failed to cast.");
                        }
                        break;

                    case TaskType.Gripper:
                        GripperTask gripperTask = task as GripperTask;
                        if (gripperTask != null)
                        {
                            if (gripperTask.actionType == GripperActionType.Open)
                            {
                                Debug.Log("Opening gripper");
                                yield return StartCoroutine(gripperController.OpenGripperAndWait());
                            }
                            else if (gripperTask.actionType == GripperActionType.Close)
                            {
                                Debug.Log("Closing gripper");
                                yield return StartCoroutine(gripperController.CloseGripperAndWait());
                            }
                        }
                        else
                        {
                            Debug.LogError($"Task {i + 1} is Gripper type but failed to cast.");
                        }
                        break;

                    default:
                        Debug.LogWarning($"Unknown task type encountered: {task.taskType}");
                        break;
                }

                // Wait for specified delay after the action
                if (task.delayAfterAction > 0)
                {
                    Debug.Log($"Waiting for {task.delayAfterAction} seconds");
                    yield return new WaitForSeconds(task.delayAfterAction);
                }
            }

            Debug.Log($"Sequence {sequenceToExecute.name} completed");

            if (repeatSequence)
            {
                Debug.Log($"Repeating task sequence: {sequenceToExecute.name}");
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

        // Move towards target position using force-based movement with obstacle avoidance
        while (Vector3.Distance(targetObject.transform.position, targetPosition) > 0.001f) // Use a slightly larger threshold for force-based movement
        {
            // 1. Calculate Attraction Force towards the target
            Vector3 attractionDirection = (targetPosition - targetObject.transform.position).normalized;
            Vector3 attractionForce = attractionDirection * attractionStrength;

            // 2. Calculate Repulsion Force from obstacles (active Nuitrack joints)
            Vector3 repulsionForce = CalculateRepulsionForce();

            // 3. Combine Forces
            // Simple addition for now, might need more sophisticated blending later
            Vector3 totalForce = attractionForce + repulsionForce;

            // 4. Apply Movement
            // Normalize the force to get direction, then apply speed.
            // Or, use the force magnitude directly if strengths are tuned.
            // Let's try using the force magnitude directly, scaled by speed and time.
            Vector3 velocity = totalForce; // Simplified velocity calculation
            targetObject.transform.position += velocity * movementSpeed * Time.deltaTime;

            // Optional: Clamp velocity or add damping if movement becomes unstable

            yield return null; // Wait for the next frame
        }
    }

    /// <summary>
    /// Calculates the combined repulsion force from nearby active obstacles (Nuitrack joints).
    /// </summary>
    private Vector3 CalculateRepulsionForce()
    {
        Vector3 totalRepulsion = Vector3.zero;

        if (nativeAvatar == null || nativeAvatar.CreatedJoint == null)
        {
            return totalRepulsion; // No avatar or joints to avoid
        }

        foreach (GameObject jointObject in nativeAvatar.CreatedJoint)
        {
            // Only consider active joints as obstacles
            if (jointObject != null && jointObject.activeSelf)
            {
                Transform obstacle = jointObject.transform;
                float distance = Vector3.Distance(targetObject.transform.position, obstacle.position);

                // Define an influence range (e.g., twice the minimum distance)
                float influenceRange = minDistanceToObstacle * 2.0f;

                // Only apply repulsion if within the influence range and not exactly at the same spot
                if (distance < influenceRange && distance > 0.001f)
                {
                    // Calculate repulsion vector (away from obstacle)
                    Vector3 repulsionDirection = (targetObject.transform.position - obstacle.position).normalized;

                    // Repulsion strength increases quadratically as we get closer (stronger push near obstacle)
                    // Inverse square falloff is common, but let's try linear falloff first for simplicity
                    // float repulsionMagnitude = repulsionStrength * (1.0f - (distance / influenceRange));
                    // Let's try inverse relationship: stronger when closer
                    float repulsionMagnitude = repulsionStrength * (influenceRange / distance - 1.0f); // Gets stronger as distance approaches 0
                    repulsionMagnitude = Mathf.Clamp(repulsionMagnitude, 0, repulsionStrength * 5); // Clamp max force


                    // Add to total repulsion
                    totalRepulsion += repulsionDirection * repulsionMagnitude;
                }
            }
        }

        // Optional: Clamp the total repulsion force magnitude if needed
        // if (totalRepulsion.magnitude > maxRepulsionForce)
        // {
        //     totalRepulsion = totalRepulsion.normalized * maxRepulsionForce;
        // }

        return totalRepulsion;
    }
    /// <summary>
    /// Adds a new Movement task to the sequence at runtime
    /// </summary>
    public void AddMovementTask(Vector3 position, float delay = 0.5f)
    {
        // --- ADD NULL CHECK ---
        if (activeTaskSequence == null)
        {
            Debug.LogError("Cannot add task: No active task sequence assigned.");
            return;
        }
        // --- Add to SO's list ---
        activeTaskSequence.tasks.Add(new MovementTask(position, delay));

        // --- Mark SO as dirty (important for saving changes made at runtime) ---
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(activeTaskSequence);
        #endif
        Debug.Log($"Added Movement Task to sequence: {activeTaskSequence.name}");
    }

    /// <summary>
    /// Adds a new Gripper task to the sequence at runtime
    /// </summary>
    public void AddGripperTask(GripperActionType action, float delay = 0.5f)
    {
        // --- ADD NULL CHECK ---
        if (activeTaskSequence == null)
        {
            Debug.LogError("Cannot add task: No active task sequence assigned.");
            return;
        }
        // --- Add to SO's list ---
        activeTaskSequence.tasks.Add(new GripperTask(action, delay));

        // --- Mark SO as dirty ---
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(activeTaskSequence);
        #endif
        Debug.Log($"Added Gripper Task to sequence: {activeTaskSequence.name}");
    }

    /// <summary>
    /// Update method to continuously save tasks at regular intervals
    /// </summary>
    // Update() method removed - Auto-save logic is no longer needed
    /// <summary>
    /// Clears all tasks from the sequence
    /// </summary>
    public void ClearTasks()
    {
        if (activeTaskSequence != null)
        {
            activeTaskSequence.tasks.Clear();
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(activeTaskSequence);
            #endif
            Debug.Log($"Cleared tasks from sequence: {activeTaskSequence.name}");
        }
        else
        {
            Debug.LogWarning("Cannot clear tasks: No active task sequence assigned.");
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
        return activeTaskSequence != null ? activeTaskSequence.tasks.Count : 0;
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
    /// Saves the current position of the target object as a new task in the active sequence.
    /// This method is intended to be called by a UI button or other runtime logic.
    /// </summary>
    public void SaveCurrentPositionAsTask()
    {
        if (targetObject == null)
        {
            Debug.LogError("Cannot save position: Target object is missing!");
            return;
        }
        // --- ADD NULL CHECK ---
        if (activeTaskSequence == null)
        {
            Debug.LogError("Cannot save position as task: No active task sequence assigned.");
            return;
        }

        Vector3 currentPosition = targetObject.transform.position;
        // --- Add directly to SO's list ---
        activeTaskSequence.tasks.Add(new MovementTask(currentPosition));

        // --- Mark SO as dirty ---
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(activeTaskSequence);
        #endif
        Debug.Log($"Saved current position {currentPosition} as a Movement Task in sequence {activeTaskSequence.name}");
    }

    // SaveTasks() method removed - Handled by ScriptableObject persistence
    // LoadTasks() method removed - Handled by ScriptableObject assignment
    /// <summary>
    /// Draws Gizmos in the Scene view for debugging obstacle avoidance ranges.
    /// </summary>
    private void OnDrawGizmos()
    {
        // --- Visualize Target Object's Minimum Distance ---
        if (targetObject != null)
        {
            Gizmos.color = Color.yellow; // Color for the target's safe zone
            Gizmos.DrawWireSphere(targetObject.transform.position, minDistanceToObstacle);
        }

        // --- Visualize Obstacles' Influence Range ---
        if (nativeAvatar != null && nativeAvatar.CreatedJoint != null)
        {
            Gizmos.color = Color.red; // Color for the obstacles' influence zone
            float influenceRange = minDistanceToObstacle * 2.0f;

            foreach (GameObject jointObject in nativeAvatar.CreatedJoint)
            {
                // Only draw for active joints
                if (jointObject != null && jointObject.activeSelf)
                {
                    Gizmos.DrawWireSphere(jointObject.transform.position, influenceRange);
                }
            }
        }
    }
}