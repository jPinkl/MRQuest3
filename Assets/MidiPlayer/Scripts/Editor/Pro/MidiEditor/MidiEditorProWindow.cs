//#define DEBUG_EDITOR
using System;
using System.Collections.Generic;
using System.IO;

namespace MidiPlayerTK
{
    using System.Xml.Serialization;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEngine;
    using static MidiPlayerTK.MidiFilePlayer;
    using Debug = UnityEngine.Debug;

    public partial class MidiEditorWindow : EditorWindow
    {

        private Rect mouseCursorRect;


        private void Awake()
        {
            //Debug.Log($"Awake");

            // Main
            //      Channels  --> Zone.Type.Channel == 0
            //      WhiteKeys --> Zone.Type.Channel == 1
            //          SubArea = list of white keys
            //      BlackKeys --> Zone.Type.Channel == 2
            //          SubArea = list of black keys
            //Debug.Log("Create Area");
            MainArea = new AreaUI()
            {
                Position = new Rect(),
                SubArea = new List<AreaUI>()
                {
                   new AreaUI()
                   {
                      areaType=AreaUI.AreaType.Channels,
                      Position = new Rect(),
                      SubArea = new List<AreaUI>(),
                   },
                   new AreaUI()
                   {
                      areaType=AreaUI.AreaType.WhiteKeys,
                      Position = new Rect(),
                      SubArea = new List<AreaUI>(),
                   },
                   new AreaUI()
                   {
                      areaType=AreaUI.AreaType.BlackKeys,
                      Position = new Rect(),
                      SubArea = new List<AreaUI>(),
                   },
                },
            };

            EditorDeserialize();
        }

        private void OnEnable()
        {
            //Debug.Log($"OnEnable");
            EditorApplication.playModeStateChanged += ChangePlayModeState;
            CompilationPipeline.compilationStarted += CompileStarted;
            InitPlayer();
            //InitGUI();

            //if (winSelectMidi != null)
            //{
            //    //Debug.Log("OnEnable winSelectMidi " + winSelectMidi.Title);
            //    winSelectMidi.SelectedIndexMidi = MidiIndex;
            //    winSelectMidi.Repaint();
            //    winSelectMidi.Focus();
            //}
        }
        private void OnDisable()
        {
            //Debug.Log($"OnDisable");
            EditorSerialize();
        }

        void OnDestroy()
        {
            //Debug.Log($"OnDestroy");
            try
            {
                if (SelectMidiWindow.winSelectMidi != null)
                {
                    //Debug.Log("OnDestroy winSelectMidi " + SelectMidiWindow.winSelectMidi.ToString());
                    SelectMidiWindow.winSelectMidi.Close();
                    SelectMidiWindow.winSelectMidi = null;
                }
            }
            catch (Exception)
            {
            }

            if (winPopupSynth != null)
            {
                winPopupSynth.Close();
                winPopupSynth = null;
            }

            EditorApplication.playModeStateChanged -= ChangePlayModeState;
            CompilationPipeline.compilationStarted -= CompileStarted;
            Player.OnEventStartPlayMidi.RemoveAllListeners();
            Player.OnEventNotesMidi.RemoveAllListeners();
            Player.OnEventEndPlayMidi.RemoveAllListeners();


            if (MidiPlayerSequencer != null) //strangely, this property can be null when window is close
                MidiPlayerSequencer.DestroyMidiObject();
            //else
            //    Debug.LogWarning("MidiPlayerEditor is null");
        }

        private void OnFocus()
        {
            // Load description of available soundfont
            try
            {
                //Debug.Log("OnFocus");
                MidiPlayerGlobal.InitPath();
                ToolsEditor.LoadMidiSet();
                ToolsEditor.CheckMidiSet();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        void OnGUI()
        {
            try
            {
                // In some case, Unity Editor lost skin, or texture, or style. Thats a random behavior but also
                // systematic at first load of MIDI Editor after launch of Unity Editor or a when scene change is done.
                // These "hack" seems correct the issue.
                if (MPTKGui.MaestroSkin == null || SepDragEventText == null || ChannelBannerStyle == null || ChannelBannerStyle.normal.background == null)
                {
                    //Debug.Log($" ********************* ReInit GUISkin **************************");
                    InitGUI();
                    mouseCursorRect.x = 0;
                    mouseCursorRect.y = 0;
                }

                // Skin must defined at each OnGUI cycle (certainly a global GUI variable)
                GUI.skin = MPTKGui.MaestroSkin;
                GUI.skin.settings.cursorColor = Color.white;
                GUI.skin.settings.cursorFlashSpeed = 0f;

                float startx = AREA_BORDER_X;
                float starty = AREA_BORDER_Y;
                float nextAreaY = starty;

                EventManagement();
                float width = window.position.width - 2 * AREA_BORDER_X;

                //Debug.Log($"{Screen.safeArea} {CurrentMouseCursor} position:{this.position}");
                //EditorGUIUtility.AddCursorRect(SceneView.lastActiveSceneView.position, CurrentMouseCursor);
                mouseCursorRect.width = this.position.width;
                mouseCursorRect.height = this.position.height;
                EditorGUIUtility.AddCursorRect(mouseCursorRect, CurrentMouseCursor);

                // Main menu at the top window 
                CmdNewLoadSaveTools(startx, starty, width, HEIGHT_HEADER);
                nextAreaY += HEIGHT_HEADER + 1;// + AREA_SPACE;

                if (MidiFileWriter != null)
                {

                    CmdSequencer(startx, nextAreaY, width, HEIGHT_PLAYER_CMD);
                    nextAreaY += HEIGHT_PLAYER_CMD - 1;

                    ShowSequencer(startx, nextAreaY, width, window.position.height - nextAreaY - 1);

                    if (DebugDisplayCell)
                        DebugAreaUI();
                }
            }
            //catch (ExitGUIException) { }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }
        }


        private void InitGUI()
        {
            //Debug.Log($"InitGUI");
            if (Context == null)
                LoadContext();

            MPTKGui.LoadSkinAndStyle(loadSkin: true);

            HeightScrollHori = MPTKGui.HorizontalThumb.fixedHeight;
            WidthScrollVert = MPTKGui.VerticalThumb.fixedWidth;
            //Debug.Log($"HeightScrollHori:{HeightScrollHori} WidthScrollVert:{WidthScrollVert}");

            SepBorder0 = new RectOffset(0, 0, 0, 0);
            SepBorder1 = new RectOffset(borderSize1, borderSize1, borderSize1, borderSize1);
            SepBorder2 = new RectOffset(borderSize2, borderSize2, borderSize2, borderSize2);
            SepBorder3 = new RectOffset(borderSize3, borderSize3, borderSize3, borderSize3);
            SepNoteTexture = MPTKGui.MakeTex(Color.blue, SepBorder1);
            SepPresetTexture = MPTKGui.MakeTex(Color.green, SepBorder2);
            SepBarText = MPTKGui.MakeTex(0.3f, SepBorder1);
            SepQuarterText = MPTKGui.MakeTex(0.6f, SepBorder1);
            SepDragEventText = MPTKGui.MakeTex(new Color(0.9f, 0.9f, 0f, 1), SepBorder1);
            SepDragMouseText = MPTKGui.MakeTex(new Color(0.9f, 0.9f, 0.7f, 1), SepBorder1);
            SepPositionTexture = MPTKGui.MakeTex(new Color(0.9f, 0.5f, 0.5f, 1), SepBorder1);
            SepLoopTexture = MPTKGui.MakeTex(new Color(0.5f, 0.9f, 0.5f, 1), SepBorder1);
            // Warning: GUI.skin.GetStyle("label") doesn't work with contentOffset!!!
            TimelineStyle = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.LabelListNormal, fontSize: 11, textAnchor: TextAnchor.MiddleCenter);
            //TimelineStyle = MPTKGui.BuildStyle(inheritedStyle: GUI.skin.GetStyle("label"), fontSize: 11, textAnchor: TextAnchor.MiddleCenter);
            //MPTKGui.ColorStyle(style: TimelineStyle,
            //    fontColor: Color.black,
            //    backColor: MPTKGui.MakeTex(10, 10, textureColor: new Color(0.8f, 0.9f, 0.8f, 1f), border: SepBorder1, bordercolor: Color.gray));
            //TimelineStyle.normal.background.
            //TestLenStyle();

            PresetButtonStyle = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.MaestroSkin.GetStyle("box"), fontSize: 10, textAnchor: TextAnchor.MiddleCenter);
            MPTKGui.ColorStyle(style: PresetButtonStyle,
                fontColor: Color.black,
                backColor: MPTKGui.MakeTex(10, 10, textureColor: new Color(0.7f, 0.9f, 0.7f, 1f), border: SepBorder1, bordercolor: Color.gray));

            MidiNoteTexture = MPTKGui.MakeTex(new Color(0.7f, 0.7f, 0.9f, 1f));
            MidiPresetTexture = MPTKGui.MakeTex(new Color(0.6f, 0.9f, 0.7f, 1f));
            MidiSelectedTexture = MPTKGui.MakeTex(new Color(0.8f, 0.4f, 0.4f, 1f));

            ChannelBannerStyle = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.MaestroSkin.GetStyle("box"), fontSize: 11, textAnchor: TextAnchor.MiddleLeft);
            MPTKGui.ColorStyle(style: ChannelBannerStyle,
                fontColor: Color.white,
                backColor: MPTKGui.MakeTex(10, 10, textureColor: new Color(0.4f, 0.2f, 0.2f, 1f), border: SepBorder0, bordercolor: new Color(0.5f, 0.5f, 0f, 1)));

            BackgroundMidiEvents = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.MaestroSkin.GetStyle("box"), fontSize: 11, textAnchor: TextAnchor.MiddleLeft);
            MPTKGui.ColorStyle(style: BackgroundMidiEvents,
                fontColor: Color.white,
                backColor: MPTKGui.MakeTex(10, 10, textureColor: new Color(0.6f, 0.6f, 0.8f, 1f), border: SepBorder1, bordercolor: new Color(0.1f, 0.1f, 0.1f, 1)));

