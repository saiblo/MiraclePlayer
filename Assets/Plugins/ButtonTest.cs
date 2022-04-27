using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonTest : MonoBehaviour
{
	private Image image;
	
    // Start is called before the first frame update
    void Start()
    {
        image = this.GetComponent<Image> ();
		image.alphaHitTestMinimumThreshold = 0.1f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
