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

public class NetworkedClient : MonoBehaviour
{


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
    public Text sessionstatus;
    public Text gameroomstatus;
    public BoardView board = new BoardView();
    // Start is called before the first frame update
    void Start()
    {
        gameMgr = FindObjectOfType<GameManager>();
        onfindsessionbtn.onClick.AddListener(OnFindSession);

    }
    public void OnFindSession()
    {
        Debug.Log("Finding session . . .");
        SendMessageToHost(ClientToServerSignifier.AddToGameSessionQueue.ToString() + ",");

        gameroomstatus.gameObject.SetActive(true);
        onfindsessionbtn.gameObject.SetActive(false);

        gameMgr.ChangeGameState(GameStates.WaitingForMatch);

    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
            SendMessageToHost(ClientToServerSignifier.TicTacToePlay + ",");

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
                SetServerStatus("Login Successful!", Color.green);
                gameMgr.ChangeGameState(GameStates.WaitingForMatch);
                //gameMgr.findSessionUI.gameObject.SetActive(true);
                //gameMgr.loginUI.gameObject.SetActive(false);
            
            
            }
            else if (status == LoginResponse.WrongName)
            {
                SetServerStatus("That username does not exist!", Color.red);
            }
            else if (status == LoginResponse.WrongPassword)
            {
                SetServerStatus("Wrong password!", Color.red);
            }
        }
        else if (signafier == ServerToClientSignifier.CreateResponse)
        {
            int status = int.Parse(data[1]);

            if (status == CreateResponse.Success)
            {
                SetServerStatus("Account creation success!", Color.green);
            }
            else if (status == CreateResponse.UsernameTaken)
            {
                SetServerStatus("That username is already taken!", Color.red);
            }
            
        }
        else if (signafier == ServerToClientSignifier.GameSessionStarted)
        {
            gameMgr.ChangeGameState(GameStates.PlayingTicTacToe);
            sessionstatus.text = "We are ready: " + data[1];
            gameMgr.mychar = data[2][0];
            //Debug.Log("WE ARE READY");
        }
        else if (signafier == ServerToClientSignifier.OpponentTicTacToePlay)
        {
            sessionstatus.text = data[1];
            //Debug.Log("OPPONENT TIC TAC TOE PLAY");
        }
        else if (signafier == ServerToClientSignifier.UpdateBoardOnClientSide)
        {
            try
            {

                int index = 1;
                for (int i = 0; i < 9; i++)
                {
                    board.slots[i].SetSlot(data[index]);
                    index++;
                }
            }
            catch(Exception e)
            {
                Debug.LogError("Error when updating board on client: " + e.Message);
            }
        }
        else if (signafier == ServerToClientSignifier.VerifyConnection)
        {
            gameMgr.connectionSuccessful = true;
            gameMgr.ChangeGameState(GameStates.Login);
        }
    }
    public void SetServerStatus(string txt, Color col)
    {
        gameMgr.serverStatus.GetComponent<Text>().text = txt;
        gameMgr.serverStatus.GetComponent<Text>().color = col;
    }

    public bool IsConnected()
    {
        return isConnected;
    }


}