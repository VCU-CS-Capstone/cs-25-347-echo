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

                    // The original position is now used directly, as rotation and scale come from the parent transform
                    Vector3 scaledPosition = originalPosition; // Renaming this variable might be good later, but keep for now to minimize changes

                    // Apply the parent's scale to the calculated local offset
                    Vector3 scaledOffset = Vector3.Scale(scaledPosition, transform.localScale);

                    // Calculate the final world position based on parent's transform and the scaled & parent-scaled offset
                    Vector3 worldPosition = transform.position + transform.rotation * scaledOffset;

                    // Apply the calculated world position. Rotation is inherited from the parent.
                    CreatedJoint[q].transform.position = worldPosition;
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