using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandleGraphicsRotation : MonoBehaviour
{
    [SerializeField]
    Transform camRotation;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlayerRotation();
    }

    void UpdatePlayerRotation()
    {
        transform.rotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x,
            camRotation.rotation.eulerAngles.y,
            transform.rotation.eulerAngles.x
            );
    }
}
