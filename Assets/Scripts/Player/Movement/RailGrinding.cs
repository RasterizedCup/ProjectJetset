using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailGrinding : PlayerMovementEffector
{
    [SerializeField]
    float railMountJumpTimeLockout;


    [SerializeField]
    private LayerMask railMask;
    [SerializeField]
    private float maxRailVelocity = 20f;
    [SerializeField]
    private float maxOverDriveRailVelocity = 50f;
    [SerializeField]
    private float velocityRailIncreaseRate = 50;
    [SerializeField]
    private float overdriveVelocityIncreaseRate = 5;

    [SerializeField]
    private float gfxVerticalRotationRate = 100f;

    bool isPositiveAccel;
    bool isAccelerating;
    private float zeroPoint = 270;
    float currVelocity, prevVelocity;


    // function designed to circle euler angles back around to 0 if they pass 360
    // handles boundaries for camera based momentum around a zero point defined by the camera angle
    // when aligned straight on a zero angle rail
    // also considers inclusive and exclusive bounding for angles
    (float, float, bool) FindZeroPointOffsetBoundaries(float railAngle)
    {
        bool outerBound = false;

        float zeroPointOffset = zeroPoint - (360 - railAngle);
        float zeroPointOffsetUpperBounds = zeroPointOffset + 90;
        if (zeroPointOffsetUpperBounds >= 360)
        {
            zeroPointOffsetUpperBounds = zeroPointOffsetUpperBounds - 360;
        }

        float zeroPointOffsetLowerBounds = zeroPointOffset - 90;
        if (zeroPointOffsetLowerBounds < 0)
        {
            zeroPointOffsetLowerBounds = 360 + zeroPointOffsetLowerBounds;
            outerBound = true;
        }
        return (Mathf.Max(zeroPointOffsetLowerBounds, zeroPointOffsetUpperBounds), Mathf.Min(zeroPointOffsetLowerBounds, zeroPointOffsetUpperBounds), outerBound);
    }

    public void handleRailGrinding(GameObject currRail, ref PlayerMovementContext moveContext)
    {
        if (AttachToRail.isAttachedToRail)
        {
            float currVelocityIncreaseRate = getCurrentSpeedBracket(moveContext.railVelocity);
            moveContext.playerVerticalVelocity.y = 0; // perpetually reset jump power whenever mounted on rail
            //currIdleTime = Time.time;

            if (moveContext.isInitialMount)
            {
                moveContext.isJumping = false;
                moveContext.railVelocity = moveContext.currAccelMatrix.magnitude;
                moveContext.currAccelMatrix = Vector3.zero;
            }

            // handle forwards and backwards
            // factor in camera rotation
            // two 180 degree quadrants, based locally on rotation of current rail
            // 359-180 == W is forward, 0-179 == W is backwards, S is inverse
            // base movement choice input on which quadrant camera is rotated into            
            float inputDirection = Input.GetAxisRaw("Vertical");
            if (Input.GetKey(KeyCode.W) || inputDirection > 0)
            {
                (float, float, bool) offsetVals = FindZeroPointOffsetBoundaries(currRail.transform.eulerAngles.y);
                // inner value checks (ex: no overlap past 0, bound is between 100 and 200)
                if (!offsetVals.Item3)
                {
                    // handle velocity inversion when mounting rail from negative angle
                    if (moveContext.isInitialMount && (offsetVals.Item1 < cam.rotation.eulerAngles.y ||
                        cam.rotation.eulerAngles.y < offsetVals.Item2))
                    {
                        moveContext.railVelocity *= -1;
                        moveContext.isInitialMount = false;
                    }

                    moveContext.railVelocity = (
                        offsetVals.Item1 >= cam.rotation.eulerAngles.y &&
                        cam.rotation.eulerAngles.y >= offsetVals.Item2 ?
                        moveContext.railVelocity + (currVelocityIncreaseRate * Time.fixedDeltaTime) :
                        moveContext.railVelocity - (currVelocityIncreaseRate * Time.fixedDeltaTime)); // calculate velocity rate increase
                }
                else
                { // xor bounds, meaning overlap past 0, bound is less than 100, greater than 200

                    // handle velocity inversion when mounting rail from negative angle
                    if (moveContext.isInitialMount && (offsetVals.Item1 > cam.rotation.eulerAngles.y &&
                        cam.rotation.eulerAngles.y > offsetVals.Item2))
                    {
                        moveContext.railVelocity *= -1;
                        moveContext.isInitialMount = false;
                    }

                    moveContext.railVelocity = (
                        offsetVals.Item1 <= cam.rotation.eulerAngles.y ||
                        cam.rotation.eulerAngles.y <= offsetVals.Item2 ?
                        moveContext.railVelocity + (currVelocityIncreaseRate * Time.fixedDeltaTime) :
                        moveContext.railVelocity - (currVelocityIncreaseRate * Time.fixedDeltaTime)); // calculate velocity rate increase
                }
            }
            else if (Input.GetKey(KeyCode.S) || inputDirection < 0)
            {
                (float, float, bool) offsetVals = FindZeroPointOffsetBoundaries(currRail.transform.eulerAngles.y);

                // Debug.Log($"Max = {offsetVals.Item1}, Min = {offsetVals.Item2}, AngleCam = {cam.rotation.eulerAngles.y}, isOuterBound = {offsetVals.Item3}");

                // inner value checks (ex: no, lap over 0, bound is between 100 and 200)
                if (!offsetVals.Item3)
                {
                    // handle velocity inversion when mounting rail from negative angle
                    if (moveContext.isInitialMount && (offsetVals.Item1 > cam.rotation.eulerAngles.y &&
                        cam.rotation.eulerAngles.y > offsetVals.Item2))
                    {
                        moveContext.railVelocity *= -1;
                        moveContext.isInitialMount = false;
                    }

                    moveContext.railVelocity = (
                        offsetVals.Item1 >= cam.rotation.eulerAngles.y &&
                        cam.rotation.eulerAngles.y >= offsetVals.Item2 ?
                        moveContext.railVelocity - (currVelocityIncreaseRate * Time.fixedDeltaTime) :
                        moveContext.railVelocity + (currVelocityIncreaseRate * Time.fixedDeltaTime)); // calculate velocity rate increase
                }
                else
                { // xor bounds, meaning overlap past 0, bound is less than 100, greater than 200
                    // handle velocity inversion when mounting rail from negative angle
                    if (moveContext.isInitialMount && (offsetVals.Item1 < cam.rotation.eulerAngles.y ||
                        cam.rotation.eulerAngles.y < offsetVals.Item2))
                    {
                        moveContext.railVelocity *= -1;
                        moveContext.isInitialMount = false;
                    }

                    moveContext.railVelocity = (
                        offsetVals.Item1 <= cam.rotation.eulerAngles.y ||
                        cam.rotation.eulerAngles.y <= offsetVals.Item2 ?
                        moveContext.railVelocity - (currVelocityIncreaseRate * Time.fixedDeltaTime) :
                        moveContext.railVelocity + (currVelocityIncreaseRate * Time.fixedDeltaTime)); // calculate velocity rate increase
                }
               /* if (moveContext.railVelocity > maxRailVelocity)
                    moveContext.railVelocity = maxRailVelocity;
                if (moveContext.railVelocity < maxRailVelocity * -1)
                    moveContext.railVelocity = maxRailVelocity * -1;*/
            }
            // only check for mount correction on first pass
            else
            {
                // get initial contact point info. if greater than center, handle neg,
                // if less than center, handle pos
                Vector3 relPosition = currRail.transform.position - moveContext.lastFramePos;

                if (moveContext.isInitialMount)
                {
                    // need to be handled differently
                    // if we aren't past half rail
                    bool dontToggle = false;
                    float currPosDistance = Mathf.Sqrt(Mathf.Pow(currRail.transform.position.x - moveContext.lastFramePos.x, 2) + Mathf.Pow(currRail.transform.position.z - moveContext.lastFramePos.z, 2));
                    float prevPosDistance = Mathf.Sqrt(Mathf.Pow(currRail.transform.position.x - moveContext.secondPrevFramePos.x, 2) + Mathf.Pow(currRail.transform.position.z - moveContext.secondPrevFramePos.z, 2));

                    if (Vector3.Dot(currRail.transform.up, relPosition) > 0 && currPosDistance < prevPosDistance)
                    {
                        // if we're moving closer to half way rail (currRail.transform.position)
                        dontToggle = true;
                    }
                    // if we are past half rail
                    if (Vector3.Dot(currRail.transform.up, relPosition) < 0 && currPosDistance > prevPosDistance)
                    {
                        // if we're moving further from half way rail (currRail.transform.position)
                        dontToggle = true;
                    }
                    if (!dontToggle)
                    {
                        moveContext.railVelocity *= -1;
                    }
                    dontToggle = false;
                }
            }

            moveContext.isInitialMount = false;

            // compare player direction and camera direction against rail direction when hopping on rail, handle momentum based on that
            // camera orientation controls *forwards* and *backwards*
            // currRail.transform.up.normalized or * -1, depending on initial momentum and camera

            // map to rail normalinstead of this
            Vector3 RailNormal = SetAndGetRailNormal();
            Vector3 railForward = Vector3.Cross(RailNormal, transform.up);
            //controller.Move(railForward * (moveContext.railVelocity) * Time.fixedDeltaTime);
            //controller.transform.rotation = Quaternion.FromToRotation(Vector3.right, RailNormal);
            controller.Move(currRail.transform.up.normalized * (moveContext.railVelocity) * Time.fixedDeltaTime);

            transform.rotation = Quaternion.Euler(0, (moveContext.railVelocity > 0 ? currRail.transform.eulerAngles.y - 90 : currRail.transform.eulerAngles.y + 90), 0);
            // instant rotation line
            // gfx.transform.localRotation = Quaternion.Euler((moveContext.railVelocity > 0 ? currRail.transform.eulerAngles.z - 90 : (currRail.transform.eulerAngles.z - 90) * -1), 0, 0);

            // gradual vertical rotation
            Vector3 newGfxRotation = Quaternion.RotateTowards(
                gfx.transform.localRotation,
                Quaternion.Euler((moveContext.railVelocity > 0 ? currRail.transform.eulerAngles.z - 90 : (currRail.transform.eulerAngles.z - 90) * -1), 0, 0),
                Mathf.Abs(gfxVerticalRotationRate * (moveContext.railVelocity / 2 * .2f) * Time.fixedDeltaTime)).eulerAngles;

            if (moveContext.railVelocity > 0 && !isPositiveAccel)
            {
                newGfxRotation.x *= -1;
                isPositiveAccel = true;
            }
            if (moveContext.railVelocity < 0 && isPositiveAccel)
            {
                newGfxRotation.x *= -1;
                isPositiveAccel = false;
            }
            gfx.transform.localRotation = Quaternion.Euler(newGfxRotation.x, newGfxRotation.y, newGfxRotation.z);
            // end gradual rotation code
            moveContext.railVelocity = Mathf.Clamp(moveContext.railVelocity, -maxOverDriveRailVelocity, maxOverDriveRailVelocity); ;
            isAccelerating = Mathf.Abs(prevVelocity) < Mathf.Abs(moveContext.railVelocity);
            prevVelocity = moveContext.railVelocity;
        }
    }

    float getCurrentSpeedBracket(float railVelocity)
    {
        // if we're accelerating and past our normal threshold
        if (isAccelerating && Mathf.Abs(railVelocity) >= maxRailVelocity)
        {
            return overdriveVelocityIncreaseRate;
        }
        return velocityRailIncreaseRate;
    }

    private Vector3 SetAndGetRailNormal()
    {
        RaycastHit rayHit;
        bool isRailHit = Physics.Raycast(transform.position, transform.up * -1, out rayHit, 3f, railMask);
        //Debug.Log(rayHit.normal);
        return rayHit.normal;
    }
}
