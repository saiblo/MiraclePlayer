using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class displayerHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CameraUI cameraUI;
    public void OnPointerEnter(PointerEventData eventData)
    {
        cameraUI.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        cameraUI.enabled = false;
    }
}
