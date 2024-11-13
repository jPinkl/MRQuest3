using System.Collections;
using System.Collections.Generic;
using UnityEngine;


    public class HandDetectorTry1 : MonoBehaviour
{
    public GameObject referenceRHand;
    public Vector3 Rpos;
    public Vector3 RposAdj;
    // this is a constant that accounts for shifts that resulted from nonidealities of making the gameobject (these were fine tuned //approximated by hand). 


    //these are the prefabs for each side, at three different stick angles each. 
    public GameObject Lprefab; //these are the shallow angles
    public GameObject Rprefab;
    public GameObject Lprefab2; //these are the extreme angles
    public GameObject Rprefab2;
    //public GameObject Lprefab3; //these are the shallow angles
    //public GameObject Rprefab3;

    public Vector3 RFactor;

    public Vector3 Lpos;
    public Vector3 LposAdj;
    public Vector3 LFactor;

    //public float RightSideRotation =0.0f;
    //public float LeftSideRotation =0.0f;
    //public Vector3 RotateRightSide = (0, 0, 0);
    Quaternion targetRotationL;
    Quaternion targetRotationR;

    public bool Rside = false;
    public bool Lside = false;


    public GameObject RightInstant1;   
    public GameObject LeftInstant1;


    //trying
    // GameObject clone;
    //public RigidBody clone;

    // Start is called before the first frame update
    void Start()
    {
        referenceRHand = GetComponent<GameObject>();
        LFactor = new Vector3(1.85f, -7.8f, 0.15f);
        //this is for the first set of prefabs (pre5.7) 
        //resetting this as an experiment
        //RFactor = new Vector3(2.5f, -3.0f, 2.0f); // this is pretty close but fine tune even more please!!!
        RFactor = new Vector3(2.85f, -3.05f, 2.2f);
        //was 5.2R but now trying 5.7NEW
    }