            BackgroundMidiEvents1 = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.MaestroSkin.GetStyle("box"), fontSize: 11, textAnchor: TextAnchor.MiddleLeft);
            MPTKGui.ColorStyle(style: BackgroundMidiEvents1,
                fontColor: Color.white,
                backColor: MPTKGui.MakeTex(10, 10, textureColor: new Color(0.5f, 0.5f, 0.7f, 1f), border: SepBorder1, bordercolor: new Color(0.1f, 0.1f, 0.1f, 1)));

            WhiteKeyLabelStyle = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.Label, fontSize: 11, textAnchor: TextAnchor.MiddleRight);
            WhiteKeyLabelStyle.normal.textColor = Color.black;
            WhiteKeyLabelStyle.focused.textColor = Color.black;

            BlackKeyLabelStyle = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.Label, fontSize: 11, textAnchor: TextAnchor.MiddleRight);
            BlackKeyLabelStyle.normal.textColor = Color.white;
            BlackKeyLabelStyle.focused.textColor = Color.white;

            WhiteKeyDrawTexture = MPTKGui.MakeTex(new Color(0.9f, 0.9f, 0.9f, 1f));
            BlackKeyDrawTexture = MPTKGui.MakeTex(new Color(0.1f, 0.1f, 0.1f, 1f));

            if (PopupItemsPreset == null)
            {
                PopupItemsPreset = new List<MPTKGui.StyleItem>();
                MidiPlayerGlobal.MPTK_ListPreset.ForEach(preset =>
                    PopupItemsPreset.Add(new MPTKGui.StyleItem(preset.Label, value: preset.Index, visible: true, selected: false)));
                PopupItemsPreset[0].Selected = true;
                //Debug.Log($"InitListPreset MidiPlayerGlobal.MPTK_ListPreset count:{MidiPlayerGlobal.MPTK_ListPreset}");
            }

            if (PopupItemsMidiCommand == null)
            {
                PopupItemsMidiCommand = new List<MPTKGui.StyleItem>
                {
                    new MPTKGui.StyleItem("Note", true, true),
                    new MPTKGui.StyleItem("Preset", true,false),
                    new MPTKGui.StyleItem("Tempo", true,false),
                    new MPTKGui.StyleItem("Text", true, false)
                };
            }

            if (PopupItemsDisplayTime == null)
            {
                PopupItemsDisplayTime = new List<MPTKGui.StyleItem>
                {
                    // value = index of the item
                    new MPTKGui.StyleItem("Ticks", 0, true, false),
                    new MPTKGui.StyleItem("Seconds", 1, true, false),
                    new MPTKGui.StyleItem("hh:mm:ss:mmm", 2, true, false)
                };
            }

            if (PopupItemsQuantization == null)
            {
                PopupItemsQuantization = new List<MPTKGui.StyleItem>
                {
                    // value = index of the item
                    new MPTKGui.StyleItem("Off", 0, true, false),
                    new MPTKGui.StyleItem("Whole", 1, true, false), //  entire length of a measure 
                    new MPTKGui.StyleItem("Half", 2,true, false), //   1/2 of the duration of a whole note (2 quarter) 
                    new MPTKGui.StyleItem("Quarter",3, true, false), //  1/4 of the duration of a whole note.
                    new MPTKGui.StyleItem("1/8", 4,true, false), // Eighth duration of a whole note.
                    new MPTKGui.StyleItem("1/16", 5,true, false), // Sixteenth duration of a whole note.
                    new MPTKGui.StyleItem("1/32", 6,true, false), // Thirty-second of the duration of a whole note.
                    new MPTKGui.StyleItem("1/64", 7,true, false), // Sixty-fourth of the duration of a whole note.
                    new MPTKGui.StyleItem("1/128",8, true, false) // Hundred twenty-eighth of the duration of a whole note.
                    //new MPTKGui.StyleItem("Off", 0, true, false),
                    //new MPTKGui.StyleItem("Whole", 1, true, false), //  entire length of a measure 
                    //new MPTKGui.StyleItem("Half", 2,true, false), //   1/2 of the duration of a whole note (2 quarter) 
                    //new MPTKGui.StyleItem("Quarter",3, true, false), //  1/4 of the duration of a whole note.
                    //new MPTKGui.StyleItem("Eighth", 4,true, false), // 1/8 duration of a whole note.
                    //new MPTKGui.StyleItem("Sixteenth", 5,true, false), // 1/16 duration of a eighth note.
                    //new MPTKGui.StyleItem("Thirty-second", 6,true, false), // 1/32 of the duration of a whole note.
                    //new MPTKGui.StyleItem("Sixty-fourth", 7,true, false), // 1/64 of the duration of a whole note.
                    //new MPTKGui.StyleItem("Hundred twenty-eighth",8, true, false) // 1/128 of the duration of a whole note.
                };
            }

            if (PopupItemsMidiChannel == null)
            {
                PopupItemsMidiChannel = new List<MPTKGui.StyleItem>();
                for (int i = 0; i <= 15; i++)
                    PopupItemsMidiChannel.Add(new MPTKGui.StyleItem($"Channel {i}", true, i == 0 ? true : false));
            }

            if (PopupItemsLoadMenu == null)
            {
                PopupItemsLoadMenu = new List<MPTKGui.StyleItem>
                {
                    new MPTKGui.StyleItem("Load from Maestro MIDI Database", true, false,MPTKGui.Button),
                    new MPTKGui.StyleItem("Load from an external MIDI file", true, false,MPTKGui.Button),
                    new MPTKGui.StyleItem("Insert from Maestro MIDI Database", true, false,MPTKGui.Button),
                    new MPTKGui.StyleItem("Insert from an external MIDI file", true, false, MPTKGui.Button),
                    new MPTKGui.StyleItem("Open Temp Folder", true, false, MPTKGui.Button),
                };
            }

            if (PopupItemsSaveMenu == null)
            {
                PopupItemsSaveMenu = new List<MPTKGui.StyleItem>
                {
                    new MPTKGui.StyleItem("Save to Maestro MIDI Database", true, false,MPTKGui.Button),
                    new MPTKGui.StyleItem("Save to an external MIDI file", true, false,MPTKGui.Button),
                    new MPTKGui.StyleItem("Save As to an external MIDI file", true, false,MPTKGui.Button),
                };
            }

            SelectPopupItemFromContext();
        }
        private static void EditorSerialize()
        {
            //Debug.Log("EditorSerialize");

            if (MidiFileWriter != null)
            {
                string filename = Path.Combine(GetTempFolder(), "_temp_.mid");
                Debug.Log("Save temp MIDI file:" + filename);
                MidiFileWriter.MPTK_WriteToFile(filename);

                string path = Path.Combine(GetTempFolder(), "context.xml");
                var serializer = new XmlSerializer(typeof(ContextEditor));
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    serializer.Serialize(stream, Context);
                }

                //string json = JsonUtility.ToJson(MidiName);
            }
        }
        private void EditorDeserialize()
        {
            //Debug.Log("EditorDeserialize"); 
            try
            {
                string filename = Path.Combine(GetTempFolder(), "_temp_.mid");
                if (File.Exists(filename))
                {
                    Debug.Log("Load temp MIDI file:" + filename);
                    LoadExternalMidiFile(filename);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Can't load temp MIDI file {ex}");
            }

            LoadContext();
        }


        void LoadContext()
        {
            //Debug.Log($"LoadContext");
            try
            {
                string path = Path.Combine(GetTempFolder(), "context.xml");
                if (File.Exists(path))
                {
                    var serializer = new XmlSerializer(typeof(ContextEditor));
                    using (var stream = new FileStream(path, FileMode.Open))
                    {
                        Context = serializer.Deserialize(stream) as ContextEditor;
                        if (Context.QuarterWidth < 2f)
                            Context.QuarterWidth = 50f;
                    }
                }
                else
                {
                    Context = new ContextEditor();
                }
                SelectPopupItemFromContext();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Can't load MIDI context " + e.ToString());
                Context = new ContextEditor();
            }
        }
        private void ChangePlayModeState(PlayModeStateChange state)
        {
            //Debug.Log(">>> LogPlayModeState MidiSequencerWindow" + state);
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                Close(); // call OnDestroy
            }
            //Debug.Log("<<< LogPlayModeState MidiSequencerWindow" + state); 
        }
        private void CompileStarted(object obj)
        {
            //Debug.Log("Compilation, close editor");
            // Don't appreciate recompilation when window is open
            Close(); // call OnDestroy which call EditorSerialize

            MidiFileWriter = null;
            MidiEvents = null;
            sectionAll = null;
            MidiFileWriter = null;
            MainArea = null;
        }



        private void InitPlayer()
        {
            //Debug.Log("InitPlayer");
            MidiPlayerSequencer = new MidiEditorLib("MidiSequencer", _logSoundFontLoaded: true, _logDebug: true);
            Player = MidiPlayerSequencer.MidiPlayer;
            Player.VerboseSynth = false;
            Player.MPTK_PlayOnStart = false;
            Player.MPTK_StartPlayAtFirstNote = false; // was true
            Player.MPTK_EnableChangeTempo = true;
            Player.MPTK_ApplyRealTimeModulator = true;
            Player.MPTK_ApplyModLfo = true;
            Player.MPTK_ApplyVibLfo = true;
            Player.MPTK_ReleaseSameNote = true;
            Player.MPTK_KillByExclusiveClass = true;
            Player.MPTK_EnablePanChange = true;
            Player.MPTK_KeepPlayingNonLooped = true;
            Player.MPTK_KeepEndTrack = false; // was true
            Player.MPTK_LogEvents = false;
            Player.MPTK_KeepNoteOff = false; // was true
            Player.MPTK_Volume = 0.5f;
            Player.MPTK_Speed = 1f;
            Player.MPTK_InitSynth();

            // not yet available 
            //Player.OnEventStartPlayMidi.AddListener(StartPlay);
            //Player.OnEventNotesMidi.AddListener(MidiReadEvents);
        }

        private void CreateMidi()
        {
            if (CheckMidiSaved())
            {
                if (MidiPlayerSequencer != null && Player != null && Player.MPTK_IsPlaying)
                    Player.MPTK_Stop();
                MidiFileWriter = new MidiFileWriter2(deltaTicksPerQuarterNote: 500, bpm: 120);
                MidiEvents = MidiFileWriter.MPTK_MidiEvents;
                Context.MidiName = "no name";
                Context.PathOrigin = "";
                CalculRatioQuantization();
                FindLastMidiEvents();
                sectionAll = new SectionAll(MidiFileWriter);
                sectionAll.InitSections();
                CurrentDuration = MidiFileWriter.MPTK_DeltaTicksPerQuarterNote;
                InitPosition();
                Context.Modified = false;
                ResetCurrent();
            }
        }

        private void ResetCurrent()
        {
            //CurrentTrack = 0;
            CurrentChannel = 0;
            CurrentCommand = 0;
            CurrentTick = 0;
            CurrentNote = 60;
            CurrentDuration = MidiFileWriter != null ? MidiFileWriter.MPTK_DeltaTicksPerQuarterNote : 500;
            CurrentVelocity = 100;
            CurrentPreset = 0;
            CurrentBPM = 120;
            CurrentText = "";
            SelectedEvent = null;
            if (PopupSelectMidiChannel != null)
                PopupSelectMidiChannel.SelectedIndex = CurrentChannel;
            if (PopupSelectMidiCommand != null)
                PopupSelectMidiCommand.SelectedIndex = CurrentCommand;
        }



        private void LoadMidiFromDB(object tag, int midiindex)
        {
            if (CheckMidiSaved())
            {
                MidiLoad midiLoader = new MidiLoad();
                midiLoader.MPTK_EnableChangeTempo = true;
                midiLoader.MPTK_KeepEndTrack = false;
                midiLoader.MPTK_KeepNoteOff = false;
                midiLoader.MPTK_Load(midiindex);
                Context.MidiIndex = midiindex;
                //Player.MPTK_MidiIndex = MidiIndex;
                //Player.MPTK_PreLoad();
                Context.MidiName = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[Context.MidiIndex];
                Context.PathOrigin = "";
                Context.Modified = false;

                ImportToMidiFileWriter(midiLoader);
                ResetCurrent();
                window.Repaint();

            }
        }
        static string GetTempFolder()
        {
            string folder = Path.Combine(Application.persistentDataPath, "MaestroMidiEditorTemp");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }

        private void LoadExternalMidiFile(string midifile)
        {
            // check with CheckMidiSaved already done
            MidiLoad midiLoader = new MidiLoad();
            try
            {
                midiLoader.MPTK_EnableChangeTempo = true;
                midiLoader.MPTK_KeepEndTrack = false;
                midiLoader.MPTK_KeepNoteOff = false;
                midiLoader.MPTK_LoadFile(midifile);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error when loading MIDI file {ex}");
            }
            if (midiLoader != null)
            {
                // Context can be null if loaded from awake
                if (Context == null)
                    Context = new ContextEditor();
                Context.MidiName = Path.GetFileNameWithoutExtension(midifile);
                Context.Modified = false;

                ImportToMidiFileWriter(midiLoader);
                ResetCurrent();
            }
            try
            {
                if (window != null)
                    window.Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error when Repaint {ex}");
            }
        }

        private void ImportToMidiFileWriter(MidiLoad midiLoader)
        {
            //Debug.Log($"ImportToMidiFileWriter midiLoader count:{midiLoader.MPTK_MidiEvents.Count} windowID:{window?.GetInstanceID()} or null");
            try
            {
                MidiFileWriter = new MidiFileWriter2();
                MidiFileWriter.MPTK_ImportFromEventsList(midiLoader.MPTK_MidiEvents, midiLoader.MPTK_DeltaTicksPerQuarterNote,
                    name: Context.MidiName, position: 0, logDebug: false);
                MidiFileWriter.MPTK_CreateTracksStat();
                MidiFileWriter.MPTK_CreateTempoMap();
                MidiEvents = MidiFileWriter.MPTK_MidiEvents;
                SelectedEvent = null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error when importing MIDI file {ex}");
            }
            //Debug.Log($"ImportToMidiFileWriter {Context.MidiName} {MidiEvents.Count}");

            if (MidiEvents != null)
            {
                CalculRatioQuantization();
                FindLastMidiEvents();
                sectionAll = new SectionAll(MidiFileWriter);
                sectionAll.InitSections();
                CurrentDuration = MidiFileWriter.MPTK_DeltaTicksPerQuarterNote;
                InitPosition();
                if (MidiPlayerSequencer != null && Player != null && Player.MPTK_IsPlaying)
                    PlayMidiFileSelected();
                //if (MidiEvents != null && MidiEvents.Count > 0)
                //Player.MPTK_Play(mfw2: MidiFileWriter, fromTick: Context.LoopStart, toTick: Context.LoopEnd, timePosition: false);
            }
        }


        private void PlayMidiFileSelected()
        {
            if (MidiPlayerGlobal.MPTK_SoundFontIsReady && MidiEvents != null && MidiEvents.Count > 0)
            {
                try
                {
                    MidiPlayerSequencer.PlayAudioSource();
                    InitPosition();
                    Player.OnEventStartPlayMidi.RemoveAllListeners();
                    Player.OnEventNotesMidi.RemoveAllListeners();
                    Player.OnEventEndPlayMidi.RemoveAllListeners();

                    Player.OnEventStartPlayMidi.AddListener(StartPlay);
                    Player.OnEventNotesMidi.AddListener(MidiReadEvents);
                    Player.OnEventEndPlayMidi.AddListener(EndPlay);

                    Player.MPTK_Stop();
                    Player.MPTK_Loop = Context.LoopPlay;
                    Player.MPTK_ModeStopVoice = Context.ModeLoop;
                    
                    if (MidiEvents != null && MidiEvents.Count > 0)
                        Player.MPTK_Play(mfw2: MidiFileWriter, fromTick: Context.LoopStart, toTick: Context.LoopEnd, timePosition: false);
                    else
                        Debug.Log("Nothing to play ...");
                }
                catch (Exception ex)
                {
                    throw new MaestroException($"PlayMidiFileSelected error.{ex.Message}");
                }
            }
        }
        private void PlayStop()
        {
            if (MidiPlayerGlobal.MPTK_SoundFontIsReady && MidiEvents != null && MidiEvents.Count > 0)
            {
                InitPosition(keepSequencerPosition: true);
                Player.OnEventStartPlayMidi.RemoveAllListeners();
                Player.OnEventNotesMidi.RemoveAllListeners();
                Player.OnEventEndPlayMidi.RemoveAllListeners();
                Player.MPTK_Stop();
            }
        }
        static public void StartPlay(string midiname)
        {
            //Debug.Log($"StartPlay {midiname}  {MidiPlayerSequencer.MidiPlayer.MPTK_TickCurrent}");
        }

        static public void EndPlay(string midiname, EventEndMidiEnum reason)
        {
            //Debug.Log($"EndPlay {midiname} {reason} {MidiPlayerSequencer.MidiPlayer.MPTK_TickCurrent}");
        }


        void InitPosition(bool keepSequencerPosition = false)
        {
            lastTickForUpdate = -1;
            lastTimeForUpdate = DateTime.Now;
            if (!keepSequencerPosition)
            {
                CurrentTickPosition = 0;
                PositionSequencerPix = 0;
            }
            LastEventPlayed = null;
            ScrollerMidiEvents = Vector2.zero;
        }

        private void FindLastMidiEvents()
        {
            LastMidiEvent = null;
            LastNoteOnEvent = null;
            if (MidiEvents != null && MidiEvents.Count > 0)
            {
                LastMidiEvent = MidiEvents[MidiEvents.Count - 1];
                if (LastMidiEvent.Command == MPTKCommand.NoteOn)
                    LastNoteOnEvent = LastMidiEvent;
                else
                {
                    for (int i = MidiEvents.Count - 1; i >= 0; i--)
                        if (MidiEvents[i].Command == MPTKCommand.NoteOn)
                        {
                            LastNoteOnEvent = MidiEvents[i];
                            break;
                        }
                }
            }
        }

        /// <summary>@brief
        /// Event fired by MidiFilePlayer when midi notes are available. 
        /// Set by Unity Editor in MidiFilePlayer Inspector or by script with OnEventNotesMidi.
        /// </summary>
        public void MidiReadEvents(List<MPTKEvent> midiEvents)
        {
            try
            {
                if (Context.LogEvents)
                    midiEvents.ForEach(midiEvent => Debug.Log(midiEvent.ToString()));

                LastEventPlayed = midiEvents[midiEvents.Count - 1];
                //midiEvents.ForEach(midiEvent =>
                //{ 
                //    if (midiEvent.Command== MPTKCommand.MetaEvent && midiEvent.Meta == MPTKMeta.SetTempo) 
                //        Player.SetTimeMidiFromStartPlay = midiEvent.RealTime; 
                //});


                //if (FollowEvent)
                {

                    //Debug.Log($"{scrollerMidiPlayer}");
                    window.Repaint();
                }
            }
            catch (Exception ex)
            {
                throw new MaestroException($"MidiReadEvents.{ex.Message}");
            }
        }


        private void SelectPopupItemFromContext()
        {
            if (PopupItemsDisplayTime != null)
                PopupItemsDisplayTime.ForEach(item => { item.Selected = item.Value == Context.DisplayTime; });
            if (PopupSelectDisplayTime != null)
                PopupSelectDisplayTime.SelectedIndex = Context.DisplayTime;

            //Debug.Log($"SelectPopupItemFromContext IndexQuantization:{Context.IndexQuantization}");
            if (PopupItemsQuantization != null)
                PopupItemsQuantization.ForEach(item => { item.Selected = item.Value == Context.IndexQuantization; });
            if (PopupSelectQuantization != null)
                PopupSelectQuantization.SelectedIndex = Context.IndexQuantization;

            CalculRatioQuantization();
        }
        void Update()
        {
            if (Player.MPTK_IsPlaying && !Player.MPTK_IsPaused)
            {
                //long tick = Player.MPTK_TickCurrent;
                long tick = Player.midiLoaded.MPTK_TickPlayer;
                if (lastTickForUpdate == tick)
                {
                    // No new MIDI event since the rectClear update, extrapolate the current tick from the ellapse time
                    tick = lastTickForUpdate + Convert.ToInt64((DateTime.Now - lastTimeForUpdate).TotalMilliseconds / Player.MPTK_PulseLenght);
                    //Debug.Log($"extrapolate Time.deltaTime:{Time.deltaTime} MPTK_TickPlayer:{Player.midiLoaded.MPTK_TickPlayer} MPTK_PulseLenght:{Player.MPTK_PulseLenght:F2} {(Time.deltaTime * 1000d) / Player.MPTK_PulseLenght:F2} lastTick:{lastTickForUpdate} tick:{tick}");
                }
                else
                {
                    lastTimeForUpdate = DateTime.Now;
                    lastTickForUpdate = tick;
                    //Debug.Log($"real tick Time.deltaTime:{Time.deltaTime} MPTK_TickPlayer:{Player.midiLoaded.MPTK_TickPlayer} MPTK_PulseLenght:{Player.MPTK_PulseLenght:F2} lastTick:{lastTickForUpdate} tick:{tick}");
                }

                // Prefer MPTK_PlayTimeTick rather MPTK_TickCurrent to have a smooth display
                //float position = ((float)tick / (float)Player.MPTK_DeltaTicksPerQuarterNote) * Context.QuarterWidth;
                float position = sectionAll.ConvertTickToPosition(tick);

                // Avoid repaint for value bellow 1 pixel
                if ((int)position != PositionSequencerPix)
                {
                    PositionSequencerPix = (int)position;
                    //Debug.Log($"Time.deltaTime:{Time.deltaTime} MPTK_TickCurrent:{Player.MPTK_TickCurrent} MPTK_PulseLenght:{Player.MPTK_PulseLenght} position:{position}");
                    Repaint();
                }
            }
        }


        private void CmdNewLoadSaveTools(float startX, float starty, float width, float height)
        {
            try
            {
                // Begin area header
                // --------------------------
                GUILayout.BeginArea(new Rect(startX, starty, width, height), MPTKGui.stylePanelGrayLight);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent() { text = "New", tooltip = "Create a new MIDI" }, MPTKGui.Button, GUILayout.Width(60)))
                {
                    CreateMidi();
                }

                // Midi Load
                // --------- 
                MPTKGui.ComboBox(ref PopupSelectLoadMenu, "Load", PopupItemsLoadMenu, false,
                        delegate (int index)
                        {
                            if (index == 0)
                            {
                                SelectMidiWindow.winSelectMidi = EditorWindow.GetWindow<SelectMidiWindow>(true, "Select a MIDI File to load in the Editor");
                                SelectMidiWindow.winSelectMidi.OnSelect = LoadMidiFromDB;
                                SelectMidiWindow.winSelectMidi.SelectedIndexMidi = Context.MidiIndex;
                            }
                            else if (index == 1)
                            {
                                if (CheckMidiSaved())
                                {

                                    string selectedFile = EditorUtility.OpenFilePanelWithFilters("Open and import MIDI file", ToolsEditor.lastDirectoryMidi,
                                       new string[] { "MIDI files", "mid,midi", "Karoke files", "kar", "All", "*" });
                                    if (!string.IsNullOrEmpty(selectedFile))
                                    {
                                        // selectedFile contins also the folder 
                                        ToolsEditor.lastDirectoryMidi = Path.GetDirectoryName(selectedFile);
                                        LoadExternalMidiFile(selectedFile);
                                        Context.PathOrigin = ToolsEditor.lastDirectoryMidi;
                                        Context.Modified = false;
                                    }
                                }
                            }
                            else if (index == 4)
                            {
                                Application.OpenURL("file://" + GetTempFolder());
                            }
                            else
                                EditorUtility.DisplayDialog("Loading option", "Not yet implemented", "OK");

                        }, MPTKGui.Button, widthPopup: 300, GUILayout.Width(60));


                // Midi Save
                // ---------

                MPTKGui.ComboBox(ref PopupSelectSaveMenu, "Save " + (Context.Modified ? " *" : ""), PopupItemsSaveMenu, false,
                        delegate (int index)
                        {
                            if (index == 0)
                            {
                                if (!string.IsNullOrWhiteSpace(Context.MidiName))
                                {
                                    MidiFileWriter.MPTK_WriteToMidiDB(Context.MidiName);
                                    AssetDatabase.Refresh();
                                    Context.Modified = false;
                                }
                                else
                                    EditorUtility.DisplayDialog("Save MIDI to MIDI DB", "Enter a filename", "Ok", "Cancel");
                            }
                            else if (index == 1)
                            {
                                if (!string.IsNullOrWhiteSpace(Context.MidiName))
                                {
                                    if (string.IsNullOrEmpty(Context.PathOrigin))
                                    {
                                        Context.PathOrigin = EditorUtility.OpenFolderPanel("Select a folder to save your MIDI file", ToolsEditor.lastDirectoryMidi, "");
                                    }
                                    if (!string.IsNullOrEmpty(Context.PathOrigin))
                                    {
                                        string filename = Path.Combine(Context.PathOrigin, Context.MidiName + ".mid");
                                        Debug.Log("Write MIDI file:" + filename);
                                        MidiFileWriter.MPTK_WriteToFile(filename);
                                        Context.Modified = false;
                                    }
                                }
                                else
                                    EditorUtility.DisplayDialog("Save MIDI to MIDI DB", "Enter a filename", "Ok", "Cancel");
                            }
                            else if (index == 2)
                            {
                                string path = EditorUtility.SaveFilePanel("Save As your MIDI file", ToolsEditor.lastDirectoryMidi, Context.MidiName + ".mid", "mid"); ;

                                if (path.Length != 0)
                                {
                                    Context.PathOrigin = Path.GetFullPath(path);
                                    Context.MidiName = Path.GetFileNameWithoutExtension(path);
                                    Debug.Log("Write MIDI file:" + path);
                                    MidiFileWriter.MPTK_WriteToFile(path);
                                    Context.Modified = false;
                                }
                            }
                            else
                                EditorUtility.DisplayDialog("Loading option", "Not yet implemented", "OK");

                        }, MPTKGui.Button, widthPopup: 300, GUILayout.Width(60));


                // MIDI Name
                // ---------
                //GUILayout.Label("Name:", MPTKGui.LabelLeft, GUILayout.Height(22), GUILayout.ExpandWidth(false));
                //MPTKGui.MaestroSkin.settings.cursorColor = Color.cyan;
                string newName = GUILayout.TextField(Context.MidiName, MPTKGui.TextField, GUILayout.Width(250));
                if (newName != Context.MidiName)
                {
                    for (int i = 0; i < InvalidFileChars.Length; i++)
                        newName = newName.Replace(InvalidFileChars[i], '_');
                    Context.MidiName = newName;
                }

                // MIDI Information 
                // ----------------
                if (MidiFileWriter != null)
                {
                    string label = $"DTPQN:{MidiFileWriter.MPTK_DeltaTicksPerQuarterNote}   Events:{MidiFileWriter.MPTK_MidiEvents.Count}   ";
                    if (Player.MPTK_IsPlaying)
                    {
                        label += $"BPM:{Player.MPTK_Tempo:F1}   ";
                        label += $"{Player.MPTK_PulseLenght * Player.MPTK_DeltaTicksPerQuarterNote / 1000f:F2} sec/quarter";
                    }
                    else
                    {
                        label += $"BPM:{MidiFileWriter.MPTK_Tempo:F1}   ";
                        label += $"{MidiFileWriter.MPTK_PulseLenght * MidiFileWriter.MPTK_DeltaTicksPerQuarterNote / 1000f:F2} sec/quarter";
                    }

                    GUILayout.Label(label, MPTKGui.LabelGray, GUILayout.Height(22)/*, GUILayout.ExpandWidth(false)*/);
                }
                else
                    GUILayout.Label($" no midi file writer", MPTKGui.LabelLeft, GUILayout.Height(22)/*, GUILayout.ExpandWidth(false)*/);

                GUILayout.FlexibleSpace();

                // Midi Tools
                // ----------

                if (GUILayout.Button("Synth", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                {
                    if (winPopupSynth != null)
                    {
                        winPopupSynth.Close();
                        winPopupSynth = null;
                    }
                    else
                    {
                        winPopupSynth = EditorWindow.GetWindow<PopupInfoSynth>(false, "Maestro MIDI Synth");
                        winPopupSynth.minSize = new Vector2(655, 73);
                        winPopupSynth.maxSize = new Vector2(655, 73);
                        winPopupSynth.MidiSynth = Player;
                        winPopupSynth.ShowUtility();
                    }
                }

                if (GUILayout.Button("Event", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                    MidiFileWriter.MPTK_Debug();

                if (GUILayout.Button(new GUIContent(MPTKGui.IconRefresh, "Restaure default value"), MPTKGui.Button, GUILayout.Width(30), GUILayout.Height(22)))
                {
                    //Debug.Log($"ReInitGUI  '{MPTKGui.MaestroSkin.name}' '{GUI.skin.name}' {MPTKGui.HorizontalThumb.fixedHeight} {MPTKGui.HorizontalThumb.fixedWidth} - {MPTKGui.VerticalThumb.fixedHeight} {MPTKGui.VerticalThumb.fixedWidth}");
                    InitGUI();
                    Context.SetDefaultSize();
                    Repaint();
                }

                if (GUILayout.Button(new GUIContent(MPTKGui.IconHelp, "Get some help on MPTK web site"), MPTKGui.Button, GUILayout.Width(30), GUILayout.Height(22)))
                    Application.OpenURL("https://paxstellar.fr/maestro-midi-editor/");
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { GUILayout.EndHorizontal(); GUILayout.EndArea(); }
        }

        private bool CheckMidiSaved()
        {
            if (!Context.Modified)
                return true;
            else
            {
                if (EditorUtility.DisplayDialogComplex("MIDI not saved", "This MIDI sequence has not beed saved, if you continue change will be lost", "Close without saving", "Cancel", "") == 0)
                    return true;
            }
            return false;
        }

        private void CmdSequencer(float startx, float starty, float width, float height)
        {
            Event currentEvent = Event.current;

            CmdPadChannel(startx, starty, height);
            CmdPadMidiEvent(startx, starty, height);

            try // Begin area MIDI player commands
            {
                GUILayout.BeginArea(new Rect(
                    startx + WIDTH_PAD_MIDI_EVENT + 1 + WIDTH_PAD_CHANNEL + 1, starty,
                    width - WIDTH_PAD_MIDI_EVENT - 1 - WIDTH_PAD_CHANNEL - 1, height),
                    MPTKGui.stylePanelGrayMiddle);

                try // Player command  --- line 1 ---
                {
                    GUILayout.BeginHorizontal();
                    CmdMidiPlayAndLoop();

                    GUILayout.FlexibleSpace();

                    // Select display time format
                    MPTKGui.ComboBox(ref PopupSelectDisplayTime, "Display Time: {Label}", PopupItemsDisplayTime, false,
                           delegate (int index) { Context.DisplayTime = index; }, null, widthPopup: 200, GUILayout.Width(200));
                    Context.LogEvents = GUILayout.Toggle(Context.LogEvents, "Log Events ", MPTKGui.styleToggle, GUILayout.Width(80));
                }
                catch (Exception ex) { Debug.LogException(ex); throw; }
                finally { GUILayout.EndHorizontal(); }

                try // Player command  --- line 2 ---
                {
                    GUILayout.BeginHorizontal();

                    Context.FollowEvent = GUILayout.Toggle(Context.FollowEvent, new GUIContent("Follow", "When enabled, horizontal scrollbar is disabled"), MPTKGui.styleToggle, GUILayout.Width(60));

                    float volume = Player.MPTK_Volume;
                    GUILayout.Label("Volume:" + volume.ToString("F2"), MPTKGui.LabelLeft, GUILayout.Width(80));
                    Player.MPTK_Volume = GUILayout.HorizontalSlider(volume, 0.0f, 1f, MPTKGui.HorizontalSlider, MPTKGui.HorizontalThumb, GUILayout.MinWidth(100));

                    float speed = Player.MPTK_Speed;
                    // Button to restore speed to 1 with label style
                    if (GUILayout.Button("   Speed: " + speed.ToString("F2"), MPTKGui.LabelRight, GUILayout.ExpandWidth(false))) speed = 1f;
                    Player.MPTK_Speed = GUILayout.HorizontalSlider(speed, 0.01f, 10f, MPTKGui.HorizontalSlider, MPTKGui.HorizontalThumb, GUILayout.MinWidth(100));
                }
                catch (Exception ex) { Debug.LogException(ex); throw; }
                finally { GUILayout.EndHorizontal(); }


                try // Player command  --- line 3 ---
                {
                    GUILayout.BeginHorizontal();

                    CmdTimeSlider(currentEvent);
                }
                catch (Exception ex) { Debug.LogException(ex); throw; }
                finally { GUILayout.EndHorizontal(); }
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { GUILayout.EndArea(); }

        }

        private void CmdMidiPlayAndLoop()
        {
            try // Player command  --- line 1 ---
            {
                if (GUILayout.Button(Player.MPTK_IsPlaying ? MPTKGui.IconPlaying : MPTKGui.IconPlay, MPTKGui.Button, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                    PlayMidiFileSelected();

                if (GUILayout.Button(Player.MPTK_IsPaused ? MPTKGui.IconPauseActivated : MPTKGui.IconPause, MPTKGui.Button, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                    if (Player.MPTK_IsPaused)
                        Player.MPTK_UnPause();
                    else
                        Player.MPTK_Pause();

                if (GUILayout.Button(MPTKGui.IconStop, MPTKGui.Button, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                    PlayStop();

                GUILayout.Space(15);

                // Set looping
                // -----------
                if (GUILayout.Button(Context.LoopPlay ? LabelLooping : LabelLoop, MPTKGui.Button, GUILayout.Width(36), GUILayout.Height(heightFirstRowCmd)))
                {
                    Context.LoopPlay = !Context.LoopPlay;
                    Player.MPTK_Loop = Context.LoopPlay;
                }

                // Set looping start
                // -----------------
                if (Context.LoopStart == 0)
                {
                    LabelSetLoopStart.image = MPTKGui.IconLoopStart;
                    LabelSetLoopStart.tooltip = "Set loop start from the value of the selected event or from the 'Tick' value";
                }
                else
                {
                    LabelSetLoopStart.image = MPTKGui.IconLoopStartSet;
                    LabelSetLoopStart.tooltip = $"Loop from tick:{Context.LoopStart}\nSet loop start from the value of the selected event or from the 'Tick' value";
                }

                if (GUILayout.Button(LabelSetLoopStart, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                {
                    Context.LoopStart = CurrentTick;
                    if (Context.LoopStart > Context.LoopEnd)
                        Context.LoopEnd = Context.LoopStart;
                    if (Player.midiLoaded != null)
                        Player.midiLoaded.MPTK_TickStart = Context.LoopStart;
                    Debug.Log($"Start playing at tick {Context.LoopStart}");
                }

                // Set looping end
                // ---------------
                if (Context.LoopEnd == 0)
                {
                    LabelSetLoopStop.image = MPTKGui.IconLoopStop;
                    LabelSetLoopStop.tooltip = "Set loop end from the value of the selected event + duration or from the 'Tick' value";
                }
                else
                {
                    LabelSetLoopStop.image = MPTKGui.IconLoopStopSet;
                    LabelSetLoopStop.tooltip = $"Loop until tick:{Context.LoopEnd}\nSet loop end from the value of the selected event + duration or from the 'Tick' value";
                }
                if (GUILayout.Button(LabelSetLoopStop, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                {
                    Context.LoopEnd = CurrentTick + (SelectedEvent != null ? CurrentDuration : 0);
                    if (Context.LoopEnd < Context.LoopStart)
                        Context.LoopEnd = Context.LoopStart;
                    if (Player.midiLoaded != null)
                        Player.midiLoaded.MPTK_TickEnd = Context.LoopEnd;
                    Debug.Log($"Stop playing at tick {Context.LoopEnd} " + Player.MPTK_ModeStopVoice);
                }
                LabelResetLoop.tooltip = $"Loop from tick {Context.LoopStart} to {Context.LoopEnd}\nClear current value. ";
                if (GUILayout.Button(LabelResetLoop, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                {
                    Context.LoopStart = Context.LoopEnd = 0;
                    Debug.Log("Reset loop");
                }

#if TO_BE_MOVED_TO_SETUP // example of popup with a label, issue with the height of tha label wich can't be defined
                GUIContent LabelModeLoop = new GUIContent(Player.ModeStopPlayLabel[(int)Player.MPTK_ModeStopVoice], MPTKGui.IconComboBox);
                GUILayout.Label(LabelModeLoop, MPTKGui.ButtonCombo);
                if (Event.current.type == EventType.MouseDown)
                {
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    if (lastRect.Contains(Event.current.mousePosition))
                    {
                        var dropDownMenu = new GenericMenu();
                        foreach (ModeStopPlay mode in Enum.GetValues(typeof(ModeStopPlay)))
                            dropDownMenu.AddItem(
                                new GUIContent(Player.ModeStopPlayLabel[(int)mode], ""),
                                Player.MPTK_ModeStopVoice == mode, () => { Player.MPTK_ModeStopVoice = mode; });
                        dropDownMenu.ShowAsContext();
                    }
                } 
#endif
                if (GUILayout.Button(LabelModeLoop, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                {
                    var dropDownMenu = new GenericMenu();
                    foreach (ModeStopPlay mode in Enum.GetValues(typeof(ModeStopPlay)))
                        dropDownMenu.AddItem(
                            new GUIContent(Player.ModeStopPlayLabel[(int)mode], ""),
                            Context.ModeLoop == mode, () => { Player.MPTK_ModeStopVoice = mode;Context.ModeLoop = mode; });
                    dropDownMenu.ShowAsContext();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
            finally { }
        }

        private void CmdTimeSlider(Event currentEvent)
        {
            long lastTick = LastMidiEvent != null ? LastMidiEvent.Tick : 0;
            //long currentTick = Player.MPTK_IsPlaying ? Player.midiLoaded.MPTK_TickCurrent : CurrentTickPosition;
            long currentTick = Player.MPTK_IsPlaying ? Player.midiLoaded.MPTK_TickPlayer : CurrentTickPosition;
            if (Context.DisplayTime == 0)
            {
                //GUILayout.Label($"{currentTick:000000} / {lastTick:000000}", MPTKGui.LabelRight, GUILayout.Width(120)); 
                if (GUILayout.Button($"{currentTick:000000}", MPTKGui.LabelRight, GUILayout.Width(50)))
                    CurrentTickPosition = currentTick;
                GUILayout.Label($"/", MPTKGui.LabelRight, GUILayout.Width(6));
                if (GUILayout.Button($"{lastTick:000000}", MPTKGui.LabelRight, GUILayout.Width(50)))
                    CurrentTickPosition = lastTick;
                long tick = (long)GUILayout.HorizontalSlider((float)currentTick, 0f, lastTick, MPTKGui.HorizontalSlider, MPTKGui.HorizontalThumb);
                if (tick != currentTick)
                {
                    CurrentTickPosition = tick;
                    if (Player.MPTK_IsPlaying)
                    {
                        Player./*MPTK_PlayTimeTick*/MPTK_TickCurrent = CurrentTickPosition;
                        //Debug.Log($"MPTK_TickCurrent:{Player.MPTK_TickCurrent} MPTK_PulseLenght:{Player.MPTK_PulseLenght}");
                    }
                    float position = sectionAll.ConvertTickToPosition(CurrentTickPosition);

                    // Avoid repaint for value bellow 1 pixel
                    if ((int)position != PositionSequencerPix)
                    {
                        SetScrollXPosition(widthVisibleEventsList);
                        PositionSequencerPix = (int)position;
                        Repaint();
                    }
                }
            }
            else
            {
                // Horible approximation which didn't take into account tempo change
                double lastPosition = LastMidiEvent != null ? LastMidiEvent.Tick * MidiFileWriter.MPTK_PulseLenght / 1000d : 0;
                double currentPosition = Math.Round(currentTick * MidiFileWriter.MPTK_PulseLenght / 1000d, 2);

                if (Context.DisplayTime == 1)
                {
                    GUILayout.Label($"{currentPosition:F2} / {lastPosition:F2}", MPTKGui.LabelLeft, GUILayout.Width(100));
                }
                else if (Context.DisplayTime == 2)
                {
                    TimeSpan timePos = TimeSpan.FromSeconds(currentPosition);
                    string playTime = string.Format("{0:00}:{1:00}:{2:00}:{3:000}", timePos.Hours, timePos.Minutes, timePos.Seconds, timePos.Milliseconds);
                    TimeSpan lastPos = TimeSpan.FromSeconds(lastPosition);
                    string lastTime = string.Format("{0:00}:{1:00}:{2:00}:{3:000}", lastPos.Hours, lastPos.Minutes, lastPos.Seconds, lastPos.Milliseconds);
                    GUILayout.Label($"{playTime} / {lastTime}", MPTKGui.LabelLeft, GUILayout.Width(165));
                }

                // slider
                double newPosition = Math.Round(GUILayout.HorizontalSlider((float)currentPosition, 0f, (float)lastPosition, MPTKGui.HorizontalSlider, MPTKGui.HorizontalThumb), 2);

                if (newPosition != currentPosition)
                {
                    // Horible approximation which didn't take into account tempo change
                    CurrentTickPosition = Convert.ToInt64((newPosition / lastPosition) * lastTick);
                    if (Player.MPTK_IsPlaying)
                    {
                        Player./*MPTK_PlayTimeTick*/MPTK_TickCurrent = CurrentTickPosition;
                        Debug.Log($"MPTK_TickCurrent:{Player.MPTK_TickCurrent} MPTK_PulseLenght:{Player.MPTK_PulseLenght}");
                    }
                    float position = sectionAll.ConvertTickToPosition(CurrentTickPosition);

                    // Avoid repaint for value bellow 1 pixel
                    if ((int)position != PositionSequencerPix)
                    {
                        SetScrollXPosition(widthVisibleEventsList);
                        PositionSequencerPix = (int)position;
                        Repaint();
                    }
                }
            }
        }
        private void CmdPadChannel(float startx, float starty, float height)
        {
            try
            {
                GUILayout.BeginArea(new Rect(startx, starty, WIDTH_PAD_CHANNEL, height), MPTKGui.stylePanelGrayMiddle);

                // Select Channel
                MPTKGui.ComboBox(ref PopupSelectMidiChannel, "{Label}", PopupItemsMidiChannel, false,
                    delegate (int index) { CurrentChannel = index; }, null, widthPopup: 80, GUILayout.Width(92), GUILayout.Height(heightFirstRowCmd));

                GUILayout.BeginHorizontal();
                GUILayout.Label(" "); // let an empty line
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                {
                    // Add channel
                    AddChannel(CurrentChannel);
                }

                if (GUILayout.Button("Del", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                {
                    // remove channel 
                    if (EditorUtility.DisplayDialog($"Delete Channel {CurrentChannel}", $"All MIDI events on the channel {CurrentChannel} will be deleted.", $"Delete Channel {CurrentChannel}", "Cancel"))
                        DeleteChannel(CurrentChannel);
                }
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally
            {
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }
        private void CmdPadMidiEvent(float startx, float starty, float height)
        {
            try
            {
                GUILayout.BeginArea(new Rect(startx + WIDTH_PAD_CHANNEL + 1, starty, WIDTH_PAD_MIDI_EVENT, height), MPTKGui.stylePanelGrayMiddle);

                // no track in this version
                // CurrentTrack = MPTKGui.IntField("Track:", CurrentTrack, min: 0, max: 999, maxLength: 3, width: 40);

                // ------ line 1 ----------
                GUILayout.BeginHorizontal();

                // Select MIDI command
                MPTKGui.ComboBox(ref PopupSelectMidiCommand, "{Label}", PopupItemsMidiCommand, false,
                    delegate (int index)
                    {
                        SelectedEvent = null;
                        CurrentCommand = index;
                        //Debug.Log($"CurrentCommand:{CurrentCommand}");
                    }, null, widthPopup: 60, GUILayout.Width(65), GUILayout.Height(heightFirstRowCmd));


                // Select quantization
                MPTKGui.ComboBox(ref PopupSelectQuantization, "Snap: {Label}", PopupItemsQuantization, false,
                       delegate (int index) { Context.IndexQuantization = index; CalculRatioQuantization(); }, null, widthPopup: 200, GUILayout.Width(105), GUILayout.Height(heightFirstRowCmd));

                CurrentTick = MPTKGui.LongField("Tick:", CurrentTick, min: 0, max: 99999999999999999, maxLength: 7, widthLabel: 30, widthText: 60);

                GUILayout.EndHorizontal();

                // ------ Line 2 -------
                GUILayout.BeginHorizontal();

                if (CurrentCommand == 0) //noteon
                {
                    CurrentNote = MPTKGui.IntField("Note:", CurrentNote, min: 0, max: 127, maxLength: 3, widthLabel: 30, widthText: 30);
                    CurrentVelocity = MPTKGui.IntField("Velocity:", CurrentVelocity, min: 0, max: 127, maxLength: 3, widthLabel: 50, widthText: 30);
                }
                else if (CurrentCommand == 1) //preset change
                {
                    MPTKGui.ComboBox(ref PopupSelectPreset, "{Label}", PopupItemsPreset, false,
                            action: delegate (int index)
                            {
                                CurrentPreset = PopupItemsPreset[index].Value;
                                Repaint();
                            },
                            null, widthPopup: 180, option: GUILayout.MinWidth(100));
                }
                else if (CurrentCommand == 2) //tempo
                {
                    CurrentBPM = MPTKGui.IntField("BPM:", CurrentBPM, min: 1, max: 9999, maxLength: 4, widthLabel: 40, widthText: 60);
                }
                else if (CurrentCommand == 3) //text
                {
                    CurrentText = GUILayout.TextField(CurrentText, MPTKGui.TextField, GUILayout.MinWidth(100));
                }

                //GUILayout.FlexibleSpace();


                if (CurrentCommand == 0)
                    CurrentDuration = MPTKGui.IntField("Duration:", CurrentDuration, min: 0, max: 999999999, maxLength: 7, widthLabel: 50, widthText: 60);

                GUILayout.EndHorizontal();

                // ------  Line 3 --------
                GUILayout.BeginHorizontal();

                CmdAddOrApplyEvent();
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally
            {
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }

        private void CmdAddOrApplyEvent()
        {
            if (SelectedEvent == null)
            {
                // Mode create
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Add", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                {
                    CreateEventFromPad();
                }
            }
            else
            {
                if (GUILayout.Button("Unselect", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                    SelectedEvent = null;

                GUILayout.FlexibleSpace();

                // Mode apply or delete on selectedInFilterList
                if (GUILayout.Button("Apply", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                {
                    ApplyMidiChangeFromPad();
                }

                if (GUILayout.Button("Del", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                {
                    DeleteEventFromMidiFileWriter(SelectedEvent);
                    SelectedEvent = null;
                }
            }
        }

        private void CreateEventFromPad()
        {
            // Add channel if needed
            AddChannel(CurrentChannel);
            MPTKEvent newEvent = null;

            CurrentTick = CalculateQuantization(CurrentTick);
            CurrentDuration = (int)CalculateQuantization((long)CurrentDuration);

            newEvent = new MPTKEvent()
            {
                Track = 1,
                Channel = CurrentChannel,
                Tick = CurrentTick,
            };

            if (CurrentCommand == 0) //noteon
            {
                newEvent.Command = MPTKCommand.NoteOn;
                newEvent.Value = CurrentNote;
                // Duration = Convert.ToInt64(LoadedMidi.MPTK_ConvertTickToTime(MidiFileWriter.MPTK_DeltaTicksPerQuarterNote)),
                // TBD: use of the current BPM
                newEvent.Duration = Convert.ToInt64(MidiFileWriter.MPTK_PulseLenght * CurrentDuration);
                newEvent.Length = CurrentDuration;
                newEvent.Velocity = CurrentVelocity;
                InsertEventIntoMidiFileWriter(newEvent);
            }
            else if (CurrentCommand == 1) //preset change
            {
                newEvent.Command = MPTKCommand.PatchChange;
                newEvent.Value = PopupSelectPreset.SelectedValue;
                InsertEventIntoMidiFileWriter(newEvent);
            }
            else
                EditorUtility.DisplayDialog("Loading option", "Not yet implemented", "OK");
        }

        private void ApplyMidiChangeFromPad()
        {
            if (CurrentCommand == 0) //noteon
            {
                CurrentTick = CalculateQuantization(CurrentTick);
                CurrentDuration = (int)CalculateQuantization((long)CurrentDuration);

                SelectedEvent.Tick = CurrentTick;
                SelectedEvent.Channel = CurrentChannel;
                SelectedEvent.Value = CurrentNote;
                // Duration = Convert.ToInt64(LoadedMidi.MPTK_ConvertTickToTime(MidiFileWriter.MPTK_DeltaTicksPerQuarterNote)),
                // TBD: use of the current BPM
                SelectedEvent.Duration = Convert.ToInt64(MidiFileWriter.MPTK_PulseLenght * CurrentDuration);
                SelectedEvent.Length = CurrentDuration;
                SelectedEvent.Velocity = CurrentVelocity;
            }
            else if (CurrentCommand == 1) //preset change
            {
                SelectedEvent.Value = PopupSelectPreset.SelectedValue;
            }
            else
                Debug.LogWarning("Not yet implemented");
            ApplyEventToMidiFileWriter(SelectedEvent);
        }

        private bool AddChannel(int channel)
        {
            try
            {
                //Debug.Log($"Add channel {channel}");
                if (channel > 15 || sectionAll.SectionExist(channel))
                    return false;

                MidiFileWriter.MPTK_AddChangePreset(1, 0, channel, 0);
                // Add a channel, with a default preset and lownote=60 and highnote=65
                sectionAll.AddSection(channel);
                Context.Modified = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AddChannel {channel} {ex}");
            }
            return true;
        }
        private bool DeleteChannel(int channel)
        {
            try
            {
                Debug.Log($"Delete channel {channel}");
                if (channel > 15 || !sectionAll.SectionExist(channel))
                    return false;

                MidiFileWriter.MPTK_DeleteChannel(channel);
                sectionAll.InitSections();
                Context.Modified = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AddChannel {channel} {ex}");
            }
            return true;
        }

        private bool ApplyEventToMidiFileWriter(MPTKEvent modifiedEvent)
        {
            try
            {
                modifiedEvent = ApplyQuantization(modifiedEvent, toLowerValue: true);
                // Not used by the Midi synth, will be calculate if reloaded but we need it if position is displayed in second
                modifiedEvent.RealTime = ConvertTickToDuration(modifiedEvent.Tick);

                //int index = MidiLoad.MPTK_SearchEventFromTick(MidiFileWriter.MPTK_MidiEvents, modifiedEvent.Tick);
                //if (index < 0) index = 0;
                //MidiEvents.Insert(index, modifiedEvent);
                Debug.Log($"ApplyEventInMidiFileWriter - MIDI Event:{modifiedEvent}");

                MidiFileWriter.MPTK_StableSortEvents();
                FindLastMidiEvents();
                sectionAll.InitSections();
                Context.Modified = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ApplyEventToMidiFileWriter {modifiedEvent} {ex}");
            }
            Repaint();
            return true;
        }
        private bool InsertEventIntoMidiFileWriter(MPTKEvent newEvent)
        {
            try
            {
                if (newEvent.Command == MPTKCommand.NoteOn && newEvent.Duration == 0)
                {
                    Debug.Log($"Can't store noteon with duration 0 {newEvent}");
                    return false;
                }

                newEvent = ApplyQuantization(newEvent, toLowerValue: true);

                // Not used by the Midi synth, will be calculate if reloaded but we need it if position is displayed in second
                newEvent.RealTime = ConvertTickToDuration(newEvent.Tick);

                int index = MidiLoad.MPTK_SearchEventFromTick(MidiEvents, newEvent.Tick);
                if (index < 0)
                    index = 0;
                if (CheckMidiEventExist(newEvent, index))
                    return false;
                MidiEvents.Insert(index, newEvent);
                //Debug.Log($"Create MIDI event -  index:{index} MIDI Event:{newEvent} ");

                MidiFileWriter.MPTK_StableSortEvents();
                FindLastMidiEvents();
                sectionAll.InitSections();
                Context.Modified = true;
                SelectedEvent = newEvent;

            }
            catch (Exception ex)
            {
                Debug.LogWarning($"InsertEventIntoMidiFileWriter {newEvent} {ex}");
            }
            Repaint();
            return true;
        }

        private bool CheckMidiEventExist(MPTKEvent newEvent, int index)
        {
            bool exist = false;
            while (index < MidiEvents.Count && MidiEvents[index].Tick == newEvent.Tick)
            {
                if (MidiEvents[index].Command == newEvent.Command &&
                    MidiEvents[index].Channel == newEvent.Channel &&
                    MidiEvents[index].Value == newEvent.Value)
                {
                    Debug.LogWarning($"MIDI event already exists - Action canceled -  index:{index} MIDI Event:{newEvent} ");
                    return true;
                }
                index++;
            }
            return exist;
        }

        private bool DeleteEventFromMidiFileWriter(MPTKEvent delEvent)
        {
            bool ok = true;
            try
            {
                int index = MidiLoad.MPTK_SearchEventFromTick(MidiFileWriter.MPTK_MidiEvents, delEvent.Tick);
                if (index >= 0)
                {
                    MidiEvents.Remove(delEvent);
                    Debug.Log($"Delete MIDI event -  index:{index} MIDI Event:{delEvent} ");

                    MidiFileWriter.MPTK_StableSortEvents();
                    FindLastMidiEvents();
                    sectionAll.InitSections();
                    Context.Modified = true;
                    SelectedEvent = delEvent;
                    //channelMidi.Print();
                    Repaint();
                }
                else
                    ok = false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DeleteEventFromMidiFileWriter {delEvent} {ex}");
            }
            return ok;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="startx">AREA_BORDER</param>
        /// <param name="starty">HEIGHT_HEADER + HEIGHT_PLAYER_CMD</param>
        /// <param name="width">width of visible area : window.position.width - 2 * AREA_BORDER</param>
        /// <param name="height">heightFirstRowCmd of visible area : window.position.heightFirstRowCmd - starty</param>
        private void ShowSequencer(float startx, float starty, float width, float height)
        {
            try // Begin area MIDI events list
            {
                //if (MidiEvents.Count == 0)
                //    // Draw background MIDI events
                //    GUI.Box(new Rect(startx, starty, width, heightFirstRowCmd), "", BackgroundMidiEvents); // MPTKGui.stylePanelGrayLight
                //else
                if (MidiEvents.Count > 0)
                {
                    if (winPopupSynth != null)
                        winPopupSynth.Repaint();

                    //DisplayPerf(null, true);
                    sectionAll.CalculateSizeAllSections(LastMidiEvent, Context.QuarterWidth, Context.CellHeight);


                    if (Event.current.type != EventType.MouseMove)
                    {
                        startXEventsList = startx + WIDTH_KEYBOARD;
                        startYEventsList = starty + HEIGHT_TIMELINE;
                        widthVisibleEventsList = width - WIDTH_KEYBOARD - WidthScrollVert; // with of the area displayed on the screen
                        heightVisibleEventsList = height - HEIGHT_TIMELINE - HeightScrollHori; // heightFirstRowCmd of the area displayed on the screen

                        // Contains timeline, keyboard, events
                        MainArea.Position.x = startx;
                        MainArea.Position.y = starty;
                        MainArea.Position.width = width - WidthScrollVert;
                        MainArea.Position.height = height - HeightScrollHori;

                        DrawMeasureLine(startXEventsList, starty, widthVisibleEventsList, HEIGHT_TIMELINE);

                        MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position.x = startx;
                        MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position.y = startYEventsList;
                        MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position.width = WIDTH_KEYBOARD - 1;
                        MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position.height = heightVisibleEventsList;
                        MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys].Position = MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position;

                        DrawKeyboard(startx, startYEventsList, WIDTH_KEYBOARD - 1, heightVisibleEventsList);

                        // Draw background MIDI events
                        GUI.Box(new Rect(startXEventsList, startYEventsList, widthVisibleEventsList /*Section.FullWidthSections + 1*/, heightVisibleEventsList /*Section.FullHeightSections + 1*/), "", BackgroundMidiEvents1); // MPTKGui.stylePanelGrayLight

                        Rect midiEventsVisibleRect = new Rect(startXEventsList, startYEventsList, widthVisibleEventsList + WidthScrollVert, heightVisibleEventsList + HeightScrollHori); // heightFirstRowCmd to integrate the scrollbar
                        Rect midiEventsContentRect = new Rect(0, 0, Section.FullWidthSections, Section.FullHeightSections);

                        MainArea.SubArea[(int)AreaUI.AreaType.Channels].Position = midiEventsVisibleRect;

                        ScrollerMidiEvents = GUI.BeginScrollView(midiEventsVisibleRect, ScrollerMidiEvents, midiEventsContentRect, false, false);

                        DrawGridAndBannerChannels(widthVisibleEventsList, heightVisibleEventsList);
                        DrawBorderDragCell(widthVisibleEventsList, heightVisibleEventsList);
                        DrawMidiEvents(startXEventsList, starty, startYEventsList, widthVisibleEventsList, heightVisibleEventsList);
                        DrawPlayingPosition(startXEventsList, widthVisibleEventsList);
                    }
                }
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { GUI.EndScrollView(); }
        }

        private void DrawPlayingPosition(float startXEventsList, float widthVisibleEventsList)
        {
            // Draw current position playing
            if (/*PositionSequencerPix > 0 &&*/ Context.FollowEvent)// || !Player.MPTK_IsPlaying)
            {
                SetScrollXPosition(widthVisibleEventsList);
            }
            if (Player.MPTK_IsPlaying)
            {
                rectPositionSequencer.x = PositionSequencerPix;
                rectPositionSequencer.height = Section.FullHeightSections;
                GUI.DrawTexture(rectPositionSequencer, SepPositionTexture);
            }
            if (Context.LoopStart > 0)
            {
                rectPositionLoopStart.x = sectionAll.ConvertTickToPosition(Context.LoopStart); ;
                rectPositionLoopStart.height = Section.FullHeightSections;
                GUI.DrawTexture(rectPositionLoopStart, SepLoopTexture);
            }
            if (Context.LoopEnd > 0)
            {
                rectPositionLoopEnd.x = sectionAll.ConvertTickToPosition(Context.LoopEnd); ;
                rectPositionLoopEnd.height = Section.FullHeightSections;
                GUI.DrawTexture(rectPositionLoopEnd, SepLoopTexture);
            }
            //Debug.Log($"x:{ScrollerMidiEvents.x} FullWidthSections:{Section.FullWidthSections} startXEventsList:{startXEventsList} widthVisibleEventsList:{widthVisibleEventsList} PositionSequencerPix:{PositionSequencerPix}");
        }

        private static void SetScrollXPosition(float widthVisibleEventsList)
        {
            ScrollerMidiEvents.x = PositionSequencerPix - widthVisibleEventsList / 2f;
            // Set min / max valeur of the scroll
            if (ScrollerMidiEvents.x > Section.FullWidthSections - widthVisibleEventsList) ScrollerMidiEvents.x = Section.FullWidthSections - widthVisibleEventsList;
            if (ScrollerMidiEvents.x < 0) ScrollerMidiEvents.x = 0;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="startx"></param>
        /// <param name="starty"></param>
        /// <param name="width">visible area</param>
        /// <param name="height">visible area</param>
        private void DrawMeasureLine(float startx, float starty, float width, float height)
        {
            try // Begin area measure line
            {
                GUILayout.BeginArea(new Rect(startx, starty, width, height), MPTKGui.stylePanelGrayLight);
                TimelineStyle.contentOffset = new Vector2(-ScrollerMidiEvents.x, 0);
                // Draw quarter/measure separator. Start from tick=0 but first separator start after the first quarter
                if (Context.QuarterWidth > 1f) // To avoid infinite loop
                {
                    int quarterBar = 0;
                    int quarterTimeline = 0;
                    int measure = 1;
                    float endLastLabelPos = 0;

                    //int longMeasureLabel = 3; // Full label "Measure 00" and quarter separator
                    //if (MeasureWidth < 4f) longMeasureLabel = 0; // ""
                    //else if (MeasureWidth < 9f) longMeasureLabel = 1; // "0"
                    //else if (MeasureWidth < 16f) longMeasureLabel = 2; // "M 00"

                    //Debug.Log($"size:{size}  MPTK_NumberBeatsMeasure:{LoadedMidi.MPTK_NumberBeatsMeasure} CellQuarterWidth:{CellQuarterWidth} ScrollerMidiEvents:{ScrollerMidiEvents}");
                    bool displayTime = true;
                    for (float xQuarter = 0; xQuarter <= Section.FullWidthSections; xQuarter += Context.QuarterWidth)
                    {

                        //Debug.Log($"ScrollerMidiEvents.x:{ScrollerMidiEvents.x} xQuarter:{xQuarter} separatorQuarterRect.xMax:{separatorQuarterRect.xMax} xQuarter:{xQuarter} quarterBar:{quarterBar} measure:{measure} MPTK_NumberBeatsMeasure:{LoadedMidi.MPTK_NumberBeatsMeasure}");
                        Rect separatorQuarterRect = new Rect(xQuarter - ScrollerMidiEvents.x, 0, 1, height);
                        //Debug.Log($"ScrollerMidiEvents.x:{ScrollerMidiEvents.x} separatorQuarterRect.x:{separatorQuarterRect.x} separatorQuarterRect.xMax:{separatorQuarterRect.xMax} xQuarter:{xQuarter} quarterBar:{quarterBar} measure:{measure} MPTK_NumberBeatsMeasure:{LoadedMidi.MPTK_NumberBeatsMeasure}");
                        // Start of a new measure, draw value measure on the previous measure
                        if (separatorQuarterRect.x >= 0 && separatorQuarterRect.x - ScrollerMidiEvents.x <= width + 2 * Context.QuarterWidth)
                        {
                            if (quarterBar == MidiFileWriter.MPTK_NumberBeatsMeasure)
                            {
                                Rect measureRect = new Rect(xQuarter - sectionAll.MeasureWidth, 0, sectionAll.MeasureWidth, height / 2f);
                                string label = "";
                                if (sectionAll.MeasureWidth > 70f) // 10 * 7
                                    label = "Measure " + measure.ToString();
                                else if (sectionAll.MeasureWidth > 28) // 4 * 7
                                    label = "M " + measure.ToString();
                                else if (sectionAll.MeasureWidth > 14)  // 2 * 7
                                    label = measure.ToString();
                                else if (sectionAll.MeasureWidth > 10 && measure % 2 == 0)
                                    label = measure.ToString();
                                else if (sectionAll.MeasureWidth > 5 && measure % 4 == 0)
                                    label = measure.ToString();
                                else if (sectionAll.MeasureWidth > 0 && measure % 8 == 0)
                                    label = measure.ToString();

                                GUI.Label(measureRect, label, TimelineStyle);
                                // Draw measure separator
                                if (Context.QuarterWidth > 5f)
                                    GUI.DrawTexture(separatorQuarterRect, SepBarText);
                            }
                            else
                            {
                                // Draw quarter separator
                                if (Context.QuarterWidth > 17f)
                                    GUI.DrawTexture(separatorQuarterRect, SepQuarterText);
                            }

                            // Draw timeline
                            if (quarterTimeline > 0)
                            {
                                Rect timeQuarterRect = new Rect(xQuarter - Context.QuarterWidth / 2f, height / 2f, Context.QuarterWidth, height / 2f);
                                string sTime = null;
                                int tick = quarterTimeline * MidiFileWriter.MPTK_DeltaTicksPerQuarterNote;

                                if (!displayTime)
                                {
                                    if (MidiFileWriter.MPTK_NumberBeatsMeasure <= 4)
                                    {
                                        if (quarterBar == MidiFileWriter.MPTK_NumberBeatsMeasure)
                                            displayTime = true;
                                    }
                                    else if (quarterBar == MidiFileWriter.MPTK_NumberBeatsMeasure / 2)
                                        displayTime = true;
                                }
                                if (displayTime)
                                {
                                    if (Context.DisplayTime == 0)
                                    {
                                        sTime = tick.ToString();
                                    }
                                    else if (Context.DisplayTime == 1)
                                    {
                                        //sTime = (LoadedMidi.MPTK_ConvertTickToTime(tick) / 1000f).ToString("F2");
                                        sTime = (MidiFileWriter.MPTK_PulseLenght * tick / 1000f).ToString("F2");
                                    }
                                    else if (Context.DisplayTime == 2)
                                    {
                                        //double timeSecond = LoadedMidi.MPTK_ConvertTickToTime(tick) / 1000d;
                                        double timeSecond = MidiFileWriter.MPTK_PulseLenght * tick / 1000d;
                                        TimeSpan timePos = TimeSpan.FromSeconds(timeSecond);
                                        if (timeSecond < 60d)
                                            sTime = string.Format("{0}.{1:000}", timePos.Seconds, timePos.Milliseconds);
                                        else if (timeSecond < 3600d)
                                            sTime = string.Format("{0}:{1}.{2:000}", timePos.Minutes, timePos.Seconds, timePos.Milliseconds);
                                        else
                                            sTime = string.Format("{0}:{1}:{2}.{3:000}", timePos.Hours, timePos.Minutes, timePos.Seconds, timePos.Milliseconds);
                                    }
                                    if (sTime != null)
                                    {
                                        float len = sTime.Length * 7f;
                                        float begNewLabelPos = timeQuarterRect.x + (timeQuarterRect.width - len) / 2f;
                                        //time = $"{endLastLabelPos:F1} {begNewLabelPos:F1}";
                                        if (begNewLabelPos > endLastLabelPos + 2f)
                                        {
                                            GUI.Label(timeQuarterRect, sTime, TimelineStyle);
                                            endLastLabelPos = timeQuarterRect.x + (timeQuarterRect.width + len) / 2f;
                                        }
                                        else
                                            displayTime = false;
                                    }
                                }
                            }
                        }

                        if (quarterBar == MidiFileWriter.MPTK_NumberBeatsMeasure)
                        {
                            measure++;
                            quarterBar = 1;
                        }
                        else
                            quarterBar++;
                        quarterTimeline++;
                    }
                }
                TimelineStyle.contentOffset = Vector2.zero;
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { GUILayout.EndArea(); }

            //DisplayPerf("DrawMeasureLine");

        }



        private void DrawKeyboard(float startx, float starty, float width, float height)
        {
            const float HEIGHT_LIMITE = 11f;
            GUIStyle keyLabelStyle;
            Texture keyDrawTexture;

            //Color savedColor = GUI.color;
            try // try keyboard
            {
                GUILayout.BeginArea(new Rect(startx, starty, width, height));//, MPTKGui.styleListTitle);
                                                                             // Draw white keys in first then black keys to overlap white keys
                MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].SubArea.Clear();
                MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys].SubArea.Clear();
                for (int isSharp = 0; isSharp <= 1; isSharp++)
                {
                    int subZone = isSharp == 0 ? (int)AreaUI.AreaType.WhiteKeys : (int)AreaUI.AreaType.BlackKeys;

                    for (int channel = 0; channel < 16; channel++)
                    {
                        Section section = sectionAll.Sections[channel];
                        if (section != null)
                        {
                            if (channel == 9)
                            {
                                //
                                // Draw Drum
                                // ---------
                                if (isSharp == 0)
                                {
                                    int keyValue = section.LayoutNote.HigherNote;
                                    for (float y = section.LayoutNote.BegY; keyValue >= section.LayoutNote.LowerNote; y += Context.CellHeight, keyValue--)
                                    {
                                        float yKey = y - ScrollerMidiEvents.y;
                                        float hKey = Context.CellHeight;
                                        float wKey = width;
                                        keyLabelStyle = WhiteKeyLabelStyle;
                                        keyDrawTexture = WhiteKeyDrawTexture;

                                        if (yKey + hKey <= 0 || yKey >= height)
                                            // out of the zone
                                            continue;

                                        // Build key rectPositionSequencer for the button
                                        Rect keyRect = new Rect(0, yKey, wKey, hKey);

                                        // Display all label if heightFirstRowCmd > 11 or only C note
                                        string label = Context.CellHeight >= HEIGHT_LIMITE ? /*keyValue.ToString() + " " +*/ HelperNoteLabel.LabelPercussion(keyValue) + " " : "";

                                        //Debug.Log($"isSharp:{isSharp} subZone:{subZone} keyValue:{keyValue} yKey:{yKey}");
                                        MainArea.SubArea[subZone].SubArea.Add(new AreaUI()
                                        {
                                            Position = new Rect(keyRect.x + startx, keyRect.y + starty, keyRect.width, keyRect.height),
                                            Channel = channel,
                                            Value = keyValue
                                        });

                                        GUI.DrawTexture(keyRect, keyDrawTexture);
                                        GUI.DrawTexture(keyRect, keyDrawTexture, ScaleMode.StretchToFill, false, 0f, Color.gray, borderWidth: 1f, borderRadius: 2f);

                                        keyLabelStyle.contentOffset = Vector2.zero;

                                        if (label.Length > 0)
                                            GUI.Label(keyRect, label, keyLabelStyle);
                                    }
                                }
                            }
                            else
                            {
                                //
                                // Draw keys
                                // ---------
                                int keyValue = section.LayoutNote.HigherNote;
                                for (float y = section.LayoutNote.BegY; keyValue >= section.LayoutNote.LowerNote; y += Context.CellHeight, keyValue--)
                                {
                                    if (isSharp == 0 && !HelperNoteLabel.IsSharp(keyValue) || isSharp == 1 && HelperNoteLabel.IsSharp(keyValue))
                                    {
                                        float yKey = y - ScrollerMidiEvents.y;
                                        float hKey = Context.CellHeight - 1f;
                                        float wKey = width;
                                        if (HelperNoteLabel.IsSharp(keyValue))
                                        {
                                            keyLabelStyle = BlackKeyLabelStyle;
                                            keyDrawTexture = BlackKeyDrawTexture;
                                        }
                                        else
                                        {
                                            keyLabelStyle = WhiteKeyLabelStyle;
                                            keyDrawTexture = WhiteKeyDrawTexture;
                                        }
                                        if (isSharp == 0)
                                        {
                                            // Make higher keys for keys with a sharp above: C, D, F, G, A
                                            if (keyValue != section.LayoutNote.HigherNote && HelperNoteLabel.IsSharp(keyValue + 1))
                                            {
                                                yKey -= Context.CellHeight / 2f;
                                                hKey += Context.CellHeight / 2f;
                                            }
                                            // Make higher keys for keys with a sharp below: D, E, G, A, B
                                            if (keyValue != section.LayoutNote.LowerNote && HelperNoteLabel.IsSharp(keyValue - 1))
                                            {
                                                hKey += Context.CellHeight / 2f;
                                            }
                                            // Label offset Only for
                                            //  white keys not at the begin and
                                            //  not at the rectClear position in the keyboard
                                            if (keyValue != section.LayoutNote.HigherNote && keyValue != section.LayoutNote.LowerNote && Context.CellHeight >= HEIGHT_LIMITE)
                                                switch (HelperNoteLabel.NoteNumber(keyValue))
                                                {
                                                    case 0: keyLabelStyle.contentOffset = new Vector2(0, 5); break;   // C
                                                    case 4: keyLabelStyle.contentOffset = new Vector2(0, -4); break;  // E
                                                    case 5: keyLabelStyle.contentOffset = new Vector2(0, 5); break;   // F
                                                    case 11: keyLabelStyle.contentOffset = new Vector2(0, -4); break; // B
                                                }
                                        }
                                        else
                                            // Black keys: smaller and label always v-centered
                                            wKey = width * 0.66f;

                                        if (yKey + hKey <= 0 || yKey >= height)
                                        {
                                            // out of the zone
                                            //Debug.Log($"out isSharp:{isSharp} subZone:{subZone} keyValue:{keyValue} yKey:{yKey}");
                                            continue;
                                        }

                                        // Build key rectPositionSequencer for the button
                                        Rect keyRect = new Rect(0, yKey, wKey, hKey);

                                        // Display all label if heightFirstRowCmd > 11 or only C note
                                        string label = Context.CellHeight >= HEIGHT_LIMITE || HelperNoteLabel.NoteNumber(keyValue) == 0 ? HelperNoteLabel.LabelC4FromMidi(keyValue) + " " : "";

                                        //Debug.Log($"isSharp:{isSharp} subZone:{subZone} keyValue:{keyValue} yKey:{yKey}");
                                        MainArea.SubArea[subZone].SubArea.Add(new AreaUI()
                                        {
                                            Position = new Rect(keyRect.x + startx, keyRect.y + starty/* + ScrollerMidiEvents.y*/, keyRect.width, keyRect.height),
                                            Channel = channel,
                                            Value = keyValue
                                        });


                                        GUI.DrawTexture(keyRect, keyDrawTexture);
                                        GUI.DrawTexture(keyRect, keyDrawTexture, ScaleMode.StretchToFill, false, 0f, Color.gray, borderWidth: 1f, borderRadius: 2f);

                                        if (label.Length > 0)
                                            GUI.Label(keyRect, label, keyLabelStyle);

                                        keyLabelStyle.contentOffset = Vector2.zero;
                                    }
                                }
                            }
                            //
                            // Draw preset area
                            // ----------------
                            if (isSharp == 0) // only one time !
                            {
                                float yPreset = section.LayoutPreset.BegY - ScrollerMidiEvents.y;
                                //if (section.Presets.Count > 2) Debug.Log("");
                                foreach (Section.PresetSet sectionPreset in section.Presets)
                                {
                                    Rect presetRect = new Rect(0, yPreset, width, Context.CellHeight);
                                    // Select display time format

                                    MPTKGui.ComboBox(presetRect, ref sectionPreset.PopupPreset, "{Label}", PopupItemsPreset, false,
                                       action: delegate (int index)
                                       {
                                           int newPresetValue = PopupItemsPreset[index].Value;
                                           //Debug.Log($"index:{index} {PopupItemsPreset[index].Caption} preset:{sectionPreset.Value} to:{newPresetValue}");
                                           MidiEvents.ForEach(m =>
                                           {
                                               if (m.Channel == section.Channel && m.Command == MPTKCommand.PatchChange && m.Value == sectionPreset.Value)
                                               {
                                                   m.Value = newPresetValue;/* Debug.Log(m);*/
                                               }
                                           });
                                           sectionPreset.Value = newPresetValue;
                                           Repaint();
                                       },
                                       style: PresetButtonStyle, widthPopup: 150, option: null);
                                    sectionPreset.PopupPreset.SelectedIndex = sectionPreset.Value;
                                    yPreset += Context.CellHeight;
                                }
                            }

                            //
                            // Draw channel number
                            // -----------------------------------
                            if (isSharp == 0) // only one time !
                            {
                                GUI.Label(new Rect(0, section.LayoutAll.BegY - ScrollerMidiEvents.y, width, HEIGHT_CHANNEL_BANNER), $"Channel {channel}", ChannelBannerStyle);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { GUILayout.EndArea();/* GUI.color = savedColor;*/ }

            //DisplayPerf("DrawKeyboard");
        }


        private void DrawGridAndBannerChannels(float width, float height)
        {
            // Draw vertical lines, quarter/measure separator
            // ----------------------------------------------
            try
            {
                if (Context.QuarterWidth > 1f) // To avoid infinite loop
                {
                    int quarter = 1;
                    for (float xQuarter = Context.QuarterWidth; xQuarter <= Section.FullWidthSections; xQuarter += Context.QuarterWidth)
                    {
                        {
                            Rect separatorQuarterRect = Rect.zero;
                            bool draw = false;
                            // Draw only visible
                            if (xQuarter >= ScrollerMidiEvents.x && xQuarter < ScrollerMidiEvents.x + width)
                            {
                                separatorQuarterRect = new Rect(xQuarter, HEIGHT_CHANNEL_BANNER, 1, Section.FullHeightSections - HEIGHT_CHANNEL_BANNER);
                                draw = true;
                            }
                            //Debug.Log($"FullWidthSections:{Section.FullWidthSections} xQuarter:{xQuarter} {MidiFileWriter.MPTK_NumberBeatsMeasure} {xQuarter % MidiFileWriter.MPTK_NumberBeatsMeasure}");
                            if (quarter == MidiFileWriter.MPTK_NumberBeatsMeasure)
                            {
                                // Draw only on visible area
                                if (Context.QuarterWidth > 5f && draw)
                                    GUI.DrawTexture(separatorQuarterRect, SepBarText);
                                quarter = 1;
                            }
                            else
                            {
                                // Draw only on visible area
                                if (Context.QuarterWidth > 17f && draw)
                                    GUI.DrawTexture(separatorQuarterRect, SepQuarterText);
                                quarter++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { }

            // Draw horizontal lines and channel banner
            // ----------------------------------------
            try
            {
                float widthBannerVisible = width;//< Section.FullWidthSections  ? Section.FullWidthSections + 1 : width;
                //Debug.Log($"FullWidthSections:{Section.FullWidthSections} width:{width} "); 

                for (int channel = 0; channel < 16; channel++)
                {
                    // Ambitus + row for preset
                    Section section = sectionAll.Sections[channel];
                    if (section != null)
                    {
                        // Draw channel banner
                        string infoChannel;
                        infoChannel = $"Channel: {channel}   Note: {section.LayoutNote.Count}   Preset: {section.LayoutPreset.Count}";
#if DEBUG_EDITOR
                        infoChannel += $"  NoteLayout:{section.LayoutNote.BegY} {section.LayoutNote.Height} Drawn:{section.LayoutAll.Count} QuarterWidth:{Context.QuarterWidth} MeasureWidth:{sectionAll.MeasureWidth} CellHeight:{Context.CellHeight} MouseAction:{MouseAction}";
#endif
                        Vector2 size = ChannelBannerStyle.CalcSize(new GUIContent(infoChannel));
                        Rect channelBannerRect = new Rect(ScrollerMidiEvents.x, section.LayoutAll.BegY, widthBannerVisible, HEIGHT_CHANNEL_BANNER);
                        // Display banner centered on the visible area
                        ChannelBannerStyle.contentOffset = new Vector2(((widthBannerVisible - size.x) / 2f), 0);
                        GUI.Box(channelBannerRect, infoChannel, ChannelBannerStyle);
                        ChannelBannerStyle.contentOffset = Vector2.zero;

                        if (Context.QuarterWidth > 5f)
                        {
                            // Draw separator between each preset line but not the first
                            for (float y = section.LayoutPreset.BegY + Context.CellHeight; y <= section.LayoutPreset.EndY; y += Context.CellHeight)
                            {
                                Rect rect = new Rect(ScrollerMidiEvents.x, y, widthBannerVisible, 1);
                                GUI.DrawTexture(rect, SepPresetTexture); // green
                            }

                            // Draw separator between each row notes including the first and the rectClear
                            for (float y = section.LayoutNote.BegY; y <= section.LayoutNote.EndY; y += Context.CellHeight)
                            {
                                // y+1 to avoid overlap with preset separator
                                Rect rect = new Rect(ScrollerMidiEvents.x, y + 1, widthBannerVisible, 1);
                                GUI.DrawTexture(rect, SepNoteTexture); // blue
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { }
            //DisplayPerf("DrawGrid");

        }

        private void DrawBorderDragCell(float width, float height)
        {

            if (LastMouseMove != null)
            {
                //Rect rectPositionSequencer = new Rect(LastMouseMove.mousePosition.x, 0, 1, ChannelMidi.FullHeightChannelsZone);
                //GUI.DrawTexture(rectPositionSequencer, SepDragMouseText);
            }

            if (SelectedEvent != null)
            {
                float cellX;
                Rect rect;
                //float x = (float)(SelectedEvent.Tick + deltaTick) * CellWidth / (float)LoadedMidi.MPTK_DeltaTicksPerQuarterNote ;

                if (MouseAction == enAction.MoveNote)// || dragAction == MouseAction.LengthLeftNote)
                {
                    // Draw vertical line at note-on
                    cellX = ((float)SelectedEvent.Tick / (float)MidiFileWriter.MPTK_DeltaTicksPerQuarterNote) * Context.QuarterWidth;
                    //Debug.Log($"cellX:{cellX} ScrollerMidiEvents.x:{ScrollerMidiEvents.x}");
                    rect = new Rect(cellX, ScrollerMidiEvents.y, 1, height);
                    GUI.DrawTexture(rect, SepDragEventText);
                }
                if (MouseAction == enAction.MoveNote)// || dragAction == MouseAction.LengthRightNote)
                {
                    // Draw vertical line at note-off
                    cellX = ((float)(SelectedEvent.Tick + SelectedEvent.Length) / (float)MidiFileWriter.MPTK_DeltaTicksPerQuarterNote) * Context.QuarterWidth;
                    rect = new Rect(cellX, ScrollerMidiEvents.y, 1, height);
                    GUI.DrawTexture(rect, SepDragEventText);
                }
                // Draw mouse position vertical
                rect = new Rect(Event.current.mousePosition.x, ScrollerMidiEvents.y, 1, height);
                GUI.DrawTexture(rect, SepDragMouseText);

                // Draw mouse position horizontal
                rect = new Rect(0, Event.current.mousePosition.y, width, 1);
                GUI.DrawTexture(rect, SepDragMouseText);
            }
            //DisplayPerf("DrawDragNote");

            // Draw always left border
            //{
            //    float x = ScrollerMidiEvents.x;
            //    Rect rectTest = new Rect(startx + x, 0, 1, heightFirstRowCmd);
            //    GUI.DrawTexture(rectTest, separatorChannelTexture);
            //    // Darw always at end of first measure
            //    x = 4 * CellQuarterWidth + ScrollerMidiEvents.x;
            //    rectTest = new Rect(startx + x, 0, 1, heightFirstRowCmd);
            //    GUI.DrawTexture(rectTest, separatorChannelTexture);
            //}
        }
        private void DrawMidiEvents(float startXEventsList, float startYLinePosition, float startYEventsList, float widthVisibleEventsList, float heightVisibleEventsList)
        {
            MainArea.SubArea[(int)AreaUI.AreaType.Channels].SubArea.Clear();
            // Foreach MIDI events on the current page
            // ---------------------------------------
            for (int channel = 0; channel < 16; channel++)
            {
                // Ambitus + row for preset
                Section section = sectionAll.Sections[channel];
                if (section != null)
                {
                    // Add an area for this channel to hold cell with MIDI events
                    AreaUI channelArea = new AreaUI()
                    {
                        Position = new Rect(startXEventsList, startYEventsList + section.LayoutAll.BegY - ScrollerMidiEvents.y, widthVisibleEventsList, section.LayoutAll.Height),
                        SubArea = new List<AreaUI>(),
                        areaType = AreaUI.AreaType.Channel,
                        Channel = channel,
                    };
                    MainArea.SubArea[(int)AreaUI.AreaType.Channels].SubArea.Add(channelArea);

                    section.LayoutAll.Count = 0;

                    // For each MIDI event filter by channel
                    foreach (MPTKEvent midiEvent in MidiEvents)
                    {
                        if (midiEvent.Channel == channel)
                        {
                            try // display one row
                            {
                                if (!DrawOneMidiEvent(startXEventsList, startYEventsList, widthVisibleEventsList, heightVisibleEventsList, section, midiEvent, channelArea))
                                {
                                    //Debug.Log($"Outside the visible area, channel {channel}");
                                    break;
                                }
                            }
                            catch (MaestroException ex)
                            {
                                Debug.LogException(ex);
                                break;
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                                throw;
                            }
                            finally { }
                        }
                    }
                }
            }
            //DisplayPerf("DrawMidiEvents");
        }

        private bool DrawOneMidiEvent(float startXEventsList, float startYEventsList, float widthVisibleEventsList, float heightVisibleEventsList, Section channelMidi, MPTKEvent midiEvent, AreaUI channelSubZone)
        {
            int index = midiEvent.Index;
            string cellText = "";
            float cellX = sectionAll.ConvertTickToPosition(midiEvent.Tick);
            float cellY;
            float cellW;
            float cellH = Context.CellHeight - 4f;

            if (cellX > ScrollerMidiEvents.x + widthVisibleEventsList)
            {
                // After the visible area, stop drawing all next channel's notes
                //Debug.Log($"After the visible area, stop drawing all channel's notes. Channel {midiEvent.Channel} Tick:{midiEvent.Tick} ScrollerMidiEvents.x:{ScrollerMidiEvents.x} width:{widthVisibleEventsList} cellX:{cellX}");
                return false;
            }
            Texture eventTexture = MidiNoteTexture; // default style
            switch (midiEvent.Command)
            {
                case MPTKCommand.NoteOn:
                    cellW = sectionAll.ConvertTickToPosition(midiEvent.Length);
                    if (midiEvent.Value < channelMidi.LayoutNote.LowerNote || midiEvent.Value > channelMidi.LayoutNote.HigherNote) return true;
                    cellY = channelMidi.LayoutNote.BegY + (channelMidi.LayoutNote.HigherNote - midiEvent.Value) * Context.CellHeight;
                    eventTexture = MidiNoteTexture;
                    if (cellH >= 6f && cellW >= 20f)
                    {
                        cellText = HelperNoteLabel.LabelC4FromMidi(midiEvent.Value);
                        if (cellW >= 40f)
                            cellText += " N:" + midiEvent.Value.ToString();
                        if (cellW >= 70f)
                            cellText += " V:" + midiEvent.Velocity.ToString();
                        else if (cellH >= 19f)
                            cellText += "\nV:" + midiEvent.Velocity.ToString();
                    }
                    break;
                case MPTKCommand.PatchChange:
                    cellW = Context.QuarterWidth / 4f; // length = quarter / 4
                    cellY = channelMidi.LayoutPreset.BegY - 1f /* for alignment with the button */ + channelMidi.GetPresetLine(midiEvent.Value) * Context.CellHeight;
                    eventTexture = MidiPresetTexture;
                    if (cellW >= 20f)
                        cellText = $"{midiEvent.Value}";
                    break;
                case MPTKCommand.NoteOff: return true;
                case MPTKCommand.ControlChange: return true;
                case MPTKCommand.MetaEvent: return true;
                case MPTKCommand.ChannelAfterTouch: return true;
                case MPTKCommand.KeyAfterTouch: return true;
                default: return true;
            }

            if (midiEvent == SelectedEvent)
                eventTexture = MidiSelectedTexture;

            //Debug.Log($"ScrollerMidiEvents:{ScrollerMidiEvents}  width:{widthVisibleEventsList} startYEventsList:{startYEventsList}  heightVisible:{heightVisibleEventsList} cellX:{cellX} cellY:{cellY} cellW:{cellW}");

            if (cellX + cellW < ScrollerMidiEvents.x)
            {
                // Before the visible area, go to next event
                //Debug.Log($"   Before the visible area, go to next event. Channel {midiEvent.Channel} Tick:{midiEvent.Tick} ScrollerMidiEvents.x:{ScrollerMidiEvents.x} width:{widthVisibleEventsList} cellX:{cellX}");
                return true;
            }

            if (cellY + Context.CellHeight < ScrollerMidiEvents.y)
            {
                // Above the visible area, go to next event
                //Debug.Log($"   Above the visible area, go to next event. Channel {midiEvent.Channel} cellY:{cellY} ScrollerMidiEvents.y:{ScrollerMidiEvents.y}  heightFirstRowCmd:{heightVisibleEventsList} CellHeight:{Sectx.CellHeight}");
                return true;
            }

            if (cellY > heightVisibleEventsList + ScrollerMidiEvents.y)
            {
                // Bellow visible area, go to next event
                //Debug.Log($"   Bellow the visible area, go to next event. Channel {midiEvent.Channel} cellY:{cellY} startYEventsList:{startYEventsList} ScrollerMidiEvents.y:{ScrollerMidiEvents.y} heightFirstRowCmd:{heightVisibleEventsList} Sectx.CellHeight:{Sectx.CellHeight}");
                return true;
            }

            // Minimum width to be able to select a MIDI event
            if (cellW < 12f) cellW = 12f;
            Rect cellRect = new Rect(cellX, cellY + 3f, cellW, cellH);
            //Debug.Log($"cellRect {cellRect} {eventTexture}");

            GUI.DrawTexture(cellRect, eventTexture);
            GUI.DrawTexture(cellRect, eventTexture, ScaleMode.StretchToFill, false, 0f, Color.gray, borderWidth: 1f, borderRadius: 0f);

            if (cellText.Length > 0)
                GUI.Label(cellRect, cellText, MPTKGui.LabelCenterSmall);

            channelMidi.LayoutAll.Count++;
            AreaUI zoneCell = new AreaUI() { midiEvent = midiEvent, Position = cellRect, };
            // Shift cell position from scroller area to absolute position
            zoneCell.Position.x += startXEventsList - ScrollerMidiEvents.x;
            zoneCell.Position.y += startYEventsList - ScrollerMidiEvents.y;
            // Add cell with note to this area
            //Debug.Log("zoneCell " + zoneCell);
            channelSubZone.SubArea.Add(zoneCell);

            return true;
        }

        void DrawRect(Rect rect, Color color, int innnerEdge)
        {
            Texture texture = MPTKGui.MakeTex(color, new RectOffset(innnerEdge, innnerEdge, innnerEdge, innnerEdge));
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, innnerEdge), texture); // top
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - innnerEdge, rect.width, innnerEdge), texture); // bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y + innnerEdge, innnerEdge, rect.height - 2 * innnerEdge), texture); // left
            GUI.DrawTexture(new Rect(rect.x + rect.width - innnerEdge, rect.y + innnerEdge, innnerEdge, rect.height - 2 * innnerEdge), texture); // right
        }

        private void EventManagement()
        {
            Event currentEvent = Event.current;
            //Debug.Log($"------- CurrentEvent  {currentEvent} --------");

            if (currentEvent.type == EventType.MouseDown)
                EventMouseDown(currentEvent);

            if (currentEvent.type == EventType.MouseUp)
                EventMouseUp();

            if (currentEvent.type == EventType.MouseDrag)
                EventMouseDrag(currentEvent);

            if (currentEvent.type == EventType.MouseMove)
                EventMouseMove(currentEvent);

            if (currentEvent.type == EventType.MouseUp & SelectedEvent != null)
            {
                NewDragEvent = null;
                //SelectedEvent = null;
                DragPosition = Vector2.zero;
                separatorDragVerticalBegin = Rect.zero;
                Repaint();
            }

            if (currentEvent.type == EventType.KeyDown)
            {
                EventKeyDown(currentEvent);

            }
            if (currentEvent.type == EventType.ScrollWheel && currentEvent.control)
            {
                EventWheel(currentEvent);
            }
        }

        private void EventMouseDown(Event currentEvent)
        {
            //Debug.Log($"Mouse Down {currentEvent.mousePosition}");
            KeyPlaying = PlayKeyboard(currentEvent);
            if (KeyPlaying == null)
            {
                // Perhaps on a a MIDI event displayed ?
                MPTKEvent mptkEvent = FindCellMouse(currentEvent);
                if (mptkEvent != null)
                {
                    if (MouseAction == enAction.DeleteNote)
                    {
                        DeleteEventFromMidiFileWriter(mptkEvent);
                    }
                    else
                    {
                        // Event found under the mouse, display it
                        //Debug.Log($"EventMouseDown selectedInFilterList MIDIEvent  {currentEvent.mousePosition} --> {mptkEvent} {MouseAction}");
                        SelectedEvent = NewDragEvent = mptkEvent;
                        ApplyEventToCmdMidi(NewDragEvent);
                    }
                    Repaint();
                }
                else if (MouseAction == enAction.CreateNote)
                {
                    //if (SelectedEvent != null)
                    //    // First step, unselect event
                    //    SelectedEvent = null;
                    //else
                    {
                        // Create a MIDI event
                        AreaUI channelsArea = MainArea.SubArea[(int)AreaUI.AreaType.Channels];
                        if (channelsArea.Position.Contains(currentEvent.mousePosition))
                        {
                            foreach (AreaUI channelZone in channelsArea.SubArea)
                            {
                                if (channelZone.Position.Contains(currentEvent.mousePosition))
                                {
                                    EventCreateMidiEvent(currentEvent, channelZone);
                                    break;
                                }
                            }
                        }
                    }
                    Repaint();
                }

            }
        }


        private void EventCreateMidiEvent(Event currentEvent, AreaUI channelZone)
        {
            Section sectionChannel = sectionAll.Sections[channelZone.Channel];
            int line = -1;
            float xMouseCorrected = currentEvent.mousePosition.x + ScrollerMidiEvents.x - X_CORR_SECTION;
            long tick = sectionAll.ConvertPositionToTick(xMouseCorrected);
            float yMouseCorrected = currentEvent.mousePosition.y + ScrollerMidiEvents.y - Y_CORR_SECTION;// position begin header channel
                                                                                                         //Debug.Log($"Create MIDI  Channel:{channelZone.Channel}  x:{xMouseCorrected} y:{yMouseCorrected} Tick:{tick} AllLayout:{sectionChannel.AllLayout.BegY} NoteLayout:{sectionChannel.NoteLayout.BegY} PresetLayout:{sectionChannel.PresetLayout.BegY}");

            if (yMouseCorrected >= sectionChannel.LayoutAll.BegY && yMouseCorrected < sectionChannel.LayoutPreset.BegY)
            {
                // Above header channel
                Debug.Log($"Header channel zone  y:{yMouseCorrected} Channel:{channelZone.Channel} AllLayout:{sectionChannel.LayoutAll} ");
                SelectedEvent = null;
            }
            else
            {
                MPTKEvent newEvent = null;
                if (yMouseCorrected >= sectionChannel.LayoutNote.BegY && yMouseCorrected <= sectionChannel.LayoutNote.EndY)
                {
                    // Above notes channel, add a note  
                    line = (int)((yMouseCorrected - sectionChannel.LayoutNote.BegY) / Context.CellHeight);
                    //Debug.Log($"Note zone  y:{yMouseCorrected} Channel:{channelZone.Channel} line:{line} NoteLayout:{sectionChannel.NoteLayout} ");
                    newEvent = new MPTKEvent()
                    {
                        Command = MPTKCommand.NoteOn,
                        Value = sectionChannel.LayoutNote.HigherNote - line,
                        //Duration = Convert.ToInt64(LoadedMidi.MPTK_ConvertTickToTime(MidiFileWriter.MPTK_DeltaTicksPerQuarterNote)),
                        // By default, create a quarter note
                        // TBD: use of the current BPM
                        Duration = Convert.ToInt64(MidiFileWriter.MPTK_PulseLenght * CurrentDuration),
                        Length = CurrentDuration,
                        Velocity = CurrentVelocity
                    };
                }
                else if (yMouseCorrected >= sectionChannel.LayoutPreset.BegY && yMouseCorrected <= sectionChannel.LayoutPreset.EndY)
                {
                    // Above preset channel, add a channel change
                    line = (int)((yMouseCorrected - sectionChannel.LayoutPreset.BegY) / Context.CellHeight);
                    Debug.Log($"Preset zone  y:{yMouseCorrected} Channel:{channelZone.Channel} Line:{line} Preset:{sectionChannel.Presets[line].Value} PresetLayout:{sectionChannel.LayoutPreset}");
                    newEvent = new MPTKEvent()
                    {
                        Command = MPTKCommand.PatchChange,
                        Value = sectionChannel.Presets[line].Value,
                    };
                }

                if (newEvent != null)
                {
                    // Common settings and add event
                    newEvent.Track = 1;
                    newEvent.Tick = tick;
                    newEvent.Channel = channelZone.Channel;
                    InsertEventIntoMidiFileWriter(newEvent);
                    ApplyEventToCmdMidi(newEvent);
                }
            }
        }

        private void EventMouseUp()
        {
            if (KeyPlaying != null)
            {
                if (KeyPlaying.Voices != null && KeyPlaying.Voices.Count > 0)
                    if (KeyPlaying.Voices[0].synth != null)
                        KeyPlaying.Voices[0].synth.MPTK_PlayDirectEvent(new MPTKEvent() { Command = MPTKCommand.NoteOff, Channel = KeyPlaying.Channel, Value = KeyPlaying.Value });
                KeyPlaying = null;
            }
            MouseAction = enAction.None;
        }

        private void EventMouseDrag(Event currentEvent)
        {
            if (NewDragEvent != null)
            {
                //Debug.Log($"******* New Event drag:  {NewDragEvent.Tick} {NewDragEvent.Value} ");
                LastMousePosition = currentEvent.mousePosition;
                DragPosition = Vector2.zero;
                SelectedEvent = NewDragEvent;
                InitialTick = SelectedEvent.Tick;
                InitialValue = SelectedEvent.Value;
                InitialDurationTick = SelectedEvent.Length;
                NewDragEvent = null;
            }
            if (SelectedEvent != null)
            {
                Section channelMidi = sectionAll.Sections[SelectedEvent.Channel];
                if (channelMidi != null)
                {
                    DragPosition += currentEvent.mousePosition - LastMousePosition;
                    LastMousePosition = currentEvent.mousePosition;
                    long deltaTick;
                    switch (MouseAction)
                    {
                        case enAction.LengthLeftNote:
                            deltaTick = Convert.ToInt64(DragPosition.x / Context.QuarterWidth * MidiFileWriter.MPTK_DeltaTicksPerQuarterNote);
                            deltaTick = CalculateQuantization(deltaTick);
                            SelectedEvent.Length = InitialDurationTick - (int)deltaTick; // sign - because the delta is negative and we need to increase the duration
                            SelectedEvent.Tick = InitialTick + deltaTick;
                            SelectedEvent.Duration = ConvertTickToDuration(SelectedEvent.Length);
                            SelectedEvent.RealTime = ConvertTickToDuration(SelectedEvent.Tick);
                            Context.Modified = true;
                            //Debug.Log($"    Event param:  DragPosition:{DragPosition} QuarterWidth:{Context.QuarterWidth}  durationTicks:{SelectedEvent.Length} deltaTick:{deltaTick}");
                            ApplyEventToCmdMidi(SelectedEvent);
                            Repaint();
                            break;

                        case enAction.LengthRightNote:
                            deltaTick = Convert.ToInt64(DragPosition.x / Context.QuarterWidth * MidiFileWriter.MPTK_DeltaTicksPerQuarterNote);
                            deltaTick = CalculateQuantization(deltaTick);
                            SelectedEvent.Length = InitialDurationTick + (int)deltaTick;
                            SelectedEvent.Length = (int)CalculateQuantization((int)SelectedEvent.Length);
                            SelectedEvent.Duration = ConvertTickToDuration(SelectedEvent.Length);
                            Context.Modified = true;
                            //Debug.Log($"    Event param:  DragPosition:{DragPosition} QuarterWidth:{Context.QuarterWidth}  durationTicks:{SelectedEvent.Length} deltaTick:{deltaTick}");
                            ApplyEventToCmdMidi(SelectedEvent);
                            Repaint();
                            break;

                        case enAction.MoveNote:
                            // Vertical move only for note
                            // ---------------------------
                            if (SelectedEvent.Command == MPTKCommand.NoteOn)
                            {
                                //Debug.Log($"    Event param:  {currentEvent.mousePosition} {currentEvent.delta} lastDragYPosition:{DragPosition} CellWidth:{CellWidth}  Sectx.CellHeight:{Sectx.CellHeight} ");
                                //Debug.Log($"        Change MIDI event DragPosition:{DragPosition} CellHeight:{Sectx.CellHeight} CellQuarterWidth:{CellQuarterWidth} to {LastMousePosition} ");
                                SelectedEvent.Value = Mathf.Clamp(InitialValue - Convert.ToInt32(DragPosition.y / Context.CellHeight), 0, 127);
                                channelMidi.LayoutNote.SetLowerHigherNote(SelectedEvent.Value);
                            }
                            // Horizontal
                            // ----------
                            deltaTick = Convert.ToInt32(DragPosition.x / Context.QuarterWidth * MidiFileWriter.MPTK_DeltaTicksPerQuarterNote);
                            SelectedEvent.Tick = CalculateQuantization(InitialTick + deltaTick);
                            Context.Modified = true;

                            if (SelectedEvent.Tick < 0)
                                SelectedEvent.Tick = 0;

                            if (SelectedEvent.Tick != InitialTick)
                            {
                                // Not used by the Midi synth, will be calculate if reloaded but we need it if position is displayed in second
                                SelectedEvent.RealTime = ConvertTickToDuration(SelectedEvent.Tick);

                                MidiFileWriter.MPTK_StableSortEvents();
                                FindLastMidiEvents();
                                //channelMidi.Print();
                            }
                            ////float x = (float)(SelectedEvent.Tick + deltaTick) * CellWidth / (float)LoadedMidi.MPTK_DeltaTicksPerQuarterNote ;
                            //float cellX = (float)SelectedEvent.Tick / (float)LoadedMidi.MPTK_DeltaTicksPerQuarterNote * CellQuarterWidth;
                            //float x = cellX;
                            //separatorDragVerticalBegin = new Rect(
                            //      x, channelMidi.startNotesZone,
                            //       1, channelMidi.startNotesZone + channelMidi.heightChannelZone);

                            if (SelectedEvent.Value != InitialValue || SelectedEvent.Tick != InitialTick)
                            {
                                ApplyEventToCmdMidi(SelectedEvent);
                                Repaint();
                            }
                            break;
                    }
                }
            }
        }


        private void EventMouseMove(Event currentEvent)
        {
            CurrentMouseCursor = MouseCursor.Arrow;
            FindCellMouse(currentEvent);
            if (CurrentMouseCursor == MouseCursor.Arrow)
                MouseAction = enAction.None;

            // ------------------------ TU search
            //float xMouseCorrected = currentEvent.mousePosition.x + ScrollerMidiEvents.x - X_CORR_SECTION;
            //long tick_raw = sectionAll.ConvertPositionToTick(xMouseCorrected);
            //long tick_quantized;
            //tick_quantized = CalculateQuantization(tick_raw);

            // FOR TESTING
            //int index = MidiLoad.MPTK_SearchEventFromTick(MidiEvents, tick_quantized);
            //if (index >= 0)
            //    Debug.Log($"Find at position {index} for tick quantized {tick_quantized} raw:{tick_raw} {MidiEvents[index]}");
            //else
            //    Debug.Log($"Not Find at position {index} for tick quantized {tick_quantized} raw:{tick_raw} ");

            // -------------------------- 

            LastMouseMove = currentEvent;
            Repaint();
        }

        private void EventKeyDown(Event currentEvent)
        {
            //Debug.Log("Ev.KeyDown: " + e);
            if (currentEvent.keyCode == KeyCode.Space || currentEvent.keyCode == KeyCode.DownArrow || currentEvent.keyCode == KeyCode.UpArrow ||
                currentEvent.keyCode == KeyCode.End || currentEvent.keyCode == KeyCode.Home)
            {
                GUI.changed = true;
                Repaint();
            }
            if (currentEvent.keyCode == KeyCode.Delete)
            {
                if (SelectedEvent != null)
                {
                    DeleteEventFromMidiFileWriter(SelectedEvent);
                    SelectedEvent = null;
                }
            }

            if (currentEvent.keyCode == KeyCode.KeypadEnter || currentEvent.keyCode == KeyCode.Return)
            {
                if (SelectedEvent != null)
                {
                    ApplyMidiChangeFromPad();
                }
                else
                    CreateEventFromPad();
            }

            if (currentEvent.keyCode == KeyCode.Escape)
                SelectedEvent = null;

            if (currentEvent.keyCode == KeyCode.Z)
            {
                DebugDisplayCell = !DebugDisplayCell;
                Repaint();
                Debug.Log($"Debug MainZone {DebugDisplayCell}");
            }

            if (currentEvent.keyCode == KeyCode.H)
            {
                Debug.Log("--------------------------------------");
                Debug.Log($"Loaded skin {GUI.skin.name}");
                Debug.Log($"QuarterWidth:{Context.QuarterWidth} MeasureWidth:{sectionAll.MeasureWidth} CellHeight:{Context.CellHeight} ");
                Debug.Log($"startXEventsList:{startXEventsList} widthVisibleEventsList: {widthVisibleEventsList}");
                Debug.Log($"startYEventsList:{startYEventsList} heightVisibleEventsList:{heightVisibleEventsList}");
                Debug.Log($"FullWidthChannelZone:{Section.FullWidthSections} FullHeightChannelsZone:{Section.FullHeightSections}");
                if (LastMidiEvent != null) Debug.Log($"LastMidiEvent:\t\t{LastMidiEvent}");
                if (LastNoteOnEvent != null) Debug.Log($"LastNoteOnEvent:\t{LastNoteOnEvent}");
                if (LastMouseMove != null) Debug.Log($"LastMouseMove:\t{LastMouseMove}");
                if (SelectedEvent != null) Debug.Log($"SelectedEvent:\t{SelectedEvent}");
                if (NewDragEvent != null) Debug.Log($"NewDragEvent:\t{NewDragEvent}");
                DebugMainAreaAndSection();
            }
        }

        private void EventWheel(Event currentEvent)
        {
            // change cell width only if no alt
            if (!currentEvent.alt)
            {
                Context.QuarterWidth -= currentEvent.delta.y / 2.5f;
                // Clamp and keep only one decimal
                Context.QuarterWidth = Mathf.Round(Mathf.Clamp(Context.QuarterWidth, 2f, 200f) * 10f) / 10f;
            }
            // change cell heightFirstRowCmd only if no shift
            if (!currentEvent.shift)
            {
                Context.CellHeight -= currentEvent.delta.y / 5f;
                // Clamp and keep only one decimal
                Context.CellHeight = Mathf.Round(Mathf.Clamp(Context.CellHeight, 5f, 40f) * 10f) / 10f;
            }
            currentEvent.Use();
            Repaint();
        }

        private MPTKEvent PlayKeyboard(Event currentEvent)
        {
            MPTKEvent playMPTK = null;
            if (MainArea.Position.Contains(currentEvent.mousePosition))
            {
                //Debug.Log($"MainZone");
                // Black keys are on top of white key, so check only white key
                if (MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position.Contains(currentEvent.mousePosition))
                {
                    //Debug.Log($"    KeyZone  {currentEvent.mousePosition} --> {MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position}");
                    AreaUI foundZone = null;
                    foreach (AreaUI zone in MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys].SubArea)
                    {
                        if (zone.Position.Contains(currentEvent.mousePosition))
                        {
                            //Debug.Log($"    Black Key  {currentEvent.mousePosition} --> {zone.Position} {zone.Value}");
                            foundZone = zone;
                            break;
                        }
                    }
                    if (foundZone == null)
                        foreach (AreaUI zone in MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].SubArea)
                        {
                            if (zone.Position.Contains(currentEvent.mousePosition))
                            {
                                //Debug.Log($"    White Key  {currentEvent.mousePosition} --> {zone.Position} {zone.Value}");
                                foundZone = zone;
                                break;
                            }
                        }
                    if (foundZone != null)
                    {
                        // Play the not found on the keyboard
                        playMPTK = new MPTKEvent() { Command = MPTKCommand.NoteOn, Channel = foundZone.Channel, Value = foundZone.Value, Duration = 10000 };
                        Player.MPTK_PlayDirectEvent(playMPTK);
                    }
                }
            }
            return playMPTK;
        }

        /// <summary>
        /// TBD taking in account BPM change ... 
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        private long ConvertTickToDuration(long tick)
        {
            return (long)(tick * MidiFileWriter.MPTK_PulseLenght + 0.5f);
        }

        // To avoid realloc at each iteration
        //Rect restrictZone = new Rect();
        private MPTKEvent FindCellMouse(Event currentEvent)
        {
            MPTKEvent foundMPTKEvent = null;
            if (MainArea.Position.Contains(currentEvent.mousePosition))
            {
                AreaUI channelsZone = MainArea.SubArea[(int)AreaUI.AreaType.Channels];
                if (channelsZone.Position.Contains(currentEvent.mousePosition))
                {
                    // foreach area as a full channel (preset + note + ...)
                    foreach (AreaUI channelZone in channelsZone.SubArea)
                    {
                        // is the mouse over a channel zone? 
                        if (channelZone.Position.Contains(currentEvent.mousePosition))
                        {
                            // Foreach subarea: note, preset, ... A SubArea contains all MIDI events visible for this channel
                            foreach (AreaUI cellZone in channelZone.SubArea)
                            {
                                // is the mouse over a cell position?  
                                if (cellZone.Position.Contains(currentEvent.mousePosition))
                                {
                                    // Yes! there is an event under the mouse
                                    //Debug.Log($"Find MIDIEvent {currentEvent.mousePosition} --> {cellZone.Position} {cellZone.Position.x + cellZone.Position.width} {cellZone.midiEvent}");
                                    foundMPTKEvent = cellZone.midiEvent;
                                    if (currentEvent.alt)
                                    {
                                        MouseAction = enAction.DeleteNote;
                                        CurrentMouseCursor = MouseCursor.ArrowMinus;
                                    }
                                    // https://docs.unity3d.com/ScriptReference/MouseCursor.html
                                    // Only note on can be resized
                                    else
                                    {
                                        // We need 8px on each side to display the action change length and 8 px inside for the move action.
                                        // Also if size <=24 we will only be able to move the cell
                                        // unless the shift key is pressed, the action will be forced to sizing by right.
                                        if (cellZone.Position.width >= 24 && currentEvent.mousePosition.x < cellZone.Position.x + 8f && foundMPTKEvent.Command == MPTKCommand.NoteOn)
                                        {
                                            MouseAction = enAction.LengthLeftNote;
                                            CurrentMouseCursor = MouseCursor.ResizeHorizontal;
                                        }
                                        // Only note-on can be resized, force a right sizing if shift key
                                        else if (currentEvent.shift || cellZone.Position.width >= 24 && currentEvent.mousePosition.x >= cellZone.Position.x + cellZone.Position.width - 8f && foundMPTKEvent.Command == MPTKCommand.NoteOn)
                                        {
                                            MouseAction = enAction.LengthRightNote;
                                            CurrentMouseCursor = MouseCursor.ResizeHorizontal;
                                        }
                                        else
                                        {
                                            MouseAction = enAction.MoveNote;
                                            CurrentMouseCursor = MouseCursor.Pan;
                                        }
                                    }
                                    // Found a MIDI event 
                                    //Debug.Log($"Find MIDIEvent - mousePosition:{currentEvent.mousePosition} --> cellZone:{cellZone.Position} {MouseAction} MidiEvent:{foundMPTKEvent ?? foundMPTKEvent}");
                                    break;
                                }
                            }
                            if (foundMPTKEvent == null && currentEvent.mousePosition.y > channelZone.Position.y + HEIGHT_CHANNEL_BANNER)
                            {
                                //Debug.Log($"{currentEvent.mousePosition.y} {(currentEvent.mousePosition.y - (channelZone.Position.y + HEIGHT_CHANNEL_BANNER)) % sectionAll.CellHeight}");
                                // Vary between 0 and CellHeight
                                float posRelativ = (currentEvent.mousePosition.y - (channelZone.Position.y + HEIGHT_CHANNEL_BANNER)) % Context.CellHeight;
                                float trigger = Context.CellHeight / 4f;
                                if (posRelativ > trigger && posRelativ < Context.CellHeight - trigger)
                                {
                                    CurrentMouseCursor = MouseCursor.ArrowPlus;
                                    MouseAction = enAction.CreateNote;
                                }
                            }
                        }
                        if (foundMPTKEvent != null) break;
                    }
                }
            }
            return foundMPTKEvent;
        }



        // Level of quantization : 
        // 0 = None --> 0
        // 1 = whole  --> dtpqn * 4
        // 2 = half --> dtpqn * 2
        // 3 = Quarter Note --> dtpqn * 1
        // 4 = Eighth Note --> dtpqn * 0.5
        // 5 = 16th Note
        // 6 = 32th Note
        // 7 = 64th Note
        // 8 = 128th Note

        private void CalculRatioQuantization()
        {
            if (MidiFileWriter != null)
            {
                if (Context.IndexQuantization == 0) // none
                    TickQuantization = 0;
                else if (Context.IndexQuantization == 1) // whole
                    TickQuantization = MidiFileWriter.MPTK_DeltaTicksPerQuarterNote * 4;
                else if (Context.IndexQuantization == 2) // half
                    TickQuantization = MidiFileWriter.MPTK_DeltaTicksPerQuarterNote * 2;
                else // quarter and bellow
                    TickQuantization = MidiFileWriter.MPTK_DeltaTicksPerQuarterNote / (1 << (Context.IndexQuantization - 3)); // division par puissance de 2

                //Debug.Log($"IndexQuantization:{Context.IndexQuantization} DeltaTicksPerQuarterNote:{MidiFileWriter.MPTK_DeltaTicksPerQuarterNote} Quantization:{TickQuantization}");
            }
        }



        private void ApplyEventToCmdMidi(MPTKEvent mptkEvent)
        {
            //Debug.Log($"ApplyMidiEventToCurrent {mptkEvent}");
            CurrentChannel = mptkEvent.Channel;
            PopupSelectMidiChannel.SelectedIndex = CurrentChannel;

            CurrentTick = mptkEvent.Tick;

            if (mptkEvent.Command == MPTKCommand.NoteOn)
            {

                CurrentCommand = 0;
                PopupSelectMidiCommand.SelectedIndex = CurrentCommand;

                CurrentNote = mptkEvent.Value;
                CurrentVelocity = mptkEvent.Velocity;
                CurrentDuration = (int)mptkEvent.Length;

            }
            else if (mptkEvent.Command == MPTKCommand.PatchChange)
            {

                CurrentCommand = 1;
                PopupSelectMidiCommand.SelectedIndex = CurrentCommand;

                CurrentPreset = mptkEvent.Value;
                PopupSelectPreset.SelectedIndex = CurrentPreset;

            }
        }

        private MPTKEvent ApplyQuantization(MPTKEvent mEvent, bool toLowerValue = false)
        {
            mEvent.Tick = CalculateQuantization(mEvent.Tick, toLowerValue);
            return mEvent;
        }

        private long CalculateQuantization(long tick, bool toLowerValue = false)
        {
            long result;
            if (TickQuantization != 0)
            {
                float round = toLowerValue ? 0f : 0.5f;
                result = (long)((tick / (float)TickQuantization) + round) * TickQuantization;
                //Debug.Log($"tick:{tick} TickQuantization:{TickQuantization} ratio:{tick / (float)TickQuantization} result:{result}");
            }
            else
                result = tick;
            return TickQuantization != 0 ? result : tick;
        }
        //private void AddCell(MPTKEvent midiEvent, MPTKGui.StyleItem item, string text, GUIStyle styleRow = null)
        //{
        //    if (!item.Hidden)
        //    {
        //        GUIStyle style = styleRow == null ? item.Style : styleRow;
        //        if (item.Offset != 0) style.contentOffset = new Vector2(item.Offset, 0);
        //        GUILayout.Label(text, style, GUILayout.Width(item.Width));
        //        if (item.Offset != 0) style.contentOffset = Vector2.zero;

        //        // User select a line ?
        //        if (Event.current.type == EventType.MouseDown)
        //            if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
        //            {
        //                //Debug.Log($"{midiEvent.Index} XXX {window.position.x + GUILayoutUtility.GetLastRect().x}");
        //                //SelectedEvent = midiEvent.Index;
        //                //MidiPlayerEditor.MidiPlayer.MPTK_TickCurrent = midiEvent.Tick;
        //                window.Repaint();
        //            }
        //    }
        //}

        private void TestLenStyle()
        {
            string test = "";
            for (int i = 1; i < 20; i++)
            {
                test += "0";
                float len = TimelineStyle.CalcSize(new GUIContent(test)).x;
                Debug.Log($"{i,-2} {len} {len / (float)i:F2} {test}");
            }
        }

        //private void DisplayPerf(string title = null, bool restart = true)
        //{
        //    //StackFrame sf = new System.Diagnostics.StackTrace().GetFrame(1);
        //    if (title != null)
        //        Debug.Log($"{title,-20} {((double)watchPerf.ElapsedTicks) / ((double)System.Diagnostics.Stopwatch.Frequency / 1000d):F2} ms ");
        //    if (restart)
        //        watchPerf.Restart();
        //}

        private void DebugAreaUI()
        {
            //Debug.Log($"White Keys:{MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].SubArea.Count} Black Keys:{MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys].SubArea.Count}");

            GUI.Label(MainArea.Position, "Mainzone", TimelineStyle);
            DrawRect(MainArea.Position, Color.yellow, 4);
            DrawRect(MainArea.SubArea[(int)AreaUI.AreaType.Channels].Position, Color.blue, 3);
            MainArea.SubArea[(int)AreaUI.AreaType.Channels].SubArea.ForEach(zone =>
            {
                DrawRect(zone.Position, Color.red, 2);
                zone.SubArea.ForEach(zoneCell =>
                {
                    DrawRect(zoneCell.Position, Color.green, 1);
                });
            });

            DrawRect(MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position, Color.black, 3);
            DrawRect(MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys].Position, Color.black, 3);
            MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].SubArea.ForEach(zone => DrawRect(zone.Position, Color.green, 1));
            MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys].SubArea.ForEach(zone => DrawRect(zone.Position, Color.red, 1));
        }
        private void DebugMainAreaAndSection()
        {
            Debug.Log("--------------- MainArea -------------------");
            DebugSubArea(MainArea);
            DebugSubArea(MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys]);
            DebugSubArea(MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys]);
            DebugSubArea(MainArea.SubArea[(int)AreaUI.AreaType.Channels]);

            Debug.Log("--------------- SectDim -------------------");
            for (int channel = 0; channel < sectionAll.Sections.Length; channel++)
            {
                // Ambitus + row for preset
                if (sectionAll.Sections[channel] != null)
                    Debug.Log(sectionAll.Sections[channel].ToString());
            }
        }

        private static void DebugSubArea(AreaUI area)
        {
            Debug.Log("\t" + area.ToString());
            if (area.SubArea != null)
                area.SubArea.ForEach(zone =>
                {
                    Debug.Log("\t\t" + zone.ToString());
                    if (zone.SubArea != null) zone.SubArea.ForEach(zoneCell => { Debug.Log("\t\t\t" + zoneCell.ToString()); });
                });
        }
        private void DebugListEvent()
        {
            MidiEvents.ForEach(e => { Debug.Log(e); });
        }
    }
}