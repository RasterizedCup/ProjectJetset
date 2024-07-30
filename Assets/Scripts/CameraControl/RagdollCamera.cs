using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollCamera : MonoBehaviour
{
    [SerializeField]
    GameObject NormalFollowObject; // player parent obj
    [SerializeField]
    GameObject NormalLookAtObject; // wallride PoF
    [SerializeField]
    GameObject RagdollFollowObject;
    [SerializeField]
    GameObject RagdollLookAtObject;
    [SerializeField]
    CinemachineFreeLook ThirdPersonCamera;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        CheckRagdollState();
    }
    
    void CheckRagdollState()
    {
        if (gameObject.GetComponent<RagdollPlayer>().isRagdoll())
        {
            ThirdPersonCamera.Follow = RagdollFollowObject.transform;
            ThirdPersonCamera.LookAt = RagdollLookAtObject.transform;
        }
        else
        {
            ThirdPersonCamera.Follow = NormalFollowObject.transform;
            ThirdPersonCamera.LookAt = NormalLookAtObject.transform;
        }
    }
}
