using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using System.Collections.Generic;


namespace NuitrackSDK.Frame
{
    public class DrawSensorFrame : MonoBehaviour
    {
        public int sensorId = 0;
        public enum FrameType
        {
            Color = 0,
            Depth = 1,
            User = 2,
        }

        [SerializeField] FrameType defaultFrameType = FrameType.Color;
        [SerializeField] RectTransform panel;

        [Header ("Frame objects")]
        [SerializeField] FrameProvider colorImage;
        [SerializeField] FrameProvider depthImage;
        [SerializeField] FrameProvider userImage;
        [SerializeField] FrameProvider segmentOverlay;

        [Header ("UI elements")]
        [SerializeField] Toggle segmentToggle;
        [SerializeField] Avatar.SkeletonsUI skeletonsOverlay;
        [SerializeField] Toggle skeletonToggle;

        [SerializeField] GameObject facesOverlay;
        [SerializeField] Toggle facesToggle;

        [SerializeField] NuitrackDemos.HandTrackerVisualization handsTrackerOverlay;
        [SerializeField] Toggle handsTrackerToggle;

        [SerializeField] NuitrackDemos.GesturesVisualization gestureVisualizationOverlay;
        [SerializeField] Toggle gesturesToggle;

        [SerializeField] Dropdown frameDropDown;

        [Header("Options")]
        [SerializeField, Range(1, 100)] int windowPercent = 20;
        [SerializeField] bool fullscreenDefault = true;
        [SerializeField] bool showSegmentOverlay = false;
        [SerializeField] bool showSkeletonsOverlay = false;
        [SerializeField] bool showFacesOverlay = false;
        [SerializeField] bool showHandsOverlay = false;
        [SerializeField] bool showGesturesOverlay = false;

        bool isFullscreen;

        public void SwitchByIndex(int frameIndex)
        {
            SelectFrame((FrameType)frameIndex);
        }

        void Start()
        {
            SelectFrame(defaultFrameType);

            isFullscreen = fullscreenDefault;
            SwitchFullscreen();

            segmentToggle.isOn = showSegmentOverlay;
            segmentOverlay.gameObject.SetActive(showSegmentOverlay);

            skeletonToggle.isOn = showSkeletonsOverlay;
            skeletonsOverlay.gameObject.SetActive(showSkeletonsOverlay);

            facesToggle.isOn = showFacesOverlay;
            facesOverlay.SetActive(showFacesOverlay);

            handsTrackerToggle.isOn = showHandsOverlay;
            handsTrackerOverlay.gameObject.SetActive(showHandsOverlay);

            gesturesToggle.isOn = showGesturesOverlay;
            gestureVisualizationOverlay.gameObject.SetActive(showGesturesOverlay);

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            List<string> frameOptions = new List<string>()
            {
                FrameType.Color.ToString(),
                FrameType.Depth.ToString(),
                FrameType.User.ToString()
            };

            frameDropDown.AddOptions(frameOptions);

            FrameProvider[] frameProviders = new FrameProvider[] { colorImage, depthImage, userImage, segmentOverlay };

            foreach (FrameProvider frameProvider in frameProviders)
                frameProvider.sensorId = sensorId;

            NuitrackAspectRatioFitter[] aspectRatioFitter = GetComponentsInChildren<NuitrackAspectRatioFitter>();

            foreach (var item in aspectRatioFitter)
                item.sensorId = sensorId;

            skeletonsOverlay.sensorId = sensorId;
            handsTrackerOverlay.sensorId = sensorId;
            gestureVisualizationOverlay.sensorId = sensorId;
        }

        bool ActiveFrameType(FrameType frameType)
        {
            return frameType switch
            {
                FrameType.Color => NuitrackManager.Instance.UseColorModule,
                FrameType.Depth => NuitrackManager.Instance.UseDepthModule,
                FrameType.User => NuitrackManager.Instance.UseUserTrackerModule,
                _ => false,
            };
        }

        void CheckToggle(Toggle toggle, bool isActive)
        {
            toggle.interactable = isActive;
            toggle.isOn = toggle.isOn && toggle.interactable;
        }

        void Update()
        {
            CheckToggle(segmentToggle, NuitrackManager.Instance.UseUserTrackerModule);
            CheckToggle(skeletonToggle, NuitrackManager.Instance.UseSkeletonTracking);
            CheckToggle(facesToggle, NuitrackManager.Instance.UseFaceTracking);

            foreach (Toggle toggle in gameObject.GetComponentsInChildren<Toggle>())
            {
                foreach(FrameType frameType in System.Enum.GetValues(typeof(FrameType)))
                {
                    string name = string.Format("Item {0}: {1}", (int)frameType, frameType.ToString());

                    if (toggle.name == name)
                        toggle.interactable = ActiveFrameType(frameType);
                }
            }
        }

        void SelectFrame(FrameType frameType)
        {
            colorImage.gameObject.SetActive(frameType == FrameType.Color);
            depthImage.gameObject.SetActive(frameType == FrameType.Depth);
            userImage.gameObject.SetActive(frameType == FrameType.User);
        }

        public void SwitchSegmentOverlay(bool value)
        {
            segmentOverlay.gameObject.SetActive(value);
        }

        public void SwitchSkeletonsOverlay(bool value)
        {
            skeletonsOverlay.gameObject.SetActive(value);
        }

        public void SwitchFacesOverlay(bool value)
        {
            facesOverlay.gameObject.SetActive(value);
        }

        public void SwitchHandsOverlay(bool value)
        {
            handsTrackerOverlay.gameObject.SetActive(value);
        }

        public void SwitchGesturesOverlay(bool value)
        {
            gestureVisualizationOverlay.gameObject.SetActive(value);
        }

        public void SwitchFullscreen()
        {
            isFullscreen = !isFullscreen;

            if (isFullscreen)
                panel.localScale = new Vector3(1.0f / 100 * windowPercent, 1.0f / 100 * windowPercent, 1.0f);
            else
                panel.localScale = new Vector3(1, 1, 1);
        }
    }
}