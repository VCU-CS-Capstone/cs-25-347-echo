using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the type of action a task represents.
/// </summary>
public enum TaskType
{
    /// <summary>A task involving robot arm movement to a target position.</summary>
    Movement,
    /// <summary>A task involving controlling the robot's gripper.</summary>
    Gripper
}

/// <summary>
/// Defines the specific action for a Gripper task.
/// </summary>
public enum GripperActionType
{
    /// <summary>Instructs the gripper to open.</summary>
    Open,
    /// <summary>Instructs the gripper to close.</summary>
    Close
}

/// <summary>
/// Base abstract class for all tasks within a sequence.
/// </summary>
[System.Serializable]
public abstract class BaseTask
{
    /// <summary>
    /// The type of this task (Movement or Gripper).
    /// </summary>
    public TaskType taskType;

    /// <summary>
    /// Delay in seconds to wait after this task completes before starting the next one.
    /// </summary>
    [Header("Timing")]
    [Tooltip("Delay in seconds after completing this task")]
    public float delayAfterAction = 0.5f;

    /// <summary>
    /// Base constructor for tasks.
    /// </summary>
    /// <param name="type">The type of the task.</param>
    protected BaseTask(TaskType type)
    {
        taskType = type;
    }
}

/// <summary>
/// Represents a task that moves the robot's target object to a specific position.
/// </summary>
[System.Serializable]
public class MovementTask : BaseTask
{
    /// <summary>
    /// The target position in world coordinates for the movement task.
    /// </summary>
    [Header("Movement")]
    public Vector3 targetPosition;

    /// <summary>
    /// Default constructor for MovementTask. Sets task type to Movement.
    /// </summary>
    public MovementTask() : base(TaskType.Movement) { }

    /// <summary>
    /// Creates a new MovementTask.
    /// </summary>
    /// <param name="position">The target world position.</param>
    /// <param name="delay">Delay in seconds after reaching the target.</param>
    public MovementTask(Vector3 position, float delay = 0.5f) : base(TaskType.Movement)
    {
        targetPosition = position;
        delayAfterAction = delay;
    }
}

/// <summary>
/// Represents a task that performs an action with the robot's gripper.
/// </summary>
[System.Serializable]
public class GripperTask : BaseTask
{
    /// <summary>
    /// The specific action (Open or Close) for the gripper.
    /// </summary>
    [Header("Gripper Action")]
    public GripperActionType actionType;

    /// <summary>
    /// Default constructor for GripperTask. Sets task type to Gripper.
    /// </summary>
    public GripperTask() : base(TaskType.Gripper) { }

    /// <summary>
    /// Creates a new GripperTask.
    /// </summary>
    /// <param name="action">The gripper action (Open or Close).</param>
    /// <param name="delay">Delay in seconds after the gripper action completes.</param>
    public GripperTask(GripperActionType action, float delay = 0.5f) : base(TaskType.Gripper)
    {
        actionType = action;
        delayAfterAction = delay;
    }
}

/// <summary>
/// Manages the execution of predefined task sequences for controlling the robot.
/// Handles movement, gripper actions, safety checks (skeleton detection), and communication status.
/// </summary>
public class TaskProgrammer : MonoBehaviour
{
    [System.Serializable]
    private class TaskSaveData
    {
        [SerializeReference]
        public List<BaseTask> tasks = new List<BaseTask>();
    }

