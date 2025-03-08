using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using nuitrack;

public class PortableActivation : EditorWindow
{
    string key = "";
    string mainMessage = "";
    bool canBeActivated = false;
    List<nuitrack.device.NuitrackDevice> devices = new List<nuitrack.device.NuitrackDevice>();
    string sensorNameAndSerial = string.Empty;

    static public void Init()
    {
        PortableActivation window = (PortableActivation)EditorWindow.GetWindow(typeof(PortableActivation));
        window.minSize = new Vector2(500, 300);
        window.maxSize = new Vector2(500, 300);
        window.Show();
    }

    void CheckSensorActivation()
    {
        canBeActivated = true;
        mainMessage = "";
        Nuitrack.Init();
        devices = Nuitrack.GetDeviceList();

        if (devices.Count == 0)
            mainMessage = "Connect sensor for activating";
        else
            sensorNameAndSerial = devices[0].GetInfo(nuitrack.device.DeviceInfoType.DEVICE_NAME) + " (" + devices[0].GetInfo(nuitrack.device.DeviceInfoType.SERIAL_NUMBER) + ")";

        if (devices.Count == 1)
        {
            mainMessage = "Connected " + sensorNameAndSerial + ". License: " + devices[0].GetActivationStatus();

            if (devices[0].GetActivationStatus() == nuitrack.device.ActivationStatus.PRO)
            {
                mainMessage = sensorNameAndSerial + " already Activated";
                canBeActivated = false;
            }
            else if (devices[0].GetActivationStatus() == nuitrack.device.ActivationStatus.TRIAL)
            {
                mainMessage = sensorNameAndSerial + " has been successfully activated. " +
                            "\nYou have a Trial license. Upgrade to PRO?";
            }
        }

        if (devices.Count > 1)
        {
            mainMessage = "Connected " + devices.Count + " sensors. Disconnect all sensors except the one you want to activate";
            canBeActivated = false;
        }

        Debug.Log(mainMessage);
        Nuitrack.Release();
    }

    void ActivateSensor()
    {
        if (key != "")
        {
            try
            {
                devices[0].Activate(key);
                CheckSensorActivation();
            }
            catch (System.Exception error)
            {
                mainMessage = error.Message;
            }
        }
        else
        {
            mainMessage = "Enter Activation Key";
        }
    }

    void OnGUI()
    {
        if (Application.isPlaying)
        {
            GUILayout.Label("Stop Playing for Nuitrack Activating", EditorStyles.boldLabel);
            return;
        }

        string buttonLabel = "Check Sensor";

        if (canBeActivated)
        {
            GUILayout.Label("Enter Activation Key", EditorStyles.boldLabel);
            key = EditorGUILayout.TextArea(key);
            if (devices.Count == 0)
            {
                canBeActivated = false;
            }
            else
            {
                if (devices[0].GetActivationStatus() == nuitrack.device.ActivationStatus.NONE)
                    buttonLabel = "Activate: " + sensorNameAndSerial;
                else
                    buttonLabel = "Upgrade to PRO license: " + sensorNameAndSerial;
            }
        }
        GUILayout.Label(mainMessage, EditorStyles.wordWrappedLabel);

        if (GUILayout.Button(buttonLabel))
        {
            if (canBeActivated)
                ActivateSensor();
            else
                CheckSensorActivation();
        }

        if (canBeActivated)
        {
            GUILayout.Label("If you don't have Activation Key, you can get it on Nuitrack.com", EditorStyles.boldLabel);
            if (GUILayout.Button("Open Nuitrack.com"))
                Application.OpenURL("https://nuitrack.com/");
        }
    }
}
