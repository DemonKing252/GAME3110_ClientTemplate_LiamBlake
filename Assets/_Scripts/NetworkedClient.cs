using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Assertions;


public static class ClientToServerSignifier
{
    public const int Login = 1;
    public const int CreateAccount = 2;

    public const int AddToGameSessionQueue = 3;
    public const int TicTacToePlay = 4;
    public const int UpdateBoard = 5;

    public const int ChatMessage = 6;

    // Observers
    public const int AddToObserverSessionQueue = 7;

    public const int LeaveSession = 8;
    public const int LeaveServer = 9;

    public const int SendRecord = 10;
    public const int RecordSendingDone = 11;

}
public static class ServerToClientSignifier
{
    public const int LoginResponse = 101;
    public const int CreateResponse = 102;

    public const int GameSessionStarted = 103;

    public const int OpponentTicTacToePlay = 104;
    public const int UpdateBoardOnClientSide = 105;
    public const int VerifyConnection = 106;

    public const int MessageToClient = 107;
    public const int UpdateSessions = 108;

    public const int ConfirmObserver = 109;
    public const int PlayerDisconnected = 110;
    
    public const int SendRecording = 111;
    public const int QueueEndOfRecord = 112;
    public const int QueueStartOfRecordings = 113;
    public const int QueueEndOfRecordings = 114;
    public const int KickPlayer = 115;

}
// manage sending our chat message to clients who we want to have authority 
public static class MessageAuthority
{
    // These responses can be xxx digits, because they wont be checked anywhere else unless under the 
    // condition of "ChatMessage" (signafier = 6)
    // just to make sure though, im leaving a space of 50 between them.

    public const int ToGameSession = 151;       // To clients in the game session
    public const int ToObservers = 152;         // To observer clients
    public const int ToOtherClients = 153;      // To game session clients
}

public static class LoginResponse
{
    public const int Success = 1001;

    public const int WrongNameAndPassword = 1002;
    public const int WrongName = 1003;
    public const int WrongPassword = 1004;
    public const int AccountAlreadyUsedByAnotherPlayer = 1005;
    public const int AccountBanned = 1006;
}
public static class CreateResponse
{
    // 10,000
    public const int Success = 10001;
    public const int UsernameTaken = 10002;
}

[System.Serializable]
public class BoardView
{
    public List<TicTacToeSlot> slots;

    public void _Reset()
    {
        foreach(TicTacToeSlot slot in slots)
        {
            slot._Reset();
        }
    }

}
[System.Serializable]
public class Sessions
{
    public int index;
    public Sessions(int i)
    {
        index = i;
    }

}

[System.Serializable]
public class Recording
{
    public string username;   // username that this was recorded from
    public string timeRecorded;
    public List<Record> records = new List<Record>();
}


public class NetworkedClient : MonoBehaviour
{
    // A list of game sessions to choose from
    List<Sessions> sessionViews = new List<Sessions>();

    public List<Record> recordViews = new List<Record>();

    public List<Recording> recordings = new List<Recording>();

    int connectionID;
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 5491;
    byte error;
    bool isConnected = false;
    int ourClientID;
    public string ip;

    private GameManager gameMgr;

    public Button onfindsessionbtn;
    public Button observebtn;
    public Button disconnectbtn;
    public Button leavebtn;
    public Button replayListViewbtn;
    public Button quitbtn;

    public Toggle sessionToggle;
    public Toggle observerToggle;
    public Toggle othersessionsToggle;

    public Text sessionstatus;
    public Text gameroomstatus;

    public BoardView boardGameView = new BoardView();
    public BoardView boardRecordView = new BoardView();

    [HideInInspector]
    public int playerNumber = -1; // can only be 1 or 2, once the game session has started

    [HideInInspector]
    public bool isObserver = false;

    // Start is called before the first frame update
    void Start()
    {
        gameMgr = FindObjectOfType<GameManager>();

        onfindsessionbtn.onClick.AddListener(OnFindSession);
        observebtn.onClick.AddListener(OnLookForObserver);
        disconnectbtn.onClick.AddListener(OnUserDisconnected);
        leavebtn.onClick.AddListener(OnLeaveSession);
        replayListViewbtn.onClick.AddListener(OnEnterReplaySessionsView);

        sessionToggle.isOn = gameMgr.sendtogamesession;
        observerToggle.isOn = gameMgr.sendtoobservers;
        othersessionsToggle.isOn = gameMgr.sendtotherclients;


        sessionToggle.onValueChanged.AddListener(OnSendToCurrentGameSession);
        observerToggle.onValueChanged.AddListener(OnSendToObservers);
        othersessionsToggle.onValueChanged.AddListener(OnSendToOtherGameSessions);

    }
    public void OnSendToCurrentGameSession(bool toggle)
    {
        gameMgr.sendtogamesession = toggle;
    }
    public void OnSendToObservers(bool toggle)
    {
        gameMgr.sendtoobservers = toggle;
    }
    public void OnSendToOtherGameSessions(bool toggle)
    {
        gameMgr.sendtotherclients = toggle;
    }

