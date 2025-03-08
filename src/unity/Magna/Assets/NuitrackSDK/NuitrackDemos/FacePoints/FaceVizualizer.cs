using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NuitrackSDK.NuitrackDemos.FacePoints
{
    public class FaceVizualizer : MonoBehaviour
    {
        [SerializeField] GameObject facePoint;
        [SerializeField] Transform faceCanvas;
        List<Transform> facePoints = new List<Transform>();
        bool spawned;

        void Update()
        {
            if (NuitrackManager.sensorsData[0].Users.Current != null && NuitrackManager.sensorsData[0].Users.Current.Face != null)
            {
                if (spawned == false)
                {
                    spawned = true;
                    for (int i = 0; i < NuitrackManager.sensorsData[0].Users.Current.Face.landmark.Length; i++)
                    {
                        facePoint.GetComponentInChildren<Text>().text = i.ToString();
                        facePoints.Add(Instantiate(facePoint, faceCanvas).transform);
                    }
                }
                else
                {
                    for (int i = 0; i < NuitrackManager.sensorsData[0].Users.Current.Face.landmark.Length; i++)
                    {
                        facePoints[i].position = new Vector2(NuitrackManager.sensorsData[0].Users.Current.Face.landmark[i].x * Screen.width, Screen.height - NuitrackManager.sensorsData[0].Users.Current.Face.landmark[i].y * Screen.height);
                    }
                }
            }
        }
    }
}
