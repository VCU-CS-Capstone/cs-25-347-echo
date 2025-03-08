using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

using System;
using System.Linq;
using System.IO;

using NuitrackSDKEditor.ErrorSolver;
using System.Collections.Generic;


namespace NuitrackSDKEditor.Wizards
{
    public class NuitrackActivationWizard : Wizard
    {
        bool haveDevices = false;
        List<string> devicesNames = null;
        List<nuitrack.device.ActivationStatus> deviceActivations = null;

        public static void Open()
        {
            Vector2 windowSize = new Vector2(500, 300);

            NuitrackActivationWizard window = GetWindow<NuitrackActivationWizard>();
            window.titleContent = new GUIContent("Nuitrack Activation Wizard");
            window.minSize = windowSize;
            window.maxSize = windowSize;

            window.Show();
        }

        void UpdateSensorState()
        {
            if (!EditorApplication.isPlaying)
                haveDevices = NuitrackChecker.HaveConnectDevices(out devicesNames, out deviceActivations);
        }

        void OnEnable()
        {
            UpdateSensorState();

            bool haveActivated = deviceActivations != null && deviceActivations.Count(k => k != nuitrack.device.ActivationStatus.NONE) > 0;
            bool withDeviceInfo = !EditorApplication.isPlaying && (!haveDevices || haveActivated);

            menuId = 1;
            drawMenus = new Dictionary<int, UnityAction>()
            {
                { 1, DrawStartMenu },
                { 2, DrawOpenActivationTool },
                { 3, DrawComplate }
            };

            if (withDeviceInfo)
            {
                menuId = 0;
                drawMenus.Add(0, DrawAwakeMenu);
            }
        }

        protected void NextMenu()
        {
            if (menuId < drawMenus.Count)
                menuId++;
        }

        protected void PreviewMenu()
        {
            if (menuId > 0)
                menuId--;
        }

        protected override void Back()
        {
            PreviewMenu();
        }

        void DrawAwakeMenu()
        {
            if (!haveDevices)
            {
                DrawHeader("No connected sensors found!");
                DrawMessage("Check whether the sensor is connected and whether this model is included in the list of supported models.");

                UnityAction updateAction = delegate
                {
                    UpdateSensorState();

                    bool haveActivated = deviceActivations != null && deviceActivations.Count(k => k != nuitrack.device.ActivationStatus.NONE) > 0;
                    bool withDeviceInfo = !EditorApplication.isPlaying && (!haveDevices || haveActivated);

                    if (!withDeviceInfo)
                        NextMenu();
                };

                Color updateColor = Color.Lerp(mainColor, GUI.color, 0.5f);
                DrawButtons("Skip", Color.gray, NextMenu, false, updateAction, "Update", updateColor);
            }
            else
            {
                DrawHeader("You have activated licenses!");

                string message = "";
                int index = 1;

                for(int i = 0; i < devicesNames.Count; i++)
                    message += string.Format("{0}. {1}: license type - {2}\n", index++, devicesNames[i], deviceActivations[i]);

                DrawMessage(message);

                DrawButtons("Continue anyway", mainColor, NextMenu, false);
            }
        }

        void DrawStartMenu()
        {
            DrawHeader("Let's get a license!");
            DrawMessage("Click \"Get a license\", fill out a simple form, and the key will be in your mail!", "StartBackground");

            UnityAction openWebSite = delegate
            {
                Application.OpenURL("https://nuitrack.com/#pricing");
                NextMenu();
            };

            DrawButtons("Get a license!", mainColor, openWebSite, false, NextMenu);
        }

        void OpenActivationtool()
        {
#if !NUITRACK_PORTABLE
            string nuitrackHomePath = Environment.GetEnvironmentVariable("NUITRACK_HOME");
            if (nuitrackHomePath == null)
                return;
            string workingDir = Path.Combine(nuitrackHomePath, "activation_tool");
            string path = Path.Combine(workingDir, "Nuitrack.exe");
            ProgramStarter.Run(path, workingDir, true);
#else
            PortableActivation.Init();
            Close();
#endif
        }

        void DrawOpenActivationTool()
        {
            DrawHeader("Let's activate Nuitrack!");
            DrawMessage("After the key is in your mail, open the activation-tool, select the sensor, enter and activate the key.", "ActivationBackground");

            UnityAction openActivationTool = delegate
            {
                OpenActivationtool();
                NextMenu();
            };

            DrawButtons("I have the key. Open the Activation-tool!", mainColor, openActivationTool, true, NextMenu);
        }

        void DrawComplate()
        {
            DrawHeader("The fun continues!");
            DrawMessage("After activating the license, you can continue!", "ComplateBackground");

            UnityAction openActivationTool = delegate
            {
                if (EditorApplication.isPlaying)
                {
                    NuitrackManager.Instance.StopNuitrack();
                    NuitrackManager.Instance.StartNuitrack();
                }
                
                Close();
            };

            DrawButtons("I have activated the license, continue!", mainColor, openActivationTool, true);
        }
    }
}