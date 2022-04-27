using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class start : MonoBehaviour
{
    private void Awake()
    {
		GameObject.Find("ExampleUI").SetActive(true);
		GameObject.Find("HoloInterfaces").SetActive(true);
		Debug.Log("————————————————没想到吧");
	}

    // Start is called before the first frame update
    void Start()
    {
		
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
