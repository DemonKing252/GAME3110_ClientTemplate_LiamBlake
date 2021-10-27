using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TicTacToeSlot : MonoBehaviour
{
    private Button slotbtn;

    [HideInInspector]
    public Text displayText;

    private GameManager gameMgr = null;
    private NetworkedClient netclient;
    public char characterinslot = ' ';

    // Start is called before the first frame update
    public void OnClicked()
    {
        if (gameMgr.mychar == gameMgr.playersturn && characterinslot == ' ' && !netclient.isObserver)
        {

            characterinslot = gameMgr.mychar;
            displayText.text = characterinslot.ToString();
            gameMgr.OnUpdateBoard();
            
            
        }
    }
    public void _Reset()
    {

        slotbtn = GetComponent<Button>();
        displayText = GetComponentInChildren<Text>();
        gameMgr = FindObjectOfType<GameManager>();
        netclient = FindObjectOfType<NetworkedClient>();

        slotbtn.onClick.AddListener(OnClicked);


        characterinslot = ' ';
        displayText.text = characterinslot.ToString();
    }

    public void SetSlot(string value)
    {
        characterinslot = value[0];
        displayText.text = characterinslot.ToString();
    }
    
    
    void Start()
    {
        _Reset();
    }

}
