using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioVisualizer : MonoBehaviour
{
    [SerializeField]
    private Camera UiCamera;

    [SerializeField]
    private Material VisualizerMat1;
    [SerializeField]
    private Material VisualizerMat2;
    [SerializeField]
    private Transform VisualizerTransform1;
    [SerializeField]
    private Transform VisualizerTransform2;

    private const int SAMPLE_SIZE = 1024;
    public float rmsValue;
    public float dbValue;
    public float pitchValue;

    public float maxVisualScale = 25f; // 3.5f
    public float visualModifier = 500.0f; // 175
    public float smoothSpeed = 20.0f; //used to change the visual overtime, dampening
    public float keepPercent = .5f; // .1f


    private int selectedRadioStation = 0; //change to enum
    [SerializeField]
    private RadioStation[] radioStationList;

    private AudioSource source; 
    private float[] samples;
    private float[] spectrum;
    private float sampleRate;

    public Transform[][] visualList;
    public Transform[] visualList2;
    private float[] visualScale;
    private int amtOfVisual = 50;

    private void Start()
    {
        visualList = new Transform[radioStationList.Length][];
        // source = GetComponent<AudioSource>();
        //   (AudioClip, string) currSong = radioStationList[0].ChooseSongDirectly(0);
        //    source.clip = currSong.Item1;
        //    source.Play();
        radioStationList[0].SelectStation(true);
        samples = new float[SAMPLE_SIZE];
        spectrum = new float[SAMPLE_SIZE];
        sampleRate = AudioSettings.outputSampleRate;
        SpawnVisualizer1();
        SpawnVisualizer2();
    }

    private void SpawnVisualizer1()
    {
        visualScale = new float[amtOfVisual]; // only need to init once
        visualList[0] = new Transform[amtOfVisual];
        for(int i=0; i < amtOfVisual; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            visualList[0][i] = go.transform;
            visualList[0][i].localScale = visualList[0][i].localScale * .1f;
            visualList[0][i].gameObject.GetComponent<BoxCollider>().enabled = false;
            visualList[0][i].gameObject.GetComponent<MeshRenderer>().material = VisualizerMat1;
            visualList[0][i].gameObject.layer = 10;
            visualList[0][i].position = (new Vector3(.1f, 0, 0) * i) + new Vector3(99, 99, 0); // offset from center
            visualList[0][i].SetParent(VisualizerTransform1);
        }
    }

    private void SpawnVisualizer2()
    {
        visualList[1] = new Transform[amtOfVisual];
        for (int i = 0; i < amtOfVisual; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            visualList[1][i] = go.transform;
            visualList[1][i].localScale = visualList[1][i].localScale * .1f;
            visualList[1][i].gameObject.GetComponent<BoxCollider>().enabled = false;
            visualList[1][i].gameObject.GetComponent<MeshRenderer>().material = VisualizerMat2;
            visualList[1][i].gameObject.layer = 10;
            visualList[1][i].position = (new Vector3(0, 0, .1f) * i) + new Vector3(87.864f, 99, -16.85f); // offset from center
            visualList[1][i].SetParent(VisualizerTransform2);
        }
    }

    private void Update()
    {
 /*       if (!source.isPlaying || Input.GetKeyDown(KeyCode.T))
        {

            (AudioClip, string) currSong = radioStationList[0].IterateSong();
            source.clip = currSong.Item1;
            source.Play();
        }*/

        if (Input.GetKeyDown(KeyCode.T) || Input.GetButtonDown("Fire1"))
        {
            if(selectedRadioStation == 0)
            {
                radioStationList[selectedRadioStation].SelectStation(false);
                selectedRadioStation++;
                radioStationList[selectedRadioStation].SelectStation(true);
                UiCamera.transform.Rotate(new Vector3(0, -90, 0));
            }

            else if (selectedRadioStation == 1)
            {
                radioStationList[selectedRadioStation].SelectStation(false);
                selectedRadioStation--;
                radioStationList[selectedRadioStation].SelectStation(true);
                UiCamera.transform.Rotate(new Vector3(0, 90, 0));
            }
        }
        // mute currentStation
        if(Input.GetKeyDown(KeyCode.O))
        {
            radioStationList[selectedRadioStation].ToggleStationMute();
        }

        AnalyzeSound();
        UpdateVisual();
    }


    private void UpdateVisual()
    {
        int visualIndex = 0;
        int spectrumIndex = 0;
        int averageSize = (int)((SAMPLE_SIZE * keepPercent) / amtOfVisual);

        while(visualIndex < amtOfVisual)
        {
            int j = 0;
            float sum = 0;
            while (j < averageSize)
            {
                sum += spectrum[spectrumIndex];
                spectrumIndex++;
                j++;
            }
            float scaleY = sum / averageSize * visualModifier;
            visualScale[visualIndex] -= Time.deltaTime * smoothSpeed;
            if (visualScale[visualIndex] < scaleY)
            {
                visualScale[visualIndex] = scaleY;
            }

            visualScale[visualIndex] = Mathf.Clamp(visualScale[visualIndex], 0, maxVisualScale);

            visualList[selectedRadioStation][visualIndex].localScale = new Vector3(visualList[selectedRadioStation][visualIndex].localScale.x, .1f, visualList[selectedRadioStation][visualIndex].localScale.z) + Vector3.up * visualScale[visualIndex];
            visualIndex++;
        }

    }

    // read article if you wanna understand, mr. cup
    private void AnalyzeSound()
    {
        radioStationList[selectedRadioStation].getCurrentSource().GetOutputData(samples, 0);

        //get RMS value
        int i = 0;
        float sum = 0;
        for(; i< SAMPLE_SIZE; i++)
        {
            sum += samples[i] * samples[i];
        }
        rmsValue = Mathf.Sqrt(sum / SAMPLE_SIZE);

        //Get the DB value
        dbValue = 20 * Mathf.Log10(rmsValue / .1f);

        //Get sound spectrum
        radioStationList[selectedRadioStation].getCurrentSource().GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        //Find pitch (may not use)
        float maxV = 0;
        var maxN = 0;
        for(i = 0; i < SAMPLE_SIZE; i++)
        {
            if (!(spectrum[i] > maxV) || !(spectrum[i] > 0.0f)){
                continue;
            }

            maxV = spectrum[i];
            maxN = i;
        }

        float freqN = maxN;
        if(maxN > 0 && maxN < SAMPLE_SIZE - 1){
            var dL = spectrum[maxN - 1] / spectrum[maxN];
            var dR = spectrum[maxN + 1] / spectrum[maxN];
            freqN += .5f * (dR * dR - dL * dL);
        }
        pitchValue = freqN * (sampleRate / 2) / SAMPLE_SIZE;
    }
}
