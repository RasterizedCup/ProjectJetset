using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadioStation : MonoBehaviour
{
    private int songIndex = 0;
    private AudioSource currPlayingOnThisStation;

    [SerializeField]
    public string RadioStationName;
    [SerializeField]
    private AudioClip[] songList;
    [SerializeField]
    private string[] songNameList;

    private void Start()
    {
        if (currPlayingOnThisStation != null)
            return;

        currPlayingOnThisStation = GetComponent<AudioSource>();
        currPlayingOnThisStation.volume = 0;
        currPlayingOnThisStation.clip = songList[songIndex];
        currPlayingOnThisStation.Play();
    }

    private void Update()
    {
        HandleStationSong();
    }

    public (AudioClip, string) IterateSong()
    {
        if(songIndex < songList.Length-1)
            songIndex++;
        else 
            songIndex = 0;

        return (songList[songIndex], songNameList[songIndex]);
    }

    // set bounds checking (mostly a debug function)
    public (AudioClip, string) ChooseSongDirectly(int index)
    {
        return (songList[index], songNameList[index]);
    }

    private void HandleStationSong()
    {
        if (!currPlayingOnThisStation.isPlaying)
        {
            if (songIndex < songList.Length - 1)
                songIndex++;
            else
                songIndex = 0;
            currPlayingOnThisStation.clip = songList[songIndex];
            currPlayingOnThisStation.Play();
        }
    }

    public string GetCurrSong()
    {
        return songNameList[songIndex];
    }

    public void SelectStation(bool isSelected)
    {
        if (currPlayingOnThisStation == null) // base case to resolve constructor race condition
            OutOfOrderInit();
        currPlayingOnThisStation.volume = (isSelected ? .1f : 0);
    }

    public void ToggleStationMute()
    {
        currPlayingOnThisStation.volume = (currPlayingOnThisStation.volume == 0 ? .1f : 0);
    }

    public AudioSource getCurrentSource()
    {
        return currPlayingOnThisStation;
    }

    public void OutOfOrderInit()
    {
        currPlayingOnThisStation = GetComponent<AudioSource>();
        currPlayingOnThisStation.volume = 0;
        currPlayingOnThisStation.clip = songList[songIndex];
        currPlayingOnThisStation.Play();
    }
}
