using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Logger : MonoBehaviour
{
    private string _logMsg = "";
    private TextMeshProUGUI _logDisplay;

    private void Start()
    {
        _logDisplay = gameObject.GetComponentInChildren<TextMeshProUGUI>();
        Debug.Log("Logger initialized.");
    }

    private void OnEnable()
    {
        Application.logMessageReceivedThreaded += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        var newString = $"[ {type} ] [ {DateTime.Now} ] {logString} \n";
        if (type == LogType.Exception)
        {
            newString = stackTrace + "\n";
        }

        _logMsg += newString;
        // _logDisplay.text += newString;
    }

    private void Update()
    {
        _logDisplay.text = _logMsg;
    }
}


// using System;
// using UnityEngine;
// using System.Collections;
//  
// public class Logger : MonoBehaviour
// {
//     string myLog;
//     Queue myLogQueue = new Queue();
//
//     void OnEnable () {
//         Application.logMessageReceivedThreaded += HandleLog;
//     }
//      
//     void OnDisable () {
//         Application.logMessageReceived -= HandleLog;
//     }
//  
//     void HandleLog(string logString, string stackTrace, LogType type){
//         myLog = logString;
//         string newString = "\n [" + type + "] : " + myLog;
//         myLogQueue.Enqueue(newString);
//         if (type == LogType.Exception)
//         {
//             newString = "\n" + stackTrace;
//             myLogQueue.Enqueue(newString);
//         }
//         myLog = string.Empty;
//         foreach(string mylog in myLogQueue){
//             myLog += mylog;
//         }
//     }
//  
//     void OnGUI () {
//         GUILayout.Label(myLog);
//     }
// }