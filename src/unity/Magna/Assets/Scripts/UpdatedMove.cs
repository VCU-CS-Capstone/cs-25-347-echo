using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.IO;

public class UpdatedMove : MonoBehaviour
{
    // Position vectors
    private Vector3 set;
    private Vector3 angles;
    
    // The cube to move
    public GameObject cube;
    
    // Animation csv files
    public TextAsset[] animations;
    
    // Animation playback state
    private List<string> lines;
    private int count;
    private bool first = true;
    private bool isPaused = false;
    private Vector3 angleStart;
    private Vector3 transStart;
    private float setx;
    private float sety;
    private float setz;
    private int currentAnimationIndex = 0;
    
    void Start()
    {
        ReadString();
        transStart = cube.transform.position;
        angleStart = cube.transform.eulerAngles;
    }

    // Property to pause/resume animation
    public bool IsPaused
    {
        get { return isPaused; }
        set { isPaused = value; }
    }

    void FixedUpdate()
    {
        if (isPaused)
        {
            return;
        }
        
        if (count < lines.Count)
        {
            // Process current animation frame
            string[] positions = lines[count].Split(',');
            if (first)
            {
                setx = float.Parse(positions[0]);
                sety = float.Parse(positions[1]);
                setz = float.Parse(positions[2]);
                first = false;
            }

            set = cube.transform.position;
            set.x = float.Parse(positions[0]) - setx;
            set.y = float.Parse(positions[1]) - sety;
            set.z = float.Parse(positions[2]) - setz;

            angles = cube.transform.eulerAngles;
            angles.x = float.Parse(positions[3]);
            angles.y = float.Parse(positions[4]);
            angles.z = float.Parse(positions[5]);
            cube.transform.position = set;
            cube.transform.eulerAngles = angles;
            count++;
        }
        else
        {
            // Return to starting position when animation completes
            var step = .01f * Time.deltaTime;
            cube.transform.position = Vector3.MoveTowards(cube.transform.position, transStart, step);
            cube.transform.rotation = Quaternion.Lerp(cube.transform.rotation, Quaternion.LookRotation(Vector3.zero), 10f * Time.deltaTime);

            if (Vector3.Distance(cube.transform.position, transStart) < 0.001f)
            {
                count = 0;
                SetNextAnimation();
            }
        }
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
        if (animations.Length == 0 || animations[currentAnimationIndex] == null)
        {
            Debug.LogError("No animation files assigned or current animation is null");
            return;
        }
        
        TextAsset currentAnimation = animations[currentAnimationIndex];
        string[] fileLines = currentAnimation.text.Split('\n');
        bool firstLine = true;
        foreach (string line in fileLines)
        {
            if (line.Length > 0)
            {
                if (firstLine)
                {
                    firstLine = false; // Skip header line
                } 
                else 
                {
                    lines.Add(line);
                }
            }
        }
        
        if (lines.Count > 0)
        {
            Debug.Log("Loaded animation with " + lines.Count + " frames");
        }
        else
        {
            Debug.LogWarning("Animation file contains no data lines");
        }
    }

    private void ResetAnimation()
    {
        first = true;
        count = 0;
        transStart = cube.transform.position;
        angleStart = cube.transform.eulerAngles;
        setx = 0;
        sety = 0;
        setz = 0;
    }
}