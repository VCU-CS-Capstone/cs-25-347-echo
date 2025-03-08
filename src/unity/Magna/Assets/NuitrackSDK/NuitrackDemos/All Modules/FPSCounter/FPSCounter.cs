using UnityEngine;

namespace NuitrackSDK.NuitrackDemos
{
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField] bool showNuitrackInfo;
        [SerializeField] float measureTime = 1f;
        [SerializeField] float delayStartTime = 3f;
        [SerializeField] float minSkelFpsWarning = 25f;

        float min_fps;
        float avg_fps;
        TextMesh tm;

        float timer = 0f;
        int frames = 0;

        ulong lastUserTimeStamp = 0;
        int userUpdates;
        int uups;

        ulong lastSkelTimeStamp = 0;
        int skelUpdates;
        int sups;

        ulong lastHandTimeStamp = 0;
        int handUpdates;
        int hups;

        void Start()
        {
            tm = gameObject.GetComponent<TextMesh>();
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            ++frames;

            if (min_fps == 0)
            {
                min_fps = 1f / Time.unscaledDeltaTime;
            }
            else
            {
                float fps = 1f / Time.unscaledDeltaTime;
                if (fps < min_fps) min_fps = fps;
            }

            string processingTimesInfo = "";
            if (timer > measureTime)
            {
                avg_fps = frames / timer;

                frames = 0;
                min_fps = 0f;
                timer = 0f;

                sups = skelUpdates;
                skelUpdates = 0;

                hups = handUpdates;
                handUpdates = 0;

                uups = userUpdates;
                userUpdates = 0;
            }

            processingTimesInfo += /*"avg: " + */"APP FPS: " + avg_fps.ToString("f2")/*"; min: " + min_fps.ToString("0")*/ + "\n";

            if (NuitrackManager.sensorsData.Count == 0)
                return;

            if (showNuitrackInfo)
            {
                if (NuitrackManager.sensorsData[0].UserTracker != null)
                {
                    if (NuitrackManager.sensorsData[0].UserFrame != null && NuitrackManager.sensorsData[0].UserFrame.Timestamp > lastUserTimeStamp)
                    {
                        ++userUpdates;
                        lastUserTimeStamp = NuitrackManager.sensorsData[0].UserFrame.Timestamp;
                    }

                    processingTimesInfo += "User FPS: " + uups.ToString("f2") + "\n";
                }


                if (NuitrackManager.sensorsData[0].SkeletonTracker != null)
                {
                    if (NuitrackManager.sensorsData[0].SkeletonTracker.GetSkeletonData().Timestamp > lastSkelTimeStamp)
                    {
                        ++skelUpdates;
                        lastSkelTimeStamp = NuitrackManager.sensorsData[0].SkeletonTracker.GetSkeletonData().Timestamp;
                    }

                    processingTimesInfo += "Skeleton FPS: " + sups.ToString("f2") + "\n";
                    if (sups < minSkelFpsWarning && Time.timeSinceLevelLoad > delayStartTime)
                    {
                        processingTimesInfo += "<color=yellow>Low skeleton performance</color>" + "\n";
                        Debug.LogWarning("Low skeleton performance. Nuitrack AI Skeleton Tracking has high CPU requirements. " +
                            "If you are sure that your PC has a high-performance CPU, try contacting Nuitrack technical support. " +
                            "You can also try using a standard algorithm (not AI), it has much less system requirements.");
                    }
                }

                if (NuitrackManager.sensorsData[0].HandTracker != null)
                {
                    if (NuitrackManager.sensorsData[0].HandTracker.GetHandTrackerData().Timestamp > lastHandTimeStamp)
                    {
                        ++handUpdates;
                        lastHandTimeStamp = NuitrackManager.sensorsData[0].HandTracker.GetHandTrackerData().Timestamp;
                    }
                    processingTimesInfo += "Hand FPS: " + hups.ToString("f2") + "\n";
                }
            }

            tm.text = processingTimesInfo;
        }
    }
}