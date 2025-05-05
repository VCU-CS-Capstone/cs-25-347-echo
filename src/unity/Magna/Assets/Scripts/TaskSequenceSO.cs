using UnityEngine;
using System.Collections.Generic;

// Defines where this asset can be created in the Unity menu
[CreateAssetMenu(fileName = "NewTaskSequence", menuName = "Tasks/Task Sequence", order = 0)]
/// <summary>
/// A ScriptableObject that holds a list of tasks (<see cref="BaseTask"/>) defining a sequence of actions for the robot.
/// This asset acts primarily as a data container, with execution logic handled by <see cref="TaskProgrammer"/>.
/// </summary>
public class TaskSequenceSO : ScriptableObject
{
    /// <summary>
    /// An optional description for this task sequence, visible in the Inspector.
    /// </summary>
    [Header("Sequence Info")]
    [Tooltip("Optional description for this task sequence.")]
    [TextArea(3, 5)] // Make description field larger
    public string description = "A sequence of tasks.";

    /// <summary>
    /// The list of tasks (<see cref="MovementTask"/>, <see cref="GripperTask"/>, etc.) that make up this sequence.
    /// Uses [SerializeReference] to allow storing derived task types.
    /// </summary>
    [Header("Task List")]
    // Keep SerializeReference to handle MovementTask, GripperTask, etc.
    [SerializeReference]
    public List<BaseTask> tasks = new List<BaseTask>();

    // Note: We keep the task logic execution within TaskProgrammer,
    // this SO primarily acts as a data container.
}