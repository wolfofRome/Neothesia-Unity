using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiJack;

public class test : MonoBehaviour
{
    
    int notenumber_min = 0, notenumber_max = 127;
    // Update is called once per frame
    void Update()
    {
        for (int notenumber = notenumber_min; notenumber < notenumber_max; notenumber ++ ) {
            if (MidiMaster.GetKeyDown(notenumber)) {
                Debug.LogFormat("KeyDown {0}", notenumber);
            }
            
            if (MidiMaster.GetKeyUp(notenumber)) {
                Debug.LogFormat("KeyUp {0}", notenumber);
            }
        }
    }
}