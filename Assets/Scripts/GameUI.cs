using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using LitJson;
// using DG.Tweening;

public enum CommandType
{
    INVALID = -1,
    GAMESTARTED = 0,
    ROUNDSTARTED = 1,
    ROUNDOVER = 2,
    SUMMON = 3,
    MOVE = 4,
    PREATK = 5,
    HURT = 6,
    DIE = 7,
    HEAL = 8,
    ATF = 9,
    GAMEOVER = 10,
    SETUPDECK = 11,
    BUFF = 12,
    DEBUFF = 13,
    ATK = 14,
    POSTATK = 15,
    LEAVE = 16,
    ARRIVE = 17,
    SUMMONSTART = 18
}

public class Command
{
    public int round;
    public CommandType type;
    public int[] arg = new int[GameUI.argNum];
    public Command(int round, CommandType type, int[] arg)
    {
        this.round = round;
        this.type = type;
        for (int i = 0; i < GameUI.argNum; i++) {
            this.arg[i] = arg[i];
        }
    }
    public override string ToString()
    {
        return "Round " + round.ToString() + ", " + type;
    }
}

class BinaryReader2 : BinaryReader // 支持读取大尾端
{
    public BinaryReader2(Stream stream) : base(stream) { }

    public int ReadInt32(bool little)
    {
        if (little) { return base.ReadInt32(); }
        else {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }
    }

    public short ReadInt16(bool little)
    {
        if (little) { return base.ReadInt16(); }
        else {
            var data = base.ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }
    }
}

/// <summary>
/// HexGameUI 脚本负责游戏模式下的游戏控制，比如生成生物，让某个生物攻击等。
/// 运行后，需要取消上方的 EditMode 选框 进入游戏模式之后 才能运作。
/// 所有 HexGameUI 设置的控制键都是临时的。
/// GameUI 脚本 用来代替临时性质的HexGameUI 脚本。
/// 
/// 使用方法：
/// 一、在Hierarchy面板上，找到UI -> Game UI 游戏Object，它就是 挂有 旧HexGameUI脚本 的Object。
/// 二、将它删去，在UI物体下创建一个新的空物体(Empty Object)，命名为New Game UI，
/// 三、将本脚本(GameUI.cs)拖到这个New Game UI物体上，
/// 四、将Hierarchy面板上的 Hex Grid 物体拖入 Inspector 面板上的 GameUI(Script) 的变量 Grid。
/// 五、将 New Game UI 的Object 拖到 Hierarchy面板中 UI -> Hex Map Editor -> Panel Top -> Toggle Mode 的Inspector面板中的"On Value Changed"一栏第二个函数槽
/// 这样New Game UI 就成为新的 进行游戏操作的 脚本类物体了。
/// 
/// 在这里暂时只定义必要的控制接口，但没有定义交互接口
/// 
/// 需解释：
/// 坐标系统 HexCoordinates
/// 
/// </summary>
public class GameUI : MonoBehaviour
{
    public HexGrid grid; // 必须要挂上场景里的Hex Grid
    public Material terrainMaterial;
    public Material PopPanelMaterial;
    public Transform InfoPanelPrefab;
    public Transform InfoPanelWithDetailPrefab;
    public Transform InfoPanelATFPrefab;
    public Transform InfoPanelATFWithDetailPrefab;

    // 加载资源，蓝 -> 红 顺序
    public Sprite[] LevelSprite; // 一级蓝 -> 一级红 -> 二级蓝 -> ... -> 三级红，共六个
    public Sprite[] ACSprite; // 0 是空，1-10是蓝，11-20是红，21之后是神器
    public Sprite[] CampSprite; // 0 是蓝，1 是红
    public Sprite[] CampHpSource; // 0 是蓝，1 是红
    public int CreaturesNum, ATFNum; // 生物种类、神器种类，不分阵营，现分别是5、3

    // 按钮组件，右 -> 左 顺序
    public Button[] BtnSummon; // 右第一个UnitPanel 三个 -> 右第二个UnitPanel 三个 -> 右第三个UnitPanel 三个 共九个
    public Button BtnAtf; // 神器按钮
    public Button BtnEndTurn; // 结束回合按钮

    // 显示组件
    public RectTransform[] BtnSummonInfoPanel; // 蓝10个红10个
    public Text[] ACName; // 右神器 -> 右三个生物 -> 左神器 -> 左三个生物 共8个
    public Image[] TypePanel; // 右 三个 -> 左 三个，共六个
    public Image[] ACImage; // 右神器 -> 右三个生物 -> 左神器 -> 左三个生物 共8个
    public Text[] ACMana; // 右神器 -> 右生物1的三个 -> 右生物2的三个 -> 右生物3的三个 -> 左神器 共11个
    public Image[] ACCamp; // 我方图标 * 6，敌方图标 * 6

    public Text ManaText; // 我方法力显示
    public Text OppManaText; // 敌方法力显示
    public Text atfCDText; // 神器冷却时间显示
    public Text[] RemainText; // 三个生物剩余槽数
    public int CDUnits_num;
    public GameObject[] CDUnits01; // Unit01 的 CD 面板队列
    public GameObject[] CDUnits02;
    public GameObject[] CDUnits03;
    public Text[] CDText01; // Unit01 的 CD 面板数值队列
    public Text[] CDText02;
    public Text[] CDText03;
    public Text[] MirHpText; // 神迹血量显示
    public Slider[] MirHpBar;
    public Text[] ScoreText; // 计分显示

    // 内置变量
    public int[][] _Deck = new int[][] { new int[4], new int[4] };
    public int[][] _Cost = new int[][] { new int[10], new int[10]};
    public int[][] _Atk = new int[][] { new int[9], new int[9] };
    public int[][] _MaxHp = new int[][] { new int[9], new int[9] };
    public int[][] _MinAtkRan = new int[][] { new int[9], new int[9] };
    public int[][] _MaxAtkRan = new int[][] { new int[9], new int[9] };
    public int[][] _MovingRan = new int[][] { new int[9], new int[9] };
    public int[][] _CD = new int[][] { new int[10], new int[10] };

    public int[] _MaxMana = new int[2];
    public int[] _Mana = new int[2];
    public bool[] _ManaIncr = new bool[2];
    public int[] _ACD = new int[2];
    public int[][] _Remain = new int[][] { new int[3], new int[3] };
    public int[][] _MaxRemain = new int[][] { new int[3], new int[3] };
    public List<int>[][] _UCDList = new List<int>[][] { 
        new List<int>[3] { new List<int>(), new List<int>(), new List<int>() }, 
        new List<int>[3] { new List<int>(), new List<int>(), new List<int>() }
    };

    public int[] _MirHp = new int[2];
    public int[] _Score = new int[2];

    // 弹出面板组件
    public RectTransform PopPanelDown;
    public Text popMainTextDown;
    public Text popSubTextDown;

    public RectTransform PopPanelUp;
    public Text popTextUp;

    public RectTransform TimePanel;
    public Text TimeText;

    public GameObject InstructionPanel;

    /// <summary>
    /// 全局变量
    /// </summary>
    public int gameMode; // 0 为离线模式，1 为在线模式，由开始界面传过来
    public int mapIndex; // 决定地图文件、地图纹理数组。目前只有0、1
    public int skyboxIndex; // 决定天空盒。白天0，晚上1
    public int myCamp; // 决定阵营。蓝方0、红方1

    // 离线模式
    string recordPath;
    public GameObject OFFLINEPanel;
    public Button BtnPlay, BtnPause, BtnStop, BtnSwap;
    public GameObject SUBOFFLINEPanel;
    public Button BtnFastForward;
    public Toggle TogAutoSwap;
    public GameObject BackBtnPanel;
    public GameObject ConcedeBtnPanel;
    public GameObject ConcedePanel;
    public Button BtnConcede;

