using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    HexCell[] cells;
    public HexMesh terrain, rivers, water, waterShore, blackWater, blackWaterShore, estuaries, abyss;
    public HexFeatureManager features;
    Canvas gridCanvas;

    static Color color1 = new Color(1f, 0f, 0f);
    static Color color2 = new Color(0f, 1f, 0f);
    static Color color3 = new Color(0f, 0f, 1f);

    private void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        //ShowUI(false);
    }

    public void AddCell(int index, HexCell cell)
    {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
        if (cell.SmokeEffect)
            cell.SmokeEffect.SetParent(transform, false);
    }

    public void Refresh() // enabled设为true，注意enabled默认就是true，因此不再需要start方法来更新
    {
        enabled = true;
    }

    private void LateUpdate() // 更新
    {
        if (enabled) Triangulate();
        enabled = false;
    }

    public void Triangulate()
    {
        terrain.Clear();
        rivers.Clear();
        water.Clear();
        waterShore.Clear();
        blackWater.Clear();
        blackWaterShore.Clear();
        estuaries.Clear();
        features.Clear();
        abyss.Clear();
        for (int i = 0; i < cells.Length; i++) {
            Triangulate(cells[i]);
        }
        terrain.Apply();
        rivers.Apply();
        water.Apply();
        waterShore.Apply();
        blackWater.Apply();
        blackWaterShore.Apply();
        estuaries.Apply();
        features.Apply();
        abyss.Apply();
    }

    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible); // 直接设置gridCanvas是否Active
    }

    void Triangulate(HexCell cell)
    {
        //三角化六个三角形
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            Triangulate(d, cell);
        }
        if (!cell.IsUnderwater) {
            if (!cell.HasRiver) {
                features.AddFeature(cell, cell.Position);
            }
            if (cell.IsSpecial) {
                features.AddSpecialFeature(cell, cell.Position);
            }
        }
    }

    void Triangulate(HexDirection direction, HexCell cell) // 三角化cell的一个direction
    {
        Vector3 center = cell.Position; // 获取坐标，作为要加入vertices的点
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);
        EdgeVertices e = new EdgeVertices(v1, v2);
        if (cell.HasRiver) {
            if (cell.HasRiverThroughEdge(direction)) {
                e.v3.y = cell.StreamBedY;
                if (cell.HasRiverBeginOrEnd) {
                    TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                }
                else {
                    TriangulateWithRiver(direction, cell, center, e);
                }
            }
            else {
                TriangulateAdjacentToRiver(direction, cell, center, e);
            }
        }
        else {
            TriangulateEdgeFan(center, e, cell.TerrainTypeIndex); // 扇面剖分，若加入道路系统还要加入道路的

            if (!cell.IsUnderwater) {
                features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
            }
        }

        if (direction <= HexDirection.SE) {
            TriangulateConnection(direction, cell, e);
        }

        if (cell.IsUnderwater) {
            TriangulateWaterOrAbyss(direction, cell, center, 0);
        }
        if (cell.isAbyss) {
            TriangulateWaterOrAbyss(direction, cell, center, 1);
        }
    }

    //创建一个方向的水体扇面、连接处、顶点三角
    void TriangulateWaterOrAbyss(HexDirection direction, HexCell cell, Vector3 center, int flag)
    {
        //水体或abyss扇面
        if (flag == 0) center.y = cell.WaterSurfaceY;
        else if (flag == 1) center.y = cell.ImHeight;
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor != null && !neighbor.IsUnderwater) {
            TriangulateWaterShoreOrCliff(direction, cell, neighbor, center, flag);
        }
        else {
            TriangulateOpenWaterOrAbyss(direction, cell, neighbor, center, flag);
        }
    }

    // 创建一个方向的开放水体
    void TriangulateOpenWaterOrAbyss(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center, int flag)
    {
        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);

        if (flag == 0) {
            if (!cell.AbnormalWater)
                water.AddTriangle(center, c1, c2);
            else
                blackWater.AddTriangle(center, c1, c2);
        }
        else if (flag == 1) {
            abyss.AddTriangle(center, c1, c2);
        }

        // 连接桥
        if (direction <= HexDirection.SE && neighbor != null) {
            Vector3 bridge = HexMetrics.GetWaterBridge(direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            if (flag == 0) {
                if (!cell.AbnormalWater)
                    water.AddQuad(c1, c2, e1, e2);
                else
                    blackWater.AddQuad(c1, c2, e1, e2);
            }
            else if (flag == 1) {
                abyss.AddQuad(c1, c2, e1, e2);
            }

            // 顶点三角
            if (direction <= HexDirection.E) {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater) {
                    return;
                }
                if (flag == 0) {
                    if (!cell.AbnormalWater)
                        water.AddTriangle(
                            c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next())
                        );
                    else
                        blackWater.AddTriangle(
                            c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next())
                        );
                }
                else if (flag == 1) {
                    abyss.AddTriangle(
                            c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next())
                    );
                }
            }
        }
    }

    // 创建一个方向的沿岸水体扇面
    void TriangulateWaterShoreOrCliff(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center, int flag)
    {
        // 细分扇面
        EdgeVertices e1 = new EdgeVertices(
            center + HexMetrics.GetFirstWaterCorner(direction),
            center + HexMetrics.GetSecondWaterCorner(direction)
        );
        if (flag == 0) {
            if (!cell.AbnormalWater) {
                water.AddTriangle(center, e1.v1, e1.v2);
                water.AddTriangle(center, e1.v2, e1.v3);
                water.AddTriangle(center, e1.v3, e1.v4);
                water.AddTriangle(center, e1.v4, e1.v5);
            }
            else {
                blackWater.AddTriangle(center, e1.v1, e1.v2);
                blackWater.AddTriangle(center, e1.v2, e1.v3);
                blackWater.AddTriangle(center, e1.v3, e1.v4);
                blackWater.AddTriangle(center, e1.v4, e1.v5);
            }
        }
        else if (flag == 1) {
            abyss.AddTriangle(center, e1.v1, e1.v2);
            abyss.AddTriangle(center, e1.v2, e1.v3);
            abyss.AddTriangle(center, e1.v3, e1.v4);
            abyss.AddTriangle(center, e1.v4, e1.v5);
        }

        // 细分连接桥（已经确认了neighbor是存在的），注意转而用waterShore渲染
        Vector3 center2 = neighbor.Position;
        center2.y = center.y;
        EdgeVertices e2 = new EdgeVertices(
            center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
            center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite())
        );
        if (cell.HasRiverThroughEdge(direction)) { // 河口特殊处理
            TriangulateEstuary(e1, e2, cell.IncomingRiver == direction, cell, flag);
        }
        else {
            if (flag == 0) {
                if (!cell.AbnormalWater) {
                    waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
                    waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
                    waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
                    waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
                    waterShore.AddQuadUV(0f, 0f, 0f, 1f);
                    waterShore.AddQuadUV(0f, 0f, 0f, 1f);
                    waterShore.AddQuadUV(0f, 0f, 0f, 1f);
                    waterShore.AddQuadUV(0f, 0f, 0f, 1f);
                }
                else {
                    blackWaterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
                    blackWaterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
                    blackWaterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
                    blackWaterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
                    blackWaterShore.AddQuadUV(0f, 0f, 0f, 1f);
                    blackWaterShore.AddQuadUV(0f, 0f, 0f, 1f);
                    blackWaterShore.AddQuadUV(0f, 0f, 0f, 1f);
                    blackWaterShore.AddQuadUV(0f, 0f, 0f, 1f);
                }
            }
            else if (flag == 1) {
                abyss.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
                abyss.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
                abyss.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
                abyss.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            }
        }

        // 顶点三角
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (nextNeighbor != null) {
            // 分情况，如果下一个邻居不再水下，则采用solidCorner（较短）；否则还要用waterCorner（较长）
            Vector3 v3 = nextNeighbor.Position + (nextNeighbor.IsUnderwater ?
                HexMetrics.GetFirstWaterCorner(direction.Previous()) :
                HexMetrics.GetFirstSolidCorner(direction.Previous()));
            v3.y = center.y;
            if (flag == 0) {
                if (!cell.AbnormalWater) {
                    waterShore.AddTriangle(e1.v5, e2.v5, v3);
                    waterShore.AddTriangleUV(
                        new Vector2(0f, 0f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f)
                    );
                }
                else {
                    blackWaterShore.AddTriangle(e1.v5, e2.v5, v3);
                    blackWaterShore.AddTriangleUV(
                        new Vector2(0f, 0f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f)
                    );
                }
            }
            else if (flag == 1) {
                abyss.AddTriangle(e1.v5, e2.v5, v3);
            }
        }
    }

    //处理河口水体
    void TriangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool incomingRiver, HexCell cell, int flag)
    {
        if (!cell.AbnormalWater) {
            waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
            waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
            waterShore.AddTriangleUV(
                new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
            );
            waterShore.AddTriangleUV(
                new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
            );
        }
        else {
            blackWaterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
            blackWaterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
            blackWaterShore.AddTriangleUV(
                new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
            );
            blackWaterShore.AddTriangleUV(
                new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
            );
        }

        // 河口水面的三角化
        if (flag == 0) {
            estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
            estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
            estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

            //添加UV集（WaterShore的部分）
            estuaries.AddQuadUV(
                new Vector2(0f, 1f), new Vector2(0f, 0f),
                new Vector2(0f, 1f), new Vector2(0f, 0f)
            );
            estuaries.AddTriangleUV(
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f)
            );
            estuaries.AddQuadUV(
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f)
            );

            // 添加UV2集（River的部分）
            if (incomingRiver) {
                estuaries.AddQuadUV2(
                    new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f),
                    new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f)
                );
                estuaries.AddTriangleUV2(
                    new Vector2(0.5f, 1.1f),
                    new Vector2(1f, 0.8f),
                    new Vector2(0f, 0.8f)
                );
                estuaries.AddQuadUV2(
                    new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f),
                    new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f)
                );
            }
            else {
                estuaries.AddQuadUV2(
                    new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
                    new Vector2(0f, 0f), new Vector2(0.5f, -0.3f)
                );
                estuaries.AddTriangleUV2(
                    new Vector2(0.5f, -0.3f),
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f)
                );
                estuaries.AddQuadUV2(
                    new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
                    new Vector2(1f, 0f), new Vector2(1.5f, -0.2f)
                );
            }
        }
        else if (flag == 1) {
            abyss.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
            abyss.AddTriangle(e1.v3, e2.v2, e2.v4);
            abyss.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);
        }
    }

    // 创建三角形扇面
    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float type)
    {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangle(center, edge.v4, edge.v5);

        terrain.AddTriangleColor(color1);
        terrain.AddTriangleColor(color1);
        terrain.AddTriangleColor(color1);
        terrain.AddTriangleColor(color1);

        Vector3 types;
        types.x = types.y = types.z = type;
        terrain.AddTriangleTerrainTypes(types);
        terrain.AddTriangleTerrainTypes(types);
        terrain.AddTriangleTerrainTypes(types);
        terrain.AddTriangleTerrainTypes(types);
    }

    // 创建有河道经过时的扇面
    void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        Vector3 centerL, centerR;
        if (cell.HasRiverThroughEdge(direction.Opposite())) {
            //将center展成一条边
            centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next())) {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous())) {
            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2())) {
            centerL = center;
            centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.innerToOuter);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous2())) {
            centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
            centerR = center;
        }
        else {
            centerL = centerR = center;
        }
        center = Vector3.Lerp(centerL, centerR, 0.5f); // center不再是原center

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(centerL, e.v1, 0.5f),
            Vector3.Lerp(centerR, e.v5, 0.5f),
            1f / 6f);
        m.v3.y = center.y = e.v3.y; // 中心点和 m边中点 都下沉

        // terrain的三角化
        TriangulateEdgeStrip(m, color1, cell.TerrainTypeIndex, e, color1, cell.TerrainTypeIndex);
        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddTriangle(centerR, m.v4, m.v5);

        terrain.AddTriangleColor(color1);
        terrain.AddQuadColor(color1);
        terrain.AddQuadColor(color1);
        terrain.AddTriangleColor(color1);

        Vector3 types;
        types.x = types.y = types.z = cell.TerrainTypeIndex;
        terrain.AddTriangleTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);
        terrain.AddTriangleTerrainTypes(types);

        // river的三角化
        if (!cell.IsUnderwater) {
            bool reversed = cell.IncomingRiver == direction;
            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
        }
    }

    // 创建有河道初始、结束的扇面
    void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        //三角化河床
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f));
        m.v3.y = e.v3.y;
        TriangulateEdgeStrip(m, color1, cell.TerrainTypeIndex, e, color1, cell.TerrainTypeIndex);
        TriangulateEdgeFan(center, m, cell.TerrainTypeIndex);

        //三角化河流
        if (!cell.IsUnderwater) {
            bool reversed = cell.HasIncomingRiver; // 不是流入就是正向流出的
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
            //TriangulateRiverTriangle(center, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed);// 顺时针
            center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
            rivers.AddTriangle(center, m.v2, m.v4);
            if (reversed) {
                rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f),
                    new Vector2(1f, 0.2f), new Vector2(0f, 0.2f)
                );
            }
            else {
                rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f),
                    new Vector2(0f, 0.6f), new Vector2(1f, 0.6f)
                );
            }
        }
    }

    // 创建所属cell有河道、且毗邻河道（河道不在本扇面上）的扇面
    void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        // 先调整center的位置
        if (cell.HasRiverThroughEdge(direction.Next())) {
            if (cell.HasRiverThroughEdge(direction.Previous())) { // 二折弯内
                center += HexMetrics.GetSolidEdgeMiddle(direction) *
                    (HexMetrics.innerToOuter * 0.5f);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous2())) { // 直道
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (
            cell.HasRiverThroughEdge(direction.Previous()) &&
            cell.HasRiverThroughEdge(direction.Next2())
        ) { // 直道
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );

        TriangulateEdgeStrip(m, color1, cell.TerrainTypeIndex, e, color1, cell.TerrainTypeIndex);
        TriangulateEdgeFan(center, m, cell.TerrainTypeIndex);

        if (!cell.IsUnderwater) {
            features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
        }
    }

    //创建四边形
    void TriangulateEdgeStrip(EdgeVertices e1, Color c1, float type1, EdgeVertices e2, Color c2, float type2)
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

        terrain.AddQuadColor(c1, c2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuadColor(c1, c2);

        Vector3 types;
        types.x = types.z = type1;
        types.y = type2;
        terrain.AddQuadTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);
    }

    // 绘制瀑布
    void TriangulateWaterfallInWater(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float waterY
    ) // 前两个顶点在顶部，后两个在底部。前后河位、以及水位
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);

        // 差值计算将v3、v4从水底拉回至水面
        float t = (waterY - y2) / (y1 - y2);
        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);

        rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
    }

    // 绘制连接桥
    void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null) return;

        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighbor.Position.y - cell.Position.y; // 桥的另一头的高度要改变
        EdgeVertices e2 = new EdgeVertices(e1.v1 + bridge, e1.v5 + bridge);
        if (cell.HasRiverThroughEdge(direction)) { // 有河床的话还要降低中点高度绘制河床
            e2.v3.y = neighbor.StreamBedY;
            if (!cell.IsUnderwater) { // 不在水下，则绘制河面
                if (!neighbor.IsUnderwater) {
                    bool reversed = cell.HasIncomingRiver && cell.IncomingRiver == direction;
                    TriangulateRiverQuad(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f, reversed);
                }
                else if (cell.Elevation > neighbor.WaterLevel) { // 高于水位，则还要画瀑布
                    TriangulateWaterfallInWater(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        neighbor.WaterSurfaceY
                    );
                }
            }
            else if (!neighbor.IsUnderwater && neighbor.Elevation > cell.WaterLevel) { // 反过来的瀑布
                TriangulateWaterfallInWater(
                    e2.v4, e2.v2, e1.v4, e1.v2,
                    neighbor.RiverSurfaceY, cell.RiverSurfaceY,
                    cell.WaterSurfaceY
                );
            }
        }

        // 根据 坡类 决定 是否在坡上插入平地
        if (cell.GetEdgeType(direction) == HexEdgeType.Slope) {
            TriangulateEdgeTerraces(e1, cell, e2, neighbor);
        }
        else {
            TriangulateEdgeStrip(e1, color1, cell.TerrainTypeIndex, e2, color2, neighbor.TerrainTypeIndex);
        }

        // 绘制顶点三角形
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor != null) {
            Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;//三角形的下一个邻居的定点 高度要改变

            // 根据三个cell的高度中的最低者，以不同的顺序绘制顶点三角形。最低者放前
            if (cell.Elevation <= neighbor.Elevation) {
                if (cell.Elevation <= nextNeighbor.Elevation) {
                    TriangulateCorner(e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor);
                }
                else {
                    TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation) {
                TriangulateCorner(e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell);
            }
            else {
                TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
            }
        }
    }

    // 在坡上插入平地
    void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell
        )
    {
        EdgeVertices e2 = HexMetrics.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(color1, color2, 1);

        float t1 = beginCell.TerrainTypeIndex, t2 = endCell.TerrainTypeIndex;
        TriangulateEdgeStrip(begin, color1, t1, e2, c2, t2);

        for (int i = 2; i < HexMetrics.terraceSteps; ++i) {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = HexMetrics.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(color1, color2, i);
            TriangulateEdgeStrip(e1, c1, t1, e2, c2, t2);
        }

        TriangulateEdgeStrip(e2, c2, t1, end, color2, t2);
    }

    //绘制顶点处三角形
    void TriangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
        )
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope) {
            if (rightEdgeType == HexEdgeType.Slope) // slope-slope-flat情况
            {
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
            else if (rightEdgeType == HexEdgeType.Flat) // slope-flat-slope情况
            {
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            }
            else // slope-cliff-???情况
            {
                TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (rightEdgeType == HexEdgeType.Slope) {
            if (leftEdgeType == HexEdgeType.Flat) // flat-slope-slope情况
            {
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else // cliff-slope-???情况
            {
                TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) // cliff-cliff-slope情况
        {
            if (leftCell.Elevation < rightCell.Elevation) {
                TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else {
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
            }
        }
        else {
            terrain.AddTriangle(bottom, left, right); // 注意要顺时针
            terrain.AddTriangleColor(color1, color2, color3);
            Vector3 types;
            types.x = bottomCell.TerrainTypeIndex;
            types.y = leftCell.TerrainTypeIndex;
            types.z = rightCell.TerrainTypeIndex;
            terrain.AddTriangleTerrainTypes(types);
        }
    }

    // 在三角形上加平地
    void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
        )
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(color1, color2, 1);
        Color c4 = HexMetrics.TerraceLerp(color1, color3, 1);
        Vector3 types;
        types.x = beginCell.TerrainTypeIndex;
        types.y = leftCell.TerrainTypeIndex;
        types.z = rightCell.TerrainTypeIndex;

        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleColor(color1, c3, c4);
        terrain.AddTriangleTerrainTypes(types);

        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(color1, color2, i);
            c4 = HexMetrics.TerraceLerp(color1, color3, i);
            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadColor(c1, c2, c3, c4);
            terrain.AddQuadTerrainTypes(types);
        }

        terrain.AddQuad(v3, v4, left, right);
        terrain.AddQuadColor(c3, c4, color2, color3);
        terrain.AddQuadTerrainTypes(types);
    }

    //在三角形上加平地+悬崖
    void TriangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
        )
    {
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b); // 根据扰动处理过后的顶点得到
        Color boundaryColor = Color.Lerp(color1, color3, b);
        Vector3 types;
        types.x = beginCell.TerrainTypeIndex;
        types.y = leftCell.TerrainTypeIndex;
        types.z = rightCell.TerrainTypeIndex;

        TriangulateBoundaryTriangle(begin, color1, left, color2, boundary, boundaryColor, types);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            //顺时针顺序
            TriangulateBoundaryTriangle(left, color2, right, color3, boundary, boundaryColor, types);
        }
        else {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(color2, color3, boundaryColor);
            terrain.AddTriangleTerrainTypes(types);
        }
    }

    //在三角形上加悬崖+平地
    void TriangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
        )
    {
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        Color boundaryColor = Color.Lerp(color1, color2, b);
        Vector3 types;
        types.x = beginCell.TerrainTypeIndex;
        types.y = leftCell.TerrainTypeIndex;
        types.z = rightCell.TerrainTypeIndex;

        //顺时针，boundary放后面
        TriangulateBoundaryTriangle(right, color3, begin, color1, boundary, boundaryColor, types);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(left, color2, right, color3, boundary, boundaryColor, types);
        }
        else {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(color2, color3, boundaryColor);
            terrain.AddTriangleTerrainTypes(types);
        }
    }

    // 悬崖三角形：先画底下的三角形
    void TriangulateBoundaryTriangle(
        Vector3 begin, Color beginColor,
        Vector3 left, Color leftColor,
        Vector3 boundary, Color boundaryColor, Vector3 types
        )
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginColor, leftColor, 1);
        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary); // 确保boundary要在cliff的边缘上，不能扰动
        terrain.AddTriangleColor(beginColor, c2, boundaryColor);
        terrain.AddTriangleTerrainTypes(types);

        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginColor, leftColor, i);
            terrain.AddTriangleUnperturbed(v1, v2, boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);
            terrain.AddTriangleTerrainTypes(types);
        }

        terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleColor(c2, leftColor, boundaryColor);
        terrain.AddTriangleTerrainTypes(types);
    }

    void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y, float v, bool reversed)
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
    }
    void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float v, bool reversed)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        rivers.AddQuad(v1, v2, v3, v4);
        if (reversed) {
            rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
        }
        else {
            rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
        }
    }
    /*void TriangulateRiverTriangle(
        Vector3 center, Vector3 v1, Vector3 v2,
        float y, float v, bool reversed
        )
    {
        center.y = v1.y = v2.y = y;
        rivers.AddTriangle(center, v1, v2);
        if (reversed) {
            rivers.AddTriangleUV( // 顺时针加入，顶点为贴图的上顶点
                new Vector2(0.5f, 0.8f - v), new Vector2(1f, 0.6f - v), new Vector2(0f, 0.6f - v)
            );
        }
        else {
            rivers.AddTriangleUV( // 顶点为贴图的下顶点
                new Vector2(0.5f, v), new Vector2(0f, v + 0.2f), new Vector2(1f, v + 0.2f)
            );
        }
    }*/
}
