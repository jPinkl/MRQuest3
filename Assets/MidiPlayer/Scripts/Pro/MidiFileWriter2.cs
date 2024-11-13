using MidiPlayerTK;
using MPTK.NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace MidiPlayerTK
{
    /// <summary>
    /// Build and Write a MIDI file from different sources.\n
    /// @version Maestro Pro 
    /// 
    /// See full example with these scripts:\n
    /// @li  TestMidiGenerator.cs for an example of MIDI stream creation.\n 
    /// @li  TinyMidiSequencer.cs for a light sequencer.\n
    /// \n
    /// This class replace MidiFileWriter with these changes: channel start at 0, new specfic event, better control.\n 
    /// More information here: https://paxstellar.fr/class-midifilewriter2/
    /// </summary>
    public class MidiFileWriter2
    {
        /// <summary>@brief
        /// Delta Ticks Per Quarter Note (or DTPQN) represent the duration time in "ticks" which make up a quarter-note. \n
        /// For example, with 96 a duration of an eighth-note in the file would be 48.\n
        /// From a MIDI file, this value is found in the MIDI Header and remains constant for all the MIDI file.\n
        /// More info here https://paxstellar.fr/2020/09/11/midi-timing/\n
        /// </summary>
        public int MPTK_DeltaTicksPerQuarterNote;

        /// <summary>@brief
        /// From TimeSignature event: The numerator counts the number of beats in a measure.\n
        /// For example a numerator of 4 means that each bar contains four beats.\n
        /// This is important to know because usually the first beat of each bar has extra emphasis.\n
        /// https://paxstellar.fr/2020/09/11/midi-timing/
        /// </summary>
        public int MPTK_NumberBeatsMeasure;


        private int _bpm;

        /// <summary>@brief
        /// @deprecated 
        /// with V2.9.0 - use MPTK_CurrentTempo in place.
        /// </summary>
        public int Bpm { get { Debug.LogWarning("Bpm is deprecated, rather use MPTK_CurrentTempo"); return _bpm; } }


        /// <summary>@brief
        /// Get current Microseconds Per Quater Note:  60 * 1000 * 1000 https://en.wikipedia.org/wiki/Tempo\n
        /// @details
        /// The tempo in a MIDI file is given in micro seconds per quarter beat. To convert this to BPM use method #MPTK_MPQN2BPM.\n
        /// This value can change during the generation when #MPTK_AddTempoChange is called.\n
        /// See here for more information https://paxstellar.fr/2020/09/11/midi-timing/        
        /// </summary>
        public int MPTK_MicrosecondsPerQuaterNote
        {
            get { return _bpm > 0 ? 60 * 1000 * 1000 / _bpm : 0; }
        }

        /// <summary>@brief
        /// @deprecated 
        /// with V2.9.0 - use MPTK_BPM2MPQN in place - Convert BPM to duration of a quarter in microsecond
        /// </summary>
        /// <param name="bpm">m</param>
        /// <returns></returns>
        public static int MPTK_GetMicrosecondsPerQuaterNote(int bpm)
        {
            Debug.LogWarning("MPTK_GetMicrosecondsPerQuaterNote is deprecated, rather use MPTK_BPM2MPQN in place");
            return 60 * 1000 * 1000 / bpm;
        }

        /// <summary>@brief
        /// Convert bmp to duration of a quarter in microsecond: 60000000/ bpm
        /// </summary>
        /// <param name="bpm">m</param>
        /// <returns></returns>
        public static int MPTK_BPM2MPQN(int bpm)
        {
            return bpm > 0 ? 60000000 / bpm : 0;
        }

        /// <summary>@brief
        /// Convert duration of a quarter in microsecond to Beats Per Minute
        /// </summary>
        /// <param name="microsecondsPerQuaterNote"></param>
        /// <returns></returns>
        public static int MPTK_MPQN2BPM(int microsecondsPerQuaterNote)
        {
            return microsecondsPerQuaterNote > 0 ? 60000000 / microsecondsPerQuaterNote : 0;
        }

        /// <summary>@brief
        /// V2.9.0 - Get the current tempo. Set with #MPTK_AddTempoChange.\n
        /// https://en.wikipedia.org/wiki/Tempo
        /// </summary>
        public double MPTK_Tempo => _bpm;

        // @cond NODOC
        // List of all tempo changes found in the MIDI. Not yet mature to be published.
        public List<MPTKTempo> MPTK_TempoMap;
        // @endcond


        /// <summary>@brief
        /// Lenght in millisecond of a MIDI tick.\n
        /// @details
        /// The pulse length is the minimum time in millisecond between two MIDI events.\n
        /// It's like a definition of resolution: the MIDI sequencer will not be able to play\n
        /// two separate MIDI events in a time below this value.\n
        /// Obviously depends on the current tempo (#MPTK_CurrentTempo) and the #MPTK_DeltaTicksPerQuarterNote.\n
        /// PulseLenght = 60000 / MPTK_CurrentTempo / MPTK_DeltaTicksPerQuarterNote
        /// </summary>
        public float MPTK_PulseLenght { get { return _bpm > 0 && MPTK_DeltaTicksPerQuarterNote > 0 ? (60000f / (float)_bpm) / (float)MPTK_DeltaTicksPerQuarterNote : 0f; } }

        /// <summary>@brief
        /// Convert the tick duration to a real time duration in millisecond regarding the current tempo.
        /// </summary>
        /// <param name="tick">duration in ticks</param>
        /// <returns>duration in milliseconds</returns>
        public long MPTK_ConvertTickToMilli(long tick)
        {
            return (long)(tick * MPTK_PulseLenght + 0.5f);
        }

        /// <summary>@brief
        /// Convert a real time duration in millisecond to a number of tick regarding the current tempo.
        /// </summary>
        /// <param name="time">duration in milliseconds</param>
        /// <returns>duration in ticks</returns>
        public long MPTK_ConvertMilliToTick(float time)
        {
            return MPTK_PulseLenght > 0d ? Convert.ToInt64(time / MPTK_PulseLenght) : 0L;
        }

        /// <summary>@brief
        /// Get the count of track. The value is available only when MPTK_CreateTracksStat() has been called.
        /// There no more limit of count of track with V2.9.0
        /// </summary>
        public int MPTK_TrackCount { get { return MPTK_TrackStat?.Count ?? 0; } }

        /// <summary>@brief
        /// Get the MIDI file type of the loaded MIDI (0,1,2)
        /// </summary>
        public int MPTK_MidiFileType;

        /// <summary>@brief
        /// Name of this MIDI stream.
        /// </summary>
        public string MPTK_MidiName;

        /// <summary>@brief
        /// Get all the MIDI events created.
        /// </summary>
        public List<MPTKEvent> MPTK_MidiEvents;

        /// <summary>@brief
        /// Last MIDI events created.
        /// </summary>
        public MPTKEvent MPTK_LastEvent => MPTK_MidiEvents == null || MPTK_MidiEvents.Count == 0 ? null : MPTK_MidiEvents[MPTK_MidiEvents.Count - 1];

        /// <summary>@brief
        /// Tick position of the last MIDI event found including the duration of this event.
        /// </summary>
        public long MPTK_TickLast;

        // @cond NODOC
        // Not yet mature to be published.
        // Track information, built with MPTK_CreateTracksStat. It's a dictionary with the track number as a key and the item holds some information about the track.
        public Dictionary<long, MPTKStat> MPTK_TrackStat;
        // @endcond

        /// <summary>@brief
        /// Count of MIDI events in the MPTK_Events
        /// </summary>
        public int MPTK_CountEvent
        {
            get { return MPTK_MidiEvents == null ? 0 : MPTK_MidiEvents.Count; }
        }

        /// <summary>@brief
        /// Create an empty MidiFileWriter2 with default or specific header midi value (for advanced use)\n
        /// Default:\n
        /// @li Delta Ticks Per Quarter Note = 240 \n
        /// @li Midi file type = 1 \n
        /// @li Beats Per Minute = 120\n
        /// </summary>
        /// <param name="deltaTicksPerQuarterNote">Delta Ticks Per Quarter Note, default is 240. See #MPTK_DeltaTicksPerQuarterNote.</param>
        /// <param name="midiFileType">type of Midi format. Must be 0 or 1, default 1</param>
        /// <param name="bpm">Initial Beats Per Minute, default 120</param>
        public MidiFileWriter2(int deltaTicksPerQuarterNote = 240, int midiFileType = 1, int bpm = 120)
        {
            MPTK_MidiEvents = new List<MPTKEvent>();
            MPTK_DeltaTicksPerQuarterNote = deltaTicksPerQuarterNote;
            MPTK_NumberBeatsMeasure = 4;
            MPTK_MidiFileType = midiFileType;
            MPTK_TickLast = 0;
            MPTK_TempoMap = new List<MPTKTempo>();

            _bpm = bpm;
        }

        /// <summary>@brief
        /// Remove all MIDI events and restore default attributs:
        /// @li MPTK_DeltaTicksPerQuarterNote = 240
        /// @li MPTK_MidiFileType = 1
        /// @li Tempo = 120
        /// </summary>
        public void MPTK_Clear()
        {
            if (MPTK_TrackStat != null)
                MPTK_TrackStat.Clear();
            //MPTK_DeltaTicksPerQuarterNote = 240;
            MPTK_MidiFileType = 1;
            _bpm = 120;
            MPTK_TickLast = 0;
            MPTK_MidiEvents.Clear();
        }

        // @cond NODOC
        // Update Tempo Map
        public void MPTK_CreateTempoMap()
        {
            MPTK_TempoMap.Clear();
            MPTK_MidiEvents.ForEach(e =>
            {
                if (e.Command == MPTKCommand.MetaEvent && e.Meta == MPTKMeta.SetTempo)
                    AddToTempoMap((int)e.Track, e.Tick, e.Value);
            });
        }
        // @endcond

        /// <summary>@brief
        /// New with version V2.9.0 Import a list of MPTKEvent.\n
        /// @details
        /// Multiple imports can be done for joining MIDI events from different sources @emoji grin.\n
        /// @details
        /// @li The first import will be the reference for the DeltaTicksPerQuarterNote (MPTK_DeltaTicksPerQuarterNote is set with the value in parameter).
        /// @li The next imports will convert time and duration of the MIDI events with the ratio of DeltaTicksPerQuarterNote in parameter and the initial DeltaTicksPerQuarterNote.
        /// @snippet TestMidiGenerator.cs ExampleMIDIImport
        /// @attention If loading the MIDI events from a MIDI file, it's mandatory to keep the noteoff events: set MidiFileLoader#MPTK_KeepNoteOff to true.
        /// </summary>
        /// <param name="midiEventsToInsert">List of MPTKEvent</param>
        /// <param name="deltaTicksPerQuarterNote">
        /// It's the DTPQN of the MIDI events to insert. \n
        /// @li If there is not yet MIDI events in #MPTK_MidiEvents, that will be the default #MPTK_DeltaTicksPerQuarterNote of the MidiFileWriter2 instance.\n
        /// @li If there is already MIDI events in #MPTK_MidiEvents, the timing of MIDI events in #midiEventsToInsert will be converted accordingly.\n
        /// </param>
        /// <param name="position">tick position to insert, -1 to append, 0 at beguinning</param>
        /// <param name="name">Name of the MIDI created (set MPTK_MidiName).</param>
        /// <param name="logDebug">Debug log.</param>
        public bool MPTK_ImportFromEventsList(List<MPTKEvent> midiEventsToInsert, int deltaTicksPerQuarterNote, long position = -1, string name = null, bool logDebug = false)
        {
            bool ok = false;
            try
            {

                if (!string.IsNullOrEmpty(name))
                    MPTK_MidiName = name;

                if (logDebug) Debug.Log($"***** MPTK_ImportFromEventsList to {name}");

                if (deltaTicksPerQuarterNote <= 0)
                    throw new MaestroException($"deltaTicksPerQuarterNote cannot be < 0, found {deltaTicksPerQuarterNote}");

                // MPTK_Events is instancied with the instance of the class, so no worry, can't be null
                if (MPTK_MidiEvents.Count == 0)
                {
                    // No event, add at beginning
                    position = 0;
                    // when no event already exist, take the DTPQN in parameters
                    MPTK_DeltaTicksPerQuarterNote = deltaTicksPerQuarterNote;
                    if (logDebug) Debug.Log($"Set MPTK_DeltaTicksPerQuarterNote to {MPTK_DeltaTicksPerQuarterNote}");
                }

                float ratioDTPQN = 1f;
                long shiftTick = 0;
                float shiftTime = 0f;

                if (deltaTicksPerQuarterNote != MPTK_DeltaTicksPerQuarterNote)
                    ratioDTPQN = (float)MPTK_DeltaTicksPerQuarterNote / (float)deltaTicksPerQuarterNote;

                if (logDebug)
                {
                    Debug.Log($"Count events in source={MPTK_MidiEvents.Count} MPTK_DeltaTicksPerQuarterNote={MPTK_DeltaTicksPerQuarterNote}");
                    Debug.Log($"Count events to import={midiEventsToInsert.Count} DTPQN={deltaTicksPerQuarterNote}");
                    Debug.Log($"ratio DTPQN = {ratioDTPQN}");
                }

                if (position == 0)
                {
                    // Insert at the beguining, get event information from the last MIDI event to insert
                    // ---------------------------------------------------------------------------------
                    if (logDebug) Debug.Log("Insert at the beguining");

                    if (ratioDTPQN != 1f)
                    {
                        if (logDebug) Debug.Log("Convert imported events to the DTPQN");

                        // DTPQN conversion
                        midiEventsToInsert.ForEach(midiEvent =>
                        {
                            midiEvent.Tick = (long)(midiEvent.Tick * ratioDTPQN);
                            midiEvent.RealTime = midiEvent.RealTime * ratioDTPQN;
                            // NON duréee en ms, ne tient pas compte du dtpqn
                            // midiEvent.Duration = (long)(midiEvent.Duration * ratioDTPQN);
                            midiEvent.Length = (int)(midiEvent.Length * ratioDTPQN);
                        }
                        );
                    }

                    if (MPTK_MidiEvents.Count != 0)
                    {
                        MPTKEvent insert = midiEventsToInsert.Last();
                        shiftTick = insert.Tick + insert.Length; // only noteon have a length, be careful with endtrack at the last position 
                        shiftTime = insert.RealTime;
                        if (logDebug) Debug.Log($"Shift {MPTK_MidiEvents.Count} source events, shift tick={shiftTick} shift time={shiftTime}");

                        // time shift source event
                        MPTK_MidiEvents.ForEach(midiEvent =>
                        {
                            midiEvent.Tick += shiftTick;
                            midiEvent.RealTime += shiftTime;
                        }
                        );
                    }
                    else if (logDebug) Debug.Log("No events in source, no time shifting");

                    // Insert at beguining
                    MPTK_MidiEvents.InsertRange(0, midiEventsToInsert);
                }
                else if (position < 0 || position >= MPTK_MidiEvents.Last().Tick)
                {
                    // Append at the end, get event information from the last MIDI event of the source
                    // -------------------------------------------------------------------------------
                    if (logDebug) Debug.Log("Append at the end");

                    MPTKEvent insert = MPTK_MidiEvents.Last();
                    shiftTick = insert.Tick + insert.Length;  // only noteon have a length, be careful with endtrack at the last position 
                    shiftTime = insert.RealTime;
                    if (logDebug) Debug.Log($"Shift {midiEventsToInsert.Count} imported events, shift tick={shiftTick} shift time={shiftTime}");

                    // shift event to append + DTPQN conversion
                    midiEventsToInsert.ForEach(midiEvent =>
                    {
                        midiEvent.Tick = (long)(midiEvent.Tick * ratioDTPQN) + shiftTick;
                        midiEvent.RealTime = midiEvent.RealTime * ratioDTPQN + shiftTime;
                    }
                    );

                    // Append, works also if there is no event in the source
                    MPTK_MidiEvents.AddRange(midiEventsToInsert);
                }
                else
                {
                    // Insert anywhere!!! get event information from the last MIDI event of the source
                    // -------------------------------------------------------------------------------
                    if (logDebug) Debug.Log($"Insert at tick position {position}");

                    int indexToInsert = MidiLoad.MPTK_SearchEventFromTick(MPTK_MidiEvents, position);
                    if (logDebug) Debug.Log($"Insert at index position {indexToInsert}");
                    if (indexToInsert < 0)
                        Debug.Log($"Not possible to insert at position tick {position}");
                    else
                    {
                        //// insert after the group with the same tick in origine
                        //while (index < MPTK_MidiEvents.Count && MPTK_MidiEvents[index].Tick >= position)
                        //    index++;
                        //Debug.Log($"Insert corrected {index} for tick {position}");

                        // get  event information from the MIDI event where to insert
                        MPTKEvent sourceEvent = MPTK_MidiEvents[indexToInsert];
                        shiftTick = sourceEvent.Tick + sourceEvent.Length;
                        shiftTime = sourceEvent.RealTime;

                        if (logDebug) Debug.Log($"Shift {midiEventsToInsert.Count} imported events, shift tick={shiftTick} shift time={shiftTime}");

                        // Time shift event to insert + DTPQN conversion
                        midiEventsToInsert.ForEach(midiEvent =>
                        {
                            midiEvent.Tick = (long)(midiEvent.Tick * ratioDTPQN) + shiftTick;
                            midiEvent.RealTime = midiEvent.RealTime * ratioDTPQN + shiftTime;
                        }
                        );

                        // Insert at the position found
                        MPTK_MidiEvents.InsertRange(indexToInsert, midiEventsToInsert);

                        // Correct events after (time shift of the source events), take tick of the last events inserted
                        MPTKEvent insert = midiEventsToInsert.Last();
                        shiftTick = insert.Tick;
                        shiftTime = insert.RealTime;

                        if (logDebug) Debug.Log($"Shift {midiEventsToInsert.Count} source events from postiion {indexToInsert}, shift tick={shiftTick} shift time={shiftTime}");

                        for (int index = indexToInsert + midiEventsToInsert.Count; index < MPTK_MidiEvents.Count; index++)
                        {

                            MPTK_MidiEvents[index].Tick += shiftTick;
                            MPTK_MidiEvents[index].RealTime += shiftTime;
                        }
                    }
                    //MPTK_MidiEvents.RemoveRange(index, MPTK_MidiEvents.Count - index);
                    ok = true;
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /// <summary>@brief
        /// @deprecated 
        /// With V2.9.0 rather use MPTK_ImportFromEventsList.
        /// </summary>
        public bool MPTK_LoadFromMPTK(/*List<TrackMidiEvent> midiEvents, int track = 1*/)
        {
            Debug.LogWarning("MPTK_LoadFromMPTK is deprecated, rather use MPTK_ImportFromEventsList");
            return false;
        }

        /// <summary>@brief
        /// Load a Midi file from OS system file (could be dependant of the OS)
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool MPTK_LoadFromFile(string filename)
        {
            bool ok = false;
            try
            {
                MidiLoad midiLoad = new MidiLoad();
                midiLoad.MPTK_KeepNoteOff = true;
                if (midiLoad.MPTK_LoadFile(filename)) // corrected in 2.89.5 MPTK_Load --> MPTK_LoadFile (pro)
                {
                    MPTK_MidiEvents = midiLoad.MPTK_MidiEvents;
                    // Added in 2.89.5
                    MPTK_DeltaTicksPerQuarterNote = midiLoad.MPTK_DeltaTicksPerQuarterNote;
                    ok = true;
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /// <summary>@brief
        /// Create a MidiFileWriter2 from a Midi found in MPTK MidiDB. All existing MIDI events before the load will be lost.
        /// @code
        ///     // Create a midi file writer
        ///     MidiFileWriter2 mfw = new MidiFileWriter2();
        ///     // Load the selected midi from MidiDB index
        ///     mfw.MPTK_LoadFromMidiDB(selectedMidi);
        ///     // build the path + filename to the midi
        ///     string filename = Path.Combine(Application.persistentDataPath, MidiPlayerGlobal.CurrentMidiSet.MidiFiles[selectedMidi] + ".mid");
        ///     // write the midi file
        ///     mfw.MPTK_WriteToFile(filename);
        /// @endcode
        /// </summary>
        /// <param name="indexMidiDb"></param>
        public bool MPTK_LoadFromMidiDB(int indexMidiDb)
        {
            bool ok = false;
            try
            {
                if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
                {
                    if (indexMidiDb >= 0 && indexMidiDb < MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count - 1)
                    {
                        string midiname = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[indexMidiDb];
                        TextAsset mididata = Resources.Load<TextAsset>(Path.Combine(MidiPlayerGlobal.MidiFilesDB, midiname));
                        MidiLoad midiLoad = new MidiLoad();
                        midiLoad.MPTK_KeepNoteOff = true;
                        midiLoad.MPTK_Load(mididata.bytes);
                        MPTK_MidiEvents = midiLoad.MPTK_MidiEvents;
                        ok = true;
                    }
                    else
                        Debug.LogWarning("Index is out of the MidiDb list");
                }
                else
                    Debug.LogWarning("No MidiDb defined");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        // @cond NODOC
        // Not yet mature to be published.
        // Build the tracks information (MPTK_TrackStat) from the MIDI event found in MPTK_Events.
        // MPTK_TrackStat is a dictionary with the track number as a key and the item holds some informatio about the track.
        public Dictionary<long, MPTKStat> MPTK_CreateTracksStat()
        {
            if (MPTK_MidiEvents == null)
                throw new MaestroException("MPTK_Events is null");
            foreach (MPTKEvent midiEvent in MPTK_MidiEvents)
                UpdateStatTrack(midiEvent);
            return MPTK_TrackStat;
        }
        // @endcond

        private void UpdateStatTrack(MPTKEvent midiEvent)
        {
            if (MPTK_TrackStat == null) MPTK_TrackStat = new Dictionary<long, MPTKStat>();
            if (!MPTK_TrackStat.ContainsKey(midiEvent.Track)) MPTK_TrackStat[midiEvent.Track] = new MPTKStat();
            MPTK_TrackStat[midiEvent.Track].CountAll++;
            if (midiEvent.Command == MPTKCommand.NoteOn) MPTK_TrackStat[midiEvent.Track].CountNote++;
            if (midiEvent.Command == MPTKCommand.PatchChange) MPTK_TrackStat[midiEvent.Track].CountPreset++;
        }

        /// <summary>@brief
        /// @deprecated 
        /// With V2.9.0 tracks are automatically created when needed.
        /// </summary>
        /// <param name="count">number of tracks to create</param>
        public void MPTK_CreateTrack(int count)
        {
            Debug.LogWarning("The method MPTK_CreateTrack is deprecated. Tracks are automatically created when a new track id detected.");
        }

        /// <summary>@brief
        /// @deprecated 
        /// With V2.9.0 tracks are automatically closed when needed.
        /// </summary>
        /// <param name="trackNumber">Track number to close</param>
        public void MPTK_EndTrack(int trackNumber)
        {
            Debug.LogWarning("The method MPTK_EndTrack is deprecated. Tracks will be automatically ended when the midi is writed");
        }

        /// <summary>@brief
        /// Add a MPTK Midi event from a MPTKEvent instance. Useful to add a raw MIDI event.\n
        /// @details
        /// These attributs must be defined in the MPTKEvent instance:
        /// @li MPTKEvent.Track
        /// @li MPTKEvent.Channel
        /// @li MPTKEvent.Command
        /// @li MPTKEvent.Tick
        /// @note
        /// Others attributs must be defined depending on the value of MPTKEvent.Command, see class MidiPlayerTK.MPTKCommand.\n
        /// For example, MPTKEvent.Length must be defined if MPTKEvent.Command=MPTKCommand.NoteOn
        /// </summary>.
        /// <param name="mptkEvent"></param>
        public void MPTK_AddRawEvent(MPTKEvent mptkEvent)
        {
            try
            {
                if (mptkEvent == null) throw new MaestroException($"mptkEvent is null");
                if (mptkEvent.Channel < 0 || mptkEvent.Channel > 15) throw new MaestroException($"The channel must be >= 0 and <= 15, found {mptkEvent.Channel}");
                if (mptkEvent.Tick < 0) throw new MaestroException($"Position (tick or time) cannot be negative, found {mptkEvent.Tick}");
                if (mptkEvent.Track < 0) throw new MaestroException($"The number of the track ({mptkEvent.Track}) cannot be negative.");
                if (mptkEvent.Track == 0 && (
                        mptkEvent.Command == MPTKCommand.NoteOn ||
                        mptkEvent.Command == MPTKCommand.NoteOff ||
                        mptkEvent.Command == MPTKCommand.KeyAfterTouch ||
                        mptkEvent.Command == MPTKCommand.ControlChange ||
                        mptkEvent.Command == MPTKCommand.PatchChange ||
                        mptkEvent.Command == MPTKCommand.ChannelAfterTouch ||
                        mptkEvent.Command == MPTKCommand.PitchWheelChange)
                    )
                {
                    throw new MaestroException($"MIDI events based on channel (noteon, noteoff, patch change ...) cannot be defined on track 0.");
                }
                MPTK_MidiEvents.Add(mptkEvent);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a note on event at a specific time in millisecond. The corresponding Noteoff is automatically created if duration > 0\n
        /// If an infinite note-on is added (duration < 0), don't forget to add a note-off, it will never created automatically.
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="timeToPlay">Time in millisecond from the start of the MIDI when playing this event.</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="note">Note must be in the range 0-127</param>
        /// <param name="velocity">Velocity must be in the range 0-127.</param>
        /// <param name="duration">Duration in millisecond. No noteoff is added if duration is < 0, need to be added with MPTK_AddOffMilli</param>
        public void MPTK_AddNoteMilli(int track, float timeToPlay, int channel, int note, int velocity, float duration)
        {
            try
            {
                long tick = MPTK_ConvertMilliToTick(timeToPlay);
                int length = duration < 0 ? -1 : (int)MPTK_ConvertMilliToTick(duration);
                MPTK_AddNote(track, tick, channel, note, velocity, length);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a note on event at an absolute time (tick count). The corresponding Noteoff is automatically created if length > 0\n
        /// If an infinite note-on is added (length < 0), don't forget to add a note-off, it will never created automatically.
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="note">Note must be in the range 0-127</param>
        /// <param name="velocity">Velocity must be in the range 0-127.</param>
        /// <param name="length">Duration in tick. No automatic noteoff is added if duration is < 0, need to be added with MPTK_AddOff</param>
        public void MPTK_AddNote(int track, long tick, int channel, int note, int velocity, int length)
        {
            try
            {
                if (velocity < 0 || velocity > 127)
                {
                    throw new MaestroException($"Velocity must be >= 0 and <= 127, found {velocity}.");
                }

                if (length < 0)
                    // duration not specifed, set a default of a quarter (not taken into account by the synth). A next note off event will whange this duration.
                    MPTK_AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.NoteOn, Value = note, Velocity = velocity, Duration = -1, Length = -1 });
                else
                {
                    long duration_ms = MPTK_ConvertTickToMilli(length);
                    MPTK_AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.NoteOn, Value = note, Velocity = velocity, Duration = duration_ms, Length = length });
                    // It's better to create note-off when saving the MIDI file
                    // MPTK don't use note-off but the duration of the event
                    // But they are mandatory for the MIDI file norm .
                    // MPTK_AddRawEvent(new MPTKEvent() { Track = track, Tick = tick + length, Channel = channel, Command = MPTKCommand.NoteOff, Value = note, Velocity = 0 });
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a silence. 
        /// @note
        /// A silent note does not exist in the MIDI norm, we simulate it with a noteon and a very low velocity = 1.\n
        /// it's not possible to create a noteon with a velocity = 0, it's considered as a noteof by MIDI
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="timeToPlay">Time in millisecond from the start of the MIDI when playing this event.</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="duration">Duration in millisecond.</param>
        public void MPTK_AddSilenceMilli(int track, float timeToPlay, int channel, float duration)
        {
            try
            {
                long tick = MPTK_ConvertMilliToTick(timeToPlay);
                int durationSilence = duration <= 0 ? -1 : (int)MPTK_ConvertMilliToTick(duration);
                MPTK_AddSilence(track, tick, channel, durationSilence);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a silence.\n
        /// @note
        /// A silent note does not exist in the MIDI norm, we simulate it with a noteon and a very low velocity = 1.\n
        /// it's not possible to create a noteon with a velocity = 0, it's considered as a noteof by MIDI
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event.</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="length">Duration in tick.</param>
        public void MPTK_AddSilence(int track, long tick, int channel, int length)
        {
            try
            {
                MPTK_AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.NoteOn, Value = 0, Velocity = 1, Length = length });
                // It's better to create note-off when saving the MIDI file
                // MPTK don't use note-off but the duration of the event
                // But they are mandatory for the MIDI file norm .
                // MPTK_AddRawEvent(new MPTKEvent() { Track = track, Tick = tick + duration, Channel = channel, Command = MPTKCommand.NoteOff, Value = 0 });
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }
        /// <summary>@brief
        /// Add a note off event at a specific time in millisecond.\n
        /// Must always succeed the corresponding NoteOn, obviously on the same channel!
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="timeToPlay">Time in millisecond from the start of the MIDI when playing this event.</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="note">Note must be in the range 0-127</param>
        public void MPTK_AddOffMilli(int track, float timeToPlay, int channel, int note)
        {
            try
            {
                long tick = MPTK_ConvertMilliToTick(timeToPlay);
                MPTK_AddOff(track, tick, channel, note);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a note off event.\n
        /// Must always succeed the corresponding NoteOn, obviously on the same channel!
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="note">Note must be in the range 0-127</param>
        public void MPTK_AddOff(int track, long tick, int channel, int note)
        {
            try
            {
                bool found = false;
                for (int index = MPTK_MidiEvents.Count - 1; index >= 0; index--)
                {
                    MPTKEvent mPTKEvent = MPTK_MidiEvents[index];
                    if (mPTKEvent.Channel == channel && mPTKEvent.Command == MPTKCommand.NoteOn && mPTKEvent.Value == note)
                    {
                        int length = Convert.ToInt32(tick - mPTKEvent.Tick);
                        if (length > 0)
                        {
                            found = true;
                            mPTKEvent.Length = length;
                            mPTKEvent.Duration = MPTK_ConvertTickToMilli(length);
                            break;
                        }
                    }
                }
                if (!found)
                    Debug.LogWarning($"No NoteOn found corresponding to this NoteOff: track={track} channel={channel} tick={tick} note={note}");
                //MPTK_AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.NoteOff, Value = note });
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        private void CalculateLastTick(MPTKEvent midiEvent)
        {
            if (MPTK_TickLast == 0)
                MPTK_TickLast = midiEvent.Tick + midiEvent.Length;
        }

        /// <summary>@brief
        /// Add a chord from a range
        /// @snippet TestMidiGenerator.cs ExampleMidiWriterBuildChordFromRange
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick"></param>
        /// <param name="channel"></param>
        /// <param name="range">See MPTKRangeLib</param>
        /// <param name="chord">See MPTKChordBuilder</param>
        public void MPTK_AddChordFromRange(int track, long tick, int channel, MPTKRangeLib range, MPTKChordBuilder chord)
        {
            try
            {
                chord.MPTK_BuildFromRange(range);
                foreach (MPTKEvent evnt in chord.Events)
                    MPTK_AddNote(track, tick, channel, evnt.Value, evnt.Velocity, (int)MPTK_ConvertMilliToTick(evnt.Duration));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a chord from a library of chord
        /// @snippet TestMidiGenerator.cs ExampleMidiWriterBuildChordFromLib
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick"></param>
        /// <param name="channel"></param>
        /// <param name="chordName">Name of the chord See #MPTKChordName</param>
        /// <param name="chord">See MPTKChordBuilder</param>
        public void MPTK_AddChordFromLib(int track, long tick, int channel, MPTKChordName chordName, MPTKChordBuilder chord)
        {
            try
            {
                chord.MPTK_BuildFromLib(chordName);
                foreach (MPTKEvent evnt in chord.Events)
                    MPTK_AddNote(track, tick, channel, evnt.Value, evnt.Velocity, (int)MPTK_ConvertMilliToTick(evnt.Duration));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a change preset
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="timeToPlay">Time in millisecond from the start of the MIDI when playing this event.</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="preset">Preset (program/patch) must be in the range 0-127</param>
        public void MPTK_AddChangePresetMilli(int track, float timeToPlay, int channel, int preset)
        {
            try
            {
                long tick = MPTK_ConvertMilliToTick(timeToPlay);
                MPTK_AddChangePreset(track, tick, channel, preset);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a change preset
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="preset">Preset (program/patch) must be in the range 0-127</param>
        public void MPTK_AddChangePreset(int track, long tick, int channel, int preset)
        {
            try
            {
                MPTK_AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.PatchChange, Value = preset });
                //AddEvent(track, new PatchChangeEvent(absoluteTime, channel + 1, preset));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a Channel After-Touch Event
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="timeToPlay">Time in millisecond from the start of the MIDI when playing this event.</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="afterTouchPressure">After-touch pressure from 0 to 127</param>
        public void MPTK_AddChannelAfterTouchMilli(int track, float timeToPlay, int channel, int afterTouchPressure)
        {
            try
            {
                long tick = MPTK_ConvertMilliToTick(timeToPlay);
                MPTK_AddChannelAfterTouch(track, tick, channel, afterTouchPressure);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a Channel After-Touch Event
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="afterTouchPressure">After-touch pressure from 0 to 127</param>
        public void MPTK_AddChannelAfterTouch(int track, long tick, int channel, int afterTouchPressure)
        {
            try
            {
                MPTK_AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.ChannelAfterTouch, Value = afterTouchPressure });
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Creates a control change event
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="timeToPlay">Time in millisecond from the start of the MIDI when playing this event.</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="controller">The MIDI Controller. See #MPTKController</param>
        /// <param name="controllerValue">Controller value</param>
        public void MPTK_AddControlChangeMilli(int track, float timeToPlay, int channel, MPTKController controller, int controllerValue)
        {
            try
            {
                long tick = MPTK_ConvertMilliToTick(timeToPlay);
                MPTK_AddControlChange(track, tick, channel, controller, controllerValue);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Creates a general control change event (CC)
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="controller">The MIDI Controller. See #MPTKController</param>
        /// <param name="controllerValue">Controller value</param>
        public void MPTK_AddControlChange(int track, long tick, int channel, MPTKController controller, int controllerValue)
        {
            try
            {
                MPTK_AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.ControlChange, Controller = controller, Value = controllerValue });
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Creates a control change event (CC) for the pitch (Pitch Wheel)\n
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="timeToPlay">Time in millisecond from the start of the MIDI when playing this event.</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="pitchWheel">Pitch Wheel Value. 1:normal value, 0:pitch mini, 2:pitch max</param>
        public void MPTK_AddPitchWheelChangeMilli(int track, float timeToPlay, int channel, float pitchWheel)
        {
            try
            {
                long tick = MPTK_ConvertMilliToTick(timeToPlay);
                MPTK_AddPitchWheelChange(track, tick, channel, pitchWheel);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Creates a control change event (CC) for the pitch (Pitch Wheel)\n
        /// pitchWheel=
        /// @li  0      minimum (0 also for midi standard event value) 
        /// @li  0.5    centered value (8192 for midi standard event value) 
        /// @li  1      maximum (16383 for midi standard event value)
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="pitchWheel">Normalized Pitch Wheel Value. Range 0 to 1. V2.88.2 range normalized from 0 to 1.</param>
        public void MPTK_AddPitchWheelChange(int track, long tick, int channel, float pitchWheel)
        {
            try
            {
                int pitch = (int)Mathf.Lerp(0f, 16383f, pitchWheel); // V2.88.2 range normalized from 0 to 1
                                                                     //Debug.Log($"{pitchWheel} --> {pitch}");
                MPTK_AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.PitchWheelChange, Value = pitch });
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a tempo change to the midi stream. There is no channel in parameter because tempo change is apply to all tracks and channel.\n
        /// Next note-on with milliseconds defined after the tempo change will take into account the new value of the BPM.
        /// </summary>
        /// <param name="track">Track for this event (it's a good practive to use track 0 for this event)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="bpm">quarter per minute</param>
        public void MPTK_AddBPMChange(int track, long tick, int bpm)
        {
            if (bpm <= 0)
            {
                Debug.LogWarning("MPTK_AddBPMChange: BPM must > 0");
                return;
            }
            try
            {
                _bpm = bpm; // MPTK_MicrosecondsPerQuaterNote is calculated from the bpm
                            //Value contains new Microseconds Per Quarter Note and Duration contains new tempo (quarter per minute).
                MPTK_AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Command = MPTKCommand.MetaEvent, Meta = MPTKMeta.SetTempo, Value = MPTK_MicrosecondsPerQuaterNote, Duration = _bpm });
                AddToTempoMap(track, tick, MPTK_BPM2MPQN(bpm));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a tempo change to the midi stream in microseconds per quarter note. \n
        /// There is no channel in parameter because tempo change is apply to all tracks and channel.\n
        /// Next note-on with milliseconds defined after the tempo change will take into account the new value of the BPM.
        /// </summary>
        /// <param name="track">Track for this event (it's a good practive to use track 0 for this event)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="microsecondsPerQuarterNote">Microseconds per quarter note</param>
        public void MPTK_AddTempoChange(int track, long tick, int microsecondsPerQuarterNote)
        {
            if (microsecondsPerQuarterNote <= 0)
            {
                Debug.LogWarning("MPTK_AddBPMChange: Microseconds Per Quarter Note must > 0");
                return;
            }
            try
            {
                _bpm = MPTK_MPQN2BPM(microsecondsPerQuarterNote);
                //Value contains new Microseconds Per Quarter Note and Duration contains new tempo (quarter per minute).
                MPTK_AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Command = MPTKCommand.MetaEvent, Meta = MPTKMeta.SetTempo, Value = microsecondsPerQuarterNote, Duration = _bpm });
                AddToTempoMap(track, tick, microsecondsPerQuarterNote);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        private void AddToTempoMap(int track, long tick, int microsecondsPerQuarterNote)
        {
            double ratio = microsecondsPerQuarterNote / (double)MPTK_DeltaTicksPerQuarterNote / 1000d;
            double cumul = 0;
            if (MPTK_TempoMap.Count > 0)
            {
                MPTKTempo prevTempo = MPTK_TempoMap[MPTK_TempoMap.Count - 1];
                prevTempo.ToTick = tick;
                cumul = prevTempo.Cumul + (tick - prevTempo.FromTick) * prevTempo.Ratio;
            }

            MPTK_TempoMap.Add(new MPTKTempo()
            {
                Track = track,
                FromTick = tick,
                ToTick = long.MaxValue,
                Ratio = ratio,
                Cumul = cumul,
                MicrosecondsPerQuarterNote = microsecondsPerQuarterNote,
            });

            if (MPTK_TempoMap.Count > 1)
                MPTK_TempoMap = MPTK_TempoMap.OrderBy(o => o.FromTick).ToList();

        }
        /// <summary>@brief
        /// Create a new TimeSignatureEvent. This event is optionnal. 
        /// Internal Midi sequencer assumes the default value is 4,4,24,32.  No track nor channel as teampo change applied to the whole midi.
        /// </summary>
        /// <param name="track">Track for this event (it's a good practive to use track 0 for this event)</param>
        /// <param name="tick">Time at which to create this event</param>
        /// <param name="numerator">Numerator</param>
        /// <param name="denominator">Denominator</param>
        /// <param name="ticksInMetronomeClick">Ticks in Metronome Click. Set to 24 for a standard value.</param>
        /// <param name="no32ndNotesInQuarterNote">No of 32nd Notes in Quarter Click. Set to 32 for a standard value.</param>
        /// More info here https://paxstellar.fr/2020/09/11/midi-timing/
        public void MPTK_AddTimeSignature(int track, long tick, int numerator = 4, int denominator = 4, int ticksInMetronomeClick = 24, int no32ndNotesInQuarterNote = 32)
        {
            try
            {
                MPTK_NumberBeatsMeasure = numerator;
                // if Meta = TimeSignature,
                //      Value contains the numerator (number of beats in a bar)
                //      Duration contains the Denominator (Beat unit: 1 means 2, 2 means 4 (crochet), 3 means 8 (quaver), 4 means 16, ...)
                MPTK_AddRawEvent(new MPTKEvent()
                {
                    Track = track,
                    Tick = tick,
                    Command = MPTKCommand.MetaEvent,
                    Meta = MPTKMeta.TimeSignature,
                    Value = numerator,
                    Duration = denominator,
                    Length = ticksInMetronomeClick,
                });
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        // MPTK_AddKeySignature to be created
        // if Meta = KeySignature, Value contains the SharpsFlats (number of sharp) and Duration contains the MajorMinor flag (0 the scale is major, 1 the scale is minor).

        /// <summary>@brief
        /// Create a new TimeSignatureEvent. This event is optionnal. Midi sequencer assumes the default value is 4,4,24,32.  No track nor channel as teampo change applied to the whole midi.
        /// </summary>
        /// <param name="track">Track for this event (it's a good practice to use track 0 for this event)</param>
        /// <param name="timeToPlay">Time in millisecond from the start of the MIDI when playing this event.</param>
        /// <param name="typeMeta">MetaEvent type (must be one that is
        /// <param name="text">The text in this type</param>
        /// associated with text data)</param>
        public void MPTK_AddTextMilli(int track, float timeToPlay, MPTKMeta typeMeta, string text)
        {
            try
            {
                long tick = MPTK_ConvertMilliToTick(timeToPlay);
                MPTK_AddText(track, tick, typeMeta, text);
            }

            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Create a new TimeSignatureEvent. This event is optionnal. Midi sequencer assumes the default value is 4,4,24,32.  No track nor channel as teampo change applied to the whole midi.
        /// </summary>
        /// <param name="track">Track for this event (it's a good practice to use track 0 for this event)</param>
        /// <param name="tick">Absolute time of this event</param>
        /// <param name="typeMeta">MetaEvent type (must be one that is
        /// <param name="text">The text in this type</param>
        /// associated with text data)</param>
        public void MPTK_AddText(int track, long tick, MPTKMeta typeMeta, string text)
        {
            try
            {
                switch (typeMeta)
                {
                    case MPTKMeta.TextEvent:
                    case MPTKMeta.Copyright:
                    case MPTKMeta.DeviceName:
                    case MPTKMeta.Lyric:
                    case MPTKMeta.ProgramName:
                    case MPTKMeta.SequenceTrackName:
                    case MPTKMeta.Marker:
                    case MPTKMeta.TrackInstrumentName:
                        MPTK_AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Command = MPTKCommand.MetaEvent, Meta = typeMeta, Info = text });
                        break;
                    default:
                        throw new Exception($"MPTK_AddText need a meta event type for text. {typeMeta} is not correct.");
                }
            }

            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Delete all MIDI events on this channel
        /// </summary>
        /// <param name="channel"></param>
        public void MPTK_DeleteChannel(int channel)
        {
            if (MPTK_MidiEvents != null)
                for (int index = 0; index < MPTK_MidiEvents.Count;)
                {
                    if (MPTK_MidiEvents[index].Channel == channel)
                        MPTK_MidiEvents.RemoveAt(index);
                    else
                        index++;
                }
        }

        /// <summary>@brief
        /// Delete all MIDI events on this track
        /// </summary>
        /// <param name="track"></param>
        public void MPTK_DeleteTrack(int track)
        {
            if (MPTK_MidiEvents != null)
                for (int index = 0; index < MPTK_MidiEvents.Count;)
                {
                    if (MPTK_MidiEvents[index].Track == track)
                        MPTK_MidiEvents.RemoveAt(index);
                    else
                        index++;
                }
        }

        /// <summary>@brief
        /// Sort in place events in MPTK_MidiEvents by ascending tick position.
        /// First priority is applied for 'preset change' and 'meta' event for a group of events with the same position (but 'end track' are set at end of the group. 
        /// @note
        /// @li No reallocation of the list is done, the events in the list are sorted in place.
        /// @li good performance for low disorder 
        /// @li not efficient for high disorder list. Typically when reading a MIDI file, list is sorted by tracks.
        /// @li in case of high disorder the use of MPTK_SortEvents is recommended at the price of a realocation of the list.
        /// </summary>
        /// <param name="logPerf"></param>
        public void MPTK_StableSortEvents(bool logPerf = false)
        {
            if (MPTK_MidiEvents != null)
            {
                System.Diagnostics.Stopwatch watch = null;
                if (logPerf)
                {
                    watch = new System.Diagnostics.Stopwatch(); // High resolution time
                    watch.Start();
                }

                //// Quick sort - NO will realloc the list. 
                //MPTK_MidiEvents = MPTK_MidiEvents.OrderBy(o => o.Tick).ToList();
                //if (logPerf)
                //{
                //    Debug.Log($"Quick Sort time {watch.ElapsedMilliseconds} {watch.ElapsedTicks}");
                //    watch.Restart();
                //}

                // Then sort with priority on meta and preset change event (too long for a not pre-sorted list)
                MidiLoad.Sort(MPTK_MidiEvents, 0, MPTK_MidiEvents.Count - 1, new MidiLoad.MidiEventComparer());
                if (logPerf)
                {
                    Debug.Log($"Stable sort time {watch.ElapsedMilliseconds} {watch.ElapsedTicks}");
                    watch.Stop();
                }
            }
            else
                Debug.LogWarning("MidiFileWriter2 - MPTK_SortEvents - MidiEvents is null");
        }
      

        /// <summary>@brief
        /// Write Midi file to an OS folder
        /// @snippet TestMidiGenerator.cs ExampleMIDIWriteAndPlay
        /// </summary>
        /// <param name="filename">filename of the midi file</param>
        /// <returns>true if ok</returns>
        public bool MPTK_WriteToFile(string filename)
        {
            bool ok = false;
            try
            {
                if (MPTK_MidiEvents != null && MPTK_MidiEvents.Count > 0)
                {
                    MidiFile midiToSave = MPTK_BuildNAudioMidi();
                    // NAudio don't create noteoff associated to noteon! they need to be added if they are missing
                    MidiFile.Export(filename, midiToSave.Events);
                    ok = true;
                }
                else
                    Debug.LogWarning("MidiFileWriter2 - Write - MidiEvents is null or empty");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /// <summary>@brief
        /// Write Midi file to MidiDB.\n
        /// To be used only in edit mode not in a standalone application.\n
        /// A call to AssetDatabase.Refresh() must be required after the file has been added to the resource.
        /// </summary>
        /// <param name="filename">filename of the midi file without any folder and any extension</param>
        /// <returns>true if ok</returns>
        public bool MPTK_WriteToMidiDB(string filename)
        {
            bool ok = false;
            try
            {
                if (Application.isEditor)
                {
                    string filenameonly = Path.GetFileNameWithoutExtension(filename) + ".bytes";
                    // Build path to midi folder 
                    string pathMidiFile = Path.Combine(Application.dataPath, MidiPlayerGlobal.PathToMidiFile);
                    string filepath = Path.Combine(pathMidiFile, filenameonly);
                    //Debug.Log(filepath);
                    MPTK_WriteToFile(filepath);
                    // To be review, can't access class in the editor project ...
                    //MidiPlayerTK.ToolsEditor.CheckMidiSet();
                    string filenoext = Path.GetFileNameWithoutExtension(filename);
                    if (!MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Contains(filenoext))
                    {
                        Debug.Log($"Add MIDI '{filenoext}' to MidiDB");
                        MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Add(filenoext);
                        MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Sort();
                        MidiPlayerGlobal.CurrentMidiSet.Save();
                    }

                    ok = true;
                }
                else
                    Debug.LogWarning("WriteToMidiDB can be call only in editor mode not in a standalone application");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /// <summary>@brief
        ///  Build a NAudio midi object from the midi events. MPTK_WriteToMidiDB and MPTK_WriteToFile call these methods just before writing the MIDI file.
        /// </summary>
        /// <returns>NAudio MidiFile</returns>
        public MidiFile MPTK_BuildNAudioMidi()
        {
            MidiFile naudioMidi = new MidiFile(MPTK_MidiFileType, MPTK_DeltaTicksPerQuarterNote);

            if (MPTK_TrackStat == null)
                MPTK_CreateTracksStat();

            foreach (int track in MPTK_TrackStat.Keys)
            {
                if (MPTK_TrackStat[track].CountAll == 0)
                    Debug.LogWarning($"MPTK_BuildNAudioMidi - Track {track} is empty");
                else
                {
                    bool endTrack = false;
                    naudioMidi.Events.AddTrack();
                    long endLastEvent = 0;
                    long prevAbsEvent = 0;
                    //Debug.Log($"Build track {track}");
                    foreach (MPTKEvent mptkEvent in MPTK_MidiEvents)
                    {
                        if (mptkEvent.Track == track)
                        {
                            MidiEvent naudioEvent = null;
                            MidiEvent naudioNoteOff = null;
                            //MidiEvent naudioNoteOff = null;
                            try
                            {
                                switch (mptkEvent.Command)
                                {
                                    case MPTKCommand.NoteOn:
                                        if (mptkEvent.Length < 0)
                                            Debug.LogWarning($"MPTK_BuildNAudioMidi - NoteOn with negative duration not processed. NoteOff Missing? {mptkEvent}");
                                        else
                                            naudioEvent = new NoteOnEvent(mptkEvent.Tick, mptkEvent.Channel + 1, mptkEvent.Value, mptkEvent.Velocity, (int)mptkEvent.Length);
                                        // noteoff are already created if event has been added with MPTK_AddNote but not if loaded with MidiLoad and KeepNoteOff is false.
                                        // NAudio don't create noteoff associated to noteon! they need to be added if they are missing
                                        // Can be added now, the events will be sorted by NAudio (MergeSort)
                                        naudioNoteOff = new NoteEvent(mptkEvent.Tick + mptkEvent.Length, mptkEvent.Channel + 1, MidiCommandCode.NoteOff, mptkEvent.Value, 0);

                                        break;
                                    //case MPTKCommand.NoteOff:
                                    //    if (!addNoteOffAuto)
                                    //        // Noteoff are added only if automatic note off creation is off.
                                    //        naudioEvent = new NoteEvent(mptkEvent.Tick, mptkEvent.Channel + 1, MidiCommandCode.NoteOff, mptkEvent.Value, 0);
                                    //    break;
                                    case MPTKCommand.PatchChange:
                                        naudioEvent = new PatchChangeEvent(mptkEvent.Tick, mptkEvent.Channel + 1, mptkEvent.Value);
                                        break;
                                    case MPTKCommand.ControlChange:
                                        naudioEvent = new ControlChangeEvent(mptkEvent.Tick, mptkEvent.Channel + 1, (MidiController)mptkEvent.Controller, mptkEvent.Value);
                                        break;
                                    case MPTKCommand.ChannelAfterTouch:
                                        naudioEvent = new ChannelAfterTouchEvent(mptkEvent.Tick, mptkEvent.Channel + 1, mptkEvent.Value);
                                        break;
                                    case MPTKCommand.KeyAfterTouch:
                                        // Not processed by NAudio
                                        // naudioEvent = new KeyAfterTouchEvent(mptkEvent.Tick, mptkEvent.Channel + 1, mptkEvent.Value);
                                        break;
                                    case MPTKCommand.MetaEvent:
                                        switch (mptkEvent.Meta)
                                        {
                                            case MPTKMeta.SetTempo:
                                                // mptkEvent.Value = microsecondsPerQuarterNote 
                                                naudioEvent = new TempoEvent(mptkEvent.Value, mptkEvent.Tick);
                                                break;
                                            case MPTKMeta.TimeSignature:
                                                // no32ndNotesInQuarterNote: This byte is usually 8 as there is usually one quarter note per beat and one quarter note contains eight 32nd notes.
                                                // https://www.recordingblogs.com/wiki/midi-time-signature-meta-message#:~:text=Assuming%2024%20MIDI%20clocks%20per%20quarter%20note%2C%20if,and%20one%20quarter%20note%20contains%20eight%2032nd%20notes.
                                                naudioEvent = new TimeSignatureEvent(mptkEvent.Tick, mptkEvent.Value, (int)mptkEvent.Duration, (int)mptkEvent.Length, 8);
                                                break;
                                            case MPTKMeta.KeySignature:
                                                naudioEvent = new KeySignatureEvent(mptkEvent.Value, (int)mptkEvent.Duration, mptkEvent.Tick);
                                                break;
                                            case MPTKMeta.EndTrack:
                                                // v2.9.0 - don't add endtrack, they are automatically processed by Maestro
                                                Debug.LogWarning($"Do not add endtrack, they are automatically processed by Maestro, track:{track}");
                                                // naudioMidi.Events.AddEvent(new MetaEvent(MetaEventType.EndTrack, 0, mptkEvent.Tick), track);
                                                // End track, no more event will be added after this event for this track
                                                endTrack = true;
                                                break;
                                            case MPTKMeta.Marker:
                                            case MPTKMeta.MidiChannel:
                                            case MPTKMeta.MidiPort:
                                            case MPTKMeta.SmpteOffset:
                                            case MPTKMeta.CuePoint:
                                                // Not processed by Maestro
                                                break;

                                            default:
                                                naudioEvent = new TextEvent(mptkEvent.Info, (MetaEventType)mptkEvent.Meta, mptkEvent.Tick);
                                                break;
                                        }
                                        break;
                                    case MPTKCommand.PitchWheelChange:
                                        naudioEvent = new PitchWheelChangeEvent(mptkEvent.Tick, mptkEvent.Channel + 1, mptkEvent.Value);
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"Can't build event {mptkEvent} {ex}");
                            }
                            try
                            {
                                if (naudioEvent != null)
                                {
                                    naudioEvent.DeltaTime = (int)(naudioEvent.AbsoluteTime - prevAbsEvent);
                                    prevAbsEvent = naudioEvent.AbsoluteTime;
                                    naudioMidi.Events.AddEvent(naudioEvent, track);
                                    //Debug.Log($"   Add event {naudioEvent}");

                                    if (endLastEvent < naudioEvent.AbsoluteTime)
                                    {
                                        endLastEvent = naudioEvent.AbsoluteTime;
                                        // v2.9.0 - there is always a noteoff with noteon
                                        //if (naudioEvent.CommandCode == MidiCommandCode.NoteOn)
                                        //    // A noteoff event will be created, so time of last event will be more later
                                        //    endLastEvent += naudioEvent.DeltaTime;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"Can't add event {mptkEvent} {ex}");
                            }
                            try
                            {
                                if (naudioNoteOff != null)
                                {
                                    naudioNoteOff.DeltaTime = (int)(naudioNoteOff.AbsoluteTime - prevAbsEvent);
                                    prevAbsEvent = naudioNoteOff.AbsoluteTime;
                                    naudioMidi.Events.AddEvent(naudioNoteOff, track);
                                    if (endLastEvent < naudioNoteOff.AbsoluteTime)
                                    {
                                        endLastEvent = naudioNoteOff.AbsoluteTime;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"Can't add noteoff event {mptkEvent} {ex}");
                            }
                        }

                        if (endTrack)
                            // exit loop on each events for this track
                            break;
                    } // foreach event

                    if (!endTrack)
                    {
                        try
                        {
                            //Debug.Log($"Close track {track} at {endLastEvent}");
                            naudioMidi.Events.AddEvent(new MetaEvent(MetaEventType.EndTrack, 0, endLastEvent), track);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Can't add end track event {ex}");
                        }
                    }
                }
            } // foreach track

            //naudioMidi.Events.MidiFileType = MPTK_MidiFileType;
            //naudioMidi.Events.PrepareForExport();

            return naudioMidi;
        }

        /// <summary>@brief
        /// Log information about the MIDI
        /// </summary>
        /// <returns></returns>
        public bool MPTK_Debug()
        {
            bool ok = false;
            //
            // REWRITED with 2.9.0
            // 
            try
            {
                if (MPTK_MidiEvents != null && MPTK_MidiEvents.Count > 0)
                {
                    Debug.Log($"<b>---------------- MidiFileWriter2: MPTK_Debug ----------------</b>");
                    Debug.Log($"<b>MPTK_DeltaTicksPerQuarterNote: {MPTK_DeltaTicksPerQuarterNote}</b>");
                    Debug.Log($"<b>MPTK_TrackCount: {MPTK_TrackCount}</b>");
                    if (MPTK_TrackStat != null)
                        foreach (int track in MPTK_TrackStat.Keys)
                        {
                            if (MPTK_TrackStat[track].CountAll != 0)
                            {
                                Debug.Log($"   Track: {track,-2}\tCount event: {MPTK_TrackStat[track].CountAll,-3}\tPreset Change: {MPTK_TrackStat[track].CountPreset,-2}\tNote: {MPTK_TrackStat[track].CountNote}");
                            }
                        }
                    else
                        Debug.Log($"   No track stat available, call MPTK_CreateTracksStat() before.");

                    if (MPTK_TempoMap != null)
                    {
                        Debug.Log($"<b>MPTK_TempoMap: {MPTK_TempoMap.Count}</b>");
                        MPTK_TempoMap.ForEach(t =>
                        {
                            string to = t.ToTick == long.MaxValue ? "End" : t.ToTick.ToString();
                            Debug.Log($"   Track: {t.Track}\tFrom: {t.FromTick,-7:000000}\tTo:{to,-7:000000}\tRatio: {t.Ratio:F2}\tCumul: {t.Cumul:F2}\tBPM: {MPTK_MPQN2BPM(t.MicrosecondsPerQuarterNote)}");
                        });
                    }

                    Debug.Log($"<b>MIDI events: {MPTK_MidiEvents.Count}</b>");
                    foreach (MPTKEvent tmidi in MPTK_MidiEvents)
                    {
                        Debug.Log("   " + tmidi.ToString());
                    }
                    Debug.Log($"--------------------------------------------------------------");
                }
                else
                    Debug.LogWarning("MidiFileWriter2 - MPTK_Debug - MidiEvents is null or empty");


            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }


        /// <summary>@brief
        /// Log information about the MIDI
        /// </summary>
        /// <returns></returns>
        public bool MPTK_DebugRaw()
        {
            bool ok = false;
            //
            // REWRITED with 2.9.0
            // 
            try
            {
                if (MPTK_MidiEvents != null && MPTK_MidiEvents.Count > 0)
                {
                    MidiFile midifile = MPTK_BuildNAudioMidi();

                    Debug.Log($"---------------- MidiFileWriter2: MPTK_DebugRaw ----------------");
                    Debug.Log($"MidiFileType: {midifile.Events.MidiFileType}");
                    Debug.Log($"Tracks Count: {midifile.Tracks}");

                    if (midifile.Events.MidiFileType == 0 && midifile.Tracks > 1)
                    {
                        throw new ArgumentException("Can't export more than one track to a type 0 file");
                    }

                    for (int track = 0; track < midifile.Events.Tracks; track++)
                    {
                        IList<MidiEvent> eventList = midifile.Events[track];

                        long absoluteTime = midifile.Events.StartAbsoluteTime;

                        // use a stable sort to preserve ordering of MIDI events whose 
                        // absolute times are the same
                        //MergeSort.Sort(eventList, new MidiEventComparer());
                        if (eventList.Count > 0)
                        {
                            // TBN Change - error if no end track
                            Debug.Assert(MidiEvent.IsEndTrack(eventList[eventList.Count - 1]), "Exporting a track with a missing end track");
                        }
                        foreach (var midiEvent in eventList)
                        {
                            string info = $"   Track:{track} {midiEvent}";
                            if (midiEvent.CommandCode == MidiCommandCode.NoteOn)
                            {
                                NoteOnEvent ev = (NoteOnEvent)midiEvent;
                                if (ev.OffEvent != null)
                                    info += $" NoteOff at:  {ev.AbsoluteTime}";
                            }
                            Debug.Log(info);
                        }

                    }

                    //foreach (IList<MidiEvent> track in midifile.Events)
                    //{
                    //    foreach (MidiEvent nAudioMidievent in track)
                    //    {
                    //        string sEvent = nAudioMidievent.ToString();  //MidiScan.ConvertnAudioEventToString(nAudioMidievent, indexTrack);
                    //        if (sEvent != null)
                    //            Debug.Log("   " + sEvent);
                    //    }
                    //    indexTrack++;
                    //}

                    Debug.Log($"--------------------------------------------------------------");
                }
                else
                    Debug.LogWarning("MidiFileWriter2 - MPTK_DebugRaw - MidiEvents is null or empty");


            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        private static bool Test(string source, string target)
        {
            bool ok = false;
            try
            {
                MidiFile midifile = new MidiFile(source);
                MidiFile.Export(target, midifile.Events);
                ok = true;
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }
    }
}
