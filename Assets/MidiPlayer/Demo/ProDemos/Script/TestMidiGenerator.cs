
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System;
using UnityEngine.Events;
using MPTK.NAudio.Midi;
using System.Linq;
using MidiPlayerTK;
using UnityEditor;

namespace DemoMPTK

{
    /// <summary>
    /// Script for the class MidiFileWriter2. 
    /// </summary>
    public class TestMidiGenerator : MonoBehaviour
    {
        //float spaceH = 30f;
        float spaceV = 5f;
        public CustomStyle myStyle;
        DateTime startPlaying;
        bool loop = false;
        Vector2 scrollerWindow = Vector2.zero;
        long loopFrom = 0;
        long loopTo = 0;
        Func<MidiFileWriter2> mfwGenerator = null;
        string mfwTitle = "nothing selected";
        int indexExample;

        public static GUIStyle BuildStyle(GUIStyle inheritedStyle = null, int fontSize = 10, bool wrapText = false,
                                       FontStyle fontStyle = FontStyle.Normal, TextAnchor textAnchor = TextAnchor.MiddleLeft)
        {
            GUIStyle style = inheritedStyle == null ? new GUIStyle() : new GUIStyle(inheritedStyle);
            style.alignment = textAnchor;
            style.fontSize = fontSize;
            style.fontStyle = fontStyle;
            style.wordWrap = wrapText;
            style.clipping = TextClipping.Overflow;
            return style;
        }
        void OnGUI()
        {
            if (!HelperDemo.CheckSFExists()) return;

            // Set custom Style. Good for background color 3E619800
            if (myStyle == null) myStyle = new CustomStyle();

            MainMenu.Display("Create a MIDI messages by Algo, Write to a MIDI file, Play", myStyle, "https://paxstellar.fr/class-midifilewriter2/");

            GUILayout.BeginVertical(myStyle.BacgDemos);
            GUILayout.Label("Write the generated notes to a Midi file and play with a MidiExternalPlay Prefab or play the generated notes with MidiFilePlayer Prefab, no file is created.", myStyle.TitleLabel2Centered);
            GUILayout.Label("A low pass effect is enabled with MidiExternalPlay prefab also it sound differently that MidiFilePlayer prefab. See inspector.", myStyle.TitleLabel2Centered);

            //GUILayout.Space(spaceV);
            scrollerWindow = GUILayout.BeginScrollView(scrollerWindow, false, false, GUILayout.Width(Screen.width));
            indexExample = 0;
            GUIExample(CreateMidiStream_four_notes_milli, "Very simple stream - 4 notes of 500 milliseconds created every 500 milliseconds");
            GUIExample(CreateMidiStream_four_notes_ticks, "Very simple stream - 4 consecutives quarters created independantly of the tempo");
            GUIExample(CreateMidiStream_preset_tempo_pitchWheel, "More complex one - Preset change, Tempo change, Pitch Wheel change, Modulation change");
            GUIExample(CreateMidiStream_Chords, "Chords with violin - tonic C major with progression I V IV V - C minor I VIm IIm V - 4 chords with piano from the Maestro library CM DmM7 Gm7b5 FM7 - silence", 70);
            GUIExample(CreateMidiStream_sandbox, "Sandbox - make your trial");
            GUIExample(CreateMidiStream_silence, "TU - silence at the end");
            GUIExample(CreateMidiStream_full_crescendo, "TU - full velocity crescendo from 0 to 127");
            GUIExample(CreateMidiStream_short_crescendo_with_noteoff_tick, "TU - fast velocity crescendo millisecond (250 ms, Quarter by 4)");
            GUIExample(CreateMidiStream_short_crescendo_with_noteoff_ms, "TU - fast velocity crescendo millisecond (250 milliseconds)");
            GUIExample(CreateMidiStream_midi_merge, "TU - merge MIDI index 0 with MIDI index 1 from the DB");
            GUIExample(CreateMidiStream_four_notes_only, "TU - only 4 noteon at dtpq=500");
            GUIExample(CreateMidiStream_tempochange, "TU - tempo changes");
            GUIExample(CreateMidiStream_stable_sort, "TU - stable sort");

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.BeginVertical(myStyle.BacgDemos);
            GUILayout.Label($"MIDI Generator selected: {mfwTitle}", myStyle.TitleLabel2Centered);

            // ----------------------
            // Button for stop playing and open folder
            // ----------------------
            GUILayout.BeginHorizontal(myStyle.BacgDemos);

            float height = 70;
            if (GUILayout.Button(loop ? "Loop Playing" : "One Shot", GUILayout.Height(height)))
            {
                loop = !loop;
                if (!loop)
                    StopAllPlaying();
            }

            if (GUILayout.Button("Stop Playing", GUILayout.Height(height)))
                StopAllPlaying();

            if (mfwGenerator != null)
            {
                if (GUILayout.Button("Write To a MIDI File\nand Play with\nMidiExternalPlay Prefab", GUILayout.Height(height)))
                {
                    StopAllPlaying();
                    WriteMidiSequenceToFileAndPlay($"{mfwTitle} - generated", mfwGenerator());
                }

                if (GUILayout.Button("Write To the MIDI DB", GUILayout.Height(height)))
                {
                    StopAllPlaying();
                    WriteMidiToMidiDB($"{mfwTitle} - generated", mfwGenerator());
                }

                if (GUILayout.Button("Play with\nMidiFilePlayer", GUILayout.Height(height)))
                {
                    StopAllPlaying();
                    PlayDirectlyMidiSequence(mfwTitle, mfwGenerator());
                }
                GUILayout.BeginVertical();
                loopFrom = LongField("From Tick:", loopFrom, min: 0, max: 50000, maxLength: 5, widthLabel: 70);
                loopTo = LongField("To Tick:", loopTo, min: 0, max: 50000, maxLength: 5, widthLabel: 70);
                GUILayout.EndVertical();
            }

            if (GUILayout.Button("Open Folder\nMIDI External", GUILayout.Height(height)))
                Application.OpenURL("file://" + Application.persistentDataPath);

            if (GUILayout.Button("Open Folder\nMaestro MIDI DB", GUILayout.Height(height)))
                Application.OpenURL("file://" + Path.Combine(Application.dataPath, MidiPlayerGlobal.PathToMidiFile));


            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

        }
        long LongField(string label = null, long val = 0, long min = 0, long max = 99999999, int maxLength = 10, int widthLabel = 60)
        {
            GUILayout.BeginHorizontal();
            long newval;
            if (label != null)
                GUILayout.Label(label, GUILayout.Width(widthLabel), GUILayout.ExpandWidth(false));
            if (val < min) val = min;
            if (val > max) val = max;

            string oldtxt = val.ToString();
            string newtxt = GUILayout.TextField(oldtxt, maxLength: maxLength, GUILayout.Width(maxLength*15));
            GUILayout.EndHorizontal();

            if (newtxt != oldtxt)
                try
                {
                    newval = newtxt.Length > 0 ? Convert.ToInt64(newtxt) : 0;
                    if (newval < min) newval = min;
                    if (newval > max) newval = max;
                    return newval;
                }
                catch { }

            return val;
        }


