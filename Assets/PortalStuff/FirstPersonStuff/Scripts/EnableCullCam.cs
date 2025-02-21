using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableCullCam : MonoBehaviour
{
    private Camera cullCamera;
    // Start is called before the first frame update
    void Start()
    {
        // lmao???
        cullCamera = GetComponent<Camera>();
        cullCamera.enabled = true;
    }
}
