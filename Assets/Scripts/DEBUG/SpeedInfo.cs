using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpeedInfo : MonoBehaviour
{
    TMP_Text text;
    public ThirdPersonMovement moveReference;
    public PlayerMovementManager playerRef;
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!AttachToRail.isAttachedToRail && !RailDetect.isOnSmoothRail)
        {
            text.text = $"{playerRef.getSpeed()}";
        }
        else
        {
            text.text = $"{playerRef.getRailSpeed()}";
        }
    }
}
