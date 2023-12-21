using System;
using System.Collections;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Minis;
using MidiJack;
using TMPro;
using System.Reflection;

public class MidiPlayer : MonoBehaviour
{
	[Header("References")]
	public PianoKeyController PianoKeyDetector;
	public static bool playAlong, pass, freeplay = true;
	public GameObject noteImage, noteUpImage, speedDisplay, timeDisplay, timeTexts, noteTexts;

	[Header("Properties")]
	public float GlobalSpeed = 1;
	public RepeatType RepeatType;

	public KeyMode KeyMode;
	public bool ShowMIDIChannelColours;
	public Color[] MIDIChannelColours;
	public TMP_Text currentTimeText, totalTimeText, currentNoteText, totalNoteText, leftNoteText;

	[Header("Ensure Song Name is filled for builds")]
	public MidiSong[] MIDISongs;

	[HideInInspector]
	public MidiNote[] MidiNotes;
	public UnityEvent OnPlayTrack { get; set; }

	MidiFileInspector _midi;

	string _path;
	string[] _keyIndex;
	int _noteIndex = 0, leftHandSameIndex = 0, leftHandInterval = 1, leftHandOnceIndex = 0, leftHandOnceInterval = 1, rightHandSameIndex = 0, rightHandInterval = 1, rightHandOnceIndex = 0, rightHandOnceInterval = 1;
	int sameLineNumber;
	public static int _midiIndex, gamelevel = 2;
    public static int[] alongKeys;
	double _timer = 0, currentTime = 0;
    float interval, imageInitY;
	[SerializeField, HideInInspector]
	bool _preset = false;
	Vector2 noteSize;

    GameObject[] u = new GameObject[88];
    float[] initTime = new float[88];
    bool[] pressed = new bool[88];

