using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO; // Required for Path and Directory operations

// Custom Editor for the TaskProgrammer component
/// <summary>
/// Custom Editor for the <see cref="TaskProgrammer"/> component.
/// Provides an enhanced Inspector experience for managing task sequences,
/// including editing the assigned <see cref="TaskSequenceSO"/> directly.
/// </summary>
[CustomEditor(typeof(TaskProgrammer))]
public class TaskProgrammerEditor : Editor
{
    // Serialized properties for the TaskProgrammer component itself
    private SerializedProperty activeSequenceProp; // Reference to the TaskSequenceSO asset

    // Serialized object and properties for the *referenced* TaskSequenceSO asset
    private SerializedObject activeSequenceSerializedObject; // Allows editing the SO asset
    private SerializedProperty sequenceTasksListProp;      // The 'tasks' list within the SO
    private SerializedProperty sequenceDescriptionProp;    // The 'description' field within the SO

    /// <summary>
    /// Called when the editor is enabled. Finds the necessary serialized properties.
    /// </summary>
    private void OnEnable()
    {
        // Find the property for the SO reference in TaskProgrammer
        activeSequenceProp = serializedObject.FindProperty("activeTaskSequence");
        // Initialize the editor state for the currently assigned SO (if any)
        UpdateActiveSequenceEditorState();
    }

    // Gets the SerializedObject and properties for the currently assigned SO
    /// <summary>
    /// Updates the internal state based on the currently assigned TaskSequenceSO asset.
    /// Creates a SerializedObject for the asset and finds its relevant properties ('tasks', 'description').
    /// </summary>
    private void UpdateActiveSequenceEditorState()
    {
        activeSequenceSerializedObject = null; // Reset
        sequenceTasksListProp = null;      // Reset
        sequenceDescriptionProp = null; // Reset description prop as well

        TaskSequenceSO currentSequence = activeSequenceProp.objectReferenceValue as TaskSequenceSO;
        if (currentSequence != null)
        {
            // Create a SerializedObject for the referenced ScriptableObject asset
            activeSequenceSerializedObject = new SerializedObject(currentSequence);
            // Find the 'tasks' property within that SerializedObject
            sequenceTasksListProp = activeSequenceSerializedObject.FindProperty("tasks");
            // Find the 'description' property
            sequenceDescriptionProp = activeSequenceSerializedObject.FindProperty("description");
        }
    }

    /// <summary>
    /// Draws the custom Inspector GUI for the TaskProgrammer component.
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Update the TaskProgrammer's serialized object representation
        serializedObject.Update();

        // Draw default fields EXCEPT the script and the sequence reference
        DrawPropertiesExcluding(serializedObject, "m_Script", "activeTaskSequence");

        EditorGUILayout.Space();

        // --- Draw the Active Task Sequence Field ---
        EditorGUI.BeginChangeCheck(); // Start checking if the SO assignment changes
        EditorGUILayout.PropertyField(activeSequenceProp, new GUIContent("Active Task Sequence"));
        bool sequenceChanged = EditorGUI.EndChangeCheck(); // End checking

        // Apply changes to TaskProgrammer (specifically the activeTaskSequence reference)
        // Do this *before* updating the editor state if the sequence changed
        serializedObject.ApplyModifiedProperties();

        // If the assigned sequence asset was changed, update our editor state
        if (sequenceChanged)
        {
            UpdateActiveSequenceEditorState();
        }

        EditorGUILayout.Space();

        // --- Button to Create New Sequence Asset ---
        if (GUILayout.Button("Create New Task Sequence Asset"))
        {
            CreateNewTaskSequenceAsset();
            // Note: Doesn't automatically assign the new asset, user needs to drag it
        }

        EditorGUILayout.Space();

        // --- Draw Task List from the Assigned SO (if any) ---
        if (activeSequenceSerializedObject != null && sequenceTasksListProp != null)
        {
            // Update the SO's representation
            activeSequenceSerializedObject.Update();

            // Draw the description field from the SO (if found)
            if (sequenceDescriptionProp != null)
            {
                 EditorGUILayout.PropertyField(sequenceDescriptionProp);
                 EditorGUILayout.Space();
            }
            else
            {
                // This might happen if the 'description' field name changes in TaskSequenceSO
                EditorGUILayout.HelpBox("Could not find 'description' field in TaskSequenceSO.", MessageType.Warning);
            }


            EditorGUILayout.LabelField("Tasks in " + activeSequenceProp.objectReferenceValue.name, EditorStyles.boldLabel);
            // Draw the 'tasks' list property from the SO
            EditorGUILayout.PropertyField(sequenceTasksListProp, true); // 'true' includes children

            // --- Add Task Buttons (only if a sequence is assigned) ---
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Movement Task"))
            {
                TaskProgrammer taskProgrammer = (TaskProgrammer)target; // Still need context
                GameObject targetObj = taskProgrammer.GetTargetObject();
                if (targetObj != null)
                {
                    Vector3 currentPosition = targetObj.transform.position;
                    // Add to the SO's list via its SerializedProperty
                    sequenceTasksListProp.arraySize++;
                    SerializedProperty newTaskProp = sequenceTasksListProp.GetArrayElementAtIndex(sequenceTasksListProp.arraySize - 1);
                    newTaskProp.managedReferenceValue = new MovementTask(currentPosition);
                    Debug.Log($"Added Movement Task to sequence {activeSequenceProp.objectReferenceValue.name}");
                }
                else { Debug.LogError("Target object is null. Cannot add movement task with position."); }
            }

            if (GUILayout.Button("Add Gripper Task"))
            {
                // Add to the SO's list
                sequenceTasksListProp.arraySize++;
                SerializedProperty newTaskProp = sequenceTasksListProp.GetArrayElementAtIndex(sequenceTasksListProp.arraySize - 1);
                newTaskProp.managedReferenceValue = new GripperTask(); // Uses default constructor
                Debug.Log($"Added Gripper Task to sequence {activeSequenceProp.objectReferenceValue.name}");
            }

            EditorGUILayout.EndHorizontal();


            // Apply changes *to the ScriptableObject*
            activeSequenceSerializedObject.ApplyModifiedProperties();
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a Task Sequence asset above to view and edit tasks.", MessageType.Info);
        }

        // No need for serializedObject.ApplyModifiedProperties(); at the very end anymore
        // as changes are applied specifically where needed.
    }

    // --- ADD HELPER METHOD ---
    /// <summary>
    /// Creates a new TaskSequenceSO asset in the project, typically within "Assets/TaskSequences".
    /// </summary>
    private void CreateNewTaskSequenceAsset()
    {
        TaskSequenceSO asset = ScriptableObject.CreateInstance<TaskSequenceSO>();

        // Define a recommended path and ensure the directory exists
        string relativeDirectory = "Assets/TaskSequences"; // Recommend creating this folder
        string fullDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), relativeDirectory);

        if (!Directory.Exists(fullDirectoryPath))
        {
            Directory.CreateDirectory(fullDirectoryPath);
            AssetDatabase.Refresh(); // Refresh AssetDatabase if directory was created
        }
        string path = AssetDatabase.GenerateUniqueAssetPath(relativeDirectory + "/NewTaskSequence.asset");

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets(); // Explicitly save the new asset
        AssetDatabase.Refresh();    // Refresh to ensure it shows up

        EditorUtility.FocusProjectWindow(); // Highlight the project window
        Selection.activeObject = asset; // Select the newly created asset

        Debug.Log($"Created new Task Sequence asset at: {path}. Assign it to the TaskProgrammer.");
    }
}