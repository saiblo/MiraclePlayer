using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class SaveLoadMenu : MonoBehaviour
{
    public HexGrid hexGrid;
    bool saveMode; // true 代表save
    public Text menuLabel, actionButtonLabel;
    public InputField nameInput;

    public RectTransform listContent;
    public SaveLoadItem itemPrefab;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Open(bool saveMode) // 弹出本canvas，并且根据saveMode改动抬头标签和ActionButton的字样
    {
        this.saveMode = saveMode;
        if (saveMode) {
            menuLabel.text = "Save Map";
            actionButtonLabel.text = "Save";
        }
        else {
            menuLabel.text = "Load Map";
            actionButtonLabel.text = "Load";
        }
        FillList();
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close() // 关闭本canvas
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    public void Action() // 按钮Save/Load触发
    {
        string path = GetSelectedPath();
        if (path == null) {
            return;
        }
        if (saveMode) {
            Save(path);
        }
        else {
            Load(path);
        }
        Close();
    }

    void Save(string path)
    {
        Debug.Log(Application.persistentDataPath);

        using (
            BinaryWriter writer =
                new BinaryWriter(File.Open(path, FileMode.Create))
        ) {
            writer.Write(2); // 保存地图的版本号。。。
            hexGrid.Save(writer);
        }
    }

    public void Load(string path)
    {
        if (!File.Exists(path)) {
            Debug.LogError("File does not exist " + path);
            return;
        }
        using (
            BinaryReader reader =
                new BinaryReader(File.OpenRead(path))
        ) {
            int header = reader.ReadInt32();
            if (header <= 2) {
                hexGrid.Load(reader, header);
            }
            else Debug.LogWarning("Unknown map format" + header);
        }
    }

    public void Delete()
    {
        string path = GetSelectedPath();
        if (path == null) return;
        if (File.Exists(path)) // 如果文件存在
            File.Delete(path); // 系统将文件删除

        nameInput.text = "";
        FillList();
    }

    string GetSelectedPath()
    {
        string mapName = nameInput.text;
        if(mapName.Length == 0) {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    void FillList()
    {
        for(int i = 0; i<listContent.childCount; i++) {
            Destroy(listContent.GetChild(i).gameObject);
        }

        string[] paths =
            Directory.GetFiles(Application.persistentDataPath, "*.map");
        Array.Sort(paths);
        for (int i = 0; i < paths.Length; i++) {
            SaveLoadItem item = Instantiate(itemPrefab);
            item.menu = this; // 注意这里，运行时就将item的menu设为是自己！秒啊
            item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
            item.transform.SetParent(listContent, false);
        }
    }

    public void SelectItem(string name)
    {
        nameInput.text = name;
    }
}
