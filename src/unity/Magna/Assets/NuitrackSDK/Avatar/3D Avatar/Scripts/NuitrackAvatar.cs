using UnityEngine;
using System.Collections.Generic;

using JointType = nuitrack.JointType;

using NuitrackSDK.Calibration;


namespace NuitrackSDK.Avatar
{
    [AddComponentMenu("NuitrackSDK/Avatar/3D/Nuitrack Avatar")]
    public class NuitrackAvatar : BaseAvatar
    {
        public enum BoneLengthType
        {
            Realtime,
            AfterCalibration,
        }

        [Header("Body")]
        [SerializeField, NuitrackSDKInspector] Transform waist;
        [SerializeField, NuitrackSDKInspector] Transform torso;
        [SerializeField, NuitrackSDKInspector] Transform collar;
        [SerializeField, NuitrackSDKInspector] Transform neck;
        [SerializeField, NuitrackSDKInspector] Transform head;

        [Header("Left hand")]
        [SerializeField, NuitrackSDKInspector] Transform leftShoulder;
        [SerializeField, NuitrackSDKInspector] Transform leftElbow;
        [SerializeField, NuitrackSDKInspector] Transform leftWrist;

        [Header("Right hand")]
        [SerializeField, NuitrackSDKInspector] Transform rightShoulder;
        [SerializeField, NuitrackSDKInspector] Transform rightElbow;
        [SerializeField, NuitrackSDKInspector] Transform rightWrist;

        [Header("Left leg")]
        [SerializeField, NuitrackSDKInspector] Transform leftHip;
        [SerializeField, NuitrackSDKInspector] Transform leftKnee;
        [SerializeField, NuitrackSDKInspector] Transform leftAnkle;

        [Header("Right leg")]
        [SerializeField, NuitrackSDKInspector] Transform rightHip;
        [SerializeField, NuitrackSDKInspector] Transform rightKnee;
        [SerializeField, NuitrackSDKInspector] Transform rightAnkle;

        List<ModelJoint> modelJoints = new List<ModelJoint>();

        List<JointType> leftLegJointTypes = new List<JointType>() { JointType.LeftHip, JointType.LeftKnee, JointType.LeftAnkle };
        List<JointType> rightLegJointTypes = new List<JointType>() { JointType.RightHip, JointType.RightKnee, JointType.RightAnkle };

        [Header ("Options")]
        [Tooltip("Aligns the size of the model's bones with the size of the bones of the user's skeleton, " +
           "ensuring that the model's size best matches the user's size.")]
        [SerializeField] bool alignmentBoneLength = false;
        [SerializeField] BoneLengthType boneLengthType;
        bool calibrationSuccess = false;
        [SerializeField, Range(0, 1)] float smoothMove = 0.5f;
        float minSmoothMove = 0.3f;
        float smoothModifier = 20;
        [SerializeField] bool alignStraightLegs = false;
        [SerializeField] float straightLegsThresold = 0.14f;
        [SerializeField] float hideDistance = 1.5f;
        [SerializeField] GameObject meshObject;

        public float SmoothMove
        {
            get
            {
                return Mathf.Clamp(smoothModifier - smoothMove * smoothModifier, minSmoothMove, smoothModifier);
            }
            set
            {
                smoothMove = value;
            }
        }

        [SerializeField] JointType rootJoint = JointType.Waist;

        [SerializeField, NuitrackSDKInspector] bool vrMode = false;
        [SerializeField, NuitrackSDKInspector] GameObject vrHead;
        [SerializeField, NuitrackSDKInspector] Transform headTransform;
        Transform spawnedHead;

        [SerializeField, NuitrackSDKInspector] bool needBorderGrid = false;
        [SerializeField, NuitrackSDKInspector] GameObject borderGrid;
        Transform spawnedBorderGrid;

        Vector3 basePivotOffset = Vector3.zero;
        Vector3 startPoint; //Root joint model bone position on start

        [Tooltip("(optional) Specify the transform, which represents the sensor " +
         "coordinate system, to display the Avatar in front of the sensor." +
         "\nCalibration is not supported." +
         "\n\nIf not specified, the object transformation is used.")]
        [SerializeField, NuitrackSDKInspector] Transform sensorSpace;

        [SerializeField, NuitrackSDKInspector] bool recenterOnSuccess;

