/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using MidiPlayerTK;
// random comment
namespace DemoMPTK
{
    public class TestMidiKeyboardOld : MonoBehaviour
    {
        public GameObject prefabLight;
        public float tempoActual;
        public InputInt InputIndexDevice;
        public InputInt InputChannel;
        public InputInt InputPreset;
        public InputInt InputNote;
        public Toggle ToggleMidiRead;
        public Toggle ToggleRealTimeRead;
        public Toggle ToggleMsgSystem;
        public Text TextSendNote;
        public Text TextAlertRT;
        public Text TextCountEventQueue;
        public MidiStreamPlayer midiStreamPlayer;
        public float startTime;
        public float strikeTime;
        public GameObject redlightR;
        public GameObject redlightL;
        public GameObject GrlightR;
        public GameObject GrlightL;
        public float decidedSpeed;
        private float idealStrike;
        //[SerializeField] private Slider tempoSlider;
        //this is for the drop down menu
        //actually all this code should be placed in the stick objects. 
        //Since these control the animator!!!
        
        //public TMPro.TMP_Dropdown mydrop;
        //public float handSpeed;
        //public float leftHandDelay;
        
        public float DelayToRefreshDeviceMilliSeconds = 1000f;

        float timeTorefresh;

        private void Start()
        {
            // Midi Keyboard need to be initialized at start
            MidiKeyboard.MPTK_Init();

            // Log version of the Midi plugins
            Debug.Log(MidiKeyboard.MPTK_Version());


            //error here
            TextAlertRT.enabled = false;
            // Open or close all Midi Input Devices
            ToggleMidiRead.onValueChanged.AddListener((bool state) =>
            {
                if (state)
                    MidiKeyboard.MPTK_OpenAllInp();
                else
                    MidiKeyboard.MPTK_CloseAllInp();
                CheckStatus($"Open/close all input");

            });

            ToggleRealTimeRead.onValueChanged.AddListener((bool state) =>
            {
                if (state)
                {
                    TextAlertRT.enabled = true;
                    //Debug.Log($"MPTK_RealTimeRead {realTimeRead} --> {value}");
                    MidiKeyboard.OnActionInputMidi += ProcessEvent;
                    MidiKeyboard.MPTK_SetRealTimeRead();
                }
                else
                {
                    TextAlertRT.enabled = false;
                    MidiKeyboard.OnActionInputMidi -= ProcessEvent;
                    MidiKeyboard.MPTK_UnsetRealTimeRead();
                }
            });

            // Read or not system message (not sysex)
            ToggleMsgSystem.onValueChanged.AddListener((bool state) =>
            {
                MidiKeyboard.MPTK_ExcludeSystemMessage(state);
            });

            InputNote.OnEventValue.AddListener((int val) =>
            {
                TextSendNote.text = "Send Note " + HelperNoteLabel.LabelFromMidi(val);
            });

            // read preset value and send a midi message to change preset on the device 'index"
            InputPreset.OnEventValue.AddListener((int val) =>
            {
                int index = InputIndexDevice.Value;

                // send a patch change
                MPTKEvent midiEvent = new MPTKEvent()
                {
                    Command = MPTKCommand.PatchChange,
                    Value = InputPreset.Value,
                    Channel = InputChannel.Value,
                    Delay = 0,
                };
                // is this 
                MidiKeyboard.MPTK_PlayEvent(midiEvent, index);


                CheckStatus($"Play PatchChange {index}");
            });
        }

        private void OnApplicationQuit()
        {
            //Debug.Log("OHHHHH");
            Debug.Log("OnApplicationQuit " + Time.time + " seconds");
            MidiKeyboard.MPTK_UnsetRealTimeRead();
            MidiKeyboard.MPTK_CloseAllInp();
            CheckStatus($"Close all input");
        }

        /// <summary>@brief
        /// Log input and output midi device
        /// </summary>
        public void RefreshDevices()
        {
            //Debug.Log("OHHHHH");
            Debug.Log($"Midi Input: {MidiKeyboard.MPTK_CountInp()} device");
            for (int i = 0; i < MidiKeyboard.MPTK_CountInp(); i++)
                Debug.Log($"   Index {i} - {MidiKeyboard.MPTK_GetInpName(i)}");

            Debug.Log($"Midi Output: {MidiKeyboard.MPTK_CountOut()} device");
            for (int i = 0; i < MidiKeyboard.MPTK_CountOut(); i++)
                Debug.Log($"   Index {i} - {MidiKeyboard.MPTK_GetOutName(i)}");
        }

