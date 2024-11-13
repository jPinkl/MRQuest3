//using System;
//using System.Collections;
//using System.Collections.Generic;
using Oculus.Haptics;
//using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using MidiPlayerTK;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit; //dont need since ill be using a different 



public class HapticsManager : MonoBehaviour
{
    
    //code based on slash grabbed from this tutorial:
    //https://www.youtube.com/watch?v=oafaIzdrj_Y

    public static HapticsManager Instance;
    [SerializeField] private HapticClip note;
    [SerializeField] private HapticClip accentedNote;

    private HapticClipPlayer _player;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != null)
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
        _player = new HapticClipPlayer(note);
    }

    public void PlayNoteR()
    {
        _player.clip = note;
        _player.Play(Controller.Right);
    }

    public void PlayNoteL()
    {
        _player.clip = note;
        _player.Play(Controller.Left);
    }

    public void PlayAccentNoteR()
    {
        _player.clip = accentedNote;
        _player.Play(Controller.Right);
    }

    public void PlayAccentNoteL()
    {
        _player.clip = accentedNote;
        _player.Play(Controller.Left);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayAccentNoteL();
            PlayAccentNoteR();
            Debug.Log("whyyyyyyy");

        }
    }
}
