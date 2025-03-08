using UnityEngine;
using UnityEditor;

using System.IO;
using System.Collections.Generic;

using NuitrackSDK;
using NuitrackSDK.Frame;


namespace NuitrackSDKEditor
{
    [CustomEditor(typeof(NuitrackManager), true)]
    public class NuitrackManagerEditor : NuitrackSDKEditor
    {
        bool openModules = false;
        bool openSensorResolution = false;

        TextureCache rgbCache = null;
        TextureCache depthCache = null;

        RenderTexture rgbTexture = null;
        RenderTexture depthTexture = null;

        string ClassParamName(string paramName)
        {
            return string.Format("{0}_{1}", GetType().Name, paramName);
        }

        bool ShowPreview
        {
            get
            {
                return EditorPrefs.GetBool(ClassParamName("showPreview"), false);
            }
            set
            {
                EditorPrefs.SetBool(ClassParamName("showPreview"), value);
            }
        }

        void OnDisable()
        {
            if (rgbCache != null)
                rgbCache.Dispose();

            if (depthCache != null)
                depthCache.Dispose();
        }

        public override void OnInspectorGUI()
        {
            DrawModules();

            DrawDefaultInspector();

            serializedObject.DrawPropertyField("runInBackground");
            serializedObject.DrawPropertyField("dontDestroyOnLoad");

            DrawSensorOptions();
            DrawAdvanced();

            DrawInitEvent();

            DrawFramePreview();


            SerializedProperty autoActivateLicense = serializedObject.DrawPropertyField("autoActivateLicense");
            if (autoActivateLicense.boolValue)
                serializedObject.DrawPropertyField("licenseKey");
        }

        void DrawModules()
        {
            openModules = EditorGUILayout.BeginFoldoutHeaderGroup(openModules, "Modules");

            if (openModules)
            {
                using (new VerticalGroup(EditorStyles.helpBox))
                {
                    serializedObject.DrawPropertyField("depthModuleOn");
                    serializedObject.DrawPropertyField("colorModuleOn");
                    serializedObject.DrawPropertyField("userTrackerModuleOn");
                    serializedObject.DrawPropertyField("skeletonTrackerModuleOn");
                    serializedObject.DrawPropertyField("gesturesRecognizerModuleOn");
                    serializedObject.DrawPropertyField("handsTrackerModuleOn");

                    NuitrackSDKGUI.PropertyWithHelpButton(
                        serializedObject,
                        "useFaceTracking",
                        "https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/Unity_Face_Tracking.md",
                        "Track and get information about faces with Nuitrack (position, angle of rotation, box, emotions, age, gender)");
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.DrawPropertyField("maxActiveUsers");
        }

        void DrawSensorOptions()
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Sensor options", EditorStyles.boldLabel);

            serializedObject.DrawPropertyField("depth2ColorRegistration");

            serializedObject.DrawPropertyField("mirror");
            serializedObject.DrawPropertyField("sensorRotation");
        }

        void DrawAdvanced()
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);

            if (PlayerSettings.colorSpace != ColorSpace.Gamma)
            {
                UnityEngine.Events.UnityAction fixAction = delegate { PlayerSettings.colorSpace = ColorSpace.Gamma; };
                NuitrackSDKGUI.DrawMessage($"The color space parameter is set as {PlayerSettings.colorSpace}, because of this, images may look darker than necessary", LogType.Warning, fixAction, "Change to Gamma");
            }

            NuitrackSDKGUI.PropertyWithHelpButton(
                serializedObject,
                "wifiConnect",
                "https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/TVico_User_Guide.md#wireless-case",
                "Only skeleton. PC, Unity Editor, MacOS and IOS");

            SerializedProperty useNuitrackAIProp = NuitrackSDKGUI.PropertyWithHelpButton(
                serializedObject,
                "useNuitrackAi",
                "https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/Nuitrack_AI.md",
                "ONLY PC! Nuitrack AI is the new version of Nuitrack skeleton tracking middleware");

            if (useNuitrackAIProp.boolValue)
            {
                NuitrackSDKGUI.DrawMessage($"Nuitrack AI Skeleton Tracking has high CPU requirements", LogType.Warning);
            }

            SerializedProperty useFileRecordProp = serializedObject.DrawPropertyField("useFileRecord", "Use record file");

            if (useFileRecordProp.boolValue)
            {
                SerializedProperty pathProperty = serializedObject.FindProperty("pathToFileRecord");

                pathProperty.stringValue = NuitrackSDKGUI.OpenFileField(pathProperty.stringValue, "Bag or oni file", "bag", "oni");

                serializedObject.ApplyModifiedProperties();
            }

            #region Object detector

            UnityEngine.Events.UnityAction helpClick = delegate
            {
                Application.OpenURL("https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/Nuitrack_AI.md#nuitrack-ai-object-detection");
            };

            Rect propertyRect = NuitrackSDKGUI.WithRightButton(helpClick, "_Help", "Track and get information about objects with Nuitrack");

            string nuitrackHome = NuitrackSDK.ErrorSolver.NuitrackErrorSolver.NuitrackHomePath;
            if (nuitrackHome != null)
            {
                string configPath = Path.Combine(nuitrackHome, "data", "nuitrack.config");

                UnityEngine.Events.UnityAction openFolderClick = delegate { EditorUtility.RevealInFinder(configPath); };

                using (new EditorGUI.DisabledGroupScope(!File.Exists(configPath)))
                    propertyRect = NuitrackSDKGUI.WithRightButton(propertyRect, openFolderClick, "Project", "Open config file folder");
            }

