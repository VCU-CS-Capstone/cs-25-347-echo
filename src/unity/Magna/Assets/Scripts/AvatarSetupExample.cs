using UnityEngine;
using NuitrackSDK;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Example script that demonstrates how to use the AvatarSetupManager in a scene.
/// This script ensures Nuitrack is initialized before setting up the avatar.
/// </summary>
public class AvatarSetupExample : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the AvatarSetupManager component")]
    public AvatarSetupManager avatarSetupManager;
    
    [Tooltip("Reference to a 3D model prefab to use as the avatar")]
    public GameObject modelPrefab;
    
    [Header("Nuitrack Settings")]
    [Tooltip("Enable to automatically start Nuitrack when the scene loads")]
    public bool autoStartNuitrack = true;
    
    [Header("UI Elements (Optional)")]
    [Tooltip("Text element to display tracking status")]
    public TextMeshProUGUI statusText;
    
    [Tooltip("Button to create the avatar")]
    public Button createAvatarButton;
    
    private GameObject nuitrackScripts;
    private bool avatarCreated = false;
    
    void Start()
    {
        // Set up UI elements if they exist
        if (createAvatarButton != null)
        {
            createAvatarButton.onClick.AddListener(CreateAvatar);
        }
        
        if (autoStartNuitrack)
        {
            InitializeNuitrack();
        }
    }
    
    void InitializeNuitrack()
    {
        // Check if NuitrackScripts already exists in the scene
        nuitrackScripts = GameObject.Find("NuitrackScripts");
        
        if (nuitrackScripts == null)
        {
            // Create NuitrackScripts if it doesn't exist
            Debug.Log("Creating NuitrackScripts GameObject...");
            nuitrackScripts = new GameObject("NuitrackScripts");
            nuitrackScripts.AddComponent<NuitrackManager>();
        }
        
        // Make sure Nuitrack is initialized
        if (NuitrackManager.Instance != null)
        {
            Debug.Log("Initializing Nuitrack...");
            // The Nuitrack SDK automatically initializes when the NuitrackManager is created
            // No need to call any initialization method explicitly
        }
    }
    
    void Update()
    {
        // Update status text if it exists
        if (statusText != null)
        {
            if (NuitrackManager.Instance != null)
            {
                if (NuitrackManager.Users.Count > 0)
                {
                    statusText.text = $"Tracking {NuitrackManager.Users.Count} users";
                    
                    // Create avatar automatically when a user is detected
                    if (!avatarCreated && modelPrefab != null)
                    {
                        CreateAvatar();
                    }
                }
                else
                {
                    statusText.text = "No users detected. Stand in front of the sensor.";
                }
            }
            else
            {
                statusText.text = "Nuitrack not initialized";
            }
        }
    }
    
    /// <summary>
    /// Creates the avatar using the AvatarSetupManager
    /// </summary>
    public void CreateAvatar()
    {
        if (avatarCreated)
            return;
            
        // Create AvatarSetupManager if it doesn't exist
        if (avatarSetupManager == null)
        {
            GameObject avatarManagerObj = new GameObject("AvatarManager");
            avatarSetupManager = avatarManagerObj.AddComponent<AvatarSetupManager>();
            Debug.Log("Created AvatarSetupManager");
        }
        
        // Assign the model prefab
        if (modelPrefab != null)
        {
            avatarSetupManager.modelPrefab = modelPrefab;
        }
        else
        {
            Debug.LogError("No model prefab assigned! Please assign a 3D model prefab in the inspector.");
            return;
        }
        
        // Configure settings in the AvatarSetupManager
        // (These will be displayed as instructions for manual configuration in the NuitrackAvatar component)
        avatarSetupManager.smoothingFactor = 0.5f;
        avatarSetupManager.adjustBoneLength = true;
        avatarSetupManager.alignStraightLegs = true;
        avatarSetupManager.hideDistance = 1.5f;
        avatarSetupManager.showDebugLogs = true;
        
        Debug.Log("After the avatar is created, you'll need to manually configure these settings in the NuitrackAvatar component:");
        Debug.Log("- Smooth Move: 0.5");
        Debug.Log("- Alignment Bone Length: Enabled");
        Debug.Log("- Align Straight Legs: Enabled");
        Debug.Log("- Hide Distance: 1.5");
        
        // Call SetupAvatar method via SendMessage to ensure it runs
        avatarSetupManager.SendMessage("SetupAvatar");
        
        avatarCreated = true;
        Debug.Log("Avatar created! Now you need to manually assign the bones in the Inspector.");
        
        // Disable the create button if it exists
        if (createAvatarButton != null)
        {
            createAvatarButton.interactable = false;
        }
    }
    
    /// <summary>
    /// Example method showing how to manually assign a user ID to the avatar
    /// </summary>
    public void AssignUserToAvatar(int userId)
    {
        if (avatarSetupManager != null)
        {
            // Find the NuitrackAvatar component in the hierarchy
            NuitrackSDK.Avatar.NuitrackAvatar avatar = FindObjectOfType<NuitrackSDK.Avatar.NuitrackAvatar>();
            if (avatar != null)
            {
                // In your version of Nuitrack, the method to set the user ID might be different
                // You may need to check the Nuitrack documentation for the correct method
                // Common alternatives include:
                
                // Option 1: Using BaseAvatar.SetUserID method if it exists
                // avatar.SetUserID(userId);
                
                // Option 2: Using a property like UserID if it exists
                // avatar.UserID = userId;
                
                // For now, we'll just log that this functionality needs to be implemented
                Debug.Log($"To assign user ID {userId} to avatar, check the Nuitrack documentation for the correct method in your version");
            }
        }
    }
}
