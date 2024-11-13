#define DEBUG_EDITOR
using System;
using System.Collections.Generic;
using System.IO;

namespace MidiPlayerTK
{
    using NUnit.Framework.Internal.Execution;
    using System.Xml.Serialization;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEngine;
    using static MidiPlayerTK.MidiFilePlayer;
    using Debug = UnityEngine.Debug;

    public partial class MidiEditorWindow : EditorWindow
    {
        #region internal classes
        public class ContextEditor
        {
            public string MidiName;
            public string PathOrigin;
            public bool Modified;

            /// <summary>
            /// Multiplier screen X to quarter length
            /// </summary>

            public float QuarterWidth;
            /// <summary>
            /// Height of a cell note
            /// </summary>
            public float CellHeight;

            /// <summary>
            /// 0:none, 1:whole, 2:half, 3:quarter, 4:height, 5:Sixteenth (16 quarter in a bar)
            /// </summary>
            public int IndexQuantization;

            public int DisplayTime;
            public int MidiIndex;
            public bool FollowEvent;
            public bool LogEvents;
            public bool LoopPlay;
            public long LoopStart;
            public long LoopEnd;
            public ModeStopPlay ModeLoop;
            public void SetDefaultSize()
            {
                QuarterWidth = 50f;
                CellHeight = 20f;
            }
            public ContextEditor()
            {
                MidiName = "name not defined";
                PathOrigin = "";
                Modified = false;
                IndexQuantization = 5;
                DisplayTime = 0;
                FollowEvent = false;
                LogEvents = false;
                LoopPlay = false;
                LoopStart = 0;
                LoopEnd = 0;
                ModeLoop = ModeStopPlay.StopWhenAllVoicesReleased;
                SetDefaultSize();
            }
        }

        class AreaUI
        {
            public enum AreaType
            {
                /// <summary>
                /// All channels, link to MainZone.SubZone[0]
                /// </summary>
                Channels = 0,

                /// <summary>
                /// All white wey, link to MainZone.SubZone[1]
                /// </summary>
                WhiteKeys = 1,

                /// <summary>
                /// All black wey, link to MainZone.SubZone[2]
                /// </summary>
                BlackKeys = 2,

                /// <summary>
                /// One channel, no link, multi channel 
                /// </summary>
                Channel,
            }

            public Rect Position;
            public AreaType areaType;
            public int Channel;
            public int Value;
            public MPTKEvent midiEvent;
            public List<AreaUI> SubArea;
            public override string ToString()
            {
                return $"Type:{areaType} Channel:{Channel} Position:{Position} MidiEvent:{midiEvent ?? midiEvent} Value:{Value}";
            }
        }

        class SectionAll
        {

            public Section[] Sections;
            private MidiFileWriter2 midiFileWriter;
            private float quarterWidth;
            private float cellHeight;
            /// <summary>
            /// Width of a measure = CellQuarterWidth * LoadedMidi.MPTK_NumberBeatsMeasure
            /// </summary>
            public float MeasureWidth;

            public SectionAll(MidiFileWriter2 midiFileWriter)
            {
                // Index 0 to 15 for MIDI Channel, section 16 for META events (Set tempo, ...)
                Sections = new Section[17];
                this.midiFileWriter = midiFileWriter;
            }

            public void InitSections()
            {
                Array.ForEach(Sections, section =>
                {
                    if (section != null)
                    {
                        section.LayoutNote.Count = 0;
                        section.LayoutPreset.Count = 0;
                    }
                });

                midiFileWriter.MPTK_MidiEvents.ForEach(midiEvent => UpdateSections(midiEvent));

                for (int s = 0; s < Sections.Length; s++)
                    if (Sections[s] != null && Sections[s].LayoutNote.Count == 0 && Sections[s].LayoutPreset.Count == 0)
                        Sections[s] = null;

                UpdateLayout(midiFileWriter.MPTK_MidiEvents);
            }

            public bool SectionExist(int section)
            {
                return Sections[section] != null;
            }

            public bool AddSection(int section)
            {
                if (SectionExist(section))
                    return false;

                Sections[section] = new Section(section);
                if (section < 16)
                {
                    // It's a MIDI channel creation
                    Sections[section].LayoutNote.LowerNote = 60;
                    Sections[section].LayoutNote.HigherNote = 65;
                    // A preset change has been added when a new section has been created
                    Sections[section].Presets.Add(new Section.PresetSet() { Line = 0, Value = 0 });
                }
                return true;
            }

