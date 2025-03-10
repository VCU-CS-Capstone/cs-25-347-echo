﻿#region Description

// The script performs a direct translation of the skeleton using only the position data of the joints.
// Objects in the skeleton will be created when the scene starts.
// Added options to rotate joints and scale the skeleton (distances between joints).

#endregion


using UnityEngine;

namespace NuitrackSDK.Tutorials.FirstProject
{
    [AddComponentMenu("NuitrackSDK/Tutorials/First Project/Native Avatar")]
    public class NativeAvatar : MonoBehaviour
    {
        string message = "";

        public nuitrack.JointType[] typeJoint;
        public GameObject[] CreatedJoint; // Made public to access from TaskProgrammer
        public GameObject PrefabJoint;

        [Header("Skeleton Customization")]
        [Tooltip("Rotation in degrees around each axis for the entire skeleton")]
        public Vector3 skeletonRotation = Vector3.zero;

        [Tooltip("Scale factor for distances between joints (skeleton size)")]
        [Range(0.1f, 3.0f)]
        public float skeletonScale = 1.0f;

        void Start()
        {
            CreatedJoint = new GameObject[typeJoint.Length];
            for (int q = 0; q < typeJoint.Length; q++)
            {
                CreatedJoint[q] = Instantiate(PrefabJoint);
                CreatedJoint[q].transform.SetParent(transform);
                CreatedJoint[q].SetActive(false); // Initially deactivate all joints
            }
            message = "Skeleton created";
        }

        void Update()
        {
            bool skeletonFound = NuitrackManager.sensorsData[NuitrackManager.sensorsData.Count > 0 ? 0 : 0].Users.Current != null &&
                                NuitrackManager.sensorsData[NuitrackManager.sensorsData.Count > 0 ? 0 : 0].Users.Current.Skeleton != null;

            if (skeletonFound)
            {
                message = "Skeleton found";

                for (int q = 0; q < typeJoint.Length; q++)
                {
                    // Activate the joint if it was inactive
                    if (!CreatedJoint[q].activeSelf)
                    {
                        CreatedJoint[q].SetActive(true);
                    }

                    UserData.SkeletonData.Joint joint = NuitrackManager.sensorsData[0].Users.Current.Skeleton.GetJoint(typeJoint[q]);

                    // Get the original position
                    Vector3 originalPosition = joint.Position;

                    // Apply skeleton rotation to the position (rotates the entire skeleton)
                    if (skeletonRotation != Vector3.zero)
                    {
                        Quaternion rotation = Quaternion.Euler(skeletonRotation);
                        originalPosition = rotation * originalPosition;
                    }

                    // Scale the position to adjust distances between joints
                    Vector3 scaledPosition = originalPosition * skeletonScale;

                    // Apply position with scaling and skeleton rotation
                    CreatedJoint[q].transform.localPosition = scaledPosition;
                }
            }
            else
            {
                message = "Skeleton not found";
                
                // Deactivate all joints when skeleton is not found
                for (int q = 0; q < typeJoint.Length; q++)
                {
                    if (CreatedJoint[q].activeSelf)
                    {
                        CreatedJoint[q].SetActive(false);
                    }
                }
            }
        }

        // Display the message on the screen
        void OnGUI()
        {
            GUI.color = Color.red;
            GUI.skin.label.fontSize = 50;
            GUILayout.Label(message);
        }
    }
}