﻿#define MPTK_PRO
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using MidiPlayerTK;

namespace DemoMPTK
{
    public class TestMidiFilePlayerScripting : MonoBehaviour
    {
        /// <summary>@brief
        /// MPTK component able to play a Midi file from your list of Midi file. This PreFab must be present in your scene.
        /// </summary>
        public MidiFilePlayer midiFilePlayer;

        [Header("[Pro] Delay to ramp up volume at startup or down at stop")]
        [Range(0f, 5000f)]
        public float DelayRampMillisecond;

        [Header("Start position in Midi defined in pourcentaage of the whole druration of the Midi")]
        [Range(0f, 100f)]
        public float StartPositionPct;

        [Header("Stop position in Midi defined in pourcentaage of the whole druration of the Midi")]
        [Range(0f, 100f)]
        public float StopPositionPct;

        [Header("Delay to apply random change")]
        [Range(0f, 10f)]
        public float DelayRandomSecond;
        public bool IsRandomPosition;
        public bool IsRandomSpeed;
        public bool IsRandomTranspose;
        public bool IsRandomPlay;

        /// <summary>@brief
        /// When true the transition between two songs is immediate, but a small crossing can occur
        /// </summary>
        public bool IsWaitNotesOff;

        public int CurrentIndexPlaying;
        public int forceBank;

        public bool toggleChannelDisplay;
        public bool toggleApplyRealTimeChange;
        public bool toggleChangeNoteOn;
        public bool toggleDisableChangePreset;
        public bool toggleChangeTempo;

        // Manage skin
        private CustomStyle myStyle;
        private static Color ButtonColor = new Color(.7f, .9f, .7f, 1f);

        private Vector2 scrollerWindow = Vector2.zero;
        private int buttonWidth = 250;
        private PopupListItem PopMidi;

        private string infoMidi;
        private string infoLyrics;
        private string infoCopyright;
        private string infoSeqTrackName;
        private Vector2 scrollPos1 = Vector2.zero;
        private Vector2 scrollPos2 = Vector2.zero;
        private Vector2 scrollPos3 = Vector2.zero;
        private Vector2 scrollPos4 = Vector2.zero;

        private float LastTimeChange;

        //DateTime localStartTimeMidi;
        TimeSpan realTimeMidi;

        /// <summary>
        /// PreProcessMidi is triggered from an internal thread.
        ///    - Accuracy is garantee (see midiFilePlayer.MPTK_PulseLenght which is the minimum time in millisecond between two MIDI events).
        ///    - MIDI Events can be modified before processed by the MIDI synth.
        ///    - Direct call to Unity API is not possible.
        ///    - Avoid huge processing in this callback, that could cause irregular musical rhythms.
        ///  set with: midiFilePlayer.OnMidiEvent = PreProcessMidi;
        /// </summary>
        /// <param name="midiEvent">a MIDI event see https://mptkapi.paxstellar.com/d9/d50/class_midi_player_t_k_1_1_m_p_t_k_event.html </param>
        void PreProcessMidi(MPTKEvent midiEvent)
        {
            if (toggleApplyRealTimeChange)
            {
                switch (midiEvent.Command)
                {

                    case MPTKCommand.NoteOn:
                        if (toggleChangeNoteOn)
                        {
                            if (midiEvent.Channel != 9)
                                // transpose one octave depending on the value channel even or odd
                                if (midiEvent.Channel % 2 == 0)
                                    midiEvent.Value += 12;
                                else
                                    midiEvent.Value -= 12;
                            else
                                // Drums are muted
                                midiEvent.Velocity = 0;
                        }
                        break;
                    case MPTKCommand.PatchChange:
                        if (toggleDisableChangePreset)
                        {
                            // Transform Patch change event to Meta text event: related channel will played the default preset 0.
                            // TextEvent has no effect on the MIDI synth but is displayed in the demo windows.
                            // It would also been possible de change the preset to another instrument.
                            midiEvent.Command = MPTKCommand.MetaEvent;
                            midiEvent.Meta = MPTKMeta.TextEvent;
                            midiEvent.Info = $"Detected MIDI event Preset Change {midiEvent.Value} removed";
                        }
                        break;
                    case MPTKCommand.MetaEvent:
                        switch (midiEvent.Meta)
                        {
                            case MPTKMeta.SetTempo:
                                if (toggleChangeTempo)
                                {
                                    // Warning: this call back is run out of the main Unity thread, Unity API (like UnityEngine.Random) can't be used.
                                    System.Random rnd = new System.Random();
                                    // Change the tempo with a random value here, because it's too late for the MIDI Sequencer (alredy taken into account).
                                    midiFilePlayer.MPTK_Tempo = rnd.Next(30, 240);
                                    Debug.Log($"Detected MIDI event Set Tempo {midiEvent.Value}, forced to a random value {midiFilePlayer.MPTK_Tempo}");
                                }
                                break;
                        }
                        break;
                }
            }
        }

