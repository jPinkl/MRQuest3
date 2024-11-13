using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using MidiPlayerTK;
using TMPro;

public class LStickScript : MonoBehaviour
{
    //public TMP_Dropdown myDrop;
    Animator LstickMovement;
    [SerializeField] private float speed, jumpSpeed;
    [SerializeField] private Slider tempoSlider;
    public float decidedSpeed = 1f;



    //public float handSpeed;
    public float leftHandDelay;


    // Start is called before the first frame update
    void Start()
    {
        LstickMovement = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        // getting rid of the keycode and just starting it automatcially
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //change decided speed here for polyrhythms
            decidedSpeed = 1f;//tempoSlider.value;
            Debug.Log(decidedSpeed*60);
            //Debug.Log("TS " tempoSlider.value);
            //gettempo
            /*if (myDrop.value == 0)
            {
                decidedSpeed = 1;
;           }
            if (myDrop.value == 1)
            {
                decidedSpeed = 2;
            }
            */
            //below needs to be Lspeed, the name of it in the animator!!!!
            LstickMovement.SetFloat("Lspeed", decidedSpeed);
            //LstickMovement.SetFloat("LstickMovementLPause.speed", decidedSpeed);
            //GetComponent<Animator>().Play("LDelayDrumStick");
            LstickMovement.SetFloat("PauseSpeed", decidedSpeed);
            //Debug.Log("yup" LstickMovement.LPause.speed);

            GetComponent<Animator>().Play("LPause");
            //GetComponent<Animator>().Play("LPolyDrumStick");
            // this animates the stick but then the audio is played once the head object detects a collision. 
        }
    }
}