        private void GUIExample(Func<MidiFileWriter2> _mfwGenerator, string _title, int height = 40)
        {
            string title = $"{++indexExample} - {_title}";
            GUILayout.BeginHorizontal(myStyle.BacgDemos1);
            GUILayout.Label(title, myStyle.LabelLeft /*GUILayout.Height(height)*/);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Select", GUILayout.Width(80)/*, GUILayout.Height(height)*/))
            {
                mfwGenerator = _mfwGenerator;
                mfwTitle = title;
            }
            if (GUILayout.Button("Log MPTK", GUILayout.Width(80)/*, GUILayout.Height(height)*/))
            {
                MidiFileWriter2 mfw = mfwGenerator();
                mfw.MPTK_CreateTracksStat();
                mfw.MPTK_CreateTempoMap();
                mfw.MPTK_Debug();
            }
            if (GUILayout.Button("Log Raw", GUILayout.Width(80)/*, GUILayout.Height(height)*/))
                mfwGenerator().MPTK_DebugRaw();

            GUILayout.EndHorizontal();
            GUILayout.Space(spaceV);
        }

        /// <summary>@brief
        /// Play four consecutive quarters from 60 (C5) to 63.
        /// Use AddNoteMS method for Tempo and duration defined in milliseconds.
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_four_notes_milli()
        {
            // In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Using multiple tracks is not mandatory, you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change.
            // By convention contains no noteon.
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 1;

            int channel0 = 0; // we are using only one channel in this demo

            // Create a Midi file of type 1 (recommended)
            MidiFileWriter2 mfw = new MidiFileWriter2();

            mfw.MPTK_AddText(track0, 0, MPTKMeta.Copyright, "Simple MIDI Generated. 4 quarter at 120 BPM");

            // Playing tempo must be defined at start of the stream. 
            // Defined BPM is mandatory when duration and delay are defined in millisecond in the stream. 
            // The value of the BPM is used to transform duration from milliseconds to internal ticks value.
            // Obviously, duration in millisecànds depends on the BPM selected.
            // With BPM=120, a quarter duration is 500 milliseconds, 240 ticks (default value for a quarter)
            mfw.MPTK_AddBPMChange(track0, 0, 120);

            // Add four consecutive quarters from 60 (C5)  to 63.
            // With BPM=120, quarter duration is 500ms (60000 / 120). So, notes are played at, 0, 500, 1000, 1500 ms from the start.
            mfw.MPTK_AddNoteMilli(track1, 0f, channel0, 60, 50, 500f);
            mfw.MPTK_AddNoteMilli(track1, 500f, channel0, 61, 50, 500f);
            mfw.MPTK_AddNoteMilli(track1, 1000f, channel0, 62, 50, 500f);
            mfw.MPTK_AddNoteMilli(track1, 1500f, channel0, 63, 50, 500f);

            // Silent note 1/4 second
            mfw.MPTK_AddSilenceMilli(track1, 3000f, channel0, 250f);

            return mfw;
        }

        /// <summary>@brief
        /// Four consecutive quarters played independently of the tempo.
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_four_notes_ticks()
        {
            // In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Using multiple tracks is not mandatory,  you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change. By convention contains no noteon.
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 1;

            int channel0 = 0; // we are using only one channel in this demo

            long absoluteTime = 0;


            // Create a Midi file of type 1 (recommended)
            MidiFileWriter2 mfw = new MidiFileWriter2();

            mfw.MPTK_AddTimeSignature(0, 0, 4, 4);

            // 240 is the default. A classical value for a Midi. define the time precision.
            int ticksPerQuarterNote = mfw.MPTK_DeltaTicksPerQuarterNote;

            // Some textual information added to the track 0 at time=0
            mfw.MPTK_AddText(track0, 0, MPTKMeta.Copyright, "Simple MIDI Generated. 4 quarter at 120 BPM");

            // Define Tempo is not mandatory when using time in ticks. The default 120 BPM will be used.
            //mfw.MPTK_AddBPMChange(track0, 0, 120);

            // Add four consecutive quarters from 60 (C5)  to 63.
            mfw.MPTK_AddNote(track1, absoluteTime, channel0, 60, 50, ticksPerQuarterNote);

            // Next note will be played one quarter after the previous
            absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddNote(track1, absoluteTime, channel0, 61, 50, ticksPerQuarterNote);

            absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddNote(track1, absoluteTime, channel0, 62, 50, ticksPerQuarterNote);

            absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddNote(track1, absoluteTime, channel0, 63, 50, ticksPerQuarterNote);

            // Silent note two quarter
            absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddSilence(track1, absoluteTime, channel0, ticksPerQuarterNote * 2);


            return mfw;
        }

