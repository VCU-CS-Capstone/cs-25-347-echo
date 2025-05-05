using UnityEngine;
using System.Collections;

/// <summary>
/// Example script demonstrating how to use the <see cref="GripperController"/>.
/// Provides public methods to trigger gripper actions and includes example keyboard controls.
/// </summary>
public class GripperAnimation : MonoBehaviour
{
    [Tooltip("Reference to the GripperController component.")]
    [SerializeField] private GripperController gripperController;

    /// <summary>
    /// Starts the example gripper demonstration sequence coroutine.
    /// </summary>
    public void DemonstrateGripper()
    {
        StartCoroutine(GripperDemonstration());
    }

    /// <summary>
    /// Coroutine wrapper to open the gripper using the referenced <see cref="GripperController"/>.
    /// Waits for the action to complete.
    /// </summary>
    /// <returns>IEnumerator for use in StartCoroutine.</returns>
    public IEnumerator OpenGripper()
    {
        if (gripperController == null) { Debug.LogError("GripperController reference missing!"); yield break; }
        yield return StartCoroutine(gripperController.OpenGripperAndWait());
    }

    /// <summary>
    /// Coroutine wrapper to close the gripper using the referenced <see cref="GripperController"/>.
    /// Waits for the action to complete.
    /// </summary>
    /// <returns>IEnumerator for use in StartCoroutine.</returns>
    public IEnumerator CloseGripper()
    {
        if (gripperController == null) { Debug.LogError("GripperController reference missing!"); yield break; }
        yield return StartCoroutine(gripperController.CloseGripperAndWait());
    }

    /// <summary>
    /// Coroutine wrapper to move the gripper to a specific width using the referenced <see cref="GripperController"/>.
    /// Waits for the action to complete.
    /// </summary>
    /// <param name="widthInMm">Target width in millimeters.</param>
    /// <returns>IEnumerator for use in StartCoroutine.</returns>
    public IEnumerator MoveGripperToPosition(float widthInMm)
    {
        if (gripperController == null) { Debug.LogError("GripperController reference missing!"); yield break; }
        // Convert mm to 1/10 mm (the unit used by the gripper)
        int widthIn10thMm = Mathf.RoundToInt(widthInMm * 10);
        yield return StartCoroutine(gripperController.MoveGripperAndWait(widthIn10thMm));
    }
    
    // Example of a complete gripper demonstration sequence
    private IEnumerator GripperDemonstration()
    {
        Debug.Log("Starting gripper demonstration");
        
        // Open the gripper fully and wait for completion
        yield return StartCoroutine(gripperController.OpenGripperAndWait());
        
        // Wait for 1 second
        yield return new WaitForSeconds(1.0f);
        
        // Close the gripper fully and wait for completion
        yield return StartCoroutine(gripperController.CloseGripperAndWait());
        
        // Wait for 1 second
        yield return new WaitForSeconds(1.0f);
        
        // Move the gripper to middle position (80mm = 800 in 1/10mm)
        yield return StartCoroutine(gripperController.MoveGripperAndWait(800));
        
        Debug.Log("Gripper demonstration completed");
    }
    
    // Example of how to use the gripper in response to input
    private void Update()
    {
        // Example: Press O to open the gripper
        if (Input.GetKeyDown(KeyCode.O))
        {
            StartCoroutine(OpenGripper());
        }
        
        // Example: Press C to close the gripper
        if (Input.GetKeyDown(KeyCode.C))
        {
            StartCoroutine(CloseGripper());
        }
        
        // Example: Press M to move the gripper to 50mm
        if (Input.GetKeyDown(KeyCode.M))
        {
            StartCoroutine(MoveGripperToPosition(50.0f));
        }
    }
}