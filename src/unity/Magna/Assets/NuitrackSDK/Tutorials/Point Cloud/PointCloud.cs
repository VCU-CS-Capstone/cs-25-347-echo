using UnityEngine;

namespace NuitrackSDK.Tutorials.PointCloud
{
    [AddComponentMenu("NuitrackSDK/Tutorials/Point Cloud/Point Cloud")]
    public class PointCloud : MonoBehaviour
    {
        [SerializeField] Material depthMat = null; // Material (optional, not required for coloring each cube)
        [SerializeField] int hRes;
        [SerializeField] Color defaultColor = Color.gray;
        [SerializeField] GameObject pointMesh;
        [SerializeField] float meshScaling = 1f;

        ulong lastFrameID = ulong.MaxValue;
        int frameStep;
        float depthToScale;

        Texture2D depthTexture;
        Color[] depthColors;
        GameObject[] points;

        bool initialized = false;

        void Start()
        {
            if (!initialized) Initialize();
        }

        void Initialize()
        {
            initialized = true;

            nuitrack.OutputMode mode = NuitrackManager.DepthSensor.GetOutputMode();

            frameStep = mode.XRes / hRes;
            if (frameStep <= 0) frameStep = 1;
            hRes = mode.XRes / frameStep;

            InitMeshes(
              (mode.XRes / frameStep),  // Width
              (mode.YRes / frameStep),  // Height
              mode.HFOV);
        }

        void InitMeshes(int cols, int rows, float hfov)
        {
            depthColors = new Color[cols * rows];
            points = new GameObject[cols * rows];

            depthTexture = new Texture2D(cols, rows, TextureFormat.RFloat, false);
            depthTexture.filterMode = FilterMode.Point;
            depthTexture.wrapMode = TextureWrapMode.Clamp;
            depthTexture.Apply();

            if (depthMat != null)
                depthMat.mainTexture = depthTexture;

            int pointId = 0;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    points[pointId++] = Instantiate(pointMesh, transform);
                }
            }
        }

        void Update()
        {
            if (NuitrackManager.DepthFrame != null)
            {
                var depthFrame = NuitrackManager.DepthFrame;
                bool haveNewFrame = (lastFrameID != depthFrame.ID);

                if (haveNewFrame)
                {
                    ProcessFrame(depthFrame);
                    lastFrameID = depthFrame.ID;
                }
            }
        }

        void ProcessFrame(nuitrack.DepthFrame depthFrame)
        {
            int pointIndex = 0;

            for (int i = 0; i < depthFrame.Rows; i += frameStep)
            {
                for (int j = 0; j < depthFrame.Cols; j += frameStep)
                {
                    ushort depthVal = depthFrame[i, j];

                    if (depthVal == 0)
                    {
                        points[pointIndex].SetActive(false);
                    }
                    else
                    {
                        points[pointIndex].SetActive(true);

                        // Depth to world coordinates
                        Vector3 newPos = NuitrackManager.DepthSensor
                            .ConvertProjToRealCoords(j, i, depthVal)
                            .ToVector3();

                        points[pointIndex].transform.position = newPos;

                        // Scale based on distance
                        float distancePoints = Vector3.Distance(
                            newPos,
                            NuitrackManager.DepthSensor.ConvertProjToRealCoords(j + 1, i, depthVal).ToVector3());

                        depthToScale = distancePoints * depthFrame.Cols / hRes;
                        points[pointIndex].transform.localScale = Vector3.one * meshScaling * depthToScale;

                        // Color based on depth (Red = close, Blue = far)
                        float normalized = Mathf.InverseLerp(500f, 4000f, depthVal); // Adjust range to your sensor
                        Color depthColor = Color.HSVToRGB(0.66f - normalized * 0.66f, 1f, 1f); // Blue → Red gradient
                        points[pointIndex].GetComponent<Renderer>().material.color = depthColor;

                        depthColors[pointIndex] = new Color(depthVal / 16384f, 0f, 0f, 1f); // Optional grayscale
                    }

                    ++pointIndex;
                }
            }

            depthTexture.SetPixels(depthColors);
            depthTexture.Apply();
        }
    }
}
