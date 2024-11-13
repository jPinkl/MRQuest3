#define DEBUG_LOGEVENT 
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using MPTK.NAudio.Midi;
using System;
using System.IO;
using System.Linq;

namespace MidiPlayerTK
{
    // Class for loading a Midi file. 
    // No sequencer, no synthetizer, so no music playing capabilities. 
    // Usefull to load all the Midi events from a Midi and process, transform, write them to what you want. 
    public partial class MidiLoad
    {
        /// <summary>@brief
        /// Search for a MIDI event from a tick position. v2.9.0\n
        /// </summary>
        /// <param name="tickSearched">tick position</param>
        /// <returns>MPTKEvent or null</returns>
        public static int MPTK_SearchEventFromTick(List<MPTKEvent> midiEvents, long tickSearched)
        {
            int index = -1;
            if (midiEvents == null)
            {
                Debug.LogWarning($"MPTK_SearchEventFromTick - MIDI events list is null");
            }
            else if (midiEvents.Count == 0)
            {
                index = 0;
            }
            else if (tickSearched <= 0)
                index = 0;
            else if (tickSearched >= midiEvents.Last().Tick)
                index = midiEvents.Count - 1;
            else
            {
                int lowIndex = 0;
                int highIndex = midiEvents.Count - 1;
                int middleIndex;
                long middleTicks;
                while (index < 0)
                {
                    middleIndex = (lowIndex + highIndex) / 2;
                    middleTicks = midiEvents[middleIndex].Tick;
                    if (tickSearched < middleTicks)
                        // before
                        highIndex = middleIndex;
                    else if (tickSearched > middleTicks)
                        // After
                        lowIndex = middleIndex;
                    else // tickSearched = middleTicks
                    {
                        index = middleIndex;
                        break;
                    }

                    if (lowIndex == highIndex)
                        // Found exact event with this tick or index adjacent
                        index = lowIndex;
                    else if (lowIndex + 1 == highIndex)
                        // index delta = 1, not divisible by 2
                        index = highIndex;
                }
                // Find event before with same tick 
                while (index > 0)
                {
                    if (midiEvents[index - 1].Tick != midiEvents[index].Tick)
                        break;
                    index--;
                }
            }
            //Debug.Log($"Insert at position {position} for tick {tick}");
            return index;
        }
        /// <summary>@brief
        /// Load MIDI file from a local file (Moved to PRO since version 2.89.5)
        /// </summary>
        /// <param name="filename">Midi path and filename to load (OS dependant)</param>
        /// <param name="strict">if true the MIDI must strictely respect the midi norm</param>
        /// <returns></returns>
        public bool MPTK_LoadFile(string filename, bool strict = false)
        {
            bool ok = true;
            try
            {
                using (Stream sfFile = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    byte[] data = new byte[sfFile.Length];
                    sfFile.Read(data, 0, (int)sfFile.Length);
                    ok = MPTK_Load(data, strict);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
                ok = false;
            }
            return ok;
        }
        /// <summary>@brief
        /// Load Midi from a MidiFileWriter2 object
        /// </summary>
        /// <param name="mfw2">MidiFileWriter2 object</param>
        /// <returns>true if loaded</returns>
        public bool MPTK_Load(MidiFileWriter2 mfw2)
        {
            InitMidiLoadAttributes();
            bool ok = true;
            try
            {
                timeStartLoad = DateTime.Now;
                //midifile = mfw2.MPTK_BuildNAudioMidi();
                //MPTK_MidiEvents = ConvertNAudioEventsToMPTKEvents();
                MPTK_MidiEvents = mfw2.MPTK_MidiEvents;
                MPTK_DeltaTicksPerQuarterNote = mfw2.MPTK_DeltaTicksPerQuarterNote;
                MPTK_MicrosecondsPerQuarterNote = MPTK_BPM2MPQN(120);
                MPTK_InitialTempo = MPTK_MPQN2BPM(MPTK_MicrosecondsPerQuarterNote);
                fluid_player_set_midi_tempo(MPTK_MicrosecondsPerQuarterNote);

                AnalyseTrackMidiEvent();
                if (MPTK_LogLoadEvents) MPTK_DisplayMidiAttributes();
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
                ok = false;
            }
            return ok;
        }
    }
}

