using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Orangedkeys.HOLOFX;
using System.IO;

// Set the scale of all the imported models to  "globalScaleModifier"
// and dont generate materials for the imported objects

public class AssetPostImportProcess_HOLOFX : AssetPostprocessor
{
    static private bool WelcomeWin = false;
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        WelcomeWin = false;
        foreach (string item in importedAssets)
        {
            if (Path.GetFileName(item) == "AssetPostImportProcess_HOLOFX.cs") WelcomeWin = true;
        }

        foreach (string itemdel in deletedAssets)
        {
            if (Path.GetFileName(itemdel) == "AssetPostImportProcess_HOLOFX.cs") WelcomeWin = false;
            break;
        }


        if (WelcomeWin)
        {
            Debug.Log("HOLOFX PACK IMPORTED !!");
            Welcome_HOLOFX.ShowWindow();
        }


    }

}