    [Header("References")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private GripperController gripperController;
    [SerializeField] private NuitrackSDK.Tutorials.FirstProject.NativeAvatar nativeAvatar;
    [SerializeField] private UDPCOMM udpCommComponent;

    [Header("Task Sequence")]
    [SerializeField] private TaskSequenceSO activeTaskSequence;

    [Header("Execution Settings")]
    [SerializeField] private bool executeOnStart = false;
    [SerializeField] private bool repeatSequence = false;

    [Header("Movement Speed")]
    [SerializeField] private float movementSpeed = 2.0f;

    [Header("Movement Smoothing")]
    [SerializeField] private float movementSmoothTime = 0.3f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private float minDistanceToObstacle = 0.3f;
    [SerializeField] private float repulsionStrength = 10.0f;
    [SerializeField] private float attractionStrength = 1.0f;

    private const float connectionMaxWaitTime = 60f;

    [Header("Safety Settings")]
    [SerializeField] private Vector3 defaultPosition = Vector3.zero;
    // No longer using minimum joints as we're now checking for skeleton presence directly

    [Header("Connection Settings")]
    [SerializeField, Tooltip("Delay in seconds after connection is established before starting task execution")]
    private float startDelayAfterConnection = 0.5f;
    
    [Header("Testing Options")]
    [SerializeField, Tooltip("If enabled, bypasses robot connection checks for testing")]
    private bool testingMode = false;
    [SerializeField, Tooltip("Auto-simulate connection when in testing mode")]
    private bool autoSimulateConnection = true;

    private bool isExecuting = false;
    private bool isUdpConnected = false;
    private bool isAtDefaultPositionDueToLostSkeleton = false;

    /// <summary>Singleton instance of the TaskProgrammer.</summary>
    public static TaskProgrammer Instance { get; private set; }
    /// <summary>Gets the target GameObject controlled by this programmer.</summary>
    public GameObject GetTargetObject() => targetObject;
    /// <summary>Gets the currently active TaskSequenceSO asset.</summary>
    public TaskSequenceSO GetActiveSequence() => activeTaskSequence;

    /// <summary>
    /// (Unity) Called when the script instance is being loaded. Initializes the singleton instance.
    /// </summary>
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
            Debug.LogWarning("Multiple TaskProgrammer instances detected. Only using the first one.");
    }

    /// <summary>
    /// (Unity) Called before the first frame update. Validates references and starts the connection waiting coroutine.
    /// </summary>
    private void Start()
    {
        if (targetObject == null)
        {
            Debug.LogError("Target object reference is missing!");
            targetObject = gameObject;
        }

        if (gripperController == null)
        {
            Debug.LogError("GripperController reference is missing!");
            gripperController = FindObjectOfType<GripperController>();
            if (gripperController == null)
                Debug.LogError("Could not find GripperController in the scene!");
        }

        if (nativeAvatar == null)
            nativeAvatar = FindObjectOfType<NuitrackSDK.Tutorials.FirstProject.NativeAvatar>();

        if (udpCommComponent == null)
            udpCommComponent = FindObjectOfType<UDPCOMM>();

        // If in testing mode and we should auto-simulate connection
        if (testingMode && autoSimulateConnection)
        {
            Debug.Log("TaskProgrammer: Testing mode enabled - simulating connection");
            isUdpConnected = true;
            
            if (executeOnStart && activeTaskSequence != null && activeTaskSequence.tasks.Count > 0)
                ExecuteTasks();
        }
        else
        {
            // Normal initialization with real connection
            StartCoroutine(WaitForUdpConnection());
        }
    }

