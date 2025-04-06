using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Custom Editor for the TaskProgrammer component
[CustomEditor(typeof(TaskProgrammer))]
public class TaskProgrammerEditor : Editor
{
    private SerializedProperty tasksListProp;

    private void OnEnable()
    {
        // Find the serialized property for the tasks list
        tasksListProp = serializedObject.FindProperty("tasks");
    }

    public override void OnInspectorGUI()
    {
        // Update the serialized object's representation
        serializedObject.Update();

        // Draw all default fields except the tasks list
        DrawPropertiesExcluding(serializedObject, "m_Script", "tasks");

        EditorGUILayout.Space(); // Add some visual spacing

        // --- Custom Task List Drawing ---
        EditorGUILayout.LabelField("Task Sequence", EditorStyles.boldLabel);

        // Draw the list header and elements using the default PropertyField drawer
        // This handles array size changes, element reordering, etc.
        EditorGUILayout.PropertyField(tasksListProp, true); // 'true' includes children (the task properties)

        EditorGUILayout.Space(); // Add spacing before buttons

        // --- Add Task Buttons ---
        EditorGUILayout.BeginHorizontal(); // Arrange buttons side-by-side
        if (GUILayout.Button("Add Movement Task"))
        {
            // Get the target TaskProgrammer instance
            TaskProgrammer taskProgrammer = (TaskProgrammer)target;

            // Add a new MovementTask instance to the list with the current target object position
            // Get the current position of the target object
            GameObject targetObj = taskProgrammer.GetTargetObject();
            if (targetObj != null)
            {
                Vector3 currentPosition = targetObj.transform.position;
                
                // Create a new MovementTask with the current position
                tasksListProp.arraySize++; // Increase the list size
                SerializedProperty newTaskProp = tasksListProp.GetArrayElementAtIndex(tasksListProp.arraySize - 1);
                
                // Using managedReferenceValue is the correct way for [SerializeReference]
                newTaskProp.managedReferenceValue = new MovementTask(currentPosition);
                
                Debug.Log($"Added Movement Task with current position: {currentPosition}");
            }
            else
            {
                Debug.LogError("Target object is null. Cannot add movement task with position.");
            }

            Debug.Log("Added Movement Task via Custom Editor");
        }

        if (GUILayout.Button("Add Gripper Task"))
        {
            // Get the target TaskProgrammer instance
            TaskProgrammer taskProgrammer = (TaskProgrammer)target;

            // Add a new GripperTask instance to the list
            tasksListProp.arraySize++; // Increase the list size
            SerializedProperty newTaskProp = tasksListProp.GetArrayElementAtIndex(tasksListProp.arraySize - 1);
            // Assign a new GripperTask instance
            newTaskProp.managedReferenceValue = new GripperTask();

            Debug.Log("Added Gripper Task via Custom Editor");
        }
        EditorGUILayout.EndHorizontal();

        // Apply any changes made to the serialized object
        serializedObject.ApplyModifiedProperties();
    }
}