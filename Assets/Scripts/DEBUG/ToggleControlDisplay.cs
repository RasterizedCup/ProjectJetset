using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleControlDisplay : MonoBehaviour
{
    [SerializeField]
    GameObject helpCanvas;
    [SerializeField]
    GameObject debugCanvas;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            helpCanvas.active = !helpCanvas.active;
        }
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            debugCanvas.active = !debugCanvas.active;
        }
    }
}