    private IEnumerator WaitForUdpConnection()
    {
        yield return new WaitForSeconds(1.0f);

        // If testing mode is enabled during the wait, exit and simulate connection
        if (testingMode)
        {
            Debug.Log("TaskProgrammer: Testing mode enabled - simulating connection");
            isUdpConnected = true;
            
            if (executeOnStart && activeTaskSequence != null && activeTaskSequence.tasks.Count > 0)
                ExecuteTasks();
                
            yield break;
        }

        float componentWait = 0f, componentMax = 10f;
        while (udpCommComponent == null && componentWait < componentMax)
        {
            udpCommComponent = FindObjectOfType<UDPCOMM>();
            componentWait += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        if (udpCommComponent == null)
        {
            Debug.LogWarning($"UDPCOMM not found after {componentMax} seconds. {(testingMode ? "Continuing in test mode" : "Aborting task execution")}.");
            if (testingMode)
            {
                isUdpConnected = true;
                if (executeOnStart && activeTaskSequence != null && activeTaskSequence.tasks.Count > 0)
                    ExecuteTasks();
            }
            yield break;
        }

        float connWait = 0f;
        while (connWait < connectionMaxWaitTime && !udpCommComponent.IsConnectionEstablished)
        {
            if (testingMode)
            {
                Debug.Log("TaskProgrammer: Testing mode enabled - simulating connection");
                break;
            }
            
            if (connWait % 5 < 0.5f)
                Debug.Log($"Waiting for EGM connection... ({connWait:F1}s)");
            connWait += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        
        if (!udpCommComponent.IsConnectionEstablished && !testingMode)
        {
            Debug.LogError("Connection timed out. Aborting task execution.");
            yield break;
        }

        isUdpConnected = true;
        if (startDelayAfterConnection > 0)
            yield return new WaitForSeconds(startDelayAfterConnection);

        if (executeOnStart && activeTaskSequence != null && activeTaskSequence.tasks.Count > 0)
            ExecuteTasks();
    }

    /// <summary>
    /// Starts the execution of the currently assigned <see cref="activeTaskSequence"/>.
    /// If not connected, it will attempt to connect first.
    /// </summary>
    public void ExecuteTasks()
    {
        if (activeTaskSequence == null || activeTaskSequence.tasks.Count == 0)
        {
            Debug.LogWarning("No active task sequence or tasks to execute!");
            return;
        }
        
        if (!isUdpConnected && !testingMode)
        {
            executeOnStart = true;
            StartCoroutine(WaitForUdpConnection());
            return;
        }
        
        if (!isExecuting)
        {
            isExecuting = true;
            StartCoroutine(ExecuteTaskSequenceCoroutine(activeTaskSequence));
        }
    }

    /// <summary>
    /// Stops the currently running task sequence execution.
    /// </summary>
    public void StopExecution()
    {
        StopAllCoroutines(); // Stops movement and sequence coroutines
        isExecuting = false;
        Debug.Log("Task execution stopped");
    }

    private IEnumerator ExecuteTaskSequenceCoroutine(TaskSequenceSO sequence)
    {
        Debug.Log($"Starting sequence '{sequence.name}'");
        var tasks = sequence.tasks;

        do
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                BaseTask task = tasks[i];
                Debug.Log($"Task {i + 1}/{tasks.Count}: {task.taskType}");

                switch (task.taskType)
                {
                    case TaskType.Movement:
                        yield return StartCoroutine(ExecuteMovementTaskWithMonitoring(task as MovementTask));
                        break;
                    case TaskType.Gripper:
                        yield return StartCoroutine(ExecuteGripperTaskWithSafety(task as GripperTask));
                        break;
                }

                if (task.delayAfterAction > 0)
                    yield return new WaitForSeconds(task.delayAfterAction);
            }

            Debug.Log($"Sequence '{sequence.name}' completed");
            if (repeatSequence)
                Debug.Log("Repeating sequence");
        }
        while (repeatSequence);

        isExecuting = false;
    }

    // --- Movement with continuous skeleton-monitoring & mid-move abort ---
    private IEnumerator MoveToPositionUntilSkeletonLoss(Vector3 targetPos)
    {
        Vector3 vel = Vector3.zero, velRef = Vector3.zero;

        if (Vector3.Distance(targetObject.transform.position, targetPos) < 0.01f)
        {
            targetObject.transform.position = targetPos;
            yield break;
        }

        while (Vector3.Distance(targetObject.transform.position, targetPos) > 0.01f)
        {
            if (!IsSkeletonDetected())
                yield break;  // abort if skeleton is lost

            Vector3 dir = (targetPos - targetObject.transform.position).normalized;
            Vector3 force = dir * attractionStrength + CalculateRepulsionForce();
            Vector3 rawVel = force * movementSpeed;
            vel = Vector3.SmoothDamp(vel, rawVel, ref velRef, movementSmoothTime);
            targetObject.transform.position += vel * Time.deltaTime;

            yield return null;
        }

        targetObject.transform.position = targetPos;
    }

