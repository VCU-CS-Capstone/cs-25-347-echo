#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Little utility for opening the Scene view in fullscreen. Will be opened on whatever Unity thinks is the "main"
/// monitor at the moment. The hotkey will toggle the window; however, if for some reason this breaks, fullscreen
/// windows can be closed via Alt+F4 as long as the editor is not in play mode.
/// </summary>
/// <remarks>
/// Confirmed to work in Unity 2019 and 2020. May work in earlier and later versions. No promises.
/// </remarks>
public static class FullscreenSceneView
{
    static readonly Type SceneViewType = Type.GetType("UnityEditor.SceneView,UnityEditor");
    static readonly PropertyInfo ShowToolbarProperty = SceneViewType.GetProperty("showToolbar", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly object False = false; // Only box once. This is a matter of principle.

    static EditorWindow instance;

    [MenuItem("Window/General/Scene (Fullscreen) %#&2", priority = 2)]
    public static void Toggle()
    {
        if (SceneViewType == null)
        {
            Debug.LogError("SceneView type not found.");
            return;
        }

        if (ShowToolbarProperty == null)
        {
            Debug.LogWarning("SceneView.showToolbar property not found.");
        }

        if (instance != null)
        {
            instance.Close();
            instance = null;
        }
        else
        {
            instance = (EditorWindow) ScriptableObject.CreateInstance(SceneViewType);

            ShowToolbarProperty?.SetValue(instance, False);

            var desktopResolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
            var halfScreenWidth = desktopResolution.x / 2f;
            var fullscreenRect = new Rect(halfScreenWidth, 0f, halfScreenWidth, desktopResolution.y); // Set position to right half of screen
            instance.ShowPopup();
            instance.position = fullscreenRect;
            instance.Focus();
        }
    }
}

#endif