        //! [Example PreloadAndAlterMIDI]
        /// <summary>
        /// Load the selected MIDI, add some notes and play (PRO only)
        /// </summary>
        private void PreloadAndAlterMIDI()
        {
#if MPTK_PRO

            // Eventually, stop the MIDIplaying.
            midiFilePlayer.MPTK_Stop();

            // There is no need of note-off events.
            midiFilePlayer.MPTK_KeepNoteOff = false;
            // MPTK_MidiName must contains the name of the MIDI to load.
            if (midiFilePlayer.MPTK_Load() != null)
            {
                Debug.Log($"Duration: {midiFilePlayer.MPTK_Duration.TotalSeconds} seconds");
                Debug.Log($"Count MIDI Events: {midiFilePlayer.MPTK_MidiEvents.Count}");
                // Insert weird notes in the  beautiful MIDI!
                for (int weirdNote = 1; weirdNote <= 10; weirdNote++)
                    midiFilePlayer.MPTK_MidiEvents.Insert(0, new MPTKEvent() { Channel = 0, Command = MPTKCommand.NoteOn, Value = 60 + weirdNote, Duration = 1000, Tick = weirdNote * 100 });
                // New event has been inserted, MIDI events list must be sorted by tick.
                midiFilePlayer.MPTK_SortEvents();

                // ... then play the modified list of events (any garantee to create the hit of the year!).
                midiFilePlayer.MPTK_Play(alreadyLoaded: true);
            }
#else
            Debug.LogWarning("MIDI preload and alter MIDI events are available only with the PRO version");
#endif

        }
        //! [Example PreloadAndAlterMIDI]


        void Start()
        {

            // Warning: avoid to define this event by script like below because the initial loading could be not trigger in the case of MidiPlayerGlobal id load before any other gamecomponent
            // It's better to set this method from MidiPlayerGlobal event inspector.
            // To be done in Start event (not Awake)
            MidiPlayerGlobal.OnEventPresetLoaded.AddListener(SoundFontIsReadyEvent);

            PopMidi = new PopupListItem()
            {
                Title = "Select A MIDI File",
                OnSelect = MidiChanged,
                Tag = "NEWMIDI",
                ColCount = 3,
                ColWidth = 250,
            };

            // the prefab MidifIlePlayer must be defined in the inspector. You can associated with midiFilePlayer variable
            // with the inspector or by script
            if (midiFilePlayer == null)
            {
                Debug.Log("No MidiFilePlayer defined with the editor inspector, try to find one");
                MidiFilePlayer fp = FindObjectOfType<MidiFilePlayer>();
                if (fp == null)
                    Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
                else
                {
                    midiFilePlayer = fp;
                }
            }

            // useless v2.9.0 MidiLoad midiLoaded = midiFilePlayer.MPTK_Load();
            //if (midiLoaded == null) throw new Exception("Could not load MIDI file");
            //Debug.Log(midiLoaded.MPTK_TrackCount);

            if (midiFilePlayer != null)
            {
#if MPTK_PRO
                // OnMidiEvent (pro) and OnEventNotesMidi are triggered for each notes read by the MIDI sequencer
                // but their uses depends on the need:
                //      OnEventNotesMidi is handled from the main Unity thread (in the Update loop) 
                //          - Accuracy not garantee because depends on the Unity process and the Time.deltaTime
                //            (interval in seconds from the last frame to the current one).
                //          - MIDI Events can't be modified before processed by the MIDI synth.
                //          - Direct call to Unity API is possible.
                //      OnMidiEvent is handled from an internal thread 
                //          - accuracy is garantee.
                //          - MIDI Events can be modified before processed by the MIDI synth.
                //          - Direct call to Unity API is not possible.


                midiFilePlayer.OnMidiEvent = PreProcessMidi;

#endif

                // There is two methods to trigger event: 
                //      1) in inpector from the Unity editor 
                //      2) by script, see below 
                // ------------------------------------------

                SetStartEvent();

                // Event trigger when midi file end playing
                // Set event by script
                Debug.Log("OnEventEndPlayMidi defined by script");
                midiFilePlayer.OnEventEndPlayMidi.AddListener(EndPlay);

                // Event trigger for each group of notes read from midi file
                // Set event by scripit
                Debug.Log("OnEventNotesMidi defined by script");
                midiFilePlayer.OnEventNotesMidi.AddListener(MidiReadEvents);

                InitPlay();
            }
        }

        private void SetStartEvent()
        {
            //! [Example OnEventStartPlayMidi]
            // Event trigger when midi file start playing (it's not in the start method, event could be already set, remove it)
            Debug.Log("OnEventStartPlayMidi defined by script");
            midiFilePlayer.OnEventStartPlayMidi.RemoveAllListeners();
            midiFilePlayer.OnEventStartPlayMidi.AddListener(info => StartMidiPlay("Event set by script"));
            //! [Example OnEventStartPlayMidi]
        }

