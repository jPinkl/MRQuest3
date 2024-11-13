using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiPlayerTK;


//4.23.2024 adding code for haptics in the left controller...
using System;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using MidiPlayerTK;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class LHeadStrike : MonoBehaviour
{
    public AudioSource leftMembranophone;
    public AudioClip leftSample;
    public GameObject LightL1;
    public MidiFilePlayer midiFilePlayer;
    //public GameObject redlightR;

    //[SerializeField] private Animator RightDrumHead1;
    //[SerializeField] private string DoorOpen = RdrumHead;
    void Start()
    {
        if (midiFilePlayer == null)
        {
            Debug.Log("No MIDI file");
            MidiFilePlayer fp = FindObjectOfType<MidiFilePlayer>();
            if (fp == null)
            {
                Debug.Log("cant");
            }
            else
            {
                midiFilePlayer = fp;
            }
        }
        //midiFilePlayer.MPTK_MidiIndex = 81;
        InitPlay();
    }

    //added in april 2024 for haptics. 
    public void startVibLeft()
    {
        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.LTouch);
        //Debug.Log("stay funky brosL");
    }
    public void stopVibLeft()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
    }

    public void InitPlay()
    {
        if (MidiPlayerGlobal.MPTK_ListMidi != null && MidiPlayerGlobal.MPTK_ListMidi.Count > 0)
        {
           // midiFilePlayer.MPTK_MidiIndex = 5;
        }
        //midiFilePlayer.MPTK_Play();
    }
    // Start is called before the first frame update
    void OnCollisionEnter(Collision collision)
    {
        //used this line for a while but dont need anymore / right now
        //Debug.Log("Enter left drum head");
        //next line is for playing audio sample
        leftMembranophone.PlayOneShot(leftSample);
        GetComponent<Animator>().Play("LdrumHead");

        //april 2024 addition
        Invoke("startVibLeft", .001f);
        Invoke("stopVibLeft", .2f);

        //redlightL.SetActive(true);
        //midiFilePlayer.MPTK_MidiIndex = 2;
        //midiFilePlayer.MPTK_Play();



        //RightDrumHead1.Play(DoorOpen);


        LightL1.SetActive(true);

    }

    private void OnCollisionStay(Collision collision)
    {
        //used this line for a while but dont need anymore / right now
        //Debug.Log("Stay left drum head");

    }

    void OnCollisionExit(Collision collision)
    {
        //used this line for a while but dont need anymore / right now
        //Debug.Log("Exit left drum head");
        //GetComponent<Animator>().StopPlayback("RdrumHead");
        LightL1.SetActive(false);


        //try this with a shorter midi file
        //midiFilePlayer.MPTK_Play();
    }

}
