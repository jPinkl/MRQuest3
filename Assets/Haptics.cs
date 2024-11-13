using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using MidiPlayerTK;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit; //dont need since ill be using a different 

public class Haptics : MonoBehaviour
{
    // t his is based on first reply of this:
    //https://communityforums.atmeta.com/t5/Unity-VR-Development/the-quot-OVRHapticsClip-quot-can-t-work-help-me-to-write-some/td-p/493037
    OVRHapticsClip hapticsClip;
    public AudioClip pickupClip;

    public void activatePickUpHaptics()
    {
        hapticsClip = new OVRHapticsClip(pickupClip);
        OVRHaptics.RightChannel.Mix(hapticsClip);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            activatePickUpHaptics();
            Debug.Log("whyyyyyyy");

        }
    }

    /*
    public static HapticsDesc GetControllerHapticsDesc(uint controllerMask)
    {
        if (version >= OVRP_1_6_0.version)
        {
            return OVRP_1_6_0.ovrp_GetControllerHapticsDesc(controllerMask);
        }
        else
        {
            //return new HapticsDesc();
        }
    }
    */

}