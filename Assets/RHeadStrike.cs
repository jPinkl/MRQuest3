using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RHeadStrike : MonoBehaviour
{
    public AudioSource rightMembranophone;
    public AudioClip rightSample;
    public GameObject LightR1;


    //for the timer (tracking if the strike is ahead or behind the beat)

    public Text timertext;
    private float startTime;


    //[SerializeField] private Animator RightDrumHead1;
    //[SerializeField] private string DoorOpen = RdrumHead;

    // Start is called before the first frame update
    void OnCollisionEnter (Collision collision)
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            startTime = Time.time;

        }

            //used this line for a while but dont need anymore / right now
            //Debug.Log("Enter Right drum head");
            rightMembranophone.PlayOneShot(rightSample);
        GetComponent<Animator>().Play("RdrumHead");
        //RightDrumHead1.Play(DoorOpen);
        LightR1.SetActive(true);

    }

    private void OnCollisionStay(Collision collision)
    {
        //used this line for a while but dont need anymore / right now
        //Debug.Log("Stay Right drum head");
    }

    void OnCollisionExit(Collision collision)
    {
        //used this line for a while but dont need anymore / right now
        //Debug.Log("Exit right drum head");
        //GetComponent<Animator>().StopPlayback("RdrumHead");
        LightR1.SetActive(false);
    }

}