        /// <summary>@brief
        /// Midi Generated with MPTK with tempo, preset, pitch wheel change
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_preset_tempo_pitchWheel()
        {
            // In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Using multiple tracks is not mandatory,  you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change. By convention contains no noteon.
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 1;

            int channel0 = 0; // we are using only one channel in this demo

            // https://paxstellar.fr/2020/09/11/midi-timing/
            int beatsPerMinute = 60;

            // a classical value for a Midi. define the time precision
            int ticksPerQuarterNote = 500;

            // Create a Midi file of type 1 (recommended)
            MidiFileWriter2 mfw = new MidiFileWriter2(ticksPerQuarterNote, 1);


            // Time to play a note expressed in ticks.
            // All durations are expressed in ticks, so this value can be used to convert
            // duration notes as quarter to ticks. https://paxstellar.fr/2020/09/11/midi-timing/
            // If ticksPerQuarterNote = 120 and absoluteTime = 120 then the note will be played a quarter delay from the start.
            // If ticksPerQuarterNote = 120 and absoluteTime = 1200 then the note will be played a 10 quarter delay from the start.
            long absoluteTime = 0;

            // Some textual information added to the track 0 at time=0
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "MIDI Generated with MPTK with tempo, preset, pitch wheel change");

            // TimeSignatureEvent (not mandatory)   https://paxstellar.fr/2020/09/11/midi-timing/
            //      Numerator(number of beats in a bar, 
            //      Denominator(which is confusingly) in 'beat units' so 1 means 2, 2 means 4(crochet), 3 means 8(quaver), 4 means 16 and 5 means 32), 
            mfw.MPTK_AddTimeSignature(track0, absoluteTime, 4, 2); // for a 4/4 signature

            // Tempo is defined in beat per minute (not mandatory, by default MIDI are played with a tempo of 120).
            // beatsPerMinute set to 60 at start, it's a slow tempo, one quarter per second.
            // Tempo is global for the whole MIDI independantly of each track and channel.
            mfw.MPTK_AddBPMChange(track0, absoluteTime, beatsPerMinute);

            // Preset for channel 1. Generally 25 is Acoustic Guitar, see https://en.wikipedia.org/wiki/General_MIDI
            // It seems that some reader (as Media Player) refused Midi file if change preset is defined in the track 0, so we set it in track 1.
            mfw.MPTK_AddChangePreset(track1, absoluteTime, channel0, 25);

            //
            // Build first bar
            // ---------------

            // Creation of the first bar in the partition : 
            //      add four quarter with a tick duration of one quarter (one second with BPM=60)
            //          57 --> A4   
            //          60 --> C5   
            //          62 --> D5  
            //          65 --> F5 

            // Some lyrics added to the track 0
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.Lyric, "Build first bar");

            mfw.MPTK_AddNote(track1, absoluteTime, channel0, note: 57, velocity: 50, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)

