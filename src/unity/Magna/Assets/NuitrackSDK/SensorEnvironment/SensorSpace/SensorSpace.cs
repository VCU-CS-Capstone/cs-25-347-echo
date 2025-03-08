using UnityEngine;

using NuitrackSDK.Frame;


namespace NuitrackSDK.SensorEnvironment
{
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu("NuitrackSDK/SensorEnvironment/Sensor Space")]
    public class SensorSpace : MonoBehaviour
    {
        Camera sceneCamera;

        [Header("Camera")]
        [SerializeField] bool cameraFovAlign = true;

        [Tooltip ("(optional) If not specified, the screen size is used.")]

        [SerializeField] Canvas viewCanvas;
        RectTransform canvasRect;

        ulong lastTimeStamp = 0;

        [Header("Floor")]
        [SerializeField] bool floorTracking = false;

        [SerializeField] Transform sensorSpace;

        [SerializeField, Range(0.001f, 1f)] float deltaHeight = 0.1f;
        [SerializeField, Range(0.1f, 90f)] float deltaAngle = 3f;
        [SerializeField, Range(0.1f, 32f)] float floorCorrectionSpeed = 8f;

        Plane floorPlane;

        Vector3 localCameraPosition;

        public Camera Camera
        {
            get
            {
                if (sceneCamera == null)
                    sceneCamera = GetComponent<Camera>();

                return sceneCamera;
            }
        }

        public float HeightToFloor
        {
            get
            {
                if (NuitrackManager.sensorsData[0].Floor == null)
                    return 0;

                Plane floorPlane = (Plane)NuitrackManager.sensorsData[0].Floor;

                return floorPlane.GetDistanceToPoint(Vector3.zero);
            }
    }

        RectTransform CanvasRect
        {
            get
            {
                if (viewCanvas != null && canvasRect == null)
                    canvasRect = viewCanvas.GetComponent<RectTransform>();
                
                return canvasRect;
            }
        }

        void Update()
        {
            if (!NuitrackManager.Instance.NuitrackInitialized)
                return;

            if (cameraFovAlign)
            {
                if (NuitrackManager.sensorsData[0].DepthFrame == null || NuitrackManager.sensorsData[0].DepthFrame.Timestamp == lastTimeStamp)
                    return;

                lastTimeStamp = NuitrackManager.sensorsData[0].DepthFrame.Timestamp;

                NuitrackManager_onColorUpdate();
            }

            if (floorTracking)
                UpdateFloor();
        }

        float ViewWidth
        {
            get
            {
                if (CanvasRect != null)
                    return CanvasRect.rect.width;
                else
                    return Screen.width;
            }
        }

        float ViewHeight
        {
            get
            {
                if (CanvasRect != null)
                    return CanvasRect.rect.height;
                else
                    return Screen.height;
            }
        }

        void NuitrackManager_onColorUpdate()
        {
            if (ViewWidth / ViewHeight > FrameUtils.DepthToTexture.AspectRatio)
                Camera.fieldOfView = FrameUtils.DepthToTexture.VFOV;
            else
            {
                nuitrack.OutputMode mode = NuitrackManager.sensorsData[0].DepthSensor.GetOutputMode();

                float targetAspectRatio = ViewHeight / ViewWidth;
                float vFOV = 2 * Mathf.Atan(Mathf.Tan(mode.HFOV * 0.5f) * targetAspectRatio) * Mathf.Rad2Deg;

                Camera.fieldOfView = vFOV;
            }
        }

        void UpdateFloor()
        {
            if (NuitrackManager.sensorsData[0].Floor == null)
                return;

            Plane newFloor = (Plane)NuitrackManager.sensorsData[0].Floor;

            if (Mathf.Approximately(newFloor.normal.sqrMagnitude, 0))
                return;

            if (floorPlane.Equals(default(Plane)))
                floorPlane = newFloor;

            float newFloorHeight = newFloor.GetDistanceToPoint(Vector3.zero);
            float floorHeight = floorPlane.GetDistanceToPoint(Vector3.zero);

            if (Vector3.Angle(newFloor.normal, floorPlane.normal) >= deltaAngle || Mathf.Abs(newFloorHeight - floorHeight) >= deltaHeight)
                floorPlane = newFloor;

            Vector3 reflectNormal = sensorSpace.TransformDirection(Vector3.Reflect(-floorPlane.normal, Vector3.up));
            Vector3 forward = sensorSpace.forward;
            Vector3.OrthoNormalize(ref reflectNormal, ref forward);

            Quaternion targetRotation = Quaternion.LookRotation(forward, reflectNormal);
            sceneCamera.transform.rotation = Quaternion.Lerp(sceneCamera.transform.rotation, targetRotation, Time.deltaTime * floorCorrectionSpeed);

            if (localCameraPosition.Equals(default))
            {
                localCameraPosition = Vector3.up * floorHeight;
                sceneCamera.transform.position = sensorSpace.TransformPoint(localCameraPosition);
            }
            else
            {
                localCameraPosition = Vector3.up * floorHeight;
                Vector3 worldCameraPosition = sensorSpace.TransformPoint(localCameraPosition);
                sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, worldCameraPosition, Time.deltaTime * floorCorrectionSpeed);
            }
        }
    }
}