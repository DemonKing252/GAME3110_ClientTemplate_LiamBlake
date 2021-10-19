using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TicTacToeSlot : MonoBehaviour
{
    private Button slotbtn;
    private Text displayText;
    private GameManager gameMgr = null;
    public char characterinslot = ' ';

    // Start is called before the first frame update
    public void OnClicked()
    {
        characterinslot = gameMgr.mychar;
        displayText.text = characterinslot.ToString();
        gameMgr.OnUpdateBoard();
    }
    public void SetSlot(string value)
    {
        characterinslot = value[0];
        displayText.text = characterinslot.ToString();
    }
    
    
    void Start()
    {
        slotbtn = GetComponent<Button>();
        displayText = GetComponentInChildren<Text>();
        gameMgr = FindObjectOfType<GameManager>();

        slotbtn.onClick.AddListener(OnClicked);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
