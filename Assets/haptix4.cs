using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using OVRTouchSample;
using System;

public class haptix4 : MonoBehaviour
{
    public float time = 3f;
    // Start is called before the first frame update

    void Start()
    {
        //just a test
        //Invoke("startVibR", 0f);
        //Invoke("stopVibR", .1f);
        //Invoke("startVibL", 0f);
        //Invoke("stopVibL", .1f);

        //getting rid of invoke since we dont want to intentionally introduce delay

        Invoke("startVibR", .001f);
        Invoke("startVibL",.001f);
        Invoke("stopVibR", .2f);
        Invoke("stopVibL", .2f);
    }
    // Update is called once per frame
    void Update()
    {

    }
    public void Vib()
    {
        //Invoke("startVibR", .1f);
        //Invoke("stopVibR", .4f);
    }
    public void startVibR()
    {
        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
        Debug.Log("stay funky brosR");
    }
    public void stopVibR()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
    }

    public void startVibL()
    {
        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.LTouch);
        Debug.Log("stay funky brosL");
    }
    public void stopVibL()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
    }
}