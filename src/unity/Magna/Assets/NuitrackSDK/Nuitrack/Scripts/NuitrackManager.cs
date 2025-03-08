using NuitrackSDK;
using NuitrackSDK.ErrorSolver;
using NuitrackSDK.Loader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using nuitrack;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public enum MultisensorType
{
    Singlesensor,
    Multisensor,
}

[Serializable]
public class InitEvent : UnityEvent<NuitrackInitState> {}

[HelpURL("https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/")]
public class NuitrackManager : MonoBehaviour
{
    public enum WifiConnect
    {
        none, VicoVR, TVico,
    }

    public enum RotationDegree
    {
        Normal = 0,
        _90 = 90,
        _180 = 180,
        _270 = 270
    }

    [SerializeField, NuitrackSDKInspector] bool customColorResolution;
    [SerializeField, NuitrackSDKInspector] int colorWidth;
    [SerializeField, NuitrackSDKInspector] int colorHeight;
    List<nuitrack.device.VideoMode> availableColorResolutions = new List<nuitrack.device.VideoMode>();

    [SerializeField, NuitrackSDKInspector] bool customDepthResolution;
    [SerializeField, NuitrackSDKInspector] int depthWidth;
    [SerializeField, NuitrackSDKInspector] int depthHeight;
    List<nuitrack.device.VideoMode> availableDepthResolutions = new List<nuitrack.device.VideoMode>();

    [SerializeField, NuitrackSDKInspector, Tooltip("For example: license:12345:12AbCDefghIjklMn")]
    string licenseKey;
    [SerializeField, NuitrackSDKInspector, Tooltip("An attempt will be made to activate the license when Nuitrack is launched (ONLY ANDROID)")]
    bool autoActivateLicense = false;

    public string ResolutionFailMessage
    {
        get;
        private set;
    } = string.Empty;

    bool _threadRunning;
    Thread _thread;

    public NuitrackInitState InitState { get { return NuitrackLoader.initState; } }
    [SerializeField, NuitrackSDKInspector]
    bool
    depthModuleOn = true,
    colorModuleOn = true,
    userTrackerModuleOn = true,
    skeletonTrackerModuleOn = true,
    gesturesRecognizerModuleOn = true,
    handsTrackerModuleOn = true;

    public static List<SensorData> sensorsData = new List<SensorData>();

    //properties for backward compatibility
    [Obsolete("sensorsData[id].DepthSensor")]
    public static DepthSensor DepthSensor { get { return sensorsData[0].DepthSensor; } }
    [Obsolete("sensorsData[id].ColorSensor")]
    public static ColorSensor ColorSensor { get { return sensorsData[0].ColorSensor; } }
    [Obsolete("sensorsData[id].UserTracker")]
    public static UserTracker UserTracker { get { return sensorsData[0].UserTracker; } }
    [Obsolete("sensorsData[id].SkeletonTracker")]
    public static SkeletonTracker SkeletonTracker { get { return sensorsData[0].SkeletonTracker; } }
    [Obsolete("sensorsData[id].GestureRecognizer")]
    public static GestureRecognizer GestureRecognizer { get { return sensorsData[0].GestureRecognizer; } }
    [Obsolete("sensorsData[id].HandTracker")]
    public static HandTracker HandTracker { get { return sensorsData[0].HandTracker; } }
    [Obsolete("sensorsData[id].DepthFrame")]
    public static DepthFrame DepthFrame { get { return sensorsData[0].DepthFrame; } }
    [Obsolete("sensorsData[id].ColorFrame")]
    public static ColorFrame ColorFrame { get { return sensorsData[0].ColorFrame; } }
    [Obsolete("sensorsData[id].UserFrame")]
    public static UserFrame UserFrame { get { return sensorsData[0].UserFrame; } }

    [Obsolete("sensorsData[id].onDepthUpdate")]
    public static event DepthSensor.OnUpdate onDepthUpdate;
    [Obsolete("sensorsData[id].onColorUpdate")]
    public static event ColorSensor.OnUpdate onColorUpdate;
    [Obsolete("sensorsData[id].onUserTrackerUpdate")]
    public static event UserTracker.OnUpdate onUserTrackerUpdate;

