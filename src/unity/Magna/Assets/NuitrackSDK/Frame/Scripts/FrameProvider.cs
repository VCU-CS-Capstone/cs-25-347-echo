using UnityEngine;

using UnityEngine.Events;


namespace NuitrackSDK.Frame
{
    [AddComponentMenu("NuitrackSDK/Frame/Frame Provider")]
    public class FrameProvider : MonoBehaviour
    {
        public int sensorId;
        [SerializeField, NuitrackSDKInspector] bool syncColorWithSkeleton;

        public enum FrameType
        {
            Color = 0,
            Depth = 1,
            Segment = 2
        }

        public enum TextureMode
        {
            RenderTexture = 0,
            Texture2D = 1,
            Texture = 2
        }

        public enum SegmentMode
        {
            All = 0,
            Single = 1
        }

        [SerializeField, NuitrackSDKInspector] FrameType frameType;

        // Depth options
        [SerializeField, NuitrackSDKInspector] bool useCustomDepthGradient = false;
        [SerializeField, NuitrackSDKInspector] Gradient customDepthGradient = new Gradient()
        {
            colorKeys = new GradientColorKey[5] {
                new GradientColorKey(new Color(1, 0, 0), 0.1f),
                new GradientColorKey(new Color(1, 1, 0), 0.3f),
                new GradientColorKey(new Color(0, 1, 0), 0.5f),
                new GradientColorKey(new Color(0, 1, 1), 0.65f),
                new GradientColorKey(new Color(0, 0, 1), 1.0f),
            },
            alphaKeys = new GradientAlphaKey[2] {
                new GradientAlphaKey(1, 0),
                new GradientAlphaKey(1, 1)
            }
        };

        // Segment options
        [SerializeField, NuitrackSDKInspector] SegmentMode segmentMode = SegmentMode.All;

        [SerializeField, NuitrackSDKInspector] bool useCustomUsersColors = false;
        [SerializeField, NuitrackSDKInspector] Color[] customUsersColors = new Color[]
        {
            Color.clear,
            Color.red,
            Color.green,
            Color.blue,
            Color.magenta,
            Color.yellow,
            Color.cyan,
            Color.grey
        };

        [SerializeField, NuitrackSDKInspector] bool useCurrentUserTracker = true;
        [SerializeField, Range(1, 6), NuitrackSDKInspector] int userID = 1;
        [SerializeField, NuitrackSDKInspector] Color userColor = Color.red;

        [SerializeField, NuitrackSDKInspector] TextureMode textureMode;

        [SerializeField, NuitrackSDKInspector] UnityEvent<Texture> onFrameUpdate;

        TextureCache textureCache;

        ulong lastSkelTimestamp;

        Gradient DepthGradient
        {
            get
            {
                return useCustomDepthGradient ? customDepthGradient : null;
            }
        }

        Color[] UsersColors
        {
            get
            {
                return useCustomUsersColors ? customUsersColors : null;
            }
        }

        void Awake()
        {
            textureCache = new TextureCache();
        }

        void OnDestroy()
        {
            if (textureCache != null)
                textureCache.Dispose();
        }

        void Update()
        {
            if (!NuitrackManager.Instance.NuitrackInitialized)
                return;

            Texture texture = GetTexture();
            if (texture != null)
                onFrameUpdate.Invoke(texture);
        }

        Texture GetTexture()
        {
            return frameType switch
            {
                FrameType.Color => GetColorTexture(),
                FrameType.Depth => GetDepthTexture(),
                FrameType.Segment => GetSegmentTexture(),
                _ => null,
            };
        }

        Texture GetColorTexture()
        {
            if (!NuitrackManager.Instance.UseColorModule)
                return null;

            SensorData sensorData = NuitrackManager.sensorsData[sensorId];
            nuitrack.ColorFrame frame = sensorData.ColorFrame;

            if (frame == null || frame.Cols == 0 || frame.Rows == 0)
                return null;

            if (syncColorWithSkeleton && sensorData.SkeletonTracker.GetSkeletonData().Skeletons.Length > 0)
            {
                if (lastSkelTimestamp != sensorData.SkeletonTracker.GetSkeletonData().Timestamp)
                    lastSkelTimestamp = sensorData.SkeletonTracker.GetSkeletonData().Timestamp;
                else
                    return null;
            }

            return textureMode switch
            {
                TextureMode.RenderTexture => frame.ToRenderTexture(textureCache),
                TextureMode.Texture2D => frame.ToTexture2D(textureCache),
                TextureMode.Texture => frame.ToTexture(textureCache),
                _ => null,
            };
        }

        Texture GetDepthTexture()
        {
            if (!NuitrackManager.Instance.UseDepthModule)
                return null;

            nuitrack.DepthFrame frame = NuitrackManager.sensorsData[sensorId].DepthFrame;
            
            if (frame == null || frame.Cols == 0 || frame.Rows == 0)
                return null;

            return textureMode switch
            {
                TextureMode.RenderTexture => frame.ToRenderTexture(DepthGradient, textureCache),
                TextureMode.Texture2D => frame.ToTexture2D(DepthGradient, textureCache),
                TextureMode.Texture => frame.ToTexture(DepthGradient, textureCache),
                _ => null,
            };
        }

        Texture GetSegmentTexture()
        {
            if (!NuitrackManager.Instance.UseUserTrackerModule)
                return null;

            if (segmentMode == SegmentMode.Single)
            {
                UserData userData = useCurrentUserTracker ? NuitrackManager.sensorsData[0].Users.Current : NuitrackManager.sensorsData[0].Users.GetUser(userID);

                if (userData == null)
                    return null;

                return textureMode switch
                {
                    TextureMode.RenderTexture => userData.SegmentRenderTexture(userColor),
                    TextureMode.Texture2D => userData.SegmentTexture2D(userColor),
                    TextureMode.Texture => userData.SegmentTexture(userColor),
                    _ => null,
                };
            }
            else
            {
                nuitrack.UserFrame frame = NuitrackManager.sensorsData[sensorId].UserFrame;
                
                if (frame == null || frame.Cols == 0 || frame.Rows == 0)
                    return null;

                return textureMode switch
                {
                    TextureMode.RenderTexture => frame.ToRenderTexture(UsersColors, textureCache),
                    TextureMode.Texture2D => frame.ToTexture2D(UsersColors, textureCache),
                    TextureMode.Texture => frame.ToTexture(UsersColors, textureCache),
                    _ => null,
                };
            }
        }
    }
}