        /// <summary>@brief
        /// Open a device for output. The index is the same read with MPTK_GetOutName
        /// </summary>
        public void OpenDevice()
        {
            int index = 0;
            try
            {
                index = InputIndexDevice.Value;
                MidiKeyboard.MPTK_OpenOut(index);
                CheckStatus($"Open Device {index}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{ex.Message}");
            }
        }

        public void PlayRandomNote()
        {
            PlayOneNote(UnityEngine.Random.Range(-12, +12));
        }

        public void PlayOneNote(int random)
        {

            //Debug.Log("OHHHHH");
            MPTKEvent midiEvent;

            int index = InputIndexDevice.Value;

            // playing a NoteOn


            midiEvent = new MPTKEvent()

            {
                Command = MPTKCommand.NoteOn,
                Value = InputNote.Value + random,
                Channel = InputChannel.Value,
                Velocity = 0x64, // Sound can vary depending on the velocity
                Delay = 0,
            };

            
            //MidiKeyboard.MPTK_PlayEvent(midiEvent, index);
            //CheckStatus($"Play NoteOn {index}");
            

            // Send Notoff with a delay of 2 seconds
            midiEvent = new MPTKEvent()
            {
                Command = MPTKCommand.NoteOff,
                Value = InputNote.Value + random,
                Channel = InputChannel.Value,
                Velocity = 0,
                Delay = 2000,
            };
            MidiKeyboard.MPTK_PlayEvent(midiEvent, index);
            // When event is delayed, last status is sent when event is send, so after the delay!
        }

        public void CloseDevice()
        {
            int index = 0;
            try
            {
                index = InputIndexDevice.Value;

                MidiKeyboard.MPTK_CloseOut(index);
                CheckStatus($"Close Device {index}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{ex.Message}");
            }
        }

        private static bool CheckStatus(string message)
        {
            MidiKeyboard.PluginError status = MidiKeyboard.MPTK_LastStatus;
            if (status == MidiKeyboard.PluginError.OK)
            {
                Debug.Log(message + " ok");
                return true;
            }
            else
            {
                Debug.Log(message + $" KO - {status}");
                return false;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                
                //gettempo
                //if (mydrop.value == 0)
                //{
                 //   //noaction... tempo is equal to 60bpm
                //}
                //if (mydrop.value == 1)
                //{
                //    //tempo is 120
               // }
                //
                //this is commented out. To be implemented in the stick objects!!!!
                startTime = Time.time;
                //decidedSpeed = tempoSlider.value;
                decidedSpeed = 2f;
                tempoActual = decidedSpeed * 60.0f;

                //i needed to put the f after 60 here. make sure im properly multiplying floats by floats. 

                //Debug.Log("OOOOOHHHHHHH");
            }
            int count = 0;
            try
            {
                TextCountEventQueue.text = $"Read queue: {MidiKeyboard.MPTK_SizeReadQueue()}";
                if (ToggleMidiRead.isOn && !ToggleRealTimeRead.isOn)
                {
                    // Check every timeTorefresh millisecond if a new device is connected or is disconnected
                    if (Time.fixedUnscaledTime > timeTorefresh)
                    {
                        //Debug.Log("OHHHHH11111");
                        timeTorefresh = Time.fixedUnscaledTime + DelayToRefreshDeviceMilliSeconds / 1000f;
                        //Debug.Log(Time.fixedUnscaledTime);
                        // Open or refresh midi input 
                        MidiKeyboard.MPTK_OpenAllInp();
                        MidiKeyboard.PluginError status = MidiKeyboard.MPTK_LastStatus;
                        if (status != MidiKeyboard.PluginError.OK)
                            Debug.LogWarning($"Midi Keyboard error, status: {status}");
                    }

                    // Process the message queue by max 100 to avoid locking Unity
                    while (count < 100)
                    {
                        count++;

                        // Parse the message.
                        MPTKEvent midievent = MidiKeyboard.MPTK_Read();

                        // No more Midi message
                        //editted this line!! ignore midi events of note off
                        //juanca helped figure out that midievent.Command was needed , not midievent only
                        if (midievent == null || midievent.Command == MPTKCommand.NoteOff) // MPTKEvent.NoteOff)//MPTKEvent.NoteOff midievent ==  InputNote.NoteOff) //MPTKCommand.NoteOff)//.NoteOff )//MPTKEvent.NoteOff)
                            break;

                        // Active Sensing. This message is intended to be sent repeatedly to tell the receiver that a connection is alive
                        // Now this message can be filter with MPTK_ExcludeSystemMessage
                        //if (midievent.Command == MPTKCommand.AutoSensing) continue;

                        ProcessEvent(midievent);
                    }
                }
            }
            catch (System.Exception ex)
            {
                //MidiPlayerGlobal.ErrorDetail(ex);

                //uncomment this !!!
                //Debug.LogError(ex.Message);

            }
        }

