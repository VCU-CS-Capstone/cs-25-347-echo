# Task Programmer

This script allows users to easily program a sequence of tasks for controlling a game object and gripper in Unity.

## Features

- Define a sequence of positions for a game object to move to
- Control gripper open/close actions at each position
- Set delays between actions
- Option to repeat the sequence
- Custom inspector for easy task management

## Setup

1. Create an empty GameObject in your scene
2. Add the `TaskProgrammer` component to it
3. Assign the target object (the object that will move)
4. Assign the GripperController reference

## Using the Task Programmer

### In the Inspector

The custom inspector provides an intuitive interface for managing tasks:

1. **References Section**
   - Set the target object (the object that will move)
   - Set the GripperController reference

2. **Execution Settings**
   - Execute on Start: Automatically start the sequence when the scene loads
   - Repeat Sequence: Loop the sequence indefinitely
   - Move Speed: How fast the object moves between positions
   - Position Threshold: How close the object needs to be to a position to consider it reached

3. **Task Sequence**
   - View, reorder, and remove existing tasks

4. **Add New Task**
   - Position: The target position for the object
   - Open Gripper: Whether to open the gripper at this position
   - Close Gripper: Whether to close the gripper at this position
   - Delay After: How long to wait after completing this task

5. **Execution Controls**
   - Execute Tasks: Start the sequence (only in Play mode)
   - Stop Execution: Stop the current sequence
   - Clear All Tasks: Remove all tasks from the sequence

### Via Script

You can also control the TaskProgrammer through scripts:

```csharp
// Get reference to the TaskProgrammer
TaskProgrammer taskProgrammer = GetComponent<TaskProgrammer>();

// Add a new task
taskProgrammer.AddTask(
    new Vector3(1, 2, 3),  // Position
    true,                  // Open gripper
    false,                 // Close gripper
    1.0f                   // Delay after task
);

// Execute the sequence
taskProgrammer.ExecuteTasks();

// Stop execution
taskProgrammer.StopExecution();

// Clear all tasks
taskProgrammer.ClearTasks();

// Set repeat option
taskProgrammer.SetRepeat(true);

// Check if tasks are executing
bool isRunning = taskProgrammer.IsExecuting();

// Get task count
int taskCount = taskProgrammer.GetTaskCount();
```

## Example Use Case

1. Add the TaskProgrammer component to an empty GameObject
2. Assign your robot arm or gripper object as the target
3. Assign the GripperController component
4. Add tasks to:
   - Move to a pickup position
   - Close the gripper to grab an object
   - Move to a placement position
   - Open the gripper to release the object
   - Return to a home position
5. Click "Execute Tasks" to run the sequence

## Notes

- The TaskProgrammer uses coroutines to execute tasks sequentially
- The gripper actions use the GripperController's OpenGripperAndWait and CloseGripperAndWait methods
- Tasks will execute in the order they are defined in the list
- You can reorder tasks using the up/down arrows in the inspector