    // Update is called once per frame
    void Update()
    {
        //instantiate right side drum and stick upon key press
        if (Input.GetKeyDown(KeyCode.R) && Rside == false)
        {
            RInstantiate();
        }


        //instantiate left side drum and stick upon key press
        if (Input.GetKeyDown(KeyCode.L) && Lside == false)
        {
            /*
            Debug.Log("L key was pressed");
            Lpos = GameObject.FindGameObjectWithTag("LHand1").transform.position;
            LposAdj = Lpos + LFactor;
            //Rpos = referenceRHand.transform.position;
            Debug.Log("Left side instantiated");
            //GameObject cloneL;
            LeftInstant1 = Instantiate(Lprefab, LposAdj, Quaternion.identity);
            // clone = Instantiate(Lprefab, LposAdj, Quaternion.identity);
            //^^^^ the above two attempts didnt work!!! cant seem to reference this object cause its a child i guess...?
            //
            Lside = true;
            */
            LInstantiate();
        }

        //microadjustment for Right Side's x-axis position positive change....
        if (Input.GetKeyDown(KeyCode.Q) && Rside == true)
        {
            //delete the current instantiation and recreate after a small shift in position... 
            Destroy(RightInstant1);

            //RightInstant1.GetComponent<>
            RposAdj = RposAdj + new Vector3(0.05f, 0f, 0f);
            RightInstant1 = Instantiate(Rprefab, RposAdj, Quaternion.identity) as GameObject;

            //COMMENTING OUT THIS ATTEMPT
            /*
            Debug.Log("original " + RFactor);
            //Destroy(RightInstant1);
            RFactor = RFactor + new Vector3(0.1f, 0f, 0f);
            Debug.Log("updated " + RFactor);
            //clone.transform.position = RFactor;
            RightInstant1.transform.position = Vector3.MoveTowards(transform.position, RFactor, 5 * Time.deltaTime);
            */
            //RInstantiate();
            //RightInstant.transform.position = RposAdj + new Vector3(-0.75f, 0.0f, 0.0f);
        }
            //microadjustment for Right Side's y-axis 

        //+ z axis change for right side
        if (Input.GetKeyDown(KeyCode.E) && Rside == true)
        {
            Destroy(RightInstant1);
            RposAdj = RposAdj + new Vector3(0f, 0f, 0.05f);
            RightInstant1 = Instantiate(Rprefab, RposAdj, Quaternion.identity) as GameObject;
        }

        //-z axis change for right side
        if (Input.GetKeyDown(KeyCode.D) && Rside == true)
        {
            Destroy(RightInstant1);
            RposAdj = RposAdj + new Vector3(0f, 0f, -0.05f);
            RightInstant1 = Instantiate(Rprefab, RposAdj, Quaternion.identity) as GameObject;

        }

        //-x axis change for Rside object. 
        if (Input.GetKeyDown(KeyCode.A) && Rside == true)
        {
            Destroy(RightInstant1);
            RposAdj = RposAdj + new Vector3(-0.05f, 0f, 0.0f);
            RightInstant1 = Instantiate(Rprefab, RposAdj, Quaternion.identity) as GameObject;
        }

        //-y axis change for Rside object. 
        if (Input.GetKeyDown(KeyCode.S) && Rside == true)
        {
            Destroy(RightInstant1);
            RposAdj = RposAdj + new Vector3(0.0f, -0.05f, 0.0f);
            RightInstant1 = Instantiate(Rprefab, RposAdj, Quaternion.identity) as GameObject;
        }

        //+y axis change for Rside object. 
        if (Input.GetKeyDown(KeyCode.W) && Rside == true)
        {
            Destroy(RightInstant1);
            RposAdj = RposAdj + new Vector3(0.0f, 0.05f, 0.0f);
            RightInstant1 = Instantiate(Rprefab, RposAdj, Quaternion.identity) as GameObject;
        }

        //LEFT SIDE BELOW
        if (Input.GetKeyDown(KeyCode.K) && Lside == true)
        {
            Destroy(LeftInstant1);
            LposAdj = LposAdj + new Vector3(0f, 0f, 0.05f);
            LeftInstant1 = Instantiate(Lprefab, LposAdj, Quaternion.identity) as GameObject;
        }

        //-z axis change for left side
        if (Input.GetKeyDown(KeyCode.M) && Lside == true)
        {
            Destroy(LeftInstant1);
            LposAdj = LposAdj + new Vector3(0f, 0f, -0.05f);
            LeftInstant1 = Instantiate(Lprefab, LposAdj, Quaternion.identity) as GameObject;
        }

        //-x axis change for left object. 
        if (Input.GetKeyDown(KeyCode.B) && Lside == true)
        {
            Destroy(LeftInstant1);
            LposAdj = LposAdj + new Vector3(-0.05f, 0f, 0.0f);
            LeftInstant1 = Instantiate(Lprefab, LposAdj, Quaternion.identity) as GameObject;
        }

        //+x axis change for left object. 
        if (Input.GetKeyDown(KeyCode.H) && Lside == true)
        {
            Destroy(LeftInstant1);
            LposAdj = LposAdj + new Vector3(0.05f, 0f, 0.0f);
            LeftInstant1 = Instantiate(Lprefab, LposAdj, Quaternion.identity) as GameObject;
        }

        //-y axis change for left object. 
        if (Input.GetKeyDown(KeyCode.N) && Lside == true)
        {
            Destroy(LeftInstant1);
            LposAdj = LposAdj + new Vector3(0f, -0.05f, 0.0f);
            LeftInstant1 = Instantiate(Lprefab, LposAdj, Quaternion.identity) as GameObject;
        }

        //+y axis change for left object. 
        if (Input.GetKeyDown(KeyCode.J) && Lside == true)
        {
            Destroy(LeftInstant1);
            LposAdj = LposAdj + new Vector3(0f, 0.05f, 0.0f);
            LeftInstant1 = Instantiate(Lprefab, LposAdj, Quaternion.identity) as GameObject;
        }

        //ATTEMPTS FOR ROTATION
        //"T" will be to rotate right object one way, "F" will be to rotate it the other
        //NVM T will be to reinstantiate the prefab at a shallow angle... F will be to reinstantiate at a more extreme angle. 
        if (Input.GetKeyDown(KeyCode.T) && Rside == true)
        {
            Destroy(RightInstant1);
            //RightSideRotation = RightSideRotation + 10.0f;
            //RightInstant1 = Instantiate(Rprefab, RposAdj, new Quaternion(0.0f, RightSideRotation, 0.0f, 1)) as GameObject;
            RightInstant1 = Instantiate(Rprefab, RposAdj, Quaternion.identity) as GameObject;

        }

        if (Input.GetKeyDown(KeyCode.F) && Rside == true)
        {
            Destroy(RightInstant1);
            //create prefab at the extreme angle.... 
            RightInstant1 = Instantiate(Rprefab2, RposAdj, Quaternion.identity) as GameObject;
            //RightSideRotation = RightSideRotation - 10.0f;
            //RightInstant1 = Instantiate(Rprefab, RposAdj, new Quaternion(0.0f, RightSideRotation, 0.0f, 1)) as GameObject;
        }


        //maybe get rid of this!!!!

        if (Input.GetKeyDown(KeyCode.P) && Lside == true)
        {
            Destroy(LeftInstant1);
            //LeftSideRotation = LeftSideRotation - 10.0f;
            //LeftInstant1 = Instantiate(Rprefab, RposAdj, new Quaternion(0.0f, LeftSideRotation, 0.0f, 1)) as GameObject;
            LeftInstant1 = Instantiate(Lprefab, LposAdj, Quaternion.identity) as GameObject;
            //https://www.youtube.com/watch?v=4ApRQRMXVFk
            //https://www.youtube.com/watch?v=LnQudtIKfnw
            //below is my attempt to implement custom 
            //targetRotationL = Quaternion.Euler(LeftInstant1.transform.eulerAngles.x, RightInstant1.transform.eulerAngles.y + 22.75f, RightInstant1.transform.eulerAngles.z);

            //LeftInstant1 = Instantiate(Lprefab3, LposAdj, Quaternion.identity) as GameObject;
        }

        if (Input.GetKeyDown(KeyCode.O) && Lside == true) //instantiate the extreme angle 
        {
            Destroy(LeftInstant1);
            LeftInstant1 = Instantiate(Lprefab2, LposAdj, Quaternion.identity) as GameObject;
            //LeftSideRotation = LeftSideRotation + 10.0f;
            //LeftInstant1 = Instantiate(Rprefab, RposAdj, new Quaternion(0.0f, LeftSideRotation, 0.0f, 1)) as GameObject;
        }



        //this is just a test to destroy
        if (Input.GetKeyDown(KeyCode.C) && Rside == true)
        {
            Destroy(RightInstant1);
            Rside = false;
        }

        if (Input.GetKeyDown(KeyCode.I) && Lside == true)
        {
            Destroy(LeftInstant1);
            Lside = false;
        }


        /*
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Debug.Log("All removedAlpha7");

        }
        */

        //key detection works but i cant update this transform... 
    }


