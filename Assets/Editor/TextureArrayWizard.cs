using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TextureArrayWizard : ScriptableWizard
{
    public Texture2D[] textures; // 默认的wizard GUI会显示public字段

    [MenuItem("Assets/Create/Texture Array")] // 这个属性可以让我们通过editor访问该wizard
    static void CreateWizard()
    {
        // 设置Wizard窗口的title和button name
        ScriptableWizard.DisplayWizard<TextureArrayWizard>("Create Texture Array", "Create");
    }

    private void OnWizardCreate() // 当Create button 按下时调用
    {
        if (textures.Length == 0) return;
        // panel name, default filename, the file extension, description
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Texture Array", "Texture Array", "asset", "Save Texture Array"
        );
        if (path.Length == 0) return;

        Texture2D t = textures[0];
        Texture2DArray textureArray = new Texture2DArray(
            t.width, t.height, textures.Length, t.format, t.mipmapCount > 1
        );
        textureArray.anisoLevel = t.anisoLevel;
        textureArray.filterMode = t.filterMode;
        textureArray.wrapMode = t.wrapMode;
        for(int i = 0; i < textures.Length; i++) {
            for(int m = 0; m < Mathf.Min(t.mipmapCount, 10); m++) {
                Graphics.CopyTexture(textures[i], 0, m, textureArray, i, m);
            }
        }

        AssetDatabase.CreateAsset(textureArray, path);
    }
}
