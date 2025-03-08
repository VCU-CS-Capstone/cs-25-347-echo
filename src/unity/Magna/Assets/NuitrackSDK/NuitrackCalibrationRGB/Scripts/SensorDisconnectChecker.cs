using UnityEngine;
using nuitrack;


namespace NuitrackSDK.Calibration
{
    public class SensorDisconnectChecker : MonoBehaviour
    {
        public delegate void ConnectionStatusChange();
        static public event ConnectionStatusChange SensorConnectionTimeOut;
        static public event ConnectionStatusChange SensorReconnected;

        bool connectionProblem = false;

        void OnEnable()
        {
            if (NuitrackManager.Instance.NuitrackInitialized)
                Nuitrack.onIssueUpdateEvent += NoConnectionIssue;
        }

        void Update()
        {
            if (!NuitrackManager.Instance.NuitrackInitialized)
                return;

            if (NuitrackManager.sensorsData[0].DepthFrame != null)
            {
                if (connectionProblem && SensorReconnected != null)
                    SensorReconnected();
                connectionProblem = false;
            }
        }

        void NoConnectionIssue(nuitrack.issues.IssuesData issData)
        {
            if (issData.GetIssue<nuitrack.issues.SensorIssue>() != null)
            {
                if (SensorConnectionTimeOut != null)
                    SensorConnectionTimeOut();
                connectionProblem = true;
            }
            else
            {
                if (connectionProblem && SensorReconnected != null)
                    SensorReconnected();
                connectionProblem = false;
            }
        }

        void OnDisable()
        {
            if (NuitrackManager.Instance.NuitrackInitialized)
                Nuitrack.onIssueUpdateEvent -= NoConnectionIssue;
        }
    }
}
