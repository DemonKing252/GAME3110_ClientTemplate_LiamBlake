using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public static class GameStates
{
    public const int Login = 1;

    public const int MainMenu = 2;
    
    public const int WaitingForMatch = 3;
    
    public const int PlayingTicTacToe = 4;

    public const int ConnectingToHost = 5;

    public const int FindingObserver = 6;

    public const int DisconnectionMenu = 7;
    
}

public enum GameResult
{
    PlayerX,
    PlayerO,
    Tie,
    NothingDetermined

}


public class GameManager : MonoBehaviour
{
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

    public Text connectionVerificationStatus;
    public InputField ipaddress;
    public InputField portNumber;
    public GameObject textPrefab;

    public GameObject inputFieldMessage;
    public GameObject sendBtn;
    public GameObject sessionPrefab;

    public char mychar = 'X';
    public char playersturn = 'X';

    public bool sendtogamesession = true;
    public bool sendtoobservers = true;
    public bool sendtotherclients = true;

    [HideInInspector]
    public string sendmsg;

    [HideInInspector]
    public int currentGameState = 1;

    private List<GameObject> textMessages = new List<GameObject>();

    public GameObject observerParent;


    // Start is called before the first frame update
    void Start()
    {
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
    private string user;
    private string password;

    [HideInInspector]
    public bool connectionSuccessful = false;

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
        BoardView b = netclient.board;
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
        foreach(TicTacToeSlot slot in netclient.board.slots)
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
        }
        else if (newState == GameStates.MainMenu)
        {
            disconnectUI.gameObject.SetActive(false);
            searchingObserver.gameObject.SetActive(false);
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(true);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(false);

        }
        else if (newState == GameStates.WaitingForMatch)
        {

            disconnectUI.gameObject.SetActive(false);
            searchingObserver.gameObject.SetActive(false);
            findSessionUI.gameObject.SetActive(true);
            loginUI.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(false);
        }
        else if (newState == GameStates.PlayingTicTacToe)
        {
            disconnectUI.gameObject.SetActive(false);
            searchingObserver.gameObject.SetActive(false);
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(true);
            connectToHostUI.gameObject.SetActive(false);

            netclient.board._Reset();
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
        }
        else if (newState == GameStates.FindingObserver)
        {

            disconnectUI.gameObject.SetActive(false);
            searchingObserver.gameObject.SetActive(true);
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(false);
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
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ClearTextMessages();
        }
    }
}
