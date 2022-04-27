using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using KT.Core;
using UnityEngine.UI;
using System;

public static class Bezier
{
    // t插值处的位置 (1 - t)^2 A + 2(1 - t)t B + t^2 C
    public static Vector3 GetPoint(Vector3 a, Vector3 b, Vector3 c, float t) // 起点，中点，终点，插值
    {
        float r = 1f - t;
        return r * r * a + 2f * r * t * b + t * t * c;
    }
    // t插值处的方向（导数Derivative）2((1 - t) (B - A) + t(C - B))
    public static Vector3 GetDerivative(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        return 2f * ((1f - t) * (b - a) + t * (c - b));
    }
}

public class HexUnit : MonoBehaviour
{
    [NonSerialized]
    public HexGrid grid;
    [NonSerialized]
    public Transform hpBar;
    [NonSerialized]
    public Text hpText;

    public static Color[] hpTextColor = new Color[] {
        new Color(0.5f, 1f, 0.5f),
        new Color(1f, 0.7f, 0.5f),
        new Color(1f, 0f, 0f)
    };

    [NonSerialized]
    public Transform infoPanel;
    [NonSerialized]
    public Text[] infoText;

    [NonSerialized]
    public Transform recoverEff; // 治疗特效
    [NonSerialized]
    public Transform strengthEff; // 强化特效
    [NonSerialized]
    public Transform debuffEff; // 解除buff特效

    // BUFF特效
    List<Transform> buffEffList = new List<Transform>(); // type == 1（牧师加攻）,type == 2（圣盾）, type = 3 （圣光之耀）
    List<int> buffType = new List<int>();
    public void addBuffEff(int type)
    {
        buffType.Add(type);
        GameObject buffEff = null;
        if (type == 1 || type == 3) { // 加攻
            buffEff = Instantiate(grid.CarryATFBuffPrefab, transform.position, Quaternion.Euler(0f, 0f, 0f));
        }
        else if (type == 2) { // 圣盾
            buffEff = Instantiate(grid.ShieldEffectPrefab, transform.position, Quaternion.Euler(0f, 0f, 0f));
        }
        Transform t = buffEff.transform;
        buffEffList.Add(t);
    }

    public bool removeBuffEff(int type)
    {
        int index = -1;
        for (int i = 0; i < buffType.Count; i++) {
            if (buffType[i] == type) {
                index = i;
                buffType.RemoveAt(i);
                break;
            }
        }
        if (index == -1) {
            return false;
        }

        Destroy(buffEffList[index].gameObject);
        Debug.Log("Remove buffEff: " + index);
        buffEffList.RemoveAt(index);

        GameObject DebuffEff = null;
        if (type == 1 || type == 3) {
            DebuffEff = Instantiate(grid.ATFDebuffPrefab, transform.position, Quaternion.Euler(0f, 0f, 0f));
            debuffEff = DebuffEff.transform;
            Destroy(DebuffEff, 10f);
        }
        else if (type == 2) {
            DebuffEff = Instantiate(grid.ShieldBrkEffectPrefab, transform.position, Quaternion.Euler(0f, 0f, 0f));
            debuffEff = DebuffEff.transform;
            Destroy(DebuffEff, 10f);
        }
        return true;
    }

    public void ClearBuffList()
    {
        while (buffEffList.Count > 0) {
            if (buffEffList[0]) {
                Destroy(buffEffList[0].gameObject);
            }
            buffEffList.RemoveAt(0);
        }
    }

    [NonSerialized]
    public bool ATFEquipTarget = false;

    // 喷火特效（火龙）
    Transform Tongue;
    public Transform FirePrepareEffectPrefab; // 喷火前
    Transform firePreEff;
    public Transform FireBallEffectPrefab; // 火球
    Transform fireBallEff;
    public Transform FireSplashEffectPrefab; // 溅射
    public Transform FireBreathEffectPrefab; // 火流
    Transform fireBreathEff;
    bool isPostATK;

    // 射箭特效（弓箭手）
    Transform archerHand;
    public Transform ArrowPrefab;
    Transform arrow;

    Rigidbody rigid;
    bool HasShot;
    bool HasHit;
    float shootTime;
    float duration;

    // 治疗特效（牧师）
    public Transform[] PriestHealPrefab; // 回合结束时触发的特效
    Transform priestEff01;
    public Transform[] PriestAtfPrefab; // 一直有
    Transform priestEff02;
    public Transform PriestPreAtkPrefab;
    public Transform PriestAtkPrefab;

