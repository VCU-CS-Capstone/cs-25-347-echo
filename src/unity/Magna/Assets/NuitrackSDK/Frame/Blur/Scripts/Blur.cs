using UnityEngine;
using UnityEngine.Events;


namespace NuitrackSDK.Frame
{
    [AddComponentMenu("NuitrackSDK/Frame/Blur")]
    public class Blur : MonoBehaviour
    {
        int blurKernelID;

        [SerializeField, NuitrackSDKInspector] bool useStaticMainTexture;
        [SerializeField, NuitrackSDKInspector] Texture staticMainTexture;
        Texture overrideMainTexture = null;

        [SerializeField, Range(0, 16)] int radius = 1;

        [SerializeField] ComputeShader computeShader;
        [SerializeField] UnityEvent<Texture> onFrameUpdate;

        ComputeShader instance = null;
        uint x, y, z;

        RenderTexture result = null;

        void Awake()
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogWarning("Compute shaders are not supported. A software conversion will be used (may cause performance issues).");
                return;
            }

            instance = Instantiate(computeShader);
            blurKernelID = instance.FindKernel("Blur");
            instance.GetKernelThreadGroupSizes(blurKernelID, out x, out y, out z);
        }

        void OnDestroy()
        {
            if (instance != null)
                Destroy(instance);

            if (result != null)
            {
                Destroy(result);
                result = null;
            }
        }

        public void MainTexture(Texture texture)
        {
            overrideMainTexture = texture;
        }

        void Update()
        {
            Texture mainTexture = useStaticMainTexture ? staticMainTexture : overrideMainTexture;

            if (mainTexture == null)
                return;

            RenderTexture output = DisptachWithSource(mainTexture);

            onFrameUpdate.Invoke(output);
        }

        RenderTexture DisptachWithSource(Texture source)
        {
            if (result == null || result.width != source.width || result.height != source.height)
            {
                result = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                result.enableRandomWrite = true;
                result.wrapMode = TextureWrapMode.Clamp;
                result.Create();

                instance.SetVector("_Size", new Vector2(source.width, source.height));
                instance.SetTexture(blurKernelID, "_Result", result);
            }

            instance.SetTexture(blurKernelID, "_Source", source);
            

            instance.SetInt("_Radius", radius);
            instance.Dispatch(blurKernelID, source.width / (int)x, source.height / (int)y, (int)z);

            return result;
        }
    }
}