    void RInstantiate()
    {
        //Debug.Log("A key was pressed");
        Rpos = GameObject.FindGameObjectWithTag("RHand1").transform.position;
        RposAdj = Rpos + RFactor;
        //Rpos = referenceRHand.transform.position;
        Debug.Log("R side instantiated");
        //Instantiate(Rprefab, RposAdj, Quaternion.identity)
        //this instantiates but doesnt assign to a variable... so i referenced this video:
        //https://www.youtube.com/watch?v=LEJY0IBeKJ8

        //GameObject RightInstant1;
        //this creates the drum at the default angle!!!
        RightInstant1 = Instantiate(Rprefab, RposAdj, Quaternion.identity) as GameObject;

        //^^^^ the above two attempts didnt work!!! cant seem to reference this object cause its a child i guess...?
        Rside = true;
    }

    void LInstantiate()
    {
        //Debug.Log("A key was pressed");
        Lpos = GameObject.FindGameObjectWithTag("LHand1").transform.position;
        LposAdj = Lpos + LFactor;
        //Rpos = referenceRHand.transform.position;
        Debug.Log("L side instantiated");
        //Instantiate(Rprefab, RposAdj, Quaternion.identity)
        //this instantiates but doesnt assign to a variable... so i referenced this video:
        //https://www.youtube.com/watch?v=LEJY0IBeKJ8

        //GameObject RightInstant1;
        LeftInstant1 = Instantiate(Lprefab, LposAdj, Quaternion.identity) as GameObject;

        //^^^^ the above two attempts didnt work!!! cant seem to reference this object cause its a child i guess...?
        Lside = true;
    }
}
