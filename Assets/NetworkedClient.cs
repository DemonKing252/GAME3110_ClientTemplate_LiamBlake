using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

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

    // Start is called before the first frame update
    void Start()
    {
        gameMgr = FindObjectOfType<GameManager>();

        Connect();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
            SendMessageToHost("Hello from client");

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

    private void Connect()
    {

        if (!isConnected)
        {
            Debug.Log("Attempting to create connection");

            NetworkTransport.Init();

            ConnectionConfig config = new ConnectionConfig();
            reliableChannelID = config.AddChannel(QosType.Reliable);
            unreliableChannelID = config.AddChannel(QosType.Unreliable);
            HostTopology topology = new HostTopology(config, maxConnections);
            hostID = NetworkTransport.AddHost(topology, 0);
            Debug.Log("Socket open.  Host ID = " + hostID);

            connectionID = NetworkTransport.Connect(hostID, ip, socketPort, 0, out error); // server is local on network

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
                gameMgr.serverStatus.GetComponent<Text>().text = "Login Successful!";
                gameMgr.serverStatus.GetComponent<Text>().color = Color.green;

            }
            else if (status == LoginResponse.WrongName)
            {

                gameMgr.serverStatus.GetComponent<Text>().text = "That username does not exist!";
                gameMgr.serverStatus.GetComponent<Text>().color = Color.red;

            }
            else if (status == LoginResponse.WrongPassword)
            {

                gameMgr.serverStatus.GetComponent<Text>().text = "Wrong password!";
                gameMgr.serverStatus.GetComponent<Text>().color = Color.red;

            }
        }
        else if (signafier == ServerToClientSignifier.CreateResponse)
        {
            int status = int.Parse(data[1]);

            if (status == CreateResponse.Success)
            {
                gameMgr.serverStatus.GetComponent<Text>().text = "Create account successful!";
                gameMgr.serverStatus.GetComponent<Text>().color = Color.green;

            }
            else if (status == CreateResponse.UsernameTaken)
            {

                gameMgr.serverStatus.GetComponent<Text>().text = "That username is already taken!";
                gameMgr.serverStatus.GetComponent<Text>().color = Color.red;

            }
            
        }
        
    }

    public bool IsConnected()
    {
        return isConnected;
    }


}