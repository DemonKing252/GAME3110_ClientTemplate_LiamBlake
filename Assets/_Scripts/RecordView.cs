using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class RecordView : MonoBehaviour
{
    public int index = -1;
    public Text recordingText;

    // Start is called before the first frame update
    void Start()
    {
        recordingText = GetComponentInChildren<Text>();

    }

    // Update is called once per frame
    void Update()
    {
    }
}
