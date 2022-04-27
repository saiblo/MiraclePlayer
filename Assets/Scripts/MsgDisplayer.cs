using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MsgDisplayer : MonoBehaviour
{
    public Transform MsgPrefab;
    public ScrollRect scrollRect;
    public Transform content;
    public HexGrid grid;
    // Start is called before the first frame update
    void Start()
    {
        enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Show()
    {
        if (gameObject.activeSelf) {
            gameObject.SetActive(false);
        }
        else {
            gameObject.SetActive(true);
        }
    }

    public Color[] textPanelColor = new Color[] {
        new Color(0.6f, 1f, 1f, 0.6f), // 默认
        new Color(1f, 0.6f, 0.9f, 0.6f), // 召唤
        new Color(1f, 0.2f, 0.2f, 0.7f), // 攻击
        new Color(0.6f, 0f, 0f, 0.5f), // 伤害
        new Color(0.8f, 0.8f, 0.8f, 0.4f), // 死亡
        new Color(1f, 0.8f, 0f, 0.6f), // 神器
        new Color(0f, 1f, 0f, 0.6f), // 治疗
        new Color(1f, 0.4f, 0f, 0.6f), // BUFF
        new Color(0f, 0f, 1f, 0.6f), // DEBUFF
    };

    public void Clear()
    {
        for (int i = content.childCount - 1; i >= 1; i--) {
            Destroy(content.GetChild(i).gameObject);
        }
    }

    public void display(Command C)
    {
        string msg = "";
        int panelColorIndex = 0;
        if (C.type == CommandType.ROUNDSTARTED) {
            string campStr = "";
            if (C.arg[0] == 0) campStr = "蓝";
            else if (C.arg[0] == 1) campStr = "红";
            msg += string.Format("回合{0}开始，{1}方行动", C.round + 1, campStr);
            panelColorIndex = 0;
        }
        else if (C.type == CommandType.ROUNDOVER) {
            string campStr = "";
            if (C.arg[0] == 0) campStr = "蓝";
            else if (C.arg[0] == 1) campStr = "红";
            msg += string.Format("{0}方结束回合", campStr);
            panelColorIndex = 0;
        }
        else if (C.type == CommandType.SUMMON) {
            int type = C.arg[0];
            string campStr = type < 10 ? "蓝" : "红";
            string levelStr = "";
            if (C.arg[1] == 1) levelStr = "一";
            else if (C.arg[1] == 2) levelStr = "二";
            else if (C.arg[1] == 3) levelStr = "三";
            string typeStr = HexMetrics.unitNameZH_CN[C.arg[0]];
            msg += string.Format("{0}方召唤了{1}星{2}，id为{3}", campStr, levelStr, typeStr, C.arg[4]);
            panelColorIndex = 1;
        }
        else if (C.type == CommandType.MOVE) {
            HexUnit u = grid.getUnitById(C.arg[0]);
            string campStr = u.Camp == 0 ? "蓝" : "红";
            string typeStr = HexMetrics.unitNameZH_CN[u.Type];
            msg += string.Format("{0}方{1}({2})移动", campStr, typeStr, C.arg[0]);
            panelColorIndex = 0;
        }
        else if (C.type == CommandType.PREATK) {
            HexUnit unit1 = grid.getUnitById(C.arg[0]);
            HexUnit unit2 = grid.getUnitById(C.arg[1]);
            string campStr1 = unit1.Camp == 0 ? "蓝" : "红";
            string campStr2 = unit2.Camp == 0 ? "蓝" : "红";
            msg += string.Format("{0}方{1}({2})对{3}方{4}({5})发起攻击",
                campStr1,
                HexMetrics.unitNameZH_CN[unit1.Type],
                unit1.Id,
                campStr2,
                HexMetrics.unitNameZH_CN[unit2.Type],
                unit2.Id
            );
            panelColorIndex = 2;
        }
        else if (C.type == CommandType.HURT) {
            HexUnit unit2 = grid.getUnitById(C.arg[0]);
            HexUnit unit1 = grid.getUnitById(C.arg[1]);
            string campStr1 = unit1.Camp == 0 ? "蓝" : "红";
            string campStr2 = unit2.Camp == 0 ? "蓝" : "红";
            msg += string.Format("{0}方{1}({2})受到了来自{3}方{4}({5})的{6}点伤害",
                campStr2,
                HexMetrics.unitNameZH_CN[unit2.Type],
                unit2.Id,
                campStr1,
                HexMetrics.unitNameZH_CN[unit1.Type],
                unit1.Id,
                C.arg[2]
            );
            panelColorIndex = 3;
        }
        else if (C.type == CommandType.DIE) {
            HexUnit u = grid.getUnitById(C.arg[0]);
            string campStr = u.Camp == 0 ? "蓝" : "红";
            msg += string.Format("{0}方{1}({2})死亡",
                campStr, HexMetrics.unitNameZH_CN[u.Type], u.Id);
            panelColorIndex = 4;
        }
        else if (C.type == CommandType.HEAL) {
            HexUnit unit2 = grid.getUnitById(C.arg[0]);
            HexUnit unit1 = grid.getUnitById(C.arg[1]);
            string campStr1 = unit1.Camp == 0 ? "蓝" : "红";
            string campStr2 = unit2.Camp == 0 ? "蓝" : "红";
            if (unit1.Type != 0) {
                msg += string.Format("{0}方{1}({2})回复了来自{3}方{4}({5})的{6}点生命",
                campStr2,
                HexMetrics.unitNameZH_CN[unit2.Type],
                unit2.Id,
                campStr1,
                HexMetrics.unitNameZH_CN[unit1.Type],
                unit1.Id,
                Mathf.Min(C.arg[2], unit2.MaxHealth - unit2.Health));
            }
            else {
                msg += string.Format("{0}方{1}({2})回复了来自{3}的{4}点生命",
                campStr2,
                HexMetrics.unitNameZH_CN[unit2.Type],
                unit2.Id,
                HexMetrics.campNameZH_CN[unit1.Camp],
                Mathf.Min(C.arg[2], unit2.MaxHealth - unit2.Health));
            }
            panelColorIndex = 6;
        }
        else if (C.type == CommandType.ATF) {
            string MiracleStr = HexMetrics.campNameZH_CN[C.arg[0]];
            string ATFStr = HexMetrics.atfNameZH_CN[C.arg[1] % 10];
            msg += string.Format("{0}使用神器：{1}", MiracleStr, ATFStr);
            panelColorIndex = 5;
        }
        else if (C.type == CommandType.GAMEOVER) {
            string campStr = "";
            if (C.arg[0] == 0) campStr = "蓝";
            else if (C.arg[0] == 1) campStr = "红";
            msg += string.Format("游戏结束，{0}方胜利", campStr);
            panelColorIndex = 0;
        }
        else if (C.type == CommandType.BUFF) {
            HexUnit u = grid.getUnitById(C.arg[0]);
            if (C.arg[1] == 1) {
                msg += string.Format("{0}({1})获得来自牧师光环的攻击加成", HexMetrics.unitNameZH_CN[u.Type], u.Id);
            }
            else if (C.arg[1] == 2) {
                msg += string.Format("{0}({1})获得圣盾", HexMetrics.unitNameZH_CN[u.Type], u.Id);
            }
            else if (C.arg[1] == 3) {
                msg += string.Format("{0}({1})获得攻击+2（圣光之耀）", HexMetrics.unitNameZH_CN[u.Type], u.Id);
            }
            else if (C.arg[1] == 4) {
                msg += string.Format("{0}({1})获得最大生命+4（阳炎之盾）", HexMetrics.unitNameZH_CN[u.Type], u.Id);
            }
            else Debug.LogError("error: BUFF 命令种类：" + C.arg[1]);
            panelColorIndex = 7;
        }
        else if (C.type == CommandType.DEBUFF) {
            HexUnit u = grid.getUnitById(C.arg[0]);
            if (u) {
                if (C.arg[1] == 1) {
                    msg += string.Format("{0}({1})解除了来自牧师光环的攻击加成", HexMetrics.unitNameZH_CN[u.Type], u.Id);
                }
                else if (C.arg[1] == 2) {
                    msg += string.Format("{0}({1})解除圣盾", HexMetrics.unitNameZH_CN[u.Type], u.Id);
                }
                else if (C.arg[1] == 3) {
                    msg += string.Format("{0}({1})解除攻击+2加成", HexMetrics.unitNameZH_CN[u.Type], u.Id);
                }
                else if (C.arg[1] == 4) {
                    msg += string.Format("{0}({1})解除最大生命+4加成", HexMetrics.unitNameZH_CN[u.Type], u.Id);
                }
                else Debug.LogError("error: BUFF 命令种类：" + C.arg[1]);
                panelColorIndex = 8;
            }
        }
        else if (C.type == CommandType.POSTATK) {
            HexUnit unit1 = grid.getUnitById(C.arg[0]);
            HexUnit unit2 = grid.getUnitById(C.arg[1]);
            if (unit2.Type != 0) {
                string campStr1 = unit1.Camp == 0 ? "蓝" : "红";
                string campStr2 = unit2.Camp == 0 ? "蓝" : "红";
                msg += string.Format("{0}方{1}({2})尝试对{3}方{4}({5})发起反击",
                    campStr2,
                    HexMetrics.unitNameZH_CN[unit2.Type],
                    unit2.Id,
                    campStr1,
                    HexMetrics.unitNameZH_CN[unit1.Type],
                    unit1.Id,
                    C.arg[2]
                );
                panelColorIndex = 2;
            }
        }

        if (msg != "") {
            Transform item = Instantiate(MsgPrefab);
            item.SetParent(content, false);
            Text itemText = item.Find("Text").GetComponent<Text>();
            itemText.text = msg;
            Image itemImage = item.GetComponent<Image>();
            itemImage.color = textPanelColor[panelColorIndex];
        }
    }
}
