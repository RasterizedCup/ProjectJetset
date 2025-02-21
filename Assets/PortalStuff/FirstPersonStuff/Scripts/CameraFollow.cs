using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    private Transform camera;

    // Update is called once per frame
    void Update()
    {
        transform.position = camera.position;
    }
}
