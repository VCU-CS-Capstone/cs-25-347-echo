using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Events;

using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace NuitrackSDKEditor.Wizards
{
    public class NuitrackProblemWizard : Wizard
    {
        List<int> menuStack = null;

        string logData = "";

        public static void Open()
        {
            Vector2 windowSize = new Vector2(640, 480);

            NuitrackProblemWizard window = GetWindow<NuitrackProblemWizard>();
            window.titleContent = new GUIContent("Nuitrack Problem Wizard");
            window.minSize = windowSize;
            window.maxSize = windowSize;

            window.Show();
        }

        void OnEnable()
        {
            if(menuStack == null)
                menuStack = new List<int> { 0 };

            drawMenus = new Dictionary<int, UnityAction>()
            {
                { 0, DrawStartScreen },
                { 1, DrawAllModulesTest },
                { 2, DrawNuitrackSampleTest },
                { 3, DrawAnotherSkeletonTrackers },
                { 4, DrawAnotherHardwareTest },
                { 5, DrawFinishScreen },
                { 6, DrawBadTracking },
                { 7, DrawUnityCrash },
            };
        }
        
        void OpenMenu(UnityAction menu, bool stacking = true)
        {
            menuId = drawMenus.FirstOrDefault(x => x.Value == menu).Key;
            if (stacking)
                menuStack.Add(menuId);
        }

        protected override void Back()
        {
            menuStack.RemoveAt(menuStack.Count - 1);

            int previewMenuID = menuStack[menuStack.Count - 1];
            OpenMenu(drawMenus[previewMenuID], false);
        }

        void DrawStartScreen()
        {
            DrawHeader("Hello");
            DrawMessage("Do you have a problem? Now let's try to solve");

            UnityAction goToBadTracking = delegate
            {
                logData = "Bad tracking";
                OpenMenu(DrawBadTracking);
            };

            UnityAction unityCrash = delegate
            {
                OpenMenu(DrawUnityCrash);
            };

            DrawButtons("Bad tracking", mainColor, goToBadTracking, false, unityCrash, "Something is not working", mainColor);
        }

        void DrawUnityCrash()
        {
            DrawHeader("Crash");
            DrawMessage("Unity frequently crashes?");

            UnityAction goToFinish = delegate
            {
                logData = "Crash";
                OpenMenu(DrawFinishScreen);
            };

            UnityAction goToAllModulesTest = delegate
            {
                OpenMenu(DrawAllModulesTest);
            };

            DrawButtons("Yes", mainColor, goToFinish, true, goToAllModulesTest, "No");
        }

        void DrawAllModulesTest()
        {
            DrawHeader("Check All Modules Scene");
            DrawMessage("Check Nuitrack and Sensor on AllModules scene. Open this scene and click \"Play\", then follow the on-screen instructions");

            UnityAction openTestScene = delegate
            {
                if (EditorApplication.isPlaying)
                    EditorApplication.ExitPlaymode();
                else
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(Path.Combine("Assets", "NuitrackSDK", "NuitrackDemos", "All Modules", "AllModulesScene.unity"));
                        EditorApplication.EnterPlaymode();
                    }
                }
            };

            UnityAction skip = delegate
            {
                OpenMenu(DrawNuitrackSampleTest);
            };

            string buttonName = "Play Test Scene";
            if (EditorApplication.isPlaying)
                buttonName = "Stop Test Scene";

            DrawButtons(buttonName, mainColor, openTestScene, true, skip, "Nothing help");
        }

        void DrawNuitrackSampleTest()
        {
            DrawHeader("Check Nuitrack Sample Test");
            DrawMessage(
                "Check Nuitrack and Sensor in Nuitrack App. Select sensor and select \"Try Nuitrack!\" (preferably several times)\n" +
                "If the sample crashes, don't forget to mention it when contacting technical support");

            UnityAction openTestScene = delegate
            {
                NuitrackMenu.OpenNuitrackApp();
            };

            UnityAction next = delegate
            {
                OpenMenu(DrawAnotherSkeletonTrackers);
            };

            DrawButtons("Open Nuitrack App", mainColor, openTestScene, true, next, "Next");
        }

        void DrawAnotherSkeletonTrackers()
        {
            DrawHeader("Another Skeleton Tracker");
            DrawMessage(
                "Have you tried installing another skeleton tracking software before?\n" +
                "If so, then try:\n" +
                "- Delete them all.\n" +
                "- Also check if there are no relevant environment variables left, delete them too\n" +
                "- Restart your PC\n" +
                "- Try testing AllModulesScene again\n");

            UnityAction next = delegate
            {
                OpenMenu(DrawAnotherHardwareTest);
            };

            DrawButtons("I'm sure I don't have any other skeleton trackers installed", default, next, true);
        }

        void DrawAnotherHardwareTest()
        {
            DrawHeader("Check on another hardware\\software");
            DrawMessage(
                "- Try to update\\rollback sensor drivers(firmware​)\n" +
                "- Perhaps the problem is related to the sensor(perhaps with a specific instance of sensor or a wire). If possible, try to run with other sensors\n" +
                "- Try to run on other computers (the more different the configuration of computers will be the better)\n" +
                "- Try to update\\rollback Unity\n" +
                "- Try to update\\rollback Nuitrack\n" +
                "- Try to run the project on a \"clean\" version of the operating system\n");

            UnityAction finish = delegate
            {
                logData = "Unexpected Error";
                OpenMenu(DrawFinishScreen);
            };

            DrawButtons("I tested it, the problem persisted", default, finish, true);
        }

        void DrawBadTracking()
        {
            DrawHeader("Bad tracking");
            DrawMessage(
                "The quality of tracking directly depends on the data received from the sensor. The accuracy of the sensors may depend on many factors:\n" +
                "1. To ensure the best possible data quality, <color=red>read the recommendations of your sensor supplier</color>. You can also read our \"General Preparation\" guide\n" +
                "2. Good quality of work requires good visibility of all the \"Joints\" of the person standing in front of the sensor:\n" +
                "  * Free up so much space so that there are no other objects or walls around a person within a radius of two meters\n" +
                "  * Make sure that no objects overlap the person\n" +
                "  * Recommended to stand at a distance of 1.5 - 3.0 meters from the sensor\n" +
                "3. Nuitrack has two skeletonization algorithms \"Standard\" (lightweight, suitable for all platforms) and NuitrackAI (PC only). You can try them both on the AllModulesScene stage.");

            UnityAction goToGeneralPrepartations = delegate
            {
                Application.OpenURL("https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/General_preparations.md");
            };

            UnityAction goToNuitrackAI = delegate
            {
                Application.OpenURL("https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/Nuitrack_AI.md");
            };

            DrawButtons("Open General Preparation Guide", mainColor, goToGeneralPrepartations, true, goToNuitrackAI, "About NuitrackAI", mainColor);
        }

        void DrawFinishScreen()
        {
            DrawHeader("A difficult case");
            DrawMessage(
                "1. Look <color=red>troubleshooting page</color>\n" +
                "2. If nothing help, contact support or ask for advice from the community\n" +
                "Provide as much <color=red>information</color> as possible about your hardware and describe the problem in detail (if needed <color=red>attach editor or player log</color>).\n" +
                "If this is your own project then do not forget to <color=red>attach it or a minimally reproducible example</color>" +
                "(upload to a file sharing site with access by link)");


            UnityAction troubleshooting = delegate
            {
                Application.OpenURL("https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/Troubleshooting.md");
            };

            UnityAction log = delegate
            {
                Application.OpenURL("https://docs.unity3d.com/Manual/LogFiles.html");
            };

            UnityAction grabLog = delegate
            {
                LogGrabber.LogGrab(logData);
            };
#if UNITY_EDITOR_WIN
            DrawButtons("Collect Logs for Support", mainColor, grabLog, true, troubleshooting, "Open troubleshooting page", mainColor);
#else
            DrawButtons("Where is the log?", mainColor, log, true, troubleshooting, "Open troubleshooting page", mainColor);
#endif
        }
    }
}