    // 在线模式
    public string onlineToken;
    public SetUpMenu setupMenu;
    public static Color[] highlightColors = {
        new Color(1f, 1f, 1f, 0.35f), // 白 0
        new Color(0f, 1f, 0f, 0.2f), // 绿 1
        new Color(0f, 0f, 1f, 0.4f), // 蓝 2
        new Color(1f, 0f, 0f, 0.4f), // 红 3
        new Color(0.9f, 0.9f, 0f, 0.4f), // 黄 4
        new Color(0.7f, 0.1f, 0.9f, 0.4f), // 紫 5
        new Color(1f, 0f, 0f, 0.2f) // 浅红 6
    };
    public GameObject FOG;
    int FOGState; // 1：正在选中Unit；2：正在选择攻击对象；3：正在选择阳炎之盾对象
    public SocketClient socketClient;
    public int deck_count = 0;

    public GameObject MsgCanvasMask;
    public GameObject GameoverPanel;

    public GameObject ErrorBarPrefab;

    public GameObject PanelSelect;
    public Text selectedUnitText;


    private void Awake()
    {
        terrainMaterial.EnableKeyword("GRID_ON");

        gameMode = PlayerPrefs.GetString("bool") == "0" ? 0 : 1;
        if (gameMode == 0) {
            recordPath = PlayerPrefs.GetString("path");
            MsgCanvasMask.SetActive(true);
            LoadCommands();
        }
        else if (gameMode == 1) {
            onlineToken = PlayerPrefs.GetString("token");
            HexMapCamera.Locked = true;
            enabled = false;
            grid.enabled = false;
            gameObject.SetActive(false);
            MsgCanvasMask.SetActive(true);
        }
        setupMenu.gameObject.SetActive(false);
    }

    HexCell previousHoverCell;
    HexCell currentHoverCell;
    HexCell currentCell;
    public HexUnit _selectedunit = null;
    public HexUnit selectedUnit {
        get { return _selectedunit; }
        set {
            _selectedunit = value;
            if (_selectedunit)
                selectedUnitText.text = _selectedunit.ToString();
            else
                selectedUnitText.text = "--";
        }
    }
    int selectedUnitState = 0; // 1 代表移动，2 代表攻击
    bool infoPanelAva = true;
    void SetInfoPanel(bool option)
    {
        infoPanelAva = option;
        if (!option) {
            if (currentHoverCell) {
                if (currentHoverCell.IsSpecial)
                    if (currentHoverCell.Unit && currentHoverCell.Unit.Type != 0 && currentHoverCell.Unit.infoPanelActive)
                        currentHoverCell.Unit.infoPanel.gameObject.SetActive(false);
                if (currentHoverCell.FlyingUnit && currentHoverCell.FlyingUnit.infoPanelActive)
                    currentHoverCell.FlyingUnit.infoPanel.gameObject.SetActive(false);
            }
            if (previousHoverCell) {
                if (previousHoverCell.Unit && previousHoverCell.Unit.Type != 0) previousHoverCell.Unit.infoPanel.gameObject.SetActive(false);
                if (previousHoverCell.FlyingUnit) previousHoverCell.FlyingUnit.infoPanel.gameObject.SetActive(false);
            }
        }
    }
    private void Update()
    {
        if (FOG.activeSelf) FOG.transform.position = FOG.transform.position = Camera.main.WorldToScreenPoint(currentCell.Position);
        if (!EventSystem.current.IsPointerOverGameObject()) { // 点的不是UI
            if (Input.GetMouseButtonDown(0)) { // 按下左键
                currentCell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
                if (!currentCell) return;
                if (!canOperate) {
                    if (gameMode == 1 && myCamp != grid.activeCamp) {
                        bool hasGU = currentCell.Unit != null;
                        bool hasFU = currentCell.FlyingUnit != null;
                        if (selectedUnit) {
                            if (currentCell.attackable_G || currentCell.attackable_F) {
                                StartCoroutine(popErrorBar("it is not your turn!"));
                            }
                            else {
                                grid.ClearPath();
                                grid.ClearHighlight();
                                selectedUnit = null;
                                if (FOG.activeSelf) FOG.SetActive(false);
                                SetInfoPanel(true);
                            }
                        }
                        else if (hasGU || hasFU) {
                            showRanInUpdate(true, hasGU, hasFU);
                        }
                    }
                    return;
                }
                bool hasMyGU = currentCell.Unit && currentCell.Unit.Camp == myCamp;
                bool hasMyFU = currentCell.FlyingUnit && currentCell.FlyingUnit.Camp == myCamp;
                if (atfShowing) { // 正在选择投放地点
                    if (currentCell.IsHighlighted) {
                        StartATF();
                    }
                    else { // 取消操作
                        grid.ClearHighlight();
                        atfShowing = false;
                        if (FOG.activeSelf) FOG.SetActive(false);
                        SetInfoPanel(true);
                    }
                }
                else if (selectedUnit) { // 正在Unit操作状态中
                    if (selectedUnitState == 1 && currentCell.reachable) {
                        StartMove();
                    }
                    else if (selectedUnitState == 2 && (currentCell.attackable_G || currentCell.attackable_F)) {
                        StartAttack();
                    }
                    else if (selectedUnitState == 3 && (currentCell.attackable_G || currentCell.attackable_F)) {
                        StartCoroutine(popErrorBar("This creature cannot attack now!"));
                    }
                    else {
                        grid.ClearPath();
                        grid.ClearHighlight();
                        selectedUnit = null;
                        if (FOG.activeSelf) FOG.SetActive(false);
                        SetInfoPanel(true);
                    }
                }
                else if (summonShowing) { // 正在选择召唤地点
                    if (currentCell.IsHighlighted) {
                        StartSummon();
                    }
                    else { // 取消
                        enableSummonCell(false);
                        SetInfoPanel(true);
                    }
                }
                else if (currentCell.Unit || currentCell.FlyingUnit) { // 开始选中Unit
                    showRanInUpdate(true, currentCell.Unit, currentCell.FlyingUnit);
                    summonShowing = false; // 如果是正在召唤，则取消召唤
                    atfShowing = false; // 如果是正在使用神器，则取消
                }
                else {
                    if (FOG.activeSelf) FOG.SetActive(false);
                    SetInfoPanel(true);
                }
            }
            else if (Input.GetMouseButtonDown(1)) { // 按下右键
                currentCell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
                if (!currentCell) return;
                if (!canOperate) {
                    if (gameMode == 1 && myCamp != grid.activeCamp) {
                        if (currentCell.Unit || currentCell.FlyingUnit) {
                            StartCoroutine(popErrorBar("It is not your turn!"));
                        }
                    }
                    return;
                }
                bool hasMyGU = currentCell.Unit && currentCell.Unit.Camp == myCamp;
                bool hasMyFU = currentCell.FlyingUnit && currentCell.FlyingUnit.Camp == myCamp;
                if (atfShowing) { // 取消操作
                    grid.ClearHighlight();
                    atfShowing = false;
                    if (FOG.activeSelf) FOG.SetActive(false);
                    SetInfoPanel(true);
                }
                else if (selectedUnit) { // 取消操作
                    grid.ClearPath();
                    grid.ClearHighlight();
                    selectedUnit = null;
                    if (FOG.activeSelf) FOG.SetActive(false);
                    SetInfoPanel(true);
                }
                else if (summonShowing) { // 取消操作
                    enableSummonCell(false);
                    SetInfoPanel(true);
                }
                else if (hasMyGU || hasMyFU) { // 开始选中Unit
                    showRanInUpdate(false, hasMyGU, hasMyFU);
                    summonShowing = false; // 如果是正在召唤，则取消召唤
                    atfShowing = false; // 如果是正在使用神器，则取消
                }
                else {
                    if (FOG.activeSelf) FOG.SetActive(false);
                    SetInfoPanel(true);
                }
            }
            else { // 没有按下键
                changeHover(true);
            }
        }
        else {
            changeHover(false);
        }
    }