    void OnApplicationQuit()
    {
        string msg = ClientToServerSignifier.LeaveServer.ToString() + "," + isObserver + ",";
        SendMessageToHost(msg);
    }
    

    public void OnLeaveSession()
    {
        string msg = ClientToServerSignifier.LeaveSession.ToString() + "," + isObserver + ",";
        SendMessageToHost(msg);

        gameroomstatus.gameObject.SetActive(false);
        onfindsessionbtn.gameObject.SetActive(true);
        observebtn.gameObject.SetActive(true);
        replayListViewbtn.gameObject.SetActive(true);
        quitbtn.gameObject.SetActive(true);

        gameMgr.ChangeGameState(GameStates.WaitingForMatch);
    }
    

    public void OnFindSession()
    {
        Debug.Log("Finding session . . .");
        SendMessageToHost(ClientToServerSignifier.AddToGameSessionQueue.ToString() + ",");

        gameroomstatus.gameObject.SetActive(true);

        onfindsessionbtn.gameObject.SetActive(false);
        observebtn.gameObject.SetActive(false);
        replayListViewbtn.gameObject.SetActive(false);
        quitbtn.gameObject.SetActive(false);

        gameMgr.ChangeGameState(GameStates.WaitingForMatch);

    }
    public void OnLookForObserver()
    {
        gameMgr.ChangeGameState(GameStates.FindingObserver);
    }
    public void OnEnterReplaySessionsView()
    {
        gameMgr.isRecording = true;
        gameMgr.ChangeGameState(GameStates.ReplayMenu);
    }
    // Update is called once per frame
    void Update()
    {
        UpdateNetworkConnection();

    }

