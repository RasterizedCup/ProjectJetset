using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PortalTraveller : MonoBehaviour
{
    bool currentlyForward = true;
    [SerializeField]
    Transform travelerCam;
    public bool isUsingCameraForRot = false;
    public GameObject graphicsObject;
    public GameObject graphicsClone { get; set; }
    public Vector3 previousOffsetFromPortal { get; set; }

    public Material[] originalMaterials { get; set; }
    public Material[] cloneMaterials { get; set; }

    public virtual void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        if (!isUsingCameraForRot)
        {
            transform.rotation = rot;
            return;
        }
        travelerCam.GetComponent<CamControl>().UpdateRotationFromTeleport(rot);
        // below line is a bandaid, not working properly
        Vector3 momentumTransferCoefficient =
            currentlyForward ?
            toPortal.forward : 
            toPortal.forward * -1;
       // Debug.Log($"portal forward: {toPortal.forward}");
        // angleAxis? to fine to speed coming out of portal
        // may not fix the negative and positive forward issue
        // short portal lockout timer
        this.GetComponent<PlayerMovement>().UpdateMoveDirectionFromTeleport(momentumTransferCoefficient, fromPortal.eulerAngles, toPortal.eulerAngles, rot); 
    }

    // Called when first touches portal
    public virtual void EnterPortalThreshold()
    {
        if (graphicsClone == null)
        {
            graphicsClone = Instantiate(graphicsObject);
            graphicsClone.transform.parent = graphicsObject.transform.parent;
            graphicsClone.transform.localScale = graphicsObject.transform.localScale;
            originalMaterials = GetMaterials(graphicsObject);
            cloneMaterials = GetMaterials(graphicsClone);
        }
        else
        {
          //  graphicsClone.SetActive(true);
        }
    }

    // Called once no longer touching portal (excluding when teleporting)
    public virtual void ExitPortalThreshold()
    {
        graphicsClone.SetActive(false);
        // Disable slicing
        for (int i = 0; i < originalMaterials.Length; i++)
        {
            originalMaterials[i].SetVector("sliceNormal", Vector3.zero);
        }
    }

    public void SetSliceOffsetDst(float dst, bool clone)
    {
        for (int i = 0; i < originalMaterials.Length; i++)
        {
            if (clone)
            {
                cloneMaterials[i].SetFloat("sliceOffsetDst", dst);
            }
            else
            {
                originalMaterials[i].SetFloat("sliceOffsetDst", dst);
            }

        }
    }
    public void setForwardorBackward(bool isForward)
    {
        currentlyForward = isForward;
    }
    Material[] GetMaterials(GameObject g)
    {
        var renderers = g.GetComponentsInChildren<MeshRenderer>();
        var matList = new List<Material>();
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.materials)
            {
                matList.Add(mat);
            }
        }
        return matList.ToArray();
    }
}