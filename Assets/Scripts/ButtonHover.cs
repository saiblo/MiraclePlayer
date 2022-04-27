using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameUI gameUI;
    public int BtnIndex;
    public void OnPointerEnter(PointerEventData eventData)
    {
        gameUI.BtnSummonInfoPanel[transfer(BtnIndex)].gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gameUI.BtnSummonInfoPanel[transfer(BtnIndex)].gameObject.SetActive(false);
    }

    int transfer(int btnInx)
    {
        if (gameUI.myCamp == 0) {
            if (btnInx == 9) return 0;
            else if (btnInx == 10) return 10;
            else return btnInx + 1;
        }
        else {
            if (btnInx == 9) return 10;
            else if (btnInx == 10) return 0;
            else return btnInx + 11;
        }
    }
}
