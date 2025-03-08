using UnityEngine;
using nuitrack;
using NuitrackSDK;
using NuitrackSDK.ErrorSolver;

public class SensorData
{
    public DepthSensor DepthSensor;
    public ColorSensor ColorSensor;
    public UserTracker UserTracker;
    public SkeletonTracker SkeletonTracker;
    public GestureRecognizer GestureRecognizer;
    public HandTracker HandTracker;
    public DepthFrame DepthFrame;
    public ColorFrame ColorFrame;
    public UserFrame UserFrame;

    SkeletonData skeletonData;
    HandTrackerData handTrackerData;
    GestureData gestureData;

    public ColorSensor.OnUpdate onColorUpdate;
    public DepthSensor.OnUpdate onDepthUpdate;
    public UserTracker.OnUpdate onUserTrackerUpdate;

    public Plane? Floor;

    public Users Users = new Users();

    bool successColorFrame, successDepthFrame;

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

    public bool
    depthModuleOn = true,
    colorModuleOn = true,
    userTrackerModuleOn = true,
    skeletonTrackerModuleOn = true,
    gesturesRecognizerModuleOn = true,
    handsTrackerModuleOn = true;

    public void ChangeModulesState(bool skeleton, bool hand, bool depth, bool color, bool gestures, bool user)
    {
        if (skeletonTrackerModuleOn != skeleton || !NuitrackManager.Instance.NuitrackInitialized)
        {
            skeletonTrackerModuleOn = skeleton;

            skeletonData = null;

            if (skeleton)
                SkeletonTracker.OnSkeletonUpdateEvent += HandleOnSkeletonUpdateEvent;
            else
                SkeletonTracker.OnSkeletonUpdateEvent -= HandleOnSkeletonUpdateEvent;
        }

        if (handsTrackerModuleOn != hand || !NuitrackManager.Instance.NuitrackInitialized)
        {
            handsTrackerModuleOn = hand;

            handTrackerData = null;

            if (hand)
                HandTracker.OnUpdateEvent += HandleOnHandsUpdateEvent;
            else
                HandTracker.OnUpdateEvent -= HandleOnHandsUpdateEvent;
        }

        if (gesturesRecognizerModuleOn != gestures || !NuitrackManager.Instance.NuitrackInitialized)
        {
            gesturesRecognizerModuleOn = gestures;

            gestureData = null;

            if (gestures)
                GestureRecognizer.OnNewGesturesEvent += OnNewGestures;
            else
                GestureRecognizer.OnNewGesturesEvent -= OnNewGestures;
        }

        if (depthModuleOn != depth || !NuitrackManager.Instance.NuitrackInitialized)
        {
            depthModuleOn = depth;

            DepthFrame = null;

            if (depth)
                DepthSensor.OnUpdateEvent += HandleOnDepthSensorUpdateEvent;
            else
                DepthSensor.OnUpdateEvent -= HandleOnDepthSensorUpdateEvent;
        }

        if (colorModuleOn != color || !NuitrackManager.Instance.NuitrackInitialized)
        {
            colorModuleOn = color;

            ColorFrame = null;

            if (color)
                ColorSensor.OnUpdateEvent += HandleOnColorSensorUpdateEvent;
            else
                ColorSensor.OnUpdateEvent -= HandleOnColorSensorUpdateEvent;
        }

        if (userTrackerModuleOn != user || !NuitrackManager.Instance.NuitrackInitialized)
        {
            userTrackerModuleOn = user;

            UserFrame = null;

            if (user)
                UserTracker.OnUpdateEvent += HandleOnUserTrackerUpdateEvent;
            else
                UserTracker.OnUpdateEvent -= HandleOnUserTrackerUpdateEvent;
        }
    }

    public void CreateModules()
    {
        DepthSensor = DepthSensor.Create();
        ColorSensor = ColorSensor.Create();
        UserTracker = UserTracker.Create();
        SkeletonTracker = SkeletonTracker.Create();
        GestureRecognizer = GestureRecognizer.Create();
        HandTracker = HandTracker.Create();
    }

    void HandleOnDepthSensorUpdateEvent(DepthFrame frame)
    {
        if (DepthFrame != null)
            DepthFrame.Dispose();

        DepthFrame = (DepthFrame)frame.Clone();

        try
        {
            onDepthUpdate?.Invoke(DepthFrame);

            if (!successDepthFrame)
            {
                successDepthFrame = true;
                Debug.Log("Update onNewDepthFrame callback");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    void HandleOnColorSensorUpdateEvent(ColorFrame frame)
    {
        if (ColorFrame != null)
            ColorFrame.Dispose();

        ColorFrame = (ColorFrame)frame.Clone();

        try
        {
            onColorUpdate?.Invoke(ColorFrame);

            if (!successColorFrame)
            {
                successColorFrame = true;
                Debug.Log("Update onNewRGBFrame callback");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    void HandleOnUserTrackerUpdateEvent(UserFrame frame)
    {
        if (UserFrame != null)
            UserFrame.Dispose();

        UserFrame = (UserFrame)frame.Clone();

        try
        {
            onUserTrackerUpdate?.Invoke(UserFrame);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }

        Floor = new Plane(frame.FloorNormal.ToVector3().normalized, frame.Floor.ToVector3() * 0.001f);
    }

    void HandleOnSkeletonUpdateEvent(SkeletonData _skeletonData)
    {
        if (skeletonData != null)
            skeletonData.Dispose();

        skeletonData = (SkeletonData)_skeletonData.Clone();
    }

    void OnNewGestures(GestureData gestures)
    {
        if (gestureData != null)
            gestureData.Dispose();

        gestureData = (GestureData)gestures.Clone();
    }

    void HandleOnHandsUpdateEvent(HandTrackerData _handTrackerData)
    {
        if (handTrackerData != null)
            handTrackerData.Dispose();

        handTrackerData = (HandTrackerData)_handTrackerData.Clone();
    }

    public void Update()
    {
        Users.UpdateData(skeletonData, handTrackerData, gestureData, NuitrackJson);

        if (gestureData != null)
        {
            gestureData.Dispose();
            gestureData = null;
        }
    }

    public void StopProcessing()
    {
        if (DepthSensor != null)
            DepthSensor.OnUpdateEvent -= HandleOnDepthSensorUpdateEvent;

        if (ColorSensor != null)
            ColorSensor.OnUpdateEvent -= HandleOnColorSensorUpdateEvent;

        if (UserTracker != null)
            UserTracker.OnUpdateEvent -= HandleOnUserTrackerUpdateEvent;

        if (SkeletonTracker != null)
            SkeletonTracker.OnSkeletonUpdateEvent -= HandleOnSkeletonUpdateEvent;

        if (GestureRecognizer != null)
            GestureRecognizer.OnNewGesturesEvent -= OnNewGestures;

        if (HandTracker != null)
            HandTracker.OnUpdateEvent -= HandleOnHandsUpdateEvent;

        DepthFrame = null;
        ColorFrame = null;
        UserFrame = null;
        skeletonData = null;
        gestureData = null;
        handTrackerData = null;

        DepthSensor = null;
        ColorSensor = null;
        UserTracker = null;
        SkeletonTracker = null;
        GestureRecognizer = null;
        HandTracker = null;

    }
}