            mfw.MPTK_AddNote(track1, absoluteTime, channel0, note: 60, velocity: 80, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote;

            mfw.MPTK_AddNote(track1, absoluteTime, channel0, note: 62, velocity: 100, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote;

            mfw.MPTK_AddNote(track1, absoluteTime, channel0, note: 65, velocity: 100, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote;

            //
            // Build second bar: Same notes but dobble tempo (using the microsecond per quarter change method)
            // -----------------------------------------------------------------------------------------------
            mfw.MPTK_AddTempoChange(track0, absoluteTime, MidiFileWriter2.MPTK_BPM2MPQN(beatsPerMinute * 2));

            //return mfw;

            mfw.MPTK_AddNote(track1, absoluteTime, channel0, note: 57, velocity: 50, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)

            mfw.MPTK_AddNote(track1, absoluteTime, channel0, note: 60, velocity: 80, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote;

            mfw.MPTK_AddNote(track1, absoluteTime, channel0, note: 62, velocity: 100, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote;

            mfw.MPTK_AddNote(track1, absoluteTime, channel0, note: 65, velocity: 100, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote;

            //
            // Build third bar : one note with a pitch change along the bar
            // -------------------------------------------------------------

            mfw.MPTK_AddChangePreset(track1, absoluteTime, channel0, 50); // synth string

            // Some lyrics added to the track 0
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.Lyric, "Pitch wheel effect");

            // Play an infinite note A4 (duration = -1) don't forget the noteoff!
            mfw.MPTK_AddNote(track1, absoluteTime, channel0, note: 57, velocity: 100, length: -1);

            // Apply pitch wheel on the channel 0
            for (float pitch = 0f; pitch <= 2f; pitch += 0.05f) // 40 steps of 0.05
            {
                mfw.MPTK_AddPitchWheelChange(track1, absoluteTime, channel0, pitch);
                // Advance position 40 steps and for a total duration of 4 quarters
                absoluteTime += (long)((float)ticksPerQuarterNote * 4f / 40f);
            }

            // The noteoff for A4
            mfw.MPTK_AddOff(track1, absoluteTime, channel0, 57);

            // Reset pitch change to normal value
            mfw.MPTK_AddPitchWheelChange(track1, absoluteTime, channel0, 0.5f);

            //
            // Build fourth bar : arpeggio of 16 sixteenth along the bar 
            // --------------------------------------------------------

            // Some lyrics added to the track 0
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.Lyric, "Arpeggio");

            // Dobble the tempo with a variant of MPTK_AddBPMChange, 
            // change tempo defined in microsecond. Use MPTK_BPM2MPQN to convert or use directly MPTK_AddBPMChange
            //mfw.MPTK_AddTempoChange(track0, absoluteTime, MidiFileWriter2.MPTK_BPM2MPQN(beatsPerMinute));

            // Patch/preset to use for channel 1. Generally 11 is Music Box, see https://en.wikipedia.org/wiki/General_MIDI
            mfw.MPTK_AddChangePreset(track1, absoluteTime, channel0, 11);

            // Add sixteenth notes (duration = quarter / 4) : need 16 sixteenth to build a bar of 4 quarter
            int note = 57;
            for (int i = 0; i < 16; i++)
            {
                mfw.MPTK_AddNote(track1, absoluteTime, channel0, note: note, velocity: 100, ticksPerQuarterNote / 4);
                // Advance the position by one sixteenth 
                absoluteTime += ticksPerQuarterNote / 4;
                note += 1;
            }

            //
            // Build fifth bar : one whole note with vibrato
            // ----------------------------------------------

            // Some lyrics added to the track 0
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.Lyric, "Vibrato");

            // Add a last whole note (4 quarters duration = 1 bar)
            mfw.MPTK_AddNote(track1, absoluteTime, channel0, note: 85, velocity: 100, length: ticksPerQuarterNote * 4);

            // Apply modulation change, (vibrato)
            mfw.MPTK_AddControlChange(track1, absoluteTime, channel0, MPTKController.Modulation, 127);

            absoluteTime += ticksPerQuarterNote * 4;

            // Reset modulation change to normal value
            mfw.MPTK_AddControlChange(track1, absoluteTime, channel0, MPTKController.Modulation, 0);

            //
            // wrap up : add a silence
            // -----------------------------

            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.Lyric, "Silence");
            absoluteTime += ticksPerQuarterNote;

            // Silent note one quarter
            mfw.MPTK_AddSilence(track1, absoluteTime, channel0, length: ticksPerQuarterNote);

            // Now it's useless, track ending is automatically done
            //mfw.MPTK_EndTrack(track0);
            //mfw.MPTK_EndTrack(track1);

            return mfw;
        }



        private MidiFileWriter2 CreateMidiStream_Chords()
        {
            // In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Using multiple tracks is not mandatory,  you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change. By convention contains no noteon.
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 1;

            int channel0 = 0; // we are using only one channel in this demo

            // https://paxstellar.fr/2020/09/11/midi-timing/
            int beatsPerMinute = 60; // One quarter per second

            // a classical value for a Midi. define the time precision
            int ticksPerQuarterNote = 500;

            // Create a Midi file of type 1 (recommended)
            MidiFileWriter2 mfw = new MidiFileWriter2(ticksPerQuarterNote, 1);

            // Time to play a note expressed in ticks.
            // All durations are expressed in ticks, so this value can be used to convert
            // duration notes as quarter to ticks. https://paxstellar.fr/2020/09/11/midi-timing/
            // If ticksPerQuarterNote = 120 and absoluteTime = 120 then the note will be played a quarter delay from the start.
            // If ticksPerQuarterNote = 120 and absoluteTime = 1200 then the note will be played a 10 quarter delay from the start.
            long absoluteTime = 0;

            // Patch/preset to use for channel 1. Generally 40 is violin, see https://en.wikipedia.org/wiki/General_MIDI and substract 1 as preset begin at 0 in MPTK
            mfw.MPTK_AddChangePreset(track1, absoluteTime, channel0, 40);

            mfw.MPTK_AddBPMChange(track0, absoluteTime, beatsPerMinute);

            // Some textual information added to the track 0 at time=0
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "Play chords");

            // Defined a duration of one quarter in millisecond
            long duration = (long)mfw.MPTK_ConvertTickToMilli(ticksPerQuarterNote);


            // From https://apprendre-le-home-studio.fr/bien-demarrer-ta-composition-46-suites-daccords-danthologie-a-tester-absolument-11-idees-de-variations/ (sorry, in french but it's more a note for me !)

            //! [ExampleMidiWriterBuildChordFromRange]

            // Play 4 chords, degree I - V - IV - V 
            // ------------------------------------
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "Play 4 chords, degree I - V - IV - V ");

            // We need degrees in major, so build a major range
            MPTKRangeLib rangeMajor = MPTKRangeLib.Range(MPTKRangeName.MajorHarmonic);

            // Build chord degree 1
            MPTKChordBuilder chordDegreeI = new MPTKChordBuilder()
            {
                // Parameters to build the chord
                Tonic = 60, // play in C
                Count = 3,  // 3 notes to build the chord (between 2 and 20, of course it doesn't make sense more than 7, its only for fun or experiementation ...)
                Degree = 1,
                // Midi Parameters how to play the chord
                Duration = duration, // millisecond, -1 to play indefinitely
                Velocity = 80, // Sound can vary depending on the velocity

                // Optionnal MPTK specific parameters
                Arpeggio = 0, // delay in milliseconds between each notes of the chord
                Delay = 0, // delay in milliseconds before playing the chord
            };

            // Build chord degree V
            MPTKChordBuilder chordDegreeV = new MPTKChordBuilder() { Tonic = 60, Count = 3, Degree = 5, Duration = duration, Velocity = 80, };

            // Build chord degree IV
            MPTKChordBuilder chordDegreeIV = new MPTKChordBuilder() { Tonic = 60, Count = 3, Degree = 4, Duration = duration, Velocity = 80, };

