using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using MidiPlayerTK;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit; //dont need since ill be using a different 


public class LStickScriptFast : MonoBehaviour
{
    //4.19 stuff for haptic that i dont think ill end up using here. 
    //these are for trying out haptic triggering based on drum stick positions!!!!
    //from this video !! using unityengine.xr.interaction.toolkit 2:30~
    //[Range(0, 1)]
    //public float intensity;
    //public float duration;

    //public TMP_Dropdown myDrop;
    Animator LstickMovementFast;
    [SerializeField] private float speed, jumpSpeed;
    [SerializeField] private Slider tempoSlider;
    public bool isLeft;

    public float decidedSpeedFast = 0.2352f;



    //public float handSpeed;
    public float leftHandDelayFast;


    // Start is called before the first frame update
    void Start()
    {
        LstickMovementFast = GetComponent<Animator>();
        //XRBaseInteractable interactable = GetComponent<XRBaseInteractable>();
        //interactable.activated.AddListener(TriggerHaptic);
        //but this needs a function too!!! so TriggerHaptic below is created too.
    }


    /*
    //new 4.19.2024 for controlling haptics!
    public void TriggerHaptic(BaseInteractionEventArgs eventArgs)
    {
        if (eventArgs.interactorObject is XRBaseControllerInteractor controllerInteractor)
        {
            //needs to convert to XR based controller
            TriggerHaptic(controllerInteractor.xrController);
        }
    }

    // this needs to be triggered!!!
    public void TriggerHaptic(XRBaseController controller)
    {
        //i think here is where the haptic signal is sent!
        if (intensity > 0)
        {
            controller.SendHapticImpulse(intensity, duration);
        }
    }
    //HOWEVER the above needs to be triggered when the "interactable is activated"
    */



    // Update is called once per frame
    void Update()
    {
        // getting rid of the keycode and just starting it automatcially
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //change decided speed here for polyrhythms
            decidedSpeedFast = 0.2352f;//tempoSlider.value;
            //TriggerHaptic(controllerInteractor.xrController);

            Debug.Log("mhm" + decidedSpeedFast*60);
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
            LstickMovementFast.SetFloat("Lspeed", 1.5f);
            //LstickMovement.SetFloat("LstickMovementLPause.speed", decidedSpeed);
            //GetComponent<Animator>().Play("LDelayDrumStick");



            //LstickMovementFast.SetFloat("PauseSpeed", decidedSpeedFast);
            //Debug.Log("yup" LstickMovement.LPause.speed);
            if (isLeft== true)
            {
                GetComponent<Animator>().Play("pause2024L");
            }
            else
            {
                GetComponent<Animator>().Play("pause2024R");
            }
            //GetComponent<Animator>().Play("LPolyDrumStick");
            // this animates the stick but then the audio is played once the head object detects a collision. 
        }

        //vc pause is 
        // press far right zero to start the left hand animation.
        // Keypad0 Numeric keypad 0.
        // in retrospect this actually might help with counting off. Get the users to really embody the 8 beat count off. 
        


    }
    /*
    */
}
