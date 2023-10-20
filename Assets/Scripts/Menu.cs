using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{

    public GameObject Menupanel, Settingspanel, Exitpanel, Background, title, flowPrefab;
    private Vector3 spawnPosition, eliminPosition;
    private Vector3 flowDirection;
    public static bool hideKeyboard;
    // Start is called before the first frame update
    void Start()
    {
        title.SetActive(true);
        Menupanel.SetActive(true);
        Settingspanel.SetActive(false);
        Exitpanel.SetActive(false);
        MidiPlayer.playAlong = false;
        hideKeyboard = false;


        //spawnPosition = GameObject.Find("spawnPosition").transform.position;
        //eliminPosition = GameObject.Find("eliminPosition").transform.position;
        spawnPosition = new Vector3(3697f, 2448f, 0f);
        eliminPosition = new Vector3(-2686f, -1568f, 0f);
        flowDirection = (eliminPosition - spawnPosition).normalized;
    }

    // Update is called once per frame
    void Update()
    {
        GameObject[] flows = GameObject.FindGameObjectsWithTag("Flow");
        foreach(GameObject flow in flows)
        {
            flow.transform.position += flowDirection * Time.deltaTime * 500f;
            
            if(Vector3.Distance(flow.GetComponent<RectTransform>().localPosition, eliminPosition) <= 12f)
            {
                Destroy(flow);

                GameObject newSpawn = Instantiate(flowPrefab, Background.transform) as GameObject;
                newSpawn.GetComponent<RectTransform>().localPosition = spawnPosition;
            }
        }

        if(Input.GetKeyUp(KeyCode.Escape))
        {
            Exit();
        }
    }

    public void SelectFile()
    {
        
    }

    public void Settings()
    {
        Settingspanel.SetActive(true);
        Menupanel.SetActive(false);
        Exitpanel.SetActive(false);
        title.SetActive(true);
    }

    public void Back()
    {
        ExitNo();
    }

    public void Exit()
    {
        title.SetActive(false);
        Menupanel.SetActive(false);
        Settingspanel.SetActive(false);
        Exitpanel.SetActive(true);
    }

    public void ExitYes()
    {
        Application.Quit(0);
    }

    public void ExitNo()
    {
        title.SetActive(true);
        Menupanel.SetActive(true);
        Settingspanel.SetActive(false);
        Exitpanel.SetActive(false);
    }

    public void Play()
    {
        SceneManager.LoadScene("Play");
    }

    public void PlayAlong()
    {
        MidiPlayer.playAlong = !MidiPlayer.playAlong;
    }

    public void HideKeyboards()
    {
        hideKeyboard = !hideKeyboard;
    }
}
