using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using MidiPlayerTK;
using TMPro;

public class controlForVC : MonoBehaviour
{
    //public TMP_Dropdown myDrop;
    Animator LstickMovementFast;
    [SerializeField] private float speed, jumpSpeed;
    [SerializeField] private Slider tempoSlider;


    public float decidedSpeedFast = 0.2352f;

    [SerializeField] GameObject RightHand;
    [SerializeField] GameObject LeftHand;



    //public float handSpeed;
    public float leftHandDelayFast;


    // Start is called before the first frame update
    void Start()
    {
        LstickMovementFast = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        // getting rid of the keycode and just starting it automatcially


        if (Input.GetKeyDown(KeyCode.Keypad9))
        {
            RightHand.SetActive(true);
            LeftHand.SetActive(true);

        }

            if (Input.GetKeyDown(KeyCode.Space))
        {
            //change decided speed here for polyrhythms
            decidedSpeedFast = 0.2352f;//tempoSlider.value;

            //right hand only animates!!!!
            RightHand.SetActive(true);
            LeftHand.SetActive(false);


            Debug.Log("mhm" + decidedSpeedFast * 60);
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

            //experimenting by comenting this out....
            //LstickMovementFast.SetFloat("Lspeed", 1.5f);

            //LstickMovement.SetFloat("LstickMovementLPause.speed", decidedSpeed);
            //GetComponent<Animator>().Play("LDelayDrumStick");



            //LstickMovementFast.SetFloat("PauseSpeed", decidedSpeedFast);
            //Debug.Log("yup" LstickMovement.LPause.speed);

            GetComponent<Animator>().Play("LPause");
            //GetComponent<Animator>().Play("LPolyDrumStick");
            // this animates the stick but then the audio is played once the head object detects a collision. 
        }



        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            //change decided speed here for polyrhythms
            decidedSpeedFast = 0.2352f;//tempoSlider.value;

            //left hand only animates!!!!
            RightHand.SetActive(false);
            LeftHand.SetActive(true);



            Debug.Log("mhm" + decidedSpeedFast * 60);
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


            //LstickMovementFast.SetFloat("Lspeed", 1.5f);


            //LstickMovement.SetFloat("LstickMovementLPause.speed", decidedSpeed);
            //GetComponent<Animator>().Play("LDelayDrumStick");



            //LstickMovementFast.SetFloat("PauseSpeed", decidedSpeedFast);
            //Debug.Log("yup" LstickMovement.LPause.speed);

            GetComponent<Animator>().Play("VCpause");
            //GetComponent<Animator>().Play("LPolyDrumStick");
            // this animates the stick but then the audio is played once the head object detects a collision. 
        }
        //vc pause is 
        // press far right zero to start the left hand animation.
        // Keypad0 Numeric keypad 0.
        // in retrospect this actually might help with counting off. Get the users to really embody the 8 beat count off. 

    }
}
