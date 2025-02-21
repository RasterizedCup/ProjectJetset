using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamControl : MonoBehaviour
{
    [SerializeField]
    private Transform orientation;
    [SerializeField]
    private Transform playerHoldableOrientation;

    [SerializeField]
    private float xSensitivity;
    [SerializeField]    
    private float ySensitivity;
    [SerializeField]
    private float sharedSensitivity;
    [SerializeField]
    private bool isSharedSens;

    private float xRot;
    private float yRot;
    
    // Start is called before the first frame update
    void Start()
    {
        // lock and hide cursor for play preview
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }   

    // Update is called once per frame
    void Update()
    {
        handlePlayerRotation();
    }

    void handlePlayerRotation()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * (isSharedSens ? sharedSensitivity : xSensitivity);
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * (isSharedSens ? sharedSensitivity : ySensitivity);

        yRot += mouseX;
        xRot -= mouseY;

        xRot = Mathf.Clamp(xRot, -90f, 90f);
        transform.rotation = Quaternion.Euler(xRot, yRot, 0);
        orientation.rotation = Quaternion.Euler(0, yRot, 0);
    }

    public void UpdateRotationFromTeleport(Quaternion teleRotation)
    {
        Vector3 rotCoords = teleRotation.eulerAngles;
        xRot = rotCoords.x + xRot;
        yRot = rotCoords.y + yRot;
    }
}
