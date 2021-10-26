using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[System.Serializable]
public class BoardView
{
    public List<TicTacToeSlot> slots;


    public void Refresh()
    {

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



public class NetworkedClient : MonoBehaviour
{
    // A list of game sessions to choose from
    List<Sessions> sessionViews = new List<Sessions>();

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
    public Text sessionstatus;
    public Text gameroomstatus;
    public BoardView board = new BoardView();
    
    public bool isObserver = false;

    // Start is called before the first frame update
    void Start()
    {
        gameMgr = FindObjectOfType<GameManager>();

        onfindsessionbtn.onClick.AddListener(OnFindSession);
        observebtn.onClick.AddListener(OnLookForObserver);

        //gameMgr.ChangeGameState(GameStates.FindingObserver);

    }
    public void OnFindSession()
    {
        Debug.Log("Finding session . . .");
        SendMessageToHost(ClientToServerSignifier.AddToGameSessionQueue.ToString() + ",");

        gameroomstatus.gameObject.SetActive(true);

        onfindsessionbtn.gameObject.SetActive(false);
        observebtn.gameObject.SetActive(false);

        gameMgr.ChangeGameState(GameStates.WaitingForMatch);

    }
    public void OnLookForObserver()
    {
        gameMgr.ChangeGameState(GameStates.FindingObserver);
    }
    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.S))
        //{
        //    GameObject go = Instantiate(gameMgr.sessionPrefab, gameMgr.observerParent.transform);
        //}
            //SendMessageToHost(ClientToServerSignifier.TicTacToePlay + ",");

        UpdateNetworkConnection();


    }

    private void UpdateNetworkConnection()
    {
        if (isConnected)
        {
            int recHostID;
            int recConnectionID;
            int recChannelID;
            byte[] recBuffer = new byte[1024];
            int bufferSize = 1024;
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
                //gameMgr.findSessionUI.gameObject.SetActive(true);
                //gameMgr.loginUI.gameObject.SetActive(false);
            
            
            }
            else if (status == LoginResponse.WrongName)
            {
                SetServerAuthenticationStatus("That username does not exist!", Color.red);
            }
            else if (status == LoginResponse.WrongPassword)
            {
                SetServerAuthenticationStatus("Wrong password!", Color.red);
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
            gameMgr.ChangeGameState(GameStates.PlayingTicTacToe);
            //sessionstatus.text = "We are ready: " + data[1];
            gameMgr.mychar = data[2][0];
            gameMgr.playersturn = data[3][0];

            if (gameMgr.mychar == gameMgr.playersturn)
                SetSessionStatus("Its your turn, pick a slot!", Color.white);
            else
                SetSessionStatus("Waiting on player to pick a slot...", Color.white);
            //Debug.Log("WE ARE READY");
        }
        else if (signafier == ServerToClientSignifier.OpponentTicTacToePlay)
        {
            isObserver = false;
            //sessionstatus.text = data[1];
            //Debug.Log("OPPONENT TIC TAC TOE PLAY");
        }
        else if (signafier == ServerToClientSignifier.UpdateBoardOnClientSide)
        {
            try
            {

                int index = 2;
                for (int i = 0; i < 9; i++)
                {
                    board.slots[i].SetSlot(data[index]);
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
                        break;

                    case GameResult.PlayerO:
                        Debug.Log("here 2");
                        if (gameMgr.mychar == 'O')
                            SetSessionStatus("You win!", Color.white);
                        else
                            SetSessionStatus("You lose!", Color.white);
                        break;

                    case GameResult.Tie:
                        Debug.Log("here 3");

                        SetSessionStatus("Its a tie game!", Color.white);
                        break;

                    case GameResult.NothingDetermined:

                        Debug.Log("here 4");
                        if (gameMgr.mychar == gameMgr.playersturn)
                            SetSessionStatus("Its your turn, pick a slot!", Color.white);
                        else
                            SetSessionStatus("Waiting on player to pick a slot...", Color.white);

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
                        SetSessionStatus("Player X wins!", Color.white);
                        break;

                    case GameResult.PlayerO:
                        SetSessionStatus("Player O wins!", Color.white);
                        break;

                    case GameResult.Tie:
                        SetSessionStatus("Its a tie game!", Color.white);
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
            int index = 2;

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
    }
    public void RefreshSessions()
    {
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