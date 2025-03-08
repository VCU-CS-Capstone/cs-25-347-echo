using UnityEngine;
using System.IO;
using System;

public class NuitrackConfigHandler
{
    public static int ActiveUsers
    {
        get
        {
            try
            {
                return int.Parse(GetValue("Skeletonization.ActiveUsers"));
            }
            catch
            {
                Debug.LogError("Failed parse Skeletonization.ActiveUsers value");
                return 1;
            }
        }
        set
        {
            SetValue("Skeletonization.ActiveUsers", value.ToString());
        }
    }

    public static string FileRecord
    {
        get
        {
            string recordPath = GetValue("OpenNIModule.FileRecord");

            if (recordPath == "")
                recordPath = GetValue("Realsense2Module.FileRecord");

            return recordPath;
        }
        set
        {
            if (!(Application.platform == RuntimePlatform.WindowsPlayer || Application.isEditor))
            {
                Debug.Log("Playback of recordings is not supported on this platform");
                return;
            }

            string path = value.Replace('\\', '/');
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                if (fileInfo.Exists && fileInfo.Extension != string.Empty)
                {
                    if (fileInfo.Extension == ".oni")
                        SetValue("OpenNIModule.FileRecord", path);
                    else
                        SetValue("Realsense2Module.FileRecord", path);
                }
                else
                    Debug.LogError(string.Format("Check the path to the recording file! File path: {0}", path));
            }
            catch (Exception)
            {
                Debug.LogError("File " + path + "  Cannot be loaded!");
            }
        }
    }

    public static bool Depth2ColorRegistration
    {
        get
        {
            return bool.Parse(GetValue("DepthProvider.Depth2ColorRegistration"));
        }
        set
        {
            SetValue("DepthProvider.Depth2ColorRegistration", value.ToString().ToLower());
        }
    }

    public static bool Mirror
    {
        get
        {
            return bool.Parse(GetValue("DepthProvider.Mirror"));
        }
        set
        {
            SetValue("DepthProvider.Mirror", value.ToString().ToLower());
        }
    }

    public static NuitrackManager.RotationDegree RotateAngle
    {
        get
        {
            return (NuitrackManager.RotationDegree)int.Parse(GetValue("DepthProvider.RotateAngle"));
        }
        set
        {
            if (value != NuitrackManager.RotationDegree.Normal)
                Debug.LogWarning("Attention! Mirror doesn't work with enabled Rotate Angle");

            SetValue("DepthProvider.RotateAngle", ((int)value).ToString());
        }
    }

    public static bool FaceTracking
    {
        get
        {
            return bool.Parse(GetValue("Faces.ToUse"));
        }
        set
        {
            SetValue("Faces.ToUse", value.ToString().ToLower());
        }
    }

    public static NuitrackManager.WifiConnect WifiConnect
    {
        get
        {
            NuitrackManager.WifiConnect wifiType = NuitrackManager.WifiConnect.none;

            if (GetValue("Settings.IPAddress") == "192.168.43.1")
                wifiType = NuitrackManager.WifiConnect.TVico;

            if (GetValue("Settings.IPAddress") == "192.168.1.1")
                wifiType = NuitrackManager.WifiConnect.VicoVR;

            return wifiType;
        }
        set
        {
            string ip = "192.168.43.1";

            if (value == NuitrackManager.WifiConnect.VicoVR)
                ip = "192.168.1.1";

            SetValue("Settings.IPAddress", ip);
        }
    }

    public static bool NuitrackAI
    {
        get
        {
            return GetValue("Skeletonization.Type") == "CNN_HPE";
        }
        set
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.LinuxPlayer || Application.isEditor)
            {
                if (value)
                    SetValue("Skeletonization.Type", "CNN_HPE");
                else
                    SetValue("Skeletonization.Type", "RegressionSkeletonization");
            }
            else
            {
                Debug.LogWarning("NuitrackAI doesn't support this platform: " + Application.platform + ". https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/Nuitrack_AI.md");
            }
        }
    }

    public static bool ObjectDetection
    {
        get
        {
            return bool.Parse(GetValue("CnnDetectionModule.ToUse"));
        }
        set
        {
            SetValue("CnnDetectionModule.ToUse", value.ToString().ToLower());
        }
    }

    public static string GetValue(string key)
    {
        return nuitrack.Nuitrack.GetConfigValue(key);
    }

    public static void SetValue(string key, string value)
    {
        nuitrack.Nuitrack.SetConfigValue(key, value);
    }
}
