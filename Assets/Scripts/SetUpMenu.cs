using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetUpMenu : MonoBehaviour
{
    public GameUI gameUI;
    public RectTransform unitContent, atfContent;
    public SetupItem ItemPrefab;

    public Button[] UnitDeck;
    public bool[] UnitSelected;
    public Image[] UnitImage;
    public int unitNum;

    public Button[] AtfDeck;
    public bool[] AtfSelected;
    public Image[] AtfImage;
    public int atfNum;

    int currentUnitType = 0; // <= 20
    int currentUnitLevel = 1;
    public ModelDisplayer displayer;
    public CameraUI cameraUI;

    public Button btnOK;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    // 初始化左边
    public void FillContent()
    {
        UnitDeck = new Button[gameUI.CreaturesNum];
        UnitSelected = new bool[gameUI.CreaturesNum];
        UnitImage = new Image[gameUI.CreaturesNum];
        unitNum = 0;

        AtfDeck = new Button[gameUI.ATFNum];
        AtfSelected = new bool[gameUI.ATFNum];
        AtfImage = new Image[gameUI.ATFNum];
        atfNum = 0;

        for (int i = unitContent.childCount - 1; i >= 0; i--) {
            Destroy(unitContent.GetChild(i).gameObject);
        }
        for (int i = atfContent.childCount - 1; i >= 0; i--) {
            Destroy(atfContent.GetChild(i).gameObject);
        }
        for (int i = 1; i <= gameUI.CreaturesNum; i++) {
            SetupItem item = Instantiate(ItemPrefab);
            item.menu = this;
            item.itemType = 1; // 1 是 Unit
            item.selectIndex = i;
            item.transform.SetParent(unitContent, false);

            UnitImage[i - 1] = item.transform.GetChild(0).GetComponent<Image>();
            UnitImage[i - 1].sprite = gameUI.ACSprite[gameUI.myCamp * 10 + i];
            UnitDeck[i - 1] = item.GetComponent<Button>();
            UnitSelected[i - 1] = false;
        }

        for (int i = 1; i <= gameUI.ATFNum; i++) {
            SetupItem item = Instantiate(ItemPrefab);
            item.menu = this;
            item.itemType = 0; // 0 是 Atf
            item.selectIndex = i;
            item.transform.SetParent(atfContent, false);

            AtfImage[i - 1] = item.transform.GetChild(0).GetComponent<Image>();
            AtfImage[i - 1].sprite = gameUI.ACSprite[20 + i];
            AtfDeck[i - 1] = item.GetComponent<Button>();
            AtfSelected[i - 1] = false;
        }

        // 初始化右边
        updateLevelPanelForUnit(0, 0);
        btnOK.interactable = false;
    }

    // 选择一项
    public void Select(int itemType, int selectIndex, bool selected)
    {
        if (itemType == 0) { // 0 是 神器
            AtfSelected[selectIndex - 1] = selected;
            ColorBlock CB = AtfDeck[selectIndex - 1].colors;
            if (selected) {
                CB.normalColor = Color.blue;
                AtfDeck[selectIndex - 1].colors = CB;
                atfNum += 1;
                if (atfNum >= 1)
                    for (int i = 0; i < AtfDeck.Length; i++)
                        if (!AtfSelected[i]) {
                            AtfDeck[i].interactable = false;
                            AtfImage[i].color = new Color(0.5f, 0.5f, 0.5f);
                        }
                updatePanelForAtf(selectIndex);
            }
            else {
                CB.normalColor = Color.black;
                AtfDeck[selectIndex - 1].colors = CB;
                atfNum -= 1;
                if (atfNum == 0)
                    for (int i = 0; i < AtfDeck.Length; i++)
                        if (!AtfSelected[i]) {
                            AtfDeck[i].interactable = true;
                            AtfImage[i].color = Color.white;
                        }
                updatePanelForAtf(0);
            }
        }
        if (itemType == 1) { // 1 是 Unit
            UnitSelected[selectIndex - 1] = selected;
            ColorBlock CB = UnitDeck[selectIndex - 1].colors;
            if (selected) {
                CB.normalColor = Color.blue;
                UnitDeck[selectIndex - 1].colors = CB;
                unitNum += 1;
                if (unitNum >= 3)
                    for (int i = 0; i < UnitDeck.Length; i++)
                        if (!UnitSelected[i]) {
                            UnitDeck[i].interactable = false;
                            UnitImage[i].color = new Color(0.5f, 0.5f, 0.5f);
                        }
                updateLevelPanelForUnit(10 * gameUI.myCamp + selectIndex, 1);
            }
            else {
                CB.normalColor = Color.black;
                UnitDeck[selectIndex - 1].colors = CB;
                unitNum -= 1;
                if (unitNum == 2)
                    for (int i = 0; i < UnitDeck.Length; i++)
                        if (!UnitSelected[i]) {
                            UnitDeck[i].interactable = true;
                            UnitImage[i].color = Color.white;
                        }
                updateLevelPanelForUnit(0, 0);
            }
        }
        if (selected && unitNum == 3 && atfNum == 1) btnOK.interactable = true;
        if (!selected) {
            if (unitNum == 3) btnOK.interactable = false;
            else if (atfNum == 1 && unitNum == 2) btnOK.interactable = false;
        }
    }

    public void finish()
    {
        if (unitNum != 3 || atfNum != 1) return;

        // 清理
        displayer.DisplayUnit(0, 0);
        displayer.enabled = false;
        displayer.gameObject.SetActive(false);
        cameraUI.enabled = false;
        enabled = false;
        gameObject.SetActive(false);

        // 唤醒场景、UI
        gameUI.AfterSelectDeck();
        HexMapCamera.ValidatePosition(gameUI.myCamp);
    }

    public Text[] infoText;
    void updateLevelPanelForUnit(int unitType, int level)
    {
        currentUnitType = unitType;
        currentUnitLevel = level;
        displayer.DisplayUnit(unitType, level);
        if (unitType == 0) {
            infoText[0].text = "Level ";
            infoText[1].text = "";
            infoText[2].text = "";
            infoText[3].text = "";
            infoText[4].text = "";
            infoText[5].text = "";
            infoText[6].text = "";
            infoText[7].text = "";
            infoText[8].text = "";
            infoText[9].text = "";
        }
        else {
            infoText[0].text = "Level " + level;
            infoText[1].text = string.Format("Name: {0}", HexMetrics.unitName[unitType]);
            infoText[2].text = string.Format("Cost: {0}", HexMetrics.unitProperty[unitType][level - 1][0]);
            infoText[3].text = string.Format("Attack: {0}", HexMetrics.unitProperty[unitType][level - 1][1]);
            infoText[4].text = string.Format("MaxHealth: {0}", HexMetrics.unitProperty[unitType][level - 1][2]);
            infoText[5].text = string.Format("Attack Range: {0}-{1}", HexMetrics.unitProperty[unitType][level - 1][3], HexMetrics.unitProperty[unitType][level - 1][4]);
            infoText[6].text = string.Format("Moving Range: {0}", HexMetrics.unitProperty[unitType][level - 1][5]);
            infoText[7].text = string.Format("CD: {0}", HexMetrics.unitProperty[unitType][level - 1][6]);
            infoText[8].text = string.Format("Max Summon Number: {0}", HexMetrics.unitProperty[unitType][level - 1][7]);
            infoText[9].text = HexMetrics.unitEntry[unitType][level - 1] + "\n<color=#FFFF00>" + HexMetrics.unitDiscription[unitType][level - 1] + "</color>";
        }
    }

    public void changeLevel(bool add)
    {
        if (currentUnitType == 0) return;
        if (add) {
            if (currentUnitLevel >= 3) return;
            currentUnitLevel += 1;
        }
        else {
            if (currentUnitLevel <= 1) return;
            currentUnitLevel -= 1;
        }
        updateLevelPanelForUnit(currentUnitType, currentUnitLevel);
    }

    void updatePanelForAtf(int atfType)
    {
        if (atfType == 0) {
            infoText[0].text = "Level ";
            infoText[1].text = "";
            infoText[2].text = "";
            infoText[3].text = "";
            infoText[4].text = "";
            infoText[5].text = "";
            infoText[6].text = "";
            infoText[7].text = "";
            infoText[8].text = "";
            infoText[9].text = "";
        }
        else {
            infoText[0].text = "Level ";
            infoText[1].text = string.Format("Name: {0}", HexMetrics.atfName[atfType]);
            infoText[2].text = string.Format("Cost: {0}", HexMetrics.atfProperty[atfType][0]);
            infoText[3].text = string.Format("CD: {0}", HexMetrics.atfProperty[atfType][1]);
            infoText[4].text = "";
            infoText[5].text = "";
            infoText[6].text = "";
            infoText[7].text = "";
            infoText[8].text = "";
            infoText[9].text = HexMetrics.atfEntry[atfType] + "\n<color=#FFFF00>" + HexMetrics.atfDiscription[atfType] + "</color>";
        }
    }
}
