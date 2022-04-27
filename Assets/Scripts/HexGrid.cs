using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI; // 要用到Text
using UnityEngine;
using System.IO;
using System;

public class HexGrid : MonoBehaviour
{
    public GameUI newGameUI;
    public Material[] terrainMaterial;

    // 所加载的 地图文件名、地图材质序号
    public string mapname;
    public int mapMatIndex;

    public int curTurn = -1; // 当前游戏回合

    public int cellCountX = 20, cellCountZ = 15; // 总的cell的数目
    int chunkCountX, chunkCountZ; // chunk数目

    public HexCell cellPrefab;
    public Text cellLabelPrefab;
    public HexGridChunk chunkPrefab;

    public GameObject SmokeEffectPrefab; // 悬崖烟雾特效
    public GameObject[] MagicFrontEffectPrefab; // 法阵特效
    public GameObject[] SummonEffectPrefab; // 召唤特效所有类别，取决于生物
    public GameObject DamageEffectPrefab;
    public GameObject RecoverEffectPrefab; // 回复特效
    public GameObject StrengthEffectPrefab; // 强化特效
    public GameObject GloryEffectPrefab; // 圣光之耀特效
    public GameObject HellFireEffectPrefab; // 地狱之火特效
    public GameObject WindEffectPrefab; // 风神之佑特效

    // 持续性BUFF特效
    public GameObject ShieldEffectPrefab; // 圣盾特效
    public GameObject ShieldBrkEffectPrefab; // 圣盾爆裂特效
    public GameObject CarryATFBuffPrefab; // 攻击加成特效
    public GameObject ATFDebuffPrefab; // 卸除攻击加成特效

    HexCell[] cells;
    HexGridChunk[] chunks;

    public int seed;
    public Texture2D noiseSource; // 不是一个组件，只能在HexGrid进行处理

    public Canvas hpCanvas;
    public Slider[] hpBarPrefab;

    public HexUnit[] unitPrefabs1; // 存有一级、二级生物prefabs类别
    public HexUnit[] unitPrefabs3; // 存有三级生物prefabs类别
    List<HexUnit> units = new List<HexUnit>(); // 当下的活着的生物队列
    public List<Command> commands = new List<Command>();
    public int curComInx = 0;
    public bool onPlay;

    [NonSerialized]
    public int activeCamp;

    public MsgDisplayer msgDisplayer;

    public void Initiate()
    {
        // 初始化HexMetrics的一些静态变量
        HexMetrics.noiseSource = noiseSource; // 让Metrics获取纹理图片的引用
        HexMetrics.InitializeHashGrid(seed);
        //HexUnit.unitPrefab = unitPrefab;

        // 一般游戏模式下，本调用默认的CreateMap函数
        //CreateMap(cellCountX, cellCountZ);
        onPlay = false;

        // AC场景直接读入standard1.map来初始化地图
        mapname = HexMetrics.mapName[newGameUI.mapIndex];
        mapMatIndex = HexMetrics.matIndex[newGameUI.mapIndex];
        LoadMap();

        // 特殊生物 —— 神迹
        AddUnit(0, 1, 0, false, GetCell(new HexCoordinates(-7, 0)), UnityEngine.Random.value * 360f);
        AddUnit(0, 1, 1, false, GetCell(new HexCoordinates(7, 0)), UnityEngine.Random.value * 360f);

        activeCamp = 0;
        curTurn = -1;
    }
    private void OnEnable() // 在重新编译时调用。
    {
        if (!HexMetrics.noiseSource) {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            //HexUnit.unitPrefab = unitPrefab;
        }
    }

    // 初始化地图
    public bool CreateMap(int x, int z)
    {
        if (
            x <= 0 || x % HexMetrics.chunkSizeX != 0 ||
            z <= 0 || z % HexMetrics.chunkSizeZ != 0
        ) {
            Debug.LogError("Unsupported map size.");
            return false;
        }

        /// reset
        ClearPath();
        ClearHighlight();
        ClearUnits();
        if (chunks != null) {
            for (int i = 0; i < chunks.Length; i++) {
                Destroy(chunks[i].gameObject); // chunks的children，cells，随后也会被Destroy
            }
        }

        /// Create
        cellCountX = x;
        cellCountZ = z;

        // 获取chunk数目
        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();
        // HexMapCamera.ValidatePosition(); // 此处和原教程不一样，是智能体场景特判代码

        return true;
    }

