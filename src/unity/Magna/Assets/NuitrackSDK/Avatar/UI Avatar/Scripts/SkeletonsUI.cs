using UnityEngine;

using System.Collections.Generic;
using nuitrack;


namespace NuitrackSDK.Avatar
{
    [AddComponentMenu("NuitrackSDK/Avatar/UI/Skeletons UI")]
    public class SkeletonsUI : MonoBehaviour
    {
        public int sensorId = 0;

        [SerializeField] RectTransform spawnRectTransform;

        [SerializeField, Range(0, 6)] int skeletonCount = 6;         //Max number of skeletons tracked by Nuitrack
        [SerializeField] UIAvatar skeletonAvatar;

        List<UIAvatar> avatars = new List<UIAvatar>();

        void Start()
        {
            for (int i = 0; i < skeletonCount; i++)
            {
                GameObject newAvatar = Instantiate(skeletonAvatar.gameObject, spawnRectTransform);
                UIAvatar skeleton = newAvatar.GetComponent<UIAvatar>();
                skeleton.UserID = i + 1;
                skeleton.sensorId = sensorId;
                avatars.Add(skeleton);
            }

            NuitrackManager.sensorsData[sensorId].SkeletonTracker.SetNumActiveUsers(skeletonCount);
        }

        void Update()
        {
            for (int i = 0; i < avatars.Count; i++)
            {
                int id = i + 1;
                UserData user = NuitrackManager.sensorsData[sensorId].Users.GetUser(id);

                if (user != null && user.Skeleton != null)
                {
                    avatars[i].gameObject.SetActive(true);
                    avatars[i].Process(user);
                }
                else
                {
                    avatars[i].gameObject.SetActive(false);
                }
            }
        }
    }
}