using System;
using System.IO;
using DefaultNamespace;
using TMPro;
using UnityEngine;

public class Logger : MonoBehaviour
{
    private string _logMsgs = "";
    private string _logMsgsTemp = "";
    [SerializeField] private TextMeshProUGUI logDisplay;
    private string _sessionName;
    private string _reportDirectory;

    private void Start()
    {
        // logDisplay = gameObject.GetComponentInChildren<TextMeshProUGUI>();
        _sessionName = "Touchscreen-Trial-Game-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        Debug.Log($"Logger initialized. Session Name: {_sessionName}");
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

        _logMsgs += newString;
        _logMsgsTemp += newString;
    }

    private void Update()
    {
        logDisplay.text = _logMsgsTemp;
    }

    public void ClearLogDisplay()
    {
        _logMsgsTemp = "";
    }

    public void SaveLogsToDisk()
    {
        _reportDirectory = Path.Combine(GameManager.Instance.RootFolder, "Reports");
        if (!Directory.Exists(_reportDirectory))
        {
            Debug.Log("Creating reports folder...");
            Directory.CreateDirectory(_reportDirectory);
        }

        Debug.Log("Saving logs...");
        
        var writer = new StreamWriter( $"{Path.Combine(_reportDirectory, _sessionName)}.txt");
        writer.Write(_logMsgs);
        writer.Close();
    }

    // private void OnApplicationQuit()
    // {
    //     SaveLogsToDisk();
    // }
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