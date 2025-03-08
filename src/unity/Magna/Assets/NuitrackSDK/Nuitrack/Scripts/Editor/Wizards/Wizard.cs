using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

using System.Collections.Generic;
using NuitrackSDKEditor;

public abstract class Wizard : EditorWindow
{
    protected Dictionary<int, UnityAction> drawMenus = null;
    protected Color mainColor = Color.green;
    protected int menuId = 0;

    void OnGUI()
    {
        drawMenus[menuId]();
    }

    protected void DrawHeader(string textHeader)
    {
        GUIStyle titleStyle = new GUIStyle(GUI.skin.GetStyle("Label"))
        {
            padding = new RectOffset(30, 20, 20, 20),
            fontStyle = FontStyle.Bold,
            fontSize = 20
        };

        string hedearText = textHeader;

        GUIContent gUIContent = new GUIContent(hedearText);

        Rect rect = GUILayoutUtility.GetRect(gUIContent, titleStyle);

        using (new GUIColor(mainColor))
            GUI.Box(rect, "");

        GUI.Label(rect, gUIContent, titleStyle);
    }

    protected void DrawMessage(string message, string imageName = null, bool withPanel = false)
    {
        if (imageName != null)
        {
            Texture2D background = Resources.Load(imageName) as Texture2D;

            Rect rect = GUILayoutUtility.GetLastRect();

            Rect backgroundRect = new Rect(0, rect.yMax, background.width, background.height);

            GUI.DrawTexture(backgroundRect, background);
        }

        GUIStyle textStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            padding = new RectOffset(30, 30, 30, 30),
            richText = true,
            wordWrap = true,
            fontStyle = FontStyle.Bold,
            fontSize = 14
        };

        GUIContent gUIContent = new GUIContent(message);

        GUILayout.Label(gUIContent, textStyle);
    }

    protected void DrawButtons(string text, Color color, UnityAction action, bool showBackButton = false, UnityAction advAction = null, string advButtonText = "Skip", Color advColor = default)
    {
        GUIContent buttonContent = new GUIContent(text);
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.GetStyle("Button"))
        {
            padding = new RectOffset(10, 10, 10, 10),
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };

        Vector2 buttonSize = buttonStyle.CalcSize(buttonContent);
        Vector2 buttonPosition = new Vector2(position.width - buttonSize.x - 10, position.height - buttonSize.y - 10);

        Rect buttonRect = new Rect(buttonPosition, buttonSize);

        color = Color.Lerp(color, GUI.color, 0.5f);

        using (new GUIColor(color, true))
            if (GUI.Button(buttonRect, buttonContent, buttonStyle))
                action.Invoke();

        if (advAction != null)
        {
            GUIContent advButtonContent = new GUIContent(advButtonText);
            Vector2 advButtonSize = buttonStyle.CalcSize(advButtonContent);
            Vector2 advButtonPosition = new Vector2(buttonRect.xMin - advButtonSize.x - 10, buttonRect.y);

            Rect advButtonRect = new Rect(advButtonPosition, advButtonSize);

            advColor = advColor.Equals(default) ? Color.gray : Color.Lerp(advColor, GUI.color, 0.5f);
            using (new GUIColor(advColor, true))
                if (GUI.Button(advButtonRect, advButtonContent, buttonStyle))
                    advAction.Invoke();
        }

        if (showBackButton)
        {
            GUIContent backButtonContent = new GUIContent("Back");
            Vector2 backButtonSize = buttonStyle.CalcSize(backButtonContent);
            Vector2 backButtonPosition = new Vector2(10, buttonRect.y);

            Rect backButtonRect = new Rect(backButtonPosition, backButtonSize);

            if (GUI.Button(backButtonRect, backButtonContent, buttonStyle))
            {
                Back();
            }
        }
    }

    protected abstract void Back();
}
