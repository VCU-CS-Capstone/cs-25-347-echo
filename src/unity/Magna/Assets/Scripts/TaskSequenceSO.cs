using UnityEngine;
using System.Collections.Generic;

// Defines where this asset can be created in the Unity menu
[CreateAssetMenu(fileName = "NewTaskSequence", menuName = "Tasks/Task Sequence", order = 0)]
public class TaskSequenceSO : ScriptableObject
{
    [Header("Sequence Info")]
    [Tooltip("Optional description for this task sequence.")]
    [TextArea(3, 5)] // Make description field larger
    public string description = "A sequence of tasks.";

    [Header("Task List")]
    // Keep SerializeReference to handle MovementTask, GripperTask, etc.
    [SerializeReference]
    public List<BaseTask> tasks = new List<BaseTask>();

    // Note: We keep the task logic execution within TaskProgrammer,
    // this SO primarily acts as a data container.
}