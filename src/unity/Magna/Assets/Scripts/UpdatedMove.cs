using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class UpdatedMove : MonoBehaviour
{
    Vector3 set;
    Vector3 angles;
    public GameObject cube;
    // animation csv files
    public TextAsset[] animations;
    private List<string> lines;
    private int count;
    private bool first = true;
    private bool _yourBooleanVariable = false;
    private Vector3 Anglestart;
    private Vector3 Transstart;
    private float setx;
    private float sety;
    private float setz;
    private int currentAnimationIndex = 0;

    public bool gripperOpens = true;

    public void gripperOpen(bool o){
        gripperOpens = o;

    }
    
    [SerializeField] private GripperAnimation grippy;
    void Start()
    {
        ReadString();
        Transstart = cube.transform.position;
        Anglestart = cube.transform.eulerAngles;
    }

    public bool YourBooleanProperty
    {
        get { return _yourBooleanVariable; }
        set { _yourBooleanVariable = value; }
    }

    // Add these variables at the top of the class
    [SerializeField] private int framesPerUpdate = 1;
    
    void FixedUpdate()
    {
        if (_yourBooleanVariable)
        {
            return;
        }
        
        // Process multiple frames per update
        for (int i = 0; i < framesPerUpdate; i++)
        {
            ProcessFrame();
        }
    }

    async Task ProcessFrame()
    {
        if (_yourBooleanVariable)
        {
            return;
        }
        
            if (count < lines.Count)
            {
                GameObject fakeCube = cube;
                string[] positions = lines[count].Split(',');
                if(positions[0].Trim() == "o"){
                    StartCoroutine(grippy.OpenGripper());
                    // TODO: Wait until gripper open fully
                } else if(positions[0].Trim() == "c"){
                    StartCoroutine(grippy.CloseGripper());
                    // TODO: Wait until gripper close fully 
                    UnityEngine.Debug.Log("closing");
                } else {
                    set = fakeCube.transform.position;
                    set.x = float.Parse(positions[1]);
                    set.y = float.Parse(positions[2]);
                    set.z = float.Parse(positions[3]);

                    angles = cube.transform.eulerAngles;
                    angles.x = float.Parse(positions[4]);
                    angles.y = float.Parse(positions[5]);
                    angles.z = float.Parse(positions[6]);
                    fakeCube.transform.position = set;
                    fakeCube.transform.eulerAngles = angles;
                    cube = fakeCube;
                }
                
                count++;
            }
            else
            {
                count = 0;
            }
    }

    void Update()
    {
    }

    public void SetNextAnimation()
    {
        currentAnimationIndex = (currentAnimationIndex + 1) % animations.Length;
        ReadString();
        ResetAnimation();
    }

    public void SetPreviousAnimation()
    {
        currentAnimationIndex = currentAnimationIndex - 1;
        if (currentAnimationIndex < 0)
        {
            currentAnimationIndex = animations.Length - 1;
        }
        ReadString();
        ResetAnimation();
    }


    private void ReadString()
    {
        lines = new List<string>();
        TextAsset currentAnimation = animations[currentAnimationIndex];
        string[] fileLines = currentAnimation.text.Split('\n');
        bool firstline = true;
        foreach (string line in fileLines)
        {
            if (line.Length > 0)
            {
                if (firstline)
                {
                    firstline = false;// Skip empty lines
                } else {
                    lines.Add(line);
                    
                }

            }
        }
        UnityEngine.Debug.Log(lines[1]);


    }

    private void ResetAnimation()
    {
        first = true;
        count = 0;
        Transstart = cube.transform.position;
        Anglestart = cube.transform.eulerAngles;
        setx = 0;
        sety = 0;
        setz = 0;
    }
}
