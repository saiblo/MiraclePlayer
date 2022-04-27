using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
//增加命名空间（关键）
public class Transfer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		
    }
	
	public void OnLoginButtonClick()
	{
		string filepath = GameObject.Find("customText123").GetComponent<InputField>().text;//选择的文件路径
        if (!File.Exists(filepath)) {
            Debug.LogError("File does not exist " + filepath);
            return;
        }
        else {
            Debug.Log(filepath);
        }
        /*Debug.Log(filepath);
		string text = System.IO.File.ReadAllText(@filepath);
		Debug.Log(text);*/
		PlayerPrefs.SetString("bool", "0");
        PlayerPrefs.SetString("path", filepath);
		//GameObject.Find("ExampleUI").SetActive(false);
		//GameObject.Find("HoloInterfaces").SetActive(false);
		SceneManager.LoadScene(1);
		//1是场景索引
	}
	
	public void OnlineMode()
	{
		string token = GameObject.Find("cT123").GetComponent<InputField>().text;
		PlayerPrefs.SetString("bool", "1");
        PlayerPrefs.SetString("token", token);
		Debug.Log(token);
		SceneManager.LoadScene(1);
	}
}