    [Obsolete("sensorsData[id].Users")]
    public static Users Users { get { return sensorsData[0].Users; } }
    [Obsolete("sensorsData[id].Floor")]
    public static Plane? Floor { get { return sensorsData[0].Floor; } }

    public static JsonInfo NuitrackJson
    {
        get
        {
            try
            {
                string json = Nuitrack.GetInstancesJson();
                return NuitrackUtils.FromJson<JsonInfo>(json);
            }
            catch (System.Exception ex)
            {
                NuitrackErrorSolver.CheckError(ex);
            }

            return null;
        }
    }

    bool nuitrackError = false;
    public LicenseInfo LicenseInfo
    {
        get;
        private set;
    } = new LicenseInfo();

    [SerializeField, Range(1, 6), NuitrackSDKInspector] int maxActiveUsers = 2;

    [Space]

    [SerializeField, NuitrackSDKInspector] bool runInBackground = false;

    [Tooltip("Do not destroy this prefab when loading a new Scene")]
    [SerializeField, NuitrackSDKInspector] bool dontDestroyOnLoad = true;

    [Tooltip("Only skeleton. PC, Unity Editor, MacOS and IOS")]
    [SerializeField, NuitrackSDKInspector] WifiConnect wifiConnect = WifiConnect.none;

    [Tooltip("ONLY PC! Nuitrack AI is the new version of Nuitrack skeleton tracking middleware")]
    [SerializeField, NuitrackSDKInspector] bool useNuitrackAi = false;

    //[Tooltip("ONLY PC!")]
    //[SerializeField, NuitrackSDKInspector] bool useObjectDetection = false;

    [Tooltip("Track and get information about faces with Nuitrack (position, angle of rotation, box, emotions, age, gender).")]
    [SerializeField, NuitrackSDKInspector] bool useFaceTracking = false;

    [Tooltip("Depth map doesn't accurately match an RGB image. Turn on this to align them")]
    [SerializeField, NuitrackSDKInspector] bool depth2ColorRegistration = false;

    [Tooltip("Mirror sensor data")]
    [SerializeField, NuitrackSDKInspector] bool mirror = false;

    [Tooltip("If you have the sensor installed vertically or upside down, you can level this. Sensor rotation is not available for mirror mode.")]
    [SerializeField, NuitrackSDKInspector] RotationDegree sensorRotation = RotationDegree.Normal;

    [SerializeField, NuitrackSDKInspector] bool useFileRecord = false;
    [SerializeField, NuitrackSDKInspector] string pathToFileRecord = string.Empty;

    [Tooltip("Asynchronous initialization, allows you to turn on the nuitrack more smoothly. In this case, you need to ensure that all components that use this script will start only after its initialization.")]
    [SerializeField, NuitrackSDKInspector] bool asyncInit = false;

    [SerializeField, NuitrackSDKInspector] InitEvent initEvent;

    static NuitrackManager instance;
    NuitrackInitState initState = NuitrackInitState.INIT_NUITRACK_MANAGER_NOT_INSTALLED;
    Dictionary<string, int> serialNumbers = new Dictionary<string, int>();

    public MultisensorType multisensorType = MultisensorType.Singlesensor;
    public List<nuitrack.device.NuitrackDevice> devices = new List<nuitrack.device.NuitrackDevice>();

    public bool NuitrackInitialized
    {
        get;
        private set;
    } = false;

    public float RunningTime
    {
        get;
        private set;
    } = 0;

    #region Use Modules

    public bool UseColorModule { get => colorModuleOn; }
    public bool UseDepthModule { get => depthModuleOn; }
    public bool UseUserTrackerModule { get => userTrackerModuleOn; }
    public bool UseSkeletonTracking { get => skeletonTrackerModuleOn; }
    public bool UseHandsTracking { get => handsTrackerModuleOn; }
    public bool UserGestureTracking { get => gesturesRecognizerModuleOn; }
    public bool UseFaceTracking { get => useFaceTracking; }
    public bool UseNuitrackAi { get => useNuitrackAi; set { useNuitrackAi = value; } }
    //public bool UseObjectDetection { get => useObjectDetection; }

