using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum TaskType
{
    Movement,
    Gripper
}

public enum GripperActionType
{
    Open,
    Close
}

[System.Serializable]
public abstract class BaseTask
{
    public TaskType taskType;

    [Header("Timing")]
    [Tooltip("Delay in seconds after completing this task")]
    public float delayAfterAction = 0.5f;

    protected BaseTask(TaskType type)
    {
        taskType = type;
    }
}

[System.Serializable]
public class MovementTask : BaseTask
{
    [Header("Movement")]
    public Vector3 targetPosition;

    public MovementTask() : base(TaskType.Movement) { }

    public MovementTask(Vector3 position, float delay = 0.5f) : base(TaskType.Movement)
    {
        targetPosition = position;
        delayAfterAction = delay;
    }
}

[System.Serializable]
public class GripperTask : BaseTask
{
    [Header("Gripper Action")]
    public GripperActionType actionType;

    public GripperTask() : base(TaskType.Gripper) { }

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
    [SerializeField] private float minDistanceToObstacle = 0.3f; // Minimum distance to maintain from obstacles
    [SerializeField] private float repulsionStrength = 10.0f;     // How strongly to push away from obstacles
    [SerializeField] private float attractionStrength = 1.0f;    // How strongly to pull towards the target

    private const float connectionMaxWaitTime = 60f; // Maximum time to wait for connection in seconds (fixed)

    [Header("Movement Settings")]
    [SerializeField] private float archHeight = 1.0f; // Controls the height of the parabolic arch

    [Header("Connection Settings")]
    [SerializeField]
    [Tooltip("Delay in seconds after connection is established before starting task execution")]
    private float startDelayAfterConnection = 2.0f; // Default 2-second delay after connection

    private bool isExecuting = false;
    private bool isUdpConnected = false;
    
    // Public accessors
    public GameObject GetTargetObject() => targetObject;
    public TaskSequenceSO GetActiveSequence() => activeTaskSequence;
    
    public static TaskProgrammer Instance { get; private set; }
    private void Awake()
    {
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

        if (udpCommComponent == null)
        {
            Debug.Log("UDPCOMM reference is missing. Attempting to find it in the scene.");
            udpCommComponent = FindObjectOfType<UDPCOMM>();

            if (udpCommComponent == null)
            {
                Debug.LogError("Could not find UDPCOMM in the scene! Tasks will not execute until UDPCOMM is available.");
            }
        }


        StartCoroutine(WaitForUdpConnection());
    }

