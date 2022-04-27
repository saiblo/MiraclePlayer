using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class NewText : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        string filepath = PlayerPrefs.GetString("path");//选择的文件路径
		Debug.Log(filepath);
		string text = System.IO.File.ReadAllText(@filepath);
		Debug.Log(text);
		GameObject.Find("Textzzz").GetComponent<Text>().text = text;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