    // 攻击特效（地狱火）
    Transform rockHandL;
    Transform rockHandR;
    public GameObject InfernoAtkEffectPrefab;
    public GameObject InfernoPostAtkEffectPrefab;

    // 喷冰特效（寒冰之龙）
    public Transform IcePrepareEffectPrefab;
    Transform icePreEff;
    public Transform IceCoreEffectPrefab;
    Transform iceCoreEff;
    public Transform IceBurstEffectPrefab;
    Transform iceBurstEff;
    public Transform IceSpikesEffectPrefab;
    Transform iceSpikesEff;

    static float batOffset = 16f;
    static float WaterAbyssOffset = 10f;
    static float AbyssBottomOffset = 20f;

    bool isFlying;
    public bool IsFlying {
        get { return isFlying; }
    }

    bool isBat;
    public bool IsBat {
        get { return isBat; }
    }

    bool isDragon;
    public bool IsDragon {
        get { return isDragon; }
    }

    // 本unit的阵营
    int camp;
    public int Camp {
        get { return camp; }
        set { camp = value; }
    }

    // 本unit的种类
    int type;
    public int Type {
        get { return type; }
        set { type = value; }
    }

    // 本unit的id
    int id;
    public int Id {
        get { return id; }
        set { id = value; }
    }

    // 本unit的星级level
    int level;
    public int Level {
        get { return level; }
        set {
            level = value;
        }
    }

    public override string ToString()
    {
        return string.Format("{0}(level {1}, id {2})", HexMetrics.unitNameZH_CN[type], level, id);
    }

    // 本unit的血量health
    int health;
    public int Health {
        get { return health; }
        set {
            if (type == 0) {
                grid.newGameUI._MirHp[camp] = value;
                grid.newGameUI.UpdateMirHpText(camp == grid.newGameUI.myCamp);

                int delta = health - value;
                grid.newGameUI._Score[1 - camp] += delta * 1000;
                grid.newGameUI.UpdateScoreText(1 - camp == grid.newGameUI.myCamp);
            }

            health = value;
            hpBar.GetComponent<Slider>().value = (float)health / maxHealth;
            hpText.text = health.ToString();
            if (health == maxHealth) {
                hpText.color = HexUnit.hpTextColor[0];
            }
            else if (health > Mathf.Max(maxHealth / 3, 2)) {
                hpText.color = HexUnit.hpTextColor[1];
            }
            else hpText.color = HexUnit.hpTextColor[2];
        }
    }
    /// <summary>
    ///  固有属性，可能会在战斗中临时改变
    /// </summary>
    // 花费
    int cost;
    public int Cost {
        get { return cost; }
        set {
            cost = value;
            infoText[1].text = string.Format("cost: {0}", cost);
        }
    }
    // 攻击
    int atk;
    public int Atk {
        get { return atk; }
        set {
            atk = value;
            infoText[2].text = string.Format("attack: {0}", atk);
            if (HexMetrics.unitProperty[type][level - 1][1] < atk) infoText[2].color = HexMetrics.unitInfoColor[2];
            else if (HexMetrics.unitProperty[type][level - 1][1] > atk) infoText[2].color = HexMetrics.unitInfoColor[3];
            else infoText[2].color = HexMetrics.unitInfoColor[camp];
        }
    }
    // 最大生命值
    int maxHealth;
    public int MaxHealth {
        get { return maxHealth; }
        set {
            maxHealth = value;
            infoText[3].text = string.Format("maxHp: {0}", maxHealth);
            if (HexMetrics.unitProperty[type][level - 1][2] < maxHealth) infoText[3].color = HexMetrics.unitInfoColor[2];
            else if (HexMetrics.unitProperty[type][level - 1][2] > maxHealth) infoText[3].color = HexMetrics.unitInfoColor[3];
            else infoText[3].color = HexMetrics.unitInfoColor[camp];
        }
    }
    // 攻击最小范围
    int minAtkRan;
    public int MinAtkRan {
        get { return minAtkRan; }
        set {
            minAtkRan = value;
            infoText[4].text = string.Format("attack range: {0}-{1}", minAtkRan, maxAtkRan);
        }
    }
    // 攻击最大范围
    int maxAtkRan;
    public int MaxAtkRan {
        get { return maxAtkRan; }
        set {
            maxAtkRan = value;
            infoText[4].text = string.Format("attack range: {0}-{1}", minAtkRan, maxAtkRan);
        }
    }
    // 行动速度
    int speed;
    public int Speed {
        get { return speed; }
        set {
            speed = value;
            infoText[5].text = string.Format("moving range: {0}", speed);
        }
    }
    // 冷却时间
    int cd;
    public int CD {
        get { return cd; }
        set {
            cd = value;
            infoText[6].text = string.Format("cd: {0}", cd);
        }
    }

