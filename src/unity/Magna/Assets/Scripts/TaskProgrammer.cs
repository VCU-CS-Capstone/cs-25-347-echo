using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using communication; // Add namespace for UDPCOMM

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
    [SerializeField] [Tooltip("Distance threshold in meters. Speed is only adjusted when joints are closer than this")]
    private float jointDistanceThreshold = 1.5f;
    
    [Header("Potential Field Settings")]
    [SerializeField] private bool usePotentialField = true;
    [SerializeField] private float attractionStrength = 1.0f;
    [SerializeField] [Tooltip("Higher values create stronger attraction at longer distances")]
    private float attractionFalloff = 0.5f;
    [SerializeField] private float repulsionStrength = 2.0f;
    [SerializeField] [Tooltip("Higher values create stronger repulsion at closer distances")]
    private float repulsionFalloff = 2.0f;
    [SerializeField] [Tooltip("Maximum distance at which joints create repulsive forces")]
    private float repulsionRadius = 1.0f;
    
    [Header("Save Settings")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private string saveFileName = "task_sequence.json";
    
    private bool isExecuting = false;
    private bool isUdpConnected = false; // Flag to track if UDP connection is established
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
    private int previousTaskCount = 0; // Used to track when new tasks are added in the inspector
    
    /// <summary>
    /// Called in the editor when values are changed in the inspector
    /// Used to automatically set the position of newly added tasks
    /// </summary>
    private void OnValidate()
    {
        // Check if we're in the editor and not playing
        if (!Application.isPlaying)
        {
            // Check if tasks were added
            if (tasks.Count > previousTaskCount)
            {
                // If targetObject is null, use this GameObject
                GameObject positionSource = targetObject != null ? targetObject : gameObject;
                
                // Update all newly added tasks with the current position
                for (int i = previousTaskCount; i < tasks.Count; i++)
                {
                    tasks[i].targetPosition = positionSource.transform.position;
                    Debug.Log($"Auto-set position for task {i} to {positionSource.transform.position}");
                }
            }
            
            // Update the previous count
            previousTaskCount = tasks.Count;
        }
    }
    private void Start()
    {
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
        while (udpCommComponent == null)
        {
            udpCommComponent = FindObjectOfType<UDPCOMM>();
            yield return new WaitForSeconds(0.5f);
        }
        
        // Wait for a reasonable amount of time to ensure connection is established
        // UDPCOMM typically needs a few seconds to establish connection
        float waitTime = 0f;
        float maxWaitTime = 30f; // Maximum time to wait in seconds
        
        while (waitTime < maxWaitTime)
        {
            // Check if cube position has been updated, which indicates communication is happening
            if (udpCommComponent.cube != null && udpCommComponent.cube.transform.position != Vector3.zero)
            {
                Debug.Log("UDPCOMM connection detected. Robot position has been updated.");
                isUdpConnected = true;
                break;
            }
            
            waitTime += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        
        if (!isUdpConnected)
        {
            Debug.LogWarning("Timed out waiting for UDPCOMM connection. Proceeding anyway.");
            isUdpConnected = true; // Assume connection to avoid blocking indefinitely
        }
        
        Debug.Log("Ready to execute tasks.");
        
        // Execute tasks if configured to do so on start
        if (executeOnStart && tasks.Count > 0)
        {
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
        
        if (!isUdpConnected)
        {
            Debug.LogWarning("Cannot execute tasks: UDPCOMM connection not established yet. Tasks will execute once connection is established.");
            
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
                Debug.Log($"Executing task {i+1}/{tasks.Count}");
                
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
        // Move towards target position over time
        while (Vector3.Distance(targetObject.transform.position, targetPosition) > 0.001f)
        {
            // Calculate dynamic speed based on joint distances if enabled
            float currentSpeed = baseSpeed;
            
            if (useJointDistanceScaling && nativeAvatar != null)
            {
                currentSpeed = CalculateSpeedBasedOnJointDistances();
            }
            
            if (usePotentialField && nativeAvatar != null)
            {
                // Calculate movement direction using potential field
                Vector3 moveDirection = CalculatePotentialFieldForce(targetPosition);
                
                // Normalize and apply speed
                if (moveDirection.magnitude > 0.001f)
                {
                    moveDirection.Normalize();
                    targetObject.transform.position += moveDirection * currentSpeed * Time.deltaTime;
                }
            }
            else
            {
                // Traditional direct movement if potential field is disabled
                targetObject.transform.position = Vector3.MoveTowards(
                    targetObject.transform.position,
                    targetPosition,
                    currentSpeed * Time.deltaTime
                );
            }
            
            yield return null;
        }
        
        // Ensure exact position
        targetObject.transform.position = targetPosition;
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
        return isUdpConnected;
    }
    
    /// <summary>
    /// Saves the current position of the target object as a new task
    /// Can be called programmatically to add a task with the current position
    /// </summary>
    public void SaveCurrentPositionAsTask()
    {
        if (targetObject == null)
        {
            Debug.LogError("Cannot save position: Target object is missing!");
            return;
        }
        
        // Get current position of the target object
        Vector3 currentPosition = targetObject.transform.position;
        
        // Add a new task with the current position only (no gripper actions)
        AddTask(currentPosition, false, false);
        
        Debug.Log($"Saved current position as task: {currentPosition}");
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
}