    void Start ()
	{
        
        pass = false;
		imageInitY = -300f + 1.92f * NoteFlow.originSpeed;
		OnPlayTrack = new UnityEvent();
		OnPlayTrack.AddListener(delegate{FindObjectOfType<MusicText>().StartSequence(MIDISongs[_midiIndex].Details);});

        //_midiIndex = 0;

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
		int t = (int)CalcTotalTime();
		totalTimeText.text = DisplayTotalTime(t);
        totalNoteText.text = MidiNotes.Length.ToString();
        leftNoteText.text = MidiNotes.Length.ToString();
        //InputSystem.onDeviceChange += (device, change) => {
        //	var midiDevice = device as Minis.MidiDevice;
        //	if (midiDevice == null) return;

        //	midiDevice.onWillNoteOn += (note, velocity) =>
        //	{

        //		// When middle C (MIDI #60) is pressed:
        //		if (note.noteNumber == 60)
        //		{

        //			// Character starts moving...
        //		}
        //	};


        //	midiDevice.onWillNoteOff += (note) => {

        //		// When middle C (MIDI #60) is released:
        //		if (note.noteNumber == 60)
        //		{

        //			// Character stops moving...
        //		}
        //	};
        //};
        
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
    int CalcUpImageIndex(int notenumber)
    {
        if (notenumber <= 2)
            return notenumber / 2 + 1;
        else
        {
            int q = (notenumber-3) / 12;
            int r = (notenumber-3) % 12;
            if (r < 6)
                return q * 7 + 2 + (r+1)/2;
            else
                return q * 7 + 2 + 4 + (r-3)/2;
        }
        
    }
	void Update ()
	{
		if (MIDISongs.Length <= 0)
			enabled = false;
		
		if (_midi != null && MidiNotes.Length > 0 && _noteIndex < MidiNotes.Length)
		{
			
            _timer += Time.deltaTime * GlobalSpeed * MidiNotes[_noteIndex].Tempo;
			currentTime += Time.deltaTime * GlobalSpeed;
			currentTimeText.text = DisplayTotalTime((int)currentTime);
            
            //Debug.LogError(MidiNotes[_noteIndex].Tempo);
            while (_noteIndex < MidiNotes.Length && MidiNotes[_noteIndex].StartTime < _timer && !freeplay)
			{
				timeDisplay.GetComponent<Slider>().value = (float)(_timer / MidiNotes[MidiNotes.Length - 1].StartTime);
				if (PianoKeyDetector.PianoNotes.ContainsKey(MidiNotes[_noteIndex].Note))
				{
					
                    GameObject g = Instantiate(noteImage, GameObject.Find("Canvas").transform) as GameObject;
                    //if (MidiNotes[0].Length <= 0.1f)
                    g.GetComponent<RectTransform>().localPosition = new Vector2(-950f  + (CalcImageIndex(MidiNotes[_noteIndex].Note) - 1) * noteSize.x / 36f * 37.2f, 540f);
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
					switch(gamelevel)
					{
						case 1:
						case 2:
						case 3:
                            
                            if ((MidiNotes[_noteIndex].Channel == 0 && CheckChannels()) || (MidiNotes[_noteIndex].Channel == 2 && !CheckChannels())) //left hand
                            {
								CheckLeftHandNotes();
                                switch (leftHandOnceInterval) //number of keys that is pressed with one hand at once //! not same time
								{
									case 1:
									switch (MidiNotes[_noteIndex].Note[0])
									{
										case 'C':
											if (MidiNotes[_noteIndex].Channel != MidiNotes[_noteIndex+1].Channel)
											{
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 191f / 255f, 228f / 255f); //thumb
												break;
                                            }
											else
											{
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 118f / 255f, 68f / 255f); //pinky
                                                break;
                                            }
										case 'F':
                                            g.GetComponent<Image>().color = new Color(255f / 255f, 118f / 255f, 68f / 255f); //pinky
											break;
                                        case 'D':
                                        case 'G':
                                            g.GetComponent<Image>().color = new Color(134f / 255f, 250f / 255f, 104f / 255f); //ring
											break;
                                        case 'E':
                                        case 'A':
                                            g.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 0f / 255f); //middle
                                            break;
                                        case 'B':
                                            g.GetComponent<Image>().color = new Color(84f / 255f, 217f / 255f, 227f / 255f); //index
                                            break;
                                    }
									break;
									case 2: //when pressing two keys at once
										if (_noteIndex == leftHandOnceIndex)
                                            g.GetComponent<Image>().color = new Color(255f / 255f, 191f / 255f, 228f / 255f); //thumb
                                        else
                                        {
											int interval = PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex + 1].Note) - PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex].Note);

                                            if (interval <= 4)
											{
                                                g.GetComponent<Image>().color = new Color(84f / 255f, 217f / 255f, 227f / 255f); //index
                                            }
											else if(interval <= 7)
                                                g.GetComponent<Image>().color = new Color(134f / 255f, 250f / 255f, 104f / 255f); //ring
											else
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 118f / 255f, 68f / 255f); //pinky
                                        }
                                        break;
                                    case 3:
                                        switch (leftHandOnceIndex - _noteIndex)
										{
											case 0:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 191f / 255f, 228f / 255f); //thumb
												break;
                                            case 1:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 0f / 255f); //middle
                                                if (MidiNotes[_noteIndex].Note.Length == 3)
                                                {
                                                    if(PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex + 1].Note) - PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex].Note) <= 4)
                                                        g.GetComponent<Image>().color = new Color(84f / 255f, 217f / 255f, 227f / 255f); //index
                                                    else
                                                        g.GetComponent<Image>().color = new Color(134f / 255f, 250f / 255f, 104f / 255f); //ring
                                                }
                                                break;
											case 2:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 118f / 255f, 68f / 255f); //pinky
                                                break;
										}
                                        break;
                                    case 4:
                                        switch (leftHandOnceIndex - _noteIndex)
                                        {
                                            case 0:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 191f / 255f, 228f / 255f); //thumb
                                                break;
                                            case 1:
                                                g.GetComponent<Image>().color = new Color(84f / 255f, 217f / 255f, 227f / 255f); //index
                                                break;
                                            case 2:
                                                g.GetComponent<Image>().color = new Color(134f / 255f, 250f / 255f, 104f / 255f); //ring
                                                break;
                                            case 3:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 118f / 255f, 68f / 255f); //pinky
                                                break;
                                        }
                                        break;
                                    case 5:
                                        switch (leftHandOnceIndex - _noteIndex)
                                        {
                                            case 0:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 191f / 255f, 228f / 255f); //thumb
                                                break;
                                            case 1:
                                                g.GetComponent<Image>().color = new Color(84f / 255f, 217f / 255f, 227f / 255f); //index
                                                break;
                                            case 2:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 0f / 255f); //middle
                                                break;
                                            case 3:
                                                g.GetComponent<Image>().color = new Color(134f / 255f, 250f / 255f, 104f / 255f); //ring
                                                break;
                                            case 4:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 118f / 255f, 68f / 255f); //pinky
                                                break;
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                CheckRightHandNotes();
                                switch (rightHandOnceInterval) //number of keys that is pressed with one hand at once //! not same time
                                {
                                    case 1:
                                        switch (MidiNotes[_noteIndex].Note[0])
                                        {
                                            case 'C':
                                                if (MidiNotes[_noteIndex].Channel != MidiNotes[_noteIndex + 1].Channel)
                                                    g.GetComponent<Image>().color = new Color(255f / 255f, 118f / 255f, 68f / 255f); //pinky
                                                else
                                                    g.GetComponent<Image>().color = new Color(255f / 255f, 191f / 255f, 228f / 255f); //thumb
                                                break;
                                            case 'F':
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 191f / 255f, 228f / 255f); //thumb
                                                break;
                                            case 'D':
                                            case 'G':
                                                g.GetComponent<Image>().color = new Color(84f / 255f, 217f / 255f, 227f / 255f); //index
                                                break;
                                            case 'E':
                                            case 'A':
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 0f / 255f); //middle
                                                break;
                                            case 'B':
                                                g.GetComponent<Image>().color = new Color(134f / 255f, 250f / 255f, 104f / 255f); //ring
                                                break;
                                        }
                                        break;
                                    case 2: //when pressing two keys at once
                                        if (_noteIndex != rightHandOnceIndex)
                                            g.GetComponent<Image>().color = new Color(255f / 255f, 191f / 255f, 228f / 255f); //thumb
                                        else
                                        {
                                            int interval = PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex].Note) - PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex-1].Note);

                                            if (interval <= 4)
                                            {
                                                g.GetComponent<Image>().color = new Color(84f / 255f, 217f / 255f, 227f / 255f); //index
                                            }
                                            else if (interval <= 7)
                                                g.GetComponent<Image>().color = new Color(134f / 255f, 250f / 255f, 104f / 255f); //ring
                                            else
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 118f / 255f, 68f / 255f); //pinky
                                        }
                                        break;
                                    case 3:
                                        switch (rightHandOnceIndex - _noteIndex)
                                        {
                                            case 0:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 118f / 255f, 68f / 255f); //pinky
                                                break;
                                            case 1:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 0f / 255f); //middle
                                                if (MidiNotes[_noteIndex].Note.Length == 3)
                                                {
                                                    if (PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex].Note) - PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex-1].Note) <= 4)
                                                        g.GetComponent<Image>().color = new Color(84f / 255f, 217f / 255f, 227f / 255f); //index
                                                    else
                                                        g.GetComponent<Image>().color = new Color(134f / 255f, 250f / 255f, 104f / 255f); //ring
                                                }
                                                break;
                                                break;
                                            case 2:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 191f / 255f, 228f / 255f); //thumb
                                                break;
                                        }
                                        break;
                                    case 4:
                                        switch (rightHandOnceIndex - _noteIndex)
                                        {
                                            case 0:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 118f / 255f, 68f / 255f); //pinky
                                                break;
                                            case 1:
                                                g.GetComponent<Image>().color = new Color(134f / 255f, 250f / 255f, 104f / 255f); //ring
                                                break;
                                            case 2:
                                                g.GetComponent<Image>().color = new Color(84f / 255f, 217f / 255f, 227f / 255f); //index
                                                break;
                                            case 3:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 191f / 255f, 228f / 255f); //thumb
                                                break;
                                        }
                                        break;
                                    case 5:
                                        switch (rightHandOnceIndex - _noteIndex)
                                        {
                                            case 0:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 118f / 255f, 68f / 255f); //pinky
                                                break;
                                            case 1:
                                                g.GetComponent<Image>().color = new Color(134f / 255f, 250f / 255f, 104f / 255f); //ring
                                                break;
                                            case 2:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 0f / 255f); //middle
                                                break;
                                            case 3:
                                                g.GetComponent<Image>().color = new Color(84f / 255f, 217f / 255f, 227f / 255f); //index
                                                break;
                                            case 4:
                                                g.GetComponent<Image>().color = new Color(255f / 255f, 191f / 255f, 228f / 255f); //thumb
                                                break;
                                        }
                                        break;
                                }
                            }
                            break;
						case 4:
                            print(MidiNotes[_noteIndex].Channel + "+" + MidiNotes[_noteIndex].StartTime + "+" + MidiNotes[_noteIndex].Note);
                            if ((MidiNotes[_noteIndex].Channel == 1 && CheckChannels()) || (MidiNotes[_noteIndex].Channel == 2 && !CheckChannels())) //left - blue
                            {
                                g.GetComponent<Image>().color = new Color(90f / 255f, 165f / 255f, 234f / 255f);
                                if (MidiNotes[_noteIndex].Note.Length == 3)
                                {
                                    g.GetComponent<Image>().color = new Color(1f / 255f, 133f / 255f, 255f / 255f);
                                }
                            }
                            else
                            {
                                g.GetComponent<Image>().color = new Color(183f / 255f, 65f / 255f, 139f / 255f);
                                if (MidiNotes[_noteIndex].Note.Length == 3)
                                {
                                    g.GetComponent<Image>().color = new Color(157f / 255f, 17f / 255f, 104f / 255f);
                                }
                            }
                            break;
						case 5:
						case 6:
                            g.GetComponent<Image>().color = new Color(90f / 255f, 165f / 255f, 234f / 255f); //all blue - one color
                            if (MidiNotes[_noteIndex].Note.Length == 3)
                            {
                                g.GetComponent<Image>().color = new Color(1f / 255f, 133f / 255f, 255f / 255f);
                            }
                            break;
					}
                    
					g.name = MidiNotes[_noteIndex].Channel + "+" + leftHandOnceInterval + "+" + MidiNotes[_noteIndex].StartTime + "+" + MidiNotes[_noteIndex].Note;
					
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
			SceneManager.LoadScene("Rosetta");
		if(pass)
		{
			Time.timeScale = 1f;
			pass = false;
		}
		switch (gamelevel)
		{
			case 1:
				GlobalSpeed = 0.2f;
				playAlong = true;
                timeTexts.SetActive(true);
                noteTexts.SetActive(false);
				break;
            case 2:
                GlobalSpeed = 1f;
                playAlong = false;
                timeTexts.SetActive(false);
                noteTexts.SetActive(true);
                leftNoteText.gameObject.SetActive(true);
                totalNoteText.gameObject.SetActive(false);
                break;
            case 3:
                GlobalSpeed = 1f;
                playAlong = false;
                timeTexts.SetActive(false);
                noteTexts.SetActive(true);
                leftNoteText.gameObject.SetActive(false);
                totalNoteText.gameObject.SetActive(true);
                break;
        }
        if (freeplay)
        {
            GlobalSpeed = 1f;
            playAlong = false;
            timeTexts.SetActive(false);
            noteTexts.SetActive(false);
            leftNoteText.gameObject.SetActive(false);
            totalNoteText.gameObject.SetActive(false);
            int notenumber;
            for (notenumber = 0; notenumber < 88; notenumber++)
            {
                if (MidiMaster.GetKeyDown(notenumber) && !pressed[notenumber])
                {
                    u[notenumber] = Instantiate(noteUpImage, GameObject.Find("Canvas").transform) as GameObject;
                    u[notenumber].GetComponent<RectTransform>().localPosition = new Vector2(-950f + (CalcImageIndex(PianoKeyDetector.noteOrder[notenumber].ToString()) - 1) * noteSize.x / 36f * 37.2f, -300f);
                    if (PianoKeyDetector.noteOrder[notenumber].Length == 3)
                    {
                        Vector2 sizeDelta = u[notenumber].GetComponent<RectTransform>().sizeDelta;
                        u[notenumber].GetComponent<RectTransform>().sizeDelta = new Vector2(sizeDelta.x / 2f, sizeDelta.y);
                        u[notenumber].GetComponent<RectTransform>().localPosition = new Vector2(u[notenumber].GetComponent<RectTransform>().localPosition.x + noteSize.x / 36f * 18.6f, u[notenumber].GetComponent<RectTransform>().localPosition.y);
                    }
                    initTime[notenumber] = Time.time;
                    pressed[notenumber] = true;
                }
                if (pressed[notenumber] && Time.time - initTime[notenumber] > 0.1f)
                {
                    print(Time.time - initTime[notenumber]);
                    Vector2 sizeDelta = u[notenumber].GetComponent<RectTransform>().sizeDelta;
                    u[notenumber].GetComponent<RectTransform>().sizeDelta = new Vector2(sizeDelta.x, (int)((Time.time - initTime[notenumber]) * 60f * 100f) / 10f);
                }
                if(MidiMaster.GetKeyUp(notenumber))
                    pressed[notenumber] = false;
            }
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
                        //print(alongKeys[i]);
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
                currentNoteText.text = (_index + 1).ToString();
                leftNoteText.text = (MidiNotes.Length - _index - 1).ToString();
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
    double CalcTotalTime()
    {
        double curTempo = MidiNotes[0].Tempo;
        int curSameTempoNotes = 1;
        double totalSongTime = 0f;
        for (int i = 1; i < MidiNotes.Length; i++)
        {
            if (MidiNotes[i].Tempo != MidiNotes[i - 1].Tempo || i == MidiNotes.Length - 1)
            {
                totalSongTime += (MidiNotes[i].StartTime - MidiNotes[i - curSameTempoNotes].StartTime) / curTempo;
                curTempo = MidiNotes[i].Tempo;
                curSameTempoNotes = 0;
            }
            else
                curSameTempoNotes++;
        }
        return totalSongTime;
    }

	bool CheckChannels() //if Channel 0 or Channel 2? t/f
	{
		for (int i = 0; i < MidiNotes.Length; i++)
		{
			if (MidiNotes[i].Channel == 0)
				return true;
			else if (MidiNotes[i].Channel == 2)
				return false;
        }
		return true;
	}
	void CheckLeftHandNotes()
	{
		int i = 0;
		while(_noteIndex + i + 1 < MidiNotes.Length && MidiNotes[_noteIndex + i].Channel == MidiNotes[_noteIndex + i + 1].Channel && MidiNotes[_noteIndex + i].StartTime == MidiNotes[_noteIndex + i + 1].StartTime)
		{
			i++;
		}
		if (leftHandSameIndex != _noteIndex + i)
		{
			leftHandInterval = i + 1;
			leftHandSameIndex = _noteIndex + i;
		}
        i = 0;
        int t = 1;
        //while (_noteIndex + i + t < MidiNotes.Length)
        //{
        //    if (MidiNotes[_noteIndex + i].Channel == MidiNotes[_noteIndex + i + t].Channel)
        //    {
        //        if (PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex + i].Note) <= PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex + i + t].Note))
        //            i++;
        //        else
        //            break;
        //    }
        //    else
        //        t++;
        //}
        while (_noteIndex + i + 1 < MidiNotes.Length && MidiNotes[_noteIndex + i].Channel == MidiNotes[_noteIndex + i + 1].Channel && PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex + i].Note) <= PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex + i + 1].Note))
        {
            i++;
        }
        if (leftHandOnceIndex != _noteIndex + i)
        {
            leftHandOnceInterval = i + 1;
            leftHandOnceIndex = _noteIndex + i;
        }
    }
	void CheckRightHandNotes()
	{
        int i = 0;
        while (_noteIndex + i + 1 < MidiNotes.Length && MidiNotes[_noteIndex + i].Channel == MidiNotes[_noteIndex + i + 1].Channel && MidiNotes[_noteIndex + i].StartTime == MidiNotes[_noteIndex + i + 1].StartTime)
        {
            i++;
        }
        if (rightHandSameIndex != _noteIndex + i)
        {
            rightHandInterval = i + 1;
            rightHandSameIndex = _noteIndex + i;
        }
        i = 0;
        while (_noteIndex + i + 1 < MidiNotes.Length && MidiNotes[_noteIndex + i].Channel == MidiNotes[_noteIndex + i + 1].Channel && PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex + i].Note) <= PianoKeyDetector.noteOrder.IndexOf(MidiNotes[_noteIndex + i + 1].Note))
        {
            i++;
        }
        if (rightHandOnceIndex != _noteIndex + i)
        {
            rightHandOnceInterval = i + 1;
            rightHandOnceIndex = _noteIndex + i;
        }
    }
	string DisplayTotalTime(int t)
	{
		string s = "";
		int m = (int)t / 60;
		s = m.ToString();
		if (m < 10)
			s = "0" + s;
        s += ":";

        t = t - m * 60;
		if (t < 10)
			s += "0" + t.ToString();
		else
			s += t.ToString();
        return s;
	}

    void SetupNextMIDI()
	{
		if(RepeatType == RepeatType.PlayOnlyOne)
		{
			_midi = null;
			return;
		}
		else if (_midiIndex >= MIDISongs.Length - 1)
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

public enum RepeatType { NoRepeat, RepeatLoop, RepeatOne, PlayOnlyOne }
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