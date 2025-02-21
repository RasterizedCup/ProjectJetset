using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForwardCalibrator : MonoBehaviour
{
    [SerializeField]
    PortalTraveller traveller;
    private void Start()
    {
       // traveller = GetComponent<PortalTraveller>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ForwardBox"))
        {
            traveller.setForwardorBackward(true);
        }
        else if (other.CompareTag("BackwardBox"))
        {
            traveller.setForwardorBackward(false);
        }
    }
}
