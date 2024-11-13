using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using MidiPlayerTK;
using TMPro;
using UnityEngine.Video;

public class StickTV : MonoBehaviour
{

    //public videoPlayer player;

    public VideoPlayer player;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {


    //player - _ GetComponent<videoPlayer


    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Keypad0))
        {

       //this.GameObject.GetComponent<videoPlayer>().Play();
       player.Play();
    }


        
    }
}
