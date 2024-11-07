using UnityEngine;
using System.Collections.Generic;

public class AnotherClass : MonoBehaviour
{
    public GameObject spawnObject;
    private UnityServer unityServer;
    private List<GameObject> instantiatedObjects = new List<GameObject>();
    public GameObject Sensorobj;

    void Start()
    {
        // Accessing xPosArray from UnityServer
        unityServer = FindObjectOfType<UnityServer>();
    }

    void Update() {
        if (unityServer != null)
        {
            foreach (GameObject obj in instantiatedObjects)
            {
                Destroy(obj);
            }
            instantiatedObjects.Clear();

            float[] xPosArray = unityServer.xPosArray;
            float[] yPosArray = unityServer.yPosArray;
            float[] zPosArray = unityServer.zPosArray;

            

            for (int i = 0; i < xPosArray.Length; i++) {
                Vector3 objCoordinate = new Vector3(-xPosArray[i], 0 - 0.227f,  -yPosArray[i] - 0.57f);
                GameObject obstacle = Instantiate(spawnObject, objCoordinate, Quaternion.identity);
                instantiatedObjects.Add(obstacle);
            }
        }
        else
        {
            Debug.LogError("UnityServer not found in the scene");
        }
    }
}