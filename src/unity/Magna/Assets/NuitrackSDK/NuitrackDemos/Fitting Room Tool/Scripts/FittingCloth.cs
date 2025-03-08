using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
class Bone
{
    public Transform targetClothBone;
    public Transform targetMannequinBone;
    public bool usePostion = true;
    public bool useRotation = true;

    [Header("Advanced")]
    public Quaternion rotationOffset = Quaternion.identity;
    public Vector3 positionOffset;

    [Header("Debug")]
    [Range(0, 0.5f)]
    public float debugSphereSize = 0.05f;
    public Color debugSphereColor = Color.blue;
}

public class FittingCloth : MonoBehaviour
{
    [SerializeField] Transform clothBaseBone;
    [SerializeField, Range(1, 2)] float clothSize = 1;
    
    [SerializeField] Bone waist;
    [SerializeField] Bone torso;
    [SerializeField] Bone collar;
    [SerializeField] Bone neck;
    [SerializeField] Bone leftShoulder;
    [SerializeField] Bone leftElbow;
    [SerializeField] Bone leftWrist;
    [SerializeField] Bone rightShoulder;
    [SerializeField] Bone rightElbow;
    [SerializeField] Bone rightWrist;
    [SerializeField] Bone rightUpLeg;
    [SerializeField] Bone rightLeg;
    [SerializeField] Bone rightFoot;
    [SerializeField] Bone leftUpLeg;
    [SerializeField] Bone leftLeg;
    [SerializeField] Bone leftFoot;
    List<Bone> bones = new List<Bone>();

    [Header("Debug")]
    [SerializeField] bool updateJointsTransformsInEditor = true;
    [SerializeField] bool updateSizeInEditor = false;

    void Start()
    {
        UpdateBonesList();
    }

    void UpdateBonesList()
    {
        bones.Clear();
        if (waist.targetClothBone && waist.targetMannequinBone) bones.Add(waist);
        if (torso.targetClothBone && torso.targetMannequinBone) bones.Add(torso);
        if (collar.targetClothBone && collar.targetMannequinBone) bones.Add(collar);
        if (neck.targetClothBone && neck.targetMannequinBone) bones.Add(neck);
        if (leftShoulder.targetClothBone && leftShoulder.targetMannequinBone) bones.Add(leftShoulder);
        if (leftElbow.targetClothBone && leftElbow.targetMannequinBone) bones.Add(leftElbow);
        if (leftWrist.targetClothBone && leftWrist.targetMannequinBone) bones.Add(leftWrist);
        if (rightShoulder.targetClothBone && rightShoulder.targetMannequinBone) bones.Add(rightShoulder);
        if (rightElbow.targetClothBone && rightElbow.targetMannequinBone) bones.Add(rightElbow);
        if (rightWrist.targetClothBone && rightWrist.targetMannequinBone) bones.Add(rightWrist);
        if (rightUpLeg.targetClothBone && rightUpLeg.targetMannequinBone) bones.Add(rightUpLeg);
        if (rightLeg.targetClothBone && rightLeg.targetMannequinBone) bones.Add(rightLeg);
        if (rightFoot.targetClothBone && rightFoot.targetMannequinBone) bones.Add(rightFoot);
        if (leftUpLeg.targetClothBone && leftUpLeg.targetMannequinBone) bones.Add(leftUpLeg);
        if (leftLeg.targetClothBone && leftLeg.targetMannequinBone) bones.Add(leftLeg);
        if (leftFoot.targetClothBone && leftFoot.targetMannequinBone) bones.Add(leftFoot);
    }

    private void OnDrawGizmos()
    {
        if (updateSizeInEditor)
        {
            UpdateSize();
        }

        UpdateBonesList();

        for (int i = 0; i < bones.Count; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(bones[i].targetClothBone.position, bones[i].targetMannequinBone.position);
            Gizmos.color = bones[i].debugSphereColor;
            Gizmos.DrawSphere(bones[i].targetClothBone.position, bones[i].debugSphereSize);

            if (updateJointsTransformsInEditor)
                UpdateBoneTransform(bones[i]);
        }
    }

    void Update()
    {
        UpdateSize();

        for (int i = 0; i < bones.Count; i++)
        {
            UpdateBoneTransform(bones[i]);
        }
    }

    void UpdateSize()
    {
        clothBaseBone.localScale = new Vector3(clothSize, clothBaseBone.localScale.y, clothSize);
    }

    void UpdateBoneTransform(Bone bone)
    {
        for (int i = 0; i < bones.Count; i++)
        {             
            if (bone.targetClothBone == null || bone.targetMannequinBone == null)
                continue;

            if (bone.usePostion)
            {
                bone.targetClothBone.position = bone.targetMannequinBone.position + bone.positionOffset;
            }

            if (bone.useRotation)
            {
                Quaternion quaternion = bone.targetMannequinBone.rotation * bone.rotationOffset;
                bone.targetClothBone.rotation = quaternion;
            }
        }
    }
}
