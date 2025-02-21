using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Parameters")]
    [SerializeField]
    private float accelerationRate;
    [SerializeField]
    private float maxVelocity;
    [SerializeField]
    private float terminalFallingVelocity;
    [SerializeField]
    private float rbVelocityMultiplier = 10;
    [SerializeField]
    private float groundDrag;

    [Header("Ground Check Parameters")]
    [SerializeField]
    private float playerHeight;
    [SerializeField]
    private float playerGroundedThreshold = .15f;
    [SerializeField]
    private LayerMask groundLayers;
    bool grounded;

    [SerializeField]
    private Transform orientation;

    float horizInput;
    float vertInput;

    Vector3 moveDirection;
    Ray debugRay;
    Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    // Update is called once per frame
    void Update()
    {
        GetInput();
        HandlePlayerDragState();
        ClampMovement();
    }

    void FixedUpdate()
    {
        PerpetuateMovement();
    }


    void GetInput()
    {
        horizInput = Input.GetAxisRaw("Horizontal"); // make remappable?
        vertInput = Input.GetAxisRaw("Vertical");
    }

    void PerpetuateMovement()
    {
        moveDirection = orientation.forward * vertInput + orientation.right * horizInput;
        rb.AddForce(moveDirection.normalized * accelerationRate * rbVelocityMultiplier, ForceMode.Force);
    }

    void ClampMovement()
    {
        Vector3 netVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if(netVelocity.magnitude > maxVelocity)
        {
            Vector3 clampedVel = netVelocity.normalized * maxVelocity;
            rb.velocity = new Vector3(clampedVel.x, rb.velocity.y, clampedVel.z);
        }
        if(rb.velocity.y < terminalFallingVelocity)
        {
            rb.velocity = new Vector3(rb.velocity.x, terminalFallingVelocity, rb.velocity.z);
        }


    }

    void HandlePlayerDragState()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * .5f + playerGroundedThreshold, groundLayers);
        if (grounded)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = 0;
        }
    }

    public void UpdateMoveDirectionFromTeleport(Vector3 portalForward, Vector3 oldPortal, Vector3 newPortal, Quaternion rotDirection, Transform transform = null)
    {

        // find a way to rotate the velocity to match the rotational change between portals
        // vary this on side of approach
       // Debug.Log(newForwardDir);
         Vector3 vel = rb.velocity.magnitude * (portalForward);

        rb.velocity = vel;
    }

    // meh, this isn't the best but it's O(1) and isnt hurting anyone
    public bool IsMoving()
    {
        return horizInput != 0 || vertInput != 0;
    }

    public float getVelocity()
    {
        return rb.velocity.magnitude;
    }

    public Vector3 getStandardizedMovementVector()
    {
        return moveDirection.normalized * accelerationRate;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(transform.position, transform.position + (Vector3.down * (playerHeight * .5f + playerGroundedThreshold)));
    }


}
