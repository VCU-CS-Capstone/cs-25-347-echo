using UnityEngine;

namespace NuitrackSDK.Tutorials.ARNuitrack
{
    public class ShaderGraphRender : MonoBehaviour
    {
        [SerializeField] MeshGenerator meshGenerator;

        Material mat;

        public void UpdateTexture(Texture tex)
        {
            if (mat != null)
                mat.SetTexture("_MainTex", tex);
        }

        public void UpdateDepth(Texture tex)
        {
            if (tex == null)
                return;

            if (!meshGenerator.Mesh)
            {
                meshGenerator.Generate(tex.width, tex.height);
                mat = meshGenerator.Material;
            }

            mat.SetTexture("_DepthTex", tex);
        }
    }
}
