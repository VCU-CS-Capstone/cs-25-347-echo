using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NuitrackSDK.Frame;

public class FramesController : MonoBehaviour
{
    [SerializeField] GridLayoutGroup parentPanel;
    [SerializeField] DrawSensorFrame sensorFrame;

    void Start()
    {
        if (NuitrackManager.Instance.devices.Count == 0)
            return;

        int size = Mathf.CeilToInt(Mathf.Sqrt(NuitrackManager.Instance.devices.Count));
        parentPanel.cellSize = new Vector2(Screen.width / size, Screen.height / size);

        for (int i = 0; i < NuitrackManager.Instance.devices.Count; i++)
        {
            DrawSensorFrame sensorFrameInst = Instantiate(sensorFrame.gameObject, parentPanel.transform).GetComponent<DrawSensorFrame>();
            sensorFrameInst.sensorId = i;
            sensorFrameInst.transform.localScale = Vector3.one;
        }
    }
}