    public void showRanInUpdate(bool isButton0, bool hasGU, bool hasFU)
    {
        if (hasGU || hasFU) {
            if (hasGU && hasFU) { // 选中飞行还是陆地？
                if (selectedUnit && (selectedUnit == currentCell.Unit || selectedUnit == currentCell.FlyingUnit)) { // 撤选
                    if (FOG.activeSelf) FOG.SetActive(false);
                    SetInfoPanel(true);
                }
                else {
                    FOG.SetActive(true);
                    FOG.transform.position = Camera.main.WorldToScreenPoint(currentCell.Position);
                    if (grid.activeCamp != myCamp) FOGState = 5; // 显示攻击范围状态
                    else if (canOperate) {
                        if (isButton0) FOGState = 2; // 攻击状态。注意，两个选项中是有可能包含5的状态的，但这里记为2
                        else FOGState = 1;
                    }
                    SetInfoPanel(false);
                }
                grid.ClearHighlight();
                grid.ClearPath();
                selectedUnit = null;
            }
            else { // 选中Unit
                HexUnit preU = null;
                if (hasGU) {
                    preU = currentCell.Unit;
                }
                else if (hasFU) {
                    preU = currentCell.FlyingUnit;
                }
                else Debug.LogError("update wrong!");

                grid.ClearHighlight();
                if (FOG.activeSelf) FOG.SetActive(false);
                if (selectedUnit && preU == selectedUnit) { // 撤选
                    selectedUnit = null;
                    grid.ClearPath();
                    SetInfoPanel(true);
                }
                else {
                    selectedUnit = preU;
                    if (grid.activeCamp != myCamp) {
                        selectedUnitState = 3;
                        grid.computeDis(currentCell, 3, selectedUnit.IsFlying, true);
                    }
                    else if (canOperate) {
                        if (selectedUnit.Camp == myCamp && selectedUnit.canAct) {
                            if (isButton0) {
                                if (selectedUnit.Atk > 0) { // 有攻击的生物可以攻击
                                    selectedUnitState = 2;
                                    grid.computeDis(currentCell, 1, selectedUnit.IsFlying, true); // 计算攻击，自带grid.ClearPath()
                                    if (currentCell.attackable_G || currentCell.attackable_F) currentCell.ChangeHighlight(highlightColors[2]);
                                }
                                else { // 没有攻击力的生物不能攻击
                                    selectedUnitState = 3;
                                    grid.computeDis(currentCell, 3, selectedUnit.IsFlying, true);
                                }
                            }
                            else {
                                selectedUnitState = 1;
                                grid.computeDis(currentCell, 0, selectedUnit.IsFlying, true); // 计算移动，自带ClearPath()
                            }
                            
                            SetInfoPanel(false);
                        }
                        else {
                            if (isButton0) {
                                selectedUnitState = 3;
                                grid.computeDis(currentCell, 3, selectedUnit.IsFlying, true);
                            }
                            else {
                                selectedUnit = null;
                                StartCoroutine(popErrorBar("This creature cannot move now!"));
                            }
                        }
                    }
                }
            }
            summonShowing = false; // 如果是正在召唤，则取消召唤
            atfShowing = false; // 如果是正在使用神器，则取消
        }
        else {
            Debug.LogError("error: showRanInUpdate wrong!");
        }
    }

    public void changeHover(bool isScene)
    {
        if (isScene)
            currentHoverCell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        else currentHoverCell = null;
        if (currentHoverCell == previousHoverCell) return;

        // 操作时移动鼠标
        if (selectedUnit) { // 有选中的Unit
            if (currentHoverCell) {
                if (selectedUnitState == 1 && currentHoverCell.reachable) {
                    currentHoverCell.ChangeHighlight(highlightColors[2]);
                }
                else if (selectedUnitState == 2 && (currentHoverCell.attackable_G || currentHoverCell.attackable_F)) {
                    currentHoverCell.ChangeHighlight(highlightColors[2]);
                }
            }
            if (previousHoverCell) {
                if (selectedUnitState == 1 && previousHoverCell.reachable) {
                    previousHoverCell.ChangeHighlight(highlightColors[0]);
                }
                else if (selectedUnitState == 2 && (previousHoverCell.attackable_G || previousHoverCell.attackable_F)) {
                    previousHoverCell.ChangeHighlight(highlightColors[3]);
                }
            }
        }
        else if (summonShowing) { // 正在选择召唤地点
            if (currentHoverCell && currentHoverCell.IsHighlighted) currentHoverCell.ChangeHighlight(highlightColors[1]);
            if (previousHoverCell && previousHoverCell.IsHighlighted) previousHoverCell.ChangeHighlight(highlightColors[5]);
        }
        else if (atfShowing) { // 正在选择投放地点
            if (currentHoverCell && currentHoverCell.IsHighlighted) currentHoverCell.ChangeHighlight(highlightColors[3]);
            if (previousHoverCell && previousHoverCell.IsHighlighted) {
                int ATFType = _Deck[myCamp][0];
                if (ATFType == 1 || ATFType == 11 || ATFType == 3 || ATFType == 13 || ATFType == 4 || ATFType == 14) previousHoverCell.ChangeHighlight(highlightColors[0]);
                else if (ATFType == 2 || ATFType == 12) previousHoverCell.ChangeHighlight(highlightColors[4]);
            }
        }

        // 信息板的显示
        if (infoPanelAva) {
            if (currentHoverCell) {
                if (currentHoverCell.Unit && currentHoverCell.Unit.Type != 0 && currentHoverCell.Unit.infoPanelActive)
                    currentHoverCell.Unit.infoPanel.gameObject.SetActive(true);
                if (currentHoverCell.FlyingUnit && currentHoverCell.FlyingUnit.infoPanelActive)
                    currentHoverCell.FlyingUnit.infoPanel.gameObject.SetActive(true);
            }
            if (previousHoverCell) {
                if (previousHoverCell.Unit && previousHoverCell.Unit.Type != 0)
                    previousHoverCell.Unit.infoPanel.gameObject.SetActive(false);
                if (previousHoverCell.FlyingUnit)
                    previousHoverCell.FlyingUnit.infoPanel.gameObject.SetActive(false);
            }
        }
        previousHoverCell = currentHoverCell;
    }

    /// <summary>
    /// 在线模式下UI界面控制函数
    /// </summary>
    bool canOperate = false;
    public bool CanOperate {
        get { return canOperate; }
        set {
            canOperate = value;
        }
    }

