using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;

[System.Serializable] // 让Unity可以存储这个结构体,Inspector可以显示这个结构体
public struct HexCoordinates
{
    [SerializeField]
    private int x, z;

    public int X { get { return x; } }
    public int Y { get { return -X - Z; } } // 注意到X + Y + Z = 0
    public int Z { get { return z; } }

    public HexCoordinates(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        x -= HexMetrics.borderCellCountX + HexMetrics.AC_offsetX;
        z -= HexMetrics.borderCellCountZ + HexMetrics.AC_offsetZ;
        if (z > 0 && z % 2 == 1) x -= 1;
        return new HexCoordinates(x - z / 2, z);
    }

    public override string ToString()
    {
        return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
    }

    public string ToStringOnSeparateLines()
    {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }

    public static HexCoordinates FromPosition(Vector3 position)
    {
        float x = position.x / (HexMetrics.innerRadius * 2f);
        float y = -x; // y坐标与x坐标是对称的
        float offset = position.z / (HexMetrics.outerRadius * 3f);
        x -= offset; // z每上升两行（即3个外半径），x坐标、y坐标都要减一
        y -= offset;
        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);
        if (iX + iY + iZ != 0) { // 四舍五入的结果是不精确的
            //算法就是：重新构建具有最大不精确度的坐标
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ) {
                iX = -iY - iZ;
            }
            else if (dZ > dY) {
                iZ = -iX - iY;
            }
            //不需要重新构建iY
        }
        ///
        iZ -= HexMetrics.borderCellCountZ + HexMetrics.AC_offsetZ;
        iX += (HexMetrics.borderCellCountZ + HexMetrics.AC_offsetZ) / 2 - HexMetrics.borderCellCountX - HexMetrics.AC_offsetX;
        ///
        return new HexCoordinates(iX, iZ);
    }

    public int DistanceTo(HexCoordinates other)
    {
        return
            ((x < other.x ? other.x - x : x - other.x) +
            (Y < other.Y ? other.Y - Y : Y - other.Y) +
            (z < other.z ? other.z - z : z - other.z)) / 2;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(x);
        writer.Write(z);
    }
    public static HexCoordinates Load(BinaryReader reader)
    {
        HexCoordinates c;
        c.x = reader.ReadInt32();
        c.z = reader.ReadInt32();
        return c;
    }
}

public class HexCell : MonoBehaviour
{
    public HexCoordinates coordinates; // 坐标
    public RectTransform uiRect; // 字体的位置
    public HexGridChunk chunk; // 所属cell块 （引用）
    public Transform SmokeEffect; // For Abyss
    public Transform specialFeature; // 其拥有的specialFeature

    public bool isInGame;
    public bool isAbyss;

    public float ImHeight; // Imaginary height for Abyss Cell
    int elevation = int.MinValue; // 以  −2,147,483,648 初始化
    public int Elevation {
        get {
            return elevation;
        }
        set {
            if (elevation == value) return;
            elevation = value;
            //cell位置变化
            RefreshPosition();
            // 防止河流上流
            ValidateRivers();

            Refresh();
        }
    }
    void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
        transform.localPosition = position;

        ImHeight = Mathf.Max(position.y, WaterSurfaceY + 0.5f * HexMetrics.elevationStep);
        if (isAbyss) {
            HexCell neighbor = GetNeighbor(HexDirection.W);
            if (neighbor) ImHeight = neighbor.ImHeight;
            neighbor = GetNeighbor(HexDirection.SW);
            if (neighbor) ImHeight = Mathf.Min(ImHeight, neighbor.ImHeight);
            neighbor = GetNeighbor(HexDirection.SE);
            if (neighbor) ImHeight = Mathf.Min(ImHeight, neighbor.ImHeight);
        }

        //字体位置变化
        Vector3 uiPosition = uiRect.localPosition;
        // 注意，这里吧水面也考虑进去了！
        uiPosition.z = -ImHeight;
        uiRect.localPosition = uiPosition;