        private void ProcessEvent(MPTKEvent midievent)
        {
            GrlightL.SetActive(false);
            GrlightR.SetActive(false);
            redlightL.SetActive(false);
            redlightR.SetActive(false);

            midiStreamPlayer.MPTK_PlayDirectEvent(midievent);
            Debug.Log($"[{DateTime.UtcNow.Millisecond:00000}] {midievent}");

            
            // this is the moment in code where the realtime midi is sent 
            //strikeTime = Time.time - startTime;
            //printing the timing of the hit
            //Debug.Log(strikeTime);
            //int iStrike = (int)strikeTime;
            //printing the integerized timing of the hit
            //Debug.Log(iStrike);
            //float jitter = strikeTime - iStrike;

            

            //editting the above code to make it modular for all tempos
            //strikeTime = Time.time - startTime;
            //float idealStrike = (((int)strikeTime)*(decidedSpeed))+startTime;
            //float jitter = strikeTime - idealStrike;

            //second try with adjustable tempo
            //strikeTime = Time.time - startTime- .13f;

            //idk what .13 is..... is it the time it takes to hit the head?

            //float idealStrike = (decidedSpeed + startTime) +((strikeTime/decidedSpeed)-1)*(1/decidedSpeed);
            //float idealStrike = (decidedSpeed*5/6) + ((int)(strikeTime/decidedSpeed)-1) * (1 / decidedSpeed);
            //float idealStrike = (((int)(strikeTime / decidedSpeed)-1)* (decidedSpeed))+(decidedSpeed*.833333f);



            // previous code here: before 7.18.2023
            //strikeTime = Time.time - startTime - .13f*decidedSpeed;
            //float idealStrike = ((Mathf.Round(strikeTime / decidedSpeed)) * (decidedSpeed)) ;
            //float jitter = strikeTime - idealStrike;


            //new code here 7.18.2023
            //strikeTime = Time.time - startTime;
            //float idealStrike = ((Mathf.Round(strikeTime *2)) / 2) ;
            //float jitter = strikeTime - idealStrike;



            //new code 7.20.2023
            strikeTime = Time.time - startTime;
            float idealStrike = ((Mathf.Round(strikeTime * 2)) / 2) - .067f; // this is the coeffcient based on the delay
            // of starting the video and animations
            //this is a bandaid fix but seems to work....
            // beware of taking off headset after pressing space bar...
            // video WILL out of sync with the drumming. 
            //leading to wrong stick interpretations

            //float idealStrike = ((Mathf.Round(strikeTime * 2)) / 2);
            float jitter = strikeTime - idealStrike;




            Debug.Log("decidedspeed " + decidedSpeed);
            Debug.Log("strikeTime " + strikeTime);
            Debug.Log("idealStrike " + idealStrike); //this one isnt working!! NaN everytime
            Debug.Log("jitter " + jitter);     //this one isnt working!! NaN everytime
                                               //if (Mathf.Abs(jitter) < .5*decidedSpeed && Mathf.Abs(jitter) > .2 *decidedSpeed)




            // previous code here: before 7.18.2023

            









            //after speaking with julian in april 2024, we decided to simplify to lighten the cognitive load.
            //so instead of notifying user if theyare ahead or behind, they will instead get a notice in the form of a red light (prefab)
            // when they are off. simply off (not behind or ahead).
            if ((jitter < .25 && jitter > .15)|| (jitter < -.15 && jitter > -.25))
            {
                Debug.Log("user is offbeat");
                //redlightL.SetActive(true);
                //redlightR.SetActive(true);

                Instantiate(prefabLight, new Vector3(0, 0, 0), Quaternion.identity);
                Destroy(prefabLight, 1);
                //GrlightL.SetActive(true);
                //GrlightR.SetActive(true);
            }
            else
            {
                Debug.Log("ON TIME");
                //GrlightL.SetActive(false);
                //GrlightR.SetActive(false);
                //redlightL.SetActive(false);
                //redlightR.SetActive(false);
            }



        }

