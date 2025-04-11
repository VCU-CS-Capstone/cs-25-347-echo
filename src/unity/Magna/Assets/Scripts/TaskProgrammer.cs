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
    [SerializeReference] // Required for polymorphism in Inspector/Serialization
    [SerializeField] private List<BaseTask> tasks = new List<BaseTask>();
    
    [Header("Execution Settings")]
    [SerializeField] private bool executeOnStart = false;
    [SerializeField] private bool repeatSequence = false;

    [Header("Movement Speed")]
    [SerializeField] private float movementSpeed = 2.0f; // Base speed, might be adjusted by forces

    [Header("Obstacle Avoidance")]
    private float minDistanceToObstacle = 0.3f; // Minimum distance to maintain from obstacles
    private float repulsionStrength = 5.0f;     // How strongly to push away from obstacles
    private float attractionStrength = 1.0f;    // How strongly to pull towards the target

    [Header("Save Settings")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private string saveFileName = "task_sequence.json";
    [SerializeField] private float saveInterval = 0.5f; // How often to save (in seconds)
    private float saveTimer = 0f;

    [Header("Connection Settings")]
    private const float connectionMaxWaitTime = 60f; // Maximum time to wait for connection in seconds (fixed)
    [SerializeField]
    [Tooltip("Delay in seconds after connection is established before starting task execution")]
    private float startDelayAfterConnection = 2.0f; // Default 2-second delay after connection

    private bool isExecuting = false;
    private bool isUdpConnected = false; // Flag to track if UDP connection is established
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
    
    // Public accessors
    public GameObject GetTargetObject() => targetObject;
    
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
                BaseTask task = tasks[i];
                Debug.Log($"Executing task {i + 1}/{tasks.Count} (Type: {task.taskType})");

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

        // Move towards target position using force-based movement with obstacle avoidance
        while (Vector3.Distance(targetObject.transform.position, targetPosition) > 0.1f) // Use a slightly larger threshold for force-based movement
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

        // Optional: Snap to the exact target position at the end if needed,
        // but force-based movement might naturally settle close enough.
        // targetObject.transform.position = targetPosition;
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
        MovementTask newTask = new MovementTask(position, delay);
        tasks.Add(newTask);
        Debug.Log($"Added new Movement Task: Position={position}, Delay={delay}");
    }

    /// <summary>
    /// Adds a new Gripper task to the sequence at runtime
    /// </summary>
    public void AddGripperTask(GripperActionType action, float delay = 0.5f)
    {
        GripperTask newTask = new GripperTask(action, delay);
        tasks.Add(newTask);
        Debug.Log($"Added new Gripper Task: Action={action}, Delay={delay}");
    }

    /// <summary>
    /// Update method to continuously save tasks at regular intervals
    /// </summary>
    private void Update()
    {
        // Only save if autoSave is enabled
        if (autoSave)
        {
            // Save at the specified interval
            saveTimer += Time.deltaTime;
            if (saveTimer >= saveInterval)
            {
                saveTimer = 0f;
                SaveTasks();
            }
        }
    }

    /// <summary>
    /// Clears all tasks from the sequence
    /// </summary>
    public void ClearTasks()
    {
        tasks.Clear();
        Debug.Log("All tasks cleared");
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
        
        // Add a new MovementTask with the current position
        AddMovementTask(currentPosition); // Use the specific method for movement tasks
        
        Debug.Log($"Saved current position as Movement Task: {currentPosition}");
        Debug.Log("SaveCurrentPositionAsTask method completed successfully");
    }

    /// <summary>
    /// Saves the current task list to a JSON file
    /// </summary>
    public void SaveTasks()
    {
        Debug.Log("SaveTasks method called");
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