using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class RecordView : MonoBehaviour
{
    public int index = -1;
    public Text recordingText;
    public Button playBackBtn;
    private GameManager gameMgr = null;

    // Start is called before the first frame update
    void Start()
    {
        gameMgr = FindObjectOfType<GameManager>();
        recordingText = GetComponentInChildren<Text>();
        playBackBtn.onClick.AddListener(OnPlayBackClicked);
    }
    public void OnPlayBackClicked()
    {
        gameMgr.activeRecordingIndex = index;
        gameMgr.ChangeGameState(GameStates.ReplayMode);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