    #endregion

    bool IsNuitrackLibrariesInitialized()
    {
        if (Application.platform == RuntimePlatform.Android && !Application.isEditor)
            return initState == NuitrackInitState.INIT_OK || wifiConnect != WifiConnect.none;
        else
            return true;
    }

    public bool GetSensorIdBySerialNumber(string sn, out int sensorId)
    {
        return serialNumbers.TryGetValue(sn, out sensorId);
    }

    public static NuitrackManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<NuitrackManager>();
                if (instance == null)
                {
                    GameObject container = new GameObject();
                    container.name = "NuitrackManager";
                    instance = container.AddComponent<NuitrackManager>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (Instance.gameObject != gameObject)
        {
            DestroyImmediate(Instance.gameObject);
            instance = this;
        }

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(instance);

        if (colorWidth <= 0 || colorHeight <= 0)
            customColorResolution = false;

        if (depthWidth <= 0 || depthHeight <= 0)
            customDepthResolution = false;

        if (Application.platform == RuntimePlatform.Android && !Application.isEditor)
            StartCoroutine(AndroidInit());
        else
            Init();
    }

    void Init()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Application.targetFrameRate = 60;
        Application.runInBackground = runInBackground;

        initState = NuitrackLoader.InitNuitrackLibraries();

        StartNuitrack();

