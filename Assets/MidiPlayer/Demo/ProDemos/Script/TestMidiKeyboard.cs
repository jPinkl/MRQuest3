using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using MidiPlayerTK;

namespace DemoMPTK
{
    public class TestMidiKeyboard : MonoBehaviour
    {

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

        public float DelayToRefreshDeviceMilliSeconds = 1000f;

        float timeTorefresh;

        private void Start()
        {
            // Midi Keyboard need to be initialized at start
            MidiKeyboard.MPTK_Init();

            // Log version of the Midi plugins
            Debug.Log(MidiKeyboard.MPTK_Version());

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
                MidiKeyboard.MPTK_PlayEvent(midiEvent, index);
                CheckStatus($"Play PatchChange {index}");
            });
        }

        private void OnApplicationQuit()
        {
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
            MidiKeyboard.MPTK_PlayEvent(midiEvent, index);
            CheckStatus($"Play NoteOn {index}");

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
            int count = 0;
            try
            {
                TextCountEventQueue.text = $"Read queue: {MidiKeyboard.MPTK_SizeReadQueue()}";
                if (ToggleMidiRead.isOn && !ToggleRealTimeRead.isOn)
                {
                    // Check every timeTorefresh millisecond if a new device is connected or is disconnected
                    if (Time.fixedUnscaledTime > timeTorefresh)
                    {
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
                        if (midievent == null)
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
                Debug.LogError(ex.Message);
            }
        }

        private void ProcessEvent(MPTKEvent midievent)
        {
            midiStreamPlayer.MPTK_PlayDirectEvent(midievent);
            Debug.Log($"[{DateTime.UtcNow.Millisecond:00000}] {midievent}");
        }

        public void GotoWeb(string uri)
        {
            Application.OpenURL(uri);
        }
    }
}