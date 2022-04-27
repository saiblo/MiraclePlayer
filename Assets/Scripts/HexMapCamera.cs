using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    public HexGrid grid;
    static HexMapCamera instance;
    public static bool Locked {
        set {
            if (instance)
                instance.enabled = !value;
        }
    }
    public static void ValidatePosition(int campIndex)
    {
        float xOffset = 3f, zOffset = 5f;
        instance.xMax = (instance.grid.cellCountX - 0.5f - HexMetrics.borderCellCountX - xOffset) * (2f * HexMetrics.innerRadius); // 减0.5f，为了在最右边时 屏幕中心在 奇数行最右cell 的中心
        instance.xMin = (HexMetrics.borderCellCountX + xOffset) * (2f * HexMetrics.innerRadius);
        instance.xMaxOffset = 3f * (2f * HexMetrics.innerRadius);
        instance.zMax = (instance.grid.cellCountZ - 1f - HexMetrics.borderCellCountZ - zOffset) * (1.5f * HexMetrics.outerRadius);
        instance.zMin = (HexMetrics.borderCellCountZ + zOffset) * (1.5f * HexMetrics.outerRadius);
        instance.zMaxOffset = 5f * (1.5f * HexMetrics.outerRadius);

        // Random.InitState(System.DateTime.Now.Second);
        instance.positions[0] = new Vector3(instance.xMin + (instance.xMax - instance.xMin) * 0.3f, 0, (instance.zMax + instance.zMin) * 0.5f);
        instance.positions[0] = instance.ClampPosition(instance.positions[0]);
        instance.rotations[0] = Quaternion.Euler(0f, 90f, 0f);
        instance.rotationAngles[0] = 90f;
        instance.zooms[0] = 0.3f;

        instance.positions[1] = new Vector3(instance.xMin + (instance.xMax - instance.xMin) * 0.7f, 0, (instance.zMax + instance.zMin) * 0.5f);
        instance.positions[1] = instance.ClampPosition(instance.positions[1]);
        instance.rotations[1] = Quaternion.Euler(0f, -90f, 0f);
        instance.rotationAngles[1] = -90f;
        instance.zooms[1] = 0.3f;

        instance.transform.localPosition = instance.positions[campIndex];
        instance.transform.localRotation = instance.rotations[campIndex];
        instance.curCamp = campIndex;
        instance.AdjustZoom(0f); // 0f 指变化量
    }

    public static void swap()
    {
        instance.curCamp = (instance.curCamp + 1) % 2;
        instance.transform.position = instance.ClampPosition(instance.positions[instance.curCamp]);
        instance.transform.rotation = instance.rotations[instance.curCamp];
        instance.AdjustZoom(0f);
    }

    // swivel 控制摄像机看向的角度
    // stick 控制摄像机的远近
    Transform swivel, stick;

    public float stickMinZoom, stickMaxZoom; // stick最小距离、最大距离
    public float swivelMinZoom, swivelMaxZoom; // swivel 最小、最大旋转角度
    public float moveSpeedMinZoom, moveSpeedMaxZoom; // 相机移动速度
    public float rotationSpeed; // 相机旋转速度

    // Camera.Position边界的具体值，初始化在 ValidatePosition 内完成
    float xMax, xMin, zMax, zMin;
    float xMaxOffset, zMaxOffset;

    // 两个视角的参量
    Vector3[] positions;
    Quaternion[] rotations;
    float[] zooms; // 表示镜头缩放比例，1f是最近，0f是最远
    float[] rotationAngles; // 旋转角度
    int curCamp;

    private void Awake()
    {
        instance = this; // 为了能在static方法中 修改非静态属性enabled
        swivel = transform.GetChild(0); // 返回第一个孩子 的transform
        stick = swivel.GetChild(0); // 返回swivel的第一个孩子 的transform
        swivel.localRotation = Quaternion.Euler(45f, 0f, 0f);
        stick.localPosition = new Vector3(0f, 0f, stickMaxZoom);

        positions = new Vector3[2];
        rotations = new Quaternion[2];
        zooms = new float[2];
        rotationAngles = new float[2];
    }

    // Update is called once per frame
    void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel") * 0.2f;// 读取鼠标滚轮产生的变化
        if (zoomDelta != 0f) {
            AdjustZoom(zoomDelta);
        }

        float rotationDelta = Input.GetAxis("Rotation"); // 
        if (rotationDelta != 0f) {
            AdjustRotation(rotationDelta);
        }

        float xDelta = Input.GetAxis("Horizontal"); // 通过A D 键控制
        float zDelta = Input.GetAxis("Vertical"); // 通过W S 键控制
        if (xDelta != 0f || zDelta != 0f) {
            AdjustPosition(xDelta, zDelta);
        }
    }

    void AdjustZoom(float delta)
    {
        zooms[curCamp] = Mathf.Clamp01(zooms[curCamp] + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zooms[curCamp]);
        stick.localPosition = new Vector3(0f, 0f, distance); // 在rotation存在的情况下 localPosition的调整变得很简便

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zooms[curCamp]);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f); // 直接调整localRotation

        positions[curCamp] = ClampPosition(transform.localPosition);
        transform.localPosition = positions[curCamp];
    }

    void AdjustPosition(float xDelta, float zDelta)
    {
        //注：rotation * vector3 就是将vector3 按照 rotation 旋转
        Vector3 direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized; // 单位化
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta)); // 阻尼系数
        float moveSpeed = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zooms[curCamp]); // 计算不同缩放比例下的速度
        float distance = moveSpeed * damping * Time.deltaTime; // 计算位移大小（乘上阻尼有奇效，使移动速度和xDelta、zDelta的真实大小成正比）

        positions[curCamp] += direction * distance;
        positions[curCamp] = ClampPosition(positions[curCamp]);
        transform.localPosition = positions[curCamp];
    }

    Vector3 ClampPosition(Vector3 position)
    {
        float xoffset = xMaxOffset * (1f - zooms[curCamp]);
        float zoffset = zMaxOffset * (1f - zooms[curCamp]);
        position.x = Mathf.Clamp(position.x, xMin + xoffset, xMax - xoffset);
        position.z = Mathf.Clamp(position.z, zMin + zoffset, zMax - zoffset);
        return position;
    }

    void AdjustRotation(float delta)
    {
        rotationAngles[curCamp] += delta * rotationSpeed * Time.deltaTime;
        while (rotationAngles[curCamp] < 0f) {
            rotationAngles[curCamp] += 360f;
        }
        while (rotationAngles[curCamp] >= 360f) {
            rotationAngles[curCamp] -= 360f;
        }
        rotations[curCamp] = Quaternion.Euler(0f, rotationAngles[curCamp], 0f);
        transform.localRotation = rotations[curCamp];
    }
}
