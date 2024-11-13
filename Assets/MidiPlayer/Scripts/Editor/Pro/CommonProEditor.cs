#define MPTK_PRO
using UnityEngine;
using UnityEditor;

using System;

using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace MidiPlayerTK
{
    /// <summary>@brief
    /// Inspector for the midi global player component
    /// </summary>
    public class CommonProEditor : ScriptableObject
    {
        public static void EffectSoundFontParameters(MidiSynth instance, CustomStyle myStyle)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("These effects will be applied independently on each voices. Effects values are defined in the SoundFont, weird sound can occurs when changing these settings.", myStyle.LabelGreen);

            instance.MPTK_ApplySFFilter = EditorGUILayout.Toggle(new GUIContent("Apply Low Pass Filter", "Low pass filter is defined in each preset of the SoudFont. Uncheck to gain some % CPU on weak device."), instance.MPTK_ApplySFFilter);
            {
                EditorGUI.indentLevel++;
                GUI.enabled = instance.MPTK_ApplySFFilter;
                instance.MPTK_SFFilterFreqOffset = EditorGUILayout.Slider(new GUIContent("Offset Cutoff Frequence", "Offset to the cutoff frequency (Low Pass) defined in the SoundFont."), instance.MPTK_SFFilterFreqOffset, -2000f, 3000f);
                instance.MPTK_SFFilterQModOffset = EditorGUILayout.Slider(new GUIContent("Offset Quality ", "Offset on the SF resonance peak defined in the SoundFont."), instance.MPTK_SFFilterQModOffset, -96f, 96f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("SoundFont Filter", GUILayout.Width(150), GUILayout.Height(15));
                if (GUILayout.Button(new GUIContent("Set Default", ""), GUILayout.Width(100), GUILayout.Height(15)))
                    instance.MPTK_SFFilterSetDefault();
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }

            instance.MPTK_ApplySFReverb = EditorGUILayout.Toggle(new GUIContent("Apply Reverb", ""), instance.MPTK_ApplySFReverb);
            {
                EditorGUI.indentLevel++;
#if MPTK_PRO && UNITY_ANDROID && UNITY_OBOE
                EditorGUILayout.LabelField("Applying this effect with Oboe can cause weird sounds or even crash some devices", myStyle.LabelAlert);
#endif
                GUI.enabled = instance.MPTK_ApplySFReverb;
                instance.MPTK_SFReverbAmplify = EditorGUILayout.Slider(new GUIContent("Amplify", ""), instance.MPTK_SFReverbAmplify, -1f, 1f);
                instance.MPTK_SFReverbLevel = EditorGUILayout.Slider(new GUIContent("Level", ""), instance.MPTK_SFReverbLevel, 0f, 1f);
                instance.MPTK_SFReverbRoomSize = EditorGUILayout.Slider(new GUIContent("Room Size", "Controls concave reverb time between 0 (0.7 second) and 1 (12.5 second)"), instance.MPTK_SFReverbRoomSize, 0f, 1f);
                instance.MPTK_SFReverbDamp = EditorGUILayout.Slider(new GUIContent("Damp", "Controls the reverb time frequency dependency."), instance.MPTK_SFReverbDamp, 0f, 1f);
                instance.MPTK_SFReverbWidth = EditorGUILayout.Slider(new GUIContent("Width", "Controls the left/right output separation."), instance.MPTK_SFReverbWidth, 0f, 100f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("SoundFont Reverb", GUILayout.Width(150), GUILayout.Height(15));
                if (GUILayout.Button(new GUIContent("Set Default", ""), GUILayout.Width(100), GUILayout.Height(15)))
                    instance.MPTK_SFReverbSetDefault();
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }


            instance.MPTK_ApplySFChorus = EditorGUILayout.Toggle(new GUIContent("Apply Chorus", ""), instance.MPTK_ApplySFChorus);
            {
                EditorGUI.indentLevel++;
#if MPTK_PRO && UNITY_ANDROID && UNITY_OBOE
                EditorGUILayout.LabelField("Applying this effect with Oboe can cause weird sounds or even crash some devices", myStyle.LabelAlert);
#endif
                GUI.enabled = instance.MPTK_ApplySFChorus;
                instance.MPTK_SFChorusAmplify = EditorGUILayout.Slider(new GUIContent("Amplify", ""), instance.MPTK_SFChorusAmplify, -1f, 1f);
                instance.MPTK_SFChorusLevel = EditorGUILayout.Slider(new GUIContent("Level", ""), instance.MPTK_SFChorusLevel, 0f, 10f);
                instance.MPTK_SFChorusSpeed = EditorGUILayout.Slider(new GUIContent("Speed", "Chorus speed in Hz"), instance.MPTK_SFChorusSpeed, 0.1f, 5f);
                instance.MPTK_SFChorusDepth = EditorGUILayout.Slider(new GUIContent("Depth", "Chorus Depth"), instance.MPTK_SFChorusDepth, 0f, 256f);
                instance.MPTK_SFChorusWidth = EditorGUILayout.Slider(new GUIContent("Width", "Allows to get a gradually stereo effect from minimum (monophonic) to maximum stereo effect"), instance.MPTK_SFChorusWidth, 0f, 10f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("SoundFont Chorus", GUILayout.Width(150), GUILayout.Height(15));
                if (GUILayout.Button(new GUIContent("Set Default", ""), GUILayout.Width(100), GUILayout.Height(15)))
                    instance.MPTK_SFChorusSetDefault();
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        public static void EffectUnityParameters(MidiSynth instance, CustomStyle myStyle)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("These effects will be applied to all voices processed by the current MPTK gameObject. You can add multiple MPTK gameObjects to apply for different effects.", myStyle.LabelGreen);
#if MPTK_PRO && UNITY_ANDROID && UNITY_OBOE
            EditorGUILayout.LabelField("These effects are not available with Oboe", myStyle.LabelAlert);
            instance.MPTK_ApplyUnityReverb = false;
            instance.MPTK_ApplyUnityChorus = false;
#endif

            instance.MPTK_ApplyUnityReverb = EditorGUILayout.Toggle(new GUIContent("Apply Reverb", ""), instance.MPTK_ApplyUnityReverb);
            {
                EditorGUI.indentLevel++;
                GUI.enabled = instance.MPTK_ApplyUnityReverb;
                instance.MPTK_ReverbDryLevel = EditorGUILayout.Slider(new GUIContent("Dry Level", "Mix level of dry signal in output"), instance.MPTK_ReverbDryLevel, 0, 1f);
                instance.MPTK_ReverbRoom = EditorGUILayout.Slider(new GUIContent("Room Size", "Room effect level at low frequencies"), instance.MPTK_ReverbRoom, 0f, 1f);
                instance.MPTK_ReverbRoomHF = EditorGUILayout.Slider(new GUIContent("Room Size HF", "Room effect high-frequency level"), instance.MPTK_ReverbRoomHF, 0f, 1f);
                instance.MPTK_ReverbRoomLF = EditorGUILayout.Slider(new GUIContent("Room Size LF", "Room effect low-frequency level"), instance.MPTK_ReverbRoomLF, 0f, 1f);
                instance.MPTK_ReverbDecayTime = EditorGUILayout.Slider(new GUIContent("Decay Time", "Reverberation decay time at low-frequencies in seconds"), instance.MPTK_ReverbDecayTime, 0.1f, 20f);
                instance.MPTK_ReverbDecayHFRatio = EditorGUILayout.Slider(new GUIContent("Decay Ratio", "Decay HF Ratio : High-frequency to low-frequency decay time ratio"), instance.MPTK_ReverbDecayHFRatio, 0.1f, 2f);
                instance.MPTK_ReverbReflectionLevel = EditorGUILayout.Slider(new GUIContent("Early Reflection", "Early reflections level relative to room effect"), instance.MPTK_ReverbReflectionLevel, 0f, 1f);
                instance.MPTK_ReverbReflectionDelay = EditorGUILayout.Slider(new GUIContent("Late Reflection", "Late reverberation level relative to room effect"), instance.MPTK_ReverbReflectionDelay, 0f, 1f);
                instance.MPTK_ReverbLevel = EditorGUILayout.Slider(new GUIContent("Reverb Level", "Late reverberation level relative to room effect"), instance.MPTK_ReverbLevel, 0f, 1f);
                instance.MPTK_ReverbDelay = EditorGUILayout.Slider(new GUIContent("Reverb Delay", "Late reverberation delay time relative to first reflection in seconds"), instance.MPTK_ReverbDelay, 0f, 0.1f);
                instance.MPTK_ReverbHFReference = EditorGUILayout.Slider(new GUIContent("HF Reference", "Reference high frequency in Hz"), instance.MPTK_ReverbHFReference, 1000f, 20000f);
                instance.MPTK_ReverbLFReference = EditorGUILayout.Slider(new GUIContent("LF Reference", "Reference low frequency in Hz"), instance.MPTK_ReverbLFReference, 20f, 1000f);
                instance.MPTK_ReverbDiffusion = EditorGUILayout.Slider(new GUIContent("Diffusion", "Reverberation diffusion (echo density) in percent"), instance.MPTK_ReverbDiffusion, 0f, 1f);
                instance.MPTK_ReverbDensity = EditorGUILayout.Slider(new GUIContent("Density", "Reverberation density (modal density) in percent"), instance.MPTK_ReverbDensity, 0f, 1f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Generic Reverb", GUILayout.Width(150), GUILayout.Height(15));
                if (GUILayout.Button(new GUIContent("Set Default", ""), GUILayout.Width(100), GUILayout.Height(15)))
                    instance.MPTK_ReverbSetDefault();
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }

            instance.MPTK_ApplyUnityChorus = EditorGUILayout.Toggle(new GUIContent("Apply Chorus", ""), instance.MPTK_ApplyUnityChorus);
            {
                EditorGUI.indentLevel++;
                GUI.enabled = instance.MPTK_ApplyUnityChorus;
                instance.MPTK_ChorusDryMix = EditorGUILayout.Slider(new GUIContent("Dry Mix", ""), instance.MPTK_ChorusDryMix, 0f, 1f);
                instance.MPTK_ChorusWetMix1 = EditorGUILayout.Slider(new GUIContent("Wet Mix 1", ""), instance.MPTK_ChorusWetMix1, 0f, 1f);
                instance.MPTK_ChorusWetMix2 = EditorGUILayout.Slider(new GUIContent("Wet Mix 2", ""), instance.MPTK_ChorusWetMix2, 0f, 1f);
                instance.MPTK_ChorusWetMix3 = EditorGUILayout.Slider(new GUIContent("Wet Mix 3", ""), instance.MPTK_ChorusWetMix3, 0f, 1f);
                instance.MPTK_ChorusDelay = EditorGUILayout.Slider(new GUIContent("Delay in ms.", ""), instance.MPTK_ChorusDelay, 0.1f, 100f);
                instance.MPTK_ChorusRate = EditorGUILayout.Slider(new GUIContent("Rate in Hz.", ""), instance.MPTK_ChorusRate, 0f, 20f);
                instance.MPTK_ChorusDepth = EditorGUILayout.Slider(new GUIContent("Modulation Depth", ""), instance.MPTK_ChorusDepth, 0f, 1f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Generic Chorus", GUILayout.Width(150), GUILayout.Height(15));
                if (GUILayout.Button(new GUIContent("Set Default", ""), GUILayout.Width(100), GUILayout.Height(15)))
                    instance.MPTK_ChorusSetDefault();
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }
    }
}