        public void GotoWeb(string uri)
        {
            Application.OpenURL(uri);
        }
    }
}
*/


//THE ABOVE SHOULD BE UNTAINTED CODE THAT WORKS
//ITS 4.26 AND I WANT TO REPLACE THE RED LIGHT PREFAB WITH SOMETHING ELSE.... 
//SO JUST TO BE SAFE IM DOING IT IN A NEW COPIED AND PASTED SCRIPT... 


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using MidiPlayerTK;
// random comment
namespace DemoMPTK
{
    public class TestMidiKeyboardOld : MonoBehaviour
    {
        public GameObject PosFB;
        public GameObject NegFB;


        public GameObject prefabLight;
        public GameObject prefabFBpos; // as of April 2024, this is the newest way to give feedback to user on current status of performance. 
        public GameObject prefabFBneg; 
        public float tempoActual;
        public InputInt InputIndexDevice;
        public InputInt InputChannel;
        public InputInt InputPreset;
        public InputInt InputNote;
        public Toggle ToggleMidiRead;
        public Toggle ToggleRealTimeRead;
        public Toggle ToggleMsgSystem;
        public Text TextSendNote;
        public Text TextAlertRT;
        public Text TextCountEventQueue;
        public MidiStreamPlayer midiStreamPlayer;
        public float startTime;
        public float strikeTime;
        public GameObject redlightR;
        public GameObject redlightL;
        public GameObject GrlightR;
        public bool isPoly;

        public GameObject GrlightL;
        public float decidedSpeed;
        private float idealStrike;
        public bool feedbacker = false;
        //[SerializeField] private Slider tempoSlider;
        //this is for the drop down menu
        //actually all this code should be placed in the stick objects. 
        //Since these control the animator!!!
        /*
        public TMPro.TMP_Dropdown mydrop;
        public float handSpeed;
        public float leftHandDelay;
        */
        public float DelayToRefreshDeviceMilliSeconds = 1000f;

        float timeTorefresh;

        private void Start()
        {
            // Midi Keyboard need to be initialized at start
            MidiKeyboard.MPTK_Init();

            // Log version of the Midi plugins
            Debug.Log(MidiKeyboard.MPTK_Version());


            //error here
            TextAlertRT.enabled = false;
            // Open or close all Midi Input Devices
            ToggleMidiRead.onValueChanged.AddListener((bool state) =>
            {
                if (state)
                    MidiKeyboard.MPTK_OpenAllInp();
                else
                    MidiKeyboard.MPTK_CloseAllInp();
                CheckStatus($"Open/close all input");

            });

            ToggleRealTimeRead.onValueChanged.AddListener((bool state) =>
            {
                if (state)
                {
                    TextAlertRT.enabled = true;
                    //Debug.Log($"MPTK_RealTimeRead {realTimeRead} --> {value}");
                    MidiKeyboard.OnActionInputMidi += ProcessEvent;
                    MidiKeyboard.MPTK_SetRealTimeRead();
                }
                else
                {
                    TextAlertRT.enabled = false;
                    MidiKeyboard.OnActionInputMidi -= ProcessEvent;
                    MidiKeyboard.MPTK_UnsetRealTimeRead();
                }
            });

            // Read or not system message (not sysex)
            ToggleMsgSystem.onValueChanged.AddListener((bool state) =>
            {
                MidiKeyboard.MPTK_ExcludeSystemMessage(state);
            });

            InputNote.OnEventValue.AddListener((int val) =>
            {
                TextSendNote.text = "Send Note " + HelperNoteLabel.LabelFromMidi(val);
            });

            // read preset value and send a midi message to change preset on the device 'index"
            InputPreset.OnEventValue.AddListener((int val) =>
            {
                int index = InputIndexDevice.Value;

                // send a patch change
                MPTKEvent midiEvent = new MPTKEvent()
                {
                    Command = MPTKCommand.PatchChange,
                    Value = InputPreset.Value,
                    Channel = InputChannel.Value,
                    Delay = 0,
                };
                // is this 
                MidiKeyboard.MPTK_PlayEvent(midiEvent, index);


                CheckStatus($"Play PatchChange {index}");
            });
        }