        /// <summary>@brief
        /// This method is defined from MidiPlayerGlobal event inspector and run when SoundFont is loaded.
        /// Warning: avoid to define this event by script because the initial loading could be not trigger in the case of MidiPlayerGlobal id load before any other gamecomponent
        /// </summary>
        public void SoundFontIsReadyEvent()
        {
            Debug.LogFormat("End loading SF '{0}', MPTK is ready to play", MidiPlayerGlobal.ImSFCurrent.SoundFontName);
            Debug.Log("   Time To Load SoundFont: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadSoundFont.TotalSeconds, 3).ToString() + " second");
            Debug.Log("   Time To Load Samples: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadWave.TotalSeconds, 3).ToString() + " second");
            Debug.Log("   Presets Loaded: " + MidiPlayerGlobal.MPTK_CountPresetLoaded);
            Debug.Log("   Samples Loaded: " + MidiPlayerGlobal.MPTK_CountWaveLoaded);
        }

        /// <summary>@brief
        /// Event fired by MidiFilePlayer when a midi is started (set by Unity Editor in MidiFilePlayer Inspector or by script see above)
        /// </summary>
        public void StartMidiPlay(string name)
        {
            infoLyrics = "";
            infoCopyright = "";
            infoSeqTrackName = "";
            //localStartTimeMidi = DateTime.Now;
            if (midiFilePlayer != null)
            {
                infoMidi = $"Load time: {midiFilePlayer.MPTK_MidiLoaded.MPTK_LoadTime:F2} milliseconds\n";
                infoMidi += $"Full Duration: {midiFilePlayer.MPTK_Duration} {midiFilePlayer.MPTK_DurationMS / 1000f:F2} seconds {midiFilePlayer.MPTK_TickLast} ticks\n";
                infoMidi += $"First note-on: {TimeSpan.FromMilliseconds(midiFilePlayer.MPTK_PositionFirstNote)} {midiFilePlayer.MPTK_PositionFirstNote / 1000f:F2} seconds {midiFilePlayer.MPTK_TickFirstNote} ticks\n";
                infoMidi += $"Last note-on : {TimeSpan.FromMilliseconds(midiFilePlayer.MPTK_PositionLastNote)} {midiFilePlayer.MPTK_PositionLastNote / 1000f:F2} seconds  {midiFilePlayer.MPTK_TickLastNote} ticks\n";
                infoMidi += $"Track Count  : {midiFilePlayer.MPTK_MidiLoaded.MPTK_TrackCount}\n";
                infoMidi += $"Initial Tempo: {midiFilePlayer.MPTK_MidiLoaded.MPTK_InitialTempo:F2}\n";
                infoMidi += $"Delta Ticks  : {midiFilePlayer.MPTK_MidiLoaded.MPTK_DeltaTicksPerQuarterNote} Ticks Per Quarter\n";
                infoMidi += $"Pulse Length : {midiFilePlayer.MPTK_PulseLenght} milliseconds (MIDI resolution)\n";
                infoMidi += $"Number Beats Measure   : {midiFilePlayer.MPTK_MidiLoaded.MPTK_NumberBeatsMeasure}\n";
                infoMidi += $"Number Quarter Beats   : {midiFilePlayer.MPTK_MidiLoaded.MPTK_NumberQuarterBeat}\n";
                infoMidi += $"Count MIDI Events      : {midiFilePlayer.MPTK_MidiEvents.Count}\n";
                infoMidi += $"Tempo Change\n";
                foreach (MPTKTempo tempo in midiFilePlayer.MPTK_MidiLoaded.MPTK_TempoMap)
                    infoMidi += $"  From tick:{tempo.FromTick,6} BPM:{MidiLoad.MPTK_MPQN2BPM(tempo.MicrosecondsPerQuarterNote)} Cumul:{tempo.Cumul:F2} Ratio:{tempo.Ratio:F2} MPQN:{tempo.MicrosecondsPerQuarterNote}\n";
                if (StartPositionPct > 0f)
                    midiFilePlayer.MPTK_TickCurrent = (long)((float)midiFilePlayer.MPTK_TickLast * (StartPositionPct / 100f));
            }
            Debug.Log($"Start Play MIDI '{name}' '{midiFilePlayer.MPTK_MidiName}' Duration: {midiFilePlayer.MPTK_DurationMS / 1000f:F2} seconds  Load time: {midiFilePlayer.MPTK_MidiLoaded.MPTK_LoadTime:F2} milliseconds");
        }

        /// <summary>@brief
        /// Event fired by MidiFilePlayer when midi notes are available. 
        /// Set by Unity Editor in MidiFilePlayer Inspector or by script with OnEventNotesMidi.
        /// </summary>
        public void MidiReadEvents(List<MPTKEvent> midiEvents)
        {
            //List<MPTKEvent> eventsOrdered = events.OrderBy(o => o.Value).ToList();
            // test exception on the callback midiEvents[10000].Channel = 1;

            // Looping in this demo is using percentage. Obviously, absolute tick value can be used.
            if (StopPositionPct < 100f)
            {
                if (StopPositionPct < StartPositionPct)
                    Debug.LogWarning($"StopPosition ({StopPositionPct} %) is defined before StartPosition ({StartPositionPct} %)");
                if (midiFilePlayer.MPTK_TickCurrent > (long)((float)midiFilePlayer.MPTK_TickLast * (StopPositionPct / 100f)))
                    if (midiFilePlayer.MPTK_Loop)
                        midiFilePlayer.MPTK_RePlay();
                    else
                        midiFilePlayer.MPTK_Next();
            }

            foreach (MPTKEvent midiEvent in midiEvents)
            {
                switch (midiEvent.Command)
                {
                    case MPTKCommand.ControlChange:
                        //Debug.LogFormat($"Pan Channel:{midiEvent.Channel} Value:{midiEvent.Value}");
                        break;

                    case MPTKCommand.NoteOn:
                        //Debug.LogFormat($"Note Channel:{midiEvent.Channel} {midiEvent.Value} Velocity:{midiEvent.Velocity} Duration:{midiEvent.Duration}");
                        break;

                    case MPTKCommand.MetaEvent:
                        switch (midiEvent.Meta)
                        {
                            case MPTKMeta.TextEvent:
                            case MPTKMeta.Lyric:
                            case MPTKMeta.Marker:
                                // Info from http://gnese.free.fr/Projects/KaraokeTime/Fichiers/karfaq.html and here https://www.mixagesoftware.com/en/midikit/help/HTML/karaoke_formats.html
                                //Debug.Log(midievent.Channel + " " + midievent.Meta + " '" + midievent.Info + "'");
                                string text = midiEvent.Info.Replace("\\", "\n");
                                text = text.Replace("/", "\n");
                                if (text.StartsWith("@") && text.Length >= 2)
                                {
                                    switch (text[1])
                                    {
                                        case 'K': text = "Type: " + text.Substring(2); break;
                                        case 'L': text = "Language: " + text.Substring(2); break;
                                        case 'T': text = "Title: " + text.Substring(2); break;
                                        case 'V': text = "Version: " + text.Substring(2); break;
                                        default: //I as information, W as copyright, ...
                                            text = text.Substring(2); break;
                                    }
                                    //text += "\n";
                                }
                                infoLyrics += text + "\n";
                                break;

                            case MPTKMeta.Copyright:
                                infoCopyright += midiEvent.Info + "\n";
                                break;

                            case MPTKMeta.SequenceTrackName:
                                infoSeqTrackName += $"Track:{midiEvent.Track:00} '{midiEvent.Info}'\n";
                                //Debug.LogFormat($"SequenceTrackName Track:{midiEvent.Track} {midiEvent.Value} Name:'{midiEvent.Info}'");
                                break;
                        }
                        break;
                }
            }
        }

        /// <summary>@brief
        /// Event fired by MidiFilePlayer when a midi is ended when reach end or stop by MPTK_Stop or Replay with MPTK_Replay
        /// The parameter reason give the origin of the end
        /// </summary>
        public void EndPlay(string name, EventEndMidiEnum reason)
        {
            Debug.LogFormat("End playing midi {0} reason:{1}", name, reason);
        }

        public void InitPlay()
        {
            //midiFilePlayer.MPTK_InitSynth(32);
            if (MidiPlayerGlobal.MPTK_ListMidi != null && MidiPlayerGlobal.MPTK_ListMidi.Count > 0)
            {
                if (IsRandomPlay)
                {
                    // Random select for the MIDI
                    int index = UnityEngine.Random.Range(0, MidiPlayerGlobal.MPTK_ListMidi.Count);
                    midiFilePlayer.MPTK_MidiIndex = index;
                    Debug.LogFormat("Random selected midi index{0} name:{1}", index, midiFilePlayer.MPTK_MidiName);
                }
                //GetMidiInfo();
                //midiFilePlayer.MPTK_Play();
                //CurrentIndexPlaying = midiFilePlayer.MPTK_MidiIndex;
            }
        }

        private void MidiChanged(object tag, int midiindex, int indexList)
        {
            Debug.Log("MidiChanged " + midiindex + " for " + tag);
            midiFilePlayer.MPTK_MidiIndex = midiindex;
            midiFilePlayer.MPTK_RePlay();
            // V2.81 Test Play and Pause
            //Debug.Log("MidiChanged **** PlayAndPauseMidi **** " + midiindex + " for " + tag);
            //midiFilePlayer.PlayAndPauseMidi(midiindex, "");//, 5000);
        }

        //! [Example TheMostSimpleDemoForMidiPlayer]
        /// <summary>@brief
        /// Load a midi file without playing it
        /// </summary>
        private void TheMostSimpleDemoForMidiPlayer()
        {
            MidiFilePlayer midiplayer = FindObjectOfType<MidiFilePlayer>();
            if (midiplayer == null)
            {
                Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
                return;
            }

            // Index of the midi from the Midi DB (find it with 'Midi File Setup' from the menu MPTK)
            midiplayer.MPTK_MidiIndex = 10;

            // Open and load the Midi
            if (midiplayer.MPTK_Load() != null)
            {
                // Read midi event to a List<>
                List<MPTKEvent> mptkEvents = midiplayer.MPTK_ReadMidiEvents(1000, 10000);

                // Loop on each Midi events
                foreach (MPTKEvent mptkEvent in mptkEvents)
                {
                    // Log if event is a note on
                    if (mptkEvent.Command == MPTKCommand.NoteOn)
                        Debug.Log($"Note on Time:{mptkEvent.RealTime} millisecond  Note:{mptkEvent.Value}  Duration:{mptkEvent.Duration} millisecond  Velocity:{mptkEvent.Velocity}");

                    // Uncomment to display all Midi events
                    //Debug.Log(mptkEvent.ToString());
                }
            }
        }
        //! [Example TheMostSimpleDemoForMidiPlayer]

        void OnGUI()
        {
            int spaceV = 10;
            //  if (!HelperDemo.CheckSFExists()) return;

            // Set custom Style. Good for background color 3E619800
            if (myStyle == null)
                myStyle = new CustomStyle();

            if (midiFilePlayer != null)
            {
                scrollerWindow = GUILayout.BeginScrollView(scrollerWindow, false, false, GUILayout.Width(Screen.width));

                // Display popup in first to avoid activate other layout behind
                PopMidi.Draw(MidiPlayerGlobal.MPTK_ListMidi, midiFilePlayer.MPTK_MidiIndex, myStyle);

                MainMenu.Display("Test MIDI File Player Scripting - Demonstrate how to use the MPTK API to Play Midi", myStyle, "https://paxstellar.fr/midi-file-player-detailed-view-2/");

                GUISelectSoundFont.Display(scrollerWindow, myStyle);

                //
                // Left column: Midi action
                // ------------------------

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical(myStyle.BacgDemos, GUILayout.Width(700));
                // Open the popup to select a midi
                if (GUILayout.Button("Current MIDI file: '" + midiFilePlayer.MPTK_MidiName + (midiFilePlayer.MPTK_IsPlaying ? "' is playing" : "' is not playing"), GUILayout.Width(500), GUILayout.Height(40)))
                    PopMidi.Show = !PopMidi.Show;
                // PopMidi.PositionWithScroll(ref scrollerWindow);

                HelperDemo.DisplayInfoSynth(midiFilePlayer, 600, myStyle);

                GUILayout.Space(spaceV);

                // Get the time of the last MIDI event read
                TimeSpan timePosition = TimeSpan.FromMilliseconds(midiFilePlayer.MPTK_Position);

                // Display the real time from the MIDI sequencer and the delta time with the last MIDI events
                if (midiFilePlayer.MPTK_IsPlaying)
                    realTimeMidi = TimeSpan.FromMilliseconds(midiFilePlayer.MPTK_RealTime);
                string realTime = $"Real Time: {realTimeMidi.Hours:00}:{realTimeMidi.Minutes:00}:{realTimeMidi.Seconds:00}:{realTimeMidi.Milliseconds:000} ";
                string deltaTime = $"Delta time with the last MIDI event: {(timePosition - realTimeMidi).TotalSeconds:F3} second";
                GUILayout.Label(realTime + deltaTime, myStyle.TitleLabel3, GUILayout.Width(500));

                GUILayout.BeginHorizontal();
                string playTime = $"{timePosition.Hours:00}:{timePosition.Minutes:00}:{timePosition.Seconds:00}:{timePosition.Milliseconds:000}";
                string realDuration = $"{midiFilePlayer.MPTK_Duration.Hours:00}:{midiFilePlayer.MPTK_Duration.Minutes:00}:{midiFilePlayer.MPTK_Duration.Seconds:00}:{midiFilePlayer.MPTK_Duration.Milliseconds:000}";
                GUILayout.Label("MIDI Position: " + playTime + " / " + realDuration, myStyle.TitleLabel3, GUILayout.Width(300));
                double currentPosition = Math.Round(midiFilePlayer.MPTK_Position / 1000d, 2);
                double newPosition = Math.Round(GUILayout.HorizontalSlider((float)currentPosition, 0f, (float)midiFilePlayer.MPTK_Duration.TotalSeconds, GUILayout.Width(150)), 2);
                if (newPosition != currentPosition)
                {
                    if (Event.current.type == EventType.Used)
                    {
                        //Debug.Log("New position " + currentPosition + " --> " + newPosition + " " + Event.current.type);
                        midiFilePlayer.MPTK_Position = newPosition * 1000d;
                    }
                }
                GUILayout.EndHorizontal();

                //Avoid slider with ticks position trigger
                GUILayout.BeginHorizontal();
                GUILayout.Label("Tick Position: " + midiFilePlayer.MPTK_TickCurrent + " / " + midiFilePlayer.MPTK_TickLast, myStyle.TitleLabel3, GUILayout.Width(300));
                long tick = (long)GUILayout.HorizontalSlider((float)midiFilePlayer.MPTK_TickCurrent, 0f, (float)midiFilePlayer.MPTK_TickLast, GUILayout.Width(150));
                if (tick != midiFilePlayer.MPTK_TickCurrent)
                {
                    if (Event.current.type == EventType.Used)
                    {
                        //Debug.Log("New tick " + midiFilePlayer.MPTK_TickCurrent + " --> " + tick + " " + Event.current.type);
                        midiFilePlayer.MPTK_TickCurrent = tick;
                    }
                }
                GUILayout.EndHorizontal();

                //Avoid slider with ticks position trigger
                GUILayout.BeginHorizontal();
                long pos = (long)((float)midiFilePlayer.MPTK_TickLast * (StartPositionPct / 100f));
                GUILayout.Label($"Tick Start Position: {StartPositionPct} % ({pos})", myStyle.TitleLabel3, GUILayout.Width(300));
                StartPositionPct = (long)GUILayout.HorizontalSlider(StartPositionPct, 0f, 100f, GUILayout.Width(150));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                pos = (long)((float)midiFilePlayer.MPTK_TickLast * (StopPositionPct / 100f));
                GUILayout.Label($"Tick Stop Position: {StopPositionPct} % ({pos})", myStyle.TitleLabel3, GUILayout.Width(300));
                StopPositionPct = (long)GUILayout.HorizontalSlider(StopPositionPct, 0f, 100f, GUILayout.Width(150));
                GUILayout.EndHorizontal();

                midiFilePlayer.MPTK_Loop = GUILayout.Toggle(midiFilePlayer.MPTK_Loop, "At End Loop on this MIDI or Play Next");


                // Define the global volume
                //GUILayout.Space(spaceV);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Global Volume: " + Math.Round(midiFilePlayer.MPTK_Volume, 2), myStyle.TitleLabel3, GUILayout.Width(220));
                midiFilePlayer.MPTK_Volume = GUILayout.HorizontalSlider(midiFilePlayer.MPTK_Volume * 100f, 0f, 100f, GUILayout.Width(buttonWidth)) / 100f;
                GUILayout.EndHorizontal();

                // Transpose each note
                //GUILayout.Space(spaceV);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Note Transpose: " + midiFilePlayer.MPTK_Transpose, myStyle.TitleLabel3, GUILayout.Width(220));
                midiFilePlayer.MPTK_Transpose = (int)GUILayout.HorizontalSlider((float)midiFilePlayer.MPTK_Transpose, -24f, 24f, GUILayout.Width(buttonWidth));
                GUILayout.EndHorizontal();

                // Transpose each note
                //GUILayout.Space(spaceV);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Speed: " + Math.Round(midiFilePlayer.MPTK_Speed, 2), myStyle.TitleLabel3, GUILayout.Width(220));
                float speed = GUILayout.HorizontalSlider((float)midiFilePlayer.MPTK_Speed, 0.1f, 10f, GUILayout.Width(buttonWidth));
                if (speed != midiFilePlayer.MPTK_Speed)
                {
                    // Avoid event as layout triggered when speed is changed
                    if (Event.current.type == EventType.Used)
                    {
                        //Debug.Log("New speed " + midiFilePlayer.MPTK_Speed + " --> " + speed + " " + Event.current.type);
                        midiFilePlayer.MPTK_Speed = speed;
                    }
                }
                GUILayout.EndHorizontal();

                toggleApplyRealTimeChange = GUILayout.Toggle(toggleApplyRealTimeChange, "  Apply MIDI Real-Time Change [Pro]");
                if (toggleApplyRealTimeChange)
                    RealTimeMIDIChange();

                // Channel setting display
                toggleChannelDisplay = GUILayout.Toggle(toggleChannelDisplay, "  Display Channels and Change Properties");
                if (toggleChannelDisplay)
                    ChannelDisplay();

                // Random playing ?
                GUILayout.BeginHorizontal();
                IsRandomPlay = GUILayout.Toggle(IsRandomPlay, "  Enable MIDI Random Playing");

                // Weak device ?
                GUILayout.EndHorizontal();
                GUILayout.Space(spaceV);

                // Play/Pause/Stop/Restart actions on midi 
                GUILayout.BeginHorizontal(GUILayout.Width(500));
                if (midiFilePlayer.MPTK_IsPlaying && !midiFilePlayer.MPTK_IsPaused)
                    GUI.color = ButtonColor;
                if (GUILayout.Button(new GUIContent("Play", "")))
                    midiFilePlayer.MPTK_Play();

                if (GUILayout.Button(new GUIContent("Alter & Play [Pro]", "")))
                    PreloadAndAlterMIDI();
                GUI.color = Color.white;

                if (midiFilePlayer.MPTK_IsPaused)
                    GUI.color = ButtonColor;
                if (GUILayout.Button(new GUIContent("Pause", "")))
                    if (midiFilePlayer.MPTK_IsPaused)
                        midiFilePlayer.MPTK_UnPause();
                    else
                        midiFilePlayer.MPTK_Pause();
                GUI.color = Color.white;

                if (GUILayout.Button(new GUIContent("Stop", "")))
                    midiFilePlayer.MPTK_Stop();

                if (GUILayout.Button(new GUIContent("Restart", "")))
                    midiFilePlayer.MPTK_RePlay();

                if (GUILayout.Button(new GUIContent("Clear", "")))
                    midiFilePlayer.MPTK_ClearAllSound(true);

                GUILayout.EndHorizontal();

#if MPTK_PRO
                GUILayout.BeginHorizontal(GUILayout.Width(500));
                if (GUILayout.Button(new GUIContent($"Play Ramp-Up {(int)DelayRampMillisecond} ms", "")))
                    midiFilePlayer.MPTK_Play(DelayRampMillisecond);
                if (GUILayout.Button(new GUIContent($"Stop Downward {(int)DelayRampMillisecond} ms", "")))
                    midiFilePlayer.MPTK_Stop(DelayRampMillisecond);
                GUILayout.Label("Delay", myStyle.TitleLabel3, GUILayout.Width(50));
                DelayRampMillisecond = GUILayout.HorizontalSlider(DelayRampMillisecond, 0f, 5000f, myStyle.SliderBar, myStyle.SliderThumb, GUILayout.Width(120));
                GUILayout.EndHorizontal();
#endif
                // Previous and Next button action on midi
                GUILayout.BeginHorizontal(GUILayout.Width(500));
                if (GUILayout.Button(new GUIContent("Previous", "")))
                {
                    if (IsWaitNotesOff)
                        StartCoroutine(NextPreviousWithWait(false));
                    else
                    {
                        midiFilePlayer.MPTK_Previous();
                        CurrentIndexPlaying = midiFilePlayer.MPTK_MidiIndex;
                    }
                }
                if (GUILayout.Button(new GUIContent("Next", "")))
                {
                    if (IsWaitNotesOff)
                        StartCoroutine(NextPreviousWithWait(true));
                    else
                    {
                        midiFilePlayer.MPTK_Next();
                        CurrentIndexPlaying = midiFilePlayer.MPTK_MidiIndex;
                    }
                    //Debug.Log("MPTK_Next - CurrentIndexPlaying " + CurrentIndexPlaying);
                }
                IsWaitNotesOff = GUILayout.Toggle(IsWaitNotesOff, "  Wait all notes off", GUILayout.Width(120));
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                if (!string.IsNullOrEmpty(infoMidi) || !string.IsNullOrEmpty(infoLyrics) || !string.IsNullOrEmpty(infoCopyright) || !string.IsNullOrEmpty(infoSeqTrackName))
                {
                    //
                    // Right Column: midi infomation, lyrics, ...
                    // ------------------------------------------
                    GUILayout.BeginVertical(myStyle.BacgDemos);
                    if (!string.IsNullOrEmpty(infoMidi))
                    {
                        scrollPos1 = GUILayout.BeginScrollView(scrollPos1, false, true);//, GUILayout.Height(heightLyrics));
                        GUILayout.Label(infoMidi, myStyle.TextFieldMultiCourier);
                        GUILayout.EndScrollView();
                    }
                    GUILayout.Space(5);
                    if (!string.IsNullOrEmpty(infoLyrics))
                    {
                        GUILayout.Label("Lyrics");
                        //Debug.Log(scrollPos + " " + countline+ " " + myStyle.TextFieldMultiLine.CalcHeight(new GUIContent(lyrics), 400));
                        //float heightLyrics = myStyle.TextFieldMultiLine.CalcHeight(new GUIContent(infoLyrics), 400);
                        //scrollPos.y = - 340;
                        //if (heightLyrics > 200) heightLyrics = 200;
                        scrollPos2 = GUILayout.BeginScrollView(scrollPos2, false, true);//, GUILayout.Height(heightLyrics));
                        GUILayout.Label(infoLyrics, myStyle.TextFieldMultiCourier);
                        GUILayout.EndScrollView();
                        //if (GUILayout.Button(new GUIContent("Add", ""))) lyrics += "\ntestest testetst";
                    }
                    GUILayout.Space(5);
                    if (!string.IsNullOrEmpty(infoCopyright))
                    {
                        GUILayout.Label("Copyright");
                        scrollPos3 = GUILayout.BeginScrollView(scrollPos3, false, true);
                        GUILayout.Label(infoCopyright, myStyle.TextFieldMultiCourier);
                        GUILayout.EndScrollView();
                    }
                    GUILayout.Space(5);
                    if (!string.IsNullOrEmpty(infoSeqTrackName))
                    {
                        GUILayout.Label("Track Name");
                        scrollPos4 = GUILayout.BeginScrollView(scrollPos4, false, true);
                        GUILayout.Label(infoSeqTrackName, myStyle.TextFieldMultiCourier);
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(myStyle.BacgDemos);
                GUILayout.Label("Go to your Hierarchy, select GameObject MidiFilePlayer: inspector contains a lot of parameters to control the sound.", myStyle.TitleLabel2);
                GUILayout.EndHorizontal();

                GUILayout.EndScrollView();
            }

        }

        private void RealTimeMIDIChange()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("", myStyle.TitleLabel3, GUILayout.Width(40));
            GUILayout.Label("Transpose one octave depending on the channel value and disable drum.", myStyle.TitleLabel3, GUILayout.Width(400));
            toggleChangeNoteOn = GUILayout.Toggle(toggleChangeNoteOn, "");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("", myStyle.TitleLabel3, GUILayout.Width(40));
            GUILayout.Label("Disable preset change", myStyle.TitleLabel3, GUILayout.Width(400));
            toggleDisableChangePreset = GUILayout.Toggle(toggleDisableChangePreset, "");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("", myStyle.TitleLabel3, GUILayout.Width(40));
            GUILayout.Label("Random change of MIDI tempo change event", myStyle.TitleLabel3, GUILayout.Width(400));
            toggleChangeTempo = GUILayout.Toggle(toggleChangeTempo, "");
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }


        private void ChannelDisplay()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Enable All"), GUILayout.Width(100)))
                for (int channel = 0; channel < midiFilePlayer.MPTK_ChannelCount(); channel++)
                    midiFilePlayer.MPTK_ChannelEnableSet(channel, true);
            if (GUILayout.Button(new GUIContent("Disable All"), GUILayout.Width(100)))
                for (int channel = 0; channel < midiFilePlayer.MPTK_ChannelCount(); channel++)
                    midiFilePlayer.MPTK_ChannelEnableSet(channel, false);
            if (GUILayout.Button(new GUIContent("Default All"), GUILayout.Width(100)))
                for (int channel = 0; channel < midiFilePlayer.MPTK_ChannelCount(); channel++)
                {
                    midiFilePlayer.MPTK_ChannelEnableSet(channel, true);
                    midiFilePlayer.MPTK_ChannelForcedPresetSet(channel, -1);
                    midiFilePlayer.MPTK_ChannelVolumeSet(channel, 1f);
                }
            GUILayout.EndHorizontal();

            //! [ExampleUsingChannelAPI]

            GUILayout.BeginHorizontal();
            GUILayout.Label("Channel  Preset                                                Preset / Bank                   Count            Enabled       Volume", myStyle.TitleLabel3);
            GUILayout.EndHorizontal();
            for (int channel = 0; channel < midiFilePlayer.MPTK_ChannelCount(); channel++)
            {
                GUILayout.BeginHorizontal();

                // Display channel number and log info
                if (GUILayout.Button($"   {channel:00}", myStyle.TitleLabel3, GUILayout.Width(60)))
                    Debug.Log(midiFilePlayer.MPTK_ChannelInfo(channel));

                // Display preset
                GUILayout.Label(midiFilePlayer.MPTK_ChannelPresetGetName(channel) ?? "not set", myStyle.TitleLabel3, GUILayout.MaxWidth(150));

                // Display preset and bank index
                string sPreset = "";
                int presetForced = midiFilePlayer.MPTK_ChannelForcedPresetGet(channel);
                if (presetForced == -1)
                {
                    // Preset not forced, get the preset defined on this channel by the Midi
                    sPreset = midiFilePlayer.MPTK_ChannelPresetGetIndex(channel).ToString();
                }
                else
                {
                    sPreset = $"F{presetForced}";
                }

                int bankIndex = midiFilePlayer.MPTK_ChannelBankGetIndex(channel);
                GUILayout.Label($"{sPreset} / {bankIndex}", myStyle.LabelRight/*, GUILayout.Width(80)*/);

                int current = midiFilePlayer.MPTK_ChannelPresetGetIndex(channel);
                // Slider to change the preset on this channel from -1 (disable forced) to 127.
                // Forced bank from the inspector.
                int forcePreset = (int)GUILayout.HorizontalSlider(current,
                                                    -1f, 127f, myStyle.SliderBar, myStyle.SliderThumb, GUILayout.Width(100));
                if (forcePreset != current)
                {
                    // Force a preset and a bank whatever the MIDI events from the MIDI file.
                    // set forcePreset to -1 to restore to the last preset and bank value known from the MIDI file.
                    // let forcebank to -1 to not force the bank.
                    midiFilePlayer.MPTK_ChannelForcedPresetSet(channel, forcePreset, forceBank);
                }

                // Display count note by channel
                GUILayout.Label($"{midiFilePlayer.MPTK_ChannelNoteCount(channel),-5}", myStyle.LabelRight, GUILayout.Width(60));

                // Toggle to enable or disable a channel
                GUILayout.Label("   ", myStyle.TitleLabel3, GUILayout.Width(60));
                bool state = GUILayout.Toggle(midiFilePlayer.MPTK_ChannelEnableGet(channel), "", GUILayout.MaxWidth(20));
                if (state != midiFilePlayer.MPTK_ChannelEnableGet(channel))
                {
                    midiFilePlayer.MPTK_ChannelEnableSet(channel, state);
                    Debug.LogFormat("Channel {0} state:{1}, preset:{2}", channel, state, midiFilePlayer.MPTK_ChannelPresetGetName(channel) ?? "not set"); /*2.84*/
                }

                // Slider to change volume
                float currentVolume = midiFilePlayer.MPTK_ChannelVolumeGet(channel);
                GUILayout.Label($"{Math.Round(currentVolume, 2)}", myStyle.LabelRight, GUILayout.Width(40));
                float volume = GUILayout.HorizontalSlider(currentVolume, 0f, 1f, myStyle.SliderBar, myStyle.SliderThumb, GUILayout.Width(60));
                if (volume != currentVolume)
                {
                    midiFilePlayer.MPTK_ChannelVolumeSet(channel, volume);
                }

                GUILayout.EndHorizontal();
            }

            //! [ExampleUsingChannelAPI]

            GUILayout.EndVertical();
        }

        /// <summary>@brief
        /// Coroutine: stop current midi playing, wait until all samples are off and go next or previous midi
        /// Example call: StartCoroutine(NextPreviousWithWait(false));
        /// </summary>
        /// <param name="next"></param>
        /// <returns></returns>
        public IEnumerator NextPreviousWithWait(bool next)
        {
            midiFilePlayer.MPTK_Stop();

            yield return midiFilePlayer.MPTK_WaitAllNotesOff(midiFilePlayer.IdSession);
            if (next)
                midiFilePlayer.MPTK_Next();
            else
                midiFilePlayer.MPTK_Previous();
            CurrentIndexPlaying = midiFilePlayer.MPTK_MidiIndex;

            yield return 0;
        }

        /// <summary>@brief
        /// Event fired by MidiFilePlayer when a midi is ended (set by Unity Editor in MidiFilePlayer Inspector)
        /// </summary>
        public void RandomPlay()
        {
            if (IsRandomPlay)
            {
                //Debug.Log("Is playing : " + midiFilePlayer.MPTK_IsPlaying);
                int index = UnityEngine.Random.Range(0, MidiPlayerGlobal.MPTK_ListMidi.Count);
                midiFilePlayer.MPTK_MidiIndex = index;
                midiFilePlayer.MPTK_Play();
            }
            else
                midiFilePlayer.MPTK_RePlay();
        }

        //Update is called once per frame
        void Update()
        {
            if (midiFilePlayer != null && midiFilePlayer.MPTK_IsPlaying)
            {
                float time = Time.realtimeSinceStartup - LastTimeChange;
                if (DelayRandomSecond > 0f && time > DelayRandomSecond)
                {
                    // It's time to apply randon change
                    LastTimeChange = Time.realtimeSinceStartup;

                    // Random position
                    if (IsRandomPosition) midiFilePlayer.MPTK_Position = UnityEngine.Random.Range(0f, (float)midiFilePlayer.MPTK_Duration.TotalMilliseconds);

                    // Random Speed
                    if (IsRandomSpeed) midiFilePlayer.MPTK_Speed = UnityEngine.Random.Range(0.1f, 5f);

                    // Random transmpose
                    if (IsRandomTranspose) midiFilePlayer.MPTK_Transpose = UnityEngine.Random.Range(-12, 13);
                }
            }
        }
    }
}