            private void UpdateLayout(List<MPTKEvent> midiEventx)
            {
                foreach (Section section in Sections)
                    if (section != null)
                    {
                        section.LayoutNote.FindLowerHigherNotes(midiEventx, section.Channel);
                        section.Presets.Sort((p1, p2) => { return p1.Value.CompareTo(p2.Value); });
                        int line = 0;
                        section.Presets.ForEach(p => p.Line = line++);
                    }
            }

            public void UpdateSections(MPTKEvent midiEvent)
            {
                int channel = midiEvent.Channel;
                switch (midiEvent.Command)
                {
                    case MPTKCommand.NoteOn:
                        if (Sections[channel] == null) Sections[channel] = new Section(channel);
                        Sections[channel].LayoutNote.Count++;
                        break;
                    case MPTKCommand.NoteOff:
                        break;
                    case MPTKCommand.PatchChange:
                        if (Sections[channel] == null) Sections[channel] = new Section(channel);
                        int line = Sections[channel].GetPresetLine(midiEvent.Value);
                        if (line < 0)
                            Sections[channel].Presets.Add(new Section.PresetSet() { Line = Sections[channel].Presets.Count, Value = midiEvent.Value });
                        Sections[channel].LayoutPreset.Count++;

                        break;
                    case MPTKCommand.MetaEvent:
                        if (Sections[16] == null) Sections[16] = new Section(16);
                        //Sections[16].PresetCount++;
                        break;
                }
            }

            public void CalculateSizeAllSections(MPTKEvent last, float QuarterWidth, float CellHeight)
            {
                quarterWidth = QuarterWidth;
                cellHeight = CellHeight;
                MeasureWidth = quarterWidth * midiFileWriter.MPTK_NumberBeatsMeasure;

                // Calculate total width
                Section.FullWidthSections = 0f;
                if (last != null)
                {
                    // Calculate position + with of the rectClear note --> full width of the bigger channel
                    Section.FullWidthSections = ConvertTickToPosition(last.Tick) + ConvertTickToPosition(last.Length);
                    //Debug.Log($"BeginArea draw MIDI cellX:{cellX} cellW:{cellW} ChannelMidi.FullWidthChannelZone:{ChannelMidi.FullWidthChannelZone}");
                }

                float miniWidth = ConvertTickToPosition(midiFileWriter.MPTK_DeltaTicksPerQuarterNote * midiFileWriter.MPTK_NumberBeatsMeasure * 4);
                if (Section.FullWidthSections < miniWidth) Section.FullWidthSections = miniWidth;

                // Calculate total height
                float currentY = 0f;
                for (int channel = 0; channel < 16; channel++)
                {
                    Section section = Sections[channel];
                    if (section != null)
                    {
                        section.LayoutAll.BegY = currentY; // AllZone contains channel header + notes + preset
                        currentY += HEIGHT_CHANNEL_BANNER;

                        // Program Change row
                        section.LayoutPreset.BegY = currentY;// - 3f; //no, was not a good idea for  Better alignment for the start of the preset section
                        section.LayoutPreset.EndY = section.LayoutPreset.BegY + cellHeight * section.Presets.Count;
                        currentY = section.LayoutPreset.EndY;

                        // Notes rows
                        if (section.LayoutNote.HigherNote != 0)
                        {
                            section.LayoutNote.BegY = currentY;
                            section.LayoutNote.EndY = section.LayoutNote.BegY + (section.LayoutNote.HigherNote - section.LayoutNote.LowerNote + 1) * cellHeight;
                            currentY = section.LayoutNote.EndY;
                        }
                        section.LayoutAll.EndY = currentY;
                    }
                }
                Section.FullHeightSections = currentY;
            }

            public float ConvertTickToPosition(long tick)
            {
                return ((float)tick / (float)midiFileWriter.MPTK_DeltaTicksPerQuarterNote) * quarterWidth;
            }

            public long ConvertPositionToTick(float x)
            {
                return (long)((x * midiFileWriter.MPTK_DeltaTicksPerQuarterNote) / quarterWidth);
            }
        }
        class Section
        {
            /// <summary>
            /// One section for each channel used in the MIDI, -1 for other section like META
            /// </summary>
            public int Channel;

            // Count preset used for this channel
            public class PresetSet
            {
                public int Value;
                public int Line;
                public MPTKGui.PopupList PopupPreset;
            }
            public List<PresetSet> Presets;