            using (new EditorGUI.DisabledGroupScope(true))
            {
                GUIContent objDetectionGUIContent = new GUIContent("Use Object Detection",
                    "Track and get information about objects with Nuitrack (set in nuitrack.config and restart scene)");

                bool objDetectionActive = false;
                if (nuitrackHome != null)
                    objDetectionActive = NuitrackConfigHandler.ObjectDetection;

                EditorGUI.Toggle(propertyRect, objDetectionGUIContent, objDetectionActive);
            }

            #endregion

            using (new EditorGUI.DisabledGroupScope(true))
            {
                GUIContent objDetectionGUIContent = new GUIContent("Use Object Detection",
                    "Track and get information about objects with Nuitrack (set in nuitrack.config and restart scene)");

                EditorGUI.Toggle(propertyRect, objDetectionGUIContent, NuitrackConfigHandler.ObjectDetection);
            }

            openSensorResolution = EditorGUILayout.BeginFoldoutHeaderGroup(openSensorResolution, "Sensor resolution");

            if(openSensorResolution)
            {
                NuitrackSDKGUI.DrawMessage("Invalid sensor resolution will be reset to default", LogType.Log);

                using (new VerticalGroup(EditorStyles.helpBox))
                {
                    SerializedProperty customColorResolution = serializedObject.DrawPropertyField("customColorResolution");

                    using (new EditorGUI.DisabledGroupScope(!customColorResolution.boolValue))
                    {
                        serializedObject.DrawPropertyField("colorWidth");
                        serializedObject.DrawPropertyField("colorHeight");
                    }
                }

                using (new VerticalGroup(EditorStyles.helpBox))
                {
                    SerializedProperty customDepthResolution = serializedObject.DrawPropertyField("customDepthResolution");

                    using (new EditorGUI.DisabledGroupScope(!customDepthResolution.boolValue))
                    {
                        serializedObject.DrawPropertyField("depthWidth");
                        serializedObject.DrawPropertyField("depthHeight");
                    }
                }

                NuitrackManager nuitrackManager = (serializedObject.targetObject as NuitrackManager);
                if(nuitrackManager != null)
                {
                    if (nuitrackManager.ResolutionFailMessage != string.Empty)
                        NuitrackSDKGUI.DrawMessage(nuitrackManager.ResolutionFailMessage, LogType.Error);
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawInitEvent()
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Init", EditorStyles.boldLabel);

            SerializedProperty useAsyncInit = serializedObject.DrawPropertyField("asyncInit");
            if (useAsyncInit.boolValue)
            {
                NuitrackSDKGUI.DrawMessage(
                    "Asynchronous initialization, allows you to turn on the nuitrack more smoothly. In this case, " +
                    "you need to ensure that all components that use this script will start only after its initialization.", LogType.Warning);
            }

            serializedObject.DrawPropertyField("initEvent");
        }

        void DrawFramePreview()
        {
            float pointScale = 0.025f;
            float lineScale = 0.01f;

            ShowPreview = EditorGUILayout.BeginFoldoutHeaderGroup(ShowPreview, "Frame viewer");

            if (ShowPreview)
            {
                if (!EditorApplication.isPlaying)
                    NuitrackSDKGUI.DrawMessage("RGB and depth frames will be displayed run time.", LogType.Log);
                else
                {
                    if (rgbCache == null)
                        rgbCache = new TextureCache();

                    if (depthCache == null)
                        depthCache = new TextureCache();

                    List<Vector2> pointCoord = new List<Vector2>();

                    rgbTexture = NuitrackManager.sensorsData[0].ColorFrame.ToRenderTexture(rgbCache);
                    depthTexture = NuitrackManager.sensorsData[0].DepthFrame.ToRenderTexture(textureCache: depthCache);

                    Rect rgbRect = NuitrackSDKGUI.DrawFrame(rgbTexture, "RGB frame");

                    Texture pointTexture = EditorGUIUtility.IconContent("sv_icon_dot0_pix16_gizmo").image;

                    float lineSize = rgbRect.size.magnitude * lineScale;

                    foreach (UserData user in NuitrackManager.sensorsData[0].Users)
                        if (user.Skeleton != null)
                        {
                            Color userColor = FrameUtils.SegmentToTexture.GetColorByID(user.ID);

                            foreach (nuitrack.JointType jointType in System.Enum.GetValues(typeof(nuitrack.JointType)))
                            {
                                nuitrack.JointType parentJointType = jointType.GetParent();

                                UserData.SkeletonData.Joint joint = user.Skeleton.GetJoint(jointType);

                                if (joint.Confidence > 0.1f)
                                {
                                    Vector2 startPoint = new Vector2(rgbRect.x + rgbRect.width * joint.Proj.x, rgbRect.y + rgbRect.height * (1 - joint.Proj.y));

                                    pointCoord.Add(startPoint);

                                    if (jointType.GetParent() != nuitrack.JointType.None)
                                    {
                                        UserData.SkeletonData.Joint parentJoint = user.Skeleton.GetJoint(parentJointType);

                                        if (parentJoint.Confidence > 0.1f)
                                        {
                                            Vector2 endPoint = new Vector2(rgbRect.x + rgbRect.width * parentJoint.Proj.x, rgbRect.y + rgbRect.height * (1 - parentJoint.Proj.y));
                                            Handles.DrawBezier(startPoint, endPoint, startPoint, endPoint, userColor, null, lineSize);
                                        }
                                    }
                                }
                            }

                            float pointSize = rgbRect.size.magnitude * pointScale;

                            foreach (Vector3 point in pointCoord)
                            {
                                Rect rect = new Rect(point.x - pointSize / 2, point.y - pointSize / 2, pointSize, pointSize);
                                GUI.DrawTexture(rect, pointTexture, ScaleMode.ScaleToFit);
                            }
                        }

                    NuitrackSDKGUI.DrawFrame(depthTexture, "Depth frame");

                    Repaint();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}