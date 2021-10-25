using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;


public static class ClientToServerSignifier
{
    public const int Login = 1;
    public const int CreateAccount = 2;

    public const int AddToGameSessionQueue = 3;
    public const int TicTacToePlay = 4;
    public const int UpdateBoard = 5;

    public const int ChatMessage = 6;

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
}
public static class CreateResponse
{
    // 10,000
    public const int Success = 10001;
    public const int UsernameTaken = 10002;
}
public static class GameStates
{
    public const int Login = 1;

    public const int MainMenu = 2;
    
    public const int WaitingForMatch = 3;
    
    public const int PlayingTicTacToe = 4;

    public const int ConnectingToHost = 5;
    
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

    public Canvas findSessionUI;
    public Canvas loginUI;
    public Canvas gameUI;
    public Canvas connectToHostUI;
    public Text connectionVerificationStatus;
    public InputField ipaddress;
    public InputField portNumber;
    public GameObject textPrefab;

    public GameObject inputFieldMessage;
    public GameObject sendBtn;

    public char mychar = 'X';
    public char playersturn = 'X';

    public bool sendtotherclients = true;
    public bool sendtoobservers = true;
    public bool sendtogamesession = true;
    public string sendmsg;

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
    public void OnSendToGameSessionToggle(bool toggle)
    {
        sendtogamesession = toggle;
    }
    public void OnSendToObservers(bool toggle)
    {
        sendtoobservers = toggle;
    }
    public void OnSendToAllOtherClients(bool toggle)
    {
        sendtotherclients = toggle;
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
    public void SpawnText(string msg)
    {
        inputFieldMessage.GetComponent<InputField>().text = "";
        GameObject go = Instantiate(textPrefab, GameObject.FindGameObjectWithTag("TextContent").transform);
        go.GetComponent<Text>().text = " " + msg;
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
        if (newState == GameStates.Login)
        {
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(true);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(false);
        }
        else if (newState == GameStates.MainMenu)
        {
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(true);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(false);

        }
        else if (newState == GameStates.WaitingForMatch)
        {
            findSessionUI.gameObject.SetActive(true);
            loginUI.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(false);
        }
        else if (newState == GameStates.PlayingTicTacToe)
        {
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(true);
            connectToHostUI.gameObject.SetActive(false);
        }
        else if (newState == GameStates.ConnectingToHost)
        {
            findSessionUI.gameObject.SetActive(false);
            loginUI.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(false);
            connectToHostUI.gameObject.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
