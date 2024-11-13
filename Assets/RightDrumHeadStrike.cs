//this gameObject used LHeadStrike initially, but we need to control the controllers separately... 
//so writing a unique script for the right hand!

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



public class RightDrumHeadStrike : MonoBehaviour
{

    public AudioSource rightMembranophone;
    public AudioClip rightSample;
    public GameObject LightR1;
    public MidiFilePlayer midiFilePlayer;
    //public GameObject redlightR;


    // Start is called before the first frame update
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
        InitPlayR();
    }

    public void startVibRight()
    {
        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
        //Debug.Log("stay funky brosL");
    }
    public void stopVibRight()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
    }




    public void InitPlayR()
    {
        if (MidiPlayerGlobal.MPTK_ListMidi != null && MidiPlayerGlobal.MPTK_ListMidi.Count > 0)
        {
            // midiFilePlayer.MPTK_MidiIndex = 5;
        }
        //midiFilePlayer.MPTK_Play();
    }


    void OnCollisionEnter(Collision collision)
    {
        //used this line for a while but dont need anymore / right now
        //Debug.Log("Enter left drum head");
        //next line is for playing audio sample
        rightMembranophone.PlayOneShot(rightSample);
        GetComponent<Animator>().Play("LdrumHead");

        //april 2024 addition
        Invoke("startVibRight", .001f);
        Invoke("stopVibRight", .2f);

        //redlightL.SetActive(true);
        //midiFilePlayer.MPTK_MidiIndex = 2;
        //midiFilePlayer.MPTK_Play();



        //RightDrumHead1.Play(DoorOpen);


        LightR1.SetActive(true);

    }




    // Update is called once per frame
    void Update()
    {
        
    }
}
