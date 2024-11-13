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
    using Debug = UnityEngine.Debug;

    public partial class MidiEditorWindow : EditorWindow
    {
        #region const
        // https://en.wikipedia.org/wiki/List_of_musical_symbols
        // CONST
        const int WIDTH_BUTTON_PLAYER = 100;
        const float WIDTH_KEYBOARD = 100f;
        const float AREA_BORDER_X = 5;
        const float X_CORR_SECTION = AREA_BORDER_X + WIDTH_KEYBOARD;

        const float AREA_BORDER_Y = 5;//7;
        const int HEIGHT_HEADER = 30;//60; // Title
        const int HEIGHT_PLAYER_CMD = 60 + 20; // MIDI Player
        const float HEIGHT_TIMELINE = 35;//28;//28; // MIDI time
        const float HEIGHT_CHANNEL_BANNER = 20;//20; // Chnnel Header
        const float Y_CORR_SECTION = AREA_BORDER_Y + HEIGHT_HEADER + HEIGHT_PLAYER_CMD + HEIGHT_TIMELINE;
        const float WIDTH_PAD_CHANNEL = 100f;
        const float WIDTH_PAD_MIDI_EVENT = 300f;
        #endregion


        #region popup
        static MPTKGui.PopupList PopupSelectPreset;
        static MPTKGui.PopupList PopupSelectDisplayTime;
        static MPTKGui.PopupList PopupSelectQuantization;
        static MPTKGui.PopupList PopupSelectMidiCommand;
        static MPTKGui.PopupList PopupSelectMidiChannel;
        static MPTKGui.PopupList PopupSelectLoadMenu;
        static MPTKGui.PopupList PopupSelectSaveMenu;

        static List<MPTKGui.StyleItem> PopupItemsPreset;
        static List<MPTKGui.StyleItem> PopupItemsDisplayTime;
        static List<MPTKGui.StyleItem> PopupItemsQuantization;
        static List<MPTKGui.StyleItem> PopupItemsMidiCommand;
        static List<MPTKGui.StyleItem> PopupItemsMidiChannel;
        static List<MPTKGui.StyleItem> PopupItemsLoadMenu;
        static List<MPTKGui.StyleItem> PopupItemsSaveMenu;
        static public PopupInfoSynth winPopupSynth;
        #endregion


        #region ui
        int borderSize1 = 1; // Border size in pixels
        int borderSize2 = 2; // Border size in pixels
        int borderSize3 = 3; // Border size in pixels

        static RectOffset SepBorder0;
        static RectOffset SepBorder1;
        static RectOffset SepBorder2;
        static RectOffset SepBorder3;
        static Texture SepChannelTexture;
        static Texture SepNoteTexture;
        static Texture SepPresetTexture;
        static Texture SepBarText;
        static Texture SepQuarterText;
        static Texture SepDragEventText;
        static Texture SepDragMouseText;
        static Texture SepPositionTexture;
        static Texture SepLoopTexture;
        static Rect separatorDragVerticalBegin;
        static Rect rectPositionSequencer = new Rect(0, 0, 2, 0);
        static Rect rectPositionLoopStart = new Rect(0, 0, 2, 0);
        static Rect rectPositionLoopEnd = new Rect(0, 0, 2, 0);

        static GUIStyle TimelineStyle;
        static GUIStyle PresetButtonStyle;
        static Texture MidiNoteTexture;
        static Texture MidiSelectedTexture;
        static Texture MidiPresetTexture;
        static Texture MidiSelected;
        static GUIStyle ChannelBannerStyle;
        static GUIStyle BackgroundMidiEvents;
        static GUIStyle BackgroundMidiEvents1;
        static GUIStyle WhiteKeyLabelStyle;
        static GUIStyle BlackKeyLabelStyle;
        static Texture WhiteKeyDrawTexture;
        static Texture BlackKeyDrawTexture;

        float HeightScrollHori;
        float WidthScrollVert;

        #endregion

        #region variable

        static AreaUI MainArea;
        static SectionAll sectionAll;

        static MPTKEvent LastMidiEvent;
        static MPTKEvent LastNoteOnEvent;

        static MidiFileWriter2 MidiFileWriter;
        static ContextEditor Context;
        static List<MPTKEvent> MidiEvents;
        
        static long TickQuantization;

        static public Vector2 ScrollerMidiEvents;
        static private MidiEditorLib MidiPlayerSequencer;

        static int CurrentChannel;
        //static int CurrentTrack;
        static int CurrentCommand;
        static long CurrentTick;
        static int CurrentNote = 60;
        static int CurrentDuration;
        static int CurrentVelocity = 100;
        static int CurrentPreset;
        static int CurrentBPM = 120;
        static string CurrentText = "";

        static long lastTickForUpdate = -1;
        static DateTime lastTimeForUpdate;

        //static string InvalidFileChars = new string(Path.GetInvalidFileNameChars());
        static char[] InvalidFileChars = Path.GetInvalidFileNameChars();

        static float startXEventsList;
        static float startYEventsList;
        static float widthVisibleEventsList; // with of the area displayed on the screen
        static float heightVisibleEventsList; // height of the area displayed on the screen

        static MPTKEvent LastEventPlayed = null;
        static long CurrentTickPosition = 0;
        static int PositionSequencerPix = 0;
        static long InitialTick = 0;
        static int InitialDurationTick = 0;
        static int InitialValue = 0;

        static MPTKEvent KeyPlaying;
        static Event LastMouseMove;
        static MPTKEvent SelectedEvent = null;
        static MPTKEvent NewDragEvent = null;
        static Vector2 DragPosition;
        enAction MouseAction = enAction.None;
        enum enAction { None, MoveNote, LengthLeftNote, LengthRightNote, CreateNote, DeleteNote }

        static Vector2 LastMousePosition;

        static MouseCursor CurrentMouseCursor = MouseCursor.Arrow;

        static bool DebugDisplayCell;

        static GUIContent LabelLoop = new GUIContent(MPTKGui.IconLoop, "Activate Loping");
        static GUIContent LabelLooping = new GUIContent(MPTKGui.IconLooping, "Disable Looping");
        static GUIContent LabelSetLoopStart = new GUIContent(MPTKGui.IconLoopStart, "updated by script");
        static GUIContent LabelSetLoopStop = new GUIContent(MPTKGui.IconLoopStop, "updated by script");
        static GUIContent LabelResetLoop = new GUIContent(MPTKGui.IconLoopReset, "updated by script");
        static GUIContent LabelModeLoop = new GUIContent(MPTKGui.IconModeLoop, "Change Loop Mode");

        static float heightFirstRowCmd = 22;

        private MidiFileEditorPlayer Player;

        //static private System.Diagnostics.Stopwatch watchPerf = new System.Diagnostics.Stopwatch();
        #endregion
    }
}