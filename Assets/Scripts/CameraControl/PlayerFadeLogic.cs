using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFadeLogic : MonoBehaviour
{
    [SerializeField]
    GameObject mainCam;

    [SerializeField]
    GameObject player;

    [SerializeField]
    Renderer Renderer;

    Material material;

    [SerializeField]
    float minTransparentVal;

    [SerializeField]
    float maxOpacityCameraDistance;

    [SerializeField]
    float minOpacityCameraDistance;
    // Start is called before the first frame update
    void Start()
    {
        material = Renderer.material;
    }

    // Update is called once per frame
    void Update()
    {
        setOpacityFromCameraProximity();
    }

    void setOpacityFromCameraProximity()
    {
        float distance = Vector3.Distance(mainCam.transform.position, player.transform.position);

        // normalize our distance from a maximum chosen point from the player and and chosen minimum point
        float normalizedDist = (distance - minOpacityCameraDistance) / (maxOpacityCameraDistance);
        //Debug.Log(normalizedDist);

        // clamp our maximum transparency to a minimum value
        normalizedDist = Mathf.Clamp(normalizedDist, minTransparentVal, 1);

        material.SetFloat("_Tweak_transparency", 0 - (1 - normalizedDist));
    }
}
