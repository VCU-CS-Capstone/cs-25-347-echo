using UnityEngine;
using System;

#if UNITY_ANDROID && UNITY_2018_1_OR_NEWER && !UNITY_EDITOR
using UnityEngine.Android;
#endif

using NuitrackSDK.ErrorSolver;
using NuitrackSDK.Loader;


namespace NuitrackSDK.NuitrackDemos
{
    public class NuitrackModules : MonoBehaviour
    {
        [SerializeField] GameObject depthUserVisualizationPrefab;
        [SerializeField] GameObject depthUserMeshVisualizationPrefab;
        [SerializeField] GameObject skeletonsVisualizationPrefab;
        [SerializeField] GameObject gesturesVisualizationPrefab;
        [SerializeField] GameObject handTrackerVisualizationPrefab;

        [SerializeField] GameObject smoothMoveSlider;

        [SerializeField] GameObject standardCamera, threeViewCamera;
        [SerializeField] GameObject indirectAvatar, directAvatar, indirectAvatarMan, directAvatarMan;

        [SerializeField] GameObject sensorFrame;

        int sensorFrameId = 0;

        GameObject root;
        GameObject skelVis;
        int skelVisId;

        public void SwitchCamera()
        {
            standardCamera.SetActive(!standardCamera.activeSelf);
            threeViewCamera.SetActive(!threeViewCamera.activeSelf);
        }

        void Awake()
        {
            NuitrackInitState state = NuitrackManager.Instance.InitState;
            if (state != NuitrackInitState.INIT_OK && Application.platform == RuntimePlatform.Android)
            {
                string error_message = "Nuitrack native libraries initialization error: " + Enum.GetName(typeof(NuitrackInitState), state);
                NuitrackErrorSolver.CheckError(error_message);
            }
        }

        public void ChangeModules(bool depthOn, bool colorOn, bool userOn, bool skeletonOn, bool handsOn, bool gesturesOn)
        {
            InitTrackers(depthOn, colorOn, userOn, skeletonOn, handsOn, gesturesOn);
        }

        public void SwitchModelByIndex(int id)
        {
            skelVisId = id;
            smoothMoveSlider.SetActive(skelVisId != 0);
            if (!root)
                root = GameObject.Find("Root_1");

            if (root)
                root.SetActive(skelVisId == 0);
            skelVis.SetActive(skelVisId == 0);
            indirectAvatar.SetActive(skelVisId == 1);
            directAvatar.SetActive(skelVisId == 2);
            indirectAvatarMan.SetActive(skelVisId == 3);
            directAvatarMan.SetActive(skelVisId == 4);
        }

        private void InitTrackers(bool depthOn, bool colorOn, bool userOn, bool skeletonOn, bool handsOn, bool gesturesOn)
        {
            if (skelVisId == 0)
            {
                if (root)
                    root.SetActive(true);
                skelVis.SetActive(true);
            }
            if (skelVisId == 1)
                indirectAvatar.SetActive(skeletonOn);
            if (skelVisId == 2)
                directAvatar.SetActive(skeletonOn);
            if (skelVisId == 3)
                indirectAvatarMan.SetActive(skeletonOn);
            if (skelVisId == 4)
                directAvatarMan.SetActive(skeletonOn);
            NuitrackManager.Instance.ChangeModulesState(skeletonOn, handsOn, depthOn, colorOn, gesturesOn, userOn);
        }

        public void InitModules()
        {
            Instantiate(depthUserVisualizationPrefab);
            Instantiate(depthUserMeshVisualizationPrefab);
            skelVis = Instantiate(skeletonsVisualizationPrefab);
            Instantiate(handTrackerVisualizationPrefab);
            Instantiate(gesturesVisualizationPrefab);
        }

        public void SwitchSensorFrame()
        {
            sensorFrameId++;
            if (sensorFrameId > 1)
                sensorFrameId = 0;

            if (sensorFrameId == 1)
            {
                sensorFrame.SetActive(true);
            }
            else if (sensorFrameId == 0)
            {
                sensorFrame.SetActive(false);
            }
        }

        public void SwitchNuitrackAi()
        {
            NuitrackManager.Instance.StopNuitrack();
            NuitrackManager.Instance.UseNuitrackAi = !NuitrackManager.Instance.UseNuitrackAi;
            NuitrackManager.Instance.StartNuitrack();
        }
    }
}
