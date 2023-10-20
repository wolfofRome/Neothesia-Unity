﻿using System;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MidiPlayer : MonoBehaviour
{
	[Header("References")]
	public PianoKeyController PianoKeyDetector;
	public static bool playAlong, pass;
	public GameObject noteImage, speedDisplay, timeDisplay;

	[Header("Properties")]
	public float GlobalSpeed = 1;
	public RepeatType RepeatType;

	public KeyMode KeyMode;
	public bool ShowMIDIChannelColours;
	public Color[] MIDIChannelColours;

	[Header("Ensure Song Name is filled for builds")]
	public MidiSong[] MIDISongs;

	[HideInInspector]
	public MidiNote[] MidiNotes;
	public UnityEvent OnPlayTrack { get; set; }

	MidiFileInspector _midi;

	string _path;
	string[] _keyIndex;
	int _noteIndex = 0;
	int _midiIndex, sameLineNumber;
	public static int[] alongKeys;
	float _timer = 0, interval, imageInitY;
	[SerializeField, HideInInspector]
	bool _preset = false;
	Vector2 noteSize;


    void Start ()
	{
		pass = false;
		imageInitY = -300f + 1.92f * NoteFlow.originSpeed;
		OnPlayTrack = new UnityEvent();
		OnPlayTrack.AddListener(delegate{FindObjectOfType<MusicText>().StartSequence(MIDISongs[_midiIndex].Details);});
		
		_midiIndex = 0;

		if (!_preset)
			PlayCurrentMIDI();
		else
		{
#if UNITY_EDITOR
			_path = string.Format("{0}/MIDI/{1}.mid", Application.streamingAssetsPath, MIDISongs[0].MIDIFile.name);
#else
			_path = string.Format("{0}/MIDI/{1}.mid", Application.streamingAssetsPath, MIDISongs[0].SongFileName);
#endif
			_midi = new MidiFileInspector(_path);
			
			OnPlayTrack.Invoke();
		}
		interval = _midi.MidiFile.DeltaTicksPerQuarterNote/4f;
        noteSize = noteImage.GetComponent<RectTransform>().sizeDelta;
    }
	int CalcImageIndex(string note)
	{
		int index = 0;
		int num = note[note.Length - 1] - '0';
        if(num == 0)
            index = note[0] - 'A' + 1;
		else
		{
			index = 2 + (num - 2) * 7 + (note[0] - 'C' + 1);
			if (note[0] == 'A' || note[0] == 'B')
				index += 7;
        }
		return index;
	}
	void Update ()
	{
		if (MIDISongs.Length <= 0)
			enabled = false;
		
		if (_midi != null && MidiNotes.Length > 0 && _noteIndex < MidiNotes.Length)
		{
			_timer += Time.deltaTime * GlobalSpeed * (float)MidiNotes[_noteIndex].Tempo;
			while (_noteIndex < MidiNotes.Length && MidiNotes[_noteIndex].StartTime < _timer)
			{
				timeDisplay.GetComponent<Slider>().value = _timer / (float)MidiNotes[MidiNotes.Length - 1].StartTime;
				if (PianoKeyDetector.PianoNotes.ContainsKey(MidiNotes[_noteIndex].Note))
				{
					
                    GameObject g = Instantiate(noteImage, GameObject.Find("Canvas").transform) as GameObject;
                    //if (MidiNotes[0].Length <= 0.1f)
                    g.GetComponent<RectTransform>().localPosition = new Vector2(-950f + (CalcImageIndex(MidiNotes[_noteIndex].Note) - 1) * noteSize.x / 36f * 37.2f, Screen.height/2);
                    //else
                    //    g.GetComponent<RectTransform>().localPosition = new Vector2(-950f + (CalcImageIndex(MidiNotes[_noteIndex].Note) - 1) * noteSize.x / 36f * 37.2f, 1080f + (float)MidiNotes[_noteIndex].StartTime / interval * noteSize.y * (int)(MidiNotes[0].Length * 100f) / 10f * 1.1f);
                    if (MidiNotes[_noteIndex].Note.Length == 3)
                    {
                        Vector2 sizeDelta = g.GetComponent<RectTransform>().sizeDelta;
                        g.GetComponent<RectTransform>().sizeDelta = new Vector2(sizeDelta.x / 2f, sizeDelta.y);
                        g.GetComponent<RectTransform>().localPosition = new Vector2(g.GetComponent<RectTransform>().localPosition.x + noteSize.x / 36f * 18.6f, g.GetComponent<RectTransform>().localPosition.y);
                    }
                    if (MidiNotes[_noteIndex].Length > 0.1f)
                    {
                        Vector2 sizeDelta = g.GetComponent<RectTransform>().sizeDelta;
                        g.GetComponent<RectTransform>().sizeDelta = new Vector2(sizeDelta.x, (int)(MidiNotes[_noteIndex].Length * sizeDelta.y * 100f) / 10f);
                    }
                    if (MidiNotes[_noteIndex].Channel == 1)
                    {
                        g.GetComponent<Image>().color = new Color(183f / 255f, 65f / 255f, 139f / 255f);
                        if (MidiNotes[_noteIndex].Note.Length == 3)
                        {
                            g.GetComponent<Image>().color = new Color(157f / 255f, 17f / 255f, 104f / 255f);
                        }
                    }
                    else
                    {
                        g.GetComponent<Image>().color = new Color(90f / 255f, 165f / 255f, 234f / 255f);
                        if (MidiNotes[_noteIndex].Note.Length == 3)
                        {
                            g.GetComponent<Image>().color = new Color(1f / 255f, 133f / 255f, 255f / 255f);
                        }
                    }
					g.name = MidiNotes[_noteIndex].Length + "+" + MidiNotes[_noteIndex].StartTime + "+" + MidiNotes[_noteIndex].Note;
					
					StartCoroutine(WaitAndPlay(1.4f, _noteIndex));

				}

				_noteIndex++;
			}
		}
		else
		{
			SetupNextMIDI();
		}
		if (Input.GetKeyUp(KeyCode.N))
			SetupNextMIDI();
		else if (Input.GetKeyUp(KeyCode.DownArrow))
		{
			if(GlobalSpeed > 0.1f)
				GlobalSpeed -= 0.1f;
			NoteFlow.flowSpeed = NoteFlow.originSpeed * GlobalSpeed;
			DisplaySpeed();
        }
		else if (Input.GetKeyUp(KeyCode.UpArrow))
		{
            GlobalSpeed += 0.1f;
            NoteFlow.flowSpeed = NoteFlow.originSpeed * GlobalSpeed;
            DisplaySpeed();
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
			if (Time.timeScale > 0)
				Time.timeScale = 0f;
			else
				Time.timeScale = 1f;
            DisplaySpeed();
        }
		else if (Input.GetKeyUp(KeyCode.A))
		{
			pass = true;
		}
		else if (Input.GetKeyUp(KeyCode.Escape))
			SceneManager.LoadScene("Main");
		if(pass)
		{
			Time.timeScale = 1f;
			pass = false;
		}
	}

	IEnumerator WaitAndPlay(float t, int _index)
	{
		yield return new WaitForSeconds(t);
		//try
		{
            if (PianoKeyDetector.PianoNotes.ContainsKey(MidiNotes[_index].Note))
            {
                if (_index == 0)
                    sameLineNumber = 1;
                else if (_index > 0 && MidiNotes[_index].StartTime == MidiNotes[_index - 1].StartTime)
				{
                    sameLineNumber++;
					
                }
                else
                    sameLineNumber = 1;
                if (_index < MidiNotes.Length - 1 && MidiNotes[_index].StartTime != MidiNotes[_index + 1].StartTime)
				{
					alongKeys = new int[sameLineNumber];
					for(int i = sameLineNumber-1; i >= 0; i--)
					{
                        alongKeys[i] = PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_index - sameLineNumber + i + 1].Note);
                        print(alongKeys[i]);
                    }
				}

                if (ShowMIDIChannelColours)
                {
                    PianoKeyDetector.PianoNotes[MidiNotes[_index].Note].Play(MIDIChannelColours[MidiNotes[_index].Channel],
                                                                            MidiNotes[_index].Velocity,
                                                                            MidiNotes[_index].Length,
                                                                            PianoKeyDetector.MidiPlayer.GlobalSpeed * MIDISongs[_midiIndex].Speed);
                }
                else
                    PianoKeyDetector.PianoNotes[MidiNotes[_index].Note].Play(MidiNotes[_index].Velocity,
                                                                            MidiNotes[_index].Length,
                                                                            PianoKeyDetector.MidiPlayer.GlobalSpeed * MIDISongs[_midiIndex].Speed);
                if (playAlong && !pass)
                {
                    Time.timeScale = 0f;
                }
            }
        }
		//catch(Exception ex)
		//{
		//	Debug.LogError(MidiNotes.Length);
		//}
    }

	void DisplaySpeed()
	{
		speedDisplay.SetActive(true);
        speedDisplay.GetComponent<Text>().text = "Speed: " + ((int)((GlobalSpeed+0.01f)*10f)/10f).ToString();
        StartCoroutine(HideText(0.5f));
	}
    IEnumerator HideText(float t)
	{
		yield return new WaitForSeconds(t);
        speedDisplay.SetActive(false);
    }


    void SetupNextMIDI()
	{
		if (_midiIndex >= MIDISongs.Length - 1)
		{
			if (RepeatType != RepeatType.NoRepeat)
				_midiIndex = 0;
			else
			{
				_midi = null;
				return;
			}
		}
		else
		{
			if (RepeatType != RepeatType.RepeatOne)
				_midiIndex++;
		}

		PlayCurrentMIDI();
	}

	void PlayCurrentMIDI()
	{
		_timer = 0;

#if UNITY_EDITOR
		_path = string.Format("{0}/MIDI/{1}.mid", Application.streamingAssetsPath, MIDISongs[_midiIndex].MIDIFile.name);
#else
		_path = string.Format("{0}/MIDI/{1}.mid", Application.streamingAssetsPath, MIDISongs[_midiIndex].SongFileName);
#endif
		_midi = new MidiFileInspector(_path);
		MidiNotes = _midi.GetNotes();
		_noteIndex = 0;

		OnPlayTrack.Invoke();
	}

	[ContextMenu("Preset MIDI")]
	void PresetFirstMIDI()
	{
#if UNITY_EDITOR
		_path = string.Format("{0}/MIDI/{1}.mid", Application.streamingAssetsPath, MIDISongs[0].MIDIFile.name);
#else
		_path = string.Format("{0}/MIDI/{1}.mid", Application.streamingAssetsPath, MIDISongs[0].SongFileName);
#endif
		_midi = new MidiFileInspector(_path);
		MidiNotes = _midi.GetNotes();
		
		_preset = true;
	}

	[ContextMenu("Clear MIDI")]
	void ClearPresetMIDI()
	{
		MidiNotes = new MidiNote[0];
		_preset = false;
	}

#if UNITY_EDITOR
	[ContextMenu("MIDI to name")]
	public void MIDIToPlaylist()
	{
		for (int i = 0; i < MIDISongs.Length; i++)
		{
			MIDISongs[i].SongFileName = MIDISongs[i].MIDIFile.name;
		}
	}
#endif
}

public enum RepeatType { NoRepeat, RepeatLoop, RepeatOne }
public enum KeyMode { Physical, ForShow }

[Serializable]
public class MidiSong
{
#if UNITY_EDITOR
	public UnityEngine.Object MIDIFile;
#endif
	public string SongFileName;
	public float Speed = 1;
	[TextArea]
	public string Details;
}