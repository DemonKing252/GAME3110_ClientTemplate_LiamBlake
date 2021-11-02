using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Assertions;

// TODO:
// 1. replay system
// 2. prevent clients from joining session with same username
// 3. back button on session view (observer and replay)

public static class GameStates
{
    public const int Login = 1;

    public const int MainMenu = 2;
    
    public const int WaitingForMatch = 3;
    
    public const int PlayingTicTacToe = 4;

    public const int ConnectingToHost = 5;

    public const int FindingObserver = 6;

    public const int DisconnectionMenu = 7;

    public const int ReplayMenu = 8;

    public const int ReplayMode = 9;
    
}
public enum GameResult
{
    PlayerX,
    PlayerO,
    Tie,
    NothingDetermined

}
[System.Serializable]
public class Record
{
    public char[] slots = new char[9]
    {
        ' ',
        ' ',
        ' ',
        ' ',
        ' ',
        ' ',
        ' ',
        ' ',
        ' ',
    };

    public string serverResponse;
    public List<string> messages;
    
    public Record()
    {
        messages = new List<string>();
    }
    public string GetParsedData()
    {
        string temp = "";
        foreach(char s in slots)
        {
            temp += s.ToString() + "|";
        }
        temp += serverResponse + "|+";
        foreach(string m in messages)
        {
            temp += m + "|";
        }

        return temp;
    }
    // Client view
}


public class GameManager : MonoBehaviour
{
    // Only if were in replay mode
    public int activeRecordingIndex = -1;
    public int index = 0;
    public float recordRate = 0.5f;
    public GameObject inputFieldUserName;
    public GameObject inputFieldPassword;

    public GameObject createAccountBtn;
    public GameObject loginAccountBtn;
    public GameObject connectBtn;
    public GameResult winner;

    private NetworkedClient netclient;

    public GameObject serverStatus;

    public Canvas searchingObserver;
    public Canvas findSessionUI;
    public Canvas loginUI;
    public Canvas gameUI;
    public Canvas connectToHostUI;
    public Canvas disconnectUI;
    public Canvas replayMenuUI;
    public Canvas replayModeUI;

    public Text debugText;
    public Text connectionVerificationStatus;
    public InputField ipaddress;
    public InputField portNumber;
    public GameObject textPrefab;

    public GameObject inputFieldMessage;
    public GameObject sendBtn;

    public GameObject sessionPrefab;
    public GameObject recordingPrefab;

    public char mychar = 'X';
    public char playersturn = 'X';

    public bool sendtogamesession = true;
    public bool sendtoobservers = true;
    public bool sendtotherclients = true;

    public Sprite playBtn;
    public Sprite pauseBtn;

    public Recording activeRecording = null;
    public int currentRecordIndex = 0;
    public float recordCounter = 0f;
    public float replayScale = 1f;
    // 60 fps consistent

    public GameObject destinationRecordingImg;

    #region ReplayAttributes
    public Text replayTime;
    public Text replayHeader;
    public Text serverSesionStatus;
    #endregion

    private bool recordingPaused = false;

    [HideInInspector]
    public string sendmsg;

    [HideInInspector]
    public int currentGameState = 1;

    private List<GameObject> textMessages = new List<GameObject>();

    private List<GameObject> recordingOptions = new List<GameObject>();

    public GameObject observerParent;
    public GameObject recordingParent;
    public List<Record> recordViews = new List<Record>();

    [HideInInspector]
    public bool isRecording = false;

    private int MaxElementsPerRecord;
    private const int recordSize = 224;
    public string user;
    public string password;

    [HideInInspector]
    public bool connectionSuccessful = false;
    public GameObject recordingTextsParent;

    // Start is called before the first frame update
    void Start()
    {
        // Maximum number of elements we can specify in one packet
        MaxElementsPerRecord = Mathf.FloorToInt((float)1024 / (float)recordSize);

        createAccountBtn.GetComponent<Button>().onClick.AddListener(OnCreateAccount);
        loginAccountBtn.GetComponent<Button>().onClick.AddListener(OnLogin);
        connectBtn.GetComponent<Button>().onClick.AddListener(OnTryConnection);

        inputFieldUserName.GetComponent<InputField>().onEndEdit.AddListener(OnInputUser);
        inputFieldPassword.GetComponent<InputField>().onEndEdit.AddListener(OnInputPassword);
        inputFieldMessage.GetComponent<InputField>().onEndEdit.AddListener(OnTextMessageEntered);
        sendBtn.GetComponent<Button>().onClick.AddListener(OnSendMessageClicked);


        netclient = FindObjectOfType<NetworkedClient>();

        ChangeGameState(GameStates.ConnectingToHost);

    }

