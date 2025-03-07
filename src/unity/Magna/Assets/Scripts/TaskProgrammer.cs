using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

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
    
    [Header("UI References")]
    [SerializeField] private Button savePositionButton;
    [Tooltip("If true, the saved task will open the gripper")]
    [SerializeField] private bool saveWithOpenGripper = false;
    [Tooltip("If true, the saved task will close the gripper")]
    [SerializeField] private bool saveWithCloseGripper = false;
    [Tooltip("Delay in seconds after the saved task")]
    [SerializeField] private float saveTaskDelay = 0.5f;
    
    [Header("Task Sequence")]
    [SerializeField] private List<ProgrammedTask> tasks = new List<ProgrammedTask>();
    
    [Header("Execution Settings")]
    [SerializeField] private bool executeOnStart = false;
    [SerializeField] private bool repeatSequence = false;
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float positionThreshold = 0.01f;
    
    [Header("Save Settings")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private string saveFileName = "task_sequence.json";
    
    private bool isExecuting = false;
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
    
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
        
        // Set up save position button if assigned
        if (savePositionButton != null)
        {
            savePositionButton.onClick.AddListener(SaveCurrentPositionAsTask);
        }
        
        // Load saved tasks
        LoadTasks();
        
        // Execute on start if configured
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
        while (Vector3.Distance(targetObject.transform.position, targetPosition) > positionThreshold)
        {
            // Move towards target position
            targetObject.transform.position = Vector3.MoveTowards(
                targetObject.transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
            
            yield return null;
        }
        
        // Ensure exact position
        targetObject.transform.position = targetPosition;
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
    /// Saves the current position of the target object as a new task
    /// This method is intended to be called by a UI button
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
        
        // Add a new task with the current position
        AddTask(currentPosition, saveWithOpenGripper, saveWithCloseGripper, saveTaskDelay);
        
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