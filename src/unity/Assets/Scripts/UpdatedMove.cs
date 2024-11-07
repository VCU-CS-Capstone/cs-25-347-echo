using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using System.IO;

public class UpdatedMove : MonoBehaviour
{
    //Physical TCP
    Vector3 set;
    Vector3 angles;
    public GameObject cube;

    // animation csv files
    public TextAsset[] animations;
    private List<string> lines;
    private int count;
    private bool first = true;
    private bool pause = false;
    private Vector3 Anglestart;
    private Vector3 Transstart;
    private float setx;
    private float sety;
    private float setz;
    private int currentAnimationIndex = 0;
    private float time = 0;
    private int delay = 0;
    private int delayEffect = 0;

    void Start()
    {
        ReadString();
        Transstart = cube.transform.position;
        Anglestart = cube.transform.eulerAngles;
    }

    public void setPause(bool isPaused){
        pause = isPaused;
        Debug.Log(pause);
    }

    public void setSlow(int isSlow) {
        delayEffect = isSlow;
        Debug.Log(delayEffect);
    }



    void FixedUpdate()
    {
        delay++;
        if (pause)
        {
            Debug.Log("PAUSED");
        } else {
            if(delay >= delayEffect){
                if (count < lines.Count)
                {
                    GameObject fakeCube = cube;
                    string[] positions = lines[count].Split(',');
                    if (first)
                    {
                        setx = float.Parse(positions[0]);
                        sety = float.Parse(positions[1]);
                        setz = float.Parse(positions[2]);
                        first = false;
                    }

                    set = fakeCube.transform.position;
                    set.x = float.Parse(positions[0]) - setx;
                    set.y = float.Parse(positions[1]) - sety;
                    set.z = float.Parse(positions[2]) - setz;

                    angles = cube.transform.eulerAngles;
                    angles.x = float.Parse(positions[3]);
                    angles.y = float.Parse(positions[4]);
                    angles.z = float.Parse(positions[5]);

                    
                    fakeCube.transform.position = set;
                    fakeCube.transform.eulerAngles = angles;
                    cube = fakeCube;
                    count++;
                }
                else
                {
                    var step = .01f * Time.deltaTime;
                    cube.transform.position = Vector3.MoveTowards(cube.transform.position, Transstart, step);
                    cube.transform.rotation = Quaternion.Lerp(cube.transform.rotation, Quaternion.LookRotation(new Vector3(0, 0, 0)), 10f * Time.deltaTime);

                    if (cube.transform.position == Transstart && cube.transform.eulerAngles == Anglestart)
                    {
                        count = 0;
                        SetNextAnimation();
                    }
                }
                delay = 0;
            }
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

    public void Pause()
    {
        pause = true;
    }

    public void Resume()
    {
        pause = false;
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