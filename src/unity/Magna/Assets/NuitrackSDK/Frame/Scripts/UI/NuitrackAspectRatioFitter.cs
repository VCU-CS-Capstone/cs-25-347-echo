using UnityEngine;
using UnityEngine.UI;


namespace NuitrackSDK.Frame
{
    [AddComponentMenu("NuitrackSDK/Frame/UI/Nuitrack Aspect Ratio Fitter")]
    public class NuitrackAspectRatioFitter : AspectRatioFitter
    {
        public int sensorId;

        public enum FrameMode
        {
            Color = 0,
            Depth = 1,
            Segment = 2
        }

        [SerializeField] FrameMode frameMode = FrameMode.Color;

        RectTransform m_Rect;

        public RectTransform RectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!NuitrackManager.Instance.NuitrackInitialized)
                return;

            switch (frameMode)
            {
                case FrameMode.Color:
                    NuitrackManager_onFrameUpdate(NuitrackManager.sensorsData[sensorId].ColorFrame);
                    break;
                case FrameMode.Depth:
                    NuitrackManager_onFrameUpdate(NuitrackManager.sensorsData[sensorId].DepthFrame);
                    break;
                case FrameMode.Segment:
                    NuitrackManager_onFrameUpdate(NuitrackManager.sensorsData[sensorId].UserFrame);
                    break;
            }
        }

        void NuitrackManager_onFrameUpdate<T>(nuitrack.Frame<T> frame) where T : struct
        {
            if (frame != null)
                SetAspectRatio(frame.Cols, frame.Rows);
        }

        public void SetAspectRatio(int width, int height)
        {
            float frameAspectRatio = (float)width / height;
            aspectRatio = frameAspectRatio;
        }
    }
}