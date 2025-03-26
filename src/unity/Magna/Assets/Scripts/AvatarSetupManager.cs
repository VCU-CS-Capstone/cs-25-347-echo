using UnityEngine;
using NuitrackSDK.Avatar;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// This script creates a test scene for the 3D model avatar with Nuitrack skeleton.
/// </summary>
public class AvatarSetupManager : MonoBehaviour
{
    [Header("Model Settings")]
    [Tooltip("The 3D model prefab to use as the avatar")]
    public GameObject modelPrefab;
    
    [Header("Tracking Settings")]
    [Tooltip("Controls how smoothly the avatar follows movements (0-1 range)")]
    [Range(0, 1)]
    public float smoothingFactor = 0.5f;
    
    [Tooltip("Adjusts the size of model bones to match the user's skeleton proportions")]
    public bool adjustBoneLength = true;
    
    [Tooltip("Helps prevent leg deformation when legs are straight")]
    public bool alignStraightLegs = true;
    
    [Tooltip("Distance threshold at which the avatar is hidden when user moves too far")]
    public float hideDistance = 1.5f;
    
    [Header("Debug")]
    [Tooltip("Enable to show debug logs during bone mapping")]
    public bool showDebugLogs = true;
    
    private GameObject avatarRoot;
    private NuitrackAvatar nuitrackAvatar;
    
    // Dictionary to store bone mappings
    private Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();
    
    void Start()
    {
        SetupAvatar();
    }
    
    /// <summary>
    /// Creates and configures the avatar with the Nuitrack skeleton tracking
    /// </summary>
    void SetupAvatar()
    {
        if (modelPrefab == null)
        {
            Debug.LogError("Model prefab is not assigned! Please assign a 3D model prefab in the inspector.");
            return;
        }
        
        // Create a root GameObject for the avatar
        avatarRoot = new GameObject("AvatarRoot");
        
        // Add the NuitrackAvatar component
        nuitrackAvatar = avatarRoot.AddComponent<NuitrackAvatar>();
        
        // Instantiate your 3D model as a child of the avatar root
        GameObject modelInstance = Instantiate(modelPrefab, avatarRoot.transform);
        
        // Store the settings to be displayed in the instructions
        // (We can't set these directly as they're private fields in NuitrackAvatar)
        string settingsInstructions = "Settings to configure in the Inspector:\n" +
            $"- Smooth Move: {smoothingFactor}\n" +
            $"- Alignment Bone Length: {(adjustBoneLength ? "Enabled" : "Disabled")}\n" +
            $"- Align Straight Legs: {(alignStraightLegs ? "Enabled" : "Disabled")}\n" +
            $"- Hide Distance: {hideDistance}";
        
        // Find and store all bones from the model
        FindAndStoreBones(modelInstance.transform);
        
        if (showDebugLogs)
        {
            Debug.Log("Avatar setup completed. You need to manually assign the bones in the Inspector.");
            Debug.Log("1. Select the AvatarRoot GameObject");
            Debug.Log("2. In the Inspector, assign each bone from your model to the corresponding field in the NuitrackAvatar component");
            Debug.Log("3. Once all bones are assigned, the avatar will follow the Nuitrack skeleton when detected");
        }
    }
    
    /// <summary>
    /// Finds and stores all bones from the model for easy reference
    /// </summary>
    void FindAndStoreBones(Transform modelRoot)
    {
        if (showDebugLogs)
        {
            Debug.Log("Finding bones in model...");
        }
        
        // Common bone names to look for
        string[] bodyBones = { "Hips", "Pelvis", "Root", "Spine", "Spine1", "Chest", "Spine2", "Neck", "Head" };
        string[] leftArmBones = { "LeftShoulder", "Left_Shoulder", "L_Shoulder", "LeftArm", "Left_Arm", "L_UpperArm", "LeftForeArm", "Left_ForeArm", "L_Forearm" };
        string[] rightArmBones = { "RightShoulder", "Right_Shoulder", "R_Shoulder", "RightArm", "Right_Arm", "R_UpperArm", "RightForeArm", "Right_ForeArm", "R_Forearm" };
        string[] leftLegBones = { "LeftUpLeg", "Left_UpLeg", "L_Hip", "LeftLeg", "Left_Leg", "L_Knee", "LeftFoot", "Left_Foot", "L_Ankle" };
        string[] rightLegBones = { "RightUpLeg", "Right_UpLeg", "R_Hip", "RightLeg", "Right_Leg", "R_Knee", "RightFoot", "Right_Foot", "R_Ankle" };
        
        // Combine all bone names
        List<string> allBoneNames = new List<string>();
        allBoneNames.AddRange(bodyBones);
        allBoneNames.AddRange(leftArmBones);
        allBoneNames.AddRange(rightArmBones);
        allBoneNames.AddRange(leftLegBones);
        allBoneNames.AddRange(rightLegBones);
        
        // Find and store all bones
        foreach (string boneName in allBoneNames)
        {
            Transform bone = FindTransformByName(modelRoot, boneName);
            if (bone != null)
            {
                boneMap[boneName] = bone;
                if (showDebugLogs)
                {
                    Debug.Log($"Found bone: {boneName} at {bone.name}");
                }
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"Found {boneMap.Count} bones in the model");
        }
        
        // Print instructions for manual assignment
        Debug.Log("To complete the setup, you need to manually assign the bones in the Inspector:");
        Debug.Log("1. Select the AvatarRoot GameObject");
        Debug.Log("2. In the Inspector, assign each bone from your model to the corresponding field in the NuitrackAvatar component");
        Debug.Log("3. Here are the bones found in your model that you can use:");
        
        foreach (var bone in boneMap)
        {
            Debug.Log($"- {bone.Key}: {bone.Value.name}");
        }
    }
    
    /// <summary>
    /// Recursively searches for a transform with the given name
    /// </summary>
    Transform FindTransformByName(Transform root, string name)
    {
        // Check if this transform's name contains the search name
        if (root.name.Contains(name))
            return root;
        
        // Recursively search children
        foreach (Transform child in root)
        {
            Transform found = FindTransformByName(child, name);
            if (found != null)
                return found;
        }
        
        return null;
    }
}