    // 初始化地图块（每个地图块含有chunkCountX * chunkCountz个六边形cell）
    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++) {
            for (int x = 0; x < chunkCountX; x++) {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.GetChild(1).GetComponent<MeshRenderer>().material = terrainMaterial[0]; // 更改地形材质
                chunk.transform.SetParent(transform);
            }
        }
    }

    // 初始化所有的六边形cell
    void CreateCells()
    {
        cells = new HexCell[cellCountZ * cellCountX];

        for (int z = 0, i = 0; z < cellCountZ; z++) {
            for (int x = 0; x < cellCountX; x++) {
                CreateCell(x, z, i++); // 先在对应的位置将HexCell实例化
            }
        }

    }

    // 初始化位置是x, z（注意不是cell的最终坐标，只是方便遍历）的六边形cell
    void CreateCell(int x, int z, int i)
    {
        // 将参数x, y转化成cell的物理坐标position
        Vector3 position;
        position.x = (x + z % 2 * 0.5f) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        cells[i] = Instantiate<HexCell>(cellPrefab);
        cells[i].transform.localPosition = position; // 实际位置初始化
        cells[i].coordinates = HexCoordinates.FromOffsetCoordinates(x, z); // 将参数x, z转化为cells[i].coordinates.的X、Y、Z最终坐标
        setCellProp(cells[i]);

        // 邻居初始化
        if (x > 0) {
            cells[i].SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0) {
            if ((z & 1) == 0) { // 是偶数
                cells[i].SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0) {
                    cells[i].SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else // 是奇数
            {
                cells[i].SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1) {
                    cells[i].SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }

        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        //label.text = cells[i].coordinates.ToString(); // 仅仅是改变了cells[i].coordinates.的X、Y
        cells[i].uiRect = label.rectTransform;

        if (cells[i].isAbyss) {
            GameObject gameObj = Instantiate(SmokeEffectPrefab);
            cells[i].SmokeEffect = gameObj.transform;
        }
        else cells[i].SmokeEffect = null;

        cells[i].Elevation = 0; // 这一步不能少，因为这就是初始化关键一步，调用了Refresh进而调用Triangulate

        AddCellToChunk(x, z, cells[i]);
    }

    void setCellProp(HexCell cell)
    {
        HexCoordinates coordinates = cell.coordinates;
        /*
         * -14 <= Z <= 14
         * -14 <= X - Y <= 14
         * -8 <= X <= 8
         * -8 <= Y <= 8
         */
        if (coordinates.Z < -14 || coordinates.Z > 14) cell.isInGame = false;
        else if (coordinates.X - coordinates.Y < -14 || coordinates.X - coordinates.Y > 14)
            cell.isInGame = false;
        else if (coordinates.X < -8 || coordinates.X > 8)
            cell.isInGame = false;
        else if (coordinates.Y < -8 || coordinates.Y > 8)
            cell.isInGame = false;
        else cell.isInGame = true;

        cell.isAbyss = false;
        if (cell.isInGame) {
            foreach (int[] C in HexMetrics.abyssCoords) {
                if (C[0] + C[1] + C[2] != 0) Debug.LogError("AbyssCoords Wrong!");
                if (coordinates.X == C[0] && coordinates.Y == C[1]) {
                    cell.isAbyss = true;
                    break;
                }
            }
        }
    }

    //将水平坐标为(x, z)（注意：是原始坐标）cell加入到其对应的chunk
    void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell); // 把加进去所对应的下标先算好
    }

    //根据全局物理坐标得到相应的cell
    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);// 转为局部坐标
        HexCoordinates coordinates = HexCoordinates.FromPosition(position); // 通过局部坐标获取XYZ坐标
        return GetCell(coordinates);
    }
    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        int x = coordinates.X + z / 2;
        /////
        x += HexMetrics.borderCellCountX + HexMetrics.AC_offsetX;
        z += HexMetrics.borderCellCountZ + HexMetrics.AC_offsetZ;
        /////
        int index = x + z * cellCountX;
        if (coordinates.Z > 0 && coordinates.Z % 2 == 1) index += 1;
        if (index < 0 || index >= cells.Length) return null;
        return cells[index];
    }
    public HexCell GetCell(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            return GetCell(hit.point);
        }
        return null;
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < chunks.Length; i++) {
            chunks[i].ShowUI(visible);
        }
    }
    public void setUItoCoord() // 按C显示游戏坐标
    {
        for (int i = 0; i < cells.Length; i++) {
            if (cells[i].isInGame) {
                cells[i].SetLabel(cells[i].coordinates.ToString());
                cells[i].uiRect.GetComponent<Text>().fontSize = 4;
            }
        }
    }
    public void ClearUI()
    {
        for (int i = 0; i < cells.Length; i++) {
            cells[i].SetLabel("");
            cells[i].uiRect.GetComponent<Text>().fontSize = 10;
        }
    }

    /// <summary>
    /// 寻路代码
    /// </summary>
    HexCell currentPathFrom;

    // 生物寻路。
    // 参数mode：0 是移动寻路，1 是攻击寻路，2 是地狱火投放寻路，3 是单纯显示攻击范围
    // 参数target：0 是地面生物的寻路、攻击；1 是飞行生物的寻路、攻击
    public void computeDis(HexCell fromCell, int mode, bool isForFlying, bool showHighlight) 
    {
        currentPathFrom = fromCell;
        if (mode != 2) {
            if (!isForFlying) {
                if (!currentPathFrom.Unit) Debug.LogError("No Unit Selected!");
            }
            else {
                if (!currentPathFrom.FlyingUnit) Debug.LogError("No Unit Selected!");
            }
        }
        // 定义搜索范围
        int speed = 0;
        if (mode == 0) {
            if (!isForFlying) speed = currentPathFrom.Unit.Speed;
            else speed = currentPathFrom.FlyingUnit.Speed;
        }
        else if (mode == 1 || mode == 3) {
            if (!isForFlying) speed = currentPathFrom.Unit.MaxAtkRan;
            else speed = currentPathFrom.FlyingUnit.MaxAtkRan;
        }
        else if (mode == 2) {
            if (fromCell.SpecialIndex == 1 || fromCell.SpecialIndex == 2) {
                speed = 7;
            }
            else if (fromCell.SpecialIndex == 6 || fromCell.SpecialIndex == 7 || fromCell.SpecialIndex == 8) {
                speed = 5;
            }
        }

        // 清理
        if (mode == 0 || mode == 2) {
            for (int i = 0; i < cells.Length; i++) {
                if (cells[i].isInGame) {
                    cells[i].Distance = int.MaxValue;
                    cells[i].reachable = false;
                }
            }
        }
        else if (mode == 1 || mode == 3) {
            for (int i = 0; i < cells.Length; i++) {
                if (cells[i].isInGame) {
                    cells[i].Distance = int.MaxValue;
                    cells[i].attackable_G = false;
                    cells[i].attackable_F = false;
                    cells[i].inAtkMaxRan = false;
                    cells[i].inAtkRan = false;
                }
            }
        }
        
        List<HexCell> frontier = new List<HexCell>();
        // 将初始cell加入
        fromCell.Distance = 0;
        frontier.Add(fromCell);
        while (frontier.Count > 0) {
            // 取出minCell
            int mindis = int.MaxValue;
            HexCell current = null;
            foreach (HexCell cell in frontier) {
                if (cell.Distance < mindis) {
                    mindis = cell.Distance;
                    current = cell;
                }
            }
            frontier.Remove(current);
            // 找到距离超过最大行动力，退出
            if (current.Distance > speed) {
                break;
            }
            else { // 否则还在可到达区域内
                if (current == currentPathFrom && mode == 0) { // 寻路模式，原点不算reachable
                    if (showHighlight) {
                        current.EnableHighlight(GameUI.highlightColors[1]);
                    }
                }
                else { // 新取出来的，已经确定距离的点
                    if (mode == 0 || mode == 2) current.reachable = true; // 更新
                    else if (mode == 1 || mode == 3) current.inAtkMaxRan = true;

                    if (showHighlight) {
                        if (mode == 0) {
                            current.EnableHighlight(GameUI.highlightColors[0]);
                        }
                        else if (mode == 2) {
                            if (!current.Unit) current.EnableHighlight(GameUI.highlightColors[0]); // 没有地面生物
                        }
                        else if (mode == 1 || mode == 3) {
                            if (!isForFlying && current.Distance < currentPathFrom.Unit.MinAtkRan) {
                                current.inAtkRan = false;
                                current.attackable_G = false; // 距离过短
                                current.attackable_F = false;
                            }
                            else if (isForFlying && current.Distance < currentPathFrom.FlyingUnit.MinAtkRan) {
                                current.inAtkRan = false;
                                current.attackable_G = false; // 距离过短
                                current.attackable_F = false;
                            }
                            else {
                                current.inAtkRan = true;
                                int thisCamp = isForFlying ? currentPathFrom.FlyingUnit.Camp : currentPathFrom.Unit.Camp;
                                // 地面生物
                                if (thisCamp == newGameUI.myCamp) { // 算的是本方生物
                                    if (current.Unit && current.Unit.Camp != activeCamp) {
                                        if (current.Unit.Health > 0) current.attackable_G = true;
                                    }

                                    if (current.FlyingUnit && current.FlyingUnit.Camp != activeCamp) {
                                        if (current.FlyingUnit.Health > 0) {
                                            if (isForFlying) current.attackable_F = true;
                                            else {
                                                if (currentPathFrom.Unit.AirCombat) current.attackable_F = true;
                                            }
                                        }
                                    }
                                }
                            }

                            if (current.attackable_G || current.attackable_F) {
                                if (mode == 1)
                                    current.EnableHighlight(GameUI.highlightColors[3]);
                                else if (mode == 3)
                                    current.EnableHighlight(GameUI.highlightColors[6]);
                            }
                            else if (current.inAtkRan) current.EnableHighlight(GameUI.highlightColors[6]);
                        }
                    }

                    // 可以停留，但是不能经过：
                    if (mode == 0) {
                        if (!isForFlying) { // 地面生物计算：敌方飞行生物
                            if (current.FlyingUnit && current.FlyingUnit.Camp != activeCamp) continue;
                        }
                        else { // 空中生物计算：敌方地面生物
                            if (current.Unit && current.Unit.Camp != activeCamp) continue;
                        }

                        // 地面生物计算：邻格有敌方地面生物；空中生物计算：邻格有敌方空中生物
                        bool canContinue = false;
                        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                            HexCell neighbor = current.GetNeighbor(d);
                            if (!isForFlying) {
                                if (neighbor.Unit && neighbor.Unit.Type != 0 && neighbor.Unit.Camp != activeCamp) {
                                    canContinue = true;
                                    break;
                                }
                            }
                            else {
                                if (neighbor.FlyingUnit && neighbor.FlyingUnit.Camp != activeCamp) {
                                    canContinue = true;
                                    break;
                                }
                            }
                        }
                        if (canContinue) continue;
                    }
                }
            }

            // 遍历邻居，更新Distance，入队新发现邻居
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = current.GetNeighbor(d);

                if (neighbor == null) continue;
                if (!neighbor.isInGame) continue;
                if (mode != 1) {
                    if (neighbor.reachable) continue;
                    if (mode == 0) {
                        if (neighbor.IsSpecial)
                            if (neighbor.SpecialIndex == 1 || neighbor.SpecialIndex == 2) continue;
                        if (!isForFlying) {
                            if (neighbor.isAbyss) continue;
                            if (neighbor.Unit) continue;
                        }
                        else {
                            if (neighbor.FlyingUnit) continue;
                        }
                    }
                    else if (mode == 2) {
                        if (neighbor.isAbyss) continue;
                    }
                }
                else {
                    if (neighbor.inAtkMaxRan) continue;
                }

                int distance = current.Distance + 1;

                if (distance >= neighbor.Distance) continue; // 这一步同时也防止了返回
                if (neighbor.Distance == int.MaxValue) {
                    frontier.Add(neighbor); // 还未检测才入队
                }
                neighbor.Distance = distance;
                if (mode != 3) neighbor.PathFrom = current;
            }
        }
    }

    public void ClearPath()
    {
        if (!currentPathFrom) return;
        for (int i = 0; i < cells.Length; i++) {
            if (cells[i].isInGame) {
                cells[i].Distance = int.MaxValue;
                cells[i].reachable = false;
                cells[i].inAtkMaxRan = false;
                cells[i].inAtkRan = false;
                cells[i].attackable_G = false;
                cells[i].attackable_F = false;
            }
        }
        currentPathFrom = null;
    }
    public void ClearHighlight()
    {
        if (cells == null) return; 
        for (int i = 0; i < cells.Length; i++) {
            if (cells[i].IsHighlighted) {
                cells[i].DisableHighlight();
            }
        }
    }
    public List<HexCell> GetPath(HexCell currentPathTo)
    {
        List<HexCell> path = ListPool<HexCell>.Get();
        for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom) {
            path.Add(c);
        }
        path.Add(currentPathFrom); // 现在是从终点回溯到起点
        path.Reverse();
        return path;
    }

    public void readyCast(int atfType) // 在线模式下
    {
        if (atfType == 1 || atfType == 11) { // 圣光之耀
            for (int i = 0; i < cells.Length; i++) {
                if (cells[i].isInGame) {
                    cells[i].EnableHighlight(GameUI.highlightColors[0]);
                }
            }
        }
        else if (atfType == 2 || atfType == 12) { // 阳炎之盾
            for (int i = 0; i < cells.Length; i++) {
                if (cells[i].isInGame) {
                    if (cells[i].Unit && cells[i].Unit.Camp == newGameUI.myCamp) {
                        if (cells[i].Unit.Type != 0) cells[i].EnableHighlight(GameUI.highlightColors[4]);
                    }
                    else if (cells[i].FlyingUnit && cells[i].FlyingUnit.Camp == newGameUI.myCamp) {
                        cells[i].EnableHighlight(GameUI.highlightColors[4]);
                    }
                }
            }
        }
        else if (atfType == 3 || atfType == 13) { // 地狱之火
            if (newGameUI.myCamp == 0) computeDis(GetCell(new HexCoordinates(-7, 0)), 2, false, true);
            else if (newGameUI.myCamp == 1) computeDis(GetCell(new HexCoordinates(7, 0)), 2, false, true);
            for (int i = 0; i < 4; i++) {
                HexCell station = GetCell(new HexCoordinates(HexMetrics.stationCoords[i][0][0], HexMetrics.stationCoords[i][0][2]));
                if (station.SpecialIndex == 6 + newGameUI.myCamp) {
                    computeDis(station, 2, false, true);
                }
            }
            ClearPath();
        }
        else if (atfType == 4 || atfType == 14) { // 风神之佑
            for (int i = 0; i < cells.Length; i++) {
                if (cells[i].isInGame) {
                    cells[i].EnableHighlight(GameUI.highlightColors[0]);
                }
            }
        }
    }

    /// 执行命令
    public bool isRunning = false;
    // 在线模式下记录是否还在本回合操作周期内
    bool isInOperateState = false;
    bool findPriestHealCommand = false;
    public void RunCommands() // 一次执行一条命令
    {
        if (newGameUI.gameMode == 0) {
            if (!onPlay) return;
        }
        float RunInterval = 0.5f;

        if (newGameUI.gameMode == 1 && curComInx == commands.Count) { // 暂时把命令跑完
            isRunning = false;
        }
        while (curComInx < commands.Count) {
            Command C = commands[curComInx];
            curComInx += 1;
            Debug.Log(string.Format("in RunCommands: curComInx = {0}, {1} [{2},{3},{4},{5},{6}]", 
                curComInx, C.ToString(), C.arg[0], C.arg[1], C.arg[2], C.arg[3], C.arg[4]));
            msgDisplayer.display(C);
            if (C.type == CommandType.GAMESTARTED) { // 游戏开始
                newGameUI.canCast = true;
            }
            else if (C.type == CommandType.SETUPDECK) { // 配置卡组
                // pass
            }
            else if (C.type == CommandType.ROUNDSTARTED) { // 回合开始
                curTurn += 1; // 回合数加一
                activeCamp = C.arg[0];

                // 结算驻扎点归属
                for (int locInx = 0; locInx < 4; locInx++) {
                    HexCell locCell = GetCell(new HexCoordinates(HexMetrics.stationCoords[locInx][0][0], HexMetrics.stationCoords[locInx][0][2]));
                    if (locCell.Unit) {
                        TransferMagicFront(locCell, locCell.Unit.Camp);
                    }
                }

                // 更新拥有的法力
                if (newGameUI._MaxMana[activeCamp] < 12) {
                    if (newGameUI._ManaIncr[activeCamp]) {
                        newGameUI._MaxMana[activeCamp] += 1;
                    }
                    newGameUI._ManaIncr[activeCamp] = !newGameUI._ManaIncr[activeCamp];
                }
                newGameUI._Mana[activeCamp] = newGameUI._MaxMana[activeCamp];
                if (activeCamp == newGameUI.myCamp) newGameUI.UpdateManaText();
                else newGameUI.UpdateOppManaText();

                // 更新生物冷却、_Remains
                newGameUI.DecreseCD(activeCamp, curTurn);
                if (activeCamp == newGameUI.myCamp)
                    for (int i = 0; i < 3; i++) {
                        newGameUI.UpdateRemainText(i);
                        newGameUI.UpdateCDText(i);
                    }

                // 更新神器冷却
                if (newGameUI._ACD[activeCamp] > 0) {
                    newGameUI._ACD[activeCamp] -= 1;
                    if (activeCamp == newGameUI.myCamp) newGameUI.UpdateAtfCDText();
                }

                if (activeCamp == newGameUI.myCamp) {
                    // 弹出提示
                    int turnNum = curTurn + 1;
                    StartCoroutine(newGameUI.popPanel("Y o u r  T u r n", "Round " + turnNum.ToString(), RunInterval, RunInterval, true));

                    if (newGameUI.gameMode == 1) {
                        // 更新生物是否可以行动
                        foreach (HexUnit u in units) {
                            if (u.Type == 0) continue;
                            if (u.Camp == activeCamp) {
                                u.canAct = true;
                            }
                        }
                    }
                }

                // 离线模式自动切换、在线模式更新可操作
                if (newGameUI.gameMode == 0) {
                    if (newGameUI._autoSwap && activeCamp != newGameUI.myCamp) {
                        newGameUI.Swap();
                    }
                }
                else if (newGameUI.gameMode == 1) {
                    if (newGameUI.myCamp == activeCamp) { // 立刻更新UI
                        isInOperateState = true;
                        newGameUI.CanOperate = isInOperateState;
                        newGameUI.updateUIControl(true, true);
                        ClearPath();
                        ClearHighlight();
                        newGameUI.selectedUnit = null;
                    }
                }

                Invoke("RunCommands", 3 * RunInterval); // 回调
                break;
            }
            else if (C.type == CommandType.ROUNDOVER) { // 回合结束
                if (activeCamp == newGameUI.myCamp) {
                    StartCoroutine(newGameUI.popPanel("E n d  T u r n", "", RunInterval, RunInterval, false));
                    if (newGameUI.gameMode == 1) { // 立刻更新面板
                        isInOperateState = false;
                        newGameUI.CanOperate = isInOperateState;
                        newGameUI.updateUIControl(true, true);
                    }
                }

                Invoke("RunCommands", 3 * RunInterval); // 回调
                break;
            }
            else if (C.type == CommandType.SUMMONSTART) { // 开始召唤
                // pass
            }
            else if (C.type == CommandType.SUMMON) { // 召唤
                int X = C.arg[2];
                int Z = -X - C.arg[3];
                HexUnit unit = AddUnit(C.arg[0], C.arg[1], C.arg[4], false, GetCell(new HexCoordinates(X, Z)), UnityEngine.Random.value * 360f);
                if (newGameUI.gameMode == 1) unit.canAct = false;

                if (C.arg[0] != 7 && C.arg[0] != 17) { // 不是地狱火
                    // 更新召唤面板的剩余生物数量
                    int unitPanelInx = newGameUI.getUnitPanelInxByType(activeCamp, C.arg[0]);
                    newGameUI._Remain[activeCamp][unitPanelInx] -= 1;
                    newGameUI._Mana[activeCamp] -= unit.Cost;

                    newGameUI.UpdateRemainText(unitPanelInx);
                    if (activeCamp == newGameUI.myCamp)
                        newGameUI.UpdateManaText();
                    else
                        newGameUI.UpdateOppManaText();

                    if (newGameUI.gameMode == 1) {
                        newGameUI.CanOperate = isInOperateState;
                        newGameUI.updateUIControl(false, false);
                    }

                    Invoke("RunCommands", RunInterval); // 回调
                    break;
                }
            }
            else if (C.type == CommandType.MOVE) { // 移动
                HexUnit unit = getUnitById(C.arg[0]);
                if (unit.isMoving || unit.isAttacking) {
                    unit.hasCmdInQueue = true;
                    curComInx -= 1;
                    break;
                }
                if (newGameUI.gameMode == 1) unit.canAct = false;
                unit.isMoving = true;

                // 先计算移动的路径
                if (newGameUI.gameMode == 0) computeDis(unit.Location, 0, unit.IsFlying, false);
                else if (newGameUI.gameMode == 1) {
                    if (unit.Camp != newGameUI.myCamp) computeDis(unit.Location, 0, unit.IsFlying, false);
                }

                // 确认目的位置
                int desX = C.arg[1];
                int desZ = -desX - C.arg[2];
                HexCell ToCell = GetCell(new HexCoordinates(desX, desZ));
                if (!ToCell.reachable) { // 一定要是可到达的
                    newGameUI.socketClient.popMsgBox("error: cell not reachable!", 1);
                    // Debug.LogError("error: cell not reachable" + curComInx);
                    unit.Travel(null);
                }
                else {
                    // 移动unit，清理路径
                    unit.Travel(GetPath(ToCell)); // 在unit的这个函数内 不再回调LoadCommands!
                }
                ClearPath();

                if (newGameUI.gameMode == 1) {
                    newGameUI.CanOperate = isInOperateState;
                    newGameUI.updateUIControl(false, false);
                    if (activeCamp != newGameUI.myCamp && unit == newGameUI.selectedUnit) {
                        ClearHighlight();
                        newGameUI.selectedUnit = null;
                    }
                }
                Invoke("RunCommands", RunInterval); // 回调
                break;
            }
            else if (C.type == CommandType.LEAVE || C.type == CommandType.ARRIVE || C.type == CommandType.ATK) {
                // pass
            }
            else if (C.type == CommandType.PREATK) { // 开始攻击
                HexUnit unit = getUnitById(C.arg[0]);
                if (unit.isMoving || unit.isAttacking) {
                    unit.hasCmdInQueue = true;
                    curComInx -= 1;
                    break;
                }
                if (newGameUI.gameMode == 1) unit.canAct = false;
                HexUnit targetUnit = getUnitById(C.arg[1]);
                unit.isAttacking = true;
                unit.Attack(targetUnit, false);

                if (newGameUI.gameMode == 1) {
                    newGameUI.CanOperate = isInOperateState;
                    newGameUI.updateUIControl(false, false);
                }
                break;
            }
            else if (C.type == CommandType.POSTATK) { // 攻击后
                HexUnit sourceUnit = getUnitById(C.arg[0]); // 原攻击方
                HexUnit targetUnit = getUnitById(C.arg[1]);
                bool hasPostAtk = false;
                int newComInx = curComInx;
                int count = 0;
                while (newComInx < commands.Count && count < 4) {
                    C = commands[newComInx];
                    if (C.type == CommandType.HURT && C.arg[1] == targetUnit.Id && C.arg[3] == 2) {
                        hasPostAtk = true;
                        newComInx += 1;
                    }
                    else {
                        newComInx += 1;
                        count += 1;
                    }
                }
                if (hasPostAtk) {
                    if (targetUnit.isMoving || targetUnit.isAttacking) {
                        targetUnit.hasCmdInQueue = true;
                        curComInx -= 1;
                        break;
                    }
                    targetUnit.isAttacking = true;
                    targetUnit.Attack(sourceUnit, true);
                    break;
                }
            }
            else if (C.type == CommandType.HURT) {
                HexUnit targetUnit = getUnitById(C.arg[0]);
                targetUnit.Hit();
                targetUnit.Health = Mathf.Max(0, targetUnit.Health - C.arg[2]);

                Vector3 damagePos = targetUnit.transform.position + Vector3.up * 7f - transform.forward * 5f;
                GameObject damageEff = Instantiate(DamageEffectPrefab, damagePos, Quaternion.Euler(0f, 0f, 0f));
                Destroy(damageEff, 3f);

                if (C.arg[3] == 1) {
                    float invokeInterval;
                    if (FindSplashAndChangeCommands(getUnitById(C.arg[1])) > 0) { // 1s 后引起溅射伤害
                        invokeInterval = 1f;
                    }
                    else invokeInterval = RunInterval;

                    Invoke("RunCommands", invokeInterval);
                    break;
                }
            }
            else if (C.type == CommandType.DIE) {
                // 神迹死亡的命令在GAMEOVER中！
                if (C.arg[0] == 0 || C.arg[0] == 1) {
                    newGameUI.socketClient.popMsgBox("HomeTower cannot DIE!", 1);
                    Debug.LogError("HomeTower cannot DIE!");
                }

                HexUnit unit = getUnitById(C.arg[0]);

                // 加入CD队列
                int curCamp = unit.Camp;
                if (unit.Type == 7 || unit.Type == 17) { // 死亡的是Inferno
                    newGameUI._ACD[unit.Camp] = newGameUI._CD[unit.Camp][0]; // 神器CD
                    if (unit.Camp == newGameUI.myCamp) {
                        newGameUI.UpdateAtfCDText();
                        newGameUI.canCast = true; // 地狱之火神器回收
                    }
                }
                else {
                    int unitPanelInx = newGameUI.getUnitPanelInxByType(unit.Camp, unit.Type);
                    if (unitPanelInx == -1) {
                        newGameUI.socketClient.popMsgBox("unit type doesn't exit in deck!: " + unit.Type, 1);
                        Debug.LogError("unit type doesn't exit in deck!: " + unit.Type);
                    }
                    newGameUI._UCDList[unit.Camp][unitPanelInx].Add(unit.CD);
                    if (unit.Camp == newGameUI.myCamp) newGameUI.UpdateCDText(unitPanelInx);
                    if (unit.ATFEquipTarget) {
                        newGameUI._ACD[unit.Camp] = newGameUI._CD[unit.Camp][0];
                        if (unit.Camp == newGameUI.myCamp) {
                            newGameUI.UpdateAtfCDText();
                            newGameUI.canCast = true; // 阳炎之盾神器回收
                        }
                    }
                }

                unit.TriggerDeath(); // 无回调
                if (C.arg[0] != 0 && C.arg[0] != 1) // 若非神迹生物则移除之
                    units.Remove(unit);
            }
            else if (C.type == CommandType.ATF) { // 使用神器
                int camp = C.arg[0];
                if (C.arg[1] == 1 || C.arg[1] == 11 || C.arg[1] == 4 || C.arg[1] == 14) { // 圣光之耀、 风神之佑
                    int X = C.arg[2];
                    int Z = -X - C.arg[3];
                    HexCell ATFCell = GetCell(new HexCoordinates(X, Z));
                    Vector3 pos = new Vector3(ATFCell.Position.x, ATFCell.ImHeight, ATFCell.Position.z);
                    if (C.arg[1] == 1 || C.arg[1] == 11) {
                        GameObject AtfEff = Instantiate(GloryEffectPrefab, pos, Quaternion.Euler(0f, 0f, 0f));
                        Destroy(AtfEff, 5f);
                    }
                    else {
                        GameObject AtfEff = Instantiate(WindEffectPrefab, pos, Quaternion.Euler(0f, 0f, 0f));
                        Destroy(AtfEff, 5f);

                        // 风神之佑效果：范围1内的所有友方生物可以重新行动
                        if (ATFCell.Unit) ATFCell.Unit.canAct = true;
                        if (ATFCell.FlyingUnit) ATFCell.FlyingUnit.canAct = true;
                        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                            HexCell neighbor = ATFCell.GetNeighbor(d);
                            if (neighbor == null) continue;
                            if (!neighbor.isInGame) continue;
                            if (neighbor.Unit && neighbor.Unit.Type != 0) {
                                neighbor.Unit.canAct = true;
                            }
                            if (neighbor.FlyingUnit) neighbor.FlyingUnit.canAct = true;
                        }
                    }

                    // 神器CD
                    newGameUI._ACD[activeCamp] = newGameUI._CD[activeCamp][0];
                    if (activeCamp == newGameUI.myCamp)
                        newGameUI.UpdateAtfCDText();
                }
                else if (C.arg[1] == 2 || C.arg[1] == 12) { // 阳炎之盾
                    HexUnit ATFUnit = getUnitById(C.arg[4]);
                    ATFUnit.ATFEquipTarget = true;
                    while (curComInx < commands.Count) {
                        C = commands[curComInx];
                        if (C.type == CommandType.BUFF && C.arg[1] == 4) { // 加血BUFF
                            msgDisplayer.display(commands[curComInx]);
                            ATFUnit.MaxHealth += 3;
                            ATFUnit.Health += 3;

                            // 强化特效
                            GameObject StrengthEff = Instantiate(StrengthEffectPrefab, ATFUnit.transform.position, Quaternion.Euler(0f, 0f, 0f));
                            ATFUnit.strengthEff = StrengthEff.transform;
                            Destroy(StrengthEff.gameObject, 10f);

                            curComInx += 1;
                        }
                        else if (C.type == CommandType.BUFF && C.arg[1] == 2) { // 圣盾BUFF
                            msgDisplayer.display(commands[curComInx]);
                            HexUnit unit = getUnitById(C.arg[0]);
                            unit.addBuffEff(C.arg[1]);

                            curComInx += 1;
                        }
                        else {
                            break;
                        }
                    }

                    // 神器CD
                    if (activeCamp == newGameUI.myCamp)
                        newGameUI.UpdateAtfCDText("casting");
                }
                else if (C.arg[1] == 3 || C.arg[1] == 13) { // 地狱之火
                    int X = C.arg[2];
                    int Z = -X - C.arg[3];
                    HexCell ATFCell = GetCell(new HexCoordinates(X, Z));
                    GameObject HellEff = Instantiate(HellFireEffectPrefab, ATFCell.Position, Quaternion.Euler(0f, 0f, 0f));
                    Destroy(HellEff, 5f);

                    // 神器CD
                    if (activeCamp == newGameUI.myCamp)
                        newGameUI.UpdateAtfCDText("casting");
                }
                else {
                    newGameUI.socketClient.popMsgBox("unknown atf type" + C.arg[1], 1);
                    Debug.LogError("unknown atf type" + C.arg[1]);
                }

                newGameUI._Mana[activeCamp] -= newGameUI._Cost[activeCamp][0];
                if (activeCamp == newGameUI.myCamp)
                    newGameUI.UpdateManaText();
                else newGameUI.UpdateOppManaText();

                if (newGameUI.gameMode == 1) {
                    newGameUI.CanOperate = isInOperateState;
                    newGameUI.updateUIControl(false, false);
                }
                Invoke("RunCommands", 2 * RunInterval);
                break;
            }
            else if (C.type == CommandType.BUFF) {
                HexUnit unit = getUnitById(C.arg[0]);
                if (C.arg[1] == 1) { // 牧师加攻
                    unit.Atk += 1;
                    unit.addBuffEff(C.arg[1]);
                }
                else if (C.arg[1] == 2) { // 圣盾
                    unit.addBuffEff(C.arg[1]);
                }
                else if (C.arg[1] == 3) { // 圣光之耀加攻
                    unit.Atk += 2;
                    unit.addBuffEff(C.arg[1]);
                }
                else if (C.arg[1] == 4) { // 阳炎之盾加血
                    // pass
                }
            }
            else if (C.type == CommandType.DEBUFF) { // BUFF击破
                HexUnit unit = getUnitById(C.arg[0]);
                if (unit) {
                    if (unit.removeBuffEff(C.arg[1])) {
                        if (C.arg[1] == 1) {
                            unit.Atk -= 1;
                        }
                        else if (C.arg[1] == 2) {
                            // pass
                        }
                        else if (C.arg[1] == 3) {
                            unit.Atk -= 2;
                        }
                    }
                    else {
                        Debug.LogError("fail to remove the buff!" + (curComInx - 1).ToString());
                        newGameUI.socketClient.popMsgBox("fail to remove the buff!" + (curComInx - 1).ToString(), 1);
                    }
                }
            }
            else if (C.type == CommandType.HEAL) {
                HexUnit sourceUnit = getUnitById(C.arg[1]);
                if (sourceUnit.Type == 4 || sourceUnit.Type == 14) {
                    sourceUnit.HealOthers();
                    sourceUnit.addHealCommands(C);
                }
                else {
                    HexUnit unit = getUnitById(C.arg[0]);
                    unit.Health = Mathf.Min(unit.MaxHealth, unit.Health + C.arg[2]);

                    // 治疗特效
                    if (unit.recoverEff) unit.recoverEff.gameObject.SetActive(false);
                    GameObject RecoverEff = Instantiate(RecoverEffectPrefab, unit.transform.position, Quaternion.Euler(0f, 0f, 0f));
                    unit.recoverEff = RecoverEff.transform;
                    Destroy(RecoverEff.gameObject, 10f);
                }
            }
            else if (C.type == CommandType.GAMEOVER) {
                Debug.Log("Player " + C.arg[0].ToString() + " Win!");

                StartCoroutine(newGameUI.popGameoverPanel(RunInterval, 3 * RunInterval));

                if (C.arg[0] == 0)
                    units[1].TriggerDeath();
                else if (C.arg[0] == 1)
                    units[0].TriggerDeath();

                if (newGameUI.gameMode == 0)
                    newGameUI.SetBtnOverState();
                else if (newGameUI.gameMode == 1) { // 更新面板
                    isInOperateState = false;
                    newGameUI.CanOperate = isInOperateState;
                    newGameUI.updateUIControl(true, true);
                }
                break; // 没有回调！
            }
            else if (C.type == CommandType.INVALID) {
                newGameUI.socketClient.popMsgBox("invalid actions!", 1);
                if (newGameUI.gameMode == 1) { // 更新面板
                    newGameUI.CanOperate = isInOperateState;
                    newGameUI.updateUIControl(true, true);
                }
            }
            else Debug.Log("wrong command" + C.type);

            if (newGameUI.gameMode == 1 && curComInx == commands.Count) { // 暂时把命令跑完
                isRunning = false;
            }
        }
    }

    // 虽然很不满逻辑那边给的事件顺序，还是自己动手丰衣足食
    int FindSplashAndChangeCommands(HexUnit sourceUnit)
    {
        int splash_num = 0;
        int newComInx = curComInx;
        int count = 0;
        int lastSplash_pos = -1;
        int postAtk_pos = -1;
        Command postAtk_C = null;
        bool needtochange = true;
        while (newComInx < commands.Count && count < 8) {
            Command C = commands[newComInx];
            if (C.type == CommandType.HURT && C.arg[1] == sourceUnit.Id && C.arg[3] == 3) {
                if (postAtk_pos == -1) {
                    needtochange = false;
                }
                splash_num += 1;
                lastSplash_pos = newComInx;
                newComInx += 1;
            }
            else if (C.type == CommandType.POSTATK && postAtk_pos == -1) {
                postAtk_pos = newComInx;
                postAtk_C = C;
                newComInx += 1;
                if (!needtochange) break;
            }
            else {
                newComInx += 1;
                count += 1;
            }
        }
        if (needtochange && splash_num > 0) {
            commands.Insert(lastSplash_pos + 1, postAtk_C);
            commands.RemoveAt(postAtk_pos);
        }
        return splash_num;
    }

    public void FastForwardCommand() // 快进
    {
        if (newGameUI.gameMode != 0) return;

        bool hasRun = false;
        while (curComInx < commands.Count) {
            Command C = commands[curComInx];

            if (C.type == CommandType.GAMESTARTED) { // 游戏开始
                newGameUI.canCast = true;
            }
            else if (C.type == CommandType.SETUPDECK) { // 配置卡组
                // pass
            }
            else if (C.type == CommandType.ROUNDSTARTED) { // 回合开始
                if (hasRun) break;
                hasRun = true;

                curTurn += 1; // 回合数加一
                activeCamp = C.arg[0];

                // 结算驻扎点归属
                for (int locInx = 0; locInx < 4; locInx++) {
                    HexCell locCell = GetCell(new HexCoordinates(HexMetrics.stationCoords[locInx][0][0], HexMetrics.stationCoords[locInx][0][2]));
                    if (locCell.Unit) {
                        TransferMagicFront(locCell, locCell.Unit.Camp);
                    }
                }

                // 更新拥有的法力
                if (newGameUI._MaxMana[activeCamp] < 12) {
                    if (newGameUI._ManaIncr[activeCamp]) {
                        newGameUI._MaxMana[activeCamp] += 1;
                    }
                    newGameUI._ManaIncr[activeCamp] = !newGameUI._ManaIncr[activeCamp];
                }
                newGameUI._Mana[activeCamp] = newGameUI._MaxMana[activeCamp];
                if (activeCamp == newGameUI.myCamp) newGameUI.UpdateManaText();
                else newGameUI.UpdateOppManaText();

                // 更新生物冷却、_Remains
                newGameUI.DecreseCD(activeCamp, curTurn);
                if (activeCamp == newGameUI.myCamp)
                    for (int i = 0; i < 3; i++) {
                        newGameUI.UpdateRemainText(i);
                        newGameUI.UpdateCDText(i);
                    }

                // 更新神器冷却
                if (newGameUI._ACD[activeCamp] > 0) {
                    newGameUI._ACD[activeCamp] -= 1;
                    if (activeCamp == newGameUI.myCamp) newGameUI.UpdateAtfCDText();
                }

                // 离线模式自动切换
                if (newGameUI._autoSwap && activeCamp != newGameUI.myCamp) {
                    newGameUI.Swap();
                }
            }
            else if (C.type == CommandType.ROUNDOVER) { // 回合结束
                // pass
            }
            else if (C.type == CommandType.SUMMONSTART) { // 开始召唤
                // pass
            }
            else if (C.type == CommandType.SUMMON) { // 召唤
                if (C.arg[0] != 7 && C.arg[0] != 17) {
                    if (hasRun) break;
                    hasRun = true;
                }
                int X = C.arg[2];
                int Z = -X - C.arg[3];
                HexUnit unit = AddUnit(C.arg[0], C.arg[1], C.arg[4], true, GetCell(new HexCoordinates(X, Z)), UnityEngine.Random.value * 360f);

                if (C.arg[0] != 7 && C.arg[0] != 17) { // 不是地狱火
                    // 更新召唤面板的剩余生物数量
                    int unitPanelInx = newGameUI.getUnitPanelInxByType(activeCamp, C.arg[0]);
                    newGameUI._Remain[activeCamp][unitPanelInx] -= 1;
                    newGameUI._Mana[activeCamp] -= unit.Cost;

                    newGameUI.UpdateRemainText(unitPanelInx);
                    if (activeCamp == newGameUI.myCamp)
                        newGameUI.UpdateManaText();
                    else
                        newGameUI.UpdateOppManaText();
                }
            }
            else if (C.type == CommandType.MOVE) { // 移动
                if (hasRun) break;
                hasRun = true;

                // 确认目的位置
                int desX = C.arg[1];
                int desZ = -desX - C.arg[2];
                HexCell ToCell = GetCell(new HexCoordinates(desX, desZ));

                HexUnit unit = getUnitById(C.arg[0]);
                unit.FastTravel(ToCell);
            }
            else if (C.type == CommandType.LEAVE || C.type == CommandType.ARRIVE || C.type == CommandType.ATK || C.type == CommandType.POSTATK) {
                // pass
            }
            else if (C.type == CommandType.PREATK) {
                if (hasRun) break;
                hasRun = true;
            }
            else if (C.type == CommandType.HURT) {
                HexUnit targetUnit = getUnitById(C.arg[0]);
                targetUnit.Health = Mathf.Max(0, targetUnit.Health - C.arg[2]);

                Vector3 damagePos = targetUnit.transform.position + Vector3.up * 7f - transform.forward * 5f;
                GameObject damageEff = Instantiate(DamageEffectPrefab, damagePos, Quaternion.Euler(0f, 0f, 0f));
                Destroy(damageEff, 3f);
            }
            else if (C.type == CommandType.DIE) {
                Debug.Log(string.Format("in RunCommands: curComInx = {0}, {1} [{2},{3},{4},{5},{6}]",
                curComInx, C.ToString(), C.arg[0], C.arg[1], C.arg[2], C.arg[3], C.arg[4]));
                msgDisplayer.display(C);

                // 神迹死亡的命令在GAMEOVER中！
                if (C.arg[0] == 0 || C.arg[0] == 1) {
                    newGameUI.socketClient.popMsgBox("HomeTower cannot DIE!", 1);
                    Debug.LogError("HomeTower cannot DIE!");
                }

                HexUnit unit = getUnitById(C.arg[0]);

                // 加入CD队列
                int curCamp = unit.Camp;
                if (unit.Type == 7 || unit.Type == 17) { // 死亡的是Inferno
                    newGameUI._ACD[unit.Camp] = newGameUI._CD[unit.Camp][0]; // 神器CD
                    if (unit.Camp == newGameUI.myCamp) {
                        newGameUI.UpdateAtfCDText();
                        newGameUI.canCast = true; // 地狱之火神器回收
                    }
                }
                else {
                    int unitPanelInx = newGameUI.getUnitPanelInxByType(unit.Camp, unit.Type);
                    if (unitPanelInx == -1) {
                        newGameUI.socketClient.popMsgBox("unit type doesn't exit in deck!: " + unit.Type, 1);
                        Debug.LogError("unit type doesn't exit in deck!: " + unit.Type);
                    }
                    newGameUI._UCDList[unit.Camp][unitPanelInx].Add(unit.CD);
                    if (unit.Camp == newGameUI.myCamp) newGameUI.UpdateCDText(unitPanelInx);
                    if (unit.ATFEquipTarget) {
                        newGameUI._ACD[unit.Camp] = newGameUI._CD[unit.Camp][0];
                        if (unit.Camp == newGameUI.myCamp) {
                            newGameUI.UpdateAtfCDText();
                            newGameUI.canCast = true; // 阳炎之盾神器回收
                        }
                    }
                }

                unit.TriggerDeath(); // 无回调
                if (C.arg[0] != 0 && C.arg[0] != 1) // 若非神迹生物则移除之
                    units.Remove(unit);
            }
            else if (C.type == CommandType.ATF) {
                if (hasRun) break;
                hasRun = true;

                int camp = C.arg[0];
                if (C.arg[1] == 1 || C.arg[1] == 11 || C.arg[1] == 4 || C.arg[1] == 14) { // 圣光之耀、 风神之佑
                    int X = C.arg[2];
                    int Z = -X - C.arg[3];
                    HexCell ATFCell = GetCell(new HexCoordinates(X, Z));
                    Vector3 pos = new Vector3(ATFCell.Position.x, ATFCell.ImHeight, ATFCell.Position.z);
                    if (C.arg[1] == 1 || C.arg[1] == 11) {
                        GameObject AtfEff = Instantiate(GloryEffectPrefab, pos, Quaternion.Euler(0f, 0f, 0f));
                        Destroy(AtfEff, 5f);
                    }
                    else {
                        GameObject AtfEff = Instantiate(WindEffectPrefab, pos, Quaternion.Euler(0f, 0f, 0f));
                        Destroy(AtfEff, 5f);

                        // 风神之佑效果：范围1内的所有友方生物可以重新行动
                        if (ATFCell.Unit) ATFCell.Unit.canAct = true;
                        if (ATFCell.FlyingUnit) ATFCell.FlyingUnit.canAct = true;
                        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                            HexCell neighbor = ATFCell.GetNeighbor(d);
                            if (neighbor == null) continue;
                            if (!neighbor.isInGame) continue;
                            if (neighbor.Unit && neighbor.Unit.Type != 0) {
                                neighbor.Unit.canAct = true;
                            }
                            if (neighbor.FlyingUnit) neighbor.FlyingUnit.canAct = true;
                        }
                    }

                    // 神器CD
                    newGameUI._ACD[activeCamp] = newGameUI._CD[activeCamp][0];
                    if (activeCamp == newGameUI.myCamp)
                        newGameUI.UpdateAtfCDText();
                }
                else if (C.arg[1] == 2 || C.arg[1] == 12) { // 阳炎之盾
                    HexUnit ATFUnit = getUnitById(C.arg[4]);
                    ATFUnit.ATFEquipTarget = true;
                    // 神器CD
                    if (activeCamp == newGameUI.myCamp)
                        newGameUI.UpdateAtfCDText("casting");
                }
                else if (C.arg[1] == 3 || C.arg[1] == 13) { // 地狱之火
                    int X = C.arg[2];
                    int Z = -X - C.arg[3];
                    HexCell ATFCell = GetCell(new HexCoordinates(X, Z));
                    GameObject HellEff = Instantiate(HellFireEffectPrefab, ATFCell.Position, Quaternion.Euler(0f, 0f, 0f));
                    Destroy(HellEff, 5f);

                    // 神器CD
                    if (activeCamp == newGameUI.myCamp)
                        newGameUI.UpdateAtfCDText("casting");
                }
                else {
                    newGameUI.socketClient.popMsgBox("unknown atf type" + C.arg[1], 1);
                    Debug.LogError("unknown atf type" + C.arg[1]);
                }

                newGameUI._Mana[activeCamp] -= newGameUI._Cost[activeCamp][0];
                if (activeCamp == newGameUI.myCamp)
                    newGameUI.UpdateManaText();
                else newGameUI.UpdateOppManaText();
            }
            else if (C.type == CommandType.BUFF) {
                HexUnit unit = getUnitById(C.arg[0]);
                if (C.arg[1] == 1) { // 牧师加攻
                    unit.Atk += 1;
                    unit.addBuffEff(C.arg[1]);
                }
                else if (C.arg[1] == 2) { // 圣盾
                    unit.addBuffEff(C.arg[1]);
                }
                else if (C.arg[1] == 3) { // 圣光之耀加攻
                    unit.Atk += 2;
                    unit.addBuffEff(C.arg[1]);
                }
                else if (C.arg[1] == 4) { // 阳炎之盾加血
                    unit.MaxHealth += 3;
                    unit.Health += 3;

                    // 强化特效
                    GameObject StrengthEff = Instantiate(StrengthEffectPrefab, unit.transform.position, Quaternion.Euler(0f, 0f, 0f));
                    unit.strengthEff = StrengthEff.transform;
                    Destroy(StrengthEff.gameObject, 10f);
                }
            }
            else if (C.type == CommandType.DEBUFF) { // BUFF击破
                HexUnit unit = getUnitById(C.arg[0]);
                if (unit) {
                    if (unit.removeBuffEff(C.arg[1])) {
                        if (C.arg[1] == 1) {
                            unit.Atk -= 1;
                        }
                        else if (C.arg[1] == 2) {
                            // pass
                        }
                        else if (C.arg[1] == 3) {
                            unit.Atk -= 2;
                        }
                    }
                    else {
                        Debug.LogError("fail to remove the buff!" + (curComInx - 1).ToString());
                        newGameUI.socketClient.popMsgBox("fail to remove the buff!" + (curComInx - 1).ToString(), 1);
                    }
                }
            }
            else if (C.type == CommandType.HEAL) {
                HexUnit unit = getUnitById(C.arg[0]);
                unit.Health = Mathf.Min(unit.MaxHealth, unit.Health + C.arg[2]);

                // 治疗特效
                if (unit.recoverEff) unit.recoverEff.gameObject.SetActive(false);
                GameObject RecoverEff = Instantiate(RecoverEffectPrefab, unit.transform.position, Quaternion.Euler(0f, 0f, 0f));
                unit.recoverEff = RecoverEff.transform;
                Destroy(RecoverEff.gameObject, 10f);
            }
            else if (C.type == CommandType.GAMEOVER) {
                Debug.Log("Player " + C.arg[0].ToString() + " Win!");

                if (C.arg[0] == 0)
                    units[1].TriggerDeath();
                else if (C.arg[0] == 1)
                    units[0].TriggerDeath();

                newGameUI.SetBtnOverState();
                break;
            }
            else Debug.Log("wrong command" + C.type);

            if (C.type != CommandType.DIE) {
                Debug.Log(string.Format("in RunCommands: curComInx = {0}, {1} [{2},{3},{4},{5},{6}]",
                curComInx, C.ToString(), C.arg[0], C.arg[1], C.arg[2], C.arg[3], C.arg[4]));
                msgDisplayer.display(C);
            }
            curComInx += 1;
        }

    }

    public void RestartCommands()
    {
        LoadMap(); // 重新加载一次！
        curComInx = 0;
        curTurn = -1;

        // 命令清空
        msgDisplayer.Clear();

        // 神迹血量归满
        if (units[0].hpBar == null) {
            Slider hpbar = Instantiate<Slider>(hpBarPrefab[6]);
            units[0].hpBar = hpbar.transform;
            hpbar.transform.SetParent(hpCanvas.transform, false);
        }
        if (units[1].hpBar == null) {
            Slider hpbar = Instantiate<Slider>(hpBarPrefab[7]);
            units[1].hpBar = hpbar.transform;
            hpbar.transform.SetParent(hpCanvas.transform, false);
        }
        units[0].hpBar.gameObject.SetActive(true);
        units[1].hpBar.gameObject.SetActive(true);
        units[0].Health = units[0].MaxHealth;
        units[1].Health = units[1].MaxHealth;

        // 神迹动画切换
        Animation animation = units[0].Location.specialFeature.gameObject.GetComponent<Animation>();
        animation.clip = animation.GetClip("free");
        animation.Play();
        
        animation = units[1].Location.specialFeature.gameObject.GetComponent<Animation>();
        animation.clip = animation.GetClip("free");
        animation.Play();
    }

    public int getUnitsCount()
    {
        return units.Count;
    }
    public HexUnit getUnitInList(int inx) // 有危险性
    {
        return units[inx];
    }
    public HexUnit getUnitById(int unitId) // 神迹的虚拟id为0和1
    {
        foreach (HexUnit u in units) {
            if (unitId == u.Id) {
                return u;
            }
        }
        return null;
    }

    // 添加种类为type，所在六边形cell，朝向是orintation的生物
    public HexUnit AddUnit(int type, int level, int id, bool isFastForward, HexCell location, float orientation) // 注意生物的type是从1开始的，神迹的虚拟type为0，虚拟id为0和1
    {
        if (type < 0 || type >= unitPrefabs1.Length) return null;
        HexUnit unit = null;
        if (level == 1 || level == 2) unit = Instantiate(unitPrefabs1[type]);
        else if (level == 3) unit = Instantiate(unitPrefabs3[type]);
        else Debug.LogError("summon level error" + level);
        unit.transform.SetParent(transform, false);
        unit.grid = this;

        // 血条实例化
        Slider hpBar = null;
        int hpInx = 0, camp = 0;
        if (type == 0) {
            if (location.coordinates.X == -7) {
                camp = 0;
            }
            else if (location.coordinates.X == 7) {
                camp = 1;
            }
            hpInx = 6 + camp;
        }
        else {
            camp = (type - 1) / 10;
            hpInx = 2 * level - 2 + camp;
        }
        hpBar = Instantiate<Slider>(hpBarPrefab[hpInx], Camera.main.WorldToScreenPoint(unit.transform.localPosition + Vector3.up * 15f), Quaternion.Euler(0f, 0f, 0f), hpCanvas.transform);
        hpBar.enabled = false;
        unit.hpBar = hpBar.transform;
        unit.hpText = hpBar.transform.Find("Image/Text").GetComponent<Text>();

        // 详细信息面板实例化
        if (type != 0) {
            unit.infoPanel = Instantiate(newGameUI.InfoPanelWithDetailPrefab, Camera.main.WorldToScreenPoint(unit.transform.localPosition), Quaternion.Euler(0f, 0f, 0f));
            unit.infoPanel.SetParent(newGameUI.transform, false);
            unit.infoText = new Text[9];
            for (int i = 0; i < 9; i++) {
                unit.infoText[i] = unit.infoPanel.GetChild(i).GetComponent<Text>();
            }
            if (camp == 0) {
                unit.infoText[0].color = new Color(0.1f, 0.1f, 1f);
                unit.infoText[8].color = new Color(0.1f, 0.1f, 1f);
            }
            else if (camp == 1) {
                unit.infoText[0].color = new Color(1f, 0.1f, 0.1f);
                unit.infoText[8].color = new Color(1f, 0.1f, 0.1f);
            }
            for (int i = 1; i <= 7; i++) unit.infoText[i].color = HexMetrics.unitInfoColor[camp];
            unit.infoPanel.GetComponent<Image>().color = HexMetrics.unitPanelColor[camp];
        }

        // 属性初始化
        unit.Camp = camp;
        unit.Type = type;
        unit.Level = level;
        unit.Id = id;
        unit.Initialize(isFastForward);
        unit.Location = location; // 在初始化IsFlying之后初始化Location
        unit.Orientation = orientation;

        units.Add(unit); // 加进生物队列

        // 特效
        Vector3 position = location.Position;
        if (type == 7 || type == 17) {
            GameObject SummonEff = Instantiate(SummonEffectPrefab[2], position, Quaternion.Euler(0f, 0f, 0f));
            Destroy(SummonEff, 5f);
        }
        else if (type == 0) {
            GameObject SummonEff = Instantiate(SummonEffectPrefab[camp], position, Quaternion.Euler(0f, 0f, 0f));
            Destroy(SummonEff, 5f);
        }
        else {
            GameObject MagicEff = Instantiate(MagicFrontEffectPrefab[camp], position, Quaternion.Euler(0f, 0f, 0f));
            Destroy(MagicEff, 4f);

            GameObject SummonEff = Instantiate(SummonEffectPrefab[camp], position, Quaternion.Euler(0f, 0f, 0f));
            Destroy(SummonEff, 5f);
        }
        return unit;
    }
    public void RemoveUnit(HexUnit unit)
    {
        units.Remove(unit);
    }
    public void ClearUnits()
    {
        while (units.Count > 2) { // 一定要注意，0和1是神迹，不能删除！
            HexUnit u = units[2];
            if (u.IsFlying) u.Location.FlyingUnit = null;
            else u.Location.Unit = null;

            // 清除Unit的相关组件
            u.ClearComponent();
            u.ClearBuffList();

            units.RemoveAt(2);
            Destroy(u.gameObject);
        }
    }

    /*public void AttackUnit(int unitIndex)
    {
        if (unitIndex > units.Count) { return; }
        units[unitIndex - 1].Attack();
    }*/
    public void HitUnit(int unitIndex)
    {
        if (unitIndex > units.Count) { return; }
        units[unitIndex - 1].Hit();
    }
    public void DeathUnit(int unitIndex)
    {
        if (unitIndex <= units.Count) {
            units[unitIndex - 1].TriggerDeath();
            units.Remove(units[unitIndex - 1]); // 剔除出生物队列
        }
    }
    public void HealUnit(int unitIndex)
    {

    }

    // locCell要确保是驻扎点，被 index 方占领了。index = 0 代表蓝，index = 1 代表红
    public void TransferMagicFront(HexCell locCell, int index)
    {
        int oldIndex = locCell.SpecialIndex - 6; // 原归属，0 代表蓝，1 代表红，2 代表中立
        if (oldIndex == index) return;

        int locInx = 0;
        bool find = false;
        for (; locInx < 4; locInx++) {
            if (locCell.coordinates.X == HexMetrics.stationCoords[locInx][0][0] &&
                locCell.coordinates.Y == HexMetrics.stationCoords[locInx][0][1]) {
                find = true;
                break;
            }
        }
        if (!find) Debug.LogError("wrong to transfer magic front");

        HexCell magicCell_1 = GetCell(new HexCoordinates(HexMetrics.stationCoords[locInx][1][0], HexMetrics.stationCoords[locInx][1][2]));
        HexCell magicCell_2 = GetCell(new HexCoordinates(HexMetrics.stationCoords[locInx][2][0], HexMetrics.stationCoords[locInx][2][2]));
        HexCell magicCell_3 = GetCell(new HexCoordinates(HexMetrics.stationCoords[locInx][3][0], HexMetrics.stationCoords[locInx][3][2]));

        magicCell_1.specialIndex = index + 3;
        magicCell_2.specialIndex = index + 3;
        magicCell_3.specialIndex = index + 3;
        locCell.specialIndex = index + 6;
        locCell.Refresh();

        GameObject MagEff_1, MagEff_2, MagEff_3;
        MagEff_1 = Instantiate(MagicFrontEffectPrefab[oldIndex], magicCell_1.Position, Quaternion.Euler(0f, 0f, 0f));
        MagEff_2 = Instantiate(MagicFrontEffectPrefab[oldIndex], magicCell_2.Position, Quaternion.Euler(0f, 0f, 0f));
        MagEff_3 = Instantiate(MagicFrontEffectPrefab[oldIndex], magicCell_3.Position, Quaternion.Euler(0f, 0f, 0f));

        Destroy(MagEff_1, 3f);
        Destroy(MagEff_2, 3f);
        Destroy(MagEff_3, 3f);
    }

    // 保存地图种子
    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);
        for (int i = 0; i < cells.Length; i++) {
            cells[i].Save(writer);
        }

        writer.Write(units.Count);
        for (int i = 0; i < units.Count; i++) {
            units[i].Save(writer);
        }
    }
    public void Load(BinaryReader reader, int header)
    {
        ClearHighlight();
        ClearPath();
        ClearUnits();
        StopAllCoroutines();
        int x = 20, z = 15;
        if (header >= 1) {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        if (x != cellCountX || z != cellCountZ) { // Load进来的地图大小不一样才Create一个新的
            if (!CreateMap(x, z)) return;
        }

        for (int i = 0; i < cells.Length; i++) {
            cells[i].Load(reader);
        }
        for (int i = 0; i < chunks.Length; i++) {
            chunks[i].Refresh();
        }

        if (header >= 2) {
            int unitCount = reader.ReadInt32();
            for (int i = 0; i < unitCount; i++) {
                HexUnit.Load(reader, this); // 本来就有生物的话，那么id默认为2
            }
        }
    }

    public static string GetSelectedPath(string mapName)
    {
        string path = Path.Combine(Application.dataPath, mapName + ".map");
        Debug.Log(path);
        return path;
    }

    // 智能体：启动时即构建地图！
    public void LoadMap()
    {
        string path = Path.Combine(Application.streamingAssetsPath, mapname + ".map");
        if (!File.Exists(path)) {
            Debug.LogError("File does not exist " + path);
            return;
        }
        using (
            BinaryReader reader =
                new BinaryReader(File.OpenRead(path))
        ) {
            int header = reader.ReadInt32();
            if (header <= 2) {
                Load(reader, header);
            }
            else Debug.LogWarning("Unknown map format" + header);
        }
    }
}
