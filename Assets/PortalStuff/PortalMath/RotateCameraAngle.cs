using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCameraAngle : MonoBehaviour
{
    [SerializeField]
    Transform cameraToAdjust;
    [SerializeField]
    Transform portalPos;
    [SerializeField]
    Transform foreignPortalPos;
    [SerializeField]
    Transform playerPos;
    RenderTexture viewTexture;

    // Start is called before the first frame update
    void Start()
    {
        
    }

   

    // Update is called once per frame
    void Update()
    {
        setCamPosAndRot();
    }

    void setCamPosAndRot()
    {
        Matrix4x4 m = foreignPortalPos.localToWorldMatrix 
            * portalPos.worldToLocalMatrix 
            * playerPos.localToWorldMatrix;
        cameraToAdjust.SetPositionAndRotation(m.GetColumn(3), m.rotation);
    }
}