    bool airCombat;
    public bool AirCombat {
        get { return airCombat; }
        set { airCombat = value; }
    }

    public bool canAct { get; set; }
    public bool isMoving { get; set; }
    public bool isAttacking { get; set; }
    public bool hasCmdInQueue { get; set; }

    public void Initialize(bool isFastForward)
    {
        cost = HexMetrics.unitProperty[type][level - 1][0];
        atk = HexMetrics.unitProperty[type][level - 1][1];
        maxHealth = HexMetrics.unitProperty[type][level - 1][2];
        minAtkRan = HexMetrics.unitProperty[type][level - 1][3];
        maxAtkRan = HexMetrics.unitProperty[type][level - 1][4];
        speed = HexMetrics.unitProperty[type][level - 1][5];
        cd = HexMetrics.unitProperty[type][level - 1][6];
        if (type != 0) {
            infoText[0].text = string.Format("{0} (level {1}, id {2})", HexMetrics.unitNameZH_CN[Type], Level, Id);
            infoText[1].text = string.Format("cost: {0}", cost);
            infoText[2].text = string.Format("attack: {0}", atk);
            infoText[3].text = string.Format("maxHp: {0}", maxHealth);
            infoText[4].text = string.Format("attack range: {0}-{1}", minAtkRan, maxAtkRan);
            infoText[5].text = string.Format("moving range: {0}", speed);
            infoText[6].text = string.Format("cd: {0}", cd);
            infoText[7].text = string.Format("{0}", HexMetrics.unitEntry[Type][Level - 1]); // 词条描述
            infoText[8].text = HexMetrics.unitDiscription[Type][Level - 1];
            infoPanel.gameObject.SetActive(false);

            // 血条的名称信息
            Text hpbarTitle = hpBar.Find("Background (1)/Text").GetComponent<Text>();
            hpbarTitle.text = string.Format("{0} ({1})", HexMetrics.unitNameZH_CN[Type], Id);
            hpbarTitle.color = HexMetrics.unitInfoColor[camp];
        }
        else {
            Text hpbarTitle = hpBar.Find("Background (1)/Text").GetComponent<Text>();
            hpbarTitle.text = string.Format("{0}", HexMetrics.campNameZH_CN[Camp]);
            hpbarTitle.color = HexMetrics.unitInfoColor[camp];
        }
        Health = MaxHealth;
        hasOverlap = false;
        infoPanelActive = true;
        canAct = false;
        isMoving = false;
        isAttacking = false;
        hasCmdInQueue = false;

        if (type >= 1) {
            anime = GetComponent<Animator>();
            if (!isFastForward) anime.Play("Load");
            else anime.Play("Idle");
        }
        else anime = null;

        if (type == 3 || type == 13) { // 蝙蝠
            isFlying = true;
            isBat = true;
            isDragon = false;
            airCombat = false;
        }
        else if (type == 5 || type == 15 || type == 6 || type == 16) { // 龙
            isFlying = false;
            isBat = false;
            isDragon = true;
            if (type == 5 || type == 15) airCombat = false;
            else airCombat = true;

            Tongue = transform.Find("CG/Pelvis/Spine/Spine1/Spine2/Neck/Neck1/Neck2/Neck3/Neck4/Head/Jaw/Tongue/Tongue02/Tongue03");
            splashCells = new List<HexCell>();
        }
        else {
            isFlying = false;
            isDragon = false;
            isBat = false;
            if (type == 2 || type == 12) { // 弓箭手
                archerHand = transform.Find("Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand");
                airCombat = true;
            }
            else if (type == 4 || type == 14) { // 牧师
                priestEff02 = Instantiate(PriestAtfPrefab[level - 1]);
                priestEff02.position = transform.position;
                healCommands = new List<Command>();
                airCombat = true;
            }
            else if (type == 7 || type == 17) { // 地狱火
                rockHandL = transform.Find("Root_M/body/chest/shoulder_L/arm_1_L/arm_2_L/hand_L/finger_L");
                rockHandR = transform.Find("Root_M/body/chest/shoulder_R/arm_1_R/arm_2_R/hand_R/finger_R");
                airCombat = true;
            }
            else { // 剑士
                airCombat = false;
            }
        }
    }

    private void OnEnable()
    {
        if (location) {
            transform.localPosition = location.Position;
        }
    }

