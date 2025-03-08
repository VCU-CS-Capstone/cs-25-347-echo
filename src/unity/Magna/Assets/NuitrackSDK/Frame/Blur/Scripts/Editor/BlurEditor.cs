using UnityEditor;

using NuitrackSDK.Frame;


namespace NuitrackSDKEditor.Frame
{
    [CustomEditor(typeof(Blur), true)]
    public class BlurEditor : NuitrackSDKEditor
    {
        public override void OnInspectorGUI()
        {
            SerializedProperty useStaticMainTextureProp = serializedObject.DrawPropertyField(
                "useStaticMainTexture",
                "Use static main texture",
                "Specify a static texture to perform the transformation on it. Otherwise, use MainTexture");

            if (useStaticMainTextureProp.boolValue)
            {
                serializedObject.DrawPropertyField("staticMainTexture", "Main texture");
                EditorGUILayout.Space();
            }

            DrawDefaultInspector();
        }
    }
}