        /// <summary> Model bones </summary> Dictionary with joints
        Dictionary<JointType, ModelJoint> jointsRigged = new Dictionary<JointType, ModelJoint>();
        Dictionary<JointType, Vector3> jointsDefaultPos = new Dictionary<JointType, Vector3>();
        Dictionary<JointType, Quaternion> jointsDefaultRot = new Dictionary<JointType, Quaternion>();

        void OnEnable()
        {
            if(CalibrationHandler.Instance != null)
                CalibrationHandler.Instance.onSuccess += OnSuccessCalib;
        }

        void SetJoint(Transform tr, JointType jointType)
        {
            ModelJoint modelJoint = new ModelJoint()
            {
                bone = tr,
                jointType = jointType
            };

            modelJoints.Add(modelJoint);
        }

        bool IsTransformSpace
        {
            get
            {
                return sensorSpace == null || sensorSpace == transform;
            }
        }

        Transform SpaceTransform
        {
            get
            {
                return IsTransformSpace ? transform : sensorSpace;
            }
        }

        void Start()
        {
            SetJoint(waist, JointType.Waist);
            SetJoint(torso, JointType.Torso);
            SetJoint(collar, JointType.LeftCollar);
            SetJoint(collar, JointType.RightCollar);
            SetJoint(neck, JointType.Neck);
            SetJoint(head, JointType.Head);

            SetJoint(leftShoulder, JointType.LeftShoulder);
            SetJoint(leftElbow, JointType.LeftElbow);
            SetJoint(leftWrist, JointType.LeftWrist);

            SetJoint(rightShoulder, JointType.RightShoulder);
            SetJoint(rightElbow, JointType.RightElbow);
            SetJoint(rightWrist, JointType.RightWrist);

            SetJoint(leftHip, JointType.LeftHip);
            SetJoint(leftKnee, JointType.LeftKnee);
            SetJoint(leftAnkle, JointType.LeftAnkle);

            SetJoint(rightHip, JointType.RightHip);
            SetJoint(rightKnee, JointType.RightKnee);
            SetJoint(rightAnkle, JointType.RightAnkle);

            //Adding model bones and JointType keys
            //Adding rotation offsets of model bones and JointType keys

            //Iterate joints from the modelJoints array
            //base rotation of the model bone is recorded 
            //then the model bones and their jointType are added to the jointsRigged dictionary
            foreach (ModelJoint modelJoint in modelJoints)
            {
                if (transform == modelJoint.bone)
                    Debug.LogError("Base transform can't be bone!");

                if (modelJoint.bone)
                {
                    modelJoint.baseRotOffset = Quaternion.Inverse(SpaceTransform.rotation) * modelJoint.bone.rotation;
                    jointsDefaultRot.Add(modelJoint.jointType, modelJoint.bone.rotation);
                    jointsRigged.Add(modelJoint.jointType.TryGetMirrored(), modelJoint);
                }
            }

            foreach (ModelJoint modelJoint in modelJoints)
            {
                //Adding base distances between the child bone and the parent bone 
                if (modelJoint.bone != null && modelJoint.jointType.GetParent() != JointType.None)
                {
                    jointsDefaultPos.Add(modelJoint.jointType, modelJoint.bone.localPosition);
                    AddBoneScale(modelJoint.jointType.TryGetMirrored(), modelJoint.jointType.GetParent().TryGetMirrored());
                }
            }

            if (vrMode)
            {
                spawnedHead = Instantiate(vrHead).transform;
                spawnedHead.position = headTransform.position;
                spawnedHead.rotation = transform.rotation;
            }

            if (jointsRigged.ContainsKey(rootJoint))
            {
                Vector3 rootPosition = jointsRigged[rootJoint].bone.position;
                startPoint = SpaceTransform.InverseTransformPoint(rootPosition);

                if (needBorderGrid)
                {
                    spawnedBorderGrid = Instantiate(borderGrid).transform;
                    spawnedBorderGrid.position = jointsRigged[rootJoint].bone.position;
                    spawnedBorderGrid.rotation = transform.rotation * Quaternion.Euler(0,180,0);
                }
            }
        }

        /// <summary>
        /// Adding distance between the target and parent model bones
        /// </summary>
        void AddBoneScale(JointType targetJoint, JointType parentJoint)
        {
            //take the position of the model bone
            Vector3 targetBonePos = jointsRigged[targetJoint].bone.position;
            //take the position of the model parent bone  
            Vector3 parentBonePos = jointsRigged[parentJoint].bone.position;
            jointsRigged[targetJoint].baseDistanceToParent = Vector3.Distance(parentBonePos, targetBonePos);
            //record the Transform of the model parent bone
            jointsRigged[targetJoint].parentBone = jointsRigged[parentJoint].bone;
        }

