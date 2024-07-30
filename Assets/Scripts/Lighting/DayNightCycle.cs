using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    Light refLight;
    [SerializeField]
    private float rotationRate;
    [SerializeField]
    private float colorOscillationRate;
    [SerializeField]
    private Color SunsetColor;
    [SerializeField]
    private float sunriseStartAngle;
    [SerializeField]
    private float sunsetStartAngle;
    [SerializeField]
    private float sunriseEndAngle;
    [SerializeField]
    private float sunsetEndAngle;

    bool isRising;

    float prevRotationVal;

    float currSatVal;
    float initialSatVal;
    float hVal, vVal;
    // Start is called before the first frame update
    void Start()
    {
        refLight = GetComponent<Light>();
        refLight.color = SunsetColor;
        Color.RGBToHSV(refLight.color, out float H, out float S, out float V);
        currSatVal = S;
        initialSatVal = currSatVal;
        hVal = H;
        vVal = V;
    }

    // Update is called once per frame
    void Update()
    {
        updateRotation();
    }

    // from X == 10->0 (white -> sunset -> white) -> -170->-180 (white -> sunrise -> white)
    void updateRotation()
    {
        prevRotationVal = transform.rotation.eulerAngles.x;
        transform.Rotate(0, rotationRate * Time.deltaTime, 0, Space.Self);
        isRising = (transform.localRotation.eulerAngles.x - prevRotationVal > 0);
        //Debug.Log(isRising);

        // sunrise-sunrise filter
        if((transform.rotation.eulerAngles.x > sunriseStartAngle || transform.rotation.eulerAngles.x < sunriseEndAngle))
            currSatVal += (colorOscillationRate * Time.deltaTime);

       // else if (!isRising && (transform.rotation.eulerAngles.x < sunsetStartAngle || transform.rotation.eulerAngles.x > sunsetStartAngle))
       //     currSatVal += (colorOscillationRate * Time.deltaTime);

        // deactivate filter
        else
            currSatVal -= (colorOscillationRate * Time.deltaTime);

        // sunset mode
      /*  if (transform.rotation.eulerAngles.x > sunsetStartAngle || transform.rotation.eulerAngles.x < sunriseEndAngle)
        {
            currSatVal += (colorOscillationRate * Time.deltaTime);
        }
        if (transform.rotation.eulerAngles.x < sunsetStartAngle && transform.rotation.eulerAngles.x > sunsetEndAngle)
        {
            currSatVal -= (colorOscillationRate * Time.deltaTime);
        }
        if(transform.rotation.eulerAngles.x < sunriseStartAngle && transform.rotation.eulerAngles.x > sunriseEndAngle)
        {
            currSatVal -= (colorOscillationRate * Time.deltaTime);
        }*/
        currSatVal = Mathf.Clamp(currSatVal, 0, initialSatVal);
        refLight.color = Color.HSVToRGB(hVal, currSatVal, vVal);
    }
}
