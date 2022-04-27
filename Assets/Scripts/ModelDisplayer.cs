using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ModelDisplayer : MonoBehaviour
{
    public HexGrid grid;
    [NonSerialized]
    public GameObject UnitObj = null;
    public void DisplayUnit(int unitType, int level)
    {
        if (unitType == 0) {
            if (UnitObj) Destroy(UnitObj);
            UnitObj = null;
            return;
        }
        if (UnitObj) Destroy(UnitObj);
        UnitObj = null;
        // 注意了，幸好HexUnit没有Awake和Start函数！
        if (level == 1 || level == 2) {
            UnitObj = Instantiate(grid.unitPrefabs1[unitType]).gameObject;
        }
        else if (level == 3) {
            UnitObj = Instantiate(grid.unitPrefabs3[unitType]).gameObject;
        }
        UnitObj.transform.position = transform.position;
    }

    private void Update()
    {
        float delta = Input.GetAxis("Horizontal") * 5f; // 通过A D 键控制
        if (delta != 0) {
            AdjustRotation(delta);
        }
    }

    void AdjustRotation(float delta)
    {
        if (UnitObj) {
            UnitObj.transform.Rotate(0f, delta, 0f);
        }
    }
}
