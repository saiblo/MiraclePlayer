using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraUI : MonoBehaviour
{
    public ModelDisplayer displayer;

    Vector3 minPos, maxPos, deltaPos;
    float zoom = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 dis = displayer.transform.position - transform.position + Vector3.up * 5f;
        minPos = transform.position + dis * 0.5f;
        maxPos = transform.position - dis * 0.5f;
        deltaPos = minPos - maxPos;
        transform.position = maxPos + zoom * deltaPos;
        transform.rotation = Quaternion.LookRotation(dis);
        enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel") * 0.2f;// 读取鼠标滚轮产生的变化
        if (zoomDelta != 0f) {
            AdjustZoom(zoomDelta);
        }
    }

    void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta);
        transform.position = maxPos + zoom * deltaPos;
    }
}
