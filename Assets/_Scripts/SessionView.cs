using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SessionView : MonoBehaviour
{
    private NetworkedClient netclient;
    public int index = -1;
    public UnityEngine.UI.Button sessionBtn;
    void Start()
    {
        netclient = FindObjectOfType<NetworkedClient>();
        sessionBtn = GetComponent<UnityEngine.UI.Button>();
        sessionBtn.onClick.AddListener(OnSessionClicked);    
    }

    public void OnSessionClicked()
    {
        netclient.SendMessageToHost(ClientToServerSignifier.AddToObserverSessionQueue + "," + index.ToString());
    }
}
