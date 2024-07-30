using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TrackStateInfo : MonoBehaviour
{
    [SerializeField]
    PlayerMovementManager playerMovementManager;

    // track all player states, update them accordingly
    [SerializeField] TMP_Text isInitialMountText;    
    [SerializeField] TMP_Text isJumpingText;
    [SerializeField] TMP_Text isDoubleJumpingText;
    [SerializeField] TMP_Text isFallingText;
    [SerializeField] TMP_Text isSlidingText;
    [SerializeField] TMP_Text isWallRunningText;
    [SerializeField] TMP_Text isGroundedText;
    [SerializeField] TMP_Text isDashingText;
    [SerializeField] TMP_Text isAimingText;
    
    // wall transition specifics
    [SerializeField] TMP_Text isAttachedToWallText;
    [SerializeField] TMP_Text isInitAttachText;
    [SerializeField] TMP_Text isNewDetachText;
    [SerializeField] TMP_Text isInTransitionText;

    // smooth rail transition specifics
    [SerializeField] TMP_Text isInitialMountsmoothRailText;
    [SerializeField] TMP_Text isOnSmoothRailText;
    [SerializeField] TMP_Text dismountJumpVelocityTransitionText;
    [SerializeField] TMP_Text dismountVelocityTransitionText;
    [SerializeField] TMP_Text handleCamYOffsetText;

    // straight rail transition specifics
    [SerializeField] TMP_Text isMountPointTransitionText;
    [SerializeField] TMP_Text isInitialDismountStraightRailText;

    // section trackers
    [SerializeField] TMP_Text moveContextVars;
    [SerializeField] TMP_Text wallRideVars;
    [SerializeField] TMP_Text smoothRailVars;
    [SerializeField] TMP_Text straightRailVars;

    // Start is called before the first frame update
    void Awake()
    {
        moveContextVars = GameObject.Find("MoveContextVars").GetComponent<TMP_Text>();
        wallRideVars = GameObject.Find("WallRideVars").GetComponent<TMP_Text>();
        smoothRailVars = GameObject.Find("SmoothRailVars").GetComponent<TMP_Text>();
        straightRailVars = GameObject.Find("StraightRailVars").GetComponent<TMP_Text>();
        isInitialMountText = GameObject.Find("isInitMountTextMoveContext").GetComponent<TMP_Text>();
        isJumpingText = GameObject.Find("isJumpingTextMoveContext").GetComponent<TMP_Text>();
        isDoubleJumpingText = GameObject.Find("isDoubleJumpingTextMoveContext").GetComponent<TMP_Text>();
        isFallingText = GameObject.Find("isFallingTextMoveContext").GetComponent<TMP_Text>();
        isSlidingText = GameObject.Find("isSlidingTextMoveContext").GetComponent<TMP_Text>();
        isWallRunningText = GameObject.Find("isWallRunningTextMoveContext").GetComponent<TMP_Text>();
        isGroundedText = GameObject.Find("isGroundedTextMoveContext").GetComponent<TMP_Text>();
        isDashingText = GameObject.Find("isDashingTextMoveContext").GetComponent<TMP_Text>();
        isAimingText = GameObject.Find("isAimingTextMoveContext").GetComponent<TMP_Text>();
        isAttachedToWallText = GameObject.Find("isAttachedToWallText").GetComponent<TMP_Text>();
        isInitAttachText = GameObject.Find("isInitAttachWallText").GetComponent<TMP_Text>();
        isNewDetachText = GameObject.Find("isNewDetachWallText").GetComponent<TMP_Text>();
        isInTransitionText = GameObject.Find("isInTransitionWallText").GetComponent<TMP_Text>();
        isInitialMountsmoothRailText = GameObject.Find("isInitialMountTextSmoothRail").GetComponent<TMP_Text>();
        isOnSmoothRailText = GameObject.Find("isOnSmoothRailText").GetComponent<TMP_Text>();
        dismountJumpVelocityTransitionText = GameObject.Find("dismountJumpVelocityTransitionTextSmoothRail").GetComponent<TMP_Text>();
        dismountVelocityTransitionText = GameObject.Find("dismountVelocityTransitionTextSmoothRail").GetComponent<TMP_Text>();
        handleCamYOffsetText = GameObject.Find("handleCamYOffsetSmoothRailText").GetComponent<TMP_Text>();
        isMountPointTransitionText = GameObject.Find("isMountPointTransitionStraightRailText").GetComponent<TMP_Text>();
        isInitialDismountStraightRailText = GameObject.Find("isInitialDismountStraightRailText").GetComponent<TMP_Text>();

        moveContextVars.text = "Move Context Variables:";
        wallRideVars.text = "Wallride Variables:";
        smoothRailVars.text = "Smooth Rail Variables:";
        straightRailVars.text = "Straight Rail Variables:";

    }

    // Update is called once per frame
    void Update()
    {
        // move context vars
        isInitialMountText.text = $"isInitialMount = {playerMovementManager.getMoveContext().isInitialMount}";
        isJumpingText.text = $"isJumping = {playerMovementManager.getMoveContext().isJumping}";
        isDoubleJumpingText.text = $"isDoubleJumping = {playerMovementManager.getMoveContext().isDoubleJumping}";
        isFallingText.text = $"isFalling = {playerMovementManager.getMoveContext().isFalling}";
        isSlidingText.text = $"isSliding = {playerMovementManager.getMoveContext().isSliding}";
        isWallRunningText.text = $"isWallRunning = {PlayerMovementContext.isWallRunning}";  // static exception
        isGroundedText.text = $"isGrounded = {playerMovementManager.getMoveContext().isGrounded}";
        isDashingText.text = $"isDashing = {playerMovementManager.getMoveContext().isDashing}";
        isAimingText.text = $"isAiming = {playerMovementManager.getMoveContext().isAiming}";

        // wall ride vars
        isAttachedToWallText.text = $"isAttachedToWall = {AttachToWall.isAttachedToWall}";
        isInitAttachText.text = $"isInitAttach = {AttachToWall.isInitAttach}";
        isNewDetachText.text = $"isNewDetach = {AttachToWall.isNewDetach}";
        isInTransitionText.text = $"isInTransition = {AttachToWall.isInTransition}";

        // smooth rail vars
        isInitialMountsmoothRailText.text = $"isInitialMountsmoothRail = {RailDetect.isInitialMount}";
        isOnSmoothRailText.text = $"isOnSmoothRail = {RailDetect.isOnSmoothRail}";
        isOnSmoothRailText.text = $"isOnSmoothRail = {RailDetect.isOnSmoothRail}";
        dismountJumpVelocityTransitionText.text = $"dismountJumpVelTransition = {SmoothRailGrinding.dismountJumpVelocityTransition}";
        dismountVelocityTransitionText.text = $"dismountVelocityTransition = {SmoothRailGrinding.dismountVelocityTransition}";
        handleCamYOffsetText.text = $"handleCamYOffset = {SmoothRailGrinding.handleCamYOffset}";

        // straight rail vars
        isMountPointTransitionText.text = $"isMountPointTransition = {AttachToRail.isMountPointTransition}";
        isInitialDismountStraightRailText.text = $"isInitialDismountStraightRail = {AttachToRail.isInitialDismount}";

    }

}
