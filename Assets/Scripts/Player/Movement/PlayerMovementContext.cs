using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MoveState
{
    Standard = 0,
    StraightRail = 1,
    SmoothRail = 2,
    WallRunning = 3
};

public class PlayerMovementContext
{
    // frame position data
    public Vector3 currFramePos = Vector3.zero;
    public Vector3 prevFramePos = Vector3.zero;
    public Vector3 secondPrevFramePos = Vector3.zero;
    public Vector3 secondPrevFrameOffset = Vector3.zero;
    public Vector3 lastFramePos = Vector3.zero;
    public Queue<Vector3> positionHistory = new Queue<Vector3>();
    public int maxPosHistoryTrack = 10;
    // movement states
    public bool isInitialMount;
    public bool isJumping = false;
    public bool isDoubleJumping = false;
    public bool isFalling = false;
    public bool isSliding = false;
    public static bool isWallRunning = false;
    public bool isGrounded = true;
    public bool isDashing = false;
    public bool isAiming = false;
    public float currIdleTime; // parse out into animator manager?

    // movement values
    public Vector3 playerVerticalVelocity = Vector3.zero; // jump applied separately
    public Vector3 currAccelMatrix = Vector3.zero; // generalized movement matrix
    public float currAccelChangeRate = 110; // globally tracked movement inertia
    public float railVelocity = 0;  // linear rail movement

    // move acceleration velocity here?
}