    private IEnumerator ExecuteMovementTaskWithMonitoring(MovementTask moveTask)
    {
        bool done = false;
        while (!done)
        {
            // pre-check
            if (!IsSkeletonDetected())
            {
                if (!isAtDefaultPositionDueToLostSkeleton)
                {
                    Debug.LogWarning($"Skeleton lost before movement. Going to default {defaultPosition}");
                    yield return StartCoroutine(MoveToPosition(defaultPosition));
                    isAtDefaultPositionDueToLostSkeleton = true;
                }
                yield return new WaitUntil(IsSkeletonDetected);
                Debug.Log("Skeleton detected. Resuming movement.");
                isAtDefaultPositionDueToLostSkeleton = false;
                yield return new WaitForSeconds(0.2f);
            }

            // attempt move
            yield return StartCoroutine(MoveToPositionUntilSkeletonLoss(moveTask.targetPosition));

            if (Vector3.Distance(targetObject.transform.position, moveTask.targetPosition) < 0.01f)
            {
                done = true;
                Debug.Log($"Reached {moveTask.targetPosition}");
            }
            else
            {
                Debug.LogWarning("Movement interrupted. Retrying.");
            }
        }
    }

    // --- Gripper only pre-checks, no mid-action abort ---
    private IEnumerator ExecuteGripperTaskWithSafety(GripperTask gripTask)
    {
        // ensure skeleton is detected
        if (!IsSkeletonDetected())
        {
            if (!isAtDefaultPositionDueToLostSkeleton)
            {
                Debug.LogWarning($"Skeleton lost before gripper action. Going to default {defaultPosition}");
                yield return StartCoroutine(MoveToPosition(defaultPosition));
                isAtDefaultPositionDueToLostSkeleton = true;
            }
            yield return new WaitUntil(IsSkeletonDetected);
            Debug.Log("Skeleton detected. Performing gripper action.");
            isAtDefaultPositionDueToLostSkeleton = false;
            yield return new WaitForSeconds(0.2f);
        }

        // perform gripper (atomic)
        if (gripTask.actionType == GripperActionType.Open)
            yield return StartCoroutine(gripperController.OpenGripperAndWait());
        else
            yield return StartCoroutine(gripperController.CloseGripperAndWait());

        Debug.Log($"Gripper {gripTask.actionType} complete.");
    }

    private Vector3 CalculateRepulsionForce()
    {
        Vector3 total = Vector3.zero;
        if (nativeAvatar?.CreatedJoint == null) return total;

        foreach (var joint in nativeAvatar.CreatedJoint)
        {
            if (joint != null && joint.activeSelf)
            {
                float dist = Vector3.Distance(targetObject.transform.position, joint.transform.position);
                float range = minDistanceToObstacle * 2f;
                if (dist < range && dist > 0.001f)
                {
                    Vector3 dir = (targetObject.transform.position - joint.transform.position).normalized;
                    float mag = repulsionStrength * (1f - dist / range);
                    total += dir * mag;
                }
            }
        }
        return total;
    }

    // original MoveToPosition (used for default returns)
    private IEnumerator MoveToPosition(Vector3 targetPos)
    {
        Vector3 vel = Vector3.zero, velRef = Vector3.zero;

        if (Vector3.Distance(targetObject.transform.position, targetPos) < 0.001f)
        {
            targetObject.transform.position = targetPos;
            yield break;
        }

        while (Vector3.Distance(targetObject.transform.position, targetPos) > 0.01f)
        {
            Vector3 dir = (targetPos - targetObject.transform.position).normalized;
            Vector3 force = dir * attractionStrength + CalculateRepulsionForce();
            Vector3 rawVel = force * movementSpeed;
            vel = Vector3.SmoothDamp(vel, rawVel, ref velRef, movementSmoothTime);
            targetObject.transform.position += vel * Time.deltaTime;
            yield return null;
        }

        targetObject.transform.position = targetPos;
    }

