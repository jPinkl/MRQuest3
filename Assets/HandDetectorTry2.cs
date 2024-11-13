using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//THE POINT OF THIS SCRIPT WAS TO TRY TO CLEAN UP Hand Detector Try 1, by moving some of the shift code into its own function (cleaning it up essentially)
//BUT THE METHOD I started to use wasn't really saving all that much... so im gonna abandon this for now MAYBE COME BACK TO IT LATER IDK....
public class HandDetectorTry2 : MonoBehaviour
{
    public GameObject referenceRHand2;
    public Vector3 Rpos2;
    public Vector3 RposAdj2;
    // this is a constant that accounts for shifts that resulted from nonidealities of making the gameobject (these were fine tuned //approximated by hand). 
    public GameObject Lprefab2;
    public Vector3 RFactor2;
    public Vector3 Lpos2;
    public Vector3 LposAdj2;
    public Vector3 LFactor2;
    public GameObject Rprefab2;
    public Vector3 upd8R;

    public bool Rside2 = false;
    public bool Lside2 = false;

    public GameObject RightInstant2;
    public GameObject LeftInstant2;
    //trying
    // GameObject clone;
    //public RigidBody clone;

    // Start is called before the first frame update
    void Start()
    {
        referenceRHand2 = GetComponent<GameObject>();
        LFactor2 = new Vector3(1.8f, -7.7f, 0.5f);
        RFactor2 = new Vector3(2.5f, -3.0f, 2.0f); // this is pretty close but fine tune even more please!!!
    }

    // Update is called once per frame
    void Update()
    {
        /*
            //instantiate right side drum and stick upon key press
            if (Input.GetKeyDown(KeyCode.R) && Rside2 == false)
            {
                RInstantiate2();
            }


            //instantiate left side drum and stick upon key press
            if (Input.GetKeyDown(KeyCode.L) && Lside2 == false)
            {
                Debug.Log("L key was pressed");
                Lpos2 = GameObject.FindGameObjectWithTag("LHand1").transform.position;
                LposAdj2 = Lpos2 + LFactor2;
                Debug.Log("Left side instantiated");
                LeftInstant2 = Instantiate(Lprefab2, LposAdj2, Quaternion.identity);
                Lside2 = true;
            }

            //microadjustment for Right Side's x-axis position positive change....
            if (Input.GetKeyDown(KeyCode.Q) && Rside2 == true)
            {
                Destroy(RightInstant2);
                RposAdj2 = RposAdj2 + new Vector3(0.07f, 0f, 0f);
                RightInstant2 = Instantiate(Rprefab2, RposAdj2, Quaternion.identity) as GameObject;
            }


            //microadjustment for Right Side's y-axis 

            //+ z axis change for right side
            if (Input.GetKeyDown(KeyCode.W) && Rside2 == true)
            {
                Destroy(RightInstant2);
                RposAdj2 = RposAdj2 + new Vector3(0f, 0f, 0.07f);
                RightInstant2 = Instantiate(Rprefab2, RposAdj2, Quaternion.identity) as GameObject;

            }
            //-z axis change for right side
            if (Input.GetKeyDown(KeyCode.E) && Rside2 == true)
            {
                Destroy(RightInstant2);
                RposAdj2 = RposAdj2 + new Vector3(0f, 0f, -0.07f);
                RightInstant2 = Instantiate(Rprefab2, RposAdj2, Quaternion.identity) as GameObject;

            }

            //-x axis change for Rside object. 
            if (Input.GetKeyDown(KeyCode.A) && Rside2 == true)
            {
                Destroy(RightInstant2);
                RposAdj2 = RposAdj2 + new Vector3(-0.07f, 0f, 0.0f);
                RightInstant2 = Instantiate(Rprefab2, RposAdj2, Quaternion.identity) as GameObject;

            }

            //LEFT SIDE BELOW
            //+ z axis change for left side

            if (Input.GetKeyDown(KeyCode.H) && Lside2 == true)
            {
                Destroy(LeftInstant2);
                LposAdj2 = LposAdj2 + new Vector3(0f, 0f, 0.07f);
                LeftInstant2 = Instantiate(Lprefab2, LposAdj2, Quaternion.identity) as GameObject;

            }

            //-z axis change for left side
            if (Input.GetKeyDown(KeyCode.J) && Lside2 == true)
            {
                Destroy(LeftInstant2);
                LposAdj2 = LposAdj2 + new Vector3(0f, 0f, -0.07f);
                LeftInstant2 = Instantiate(Lprefab2, LposAdj2, Quaternion.identity) as GameObject;

            }

            //-x axis change for left object. 
            if (Input.GetKeyDown(KeyCode.K) && Lside2 == true)
            {
                Destroy(LeftInstant2);
                LposAdj2 = LposAdj2 + new Vector3(-0.07f, 0f, 0.0f);
                LeftInstant2 = Instantiate(Lprefab2, LposAdj2, Quaternion.identity) as GameObject;
            }

            //-x axis change for left object. 
            if (Input.GetKeyDown(KeyCode.O) && Lside2 == true)
            {
                Destroy(LeftInstant2);
                Vector3 myVar = UpdateRPos(Vector3(0.07f, 0f, 0.0f));
                //LposAdj2 = LposAdj2 + new Vector3(0.07f, 0f, 0.0f);
                LeftInstant2 = Instantiate(Lprefab2, LposAdj2, Quaternion.identity) as GameObject;
            }


            //this is just a test to destroy
            if (Input.GetKeyDown(KeyCode.T) && Rside2 == true)
            {
                Destroy(RightInstant2);
                Rside2 = false;
            }

            if (Input.GetKeyDown(KeyCode.M) && Lside2 == true)
            {
                Destroy(LeftInstant2);
                Lside2 = false;
            }

        }



        void RInstantiate2()
        {
            //Debug.Log("A key was pressed");
            Rpos2 = GameObject.FindGameObjectWithTag("RHand1").transform.position;
            RposAdj2 = Rpos2 + RFactor2;
            //Rpos = referenceRHand.transform.position;
            Debug.Log("R side instantiated");
            //Instantiate(Rprefab, RposAdj, Quaternion.identity)
            //this instantiates but doesnt assign to a variable... so i referenced this video:
            //https://www.youtube.com/watch?v=LEJY0IBeKJ8

            //GameObject RightInstant1;
            RightInstant2 = Instantiate(Rprefab2, RposAdj2, Quaternion.identity) as GameObject;

            //^^^^ the above two attempts didnt work!!! cant seem to reference this object cause its a child i guess...?
            Rside2 = true;
        }

        void LInstantiate2()
        {
            //Debug.Log("A key was pressed");
            Lpos2 = GameObject.FindGameObjectWithTag("LHand1").transform.position;
            LposAdj2 = Lpos2 + LFactor2;
            //Rpos = referenceRHand.transform.position;
            Debug.Log("L side instantiated");
            //Instantiate(Rprefab, RposAdj, Quaternion.identity)
            //this instantiates but doesnt assign to a variable... so i referenced this video:
            //https://www.youtube.com/watch?v=LEJY0IBeKJ8

            //GameObject RightInstant1;
            LeftInstant2 = Instantiate(Lprefab2, LposAdj2, Quaternion.identity) as GameObject;

            //^^^^ the above two attempts didnt work!!! cant seem to reference this object cause its a child i guess...?
            Lside2 = true;
        }


        int UpdateRPos (int upd8R)
        {

            vector3 newPos = upd8R ;
            return newPos;

        }
        */

    }
}
