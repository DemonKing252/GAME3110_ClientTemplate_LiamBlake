using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class NetworkedClient : MonoBehaviour
{
    public int MaxConnections = 16;

    // Message is guaranteed however it may not be in order
    public int ReliableConnection;

    // Message is not guaranteed, may not be in order (thus the name 'unreliable')
    public int UnrealiableConnection;

    public int port = 5491;

    public int hostID;
    public int connectionID;
    public byte errors;
    public bool connected = false;

    public Text latestMessage;

    private void Start()
    {
        // TODO: 
        // 1. Establish a connection
        // 2. Print a message when a client joins

        TryConnection();

    }

    private void Update()
    {
        HandleMessages();
    }

    private bool isConnected = false;

    private void HandleMessages()
    {
        if (Input.GetKey(KeyCode.S))
        {
            if (connected)
            {
                string strmessage = string.Format("Hello from client host id: {0}, connection id: {1}", hostID, connectionID);
                byte[] msgbyte = Encoding.Unicode.GetBytes(strmessage);


                NetworkTransport.Send(hostID, connectionID, ReliableConnection, msgbyte, msgbyte.Length, out errors);
            }
        }





        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error = 0;

        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID,
            out recConnectionID,
            out recChannelID,
            recBuffer,
            bufferSize,
            out dataSize,
            out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.ConnectEvent:
                //Debug.Log("Connecting to server . . .");
                latestMessage.text = "Client connecting: " + recConnectionID.ToString();

                //Debug.Log("Client connecting: " + recConnectionID.ToString());
                break;
            case NetworkEventType.DataEvent:


                latestMessage.text = "Server says: " + Encoding.Unicode.GetString(recBuffer);

                // Do what you want with data here:
                print("Client says: " + Encoding.Unicode.GetString(recBuffer));

                break;
            case NetworkEventType.DisconnectEvent:
                latestMessage.text = "Client connecting: " + recConnectionID.ToString();

                //Debug.Log("Connecting to server . . .");
                //Debug.Log("Client disconnecting: " + recConnectionID.ToString());
                break;
        }


    }
    private void OnApplicationQuit()
    {
        string str = "Good bye from client!";
        byte[] buffer = Encoding.Unicode.GetBytes(str);
        NetworkTransport.Send(hostID, connectionID, ReliableConnection, buffer, buffer.Length, out errors);

    }

    private void TryConnection()
    {
        NetworkTransport.Init();

        ConnectionConfig config = new ConnectionConfig();

        // https://docs.unity3d.com/ScriptReference/Networking.QosType.html

        // Quality of service: Messages are guaranteed, but may not be in order
        ReliableConnection = config.AddChannel(QosType.Reliable);

        // Quality of service: Messages are not guaranteed, and may not be in order
        UnrealiableConnection = config.AddChannel(QosType.Unreliable);

        /*
        Host topology: 
        (1) how many connection with default config will be supported
        (2) what will be special connections (connections with config different from default). 
         
        */

        HostTopology hostTop = new HostTopology(config, MaxConnections);
        hostID = NetworkTransport.AddHost(hostTop, 0); 

        connectionID = NetworkTransport.Connect(hostID, "192.168.50.75", port, 0, out errors);


        if ((NetworkError)errors == NetworkError.Ok)
        {
            Debug.Log("Connected!");
            connected = true;
        }
        else
        {
            connected = false;
            Debug.LogError("NetworkedClient.cs: Could not connect to server!, error type: " + (NetworkError)errors);

        }
    }
}
