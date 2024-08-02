using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSlide : PlayerMovementEffector
{
    [SerializeField]
    float slideDuration;
    [SerializeField]
    float slidePower;
    [SerializeField]
    float slideVelocityChangeRate;

    bool slideInputted;
    bool isSliding;

    float currTime;

    float currSlideGraceFrame;
    float slideInputGraceFrames;
    // Start is called before the first frame update
    void Start()
    {
        isSliding = false;
        slideInputGraceFrames = 5;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void checkIfSlide()
    {
        if (Input.GetButtonDown("Slide")) // temp slide button
        {
            slideInputted = true;
        } // sometimes input is overlooked. allow a brief grace period to compensate
        else if (currSlideGraceFrame > slideInputGraceFrames && slideInputted)
        {
            currSlideGraceFrame = 0;
            slideInputted = false;
        }
        else if (slideInputted)
        {
            currSlideGraceFrame++;
        }
    }

    public void handleSlide(ref PlayerMovementContext moveContext)
    {
        // initial run
        if(!moveContext.isSliding && moveContext.isGrounded && slideInputted)
        {
            moveContext.isSliding = true;
            moveContext.currAccelChangeRate = slideVelocityChangeRate;
            moveContext.currAccelMatrix *= slidePower;

            currTime = Time.time + slideDuration;
        }
        if ((moveContext.isSliding && Time.time > currTime) || moveContext.isJumping)
        {
            moveContext.isSliding = false;
        }
    }
}