        void Update()
        {
            if (meshObject != null)
                meshObject.SetActive(ControllerUser != null && ControllerUser.Skeleton != null && ControllerUser.Skeleton.GetJoint(rootJoint).Position.z > hideDistance);

            if (ControllerUser != null && ControllerUser.Skeleton != null)
                Process(ControllerUser);

            if (vrMode)
                spawnedHead.position = headTransform.position;
        }

        Vector3 GetJointLocalPos(Vector3 jointPosition)
        {
            Vector3 jointPos = jointPosition - basePivotOffset;
            Vector3 localPos = IsTransformSpace ? Quaternion.Euler(0, 180, 0) * jointPos : jointPos;

            return SpaceTransform.TransformPoint(localPos); ;
        }

        /// <summary>
        /// Getting skeleton data from thr sensor and updating transforms of the model bones
        /// </summary>
        void Process(UserData user)
        {
            if (!alignmentBoneLength || boneLengthType == BoneLengthType.AfterCalibration)
            {
                jointsRigged[rootJoint].bone.position = GetJointLocalPos(GetJoint(rootJoint).Position);
            }

            foreach (var riggedJoint in jointsRigged)
            {
                //Get modelJoint
                ModelJoint modelJoint = riggedJoint.Value;

                //Get joint from the Nuitrack
                //nuitrack.Joint joint = skeleton.GetJoint(riggedJoint.Key);
                UserData.SkeletonData.Joint jointTransform = user.Skeleton.GetJoint(riggedJoint.Key);

                //Bone rotation
                Quaternion jointRotation = IsTransformSpace ? jointTransform.RotationMirrored : jointTransform.Rotation;

                if (smoothMove == 0)
                    modelJoint.bone.rotation = GetJointRotation(user, jointRotation, modelJoint);
                else
                    modelJoint.bone.rotation = Quaternion.Slerp(modelJoint.bone.rotation, GetJointRotation(user, jointRotation, modelJoint), Time.deltaTime * SmoothMove);

                if (alignmentBoneLength &&
                        (boneLengthType == BoneLengthType.Realtime || (boneLengthType == BoneLengthType.AfterCalibration && calibrationSuccess)))
                {
                    Vector3 newPos = GetJointPosition(user, jointRotation, modelJoint, jointTransform);
                    if (smoothMove == 0)
                        modelJoint.bone.position = newPos;
                    else
                        modelJoint.bone.position = Vector3.Lerp(modelJoint.bone.position, newPos, Time.deltaTime * SmoothMove);

                    //Bone scale
                    if (modelJoint.parentBone != null && modelJoint.jointType.GetParent() != rootJoint)
                    {
                        //Take the Transform of a parent bone
                        Transform parentBone = modelJoint.parentBone;
                        //calculate how many times the distance between the child bone and its parent bone has changed compared to the base distance (which was recorded at the start)
                        float scaleDif = modelJoint.baseDistanceToParent / Vector3.Distance(newPos, parentBone.position);
                        //change the size of the bone to the resulting value (On default bone size (1,1,1))
                        parentBone.localScale = Vector3.one / scaleDif;
                        //compensation for size due to hierarchy
                        parentBone.localScale *= parentBone.localScale.x / parentBone.lossyScale.x;
                    }
                }
            }

            calibrationSuccess = false;
        }

        Quaternion GetJointRotation(UserData user, Quaternion jointRotation, ModelJoint modelJoint)
        {
            Quaternion newRot = SpaceTransform.rotation * (jointRotation * modelJoint.baseRotOffset);

            if (alignStraightLegs)
            {
                if (leftLegJointTypes.Contains(modelJoint.jointType))
                {
                    if (CheckFailedLegsJoint(user, leftLegJointTypes[0], leftLegJointTypes[1], leftLegJointTypes[2], straightLegsThresold))
                        newRot = Quaternion.Euler(0, jointsRigged[JointType.Waist].bone.eulerAngles.y, 0) * modelJoint.baseRotOffset;
                }

                if (rightLegJointTypes.Contains(modelJoint.jointType))
                {
                    if (CheckFailedLegsJoint(user, rightLegJointTypes[0], rightLegJointTypes[1], rightLegJointTypes[2], straightLegsThresold))
                        newRot = Quaternion.Euler(0, jointsRigged[JointType.Waist].bone.eulerAngles.y, 0) * modelJoint.baseRotOffset;
                }
            }

            return newRot;
        }

