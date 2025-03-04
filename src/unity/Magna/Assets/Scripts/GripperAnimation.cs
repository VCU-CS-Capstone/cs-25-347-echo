using UnityEngine;
using System.Collections;

/// <summary>
/// Example script demonstrating how to use the GripperController
/// </summary>
public class GripperAnimation : MonoBehaviour
{
    [SerializeField] private GripperController gripperController;
    
    // Example method to demonstrate gripper usage
    public void DemonstrateGripper()
    {
        StartCoroutine(GripperDemonstration());
    }
    
    // Example method that can be called from another script
    public IEnumerator OpenGripper()
    {
        yield return StartCoroutine(gripperController.OpenGripperAndWait());
    }
    
    // Example method that can be called from another script
    public IEnumerator CloseGripper()
    {
        yield return StartCoroutine(gripperController.CloseGripperAndWait());
    }
    
    // Example method that can be called from another script
    public IEnumerator MoveGripperToPosition(float widthInMm)
    {
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