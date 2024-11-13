using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using MidiPlayerTK;
using TMPro;

public class RStickScript : MonoBehaviour
{
    Animator RstickMovement;
    [SerializeField] private Slider tempoSlider;
    public float decidedSpeed = 1;

    // Start is called before the first frame update
    void Start()
    {
        RstickMovement = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //getting rid of the animation space bar input...
        //
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //decidedSpeed = tempoSlider.value;
            //Debug.Log(decidedSpeed);

            RstickMovement.SetFloat("Rspeed", decidedSpeed);

            GetComponent<Animator>().Play("NewRDrumStick");
        }
    }
}