    public void OnTryConnection()
    {
        connectionVerificationStatus.text = "Trying to connect . . .";
        connectionVerificationStatus.color = Color.white;
        netclient.Connect(ipaddress.text, int.Parse(portNumber.text));
        
        // time out of 3 seconds
        Invoke("_VerifyConnection", 3f);
    }
   
    public void _VerifyConnection()
    {
        // Destroy network transport objects, and reinitialize and try for 
        // another connection when the client enters a new ip and/or port
        if (!connectionSuccessful)
        {
            NetworkTransport.Shutdown();

            connectionVerificationStatus.text = "Error: Unknown host!";
            connectionVerificationStatus.color = Color.red;

        }
    }
    
    public void OnSendMessageClicked()
    {
        //TODO: options for sending to certain clients

        netclient.SendMessageToHost
            (
                ClientToServerSignifier.ChatMessage + "," +
                sendtogamesession.ToString() + "," +
                sendtoobservers.ToString() + "," +
                sendtotherclients.ToString() + "," + 
                sendmsg
            );

        // spawntext()
    }
    public void OnTextMessageEntered(string msg)
    {
        // Remove comma seperated, '|' and '+' seperated values because they will interfere
        // when comparing signifier

        msg.Replace(",", " ");
        msg.Replace("+", " ");
        msg.Replace("|", " ");

        // needed if the user decides to click the button
        sendmsg = user + ": " + msg;
    }
    public void ClearTextMessages()
    {
        foreach(GameObject go in textMessages)
            Destroy(go);    

        textMessages.Clear();
    }

    public void SpawnText(string msg)
    {
        inputFieldMessage.GetComponent<InputField>().text = "";
        GameObject go = Instantiate(textPrefab, GameObject.FindGameObjectWithTag("TextContent").transform);
        go.GetComponent<Text>().text = " " + msg;

        textMessages.Add(go);
    }
    public GameResult CheckGameResult()
    {
        BoardView b = netclient.boardGameView;
        int column = 0;
        for(int i = 0; i < 9; i+=3)
        {
            // ----------------------- X player -----------------------
            // horizontal check
            if (b.slots[i  ].characterinslot == 'X' &&
                b.slots[i+1].characterinslot == 'X' &&
                b.slots[i+2].characterinslot == 'X')
            {
                return GameResult.PlayerX;
            }
            
            // horizontal check
            else if 
               (b.slots[    column].characterinslot == 'X' &&
                b.slots[3 + column].characterinslot == 'X' &&
                b.slots[6 + column].characterinslot == 'X')
            {
                return GameResult.PlayerX;
            }
            // --------------------------------------------------------


            // ----------------------- O player -----------------------
            if (b.slots[i].characterinslot == 'O' &&
                b.slots[i + 1].characterinslot == 'O' &&
                b.slots[i + 2].characterinslot == 'O')
            {
                return GameResult.PlayerO;
            }

            // vertical check
            else if
               (b.slots[column].characterinslot == 'O' &&
                b.slots[3 + column].characterinslot == 'O' &&
                b.slots[6 + column].characterinslot == 'O')
            {
                return GameResult.PlayerO;
            }

            // --------------------------------------------------------
            column++;
        }
        // --------------------------------------------------------

        // ----------------------- X player -----------------------
        // diagonal check Top left - bottom right
        if (b.slots[0].characterinslot == 'X' && b.slots[4].characterinslot == 'X' && b.slots[8].characterinslot == 'X')
            return GameResult.PlayerX;

        // diagonal check Top right - bottom left
        else if (b.slots[2].characterinslot == 'X' && b.slots[4].characterinslot == 'X' && b.slots[6].characterinslot == 'X')
            return GameResult.PlayerX;

        // --------------------------------------------------------

        // ----------------------- O player -----------------------
        // diagonal check Top left - bottom right

        if (b.slots[0].characterinslot == 'O' && b.slots[4].characterinslot == 'O' && b.slots[8].characterinslot == 'O')
            return GameResult.PlayerO;

        // diagonal check Top right - bottom left
        else if (b.slots[2].characterinslot == 'O' && b.slots[4].characterinslot == 'O' && b.slots[6].characterinslot == 'O')
            return GameResult.PlayerO;


        // --------------------------------------------------------
        // else is it a tie game?
        bool tiegame = true;
        for (int i = 0; i < 9; i++)
            if (b.slots[i].characterinslot == ' ')
                tiegame = false;

        if (tiegame)
            return GameResult.Tie;

        return GameResult.NothingDetermined;
    }
    public void OnUpdateBoard()
    {
        string message = ClientToServerSignifier.UpdateBoard + ",";
        foreach(TicTacToeSlot slot in netclient.boardGameView.slots)
        {
            message += slot.characterinslot.ToString();
            message += ",";
        }
        netclient.SendMessageToHost(message);
        Debug.Log("Sending . . . " + message);
    }

