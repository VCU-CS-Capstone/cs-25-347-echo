using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera[] cameras; // Array of camera objects representing different camera positions

    private int currentCameraIndex = 0; // Index of the current camera position

    // Start is called before the first frame update
    void Start()
    {
        // Activate the initial camera
        SwitchToCamera(currentCameraIndex);
    }

    // Update is called once per frame
    void Update()
    {
        // Example: Switch to the next camera when pressing spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Move to the next camera position
            currentCameraIndex = (currentCameraIndex + 1) % cameras.Length;
            SwitchToCamera(currentCameraIndex);
        }
    }

    private void SwitchToCamera(int index)
    {
        // Deactivate all cameras
        foreach (Camera cam in cameras)
        {
            cam.gameObject.SetActive(false);
        }

        // Activate the selected camera
        cameras[index].gameObject.SetActive(true);
    }
}