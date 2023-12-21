using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteFlow : MonoBehaviour
{
    public static float originSpeed, flowSpeed;
    // Start is called before the first frame update
    void Start()
    {
        originSpeed = flowSpeed = 600;
    }
    
    // Update is called once per frame
    void Update()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if(!MidiPlayer.freeplay)
            rect.localPosition = new Vector2(rect.localPosition.x, rect.localPosition.y - Time.deltaTime * flowSpeed);
        else
            rect.localPosition = new Vector2(rect.localPosition.x, rect.localPosition.y + Time.deltaTime * flowSpeed);
    }
}
