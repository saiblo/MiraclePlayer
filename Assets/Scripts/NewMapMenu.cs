using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMapMenu : MonoBehaviour
{
    public HexGrid hexGrid;
    private void Awake()
    {
        gameObject.SetActive(false);
    }
    public void Open()
    {
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }
    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    void CreateMap(int x, int z)
    {
        hexGrid.CreateMap(x, z);
        // 此处把HexMapCamera.ValidatePosition();移进CreateMap里面
        Close();
    }

    public void CreateSmallMap()
    {
        CreateMap(20, 15);
    }
    public void CreateMediumMap()
    {
        CreateMap(40, 30);
    }
    public void CreateLargeMap()
    {
        CreateMap(80, 60);
    }
    public void CreateStandardMap()
    {
        CreateMap(20, 35);
    }
}
