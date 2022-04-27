using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SetupItem : MonoBehaviour
{
    [NonSerialized]
    public SetUpMenu menu;
    [NonSerialized]
    public int itemType; // 0 是 ATF，1 是 Unit
    [NonSerialized]
    public int selectIndex;

    bool selected;
    public void Select()
    {
        selected = !selected;
        menu.Select(itemType, selectIndex, selected);
    }
}