        private void OnApplicationQuit()
        {
            //Debug.Log("OHHHHH");
            Debug.Log("OnApplicationQuit " + Time.time + " seconds");
            MidiKeyboard.MPTK_UnsetRealTimeRead();
            MidiKeyboard.MPTK_CloseAllInp();
            CheckStatus($"Close all input");
        }

        /// <summary>@brief
        /// Log input and output midi device
        /// </summary>
        public void RefreshDevices()
        {
            //Debug.Log("OHHHHH");
            Debug.Log($"Midi Input: {MidiKeyboard.MPTK_CountInp()} device");
            for (int i = 0; i < MidiKeyboard.MPTK_CountInp(); i++)
                Debug.Log($"   Index {i} - {MidiKeyboard.MPTK_GetInpName(i)}");

            Debug.Log($"Midi Output: {MidiKeyboard.MPTK_CountOut()} device");
            for (int i = 0; i < MidiKeyboard.MPTK_CountOut(); i++)
                Debug.Log($"   Index {i} - {MidiKeyboard.MPTK_GetOutName(i)}");
        }

        /// <summary>@brief
        /// Open a device for output. The index is the same read with MPTK_GetOutName
        /// </summary>
        public void OpenDevice()
        {
            int index = 0;
            try
            {
                index = InputIndexDevice.Value;
                MidiKeyboard.MPTK_OpenOut(index);
                CheckStatus($"Open Device {index}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{ex.Message}");
            }
        }

        public void PlayRandomNote()
        {
            PlayOneNote(UnityEngine.Random.Range(-12, +12));
        }

        public void PlayOneNote(int random)
        {

            //Debug.Log("OHHHHH");
            MPTKEvent midiEvent;

            int index = InputIndexDevice.Value;

            // playing a NoteOn


            midiEvent = new MPTKEvent()

            {
                Command = MPTKCommand.NoteOn,
                Value = InputNote.Value + random,
                Channel = InputChannel.Value,
                Velocity = 0x64, // Sound can vary depending on the velocity
                Delay = 0,
            };

            /*
            MidiKeyboard.MPTK_PlayEvent(midiEvent, index);
            CheckStatus($"Play NoteOn {index}");
            */

            // Send Notoff with a delay of 2 seconds
            midiEvent = new MPTKEvent()
            {
                Command = MPTKCommand.NoteOff,
                Value = InputNote.Value + random,
                Channel = InputChannel.Value,
                Velocity = 0,
                Delay = 2000,
            };
            MidiKeyboard.MPTK_PlayEvent(midiEvent, index);
            // When event is delayed, last status is sent when event is send, so after the delay!
        }

        public void CloseDevice()
        {
            int index = 0;
            try
            {
                index = InputIndexDevice.Value;

                MidiKeyboard.MPTK_CloseOut(index);
                CheckStatus($"Close Device {index}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{ex.Message}");
            }
        }