    /// <summary>
    /// Coroutine that waits for the UDPCOMM connection to be established before executing tasks
    /// </summary>
    private IEnumerator WaitForUdpConnection()
    {
        Debug.Log("Waiting for UDPCOMM to establish connection...");

        yield return new WaitForSeconds(1.0f);

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
        if (activeTaskSequence == null)
        {
            Debug.LogWarning("Cannot execute: No active task sequence assigned!");
            return;
        }
        if (activeTaskSequence.tasks.Count == 0)
        {
            Debug.LogWarning($"No tasks in the active sequence '{activeTaskSequence.name}' to execute!");
            return;
        }

        if (udpCommComponent == null || !udpCommComponent.IsConnectionEstablished)
        {
            Debug.LogWarning("Cannot execute tasks: UDPCOMM connection not established.");
            Debug.LogWarning($"Connection status: {(udpCommComponent == null ? "UDPCOMM not found" : $"Connection={udpCommComponent.IsConnectionEstablished}")}");

            executeOnStart = true;

            if (udpCommComponent == null)
            {
                udpCommComponent = FindObjectOfType<UDPCOMM>();
            }

            StartCoroutine(WaitForUdpConnection());
            return;
        }

        if (!isExecuting)
        {
            isExecuting = true;
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
    private IEnumerator ExecuteTaskSequenceCoroutine(TaskSequenceSO sequenceToExecute)
    {
        Debug.Log($"Starting execution of sequence: {sequenceToExecute.name}");
        List<BaseTask> tasksToRun = sequenceToExecute.tasks;

        do
        {
            for (int i = 0; i < tasksToRun.Count; i++)
            {
                BaseTask task = tasksToRun[i];
                Debug.Log($"Executing task {i + 1}/{tasksToRun.Count} (Type: {task.taskType}) from sequence {sequenceToExecute.name}");

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

        if (totalDistance < 0.001f)
        {
            targetObject.transform.position = targetPosition;
            yield break;
        }

        while (Vector3.Distance(targetObject.transform.position, targetPosition) > 0.01f)
        {
            Vector3 attractionDirection = (targetPosition - targetObject.transform.position).normalized;
            Vector3 attractionForce = attractionDirection * attractionStrength;

            Vector3 repulsionForce = CalculateRepulsionForce();

            // 3. Combine Forces
            Vector3 totalForce = attractionForce + repulsionForce;

            // 4. Apply Movement
            Vector3 velocity = totalForce;
            targetObject.transform.position += velocity * movementSpeed * Time.deltaTime;

            yield return null; // Wait for the next frame
        }

        targetObject.transform.position = targetPosition;
    }

    /// <summary>
    /// Calculates the combined repulsion force from nearby active obstacles (Nuitrack joints).
    /// </summary>
    private Vector3 CalculateRepulsionForce()
    {
        Vector3 totalRepulsion = Vector3.zero;

        if (nativeAvatar == null || nativeAvatar.CreatedJoint == null)
        {
            return totalRepulsion;
        }

        foreach (GameObject jointObject in nativeAvatar.CreatedJoint)
        {
            if (jointObject != null && jointObject.activeSelf)
            {
                Transform obstacle = jointObject.transform;
                float distance = Vector3.Distance(targetObject.transform.position, obstacle.position);

                float influenceRange = minDistanceToObstacle * 2.0f;

                if (distance < influenceRange && distance > 0.001f)
                {
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


        return totalRepulsion;
    }
    /// <summary>
    /// Adds a new Movement task to the sequence at runtime
    /// </summary>
    public void AddMovementTask(Vector3 position, float delay = 0.5f)
    {
        if (activeTaskSequence == null)
        {
            Debug.LogError("Cannot add task: No active task sequence assigned.");
            return;
        }
        activeTaskSequence.tasks.Add(new MovementTask(position, delay));

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
        if (activeTaskSequence == null)
        {
            Debug.LogError("Cannot add task: No active task sequence assigned.");
            return;
        }
        activeTaskSequence.tasks.Add(new GripperTask(action, delay));

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(activeTaskSequence);
        #endif
        Debug.Log($"Added Gripper Task to sequence: {activeTaskSequence.name}");
    }

    /// <summary>
    /// Update method to continuously save tasks at regular intervals
    /// </summary>
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
        if (activeTaskSequence == null)
        {
            Debug.LogError("Cannot save position as task: No active task sequence assigned.");
            return;
        }

        Vector3 currentPosition = targetObject.transform.position;
        activeTaskSequence.tasks.Add(new MovementTask(currentPosition));

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(activeTaskSequence);
        #endif
        Debug.Log($"Saved current position {currentPosition} as a Movement Task in sequence {activeTaskSequence.name}");
    }

    /// <summary>
    /// Removes the last task added to the active sequence.
    /// </summary>
    public void RemoveLastTask()
    {
        if (activeTaskSequence == null)
        {
            Debug.LogError("Cannot remove task: No active task sequence assigned.");
            return;
        }

        if (activeTaskSequence.tasks.Count > 0)
        {
            int lastIndex = activeTaskSequence.tasks.Count - 1;
            BaseTask removedTask = activeTaskSequence.tasks[lastIndex];
            activeTaskSequence.tasks.RemoveAt(lastIndex);

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(activeTaskSequence);
            #endif
            Debug.Log($"Removed last task (Type: {removedTask.taskType}) from sequence {activeTaskSequence.name}. Remaining tasks: {activeTaskSequence.tasks.Count}");
        }
        else
        {
            Debug.LogWarning($"Cannot remove task: Sequence '{activeTaskSequence.name}' is already empty.");
        }
    }
 
    /// <summary>
    /// Draws Gizmos in the Scene view for debugging obstacle avoidance ranges.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (targetObject != null)
        {
            Gizmos.color = Color.yellow; // Color for the target's safe zone
            Gizmos.DrawWireSphere(targetObject.transform.position, minDistanceToObstacle);
        }

        if (nativeAvatar != null && nativeAvatar.CreatedJoint != null)
        {
            Gizmos.color = Color.red; // Color for the obstacles' influence zone
            float influenceRange = minDistanceToObstacle * 2.0f;

            foreach (GameObject jointObject in nativeAvatar.CreatedJoint)
            {
                if (jointObject != null && jointObject.activeSelf)
                {
                    Gizmos.DrawWireSphere(jointObject.transform.position, influenceRange);
                }
            }
        }
    }
}