        StartCoroutine(InitEventStart());
    }

    IEnumerator AndroidInit()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        int androidApiLevel;

        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            androidApiLevel = version.GetStatic<int>("SDK_INT");
        }

        while (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            yield return null;
        }

        while (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
            yield return null;
        }

        while (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
        {
            Permission.RequestUserPermission(Permission.CoarseLocation);
            yield return null;
        }

        if (androidApiLevel > 26) // camera permissions required for Android newer than Oreo 8
        {
            while (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                yield return null;
            }
        }

        yield return null;
#endif
        Init();

        yield return null;
    }

    void NuitrackInit()
    {
        if (!asyncInit && Application.isEditor)
        {
            if (PlayerPrefs.GetInt("failStart") == 1 && Application.isEditor)
                return;

            PlayerPrefs.SetInt("failStart", 1);
        }

        try
        {
            RunningTime = 0;
#if UNITY_EDITOR_WIN
            if (NuitrackConfigHandler.ObjectDetection /*|| useObjectDetection*/)
            {
                if (!NuitrackErrorSolver.CheckCudnn())
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                    return;
                }
            }
#endif

            devices.Clear();
            sensorsData.Clear();

            if (wifiConnect != WifiConnect.none)
            {
                Debug.Log("If something doesn't work, then read this (Wireless case section): github.com/3DiVi/nuitrack-sdk/blob/master/doc/TVico_User_Guide.md#wireless-case");
                Nuitrack.Init("", Nuitrack.NuitrackMode.DEBUG);
                NuitrackConfigHandler.WifiConnect = wifiConnect;
                devices.Add(null);
                sensorsData.Add(new SensorData());
                sensorsData[0].CreateModules();
            }
            else
            {
                Nuitrack.Init();

                if (useFileRecord)
                    NuitrackConfigHandler.FileRecord = pathToFileRecord;

                NuitrackConfigHandler.ActiveUsers = maxActiveUsers;

                NuitrackConfigHandler.Depth2ColorRegistration = depth2ColorRegistration;
                //NuitrackConfigHandler.ObjectDetection = useObjectDetection;
                NuitrackConfigHandler.NuitrackAI = useNuitrackAi;
                NuitrackConfigHandler.FaceTracking = useFaceTracking;
                NuitrackConfigHandler.Mirror = mirror;
                NuitrackConfigHandler.RotateAngle = sensorRotation;
                //licenseInfo = JsonUtility.FromJson<LicenseInfo>(Nuitrack.GetDeviceList());

                string devicesInfo = "";

                List<nuitrack.device.NuitrackDevice> currentDevices = Nuitrack.GetDeviceList();

                if (multisensorType == MultisensorType.Singlesensor)
                {
                    if (currentDevices.Count > 0)
                    {
                        devices.Add(currentDevices[0]);

                        string deviceName = devices[0].GetInfo(nuitrack.device.DeviceInfoType.DEVICE_NAME);

                        if (customColorResolution)
                        {
                            availableColorResolutions = devices[0].GetAvailableVideoModes(nuitrack.device.StreamType.COLOR);
                            ChangeResolution(colorWidth, colorHeight, deviceName, "RGB");
                        }

                        if (customDepthResolution)
                        {
                            availableDepthResolutions = devices[0].GetAvailableVideoModes(nuitrack.device.StreamType.DEPTH);
                            ChangeResolution(depthWidth, depthHeight, deviceName, "Depth");
                        }
                    }
                }
                else
                {
                    devices = currentDevices;
                }

                if (devices.Count == 0)
                {
                    NuitrackErrorSolver.CheckError("Can't create DepthSensor");
                    nuitrackError = true;
                    PlayerPrefs.SetInt("failStart", 0);
                    return;
                }

                serialNumbers.Clear();

                for (int i = 0; i < devices.Count; i++)
                {
                    Nuitrack.SetDevice(devices[i]);
                    string sensorName = devices[i].GetInfo(nuitrack.device.DeviceInfoType.DEVICE_NAME);
                    if (i == 0)
                    {
                        LicenseInfo.Trial = devices[i].GetActivationStatus() == nuitrack.device.ActivationStatus.TRIAL;
                        LicenseInfo.SensorName = sensorName;
                    }

                    devicesInfo += "\nDevice " + i + " [Sensor Name: " + sensorName + ", License: " + devices[i].GetActivationStatus() + "] ";

                    serialNumbers.Add(devices[i].GetInfo(nuitrack.device.DeviceInfoType.SERIAL_NUMBER), i);
                    sensorsData.Add(new SensorData());
                    sensorsData[i].CreateModules();
                }

                //licenseInfo = JsonUtility.FromJson<LicenseInfo>(nuitrack.Nuitrack.GetDeviceList());

                Debug.Log(
                    "Nuitrack Start Info:\n" +
                    "NuitrackAI: " + NuitrackConfigHandler.NuitrackAI + "\n" +
                    "Faces using: " + NuitrackConfigHandler.FaceTracking + GetDevicesInfo());
            }

            Nuitrack.UpdateConfig();

            Debug.Log("Nuitrack Init OK");

            //OutputMode outputMode = sensorsData[0].DepthSensor.GetOutputMode();
            //Debug.Log("CX " + outputMode.Intrinsics.CX);
            //Debug.Log("CY " + outputMode.Intrinsics.CY);
            //Debug.Log("FX " + outputMode.Intrinsics.FX);
            //Debug.Log("FY " + outputMode.Intrinsics.FY);
            //Debug.Log("HFOV " + outputMode.HFOV);

            if (autoActivateLicense)
            {
                string licenseActivateResult;
                TryActivateLicense(devices[0], licenseKey, out licenseActivateResult);
                Debug.Log(licenseActivateResult);
            }

            Nuitrack.Run();
            Debug.Log("Nuitrack Run OK");

            ChangeModulesState(skeletonTrackerModuleOn, handsTrackerModuleOn, depthModuleOn, colorModuleOn, gesturesRecognizerModuleOn, userTrackerModuleOn);

            NuitrackInitialized = true;
#if UNITY_EDITOR
            if(!asyncInit && UnityEditor.PlayerSettings.colorSpace != ColorSpace.Gamma)
                Debug.LogWarning($"The color space parameter is set as {UnityEditor.PlayerSettings.colorSpace}, because of this, images may look darker than necessary");
#endif
        }
        catch (System.Exception ex)
        {
            nuitrackError = true;
            NuitrackErrorSolver.CheckError(ex);
        }

        if (!asyncInit)
            PlayerPrefs.SetInt("failStart", 0);
    }

    static public bool TryActivateLicense(nuitrack.device.NuitrackDevice device, string key, out string result)
    {
        result = "License key not entered";
        if (key == "")
            return false;

        Debug.Log("Activating sensor... Current License:" + device.GetActivationStatus());

        try
        {
            device.Activate(key);
            result = "License Activation Success";
        }
        catch (System.Exception ex)
        {
            result = "License Activation Failed\n" + ex.Message;
            return false;
        }
        Debug.Log("License status: " + device.GetActivationStatus());

        return true;
    }

    public void ChangeModulesState(bool skeleton, bool hand, bool depth, bool color, bool gestures, bool user)
    {
        if (sensorsData.Count == 0)
            return;

        for (int i = 0; i < sensorsData.Count; i++)
            sensorsData[i].ChangeModulesState(skeleton, hand, depth, color, gestures, user);

        if (color)
            sensorsData[0].onColorUpdate += HandleOnColorSensorUpdateEvent;
        else
            sensorsData[0].onColorUpdate -= HandleOnColorSensorUpdateEvent;

        if (depth)
            sensorsData[0].onDepthUpdate += HandleOnDepthSensorUpdateEvent;
        else
            sensorsData[0].onDepthUpdate -= HandleOnDepthSensorUpdateEvent;

        if (user)
            sensorsData[0].onUserTrackerUpdate += HandleUserTrackerUpdateEvent;
        else
            sensorsData[0].onUserTrackerUpdate -= HandleUserTrackerUpdateEvent;
    }

    void HandleOnDepthSensorUpdateEvent(DepthFrame frame)
    {
        if (multisensorType == MultisensorType.Singlesensor)
        {
            if (customDepthResolution && (frame.Cols != depthWidth || frame.Rows != depthHeight))
            {
                if (availableDepthResolutions.Count != 0)
                    ShowResFailMessage("DEPTH", availableDepthResolutions);
                else
                    ResolutionFailMessage = "No available DEPTH resolutions for this sensor";
            }

            depthWidth = frame.Cols;
            depthHeight = frame.Rows;
        }

        if (onDepthUpdate != null)
            onDepthUpdate.Invoke(frame);
    }

    void HandleOnColorSensorUpdateEvent(ColorFrame frame)
    {
        if (multisensorType == MultisensorType.Singlesensor)
        {
            if (ResolutionFailMessage == string.Empty && customColorResolution && (frame.Cols != colorWidth || frame.Rows != colorHeight))
            {
                if (availableColorResolutions.Count != 0)
                    ShowResFailMessage("COLOR", availableColorResolutions);
                else
                    ResolutionFailMessage = "No available COLOR resolutions for this sensor";
            }

            colorWidth = frame.Cols;
            colorHeight = frame.Rows;
        }

        if (onColorUpdate != null)
            onColorUpdate.Invoke(frame);
    }

    void HandleUserTrackerUpdateEvent(UserFrame frame)
    {
        if (onUserTrackerUpdate != null)
            onUserTrackerUpdate.Invoke(frame);
    }

    string GetDevicesInfo()
    {
        string devicesInfo = "";

        List<nuitrack.device.NuitrackDevice> devices = Nuitrack.GetDeviceList();

        if (devices.Count > 0)
        {
            for (int i = 0; i < devices.Count; i++)
            {
                nuitrack.device.NuitrackDevice device = devices[i];
                string sensorName = device.GetInfo(nuitrack.device.DeviceInfoType.DEVICE_NAME);
                if (i == 0)
                {
                    LicenseInfo.Trial = device.GetActivationStatus() == nuitrack.device.ActivationStatus.TRIAL;
                    LicenseInfo.SensorName = sensorName;
                }

                devicesInfo += "\nDevice " + i + " [Sensor Name: " + sensorName + ", License: " + device.GetActivationStatus() + "] ";
            }
        }

        return devicesInfo;
    }

    void ShowResFailMessage(string channel, List<nuitrack.device.VideoMode> videoModes)
    {
        List<string> listRes = videoModes.Select(w => string.Format("{0} X {1}", w.width, w.height)).Distinct().ToList();

        string message = string.Format("Custom {0} resolution was not applied\n" +
            "Try one of these {0} resolutions:\n{1}", channel, string.Join("\n", listRes));

        if (ResolutionFailMessage != string.Empty)
            ResolutionFailMessage += '\n';

        ResolutionFailMessage += message;

        Debug.LogError(message);
    }

    bool canBePaused;
    void OnApplicationPause(bool pauseStatus)
    {
        if (!canBePaused)
        {
            canBePaused = true;
            return;
        }

        if (pauseStatus)
            StopNuitrack();
        else
            StartCoroutine(DelayStartNuitrack());
    }

    IEnumerator DelayStartNuitrack()
    {
        while (NuitrackInitialized)
            yield return null;

        StartNuitrack();
    }

    public void StartNuitrack()
    {
        nuitrackError = false;

        if (!IsNuitrackLibrariesInitialized())
            return;

        if (asyncInit)
            StartThread();
        else
            NuitrackInit();
    }

    public void StopNuitrack()
    {
        if (!IsNuitrackLibrariesInitialized() || !NuitrackInitialized)
            return;

        try
        {
            for (int i = 0; i < devices.Count; i++)
                sensorsData[i].StopProcessing();

            if (colorModuleOn)
                sensorsData[0].onColorUpdate -= HandleOnColorSensorUpdateEvent;

            if (depthModuleOn)
                sensorsData[0].onDepthUpdate -= HandleOnDepthSensorUpdateEvent;

            if (userTrackerModuleOn)
                sensorsData[0].onUserTrackerUpdate -= HandleUserTrackerUpdateEvent;

            Nuitrack.Release();
            Debug.Log("Nuitrack Stop OK");
            NuitrackInitialized = false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
    }

    IEnumerator InitEventStart()
    {
        while (!NuitrackInitialized)
            yield return null;

        if (initEvent != null)
            initEvent.Invoke(initState);
    }

    void Update()
    {
        if (nuitrackError || !IsNuitrackLibrariesInitialized() || !NuitrackInitialized || (asyncInit && _threadRunning))
            return;

        RunningTime += Time.deltaTime;

        try
        {
            if (multisensorType == MultisensorType.Singlesensor)
            {
                Nuitrack.Update();
            }
            else
            {
                for (int i = 0; i < devices.Count; i++)
                    Nuitrack.WaitUpdate(sensorsData[i].SkeletonTracker);
            }

            for (int i = 0; i < devices.Count; i++)
                sensorsData[i].Update();
        }
        catch (System.Exception ex)
        {
            NuitrackErrorSolver.CheckError(ex, true, false);
            nuitrackError = true;
        }
    }

    void ChangeResolution(int width, int height, string device, string channel)
    {
        if (device.Contains("RealSense")) device = "RealSense";
        if (device.Contains("Astra")) device = "Astra";

        switch (device)
        {
            case "Astra":
                Nuitrack.SetConfigValue("OpenNIModule." + channel + ".Width", width.ToString());
                Nuitrack.SetConfigValue("OpenNIModule." + channel + ".Height", height.ToString());
                break;
            case "RealSense":
                Nuitrack.SetConfigValue("Realsense2Module." + channel + ".ProcessWidth", width.ToString());
                Nuitrack.SetConfigValue("Realsense2Module." + channel + ".ProcessHeight", height.ToString());
                Nuitrack.SetConfigValue("Realsense2Module." + channel + ".RawWidth", width.ToString());
                Nuitrack.SetConfigValue("Realsense2Module." + channel + ".RawHeight", height.ToString());
                break;
            default:
                ResolutionFailMessage = "You cannot change the resolution on this sensor (Only Orbbec Astra and Realsense)";
                Debug.LogWarning(ResolutionFailMessage);

                if (channel == "RGB")
                    customColorResolution = false;

                if (channel == "Depth")
                    customDepthResolution = false;
                break;
        }

        Debug.Log(device + " used custom " + channel + " resolution: " + width.ToString() + "X" + height.ToString());
    }

    void OnDestroy()
    {
        StopNuitrack();
    }

    #region Async Init
    void StartThread()
    {
        if (_threadRunning)
            return;

        _threadRunning = true;

        _thread = new Thread(WorkingThread);
        _thread.Start();
    }

    void WorkingThread()
    {
        NuitrackInit();
        StopThread();
    }

    void StopThread()
    {
        if (!_threadRunning)
            return;

        _threadRunning = false;
        _thread.Join();
    }
    #endregion
}
