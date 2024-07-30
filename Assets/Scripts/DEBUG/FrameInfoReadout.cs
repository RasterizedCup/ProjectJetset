using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Playables;

public class FrameInfoReadout : MonoBehaviour
{
    TMP_Text text;
    float displayedframerate; // only update if a big difference
    float actualframerate;
   // [SerializeField]
   // float frameDisplayUpdateThreshold = 5;
    [SerializeField]
    float frameDisplayUpdateFrequency = .2f;
    float currTime;
    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        currTime = Time.time;
        text = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        actualframerate = 1f / Time.deltaTime;
        if (Time.time > currTime)
        {
            currTime = Time.time + frameDisplayUpdateFrequency;
            displayedframerate = actualframerate;
        }
        text.text = $"{Mathf.RoundToInt(displayedframerate)}";
    }
}