            /// <summary>
            /// Search the line of the preset to display
            /// </summary>
            /// <param name="value"></param>
            /// <returns>-1 if not found, 0 for the first line</returns>
            public int GetPresetLine(int value)
            {
                foreach (PresetSet p in Presets)
                    if (p.Value == value)
                        return p.Line;
                // Not found
                return -1;
            }
            /// <summary>
            /// Contains channel info + notes zone + preset zone
            /// </summary>
            public Layout LayoutAll;
            public Layout LayoutNote;
            public Layout LayoutPreset;

            /// <summary>
            /// Width of the full are to display MIDI events (even if note hidden are not draw). Calculated at start of each OnGUI call.
            /// </summary>
            public static float FullWidthSections;

            /// <summary>
            /// Height of the full are to display MIDI events. Calculated at each OnGUI.
            /// </summary>
            public static float FullHeightSections;

            public Section(int channel)
            {
                Channel = channel;
                Presets = new List<PresetSet>();
                LayoutAll = new Layout();
                LayoutNote = new Layout();
                LayoutPreset = new Layout();
            }
            public override string ToString()
            {
                string detail = $"Channel:{Channel} NoteCount:{LayoutNote.Count} PresetCount:{LayoutPreset.Count} Drawn:{LayoutAll.Count}  FullWidthSections:{FullWidthSections} FullHeightSections:{FullHeightSections}";
                if (LayoutAll != null) detail += "\n\tAll layout=" + LayoutAll.ToString();
                if (LayoutPreset != null) detail += "\n\tPreset lay=" + LayoutPreset.ToString();
                if (LayoutNote != null) detail += "\n\tNote lay  =" + LayoutNote.ToString();
                return detail;
            }
        }


        class Layout
        {
            public int LowerNote;
            public int HigherNote;
            public int Count;
            //public float x; always start at 0
            /// Y start position of this zone from the beginarea
            public float BegY;
            public float EndY;
            // public float width; always the full width
            public float Height { get { return EndY - BegY; } }

            public Layout()
            {
                LowerNote = 9999;
                HigherNote = 0;
                Count = 0;
            }
            public void FindLowerHigherNotes(List<MPTKEvent> midiEvents, int channel, bool shrink = false)
            {
                //DisplayPerf(restart: true);
                //if (midiEvents.Count == 0)
                //{
                //    LowerNote = 60;
                //    HigherNote = 65;
                //}
                //else
                {
                    int l = 9999;
                    int h = 0;
                    //LowerNote = 9999;
                    //HigherNote = 0;
                    midiEvents.ForEach(noteon =>
                    {
                        if (noteon.Channel == channel && noteon.Command == MPTKCommand.NoteOn)
                        {
                            if (noteon.Value < l) l = noteon.Value;
                            if (noteon.Value > h) h = noteon.Value;
                        }
                    });

                    //if (l != 9999 && l != LowerNote)
                    //    if (l < LowerNote || shrink)
                    //        LowerNote = l;
                    //if (h != 0 && h != HigherNote)
                    //    if (h > HigherNote || shrink)
                    //        HigherNote = h;
                    if (l == 9999 && h == 0)
                    {
                        l = 60;
                        h = 65;
                    }
                    if (l < LowerNote) LowerNote = l;
                    if (h > HigherNote) HigherNote = h;


                    //if (l == 9999)
                    //    LowerNote = h - 5;
                    //else
                    //    LowerNote = l;

                    //if (h == 0)
                    //    HigherNote = l + 5;
                    //else
                    //    HigherNote = h;

                    //int ambitus = HigherNote - LowerNote;
                    //if (ambitus < 3)
                    //{
                    //    HigherNote += 1;
                    //    LowerNote -= 1;
                    //}

                    //Debug.Log($"FindLowerHigherNotes channel:{channel}     l:{l} h:{h} LowerNote:{LowerNote} HigherNote:{HigherNote}");
                }
                //DisplayPerf("FindLowerHigherNotes");
            }
            //int ambitus = HigherNote - LowerNote;
            //if (ambitus < 3)
            //{
            //    HigherNote += 1;
            //    LowerNote -= 1;
            //}


            public void SetLowerHigherNote(int value)
            {
                if (value < LowerNote) LowerNote = value;
                if (value > HigherNote) HigherNote = value;
            }

            public override string ToString()
            {
                return $"  BegY:{BegY:0000}  EndY:{EndY:0000}  Height:{Height:0000}  LowerNote:{LowerNote} HigherNote:{HigherNote}";
            }
        }
        #endregion
   }
}