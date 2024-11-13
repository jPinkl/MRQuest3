using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserManipCylinder : MonoBehaviour
{
    AudioSource cylAudioSource; 
    //public float cylAudioVol = cylAudioSource.volume;
    //public float cylGain;
    public float cylinderVolume = 1f; // this is volume of the object, NOT audio gain
    public float cylinderVolScaled;
    // Start is called before the first frame update
    void Start()
    {
        cylAudioSource = GetComponent<AudioSource>();

    }

    // Update is called once per frame
    void Update()
    {
        //volume calculation
        //V=πr2h
        //cylinder has a collider so use that:
        Vector3 cylinderDimensions = GetComponent<Collider>().bounds.size;
        cylinderVolume = 3.1415f*cylinderDimensions.x*cylinderDimensions.y;
        //.02 min 1.15 max
        cylinderVolScaled = cylinderVolume/1.15f;
        cylAudioSource.volume = cylinderVolScaled; 
    }



}
