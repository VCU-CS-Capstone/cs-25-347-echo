using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

// Author: Miles Popiela
// Description: Represents the one-directional graph for robotic waypoint pathing.
public class Directional_Graph : MonoBehaviour
{
    
    public Transform wayPoints;
    public GameObject ToolCenterPoint;
    public GameObject MoveTarget;
    public List<GameObject> targetList;
    LineRenderer lineRenderer;
    private bool isLinked = false;
    private bool isFirst = true;
    public Color c1 = Color.cyan;
    public Color c2 = Color.magenta;
    bool isRecording = false;

    public GameObject ogripObj;

    public GameObject cgripObj;
[SerializeField] private GripperController gripperController;    UnityEngine.Vector3 pastPosition;
    Quaternion pastRotation;

#if UNITY_EDITOR

    [CustomEditor(typeof(Directional_Graph))]
    public class Directional_GraphEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            Directional_Graph myScript = (Directional_Graph)target;
            
            if (GUILayout.Button("Toggle Recording On"))
            {
                myScript.ToggleRecording(true);
            }
            if (GUILayout.Button("Toggle Recording Off"))
            {
                myScript.ToggleRecording(false);
            }
            if (GUILayout.Button("Clear Waypoints"))
            {
                myScript.ClearWaypoints();
            }
            if (GUILayout.Button("Remove Last Waypoint"))
            {
                myScript.RemoveLastWaypoint();
            }
            if (GUILayout.Button("Add Waypoint"))
            {
                myScript.AddWaypoint();
            }
            if (GUILayout.Button("Save TargetList To CSV"))
            {
                myScript.SaveTargetListToCSV();
            }
        }
    }
#endif

    //Toggles recording
    public void ToggleRecording(bool play)
    {
        isRecording = play;
    }

    // Clears all waypoints.
    public void ClearWaypoints()
    {
        foreach (var target in targetList)
        {
            Destroy(target);
        }

        isLinked = false;
        targetList.Clear();
        lineRenderer.positionCount = 0;
    }

    //Removes Last Waypoint
    public void RemoveLastWaypoint()
    {
        if (targetList.Count > 0)
        {
            Destroy(targetList[targetList.Count - 1]);
            targetList.RemoveAt(targetList.Count - 1);
            lineRenderer.positionCount -= 1;
        }
    }

    //Adds Waypoint
    public void AddWaypoint()
    {
        if (targetList.Count == 0 && isFirst)
        {
            ConfigureLineRenderer();
            isFirst = false;
        }

        isLinked = true;
        GameObject newObject = Instantiate(MoveTarget, ToolCenterPoint.transform.position, ToolCenterPoint.transform.rotation);
        newObject.transform.SetParent(wayPoints);
        targetList.Add(newObject);
        Debug.Log("Posted");
    }

    public void AddGripCommand(string command)
    {
        if (command == "o")
        {
            GameObject newObject = Instantiate(ogripObj, ToolCenterPoint.transform.position, ToolCenterPoint.transform.rotation);
            newObject.transform.SetParent(wayPoints);
            targetList.Add(newObject);
        }
        else
        {
            GameObject newObject = Instantiate(cgripObj, ToolCenterPoint.transform.position, ToolCenterPoint.transform.rotation);
            newObject.transform.SetParent(wayPoints);
            targetList.Add(newObject);
        }
    }

    // Links new Target node with prior node and keeps paths updated.
    private void LinkTargets()
    {
        if (isLinked)
        {
            for (int i = 0; i < targetList.Count; i++)
            {
                lineRenderer.positionCount = i + 1;
                lineRenderer.SetPosition(i, targetList[i].transform.position);
            }
        }
    }

    void ConfigureLineRenderer()
    {

        lineRenderer = this.gameObject.GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(c1, 0.0f), new GradientColorKey(c2, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
        );
        lineRenderer.colorGradient = gradient;
    }


    // Start is called before the first frame update
    void Start()
    {
        targetList = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {


    }

    void FixedUpdate()
    {
        if ((isRecording == true) && ((ToolCenterPoint.transform.position != pastPosition)))
        {
            AddWaypoint();
        }

        pastPosition = ToolCenterPoint.transform.position;
        pastRotation = ToolCenterPoint.transform.rotation;
        LinkTargets();
    }


    public List<GameObject> GetList()
    {
        return targetList;
    }

    public void SaveTargetListToCSV()
    {
        string filePath = System.IO.Path.Combine(Application.dataPath, "TargetList.csv");
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            for (int i = 0; i < targetList.Count; i++)
            {
                string line = "";
                GameObject target = targetList[i];
                if (target.name == "Sphere(Clone)")
                {
                    Vector3 position = target.transform.position;
                    Vector3 rotation = target.transform.rotation.eulerAngles;

                    line = string.Format("{0},{1},{2},{3},{4},{5},{6}",
                        i,
                        position.x,
                        position.y,
                        position.z,
                        rotation.x,
                        rotation.y,
                        rotation.z);
                }
                else if (target.name == "Closer(Clone)")
                {
                    line = "c";
                }
                else if (target.name == "Opener(Clone)")
                {
                    line = "o";
                } else {
                    Debug.LogError("Unknown target type: " + target.name);
                    throw new System.InvalidOperationException("Encountered unknown target type: " + target.name);
                }

                writer.WriteLine(line);
            }
        }

        Debug.Log("Target list saved to CSV file: " + filePath);
        AssetDatabase.Refresh();
    }
}