using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;

public class FileControllor : MonoBehaviour
{
	public void OpenFile()
    {
		string filepath = GameObject.Find("customText123").GetComponent<Text>().text;//选择的文件路径
		Debug.Log(filepath);
		string text = System.IO.File.ReadAllText(@filepath);
		GameObject.Find("Text123").GetComponent<Text>().text = text;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update() //C:\Users\zzz\Desktop\PA.txt
    {
        
    }
}
