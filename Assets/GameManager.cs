using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//GameObject[] gos = GameObject.FindObjectsOfType<GameObject>();

//foreach(GameObject go in gos)
//{
//    if (go.name == "SubmitButton")
//        inputFieldUserName = go;
//    else if (go.name == "CreateToggle")
//        inputFieldPassword = go;
//    else if (go.name == "LoginToggle")
//        toggleLogin = go;
//    else if (go.name == "CreateToggle")
//        toggleCreate = go;
//    else if (go.name == "SubmitButton")
//        buttonSubmit = go;
//}
public static class ClientToServerSignifier
{
    public const int Login = 1;
    public const int CreateAccount = 2;

}
public static class ServerToClientSignifier
{
    public const int LoginResponse = 101;
    public const int CreateResponse = 102;

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




public class GameManager : MonoBehaviour
{
    public GameObject inputFieldUserName;
    public GameObject inputFieldPassword;

    public GameObject createAccountBtn;
    public GameObject loginAccountBtn;


    private NetworkedClient client;

    public GameObject serverStatus;


    // Start is called before the first frame update
    void Start()
    {
        createAccountBtn.GetComponent<Button>().onClick.AddListener(OnCreateAccount);
        loginAccountBtn.GetComponent<Button>().onClick.AddListener(OnLogin);

        inputFieldUserName.GetComponent<InputField>().onEndEdit.AddListener(OnInputUser);
        inputFieldPassword.GetComponent<InputField>().onEndEdit.AddListener(OnInputPassword);


        client = FindObjectOfType<NetworkedClient>();

    }
    private string user;
    private string password;


    public void OnCreateAccount()
    {
        //if (toggleCreate.GetComponent<Toggle>().isOn)
        string msg = ClientToServerSignifier.CreateAccount.ToString() + "," + user + "," + password;
        client.SendMessageToHost(msg);

    }
    public void OnLogin()
    {
        string msg = ClientToServerSignifier.Login.ToString() + "," + user + "," + password;
        client.SendMessageToHost(msg);
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