    public void OnCreateAccount()
    {
        //if (toggleCreate.GetComponent<Toggle>().isOn)
        string msg = ClientToServerSignifier.CreateAccount.ToString() + "," + user + "," + password;
        netclient.SendMessageToHost(msg);

    }
    
    public void OnLogin()
    {
        string msg = ClientToServerSignifier.Login.ToString() + "," + user + "," + password;
        netclient.SendMessageToHost(msg);
    }
    public void OnInputUser(string txt)
    {
        user = txt;
        Debug.Log("input field is : " + txt);
    }
    public void OnInputPassword(string txt)
    {
        password = txt;
        Debug.Log("input field is : " + txt);
    }
    public void OnToggleLogin(bool toggle)
    {
        Debug.Log("value is : " + toggle.ToString());
    }
    public void OnToggleCreate(bool toggle)
    {

        Debug.Log("value is : " + toggle.ToString());
    }
    public void ChangeGameState(int newState)
    {
        currentGameState = newState;
        if (newState == GameStates.Login)
        {
            disconnectUI.gameObject.SetActive(false);
            searchingObserver.gameObject.SetActive(false);
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(true);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(false);
            replayMenuUI.gameObject.SetActive(false);
            replayModeUI.gameObject.SetActive(false);
        }
        else if (newState == GameStates.MainMenu)
        {
            disconnectUI.gameObject.SetActive(false);
            searchingObserver.gameObject.SetActive(false);
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(true);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(false);
            replayMenuUI.gameObject.SetActive(false);
            replayModeUI.gameObject.SetActive(false);

        }
        else if (newState == GameStates.WaitingForMatch)
        {
            disconnectUI.gameObject.SetActive(false);
            searchingObserver.gameObject.SetActive(false);
            findSessionUI.gameObject.SetActive(true);
            loginUI.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(false);
            replayMenuUI.gameObject.SetActive(false);
            replayModeUI.gameObject.SetActive(false);
        }
        else if (newState == GameStates.PlayingTicTacToe)
        {

            disconnectUI.gameObject.SetActive(false);
            searchingObserver.gameObject.SetActive(false);
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(true);
            connectToHostUI.gameObject.SetActive(false);
            replayMenuUI.gameObject.SetActive(false);
            replayModeUI.gameObject.SetActive(false);

            netclient.boardGameView._Reset();
            ClearTextMessages();
        }
        else if (newState == GameStates.ConnectingToHost)
        {
            disconnectUI.gameObject.SetActive(false);
            searchingObserver.gameObject.SetActive(false);
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(true);
            replayMenuUI.gameObject.SetActive(false);
            replayModeUI.gameObject.SetActive(false);
        }
        else if (newState == GameStates.FindingObserver)
        {

            disconnectUI.gameObject.SetActive(false);
            searchingObserver.gameObject.SetActive(true);
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(false);
            replayMenuUI.gameObject.SetActive(false);
            replayModeUI.gameObject.SetActive(false);
            netclient.RefreshSessions();
        }
        else if (newState == GameStates.DisconnectionMenu)
        {
            disconnectUI.gameObject.SetActive(true);
            searchingObserver.gameObject.SetActive(false);
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(false);
            replayMenuUI.gameObject.SetActive(false);
            replayModeUI.gameObject.SetActive(false);
        }
        else if (newState == GameStates.ReplayMenu)
        {
            disconnectUI.gameObject.SetActive(false);
            searchingObserver.gameObject.SetActive(false);
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(false);
            replayMenuUI.gameObject.SetActive(true);
            replayModeUI.gameObject.SetActive(false);
            RefreshRecordings();
        }
        else if (newState == GameStates.ReplayMode)
        {
            disconnectUI.gameObject.SetActive(false);
            searchingObserver.gameObject.SetActive(false);
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(false);
            replayMenuUI.gameObject.SetActive(false);
            replayModeUI.gameObject.SetActive(true);

            // Get our needed variables reset 
            recordCounter = 0f;
            currentRecordIndex = 0;
            replayScale = 1f;
            recordingPaused = false;

            replayHeader.text = "Replaying - Recording #" + activeRecordingIndex.ToString() + " by " + netclient.recordings[activeRecordingIndex].username + " on " + netclient.recordings[activeRecordingIndex].timeRecorded;

        }
    }
    public void StopRecording()
    {
        // Observers cant record there sessions
        if (netclient.isObserver)
            return;

        isRecording = false;
        // Record the last frame in case it was missed
        OnRecordScreenState();
        CancelInvoke("OnRecordScreenState");

        // Since the server needs time to process each recording,
        // we need to give player 1 some time to upload their recording
        // to the server before player 2.
        if (netclient.playerNumber == 1)
        {
            UploadRecording();

        }
        else if (netclient.playerNumber == 2)
        {
            Invoke("DelayThenStop", 2f);
        }
    }
    
