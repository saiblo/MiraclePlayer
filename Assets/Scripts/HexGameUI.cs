using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour
{
    public HexGrid grid;
    HexCell previousCell, currentCell;
    HexUnit selectedUnit;
    int unitSpeed = 5;

    public void SetEditMode(bool toggle)
    {
        enabled = !toggle; // 取消enabled状态
        grid.ShowUI(!toggle);
        grid.ClearPath();
    }

    bool CPress = false;
    public int AddUnitType;
    private void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject()) { // 点的不是UI
            if (Input.GetMouseButtonDown(0)) { // 按下左键
                UpdateCurrentCell();
                if (!selectedUnit) { // 还没有选中的Unit
                    if (!currentCell) return; // 没有实质操作，直接退出

                    selectedUnit = currentCell.Unit; // 有可能为空
                    if (selectedUnit) { // 有选中的Unit了，注意如果Unit和Dacro重叠了，就只能先走Unit， 暂时不能出兵
                        ComputeDis(selectedUnit);
                    }
                    else if(
                        currentCell.SpecialIndex == 3 ||
                        currentCell.SpecialIndex == 4 ||
                        currentCell.SpecialIndex == 5
                        ) {
                        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) { // 在出兵点的邻居格子内出场
                            HexCell neighbor = currentCell.GetNeighbor(d);
                            if(neighbor.IsSpecial && neighbor.SpecialIndex <= 8) {
                                continue;
                            }
                            if (neighbor.Unit) continue;
                            
                            grid.AddUnit(AddUnitType, 1, 2, false, neighbor, Random.Range(0f, 360f));
                            break;
                        }
                    }
                }
                else { // 有选中的Unit
                    if (currentCell) {
                        if (currentCell.Unit && selectedUnit != currentCell.Unit) { // 改选Unit
                            selectedUnit = currentCell.Unit;
                            ComputeDis(selectedUnit);
                        }
                        else if (currentCell.reachable) { // 让Unit移动
                            selectedUnit.Travel(grid.GetPath(currentCell));
                            grid.ClearPath();
                            selectedUnit = null;
                        }
                        else { // 撤选Unit
                            grid.ClearPath();
                            selectedUnit = null;
                        }
                    }
                    else {
                        grid.ClearPath();
                        selectedUnit = null;
                    }
                }
            }
            else {
                if (selectedUnit) {
                    currentCell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
                    if (currentCell == previousCell) return;

                    if (currentCell && currentCell.reachable)
                        currentCell.ChangeHighlight(Color.red);
                    if (previousCell && previousCell.reachable)
                        previousCell.ChangeHighlight(Color.white);
                    previousCell = currentCell;
                }
                else {
                    if (!CPress && Input.GetKeyDown(KeyCode.C)) {
                        CPress = true;
                        grid.setUItoCoord();
                    }
                    else if (CPress && Input.GetKeyUp(KeyCode.C)) {
                        CPress = false;
                        grid.ClearUI();
                    }

                    if (Input.GetKeyDown(KeyCode.Alpha1)) HandleUnit(1);
                    else if (Input.GetKeyDown(KeyCode.Alpha2)) HandleUnit(2);
                    else if (Input.GetKeyDown(KeyCode.Alpha3)) HandleUnit(3);
                    else if (Input.GetKeyDown(KeyCode.Alpha4)) HandleUnit(4);
                    else if (Input.GetKeyDown(KeyCode.Alpha5)) HandleUnit(5);
                    else if (Input.GetKeyDown(KeyCode.Alpha6)) HandleUnit(6);
                    else if (Input.GetKeyDown(KeyCode.Alpha7)) HandleUnit(7);
                    else if (Input.GetKeyDown(KeyCode.Alpha8)) HandleUnit(8);
                    else if (Input.GetKeyDown(KeyCode.Alpha9)) HandleUnit(9);
                    else if (Input.GetKeyDown(KeyCode.Alpha0)) HandleUnit(10);
                }
            }
        }
    }
    void UpdateCurrentCell() // 通过鼠标点击更新currentCell，点到外面时就更新为空
    {
        HexCell cell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        if(cell != currentCell) {
            currentCell = cell;
        }
    }

    void ComputeDis(HexUnit unit)
    {
        grid.computeDis(currentCell, 0, unit.IsFlying, true);
    }

    void HandleUnit(int unitIndex)
    {
        if (Input.GetKey(KeyCode.LeftShift)) {
            Debug.Log("death!");
            grid.DeathUnit(unitIndex);
        }
        else if (Input.GetKey(KeyCode.RightShift)) {
            Debug.Log("Hit!");
            grid.HitUnit(unitIndex);
        }
        else {
            Debug.Log("Attack!");
            // grid.AttackUnit(unitIndex);
        }
    }
}