    public void updateUIControl(bool clearPath, bool clearHighlight)
    {
        if (!canOperate) { // 锁定界面
            for (int i = 0; i < BtnSummon.Length; i++) {
                BtnSummon[i].interactable = false;
            }
            BtnAtf.interactable = false;
            ACImage[0].color = new Color(0.5f, 0.5f, 0.5f);
            BtnEndTurn.interactable = false;
            BtnConcede.interactable = false;

            // 清理
            if (clearPath) grid.ClearPath();
            if (clearHighlight) {
                grid.ClearHighlight();
                selectedUnit = null;
            }
            atfShowing = false;
            summonShowing = false;
            if (FOG.activeSelf) FOG.SetActive(false);
            SetInfoPanel(true);
        }
        else { // 解锁界面
            // 召唤生物按钮
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    if (_Mana[myCamp] >= _Cost[myCamp][i * 3 + j + 1] && _Remain[myCamp][i] > 0)
                        BtnSummon[i * 3 + j].interactable = true;
                    else BtnSummon[i * 3 + j].interactable = false;
                }
            }
            // 神器按钮
            if (canCast && _Mana[myCamp] >= _Cost[myCamp][0] && _ACD[myCamp] == 0) {
                BtnAtf.interactable = true;
                ACImage[0].color = new Color(1f, 1f, 1f);
            }
            else {
                BtnAtf.interactable = false;
                ACImage[0].color = new Color(0.5f, 0.5f, 0.5f);
            }
            BtnEndTurn.interactable = true;
            BtnConcede.interactable = true;
        }
    }

    int previousBtnInx = -1;
    int readyUnitType = 0;
    int readyUnitLevel = 0;
    bool summonShowing = false;
    public void readySummon(int btnInx) // 0 到 8，由场景UI调用
    {
        if (summonShowing && previousBtnInx == btnInx) {
            enableSummonCell(false);
            return;
        }

        grid.ClearHighlight();
        grid.ClearPath();
        selectedUnit = null;
        atfShowing = false;
        if (FOG.activeSelf) FOG.SetActive(false);
        SetInfoPanel(false);

        readyUnitType = _Deck[myCamp][btnInx / 3 + 1];
        readyUnitLevel = btnInx % 3 + 1;
        enableSummonCell(true);
        previousBtnInx = btnInx;
    }
    public void enableSummonCell(bool option)
    {
        summonShowing = option;
        for (int i = 0; i < HexMetrics.summonPointCoords.Length; i++) {
            HexCell cell = grid.GetCell(new HexCoordinates(HexMetrics.summonPointCoords[i][0], HexMetrics.summonPointCoords[i][2]));
            if (cell.SpecialIndex == 3 + myCamp) {
                if (option) {
                    if (HexMetrics.isFlying(readyUnitType)) {
                        if (!cell.FlyingUnit) cell.EnableHighlight(highlightColors[5]);
                    }
                    else {
                        if (!cell.Unit) cell.EnableHighlight(highlightColors[5]);
                    }
                }
                else cell.DisableHighlight();
            }
        }
    }

    bool atfShowing = false;
    public void readyCast() // 由场景调用
    {
        if (atfShowing) {
            atfShowing = false;
            grid.ClearHighlight();
            if (FOG.activeSelf) FOG.SetActive(false);
            return;
        }
        atfShowing = true;

        summonShowing = false;
        grid.ClearHighlight();
        grid.ClearPath();
        selectedUnit = null;
        if (FOG.activeSelf) FOG.SetActive(false);
        SetInfoPanel(false);

        grid.readyCast(_Deck[myCamp][0]);
    }

    int unitId = 2;
    public void StartSummon()
    {
        // 发送命令
        int[] arg = new int[] { readyUnitType, readyUnitLevel, currentCell.coordinates.X, currentCell.coordinates.Y, unitId }; // id不知道
        unitId += 1;
        Command C = new Command(grid.curTurn, CommandType.SUMMON, arg);
        sendCommand(C);

        CanOperate = false;
        updateUIControl(false, true);
    }

    public void StartMove()
    {
        // 发送命令
        int[] arg = new int[] { selectedUnit.Id, currentCell.coordinates.X, currentCell.coordinates.Y, 0, 0 };
        Command C = new Command(grid.curTurn, CommandType.MOVE, arg);
        sendCommand(C);

        CanOperate = false;
        updateUIControl(false, true);
    }
    public void StartAttack()
    {
        if (currentCell.attackable_G && currentCell.attackable_F) { // 不确定攻击哪一个
            FOG.SetActive(true);
            FOG.transform.position = Camera.main.WorldToScreenPoint(currentCell.Position);
            FOGState = 3;
        }
        else {
            if (currentCell.attackable_G) StartAttack02(false);
            else if (currentCell.attackable_F) StartAttack02(true);
            else Debug.LogError("no Attack Target!");
        }
    }
    void StartAttack02(bool isFU)
    {
        // 发送命令
        int targetId;
        if (isFU) targetId = currentCell.FlyingUnit.Id;
        else targetId = currentCell.Unit.Id;
        int[] arg = new int[] { selectedUnit.Id, targetId, 0, 0, 0 };
        Command C = new Command(grid.curTurn, CommandType.PREATK, arg);
        sendCommand(C);

        CanOperate = false;
        updateUIControl(true, true);
    }

    public bool canCast { get; set; }
    public void StartATF()
    {
        // 发送命令
        int ATFType = _Deck[myCamp][0];
        Command C = null;
        if (ATFType == 1 || ATFType == 11 || ATFType == 3 || ATFType == 13 || ATFType == 4 || ATFType == 14) {
            // 发送命令
            int[] arg = new int[] { myCamp, ATFType, currentCell.coordinates.X, currentCell.coordinates.Y, 0 };
            C = new Command(grid.curTurn, CommandType.ATF, arg);
            sendCommand(C);
            if (ATFType == 3 || ATFType == 13) {
                canCast = false;
            }

            CanOperate = false;
            updateUIControl(false, true);
        }
        else if (ATFType == 2 || ATFType == 12) { // 选择飞行还是陆地？
            bool hasGU = currentCell.Unit && currentCell.Unit.Camp == myCamp;
            bool hasFU = currentCell.FlyingUnit && currentCell.FlyingUnit.Camp == myCamp;
            if (hasGU && hasFU) {
                FOG.SetActive(true);
                FOG.transform.position = Camera.main.WorldToScreenPoint(currentCell.Position);
                FOGState = 4;
            }
            else {
                if (hasGU) ShieldATF(false);
                else if (hasFU) ShieldATF(true);
                else Debug.LogError("error: !hasGU && !hasFU...");
            }
        }
    }
    void ShieldATF(bool isFU)
    {
        // 发送命令
        int[] arg;
        if (isFU) arg = new int[] { myCamp, _Deck[myCamp][0], 0, 0, currentCell.FlyingUnit.Id };
        else arg = new int[] { myCamp, _Deck[myCamp][0], 0, 0, currentCell.Unit.Id };
        Command C = new Command(grid.curTurn, CommandType.ATF, arg);
        sendCommand(C);
        canCast = false;

        CanOperate = false;
        updateUIControl(false, true);
    }

    public void chooseFOG(bool chooseFlying)
    {
        HexUnit u = null;
        if (chooseFlying) u = currentCell.FlyingUnit;
        else u = currentCell.Unit;
        if (FOGState == 1) { // 选择移动
            if (u.canAct) {
                selectedUnit = u;
                selectedUnitState = 1;
                grid.computeDis(currentCell, 0, chooseFlying, true);
            }
            else {
                StartCoroutine(popErrorBar("This creature cannot move now!"));
                selectedUnit = null;
            }
        }
        else if (FOGState == 2) { // 选择攻击
            if (u.Camp == myCamp && u.canAct) {
                selectedUnit = u;
                selectedUnitState = 2;
                grid.computeDis(currentCell, 1, chooseFlying, true);
            }
            else {
                selectedUnit = u;
                selectedUnitState = 3;
                grid.computeDis(currentCell, 3, chooseFlying, true);
            }
        }
        else if (FOGState == 3) { // 选择攻击对象
            StartAttack02(chooseFlying);
        }
        else if (FOGState == 4) { // 选择阳炎之盾对象
            ShieldATF(chooseFlying);
        }
        else if (FOGState == 5) { // 观测攻击范围
            selectedUnit = u;
            selectedUnitState = 3;
            grid.computeDis(currentCell, 3, chooseFlying, true);
        }
        else Debug.LogError("no such FOGState" + FOGState);
        FOG.SetActive(false);
    }

    public void endTurn()
    {
        CanOperate = false;
        updateUIControl(false, true);
        // 发送命令
        int[] arg = new int[] { myCamp, 0, 0, 0, 0 };
        Command C = new Command(grid.curTurn, CommandType.ROUNDOVER, arg);
        sendCommand(C);
    }

    public int getUnitPanelInxByType(int campInx, int type) // 0 或 1 或 2
    {
        for (int i = 1; i <= 3; i++) {
            if (_Deck[campInx][i] == type) return (i - 1);
        }
        return -1;
    }

    /// <summary>
    /// 离线模式下UI界面控制函数
    /// </summary>
    public void Play()
    {
        Debug.Log("Start Playing!");
        grid.onPlay = true;
        SetBtnOnPlayState();
        grid.RunCommands();
    }

    public void Pause()
    {
        grid.onPlay = false;
        SetBtnStartState();
    }

    bool Lock = false;
    public void Replay()
    {
        if (!Lock) Lock = true;
        grid.onPlay = false;
        SetBtnStartState();
        grid.RestartCommands();
        reinitVariables();
        UpdateAllPanel();
        Lock = false;
    }

    public void FastForward()
    {
        grid.FastForwardCommand();
    }

    public void Swap()
    {
        myCamp = (myCamp + 1) % 2;
        UpdateAllPanel();
        HexMapCamera.swap();
    }

    public void Back()
    {
        SceneManager.LoadScene(0);
    }

    public bool _autoSwap { get; set; }
    public void autoSwap()
    {
        _autoSwap = TogAutoSwap.isOn;
        BtnSwap.interactable = !_autoSwap;
    }

    void SetBtnStartState()
    {
        BtnPlay.interactable = true;
        BtnPause.interactable = false;
        BtnStop.interactable = true;

        SUBOFFLINEPanel.SetActive(true);
        BtnFastForward.interactable = true;
    }

    void SetBtnOnPlayState()
    {
        BtnPlay.interactable = false;
        BtnPause.interactable = true;
        BtnStop.interactable = false;

        SUBOFFLINEPanel.SetActive(false);
        BtnFastForward.interactable = false;
    }

    public void SetBtnOverState()
    {
        BtnPlay.interactable = false;
        BtnPause.interactable = false;
        BtnStop.interactable = true;
        BtnFastForward.interactable = false;
    }

    public void PopConcedePanel()
    {
        ConcedePanel.SetActive(true);
        HexMapCamera.Locked = true;
        enabled = false;
    }

    public void WithDrawConcedePanel()
    {
        ConcedePanel.SetActive(false);
        HexMapCamera.Locked = false;
        enabled = true;
    }

    public void Concede()
    {
        // Application.Quit();
        WithDrawConcedePanel();

        int[] arg = new int[] { myCamp, 0, 0, 0, 0 };
        Command C = new Command(grid.curTurn, CommandType.GAMEOVER, arg);
        sendCommand(C);
    }

    /// <summary>
    /// 命令操作
    /// </summary>
    public const int argNum = 5;
    public void LoadCommands()
    {
        grid.commands.Clear();
        // string path = "E:/Tsinghua/AC/record(3)";
        if (!File.Exists(recordPath)) {
            socketClient.popMsgBox("File does not exist " + recordPath, 0);
            Debug.LogError("File does not exist " + recordPath);
            return;
        }

        using (BinaryReader2 reader = new BinaryReader2(File.OpenRead(recordPath))) {
            /*int header = reader.ReadInt32(false);
            if (header != 0) Debug.LogError("file error!");*/
            try {
                int curComInx = 0;
                while (true) { // 不停地读入命令数据
                    int round = reader.ReadInt32(false);
                    if (round == -1) {
                        Debug.Log("reading file success: " + recordPath);
                        Debug.Log("commands total count: " + grid.commands.Count.ToString());
                        break;
                    }
                    Debug.Log("curComInx = " + curComInx.ToString());
                    Debug.Log("round = " + round);
                    CommandType command = (CommandType)reader.ReadInt32(false);
                    Debug.Log("command = " + command);
                    int[] arg = new int[argNum];
                    for (int i = 0; i < argNum; i++) {
                        arg[i] = reader.ReadInt32(false);
                        Debug.Log("arg[" + i + "] = " + arg[i]);
                    }
                    grid.commands.Add(new Command(round, command, arg));
                    curComInx += 1;
                }
            }
            catch (Exception e) {
                socketClient.popMsgBox("replay file error!", 0);
                Debug.LogError(e);
                return;
            }
        }

        try {
            // 提前读取地图、白天or黑夜、阵营
            InitiateEnvFromCommands();
            // 离线模式下提前读取命令初始化Deck
            InitiateDeckFromCommands();
            InitiateVariablesFromDeck();
            InitiatePanel();
            UpdateAllPanel();
        }
        catch (Exception e) {
            socketClient.popMsgBox("fail to initiate the game!", 0);
            Debug.LogError(e);
            return;
        }
        MsgCanvasMask.SetActive(false);
    }

    public void sendCommand(Command C)
    {
        string msg = socketClient.CommandToMsg(C);
        socketClient.Send(msg);
    }

    // 初始化全局环境变量
    bool hasInitEnv = false;
    public void InitiateEnvFromCommands()
    {
        if (!hasInitEnv) {
            hasInitEnv = true;

            // 直接读取第一个命令
            Command C = grid.commands[0];
            myCamp = C.arg[0];
            mapIndex = C.arg[1];
            skyboxIndex = 0;

            // 立刻改变漫反射颜色
            if (skyboxIndex == 0)
                RenderSettings.ambientLight = new Color(142f / 255, 149f / 255, 164f / 255);
            else if (skyboxIndex == 1) {
                RenderSettings.ambientLight = new Color(10f / 255, 12f / 255, 87f / 255);
            }

            grid.Initiate();
            if (gameMode == 0) {
                HexMapCamera.ValidatePosition(myCamp);
            }
            else if (gameMode == 1) {
                setupMenu.gameObject.SetActive(true);
            }
        }
    }

    // 初始化Deck
    bool hasInitDeck = false;
    public void InitiateDeckFromCommands()
    {
        if (!hasInitDeck) {
            hasInitDeck = true;
            int comPointer = 0;
            for (int k = 0; k < 2; k++) {
                while (grid.commands[comPointer].type != CommandType.SETUPDECK) {
                    comPointer += 1;
                    if (comPointer > 3) Debug.LogError("The First few Commands are not SETUPDECK!" + grid.commands[0].type);
                }
                Command C = grid.commands[comPointer];
                int activeCamp = C.arg[0];
                for (int i = 0; i < 4; i++) {
                    if (C.arg[i + 1] != _Deck[activeCamp][i]) {
                        _Deck[activeCamp][i] = C.arg[i + 1];
                    }
                }
                comPointer += 1;
            }
        }
    }

    public void InitiateVariablesFromDeck()
    {
        // 初始化
        for (int campInx = 0; campInx < 2; campInx++) {
            int atfType = _Deck[campInx][0];
            _Cost[campInx][0] = HexMetrics.atfProperty[atfType % 10][0];
            _CD[campInx][0] = HexMetrics.atfProperty[atfType % 10][1];
            for (int i = 0; i < 9; i += 3) {
                for (int j = 1; j <= 3; j++) {
                    _Cost[campInx][i + j] = HexMetrics.unitProperty[_Deck[campInx][i / 3 + 1]][j - 1][0];
                    _Atk[campInx][i + j - 1] = HexMetrics.unitProperty[_Deck[campInx][i / 3 + 1]][j - 1][1];
                    _MaxHp[campInx][i + j - 1] = HexMetrics.unitProperty[_Deck[campInx][i / 3 + 1]][j - 1][2];
                    _MinAtkRan[campInx][i + j - 1] = HexMetrics.unitProperty[_Deck[campInx][i / 3 + 1]][j - 1][3];
                    _MaxAtkRan[campInx][i + j - 1] = HexMetrics.unitProperty[_Deck[campInx][i / 3 + 1]][j - 1][4];
                    _MovingRan[campInx][i + j - 1] = HexMetrics.unitProperty[_Deck[campInx][i / 3 + 1]][j - 1][5];
                    _CD[campInx][i + j] = HexMetrics.unitProperty[_Deck[campInx][i / 3 + 1]][j - 1][6];
                }
                _MaxRemain[campInx][i / 3] = HexMetrics.unitProperty[_Deck[campInx][i / 3 + 1]][0][7];
                _Remain[campInx][i / 3] = _MaxRemain[campInx][i / 3];
                _UCDList[campInx][i / 3].Clear();
            }
            _ACD[campInx] = 0;
            _MirHp[campInx] = 30;
            _Score[campInx] = 0;
        }
        _MaxMana[0] = _Mana[0] = 1;
        _MaxMana[1] = _Mana[1] = 2;
        _ManaIncr[0] = true;
        _ManaIncr[1] = false;
    }

    public void InitiatePanel()
    {
        // 初始化按钮信息面板的位置、内容
        BtnSummonInfoPanel = new RectTransform[20];
        Text[] infoText = new Text[8];
        for (int i = 0; i < 20; i++) {
            int campInx = i / 10;
            if (i != 0 && i != 10) {
                BtnSummonInfoPanel[i] = Instantiate(InfoPanelPrefab).GetComponent<RectTransform>();
                for (int j = 0; j < 8; j++) {
                    infoText[j] = BtnSummonInfoPanel[i].GetChild(j).GetComponent<Text>();
                }

                int unitLevel, unitType;
                if (i < 10) {
                    unitType = _Deck[campInx][(i - 1) / 3 + 1];
                    unitLevel = (i - 1) % 3 + 1;
                }
                else {
                    unitType = _Deck[campInx][(i - 11) / 3 + 1];
                    unitLevel = (i - 11) % 3 + 1;
                }

                infoText[0].text = string.Format("{0} (level {1})", HexMetrics.unitNameZH_CN[unitType], unitLevel);
                infoText[1].text = string.Format("cost: {0}", HexMetrics.unitProperty[unitType][unitLevel - 1][0]); // 费用
                infoText[2].text = string.Format("attack: {0}", HexMetrics.unitProperty[unitType][unitLevel - 1][1]); // 攻击
                infoText[3].text = string.Format("maxHp: {0}", HexMetrics.unitProperty[unitType][unitLevel - 1][2]); // 最大生命
                infoText[4].text = string.Format("attack range: {0}-{1}", HexMetrics.unitProperty[unitType][unitLevel - 1][3], HexMetrics.unitProperty[unitType][unitLevel - 1][4]);
                infoText[5].text = string.Format("moving range: {0}", HexMetrics.unitProperty[unitType][unitLevel - 1][5]); // 行动速度
                infoText[6].text = string.Format("cd: {0}", HexMetrics.unitProperty[unitType][unitLevel - 1][6]); // cd
                infoText[7].text = string.Format("{0}", HexMetrics.unitEntry[unitType][unitLevel - 1]); // 词条描述

                // 颜色
                BtnSummonInfoPanel[i].GetComponent<Image>().color = HexMetrics.unitPanelColor[campInx];
                if (campInx == 0) infoText[0].color = new Color(0.1f, 0.1f, 1f);
                else if (campInx == 1) infoText[0].color = new Color(1f, 0.1f, 0.1f);
                for (int k = 1; k <= 7; k++) infoText[k].color = HexMetrics.unitInfoColor[campInx];
            }
            else {
                BtnSummonInfoPanel[i] = Instantiate(InfoPanelATFPrefab).GetComponent<RectTransform>();
                for (int j = 0; j < 4; j++) {
                    infoText[j] = BtnSummonInfoPanel[i].GetChild(j).GetComponent<Text>();
                }

                int ATFInx = (_Deck[campInx][0] - 1) % 10 + 1;
                infoText[0].text = HexMetrics.atfNameZH_CN[ATFInx];
                infoText[1].text = string.Format("cost: {0}", HexMetrics.atfProperty[ATFInx][0]); // 费用
                infoText[2].text = string.Format("cd: {0}", HexMetrics.atfProperty[ATFInx][1]); // cd
                infoText[3].text = HexMetrics.atfEntry[ATFInx];
            }
            BtnSummonInfoPanel[i].transform.SetParent(transform, false);
            BtnSummonInfoPanel[i].pivot = new Vector2(1f, 1f);
            BtnSummonInfoPanel[i].anchorMin = new Vector2(1f, 1f);
            BtnSummonInfoPanel[i].anchorMax = new Vector2(1f, 1f);
            BtnSummonInfoPanel[i].anchoredPosition = new Vector2(-320f, 0f);
            BtnSummonInfoPanel[i].gameObject.SetActive(false);
        }

        TimePanel.gameObject.SetActive(false);

        if (gameMode == 1) {
            OFFLINEPanel.SetActive(false);
            SUBOFFLINEPanel.SetActive(false);
            BackBtnPanel.SetActive(false);
            ConcedeBtnPanel.SetActive(true);
            PanelSelect.SetActive(true);
        }
        else if (gameMode == 0) {
            OFFLINEPanel.SetActive(true);
            SUBOFFLINEPanel.SetActive(true);
            BackBtnPanel.SetActive(true);
            ConcedeBtnPanel.SetActive(false);
            PanelSelect.SetActive(false);
            SetBtnStartState();
            autoSwap();
        }

        FOG.SetActive(false);

        // 唤醒显示
        HexMapCamera.Locked = false;
        enabled = true;
        grid.enabled = true;
        gameObject.SetActive(true);

        // 确定各个按钮是否可操作
        CanOperate = false;
        updateUIControl(true, true);
    }

    public void UpdateAllPanel() // 根据myCamp来更新面板，确保Deck的八个位都赋值了
    {
        // 更新按钮、名称面板颜色 BtnSummon、TypePanel
        for (int i = 0; i < 3; i++) {
            BtnSummon[i].GetComponent<Image>().sprite = LevelSprite[2 * i + myCamp];
            BtnSummon[i + 3].GetComponent<Image>().sprite = LevelSprite[2 * i + myCamp];
            BtnSummon[i + 6].GetComponent<Image>().sprite = LevelSprite[2 * i + myCamp];
            if (myCamp == 0) {
                TypePanel[i].color = Color.blue;
                TypePanel[i + 3].color = Color.red;
            }
            else {
                TypePanel[i].color = Color.red;
                TypePanel[i + 3].color = Color.blue;
            }
        }

        // 更新PopPanel的颜色
        if (myCamp == 0) {
            PopPanelDown.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.7f, 0.4f);
            PopPanelUp.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.8f, 0.8f);
            PopPanelMaterial.color = new Color(0.1f, 0.1f, 0.8f, 1f);
        }
        else {
            PopPanelDown.GetComponent<Image>().color = new Color(0.7f, 0f, 0.1f, 0.4f);
            PopPanelUp.GetComponent<Image>().color = new Color(0.8f, 0.1f, 0.1f, 0.8f);
            PopPanelMaterial.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        }

        // 更新阵营计分板、神迹血槽颜色
        if (myCamp == 0) {
            ACCamp[0].color = new Color(0f, 0f, 1f, 0.78f);
            ACCamp[6].color = new Color(1f, 0f, 0f, 0.78f);
            ACCamp[1].color = new Color(0f, 0.12f, 0.67f, 0.9f);
            ACCamp[7].color = new Color(0.5f, 0f, 0f, 0.9f);
            ACCamp[2].sprite = CampHpSource[0];
            ACCamp[2].color = new Color(0f, 0f, 1f, 0.9f);
            ACCamp[8].sprite = CampHpSource[1];
            ACCamp[8].color = new Color(1f, 0f, 0f, 0.9f);
            ACCamp[3].color = new Color(0f, 0f, 1f, 0.95f);
            ACCamp[9].color = new Color(1f, 0f, 0f, 0.95f);
            ACCamp[4].color = new Color(0.27f, 0.66f, 1f, 1f);
            ACCamp[10].color = new Color(1f, 0.27f, 0.22f, 1f);
            ACCamp[5].sprite = CampSprite[0];
            ACCamp[11].sprite = CampSprite[1];
        }
        else {
            ACCamp[0].color = new Color(1f, 0f, 0f, 0.78f);
            ACCamp[6].color = new Color(0f, 0f, 1f, 0.78f);
            ACCamp[1].color = new Color(0.5f, 0f, 0f, 0.9f);
            ACCamp[7].color = new Color(0f, 0.12f, 0.67f, 0.9f);
            ACCamp[2].sprite = CampHpSource[1];
            ACCamp[2].color = new Color(1f, 0f, 0f, 0.9f);
            ACCamp[8].sprite = CampHpSource[0];
            ACCamp[8].color = new Color(0f, 0f, 1f, 0.9f);
            ACCamp[3].color = new Color(1f, 0f, 0f, 0.95f);
            ACCamp[9].color = new Color(0f, 0f, 1f, 0.95f);
            ACCamp[4].color = new Color(1f, 0.27f, 0.22f, 1f);
            ACCamp[10].color = new Color(0.27f, 0.66f, 1f, 1f);
            ACCamp[5].sprite = CampSprite[1];
            ACCamp[11].sprite = CampSprite[0];
        }
        

        // 更新生物和神器的名字和图像 ACName、ACImage
        int OppCamp = (myCamp + 1) % 2;
        int atfType_M = _Deck[myCamp][0] % 10;
        int atfType_O = _Deck[OppCamp][0] % 10;
        ACName[0].text = HexMetrics.atfNameZH_CN_Lite[atfType_M];
        ACName[4].text = HexMetrics.atfNameZH_CN_Lite[atfType_O];
        ACImage[0].sprite = ACSprite[atfType_M + 20];
        ACImage[4].sprite = ACSprite[atfType_O + 20];

        for (int i = 1; i <= 3; i++) {
            int type_M = _Deck[myCamp][i];
            int type_O = _Deck[OppCamp][i];
            
            ACName[i].text = HexMetrics.unitNameZH_CN[type_M];
            ACName[i + 4].text = HexMetrics.unitNameZH_CN[type_O];
            ACImage[i].sprite = ACSprite[type_M];
            ACImage[i + 4].sprite = ACSprite[type_O];
        }

        // 更新法力消耗显示 ACMana
        ACMana[0].text = _Cost[myCamp][0].ToString();
        ACMana[10].text = _Cost[OppCamp][0].ToString();
        for (int i = 0; i < 9; i += 3) {
            for (int j = 1; j <= 3; j++) {
                ACMana[i + j].text = HexMetrics.unitProperty[_Deck[myCamp][i / 3 + 1]][j - 1][0].ToString();
            }
        }

        // 更新剩余法力显示 ManaText、OppManaText
        UpdateManaText();
        UpdateOppManaText();

        // 神器冷却时间显示 atfCDText
        UpdateAtfCDText();

        // 更新生物槽容量 RemainText
        for (int i = 0; i < 3; i++) UpdateRemainText(i);

        // 更新 CDUnits01-03、CDText01-03
        for (int i = 0; i < 3; i++) UpdateCDText(i);

        // 更新 MirHpText、ScoreText
        UpdateMirHpText(true);
        UpdateMirHpText(false);
        UpdateScoreText(true);
        UpdateScoreText(false);

        // 隐藏所有按钮信息面板
        for (int i = 0; i < 10; i++) {
            BtnSummonInfoPanel[i + OppCamp * 10].gameObject.SetActive(false);
        }
    }

    public void UpdateManaText()
    {
        ManaText.text = string.Format("{0}/{1}", _Mana[myCamp], _MaxMana[myCamp]);
    }
    public void UpdateOppManaText()
    {
        int OppCamp = (myCamp + 1) % 2;
        OppManaText.text = string.Format("{0}/{1}", _Mana[OppCamp], _MaxMana[OppCamp]);
    }
    public void UpdateAtfCDText(string s = "")
    {
        if (s != "") atfCDText.text = s;
        else {
            if (_ACD[myCamp] == 0) atfCDText.text = "";
            else atfCDText.text = _ACD[myCamp].ToString();
        }
    }
    public void UpdateRemainText(int i)
    {
        RemainText[i].text = string.Format("{0}/{1}", _Remain[myCamp][i], _MaxRemain[myCamp][i]);
    }
    public void UpdateCDText(int index)
    {
        if (index == 0) {
            int count01 = _UCDList[myCamp][0].Count;
            for (int i = 0; i < count01; i++) {
                CDUnits01[i].SetActive(true);
                CDText01[i].text = _UCDList[myCamp][0][i].ToString();
            }
            for (int i = count01; i < CDUnits_num; i++) {
                CDUnits01[i].SetActive(false);
                CDText01[i].text = "";
            }
        }
        else if (index == 1) {
            int count02 = _UCDList[myCamp][1].Count;
            for (int i = 0; i < count02; i++) {
                CDUnits02[i].SetActive(true);
                CDText02[i].text = _UCDList[myCamp][1][i].ToString();
            }
            for (int i = count02; i < CDUnits_num; i++) {
                CDUnits02[i].SetActive(false);
                CDText02[i].text = "";
            }
        }
        else if (index == 2) {
            int count03 = _UCDList[myCamp][2].Count;
            for (int i = 0; i < count03; i++) {
                CDUnits03[i].SetActive(true);
                CDText03[i].text = _UCDList[myCamp][2][i].ToString();
            }
            for (int i = count03; i < CDUnits_num; i++) {
                CDUnits03[i].SetActive(false);
                CDText03[i].text = "";
            }
        }
        else 
            Debug.LogError("Error: UpdateCDText index out of range! index = " + index);
    }

    public void UpdateMirHpText(bool isMyCamp)
    {
        int health;
        Text hpText;
        Slider hpBar;
        if (isMyCamp) {
            health = _MirHp[myCamp];
            hpText = MirHpText[0];
            hpBar = MirHpBar[0];
        }
        else {
            health = _MirHp[1 - myCamp];
            hpText = MirHpText[1];
            hpBar = MirHpBar[1];
        }

        hpText.text = health.ToString();
        if (health == 30) {
            hpText.color = HexUnit.hpTextColor[0];
        }
        else if (health > 10) {
            hpText.color = HexUnit.hpTextColor[1];
        }
        else hpText.color = HexUnit.hpTextColor[2];

        if (health == 0)
            hpBar.value = 0;
        else hpBar.value = health + 2;
    }

    public void UpdateScoreText(bool isMyCamp)
    {
        if (isMyCamp) {
            ScoreText[0].text = _Score[myCamp].ToString();
        }
        else {
            ScoreText[1].text = _Score[1 - myCamp].ToString();
        }
    }

    public void reinitVariables()
    {
        // 初始化
        for (int campInx = 0; campInx < 2; campInx++) {
            for (int i = 0; i < 9; i += 3) {
                _Remain[campInx][i / 3] = _MaxRemain[campInx][i / 3];
                _UCDList[campInx][i / 3].Clear();
            }
            _ACD[campInx] = 0;
            _MirHp[campInx] = 30;
            _Score[campInx] = 0;
        }
        _MaxMana[0] = _Mana[0] = 1;
        _MaxMana[1] = _Mana[1] = 2;
        _ManaIncr[0] = true;
        _ManaIncr[1] = false;
    }
    
    // 在线模式下，选完卡组后调用
    public void AfterSelectDeck()
    {
        for (int i = 1, j = 0; i < 4 && j < CreaturesNum; j++) {
            if (setupMenu.UnitSelected[j]) {
                _Deck[myCamp][i] = myCamp * 10 + j + 1;
                i++;
            }
        }
        for (int j = 0; j < ATFNum; j++) {
            if (setupMenu.AtfSelected[j]) {
                _Deck[myCamp][0] = myCamp * 10 + j + 1;
                break;
            }
        }
        int[] arg = new int[] { myCamp, _Deck[myCamp][0], _Deck[myCamp][1], _Deck[myCamp][2], _Deck[myCamp][3] };
        Command C = new Command(0, CommandType.SETUPDECK, arg);
        // 发送命令
        sendCommand(C);
    }

    public IEnumerator popPanel(string mainText, string subText, float duration, float duration2, bool fromDown)
    {
        RectTransform PopPanel = null;
        if (fromDown) {
            PopPanelDown.anchoredPosition = new Vector2(-350, -200);
            PopPanelDown.gameObject.SetActive(true);
            popMainTextDown.text = mainText;
            popSubTextDown.text = subText;
            PopPanel = PopPanelDown;
        }
        else {
            PopPanelUp.anchoredPosition = new Vector2(490, 260);
            PopPanelUp.gameObject.SetActive(true);
            popTextUp.text = mainText;
            PopPanel = PopPanelUp;
        }

        Vector3 a = PopPanel.anchoredPosition;
        Vector3 b = a;
        if (fromDown) b += Vector3.up * 400f;
        else b -= Vector3.up * 400f;
        Vector3 c = b;
        float speed = 1 / duration;
        for (float t = 0; t < 1f; t += Time.deltaTime * speed) {
            PopPanel.anchoredPosition = Bezier.GetPoint(a, b, c, t);
            yield return null;
        }
        yield return new WaitForSeconds(duration2);

        c = a;
        a = b;
        b = c;
        for (float t = 0; t < 1f; t += Time.deltaTime * speed) {
            PopPanel.anchoredPosition = Bezier.GetPoint(a, b, c, t);
            yield return null;
        }

        PopPanel.gameObject.SetActive(false);
    }

    public IEnumerator popGameoverPanel(float duration, float duration2)
    {
        Image panelIm = GameoverPanel.GetComponent<Image>();
        Image iconIm = GameoverPanel.transform.GetChild(0).GetComponent<Image>();
        Text textL = GameoverPanel.transform.GetChild(1).GetComponent<Text>();
        Text textR = GameoverPanel.transform.GetChild(2).GetComponent<Text>();
        Color panelImColor = panelIm.color;
        Color iconImColor = iconIm.color;
        Color textLColor = textL.color;
        Color textRColor = textR.color;

        float speed = 1 / duration;
        GameoverPanel.SetActive(true);
        for (float t = 0; t < 1f; t += Time.deltaTime * speed) {
            panelIm.color = new Color(panelImColor.r, panelImColor.g, panelImColor.b, t);
            iconIm.color = new Color(iconImColor.r, iconImColor.g, iconImColor.b, t);
            textL.color = new Color(textLColor.r, textLColor.g, textLColor.b, t);
            textR.color = new Color(textRColor.r, textRColor.g, textRColor.b, t);
            yield return null;
        }
        yield return new WaitForSeconds(duration2);

        for (float t = 0; t < 1f; t += Time.deltaTime * speed) {
            panelIm.color = new Color(panelImColor.r, panelImColor.g, panelImColor.b, 1 - t);
            iconIm.color = new Color(iconImColor.r, iconImColor.g, iconImColor.b, 1 - t);
            textL.color = new Color(textLColor.r, textLColor.g, textLColor.b, 1 - t);
            textR.color = new Color(textRColor.r, textRColor.g, textRColor.b, 1 - t);
            yield return null;
        }

        GameoverPanel.SetActive(false);
    }

    public IEnumerator popTimePanel(bool enter)
    {
        if (enter) {
            TimePanel.anchoredPosition = new Vector2(330f, 110f);
            TimePanel.gameObject.SetActive(true);
        }
        else TimePanel.anchoredPosition = new Vector2(330f, -10f);

        Vector3 a = TimePanel.anchoredPosition;
        Vector3 b = a;
        if (enter) b -= Vector3.up * 120f;
        else b += Vector3.up * 120f;
        Vector3 c = b;
        for (float t = 0; t < 1f; t += Time.deltaTime) {
            TimePanel.anchoredPosition = Bezier.GetPoint(a, b, c, t);
            yield return null;
        }
        if (!enter) TimePanel.gameObject.SetActive(false);
    }

    public IEnumerator popErrorBar(string msg)
    {
        GameObject errorBar = Instantiate(ErrorBarPrefab);
        errorBar.transform.SetParent(transform);
        errorBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 380f);
        Image errorImage = errorBar.GetComponent<Image>();
        Text errorText = errorBar.transform.GetChild(0).GetComponent<Text>();
        errorText.text = msg;

        float alpha = 0.8f;
        errorImage.color = new Color(0f, 0f, 0f, alpha);
        errorText.color = new Color(1f, 1f, 1f, alpha);
        yield return new WaitForSeconds(2f);

        while (true) {
            alpha -= Time.deltaTime * 0.2f;
            if (alpha <= 0) break;
            errorImage.color = new Color(0f, 0f, 0f, alpha);
            errorText.color = new Color(1f, 1f, 1f, alpha);
            
            yield return null;
        }
        Destroy(errorBar);
    }

    public void PopInstructionPanel(bool opt)
    {
        InstructionPanel.SetActive(opt);
        HexMapCamera.Locked = opt;
        enabled = !opt;
        grid.enabled = !opt;
    }
    public void DecreseCD(int campInx, int curTurn)
    {
        for (int i = 0; i < 3; i++) {
            for (int j = _UCDList[campInx][i].Count - 1; j >= 0; j--) {
                _UCDList[campInx][i][j] -= 1;
                if (_UCDList[campInx][i][j] == 0) {
                    _UCDList[campInx][i].RemoveAt(j);
                    _Remain[campInx][i] += 1;
                }
            }
        }
        if (curTurn == 50) {
            for (int camp = 0; camp < 2; camp++) {
                for (int i = 0; i < 3; i++) {
                    int unit_type = _Deck[camp][i + 1];
                    _Remain[camp][i] += HexMetrics.unitProperty[unit_type][1][7] - _MaxRemain[camp][i];
                    _MaxRemain[camp][i] = HexMetrics.unitProperty[unit_type][1][7];
                }
            }
        }
        else if (curTurn == 75) {
            for (int camp = 0; camp < 2; camp++) {
                for (int i = 0; i < 3; i++) {
                    int unit_type = _Deck[camp][i + 1];
                    _Remain[camp][i] += HexMetrics.unitProperty[unit_type][2][7] - _MaxRemain[camp][i];
                    _MaxRemain[camp][i] = HexMetrics.unitProperty[unit_type][2][7];
                }
            }
        }
    }
}
