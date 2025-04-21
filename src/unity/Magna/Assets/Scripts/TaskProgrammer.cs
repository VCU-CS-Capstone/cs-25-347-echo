using System.Collections;
using System.Collections.Generic;
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
    [SerializeField, Tooltip("Minimum number of joints that must be detected to continue execution.")]
    private int minRequiredJoints = 3;

    [Header("Connection Settings")]
    [SerializeField, Tooltip("Delay in seconds after connection is established before starting task execution")]
    private float startDelayAfterConnection = 0.5f;

    private bool isExecuting = false;
    private bool isUdpConnected = false;
    private bool isAtDefaultPositionDueToLostJoints = false;

    public static TaskProgrammer Instance { get; private set; }
    public GameObject GetTargetObject() => targetObject;
    public TaskSequenceSO GetActiveSequence() => activeTaskSequence;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
            Debug.LogWarning("Multiple TaskProgrammer instances detected. Only using the first one.");
    }

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

        StartCoroutine(WaitForUdpConnection());
    }

    private IEnumerator WaitForUdpConnection()
    {
        yield return new WaitForSeconds(1.0f);

        float componentWait = 0f, componentMax = 10f;
        while (udpCommComponent == null && componentWait < componentMax)
        {
            udpCommComponent = FindObjectOfType<UDPCOMM>();
            componentWait += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        if (udpCommComponent == null)
        {
            Debug.LogError($"UDPCOMM not found after {componentMax} seconds.");
            yield break;
        }

        float connWait = 0f;
        while (connWait < connectionMaxWaitTime && !udpCommComponent.IsConnectionEstablished)
        {
            if (connWait % 5 < 0.5f)
                Debug.Log($"Waiting for EGM connection... ({connWait:F1}s)");
            connWait += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        if (!udpCommComponent.IsConnectionEstablished)
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

    public void ExecuteTasks()
    {
        if (activeTaskSequence == null || activeTaskSequence.tasks.Count == 0)
        {
            Debug.LogWarning("No active task sequence or tasks to execute!");
            return;
        }
        if (!isUdpConnected)
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

    public void StopExecution()
    {
        StopAllCoroutines();
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

    // --- Movement with continuous joint-monitoring & mid-move abort ---
    private IEnumerator MoveToPositionUntilJointLoss(Vector3 targetPos)
    {
        Vector3 vel = Vector3.zero, velRef = Vector3.zero;

        if (Vector3.Distance(targetObject.transform.position, targetPos) < 0.01f)
        {
            targetObject.transform.position = targetPos;
            yield break;
        }

        while (Vector3.Distance(targetObject.transform.position, targetPos) > 0.01f)
        {
            if (!AreEnoughJointsDetected())
                yield break;  // abort if joints drop

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
            if (!AreEnoughJointsDetected())
            {
                if (!isAtDefaultPositionDueToLostJoints)
                {
                    Debug.LogWarning($"Joints lost before movement. Going to default {defaultPosition}");
                    yield return StartCoroutine(MoveToPosition(defaultPosition));
                    isAtDefaultPositionDueToLostJoints = true;
                }
                yield return new WaitUntil(AreEnoughJointsDetected);
                Debug.Log("Joints regained. Resuming movement.");
                isAtDefaultPositionDueToLostJoints = false;
                yield return new WaitForSeconds(0.2f);
            }

            // attempt move
            yield return StartCoroutine(MoveToPositionUntilJointLoss(moveTask.targetPosition));

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
        // ensure joints present
        if (!AreEnoughJointsDetected())
        {
            if (!isAtDefaultPositionDueToLostJoints)
            {
                Debug.LogWarning($"Joints lost before gripper action. Going to default {defaultPosition}");
                yield return StartCoroutine(MoveToPosition(defaultPosition));
                isAtDefaultPositionDueToLostJoints = true;
            }
            yield return new WaitUntil(AreEnoughJointsDetected);
            Debug.Log("Joints regained. Performing gripper action.");
            isAtDefaultPositionDueToLostJoints = false;
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

    // Utility joint checks
    private bool AreEnoughJointsDetected() => CountDetectedJoints() >= minRequiredJoints;

    private int CountDetectedJoints()
    {
        if (nativeAvatar == null)
        {
            Debug.LogWarning("NativeAvatar missing; assuming joints OK.");
            return minRequiredJoints;
        }
        if (nativeAvatar.CreatedJoint == null || nativeAvatar.CreatedJoint.Length == 0)
            return 0;

        int count = 0;
        foreach (var joint in nativeAvatar.CreatedJoint)
            if (joint != null && joint.activeSelf) count++;
        return count;
    }

    // Runtime task editing & utilities

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

    public void SetRepeat(bool repeat)
    {
        repeatSequence = repeat;
        Debug.Log($"Repeat sequence set to: {repeat}");
    }

    public int GetTaskCount() => activeTaskSequence != null ? activeTaskSequence.tasks.Count : 0;

    public bool IsExecuting() => isExecuting;

    public bool IsUdpConnected() => udpCommComponent != null && isUdpConnected && udpCommComponent.IsConnectionEstablished;

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