            // Add degrees I - V - IV - V in the MIDI (all in major) 
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMajor, chordDegreeI); absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMajor, chordDegreeV); absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMajor, chordDegreeIV); absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMajor, chordDegreeV); absoluteTime += ticksPerQuarterNote;

            //! [ExampleMidiWriterBuildChordFromRange]

            // Add a silent just by moving the time of the next event for one quarter
            absoluteTime += ticksPerQuarterNote;

            // Play 4 others chords, degree  I – VIm – IIm – V
            // -----------------------------------------------
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "Play 4 chords, degree I – VIm – IIm – V");

            // We need 2 degrees in minor, build a minor range
            MPTKRangeLib rangeMinor = MPTKRangeLib.Range(MPTKRangeName.MinorHarmonic);

            // then degree 2 and 6
            MPTKChordBuilder chordDegreeII = new MPTKChordBuilder() { Tonic = 60, Count = 3, Degree = 2, Duration = duration, Velocity = 80, };
            MPTKChordBuilder chordDegreeVI = new MPTKChordBuilder() { Tonic = 60, Count = 3, Degree = 6, Duration = duration, Velocity = 80, };

            // Add degrees I – VIm – IIm – V intp the MidiFileWriter2 MIDI stream
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMajor, chordDegreeI); absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMinor, chordDegreeVI); absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMinor, chordDegreeII); absoluteTime += ticksPerQuarterNote;
            mfw.MPTK_AddChordFromRange(track1, absoluteTime, channel0, rangeMajor, chordDegreeV); absoluteTime += ticksPerQuarterNote;

            // Add a silent
            absoluteTime += ticksPerQuarterNote;


            // Play 4 chords from library
            // --------------------------
            mfw.MPTK_AddText(track0, absoluteTime, MPTKMeta.SequenceTrackName, "Play 4 chords from library");

            // Piano
            mfw.MPTK_AddChangePreset(track1, absoluteTime, channel0, 0);

            //! [ExampleMidiWriterBuildChordFromLib]

            MPTKChordBuilder chordLib = new MPTKChordBuilder() { Tonic = 60, Duration = duration, Velocity = 80, };
            mfw.MPTK_AddChordFromLib(track1, absoluteTime, channel0, MPTKChordName.Major, chordLib); absoluteTime += ticksPerQuarterNote;
            chordLib.Tonic = 62;
            mfw.MPTK_AddChordFromLib(track1, absoluteTime, channel0, MPTKChordName.mM7, chordLib); absoluteTime += ticksPerQuarterNote;
            chordLib.Tonic = 67;
            mfw.MPTK_AddChordFromLib(track1, absoluteTime, channel0, MPTKChordName.m7b5, chordLib); absoluteTime += ticksPerQuarterNote;
            chordLib.Tonic = 65;
            mfw.MPTK_AddChordFromLib(track1, absoluteTime, channel0, MPTKChordName.M7, chordLib); absoluteTime += ticksPerQuarterNote;

            // Then add a silence with a mute note (mandatory to have a silence at the end)
            mfw.MPTK_AddSilence(track1, absoluteTime, channel0, ticksPerQuarterNote);
            //! [ExampleMidiWriterBuildChordFromLib]


            // Return a MidiFileWriter2 object to be played or write
            // see PlayDirectlyMidiSequence() or WriteMidiSequenceToFileAndPlay ()
            return mfw;
        }


        /// <summary>@brief
        /// Play four consecutive quarters from 60 (C5) to 63.
        /// Use AddNoteMS method for Tempo and duration defined in milliseconds.
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_sandbox()
        {
            //In this demo, we are using variable to contains tracks and channel values only for better understanding. 

            // Track is interesting to structure your Midi. It will be more readable on a sequencer. 
            // Also, track has no effect on the music, must not be confused with channel!
            // Using multiple tracks is not mandatory,  you can arrange your song as you want.
            // But first track (index=0) is often use for general MIDI information track, lyrics, tempo change. By convention contains no noteon.
            // Track management is done automatically, they are created and ended when needed. There is no real limit, but this class limit the count to 64
            int track0 = 0;

            // Second track (index=1) will contains the notes, preset change, .... all events associated to a channel.
            int track1 = 111;

            int channel0 = 0; // we are using only one channel in this demo
            int channel1 = 1; // we are using only one channel in this demo

            // Create a Midi file of type 1 (recommended)
            MidiFileWriter2 mfw = new MidiFileWriter2();

            // Some textual information added to the track 0 at time=0
            mfw.MPTK_AddText(track0, 0, MPTKMeta.SequenceTrackName, "Sandbox");
            mfw.MPTK_AddChangePreset(track1, 0, channel0, 65); // alto sax
            mfw.MPTK_AddChangePreset(track1, 0, channel1, 18); // rock organ

            mfw.MPTK_AddBPMChange(track0, 0, 120);

            mfw.MPTK_AddTextMilli(track0, 3000f, MPTKMeta.TextEvent, "Alto Sax, please");
            mfw.MPTK_AddNoteMilli(track1, 1000f, channel0, 62, 50, -1);
            mfw.MPTK_AddOffMilli(track1, 4000f, channel0, 62);

            mfw.MPTK_AddNoteMilli(track1, 10, channel0, 60, 50, -1);
            mfw.MPTK_AddOffMilli(track1, 3000, channel0, 60);

            mfw.MPTK_AddTextMilli(track0, 3000f, MPTKMeta.TextEvent, "Rock Organ, please");
            mfw.MPTK_AddNoteMilli(track1, 3000f, channel1, 65, 50, 3000);
            mfw.MPTK_AddNoteMilli(track1, 3500f, channel1, 66, 50, 2500);
            mfw.MPTK_AddNoteMilli(track1, 4000f, channel1, 67, 50, 2000);


            mfw.MPTK_AddNoteMilli(track1, 1000f, channel1, 62, 50, -1);
            mfw.MPTK_AddOffMilli(track1, 4000f, channel1, 62);

            mfw.MPTK_AddTextMilli(track0, 6000f, MPTKMeta.TextEvent, "Ending Bip");

            mfw.MPTK_AddNoteMilli(track1, 6000f, channel0, 80, 50, 100f);
            return mfw;
        }

        /// <summary>@brief
        /// Midi Generated with MPTK for unitary test
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_full_crescendo()
        {
            int ticksPerQuarterNote = 500;

            MidiFileWriter2 mfw = new MidiFileWriter2(ticksPerQuarterNote, 1);

            long absoluteTime = 0;

            mfw.MPTK_AddBPMChange(track: 0, absoluteTime, 240);
            mfw.MPTK_AddChangePreset(track: 1, absoluteTime, channel: 0, 0);

            for (int velocity = 0; velocity <= 127; velocity += 5)
            {
                // Duration = 1 second for a quarter at BPM 60
                mfw.MPTK_AddNote(track: 1, absoluteTime, channel: 0, note: 60, velocity: velocity, length: ticksPerQuarterNote);
                absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)
            }
            // wrap up : add a silence of a ticksPerQuarterNote
            mfw.MPTK_AddSilence(track: 1, absoluteTime, channel: 0, length: ticksPerQuarterNote * 4);

            return mfw;
        }

        /// <summary>@brief
        /// Midi Generated with MPTK for unitary test
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_short_crescendo_with_noteoff_tick()
        {
            int ticksPerQuarterNote = 500;

            MidiFileWriter2 mfw = new MidiFileWriter2(ticksPerQuarterNote, 1);

            long absoluteTime = 0;

            mfw.MPTK_AddBPMChange(track: 0, absoluteTime, 240);
            mfw.MPTK_AddChangePreset(track: 1, absoluteTime, channel: 0, 0);

            for (int velocity = 40; velocity <= 80; velocity += 5)
            {
                // Duration = 0.25 second for a quarter at BPM 240
                mfw.MPTK_AddNote(track: 1, absoluteTime, channel: 0, note: 60, velocity: velocity, length: -1);
                absoluteTime += ticksPerQuarterNote; // Noteoff one quarter after and will be also the next noteon
                mfw.MPTK_AddOff(track: 1, absoluteTime, channel: 0, note: 60);
            }
            // wrap up : add a silence of a ticksPerQuarterNote
            mfw.MPTK_AddSilence(track: 1, absoluteTime, channel: 0, length: ticksPerQuarterNote * 4);

            return mfw;
        }

        /// <summary>@brief
        /// Midi Generated with MPTK for unitary test
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_short_crescendo_with_noteoff_ms()
        {
            int ticksPerQuarterNote = 500;

            MidiFileWriter2 mfw = new MidiFileWriter2(ticksPerQuarterNote, 1);

            float timeToPlay = 0f;

            mfw.MPTK_AddBPMChange(track: 0, tick: 0, bpm: 240);
            mfw.MPTK_AddChangePreset(track: 1, tick: 0, channel: 0, preset: 0);

            for (int velocity = 40; velocity <= 80; velocity += 5)
            {
                // Duration = 1 second for a quarter at BPM 60
                mfw.MPTK_AddNoteMilli(track: 1, timeToPlay: timeToPlay, channel: 0, note: 60, velocity: velocity, duration: -1);
                timeToPlay += 250; // Noteoff 100 milliseconds after and will be also the next noteon
                mfw.MPTK_AddOffMilli(track: 1, timeToPlay: timeToPlay, channel: 0, note: 60);
            }
            // wrap up : add a silence of a ticksPerQuarterNote
            //      mfw.MPTK_AddSilenceMilli(track: 1, timeToPlay: timeToPlay, channel: 0, duration: 100);

            return mfw;
        }


        /// <summary>@brief
        /// Play four consecutive quarters from 60 (C5) to 63.
        /// Use AddNoteMS method for Tempo and duration defined in milliseconds.
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_four_notes_only()
        {
            MidiFileWriter2 mfw = new MidiFileWriter2(deltaTicksPerQuarterNote: 500);
            mfw.MPTK_AddNote(1, 0 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 60, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddNote(1, 1 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 61, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddNote(1, 2 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 62, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddNote(1, 3 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 63, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            return mfw;
        }

        /// <summary>@brief
        /// Play 3x4 quarters with a tempo change.
        /// Use AddNoteMS method for Tempo and duration defined in milliseconds.
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_tempochange()
        {
            MidiFileWriter2 mfw = new MidiFileWriter2(deltaTicksPerQuarterNote: 500);
            mfw.MPTK_AddBPMChange(1, 0 * mfw.MPTK_DeltaTicksPerQuarterNote, 60);
            mfw.MPTK_AddNote(1, 0 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 60, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddNote(1, 1 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 61, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddNote(1, 2 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 62, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddNote(1, 3 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 63, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddBPMChange(1, 4 * mfw.MPTK_DeltaTicksPerQuarterNote, 120);
            mfw.MPTK_AddNote(1, 4 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 60, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddNote(1, 5 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 61, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddNote(1, 6 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 62, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddNote(1, 7 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 63, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddBPMChange(1, 8 * mfw.MPTK_DeltaTicksPerQuarterNote, 240);
            mfw.MPTK_AddNote(1, 8 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 60, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddNote(1, 9 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 61, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddNote(1, 10 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 62, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            mfw.MPTK_AddNote(1, 11 * mfw.MPTK_DeltaTicksPerQuarterNote, 0, 63, 60, mfw.MPTK_DeltaTicksPerQuarterNote);
            return mfw;
        }

        /// <summary>@brief
        /// Create some note, meta event not in order and check stable sort
        /// Use AddNoteMS method for Tempo and duration defined in milliseconds.
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_stable_sort()
        {
            MidiFileWriter2 mfw = new MidiFileWriter2(deltaTicksPerQuarterNote: 500);
            mfw.MPTK_AddText(0, 0, MPTKMeta.Lyric, "some text");
            mfw.MPTK_AddNote(1, tick: 500, 0, 61, 100, 500);
            mfw.MPTK_AddNote(1, 0, 0, 60, 100, 500);
            mfw.MPTK_AddChangePreset(1, 0, 0, 100);
            mfw.MPTK_AddChangePreset(1, 10, 0, 100);
            mfw.MPTK_AddText(0, 0, MPTKMeta.Lyric, "other text");
            mfw.MPTK_StableSortEvents(logPerf: true);
            return mfw;
        }
        //! [ExampleMIDIImport]
        /// <summary>@brief
        /// Join two MIDI fromm the MidiDB
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_midi_merge()
        {
            MidiFileWriter2 mfw = null;
            try
            {
                // Create a Midi File Writer instance
                mfw = new MidiFileWriter2();

                // A MIDI loader is usefull to load all MIDI events from a MIDI file.
                MidiFileLoader mfLoader = FindObjectOfType<MidiFileLoader>();
                if (mfLoader == null)
                {
                    Debug.LogWarning("Can't find a MidiFileLoader Prefab in the current Scene Hierarchy. Add it with the Maestro menu.");
                    return null;
                }

                // It's mandatory to keep noteoff when loading MIDI events for merging
                mfLoader.MPTK_KeepNoteOff = true;
                // it's recommended to not keep end track
                mfLoader.MPTK_KeepEndTrack = false;

                // Load the initial MIDI index 0 from the MidiDB
                mfLoader.MPTK_MidiIndex = 0;
                mfLoader.MPTK_Load();
                // All merge operation will be done with the ticksPerQuarterNote of the first MIDI
                int ticksPerQuarterNote = mfLoader.MPTK_DeltaTicksPerQuarterNote;
                mfw.MPTK_ImportFromEventsList(mfLoader.MPTK_MidiEvents, ticksPerQuarterNote, name: mfLoader.MPTK_MidiName);
                Debug.Log($"{mfLoader.MPTK_MidiName} Loaded {mfLoader.MPTK_MidiEvents.Count} events");

                // Load the MIDI index 1 from the MidiDB 
                mfLoader.MPTK_MidiIndex = 1;
                mfLoader.MPTK_Load();
                // All MIDI events loaded will be added to the MidiFileWriter2.
                // Position and Duration will be converted according the ticksPerQuarterNote initial and ticksPerQuarterNote in parameter.
                mfw.MPTK_ImportFromEventsList(mfLoader.MPTK_MidiEvents, ticksPerQuarterNote, name: "MidiMerged");
                Debug.Log($"{mfLoader.MPTK_MidiName} Loaded {mfLoader.MPTK_MidiEvents.Count} events added {mfw.MPTK_MidiEvents.Count} events merged");

                // Add a silence of a 4 Quarter Notes after the last event.
                // It's optionnal but recommended if you want to loop on the generated MIDI with a silence before looping.
                long absoluteTime = mfw.MPTK_MidiEvents.Last().Tick + mfw.MPTK_MidiEvents.Last().Length;
                Debug.Log($"Add a silence at {mfw.MPTK_MidiEvents.Last().Tick} + {mfw.MPTK_MidiEvents.Last().Length} = {absoluteTime} ");
                mfw.MPTK_AddSilence(track: 1, absoluteTime, channel: 0, length: ticksPerQuarterNote * 4);
            }
            catch (Exception ex) { Debug.LogException(ex); }

            // 
            return mfw;
        }
        /// <summary>@brief
        /// Midi Generated with MPTK for unitary test
        /// </summary>
        /// <returns></returns>
        private MidiFileWriter2 CreateMidiStream_silence()
        {
            int ticksPerQuarterNote = 500;

            MidiFileWriter2 mfw = new MidiFileWriter2(ticksPerQuarterNote, 1);

            long absoluteTime = 0;

            mfw.MPTK_AddBPMChange(track: 0, absoluteTime, 60);
            mfw.MPTK_AddChangePreset(track: 1, absoluteTime, channel: 0, 21);

            // Duration = 1 second for a quarter at BPM 60
            mfw.MPTK_AddNote(track: 1, absoluteTime, channel: 0, note: 57, velocity: 60, length: ticksPerQuarterNote);
            absoluteTime += ticksPerQuarterNote; // Next note will be played one quarter after the previous (time signature is 4/4)

            // wrap up : add a silence of 2 seconds
            mfw.MPTK_AddSilence(track: 1, absoluteTime, channel: 0, length: ticksPerQuarterNote * 4);

            return mfw;
        }


        //! [ExampleMIDIImport]

        //! [ExampleMIDIPlayFromWriter]
        private void PlayDirectlyMidiSequence(string name, MidiFileWriter2 mfw)
        {
            // Play MIDI with the MidiExternalPlay prefab without saving MIDI in a file
            MidiFilePlayer midiPlayer = FindObjectOfType<MidiFilePlayer>();
            if (midiPlayer == null)
            {
                Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
                return;
            }

            midiPlayer.MPTK_Stop();
            mfw.MPTK_MidiName = name;

            midiPlayer.OnEventStartPlayMidi.RemoveAllListeners();
            midiPlayer.OnEventStartPlayMidi.AddListener((string midiname) =>
            {
                startPlaying = DateTime.Now;
                Debug.Log($"Start playing '{midiname}'");
            });

            midiPlayer.OnEventEndPlayMidi.RemoveAllListeners();
            midiPlayer.OnEventEndPlayMidi.AddListener((string midiname, EventEndMidiEnum reason) =>
            {
                Debug.Log($"End playing '{midiname}' {reason} Real Duration={(DateTime.Now - startPlaying).TotalSeconds:F3} seconds");
            });

            midiPlayer.OnEventNotesMidi.RemoveAllListeners();
            midiPlayer.OnEventNotesMidi.AddListener((List<MPTKEvent> events) =>
            {
                foreach (MPTKEvent midievent in events)
                    Debug.Log($"At {midievent.RealTime:F1} ms play: {midievent.ToString()}");
            });


            // Sort the events by ascending absolute time (optional)
            mfw.MPTK_StableSortEvents();

            // Send the MIDI sequence to the internal MIDI sequencer
            // -----------------------------------------------------
            midiPlayer.MPTK_Loop = loop;
            midiPlayer.MPTK_Play(mfw2: mfw, fromTick: loopFrom, toTick: loopTo, timePosition: false);
        }
        //! [ExampleMIDIPlayFromWriter]

        //! [ExampleMIDIWriteAndPlay]
        private void WriteMidiSequenceToFileAndPlay(string name, MidiFileWriter2 mfw)
        {
            // build the path + filename to the midi
            string filename = Path.Combine(Application.persistentDataPath, name + ".mid");
            Debug.Log("Write MIDI file:" + filename);

            // Sort the events by ascending absolute time (optional)
            mfw.MPTK_StableSortEvents();
            mfw.MPTK_Debug();

            // Write the MIDI file
            mfw.MPTK_WriteToFile(filename);

            // Need an external player to play MIDI from a file from a folder
            MidiExternalPlayer midiExternalPlayer = FindObjectOfType<MidiExternalPlayer>();
            if (midiExternalPlayer == null)
            {
                Debug.LogWarning("Can't find a MidiExternalPlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
                return;
            }
            midiExternalPlayer.MPTK_Stop();

            // this prefab is able to load a MIDI file from the device or from an url (http)
            // -----------------------------------------------------------------------------
            midiExternalPlayer.MPTK_MidiName = "file://" + filename;

            midiExternalPlayer.OnEventStartPlayMidi.RemoveAllListeners();
            midiExternalPlayer.OnEventStartPlayMidi.AddListener((string midiname) => { Debug.Log($"Start playing {midiname}"); });

            midiExternalPlayer.OnEventEndPlayMidi.RemoveAllListeners();
            midiExternalPlayer.OnEventEndPlayMidi.AddListener((string midiname, EventEndMidiEnum reason) => { Debug.Log($"End playing {midiname} {reason}"); });

            midiExternalPlayer.MPTK_Loop = loop;
            // Mandatory to keep noteoff with external player with a generated MIDI file.
            midiExternalPlayer.MPTK_KeepNoteOff = true;

            midiExternalPlayer.MPTK_Play();
        }
        //! [ExampleMIDIWriteAndPlay]

        //! [ExampleMIDIWriteToDB]
        private void WriteMidiToMidiDB(string name, MidiFileWriter2 mfw)
        {
            // build the path + filename to the midi
            string filename = Path.Combine(Application.persistentDataPath, name + ".mid");
            Debug.Log("Write MIDI file:" + filename);

            // Sort the events by ascending absolute time (optional)
            mfw.MPTK_StableSortEvents();

            mfw.MPTK_Debug();

            // Write the MIDI file
            mfw.MPTK_WriteToMidiDB(filename);
            //AssetDatabase.Refresh();

            //// Can't play immediately a MIDI file added to the MIDI DB. It's a Unity resource
            //// The MIDI is not yet available at this time.

            //// Need an external player to play MIDI from a file from a folder
            //MidiFilePlayer midiPlayer = FindObjectOfType<MidiFilePlayer>();
            //if (midiPlayer == null)
            //{
            //    Debug.LogWarning("Can't find a MidiFilePlayer Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
            //    return;
            //}
            //midiPlayer.MPTK_Stop();

            //// this prefab is able to load a MIDI file from the device or from an url (http)
            //// -----------------------------------------------------------------------------
            //midiPlayer.MPTK_MidiName = filename;

            //midiPlayer.OnEventStartPlayMidi.RemoveAllListeners();
            //midiPlayer.OnEventStartPlayMidi.AddListener((string midiname) => { Debug.Log($"Start playing {midiname}"); });

            //midiPlayer.OnEventEndPlayMidi.RemoveAllListeners();
            //midiPlayer.OnEventEndPlayMidi.AddListener((string midiname, EventEndMidiEnum reason) => { Debug.Log($"End playing {midiname} {reason}"); });

            //midiPlayer.MPTK_Loop = loop;
            //// Mandatory to keep noteoff with external player with a generated MIDI file.
            //midiPlayer.MPTK_KeepNoteOff = true;

            //midiPlayer.MPTK_Play();
        }

        //! [ExampleMIDIWriteAndPlay]
        private static void StopAllPlaying()
        {
            MidiExternalPlayer midiExternalPlayer = FindObjectOfType<MidiExternalPlayer>();
            if (midiExternalPlayer != null)
                midiExternalPlayer.MPTK_Stop();
            MidiFilePlayer midiFilePlayer = FindObjectOfType<MidiFilePlayer>();
            if (midiFilePlayer != null)
                midiFilePlayer.MPTK_Stop();
        }
    }
}