    private void UpdateNetworkConnection()
    {
        if (isConnected)
        {
            int recHostID;
            int recConnectionID;
            int recChannelID;
            byte[] recBuffer = new byte[8192];
            int bufferSize = 8192;
            int dataSize;
            NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);

            if (error == 0)
            {
                switch (recNetworkEvent)
                {
                    case NetworkEventType.ConnectEvent:
                        Debug.Log("connected.  " + recConnectionID);
                        ourClientID = recConnectionID;
                        break;
                    case NetworkEventType.DataEvent:
                        string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                        ProcessRecievedMsg(msg, recConnectionID);
                        //Debug.Log("got msg = " + msg);
                        break;
                    case NetworkEventType.DisconnectEvent:
                        isConnected = false;
                        Debug.Log("disconnected.  " + recConnectionID);
                        break;
                }
            }
            
        }
    }

    public void Connect(string ip_address, int port)
    {

        //if (!isConnected)
        {
            Debug.Log("Attempting to create connection");

            NetworkTransport.Init();
            
            ConnectionConfig config = new ConnectionConfig();
            reliableChannelID = config.AddChannel(QosType.Reliable);
            unreliableChannelID = config.AddChannel(QosType.Unreliable);
            HostTopology topology = new HostTopology(config, maxConnections);
            hostID = NetworkTransport.AddHost(topology, 0);
            Debug.Log("Socket open.  Host ID = " + hostID);

            connectionID = NetworkTransport.Connect(hostID, ip_address, port, 0, out error); // server is local on network

            ip = ip_address;
            socketPort = port;

            if (error == 0)
            {
                isConnected = true;

                Debug.Log("Connected, id = " + connectionID);

            }
        }
    }

    public void Disconnect()
    {
        NetworkTransport.Disconnect(hostID, connectionID, out error);
    }

    public void SendMessageToHost(string msg)
    {
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, connectionID, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }
    private List<Record> tempRecords = new List<Record>();


    private void ProcessRecievedMsg(string msg, int id)
    {
        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);

        string[] data = msg.Split(',');

        int signafier = int.Parse(data[0]);

        if (signafier == ServerToClientSignifier.LoginResponse)
        {
            int status = int.Parse(data[1]);    

            if (status == LoginResponse.Success)
            {
                SetServerAuthenticationStatus("Login Successful!", Color.green);
                gameMgr.ChangeGameState(GameStates.WaitingForMatch);
            
            
            }
            else if (status == LoginResponse.WrongName)
            {
                SetServerAuthenticationStatus("That username does not exist!", Color.red);
            }
            else if (status == LoginResponse.WrongPassword)
            {
                SetServerAuthenticationStatus("Wrong password!", Color.red);
            }
            else if (status == LoginResponse.AccountAlreadyUsedByAnotherPlayer)
            {
                SetServerAuthenticationStatus("That username is already logged on to this server!", Color.yellow);
            }
            else if (status == LoginResponse.AccountBanned)
            {
                SetServerAuthenticationStatus("That account has been banned from this server!", new Color(1.0f, 0.65f, 0.0f));
            }
        }
        else if (signafier == ServerToClientSignifier.CreateResponse)
        {
            int status = int.Parse(data[1]);

            if (status == CreateResponse.Success)
            {
                SetServerAuthenticationStatus("Account creation success!", Color.green);
            }
            else if (status == CreateResponse.UsernameTaken)
            {
                SetServerAuthenticationStatus("That username is already taken!", Color.red);
            }
            
        }
        else if (signafier == ServerToClientSignifier.GameSessionStarted)
        {
            // Start recording the session
            // eventually have a checkbox if this should be recorded or not.
            if (!isObserver)
                gameMgr.StartRecording();

            gameMgr.ChangeGameState(GameStates.PlayingTicTacToe);
            gameMgr.mychar = data[2][0];
            gameMgr.playersturn = data[3][0];
            playerNumber = int.Parse(data[4]);


            if (gameMgr.mychar == gameMgr.playersturn)
                SetSessionStatus("Its your turn pick a slot", Color.white);
            else
                SetSessionStatus("Waiting on player to pick a slot", Color.white);
        }
        else if (signafier == ServerToClientSignifier.OpponentTicTacToePlay)
        {
            isObserver = false;
        }
        else if (signafier == ServerToClientSignifier.UpdateBoardOnClientSide)
        {
            try
            {

                int index = 2;
                for (int i = 0; i < 9; i++)
                {
                    boardGameView.slots[i].SetSlot(data[index]);
                    index++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error when updating board on client: " + e.Message);
            }

            if (!isObserver)
                gameMgr.playersturn = data[1][0];   // parsing to char is just a matter of using string at index whatever

            // check if there is a final outcome
            GameResult result = gameMgr.CheckGameResult();

            if (!isObserver)
            {
                switch (result)
                {
                    case GameResult.PlayerX:



                        Debug.Log("here 1");
                        if (gameMgr.mychar == 'X')
                            SetSessionStatus("You win!", Color.white);
                        else
                            SetSessionStatus("You lose!", Color.white);
                        gameMgr.StopRecording();
                        break;

                    case GameResult.PlayerO:


                        Debug.Log("here 2");
                        if (gameMgr.mychar == 'O')
                            SetSessionStatus("You win", Color.white);
                        else
                            SetSessionStatus("You lose", Color.white);
                        gameMgr.StopRecording();
                        break;

                    case GameResult.Tie:
                        Debug.Log("here 3");


                        SetSessionStatus("Its a tie game", Color.white);
                        gameMgr.StopRecording();
                        break;

                    case GameResult.NothingDetermined:

                        Debug.Log("here 4");
                        if (gameMgr.mychar == gameMgr.playersturn)
                            SetSessionStatus("Its your turn pick a slot", Color.white);
                        else
                            SetSessionStatus("Waiting on player to pick a slot", Color.white);

                        break;
                }
            }
            else
            {
                // Observers can see which player wins, but they can't win or lose themselves because 
                // they dont play the game.
                switch (result)
                {
                    case GameResult.PlayerX:                        
                        SetSessionStatus("Player X wins", Color.white);
                        break;

                    case GameResult.PlayerO:
                        SetSessionStatus("Player O wins", Color.white);
                        break;

                    case GameResult.Tie:
                        SetSessionStatus("Its a tie game", Color.white);
                        break;

                    case GameResult.NothingDetermined:
                        SetSessionStatus("Observers are not authorized to play", Color.white);
                        break;
                }
            }
            

            
        }
        else if (signafier == ServerToClientSignifier.VerifyConnection)
        {
            gameMgr.connectionSuccessful = true;
            gameMgr.ChangeGameState(GameStates.Login);
        }
        else if (signafier == ServerToClientSignifier.MessageToClient)
        {
            gameMgr.SpawnText(data[1]);
        }
        else if (signafier == ServerToClientSignifier.UpdateSessions)
        {
            int numSessions = int.Parse(data[1]);

            sessionViews.Clear();
            
            for (int i = 0; i < numSessions; i++)            
                sessionViews.Add(new Sessions(i));
            
            // This will be handled when we join observer session game state otherwise
            // we cant spawn objects under the parent of an object if its disabled 
            // aka the disabled observer menu in any other game state
            if (gameMgr.currentGameState == GameStates.FindingObserver)
            {
                RefreshSessions();
            }

        }
        else if (signafier == ServerToClientSignifier.ConfirmObserver)
        {
            isObserver = true;
            gameMgr.ChangeGameState(GameStates.PlayingTicTacToe);
            SetSessionStatus("Observers are not authorized to play", Color.white);
        }
        else if (signafier == ServerToClientSignifier.PlayerDisconnected)
        {
            Debug.Log("Were right here");
            isObserver = false;
            gameMgr.ChangeGameState(GameStates.DisconnectionMenu);
        }
        else if (signafier == ServerToClientSignifier.SendRecording)
        {

            Debug.Log("Adding record to queue . . .");
            int index = 2;
            int numSubDivisions = int.Parse(data[1]);
            for (int i = 0; i < numSubDivisions; i++)
            {
                string[] gameData = data[index].Split('+');

                string[] boardData = gameData[0].Split('|');
                Record r = new Record();

                // using index 0 will allow you to get the character in the string (index 0)
                r.slots[0] = boardData[0][0];  // characters
                r.slots[1] = boardData[1][0];  // characters
                r.slots[2] = boardData[2][0];  // characters
                r.slots[3] = boardData[3][0];  // characters
                r.slots[4] = boardData[4][0];  // characters
                r.slots[5] = boardData[5][0];  // characters
                r.slots[6] = boardData[6][0];  // characters
                r.slots[7] = boardData[7][0];  // characters
                r.slots[8] = boardData[8][0];  // characters

                // Server response status (the text on screen above the board)
                r.serverResponse = boardData[9];

                string[] textData = gameData[1].Split('|');
                foreach (string s in textData)
                {
                    r.messages.Add(s);
                }

                index++;

                tempRecords.Add(r);
            }
        }
        else if (signafier == ServerToClientSignifier.QueueEndOfRecord)
        {
            Debug.Log("End of records queued. . .");

            Recording r = new Recording();

            r.timeRecorded = data[1];
            r.username = data[2];

            Record[] _tempRecords = new Record[tempRecords.Count];
            tempRecords.CopyTo(_tempRecords, 0);

            foreach (Record temp in _tempRecords)
                r.records.Add(temp);

            recordings.Add(r);

            tempRecords.Clear();
        }
        else if (signafier == ServerToClientSignifier.QueueStartOfRecordings)
        {
            recordings.Clear();
            tempRecords.Clear();
        }
        else if (signafier == ServerToClientSignifier.QueueEndOfRecordings)
        {
            if (gameMgr.currentGameState == GameStates.ReplayMenu)
                gameMgr.RefreshRecordings();
        }
        else if (signafier == ServerToClientSignifier.KickPlayer)
        {
            if (gameMgr.currentGameState == GameStates.PlayingTicTacToe)
            {
                string _msg = ClientToServerSignifier.LeaveSession.ToString() + "," + isObserver + ",";
                SendMessageToHost(_msg);


                gameroomstatus.gameObject.SetActive(false);
                onfindsessionbtn.gameObject.SetActive(true);
                observebtn.gameObject.SetActive(true);
                replayListViewbtn.gameObject.SetActive(true);
                quitbtn.gameObject.SetActive(true);
            }

            gameMgr.ChangeGameState(GameStates.Login);
            gameMgr.user = string.Empty;
            gameMgr.password = string.Empty;

            gameMgr.inputFieldUserName.GetComponent<InputField>().text = string.Empty;
            gameMgr.inputFieldPassword.GetComponent<InputField>().text = string.Empty;


            SetServerAuthenticationStatus("You have been kicked from the server!", new Color(1.0f, 0.65f, 0.0f));

        }
    }
    
    public void OnUserDisconnected()
    {
        isObserver = false;

        gameroomstatus.gameObject.SetActive(false);
        onfindsessionbtn.gameObject.SetActive(true);
        observebtn.gameObject.SetActive(true);
        replayListViewbtn.gameObject.SetActive(true);

        quitbtn.gameObject.SetActive(true);

        //netclient.board.Reset();

        gameMgr.ChangeGameState(GameStates.WaitingForMatch);

    }
    public void RefreshSessions()
    {
        // We cant spawn items on a gameobject if its inactive (were not in that state)
        // so we need some decision making on when to add it

        GameObject[] sessions = GameObject.FindGameObjectsWithTag("ObserverSession");
        
        foreach (GameObject view in sessions)
            Destroy(view);

        foreach(Sessions s in sessionViews)
        {
            GameObject go = Instantiate(gameMgr.sessionPrefab, gameMgr.observerParent.transform);
            go.GetComponent<SessionView>().index = s.index;
            go.GetComponentInChildren<Text>().text = "Session #" + s.index.ToString();
        }
    }
    // we have white as a default parameter because this a general message
    public void SetSessionStatus(string txt, Color col)
    {
        sessionstatus.text = txt;
        sessionstatus.color = col;
    }

    // this function is used for notifying the player about authentication
    public void SetServerAuthenticationStatus(string txt, Color col)
    {
        gameMgr.serverStatus.GetComponent<Text>().text = txt;
        gameMgr.serverStatus.GetComponent<Text>().color = col;

    }

    public bool IsConnected()
    {
        return isConnected;
    }
    

}