    bool overlap;
    public bool hasOverlap { 
        get {
            return overlap;
        }
        set {
            if (infoPanel && overlap != value) {
                float offset = value ? 1f : 0f;
                infoPanel.GetComponent<RectTransform>().pivot = new Vector2(offset, 1f);
            }
            overlap = value;
        } 
    }
    public bool infoPanelActive {
        get; set;
    }
    private void Update()
    {
        if (hpBar)
            hpBar.position = Camera.main.WorldToScreenPoint(transform.localPosition + Vector3.up * 15f);
        if (infoPanel)
            infoPanel.position = Camera.main.WorldToScreenPoint(transform.localPosition);

        if (recoverEff) recoverEff.localPosition = transform.localPosition;
        if (strengthEff) strengthEff.localPosition = transform.localPosition;
        if (debuffEff) debuffEff.localPosition = transform.localPosition;
        for (int p = 0; p < buffEffList.Count; p++) {
            if (buffEffList[p]) {
                buffEffList[p].localPosition = transform.localPosition;
            }
        }

        if (firePreEff) { // 喷火前特效
            firePreEff.position = Tongue.position;
            firePreEff.rotation = Quaternion.Euler(0f, orientation, 0f);
        }
        if (fireBallEff) {
            fireBallEff.rotation = Quaternion.LookRotation(rigid.velocity);
        }
        if (fireBreathEff) { 
            fireBreathEff.position = Tongue.position;
            fireBreathEff.rotation = Quaternion.Euler(0f, orientation, 0f);
        }
        if (arrow) {
            if (!HasShot) {
                arrow.position = archerHand.position;
                Vector3 dir = 3 * archerHand.forward + 4 * archerHand.up + archerHand.right;
                arrow.rotation = Quaternion.LookRotation(dir);
            }
            else {
                arrow.rotation = Quaternion.LookRotation(rigid.velocity);
                shootTime += Time.deltaTime;
                if (!HasHit && shootTime > duration) {
                    HasHit = true;
                    rigid.velocity = new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value) * 10f;
                }
            }
        }
        if (priestEff01) priestEff01.localPosition = transform.localPosition;
        if (priestEff02) priestEff02.localPosition = transform.localPosition;
        if (icePreEff) { // 喷冰前特效
            icePreEff.position = Tongue.position;
        }
    }

    HexCell location; // 所在位置
    public HexCell Location {
        get { return location; }
        set {
            dealPosition(value);
            ValidatePosition();
        }
    }

    void dealPosition(HexCell destiCell)
    {
        if (location) { // 离开时，把原先cell.Unit/FlyingUnit置零
            if (IsFlying) {
                location.FlyingUnit = null;
                if (location.Unit) {
                    hasOverlap = false;
                    location.Unit.hasOverlap = false;
                }
            }
            else {
                location.Unit = null;
                if (location.FlyingUnit) {
                    hasOverlap = false;
                    location.FlyingUnit.hasOverlap = false;
                }
            }
        }
        location = destiCell; // 改变location
        if (IsFlying) {
            location.FlyingUnit = this;
            if (location.Unit) {
                hasOverlap = true; // 另一个的hasOverlap不变！
            }
        }
        else {
            location.Unit = this;
            if (location.FlyingUnit) {
                hasOverlap = true;
            }
        }
    }

    float orientation; // y轴旋转角
    public float Orientation {
        get { return orientation; }
        set {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    const float rotationSpeed = 360f;

    // ActionType: 0 是移动，1 是攻击，2 是反击
    IEnumerator LookAt(Vector3 point, int ActionType)
    {
        if ((isDragon) && ActionType == 0) anime.SetBool("Move", true);
        else anime.SetBool("Turn", true);

        point.y = transform.localPosition.y;
        Quaternion fromRotation = transform.localRotation;
        Quaternion toRotation = Quaternion.LookRotation(point - transform.localPosition); // 给一个朝向，返回对应Rotation四元数
        float angle = Quaternion.Angle(fromRotation, toRotation); // 首尾Rotation四元数表示方向的差角
        if (angle > 0f) {
            float speed = rotationSpeed / angle;
            for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed) {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t); // 球面插值
                yield return null;
            }
        }

        transform.LookAt(point); // 这可以改变朝向
        orientation = transform.localRotation.eulerAngles.y; // 更新朝向
        
        anime.SetBool("Turn", false);

        if (ActionType == 0) {
            if (!isFlying) { // 飞行生物不停留
                yield return new WaitForSeconds(0.2f);
            }
            anime.SetBool("Move", true);
        }
        else {
            if (ActionType == 1) anime.SetBool("Attack", true);
            else if (ActionType == 2) anime.SetBool("PostAttack", true);
            if (type == 5 || type == 15) { // 火龙要喷火
                firePreEff = Instantiate(FirePrepareEffectPrefab, Tongue.position, Quaternion.Euler(0f, orientation, 0f));
                Destroy(firePreEff.gameObject, 5f);
            }
            else if (type == 6 || type == 16) { // 冰龙要喷冰
                icePreEff = Instantiate(IcePrepareEffectPrefab, Tongue.position, Quaternion.Euler(0f, orientation, 0f));
                Destroy(icePreEff.gameObject, 5f);
            }
            else if (type == 2 || type == 12) { // 弓箭手要射箭
                arrow = Instantiate(ArrowPrefab, archerHand.position, 
                    Quaternion.LookRotation(3 * archerHand.forward + 4 * archerHand.up + archerHand.right));
                rigid = arrow.GetComponent<Rigidbody>();
                HasShot = false;
                HasHit = false;
            }
            else if (type == 4 || type == 14) {
                Transform priestPreAtkEff = Instantiate(PriestPreAtkPrefab, transform.position, Quaternion.Euler(0f, 0f, 0f));
                Destroy(priestPreAtkEff.gameObject, 5f);
            }
        }
    }

    public void ValidatePosition()
    {
        Debug.Log("ValidatePosition!");
        float deltaH = 0f;
        // 蝙蝠额外抬升12格
        if (isBat) deltaH += batOffset;

        // 如果是飞行生物 在 深渊 或 水面 上
        if (isFlying) {
            if (location.isAbyss) {
                if (!isBat) deltaH += WaterAbyssOffset;
                deltaH += location.ImHeight - location.Position.y;
            }
            else if (location.IsUnderwater) {
                if (!isBat) deltaH += WaterAbyssOffset;
                deltaH += location.WaterSurfaceY - location.Position.y;
            }
        }

        transform.localPosition = new Vector3(location.Position.x, location.Position.y + deltaH, location.Position.z);
        Orientation = orientation; // 更新回正确的朝向
    }

    public Vector3 ValidPos(HexCell cell)
    {
        float deltaH = 0f;
        // 蝙蝠额外抬升六格
        if (isBat) deltaH += batOffset;

        // 如果是飞行生物 在 深渊 或 水面 上
        if (isFlying) {
            if (cell.isAbyss) {
                if (!isBat) deltaH += WaterAbyssOffset;
                return new Vector3(cell.Position.x, cell.ImHeight + deltaH, cell.Position.z);
            }
            else if (cell.IsUnderwater) {
                if (!isBat) deltaH += WaterAbyssOffset;
                return new Vector3(cell.Position.x, cell.WaterSurfaceY + deltaH, cell.Position.z);
            }
        }
        return new Vector3(cell.Position.x, cell.Position.y + deltaH, cell.Position.z);
    }

    public void FastTravel(HexCell ToCell)
    {
        dealPosition(ToCell);
        transform.localPosition = ValidPos(ToCell);
    }

    List<HexCell> pathToTravel;
    public void Travel(List<HexCell> path)
    {
        // 实际位置还是瞬移
        if (path != null)
            dealPosition(path[path.Count - 1]);

        pathToTravel = path;
        StopAllCoroutines();
        if (pathToTravel != null) {
            // 开始移动后，不显示信息面板
            if (infoPanel) infoPanel.gameObject.SetActive(false);
            infoPanelActive = false;

            StartCoroutine(TravelPath());
        }
    }

    const float travelSpeed = 4f;
    IEnumerator TravelPath() // 贝塞尔太妙了。。
    {
        Vector3 a = transform.localPosition;
        Vector3 b = a, c = a;
        Vector3 prePoint;

        yield return LookAt(pathToTravel[1].Position, 0); // 先转向

        float t = Time.deltaTime * travelSpeed; // 立即出发，不在第一帧停留，这能防止算出的导数不存在（即d = 0）
        for (int i = 1; i < pathToTravel.Count; i++) {
            if (i != 1) {
                a = c;
                b = ValidPos(pathToTravel[i - 1]);
                c = (b + ValidPos(pathToTravel[i])) * 0.5f;
            }
            else {
                c = (ValidPos(pathToTravel[i - 1]) + ValidPos(pathToTravel[i])) * 0.5f;
            }
            prePoint = a;
            for (; t < 1f; t += Time.deltaTime * travelSpeed) {
                Vector3 point = Bezier.GetPoint(a, b, c, t);
                float deltaH = point.y - prePoint.y;
                prePoint = point;
                transform.localPosition = new Vector3(point.x, transform.localPosition.y + deltaH, point.z);

                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0; // 保持朝向是水平的！
                transform.localRotation = Quaternion.LookRotation(d); // LookRotation可以直接将方向转化为四元数

                yield return null; // 不定帧数？
            }
            t -= 1f; // 这样就保证了不会有时间损失了！太妙了！
        }

        a = c;
        b = ValidPos(pathToTravel[pathToTravel.Count - 1]);
        c = b;
        prePoint = a;
        for (; t < 1f; t += Time.deltaTime * travelSpeed) {
            Vector3 point = Bezier.GetPoint(a, b, c, t);
            float deltaH = point.y - prePoint.y;
            prePoint = point;
            transform.localPosition = new Vector3(point.x, transform.localPosition.y + deltaH, point.z);

            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0;
            transform.localRotation = Quaternion.LookRotation(d);

            yield return null;
        }

        // 确保完全到达终点位置、终点朝向正确
        //transform.localPosition = ValidPos(location);
        orientation = transform.localRotation.eulerAngles.y;

        // 归还list pool
        ListPool<HexCell>.Add(pathToTravel);
        pathToTravel = null;

        // 动画结束后的工作
        anime.SetBool("Move", false);
        infoPanelActive = true;
        isMoving = false;
        if (hasCmdInQueue) {
            hasCmdInQueue = false;
            RecallRunCommands();
        }
    }

    /// <summary>
    /// 攻击部分
    /// </summary>
    Animator anime;
    HexUnit ATKTargetUnit;

    public void Attack(HexUnit targetUnit, bool isPost)
    {
        ATKTargetUnit = targetUnit;

        isPostATK = isPost;
        if (isPost) {
            if (type == 5 || type == 15 || 
                type == 6 || type == 16 || 
                type == 7 || type == 17) {
                StartCoroutine(LookAt(targetUnit.Location.Position, 2));
            }
            else {
                StartCoroutine(LookAt(targetUnit.Location.Position, 1));
            }
        }
        else {
            StartCoroutine(LookAt(targetUnit.Location.Position, 1));
            if (type == 5 || type == 15) {
                calculateSplashCells();
            }
        }
    }

    void RecallRunCommands()
    {
        grid.RunCommands();
        if ((type == 5 || type == 15) && !isPostATK) {
            FireSplash();
        }
    }

    // 处理攻击点时造成的damage
    public void AttackPoint() // 动画事件回调
    {
        if (type == 5 || type == 15) { // 火龙要喷火
            if (!isPostATK) {
                fireBallEff = Instantiate(FireBallEffectPrefab);
                rigid = fireBallEff.GetComponent<Rigidbody>();
                duration = 1f;
                float g = Mathf.Abs(Physics.gravity.y);
                
                // 喷射火球
                Vector3 Distance = ATKTargetUnit.Location.Position - Tongue.position;
                Vector3 Vz = new Vector3(Distance.x, 0f, Distance.z) / duration; // 水平速度
                Vector3 Vy = Vector3.up * ((Distance.y + 5f) / duration + 0.5f * g * duration); // 竖直速度
                fireBallEff.position = Tongue.position;
                fireBallEff.rotation = Quaternion.LookRotation(Vz, Vy);

                rigid.velocity = Vz + Vy;
                Destroy(fireBallEff.gameObject, 5f);
            }
            else {
                fireBreathEff = Instantiate(FireBreathEffectPrefab);
                Destroy(fireBreathEff.gameObject, 5f);
            }
            Invoke("RecallRunCommands", 1f);
        }
        else if (type == 6 || type == 16) {
            duration = 1f;
            Vector3 core_pos0 = Tongue.position;
            core_pos0.y = grid.GetCell(Tongue.position).ImHeight;
            iceCoreEff = Instantiate(IceCoreEffectPrefab, core_pos0, Quaternion.Euler(0f, orientation, 0f));
            Vector3 target_pos = ATKTargetUnit.Location.Position;
            target_pos.y = ATKTargetUnit.Location.ImHeight;
            StartCoroutine(IceCore(core_pos0, target_pos));
            iceBurstEff = Instantiate(IceBurstEffectPrefab, target_pos, Quaternion.Euler(0f, orientation, 0f));
            iceSpikesEff = Instantiate(IceSpikesEffectPrefab, target_pos, Quaternion.Euler(0f, orientation, 0f));
            Destroy(iceCoreEff.gameObject, 5f);
            Destroy(iceBurstEff.gameObject, 5f);
            Destroy(iceSpikesEff.gameObject, 5f);

            Invoke("RecallRunCommands", 1f);
        }
        else if (type == 2 || type == 12){
            HasShot = true;
            float g = Mathf.Abs(Physics.gravity.y);
            duration = 0.1f;
            Vector3 dis = ATKTargetUnit.transform.position + Vector3.up * 7f - archerHand.position;
            dis -= transform.forward * 5f;
            Vector3 vz = new Vector3(dis.x, 0f, dis.z) / duration;
            Vector3 vy = Vector3.up * (dis.y / duration + 0.5f * g * duration);

            rigid.velocity = vz + vy;
            shootTime = 0f;
            Destroy(arrow.gameObject, 5f);

            // Duration 后才受击！
            Invoke("RecallRunCommands", duration);
        }
        else if (type == 7 || type == 17) {
            Vector3 burnPoint = (rockHandL.position + rockHandR.position) / 2f;
            if (!isPostATK) {
                GameObject burnEff = Instantiate(InfernoAtkEffectPrefab, burnPoint, Quaternion.Euler(0f, 0f, 0f));
                Destroy(burnEff, 5f);
            }
            else {
                GameObject burnEff = Instantiate(InfernoPostAtkEffectPrefab, burnPoint, Quaternion.Euler(0f, 0f, 0f));
                Destroy(burnEff, 5f);
            }
            RecallRunCommands();
        }
        else if (type == 4 || type == 14) {
            Vector3 pos = ATKTargetUnit.Location.Position;
            pos.y = ATKTargetUnit.Location.ImHeight;
            Transform priestAtkEff = Instantiate(PriestAtkPrefab, pos, Quaternion.Euler(0f, 0f, 0f));
            Destroy(priestAtkEff.gameObject, 5f);
            RecallRunCommands();
        }
        else {
            RecallRunCommands();
        }

        anime.SetBool("Attack", false);
        anime.SetBool("PostAttack", false);
        isAttacking = false;
        if (hasCmdInQueue) {
            hasCmdInQueue = false;
            RecallRunCommands();
        }
    }

    // 火龙溅射处理
    List<HexCell> splashCells = new List<HexCell>(); // 溅射cell
    void calculateSplashCells()
    {
        if (ATKTargetUnit.Type == 0) {
            return;
        }
        // 计算溅射cell
        HexCell targetCell = ATKTargetUnit.Location;
        location.Distance = 0;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            HexCell neighbor = location.GetNeighbor(d);
            neighbor.Distance = 1;
            for (HexDirection dd = HexDirection.NE; dd <= HexDirection.NW; dd++) {
                HexCell neighbor2 = neighbor.GetNeighbor(dd);
                if (neighbor2.Distance <= 1) continue;
                neighbor2.Distance = 2;
            }
        }
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            HexCell neighbor = targetCell.GetNeighbor(d);
            if (neighbor.Distance == 2 && neighbor.isInGame && !neighbor.isAbyss) splashCells.Add(neighbor);
        }
        Debug.Log(splashCells.Count);

        // 清空距离信息
        location.Distance = int.MaxValue;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            HexCell neighbor = location.GetNeighbor(d);
            neighbor.Distance = int.MaxValue;
            for (HexDirection dd = HexDirection.NE; dd <= HexDirection.NW; dd++) {
                neighbor.GetNeighbor(dd).Distance = int.MaxValue;
            }
        }
    }
    void FireSplash()
    {
        // 火球到达目的地，开始溅射
        int splashNum = splashCells.Count;
        float g = Mathf.Abs(Physics.gravity.y);
        Vector3 pos = ATKTargetUnit.Location.Position + Vector3.up * 5f;

        // 初始化每一个溅射特效的位置、角度、初速度
        for (int i = 0; i < splashNum; i++) {
            Transform splashEff = Instantiate(FireSplashEffectPrefab);
            Vector3 dis = splashCells[i].Position - pos;
            Vector3 vz = new Vector3(dis.x, 0f, dis.z) / duration;
            Vector3 vy = Vector3.up * (dis.y / duration + 0.5f * g * duration);
            splashEff.position = pos;
            splashEff.rotation = Quaternion.LookRotation(vz, vy);
            splashEff.GetComponent<Rigidbody>().velocity = vz + vy;
            Destroy(splashEff.gameObject, 10f);
        }

        // 清理 splashCells
        splashCells.Clear();
        if (splashCells.Count > 0) {
            Debug.LogError("fail to clear the splashCells, splashCells.Count = " + splashCells.Count);
            grid.newGameUI.socketClient.popMsgBox("fail to clear the splashCells, splashCells.Count = " + splashCells.Count, 1);
        }
    }

    // 冰龙特效
    IEnumerator IceCore(Vector3 pos0, Vector3 targetPos)
    {
        Vector3 Velocity = (targetPos - pos0) / duration;
        for (float t = 0f; t < duration; t += Time.deltaTime) {
            iceCoreEff.position = pos0 + Velocity * t;
            yield return null;
        }
    }

    // 牧师在回合结束时治疗
    List<Command> healCommands;
    bool startHeal = false;
    public void addHealCommands(Command C)
    {
        healCommands.Add(C);
    }
    public void HealOthers()
    {
        if (startHeal) return;
        startHeal = true;
        priestEff01 = Instantiate(PriestHealPrefab[level - 1], transform.position, Quaternion.Euler(0f, 0f, 0f));
        Destroy(priestEff01.gameObject, 10f);
        Invoke("targetUnitsGetHeal", 1f);
    }

    void targetUnitsGetHeal()
    {
        while (healCommands.Count > 0) {
            Command C = healCommands[0];
            Debug.Log(C);
            grid.msgDisplayer.display(C);
            if (C.type == CommandType.HEAL && C.arg[1] == Id) {
                HexUnit targetUnit = grid.getUnitById(C.arg[0]);
                targetUnit.Health = Mathf.Min(targetUnit.MaxHealth, targetUnit.Health + C.arg[2]);
                // 治疗特效
                GameObject RecoverEff = Instantiate(grid.RecoverEffectPrefab, targetUnit.transform.position, Quaternion.Euler(0f, 0f, 0f));
                targetUnit.recoverEff = RecoverEff.transform;
                Destroy(RecoverEff.gameObject, 10f);
            }
            else {
                Debug.LogError("wrong command type during priest heal!" + C.type.ToString());
            }
            healCommands.RemoveAt(0);
        }
        startHeal = false;
    }
    
    /// <summary>
    /// 受伤与死亡部分
    /// </summary>
    public void Hit()
    {
        if(type >= 1) {
            anime.SetBool("Hit", true, StopHit);
        }
        else { // 神迹动画特效？
            
        }
    }
    void StopHit()
    {
        anime.SetBool("Hit", false);
        // ValidatePosition();
    }

    [NonSerialized]
    public bool hasDead = false;
    public void TriggerDeath()
    {
        if (type >= 1) {
            hasDead = true;
            anime.SetTrigger("Death");
            if (IsFlying) {
                location.FlyingUnit = null;
                float offset = 0;
                if (isBat) offset += batOffset;
                if (location.isAbyss) {
                    offset += AbyssBottomOffset;
                }
                StartCoroutine(Drop(offset));
            }
            else location.Unit = null; // 及时将其所在cell清除，不然影响其他生物的位置计算
            ClearComponent();
            ClearBuffList();
            Destroy(gameObject, 10f);

            // 更新计分
            grid.newGameUI._Score[1 - camp] += level;
            grid.newGameUI.UpdateScoreText((1 - camp) == grid.newGameUI.myCamp);
        }
        else { // 神迹动画特效：不会真的销毁神迹
            Animation animation = location.specialFeature.gameObject.GetComponent<Animation>();
            animation.clip = animation.GetClip("death");
            animation.Play();
            hpBar.gameObject.SetActive(false);
        }
    }
    public void ClearComponent()
    {
        if (hpBar) Destroy(hpBar.gameObject);
        hpBar = null;
        if (infoPanel) Destroy(infoPanel.gameObject);
        infoPanel = null;
        if (priestEff02) Destroy(priestEff02.gameObject);
        priestEff02 = null;
    }
    
    IEnumerator Drop(float offset)// 向下掉
    {
        float v = 0f;
        float s = 0f;
        float g = Mathf.Abs(Physics.gravity.y);
        while (s < offset) {
            float deltaT = Time.deltaTime;
            v += g * deltaT;
            float deltaS = v * deltaT;
            transform.position -= Vector3.up * deltaS;
            s += deltaS;
            yield return null;
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)type);
        location.coordinates.Save(writer);
        writer.Write(orientation);
    }

    public static void Load(BinaryReader reader, HexGrid grid)
    {
        int type = reader.ReadByte();
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle(); // 读取单精浮点
        grid.AddUnit(type, 1, 2, true, grid.GetCell(coordinates), orientation); // 注意，有潜在危险，格式不再一样了
    }
}