using UnityEngine;

[System.Serializable]
public class ClothBone : MonoBehaviour
{
    public Transform targetHumanBone;
    public bool usePostion = true;
    public bool useRotation = true;

    [Header("Advanced")]
    public Quaternion rotationOffset = Quaternion.identity;
    public Vector3 positionOffset;

    [Header("Debug")]
    public bool updateTransformInEditor;
    [SerializeField, Range(0, 0.5f)]
    float debugSphereSize = 0.05f;
    [SerializeField]
    Color debugSphereColor = Color.blue;

    void Start()
    {
        if (targetHumanBone == null)
        {
            Debug.LogError($"targetHumanBone not setted in GameObject: " + gameObject.name);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = debugSphereColor;
        Gizmos.DrawSphere(transform.position, debugSphereSize);

        if (updateTransformInEditor)
        {
            UpdateTransform();
            gameObject.SetActive(true);
        }
    }
#endif

    void Update()
    {
        UpdateTransform();
    }

    void UpdateTransform()
    {
        if (usePostion)
        {
            transform.position = targetHumanBone.position + positionOffset;
        }

        if (useRotation)
        {
            transform.rotation = targetHumanBone.rotation * rotationOffset;
        }
    }
}