        private static bool CheckStatus(string message)
        {
            MidiKeyboard.PluginError status = MidiKeyboard.MPTK_LastStatus;
            if (status == MidiKeyboard.PluginError.OK)
            {
                Debug.Log(message + " ok");
                return true;
            }
            else
            {
                Debug.Log(message + $" KO - {status}");
                return false;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {

                //may 2024 addition
                // the scene has started so NOW we want feedback prefabs to appear....
                feedbacker = true;
                /*
                //gettempo
                if (mydrop.value == 0)
                {
                    //noaction... tempo is equal to 60bpm
                }
                if (mydrop.value == 1)
                {
                    //tempo is 120
                }
                */
                //this is commented out. To be implemented in the stick objects!!!!
                startTime = Time.time;
                //decidedSpeed = tempoSlider.value;
                decidedSpeed = 2f;
                tempoActual = decidedSpeed * 60.0f;

                //i needed to put the f after 60 here. make sure im properly multiplying floats by floats. 

                //Debug.Log("OOOOOHHHHHHH");
            }
            int count = 0;
            try
            {
                TextCountEventQueue.text = $"Read queue: {MidiKeyboard.MPTK_SizeReadQueue()}";
                if (ToggleMidiRead.isOn && !ToggleRealTimeRead.isOn)
                {
                    // Check every timeTorefresh millisecond if a new device is connected or is disconnected
                    if (Time.fixedUnscaledTime > timeTorefresh)
                    {
                        //Debug.Log("OHHHHH11111");
                        timeTorefresh = Time.fixedUnscaledTime + DelayToRefreshDeviceMilliSeconds / 1000f;
                        //Debug.Log(Time.fixedUnscaledTime);
                        // Open or refresh midi input 
                        MidiKeyboard.MPTK_OpenAllInp();
                        MidiKeyboard.PluginError status = MidiKeyboard.MPTK_LastStatus;
                        if (status != MidiKeyboard.PluginError.OK)
                            Debug.LogWarning($"Midi Keyboard error, status: {status}");
                    }

                    // Process the message queue by max 100 to avoid locking Unity
                    while (count < 100)
                    {
                        count++;

                        // Parse the message.
                        MPTKEvent midievent = MidiKeyboard.MPTK_Read();

                        // No more Midi message
                        //editted this line!! ignore midi events of note off
                        //juanca helped figure out that midievent.Command was needed , not midievent only
                        if (midievent == null || midievent.Command == MPTKCommand.NoteOff) // MPTKEvent.NoteOff)//MPTKEvent.NoteOff midievent ==  InputNote.NoteOff) //MPTKCommand.NoteOff)//.NoteOff )//MPTKEvent.NoteOff)
                            break;

                        // Active Sensing. This message is intended to be sent repeatedly to tell the receiver that a connection is alive
                        // Now this message can be filter with MPTK_ExcludeSystemMessage
                        //if (midievent.Command == MPTKCommand.AutoSensing) continue;

                        ProcessEvent(midievent);
                    }
                }
            }
            catch (System.Exception ex)
            {
                //MidiPlayerGlobal.ErrorDetail(ex);

                //uncomment this !!!
                //Debug.LogError(ex.Message);

            }
        }

        private void ProcessEvent(MPTKEvent midievent)
        {
            GrlightL.SetActive(false);
            GrlightR.SetActive(false);
            redlightL.SetActive(false);
            redlightR.SetActive(false);

            midiStreamPlayer.MPTK_PlayDirectEvent(midievent);
            Debug.Log($"[{DateTime.UtcNow.Millisecond:00000}] {midievent}");

            /*
            // this is the moment in code where the realtime midi is sent 
            strikeTime = Time.time - startTime;
            //printing the timing of the hit
            //Debug.Log(strikeTime);
            int iStrike = (int)strikeTime;
            //printing the integerized timing of the hit
            //Debug.Log(iStrike);
            float jitter = strikeTime - iStrike;

            */

            //editting the above code to make it modular for all tempos
            //strikeTime = Time.time - startTime;
            //float idealStrike = (((int)strikeTime)*(decidedSpeed))+startTime;
            //float jitter = strikeTime - idealStrike;

            //second try with adjustable tempo
            //strikeTime = Time.time - startTime- .13f;

            //idk what .13 is..... is it the time it takes to hit the head?

            //float idealStrike = (decidedSpeed + startTime) +((strikeTime/decidedSpeed)-1)*(1/decidedSpeed);
            //float idealStrike = (decidedSpeed*5/6) + ((int)(strikeTime/decidedSpeed)-1) * (1 / decidedSpeed);
            //float idealStrike = (((int)(strikeTime / decidedSpeed)-1)* (decidedSpeed))+(decidedSpeed*.833333f);



            // previous code here: before 7.18.2023
            //strikeTime = Time.time - startTime - .13f*decidedSpeed;
            //float idealStrike = ((Mathf.Round(strikeTime / decidedSpeed)) * (decidedSpeed)) ;
            //float jitter = strikeTime - idealStrike;


            //new code here 7.18.2023
            //strikeTime = Time.time - startTime;
            //float idealStrike = ((Mathf.Round(strikeTime *2)) / 2) ;
            //float jitter = strikeTime - idealStrike;



            //new code 7.20.2023
            strikeTime = Time.time - startTime;
            float idealStrike = ((Mathf.Round(strikeTime * 2)) / 2) - .067f; // this is the coeffcient based on the delay
            // of starting the video and animations
            //this is a bandaid fix but seems to work....
            // beware of taking off headset after pressing space bar...
            // video WILL out of sync with the drumming. 
            //leading to wrong stick interpretations

            //float idealStrike = ((Mathf.Round(strikeTime * 2)) / 2);
            float jitter = strikeTime - idealStrike;




            Debug.Log("decidedspeed " + decidedSpeed);
            Debug.Log("strikeTime " + strikeTime);
            Debug.Log("idealStrike " + idealStrike); //this one isnt working!! NaN everytime
            Debug.Log("jitter " + jitter);     //this one isnt working!! NaN everytime
                                               //if (Mathf.Abs(jitter) < .5*decidedSpeed && Mathf.Abs(jitter) > .2 *decidedSpeed)




            // previous code here: before 7.18.2023
            /*
            if (jitter < .5 * decidedSpeed && jitter > .35 * decidedSpeed)
            {
                Debug.Log("BEHIND THE BEAT!!!");
                redlightL.SetActive(false);
                redlightR.SetActive(false);
                GrlightL.SetActive(true);
                GrlightR.SetActive(true);
            }
            else if (jitter < -.2*decidedSpeed && jitter > -.5*decidedSpeed)
            {
                Debug.Log("AHEAD OF THE BEAT!!!");
                GrlightL.SetActive(false);
                GrlightR.SetActive(false);
                redlightL.SetActive(true);
                redlightR.SetActive(true);
            }
            else
            {
                Debug.Log("ON TIME");
                GrlightL.SetActive(false);
                GrlightR.SetActive(false);
                redlightL.SetActive(false);
                redlightR.SetActive(false);
            }
            */





            //new code here 7.18.2023
            //commenting this out! see below for why.....
            /*
            if (jitter < .25 && jitter > .15)
            {
                Debug.Log("BEHIND THE BEAT!!!");
                redlightL.SetActive(false);
                redlightR.SetActive(false);
                GrlightL.SetActive(true);
                GrlightR.SetActive(true);
            }
            else if (jitter < -.15 && jitter > -.25)
            {
                Debug.Log("AHEAD OF THE BEAT!!!");
                GrlightL.SetActive(false);
                GrlightR.SetActive(false);
                redlightL.SetActive(true);
                redlightR.SetActive(true);
            }
            else
            {
                Debug.Log("ON TIME");
                GrlightL.SetActive(false);
                GrlightR.SetActive(false);
                redlightL.SetActive(false);
                redlightR.SetActive(false);
            }
            */


            //after speaking with julian in april 2024, we decided to simplify to lighten the cognitive load.
            //so instead of notifying user if theyare ahead or behind, they will instead get a notice in the form of a red light (prefab)
            // when they are off. simply off (not behind or ahead).

            //adding this check for bool in order to display fb prefabs
            if(feedbacker == true)
            {
                if ((jitter < .35 && jitter > .3) || (jitter < -.3 && jitter > -.35))
                {
                    if(isPoly ==false)
                    {
                        Debug.Log("user is offbeat");
                        //redlightL.SetActive(true);
                        //redlightR.SetActive(true);

                        Destroy(PosFB);
                        Destroy(NegFB);
                        //Instantiate(prefabLight, new Vector3(0, 0, 0), Quaternion.identity);
                        //^^ commented this out bc were no longer using this method of feedback. 

                        //NegFB = Instantiate(prefabFBneg, new Vector3(0, 2f, 0), transform.rotation);
                        NegFB = Instantiate(prefabFBneg, new Vector3(-.156f, 2.503f, 2.42f), Quaternion.Euler(0, 0, 180));


                        Destroy(prefabLight, 1);
                        //GrlightL.SetActive(true);
                        //GrlightR.SetActive(true);
                    }

                }
                else
                {


                    if (isPoly == false)
                    {
                        Destroy(PosFB);
                        Destroy(NegFB);

                        //Debug.Log("ON TIME");
                        PosFB = Instantiate(prefabFBpos, new Vector3(-.156f, 2.503f, 2.42f), Quaternion.Euler(0, 0, 180));
                    }
                    
                    //PosFB = Instantiate(prefabFBpos, transform.position, transform.rotation);


                    //GrlightL.SetActive(false);
                    //GrlightR.SetActive(false);
                    //redlightL.SetActive(false);
                    //redlightR.SetActive(false);
                }
            }




        }

        public void GotoWeb(string uri)
        {
            Application.OpenURL(uri);
        }
    }
}






