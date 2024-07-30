using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandleAccelObjGuidance : MonoBehaviour
{
    [SerializeField]
    private GameObject playerObj;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = new Vector3(0, 0, playerObj.transform.forward.z + 26);
    }
}
