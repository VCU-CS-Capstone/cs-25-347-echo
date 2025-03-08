using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace NuitrackSDKEditor.Wizards
{
    public class LogGrabber : MonoBehaviour
    {
        public static void LogGrab(string data = "")
        {
            string systemTime = DateTime.Now.ToString().Replace(":", "_");
            string supportFolderName = "For support " + systemTime;
            string supportFolderPath = Path.Combine(Application.dataPath, supportFolderName);
            string editorLogFolderPath = Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA"), "Unity", "Editor");
            string nuitrackLogPath = Path.Combine(supportFolderPath, "NuitrackLog.txt");

            try
            {
                AssetDatabase.CreateFolder("Assets", supportFolderName);

                FileUtil.CopyFileOrDirectory(Path.Combine(editorLogFolderPath, "Editor.log"), Path.Combine(supportFolderPath, "Editor.log"));
                if(File.Exists(Path.Combine(editorLogFolderPath, "Editor-prev.log")))
                    FileUtil.CopyFileOrDirectory(Path.Combine(editorLogFolderPath, "Editor-prev.log"), Path.Combine(supportFolderPath, "Editor-prev.log"));

                using (StreamWriter writer = new StreamWriter(nuitrackLogPath, false))
                {
                    writer.WriteLine(data);
                    writer.Flush();
                }

                AssetDatabase.Refresh();

                if(EditorUtility.DisplayDialog(
                    "Success", "Folder \"" + supportFolderName + "\" has been successfully created in \"Assets\" folder and the editor logs have been copied to this folder. " +
                    "Attach logs and ALL necessary files when contacting technical support", "Show in Explorer", "Okay, I got it"))
                {
                    EditorUtility.RevealInFinder(supportFolderPath);
                }
            }
            catch (Exception)
            {
                if(EditorUtility.DisplayDialog("Fail", "It was not possible to automatically collect the editor's logs. Do it manually and send it to support", "Where are the logs?", "OK"))
                {
                    Application.OpenURL("https://docs.unity3d.com/Manual/LogFiles.html");
                }
            }
        }
    }
}