    public void DelayThenStop()
    {
        UploadRecording();
    }
    public void OnLeaveRecording()
    {
        ChangeGameState(GameStates.ReplayMenu);
    }
    public void UploadRecording() 
    { 
        // send all records to the server
        int SubDivisions = MaxElementsPerRecord;
        int SubDivisionsPerList = (recordViews.Count / SubDivisions);


        // Add to the list of records:
        List<Record> tempRecords = new List<Record>();

        // In all of our records (seperated by "subdivision count")
        for (int i = 0; i < SubDivisionsPerList + 1; i++)
        {
            int indexStart = i * SubDivisions;
            int indexEnd = (i + 1) * SubDivisions;

            tempRecords.Clear();

            // For every record in this sub divided list
            for (int j = indexStart; j < indexEnd; j++)
            {
                // Were sub dividing by a floored number so we will always have a remainder
                // of elements after the last sub division, just do a simple check
                if (j < recordViews.Count)
                {
                    tempRecords.Add(recordViews[j]);
                }
            }
            // parse the data into comma seperated values
            string msg = ClientToServerSignifier.SendRecord + "," + tempRecords.Count.ToString() + ",";

            // Get parsed data returns the board slots as '|' seperated values.
            // We can have seperated values inside other seperated values.
            // a comma will seperate the records, while a '|' will seperate the board slots themselves
            foreach (Record r in tempRecords)                
                msg += r.GetParsedData() + ",";


            // and now we can send it to the server
            netclient.SendMessageToHost(msg);

            //Debug.Log("Message: " + msg.ToString());
        }
        // tell the server that were done sending records
        // so the server can add it to the list of saved recordings
        netclient.SendMessageToHost(ClientToServerSignifier.RecordSendingDone.ToString() + "," + GetFormattedTime() + "," + user + ",");

        // Clear our records, the server has them and well get a list of recordings for replaying.
        recordViews.Clear();

    }
    public void StartRecording()
    {
        recordViews.Clear();
        InvokeRepeating("OnRecordScreenState", 0f, recordRate);

    }

