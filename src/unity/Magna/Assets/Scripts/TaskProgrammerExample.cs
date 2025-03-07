using UnityEngine;

/// <summary>
/// Example script demonstrating how to use the TaskProgrammer programmatically
/// </summary>
public class TaskProgrammerExample : MonoBehaviour
{
    [SerializeField] private TaskProgrammer taskProgrammer;
    [SerializeField] private KeyCode executeKey = KeyCode.Space;
    [SerializeField] private KeyCode stopKey = KeyCode.Escape;
    [SerializeField] private KeyCode toggleRepeatKey = KeyCode.R;
    
    // Example predefined task sequence
    [SerializeField] private Vector3[] examplePositions = new Vector3[]
    {
        new Vector3(0, 0, 0),      // Home position
        new Vector3(1, 0, 1),      // Pickup position
        new Vector3(1, 1, 1),      // Lifted position
        new Vector3(-1, 1, 1),     // Move to drop area
        new Vector3(-1, 0, 1),     // Drop position
        new Vector3(0, 0, 0)       // Return home
    };
    
    private void Start()
    {
        if (taskProgrammer == null)
        {
            Debug.LogError("TaskProgrammer reference is missing!");
            taskProgrammer = GetComponent<TaskProgrammer>();
            
            if (taskProgrammer == null)
            {
                Debug.LogError("Could not find TaskProgrammer component!");
                return;
            }
        }
        
        // Display controls in console
        Debug.Log($"TaskProgrammer Example Controls:");
        Debug.Log($"- Press {executeKey} to execute the example task sequence");
        Debug.Log($"- Press {stopKey} to stop execution");
        Debug.Log($"- Press {toggleRepeatKey} to toggle repeat mode");
        Debug.Log($"- Press 1 to load example pick-and-place sequence");
        Debug.Log($"- Press 2 to load example inspection sequence");
    }
    
    private void Update()
    {
        // Execute tasks
        if (Input.GetKeyDown(executeKey) && !taskProgrammer.IsExecuting())
        {
            Debug.Log("Executing task sequence");
            taskProgrammer.ExecuteTasks();
        }
        
        // Stop execution
        if (Input.GetKeyDown(stopKey) && taskProgrammer.IsExecuting())
        {
            Debug.Log("Stopping task execution");
            taskProgrammer.StopExecution();
        }
        
        // Toggle repeat
        if (Input.GetKeyDown(toggleRepeatKey))
        {
            bool currentRepeat = taskProgrammer.IsExecuting();
            taskProgrammer.SetRepeat(!currentRepeat);
            Debug.Log($"Repeat mode set to: {!currentRepeat}");
        }
        
        // Load example pick-and-place sequence
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            LoadPickAndPlaceSequence();
        }
        
        // Load example inspection sequence
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            LoadInspectionSequence();
        }
    }
    
    /// <summary>
    /// Loads an example pick-and-place task sequence
    /// </summary>
    public void LoadPickAndPlaceSequence()
    {
        Debug.Log("Loading pick-and-place sequence");
        
        // Clear existing tasks
        taskProgrammer.ClearTasks();
        
        // Move to pickup position
        taskProgrammer.AddTask(
            examplePositions[1],  // Pickup position
            false,                // Don't open gripper
            false,                // Don't close gripper
            0.5f                  // Wait 0.5 seconds
        );
        
        // Close gripper to grab object
        taskProgrammer.AddTask(
            examplePositions[1],  // Same position
            false,                // Don't open gripper
            true,                 // Close gripper
            1.0f                  // Wait 1 second for grip to secure
        );
        
        // Lift object
        taskProgrammer.AddTask(
            examplePositions[2],  // Lifted position
            false,                // Don't open gripper
            false,                // Don't close gripper
            0.5f                  // Wait 0.5 seconds
        );
        
        // Move to drop area
        taskProgrammer.AddTask(
            examplePositions[3],  // Move to drop area
            false,                // Don't open gripper
            false,                // Don't close gripper
            0.5f                  // Wait 0.5 seconds
        );
        
        // Lower to drop position
        taskProgrammer.AddTask(
            examplePositions[4],  // Drop position
            false,                // Don't open gripper
            false,                // Don't close gripper
            0.5f                  // Wait 0.5 seconds
        );
        
        // Open gripper to release object
        taskProgrammer.AddTask(
            examplePositions[4],  // Same position
            true,                 // Open gripper
            false,                // Don't close gripper
            1.0f                  // Wait 1 second for release
        );
        
        // Return to home position
        taskProgrammer.AddTask(
            examplePositions[0],  // Home position
            false,                // Don't open gripper
            false,                // Don't close gripper
            0.5f                  // Wait 0.5 seconds
        );
        
        Debug.Log($"Loaded {taskProgrammer.GetTaskCount()} tasks");
    }
    
    /// <summary>
    /// Loads an example inspection sequence
    /// </summary>
    public void LoadInspectionSequence()
    {
        Debug.Log("Loading inspection sequence");
        
        // Clear existing tasks
        taskProgrammer.ClearTasks();
        
        // Define inspection points in a circle
        Vector3 center = new Vector3(0, 1, 0);
        float radius = 1.5f;
        int points = 8;
        
        for (int i = 0; i < points; i++)
        {
            float angle = i * (360f / points) * Mathf.Deg2Rad;
            Vector3 position = center + new Vector3(
                Mathf.Sin(angle) * radius,
                0,
                Mathf.Cos(angle) * radius
            );
            
            // Add inspection point
            taskProgrammer.AddTask(
                position,
                false,  // Don't open gripper
                false,  // Don't close gripper
                1.0f    // Pause for 1 second at each point
            );
        }
        
        // Return to home position
        taskProgrammer.AddTask(
            examplePositions[0],  // Home position
            false,                // Don't open gripper
            false,                // Don't close gripper
            0.5f                  // Wait 0.5 seconds
        );
        
        Debug.Log($"Loaded {taskProgrammer.GetTaskCount()} tasks");
    }
}