    // Utility skeleton detection check
    private bool IsSkeletonDetected()
    {
        if (nativeAvatar == null)
        {
            Debug.LogWarning("NativeAvatar missing; assuming skeleton is OK.");
            return true;
        }
        
        // Check if skeleton is found using the same logic as in NativeAvatar.cs
        bool skeletonFound = NuitrackManager.sensorsData[NuitrackManager.sensorsData.Count > 0 ? 0 : 0].Users.Current != null &&
                           NuitrackManager.sensorsData[NuitrackManager.sensorsData.Count > 0 ? 0 : 0].Users.Current.Skeleton != null;
        
        return skeletonFound;
    }

    // Runtime task editing & utilities

    /// <summary>
    /// Adds a new MovementTask to the end of the active task sequence.
    /// </summary>
    /// <param name="position">The target world position for the movement.</param>
    /// <param name="delay">Delay in seconds after this task completes.</param>
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
    /// Adds a new GripperTask to the end of the active task sequence.
    /// </summary>
    /// <param name="action">The gripper action (Open or Close).</param>
    /// <param name="delay">Delay in seconds after this task completes.</param>
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
    /// Removes all tasks from the currently active task sequence.
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
        else Debug.LogWarning("Cannot clear tasks: No active task sequence assigned.");
    }

    /// <summary>
    /// Sets whether the active task sequence should repeat after completion.
    /// </summary>
    /// <param name="repeat">True to enable repeating, false otherwise.</param>
    public void SetRepeat(bool repeat)
    {
        repeatSequence = repeat;
        Debug.Log($"Repeat sequence set to: {repeat}");
    }

    /// <summary>Gets the number of tasks in the active sequence.</summary>
    /// <returns>The task count, or 0 if no sequence is active.</returns>
    public int GetTaskCount() => activeTaskSequence != null ? activeTaskSequence.tasks.Count : 0;

    /// <summary>Checks if a task sequence is currently being executed.</summary>
    /// <returns>True if executing, false otherwise.</returns>
    public bool IsExecuting() => isExecuting;

    /// <summary>Checks if the UDP connection to the robot is established and active.</summary>
    /// <returns>True if connected, false otherwise.</returns>
    public bool IsUdpConnected() => testingMode || (udpCommComponent != null && isUdpConnected && udpCommComponent.IsConnectionEstablished);

    /// <summary>
    /// Adds a new MovementTask to the sequence using the current position of the target object.
    /// </summary>
    public void SaveCurrentPositionAsTask()
    {
        if (targetObject == null || activeTaskSequence == null)
        {
            Debug.LogError("Cannot save position as task: missing references.");
            return;
        }
        var pos = targetObject.transform.position;
        activeTaskSequence.tasks.Add(new MovementTask(pos));
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(activeTaskSequence);
#endif
        Debug.Log($"Saved current position {pos} as Movement Task in {activeTaskSequence.name}");
    }

    /// <summary>
    /// Removes the last task added to the active task sequence.
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
            var idx = activeTaskSequence.tasks.Count - 1;
            var removed = activeTaskSequence.tasks[idx];
            activeTaskSequence.tasks.RemoveAt(idx);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(activeTaskSequence);
#endif
            Debug.Log($"Removed last task (Type: {removed.taskType}) from {activeTaskSequence.name}. Remaining: {activeTaskSequence.tasks.Count}");
        }
        else Debug.LogWarning($"Cannot remove task: Sequence '{activeTaskSequence.name}' is already empty.");
    }

    private void OnDrawGizmos()
    {
        if (targetObject != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetObject.transform.position, minDistanceToObstacle);
        }

        if (nativeAvatar != null && nativeAvatar.CreatedJoint != null)
        {
            Gizmos.color = Color.red;
            float influenceRange = minDistanceToObstacle * 2f;
            foreach (var joint in nativeAvatar.CreatedJoint)
            {
                if (joint != null && joint.activeSelf)
                    Gizmos.DrawWireSphere(joint.transform.position, influenceRange);
            }
        }
    }
}
