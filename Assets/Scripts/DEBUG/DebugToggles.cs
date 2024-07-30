using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugToggles : MonoBehaviour
{
    [SerializeField]
    GameObject auxSpawnLocation;
    Vector3 spawnPosition;
    Vector3 auxTeleportLocation;
    // Start is called before the first frame update
    void Start()
    {
        spawnPosition = GameObject.Find("ThirdPersonPlayer_physboned").transform.position;
        auxTeleportLocation = auxSpawnLocation.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateFramerate();
        CheckRespawn();
    }

    void UpdateFramerate()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Application.targetFrameRate = 60;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Application.targetFrameRate = 90;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Application.targetFrameRate = 120;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Application.targetFrameRate = 144;
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Application.targetFrameRate = 165;
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Application.targetFrameRate = 240;
        }
    }

    void CheckRespawn()
    {
        if(Input.GetKeyDown(KeyCode.X))
            GameObject.Find("ThirdPersonPlayer_physboned").transform.position = spawnPosition;
        if (Input.GetKeyDown(KeyCode.V))
            GameObject.Find("ThirdPersonPlayer_physboned").transform.position = auxTeleportLocation;
    }
}