        // 深渊位置变化
        if (isAbyss) {
            SmokeEffect.localPosition = new Vector3(Position.x, ImHeight, Position.z);
        }
    }

    // 地形编号
    int terrainTypeIndex;
    public int TerrainTypeIndex {
        get {
            return terrainTypeIndex;
        }
        set {
            if (terrainTypeIndex != value) {
                terrainTypeIndex = value;
                Refresh();
            }
        }
    }

    public Vector3 Position {
        get {
            return transform.localPosition;
        }
    }

    bool hasIncomingRiver, hasOutgoingRiver;
    HexDirection incomingRiver, outgoingRiver;
    public bool HasIncomingRiver {
        get {
            return hasIncomingRiver;
        }
    }
    public bool HasOutgoingRiver {
        get {
            return hasOutgoingRiver;
        }
    }
    public HexDirection IncomingRiver {
        get {
            return incomingRiver;
        }
    }
    public HexDirection OutgoingRiver {
        get {
            return outgoingRiver;
        }
    }
    public bool HasRiver {
        get {
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }
    public bool HasRiverBeginOrEnd {
        get {
            return hasIncomingRiver != hasOutgoingRiver;
        }
    }
    public bool HasRiverThroughEdge(HexDirection direction) // 原版教程里没有括号？
    {
        return
            (hasIncomingRiver && incomingRiver == direction) ||
            (hasOutgoingRiver && outgoingRiver == direction);
    }

    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver) {
            return;
        }
        hasOutgoingRiver = false;
        RefreshSelfOnly(); // 不需要把六个邻居都遍历一遍

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }
    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver) {
            return;
        }
        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }
    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void SetoutgoingRiver(HexDirection direction)
    {
        if (hasOutgoingRiver && outgoingRiver == direction) return;
        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor)) return;
        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction) {
            RemoveIncomingRiver();
        }

        hasOutgoingRiver = true;
        outgoingRiver = direction;
        specialIndex = 0;
        RefreshSelfOnly(); // 在第七章时被删去了

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.specialIndex = 0;
        neighbor.RefreshSelfOnly();
    }

    public float StreamBedY {
        get {
            return (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
        }
    }

    public float RiverSurfaceY {
        get {
            return (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
        }
    }

    bool abnormalWater = false;
    public bool AbnormalWater {
        get { return abnormalWater; }
        set {
            if (abnormalWater == value) return;
            abnormalWater = value;
            if (IsUnderwater) Refresh();
        }
    }
    int waterLevel;
    public int WaterLevel { // 标准水位
        get {
            return waterLevel;
        }
        set { // 可以强制某些水下建筑结构
            if (waterLevel == value) {
                return;
            }
            waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }

    public bool IsUnderwater {
        get {
            return waterLevel > elevation;
        }
    }

    public float WaterSurfaceY {
        get {
            return
                (waterLevel + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    bool IsValidRiverDestination(HexCell neighbor) // 检测是否可以有河流流到neighbor
    {
        return neighbor && (
            elevation >= neighbor.elevation || waterLevel == neighbor.elevation
        );
    }
    void ValidateRivers()
    {
        if (hasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(outgoingRiver))) {
            RemoveOutgoingRiver();
        }
        if (hasIncomingRiver && !GetNeighbor(incomingRiver).IsValidRiverDestination(this)) {
            RemoveIncomingRiver();
        }
    }

    int feature01Level, feature02Level, feature03Level;
    public int Feature01Level {
        get {
            return feature01Level;
        }
        set {
            if (feature01Level != value) {
                feature01Level = value;
                RefreshSelfOnly();
            }
        }
    }
    public int Feature02Level {
        get {
            return feature02Level;
        }
        set {
            if (feature02Level != value) {
                feature02Level = value;
                RefreshSelfOnly();
            }
        }
    }
    public int Feature03Level {
        get {
            return feature03Level;
        }
        set {
            if (feature03Level != value) {
                feature03Level = value;
                RefreshSelfOnly();
            }
        }
    }

    public int specialIndex; // 一定注意，从 1 开始！
    public int SpecialIndex {
        get {
            return specialIndex;
        }
        set {
            if (specialIndex != value && !HasRiver) {
                specialIndex = value;
                RefreshSelfOnly();
            }
        }
    }
    public bool IsSpecial {
        get {
            return specialIndex > 0;
        }
    }

    [SerializeField]
    HexCell[] neighbors = new HexCell[6];
    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }
    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this; // 立刻将对面的cell的相反方向的邻居记为自己
    }

    public HexEdgeType GetEdgeType(HexDirection direction) // 得到相应方向的邻居之间的坡类
    {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
    }
    public HexEdgeType GetEdgeType(HexCell otherCell) // 得到cell和otherCell之间的坡类
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    public void Refresh() // 不用设为public，因为cell自己知道什么时候进行刷新 // 更新，在更新驻扎点的时候用了一次
    {
        if (chunk) {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++) {
                HexCell neighbor = neighbors[i];
                // 如果邻居不在一个chunk上，则要刷新相邻的chunk
                if (neighbor != null && neighbor.chunk != chunk) {
                    neighbor.chunk.Refresh();
                }
            }

            if (Unit) Unit.ValidatePosition();
            if (FlyingUnit) FlyingUnit.ValidatePosition();
        }
    }
    void RefreshSelfOnly()
    {
        chunk.Refresh();
        if (Unit) Unit.ValidatePosition();
        if (FlyingUnit) FlyingUnit.ValidatePosition();
    }

    int distance; // 寻路距离
    public int Distance {
        get { return distance; }
        set {
            distance = value;
        }
    }

    public void SetLabel(string text)
    {
        Text label = uiRect.GetComponent<Text>();
        label.text = text;
    }

    bool isHighlighted;
    public bool IsHighlighted {
        get { return isHighlighted; }
    }
    public void DisableHighlight()
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
        isHighlighted = false;
    }
    public void EnableHighlight(Color color)
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.color = color; // 可以直接调色
        highlight.enabled = true;
        isHighlighted = true;
    }
    public void ChangeHighlight(Color color)
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.color = color;
    }

    // 寻路的时候，标记这个cell的父cell
    public HexCell PathFrom { get; set; }
    // 标记一次search后cell是否是可到达的
    public bool reachable;
    public bool inAtkMaxRan;
    public bool inAtkRan;
    public bool attackable_G;
    public bool attackable_F;

    public HexUnit Unit { get; set; }
    public HexUnit FlyingUnit { get; set; }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)elevation);
        writer.Write((byte)waterLevel);
        writer.Write(abnormalWater);
        writer.Write((byte)feature01Level);
        writer.Write((byte)feature02Level);
        writer.Write((byte)feature03Level);
        writer.Write((byte)specialIndex);

        if (hasIncomingRiver) {
            writer.Write((byte)(incomingRiver + 128));
        }
        else {
            writer.Write((byte)0);
        }

        if (hasOutgoingRiver) {
            writer.Write((byte)(outgoingRiver + 128));
        }
        else {
            writer.Write((byte)0);
        }
    }
    public void Load(BinaryReader reader)
    {
        terrainTypeIndex = reader.ReadByte();
        elevation = reader.ReadByte();
        waterLevel = reader.ReadByte();
        abnormalWater = reader.ReadBoolean();
        feature01Level = reader.ReadByte();
        feature02Level = reader.ReadByte();
        feature03Level = reader.ReadByte();
        specialIndex = reader.ReadByte();

        byte riverData = reader.ReadByte();
        if (riverData >= 128) {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else {
            hasIncomingRiver = false;
        }
        riverData = reader.ReadByte();
        if (riverData >= 128) {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(riverData - 128);
        }
        else {
            hasOutgoingRiver = false;
        }

        RefreshPosition();
    }
}