    // Record the state of our game.
    public void OnRecordScreenState()
    {
        Record r = new Record();
        
        // Throw an exception, these container sizes should be the same
        Assert.IsTrue(r.slots.Length == netclient.boardGameView.slots.Count, "Error: Record board size and board view size needs to be the same!");

        for (int i = 0; i < r.slots.Length; i++)
        {
            r.slots[i] = netclient.boardGameView.slots[i].characterinslot;
        }
        r.serverResponse = netclient.sessionstatus.text;

        foreach(GameObject go in textMessages)
        {
            r.messages.Add(go.GetComponent<Text>().text);
        }

        recordViews.Add(r);

    }
    public static string GetFormattedTime()
    {

        System.DateTime dateTime = System.DateTime.Now;
        string txt =
            dateTime.Month.ToString("00") +
            "-" + dateTime.Day.ToString("00") +
            "-" + dateTime.Year.ToString("00") +
            " " + dateTime.Hour.ToString("00") +
            ":" + dateTime.Minute.ToString("00") +
            ":" + dateTime.Second.ToString("00");

        return txt;
    }
    public void RefreshRecordings()
    {
        GameObject[] _records = GameObject.FindGameObjectsWithTag("RecordingButton");
        
        foreach (GameObject go in _records)
            Destroy(go);

        for (int i = 0; i < netclient.recordings.Count; i++)
        {
            GameObject go = Instantiate(recordingPrefab, recordingParent.transform);
            go.GetComponent<RecordView>().index = i;
            go.GetComponent<RecordView>().recordingText.text = "Record #" + i.ToString() + " by: " + netclient.recordings[i].username + " on " + netclient.recordings[i].timeRecorded;

        }
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            netclient.SendMessageToHost(144.ToString() + "," + netclient.recordings[0].records[index].GetParsedData());
        }
    }
    
    void FixedUpdate()
    {
        if (currentGameState == GameStates.ReplayMode)
            ReplayUpdate();
    }
    // Pause/Fast fwd/Reverse
    public void OnPlayBackEvent(int evt)
    {
        // Reverse 
        if (evt == 0)
        {
            replayScale = -1f;
            recordingPaused = false;
            destinationRecordingImg.GetComponent<Image>().sprite = pauseBtn;
            destinationRecordingImg.transform.localScale = new Vector3(1f, 1f, 1f);
        }
        // Pause/Play
        else if (evt == 1)
        {
            recordingPaused = !recordingPaused;

            if (recordingPaused)
            {
                replayScale = 0f;
                destinationRecordingImg.GetComponent<Image>().sprite = playBtn;
                destinationRecordingImg.transform.localScale = new Vector3(2f, 1f, 1f);
            }
            else
            {
                destinationRecordingImg.GetComponent<Image>().sprite = pauseBtn;
                destinationRecordingImg.transform.localScale = new Vector3(1f, 1f, 1f);
                replayScale = 1f;

            }
        }
        // Fast forward
        else if (evt == 2)
        {
            recordingPaused = false;
            destinationRecordingImg.GetComponent<Image>().sprite = pauseBtn;
            destinationRecordingImg.transform.localScale = new Vector3(1f, 1f, 1f);
            replayScale = 1.5f;
        }
    }


    public void ReplayUpdate()
    {
        try 
        {
            // We don't have to worry about delta time here, because fixed update will sync our play back anyway!
            // Unity is very generous
            

            activeRecording = netclient.recordings[activeRecordingIndex];

            recordCounter += recordRate * replayScale;

            recordCounter = Mathf.Clamp(recordCounter, 0, (float)(activeRecording.records.Count - 1));
            currentRecordIndex = (int)recordCounter;

            float secondsNow = (float)currentRecordIndex / (recordRate * 60f);
            float totalSeconds = (float)activeRecording.records.Count / (recordRate * 60f);


            replayTime.text = GetFormattedTime(secondsNow) + " / " + GetFormattedTime(totalSeconds);

            // Update board
            Record currentRecord = activeRecording.records[currentRecordIndex];

            // Board status
            for (int i = 0; i < 9; i++)
                netclient.boardRecordView.slots[i].SetSlot(currentRecord.slots[i]);

            GameObject[] gos = GameObject.FindGameObjectsWithTag("RecordedTextContent");
            foreach (GameObject g in gos)
                Destroy(g);

            foreach(string text in currentRecord.messages)
            {
                // We don't need to waste time spawning text messages that clients
                // send that are empty. Theres no point
                if (text == "")
                    continue;

                GameObject go = Instantiate(textPrefab, recordingTextsParent.transform);
                go.GetComponent<Text>().text = text;
                go.tag = "RecordedTextContent";
            }


            // Board session header (the text above the tic tac toe board)
            serverSesionStatus.text = currentRecord.serverResponse;

        }
        catch(System.Exception e) 
        {
            Debug.LogError("EXCEPTION when replaying: " + e.Message);
        }
    }
    public string GetFormattedTime(float secs)
    {
        int min = (int)secs / 60;
        int sec = (int)secs % 60;

        string temp = min.ToString() + ":" + sec.ToString("00");
        return temp;
    }
    public void OnQuitApp()
    {
        // If were in unity editor, closing the app will be done through editor application
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
