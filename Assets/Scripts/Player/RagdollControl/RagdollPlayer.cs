using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollPlayer : MonoBehaviour
{
    [SerializeField]
    float ragdollDuration;
    [SerializeField]
    GameObject AnimatorObject;
    [SerializeField]
    GameObject ControllerObject;
    [SerializeField]
    GameObject RagdollTrackObject;
    [SerializeField]
    AudioClip impactSoundMetal;
    [SerializeField]
    AudioSource impactPlayer;

    private bool isInRagdoll;
    private float currTime;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
            ToggleRagdoll();

        // unset ragdoll after set time
    }

    void ToggleRagdoll()
    {
        bool wasEnabled = isInRagdoll;
        isInRagdoll = !isInRagdoll;
        if (!isInRagdoll && wasEnabled) // one time toggle
            DisableRagdoll();
        AnimatorObject.GetComponent<Animator>().enabled = !isInRagdoll;
        ControllerObject.GetComponent<CharacterController>().enabled = !isInRagdoll;
        // temp debug for disable 
    }

    public void EnableRagdoll()
    {
        isInRagdoll = true;
        AnimatorObject.GetComponent<Animator>().enabled = false;
        ControllerObject.GetComponent<CharacterController>().enabled = !isInRagdoll;

        // need to set momentum change vals for ragdoll to be constant
        // maybe 80? experiment
    }

    public void DisableRagdoll()
    {
        isInRagdoll = false;
        // move character controller to new local position of the gfx from ragdoll movement
        // get offset from hipjoint
        // before enabling animator
        // based on rotation. get another point of reference
        ControllerObject.transform.position = new Vector3 
            (ControllerObject.transform.position.x + RagdollTrackObject.transform.localPosition.y,
            ControllerObject.transform.position.y + RagdollTrackObject.transform.localPosition.z,
            ControllerObject.transform.position.z + RagdollTrackObject.transform.localPosition.x);
        AnimatorObject.GetComponent<Animator>().enabled = true;
        // ASSESS new state of character with this new position and update?

    }

    public bool isRagdoll()
    {
        return isInRagdoll;
        // get colliders working for ragdoll
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RagdollForce") && !isInRagdoll){
            EnableRagdoll();
           // impactPlayer.clip = impactSoundMetal;
            //impactPlayer.Play();
        }
    }
}