        Vector3 GetJointPosition(UserData user, Quaternion jointRotation, ModelJoint modelJoint, UserData.SkeletonData.Joint jointTransform)
        {
            Vector3 newPos = GetJointLocalPos(jointTransform.Position);
            if (jointTransform.IsGoodDepth == false && jointsDefaultPos.ContainsKey(modelJoint.jointType))
                newPos = modelJoint.bone.parent.TransformPoint(jointsDefaultPos[modelJoint.jointType]);

            if (alignStraightLegs)
            {
                if (leftLegJointTypes.Contains(modelJoint.jointType))
                {
                    if (CheckFailedLegsJoint(user, leftLegJointTypes[0], leftLegJointTypes[1], leftLegJointTypes[2], straightLegsThresold))
                        newPos = modelJoint.bone.parent.TransformPoint(jointsDefaultPos[modelJoint.jointType]);
                }

                if (rightLegJointTypes.Contains(modelJoint.jointType))
                {
                    if (CheckFailedLegsJoint(user, rightLegJointTypes[0], rightLegJointTypes[1], rightLegJointTypes[2], straightLegsThresold))
                        newPos = modelJoint.bone.parent.TransformPoint(jointsDefaultPos[modelJoint.jointType]);
                }
            }

            return newPos;
        }

        bool CheckFailedLegsJoint(UserData user, JointType hip, JointType knee, JointType ankle, float threshold)
        {
            UserData.SkeletonData.Joint hipJoint = user.Skeleton.GetJoint(hip);
            UserData.SkeletonData.Joint kneeJoint = user.Skeleton.GetJoint(knee);
            UserData.SkeletonData.Joint ankleJoint = user.Skeleton.GetJoint(ankle);

            UserData.SkeletonData.Joint waistJoint = user.Skeleton.GetJoint(JointType.Waist);
            UserData.SkeletonData.Joint torsoJoint = user.Skeleton.GetJoint(JointType.LeftCollar);

            bool badConfidence = hipJoint.Confidence <= JointConfidence || kneeJoint.Confidence <= JointConfidence || ankleJoint.Confidence <= JointConfidence;
            bool badAIDepth = !hipJoint.IsGoodDepth || !kneeJoint.IsGoodDepth|| !ankleJoint.IsGoodDepth;
            return (CheckKneePosInDeadzone(hipJoint, kneeJoint, ankleJoint, threshold) || badConfidence || badAIDepth);
        }

        bool CheckKneePosInDeadzone(UserData.SkeletonData.Joint hip, UserData.SkeletonData.Joint knee, UserData.SkeletonData.Joint ankle, float threshold)
        {
            Vector2 xzHipPos = new Vector2(hip.Position.x, hip.Position.z);
            Vector2 xzKneePos = new Vector2(knee.Position.x, knee.Position.z);
            Vector2 xzAnklePos = new Vector2(ankle.Position.x, ankle.Position.z);
            return Vector2.Distance(xzHipPos, xzKneePos) < threshold && Vector2.Distance(xzAnklePos, xzKneePos) < threshold;
        }

        void OnSuccessCalib(Quaternion rotation)
        {
            calibrationSuccess = true;

            if (!recenterOnSuccess || !IsTransformSpace)
                return;

            CalculateOffset();
            if (needBorderGrid)
                spawnedBorderGrid.position = GetJointLocalPos(GetJoint(rootJoint).Position - basePivotOffset);
        }

        void CalculateOffset()
        {
            if (jointsRigged.ContainsKey(rootJoint))
            {
                Vector3 rootPosition = jointsRigged[rootJoint].bone.position;

                Vector3 rootSpacePosition = SpaceTransform.InverseTransformPoint(rootPosition);

                basePivotOffset.y = -basePivotOffset.y;
                Vector3 newPivotOffset = startPoint - rootSpacePosition + basePivotOffset;
                newPivotOffset.x = 0;

                basePivotOffset = newPivotOffset;
            }
        }

        void OnDisable()
        {
            if(CalibrationHandler.Instance != null)
                CalibrationHandler.Instance.onSuccess -= OnSuccessCalib;
        }
    }
}