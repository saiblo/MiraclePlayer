using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;

public class HexMapEditor : MonoBehaviour
{
    public HexGrid hexGrid;
    public Material terrainMaterial;
    private void Awake()
    {
        terrainMaterial.EnableKeyword("GRID_ON");
        SetEditMode(false);
    }

    public Image leftPanel, rightPanel;

    private int activeTerrainTypeIndex;
    private int activeElevation;
    private int activeWaterLevel;
    private int brushSize;
    public int activeFeature01Level, activeFeature02Level, activeFeature03Level;
    public int activeSpecialIndex;
    bool applyTerrainTypeIndex = true;
    bool applyElevation = true;
    bool applyWaterLevel = false;
    bool isBlackWater = false;
    bool applyFeature01Level = false, applyFeature02Level = false, applyFeature03Level = false;
    bool applySpecialIndex;

    public Slider elevationSlider; // 秒啊，通过这种方式实时获得slider的value
    public Slider waterSlider;
    public Slider brushsizeSlider;
    public Slider feature01Slider, feature02Slider, feature03Slider;
    enum OptionalToggle
    {
        Ignore, Yes, No
    }
    OptionalToggle riverMode;

    bool isDrag;
    HexDirection dragDirection; // 拖动方向
    HexCell previousCell;

    public void SetTerrainTypeIndex(int index)
    {
        applyTerrainTypeIndex = index >= 0;
        if(applyTerrainTypeIndex)
            activeTerrainTypeIndex = index;
    }
    public void SetElevation()
    {
        float elevation = elevationSlider.GetComponent<Slider>().value;
        activeElevation = (int)elevation;
    }
    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }
    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }
    public void SetApplyWaterLevel(bool toggle)
    {
        applyWaterLevel = toggle;
    }
    public void SetWaterLevel()
    {
        float level = waterSlider.GetComponent<Slider>().value;
        activeWaterLevel = (int)level;
    }
    public void SetBlackWater(bool toggle)
    {
        isBlackWater = toggle;
    }
    public void SetBrushSize()
    {
        float size = brushsizeSlider.GetComponent<Slider>().value;
        brushSize = (int)size;
    }
    public void setApplyFeature01Level(bool toggle)
    {
        applyFeature01Level = toggle;
    }
    public void SetFeature01Level()
    {
        float level = feature01Slider.GetComponent<Slider>().value;
        activeFeature01Level = (int)level;
    }
    public void setApplyFeature02Level(bool toggle)
    {
        applyFeature02Level = toggle;
    }
    public void SetFeature02Level()
    {
        float level = feature02Slider.GetComponent<Slider>().value;
        activeFeature02Level = (int)level;
    }
    public void setApplyFeature03Level(bool toggle)
    {
        applyFeature03Level = toggle;
    }
    public void SetFeature03Level()
    {
        float level = feature03Slider.GetComponent<Slider>().value;
        activeFeature03Level = (int)level;
    }
    public void SelectSpecialIndex(int index)
    {
        applySpecialIndex = index >= 0;
        if (applySpecialIndex)
            activeSpecialIndex = index;
    }

    public void SetEditMode(bool toggle)
    {
        enabled = toggle;
        leftPanel.gameObject.SetActive(toggle);
        rightPanel.gameObject.SetActive(toggle);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) // 每一帧鼠标左键处于按下状态都会返回true
        { //点的不是UI才handleInput
            HandleInput();
        }
        else { // 点的是UI
            previousCell = null;
        }
        if (!EventSystem.current.IsPointerOverGameObject()) {
            if (Input.GetMouseButton(0)) {
                HandleInput();
                return;
            }
            if (Input.GetKeyDown(KeyCode.U)) {
                if (Input.GetKey(KeyCode.LeftShift)) {
                    DestroyUnit();
                }
                else {
                    CreateUnit();
                }
                return;
            }
        }
        previousCell = null;
    }
    void HandleInput()
    {
        HexCell currentCell = GetCellUnderCursor();
        if (currentCell) // 如果鼠标发射的射线能接触到某一点，输出为hit
        {
            if (previousCell && previousCell != currentCell) {
                ValidateDrag(currentCell);
            }
            else { // 大多数情况都会在这里，因为人的拖拽很慢，总是停留在一个cell内
                isDrag = false;
            }
            EditCells(currentCell);
            previousCell = currentCell;
        }
        else {
            previousCell = null;
        }
    }

    void ValidateDrag(HexCell currentCell) // 拖拽抖动？
    {
        for(dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++) {
            if(previousCell.GetNeighbor(dragDirection) == currentCell) {
                isDrag = true;
                return;
            }
        }
        Debug.Log("here");
        isDrag = false;
    }

    HexCell GetCellUnderCursor()
    {
        return
            hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
    }

    void EditCells(HexCell center)
    {
        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;

        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++){
            for(int x = centerX - r; x<=centerX + brushSize; x++){
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++){
            for (int x = centerX - brushSize; x <= centerX + r; x++){
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    void EditCell(HexCell cell)
    {
        if (!cell) return;
        if (applyTerrainTypeIndex)
            cell.TerrainTypeIndex = activeTerrainTypeIndex;
        if (applyElevation)
            cell.Elevation = activeElevation;
        if (applyWaterLevel) {
            cell.WaterLevel = activeWaterLevel;
            cell.AbnormalWater = isBlackWater;
        }
        if (applyFeature01Level)
            cell.Feature01Level = activeFeature01Level;
        if (applyFeature02Level)
            cell.Feature02Level = activeFeature02Level;
        if (applyFeature03Level)
            cell.Feature03Level = activeFeature03Level;
        if (applySpecialIndex)
            cell.SpecialIndex = activeSpecialIndex;

        if (riverMode == OptionalToggle.No) {
            cell.RemoveRiver();
        }
        else if(isDrag && riverMode == OptionalToggle.Yes) {
            HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
            if (otherCell) { // 在BrushSize不为0的情况下，otherCell可能为null
                otherCell.SetoutgoingRiver(dragDirection);
            }
        }
    }

    void CreateUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && !cell.Unit) {
            hexGrid.AddUnit(1, 1, 2, false, cell, Random.Range(0f, 360f));
        }
    }
    void DestroyUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && cell.Unit) {
            hexGrid.RemoveUnit(cell.Unit);
            Destroy(cell.Unit.gameObject);
        }
    }
}
