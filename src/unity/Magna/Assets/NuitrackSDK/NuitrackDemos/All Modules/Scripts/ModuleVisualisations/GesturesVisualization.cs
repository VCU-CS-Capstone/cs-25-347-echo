using UnityEngine;
using System;


namespace NuitrackSDK.NuitrackDemos
{
    public class GesturesVisualization : MonoBehaviour
    {
        public int sensorId;
        [SerializeField] ExceptionsLogger exceptionsLogger;

        void Update()
        {
            Users users = NuitrackManager.sensorsData[sensorId].Users;

            foreach (UserData user in users)
            {
                if (user != null && user.GestureType != null)
                {
                    nuitrack.GestureType gesture = user.GestureType.Value;

                    string newEntry =
                        "User " + user.ID + ": " +
                        Enum.GetName(typeof(nuitrack.GestureType), (int)gesture);
                    exceptionsLogger.AddEntry(newEntry);
                }
            }
        }

        void Start()
        {
            if (exceptionsLogger == null)
                exceptionsLogger = FindObjectOfType<ExceptionsLogger>();
        }
    }
}