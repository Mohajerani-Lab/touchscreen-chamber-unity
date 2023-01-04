using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefaultNamespace;
using TMPro;
using UnityEngine;

public class Logger : MonoBehaviour
{
    private class LogObject
    {
        private readonly string _message;
        private readonly LogType _type;
        private readonly DateTime _timeCreated;

        public LogObject(string message, LogType type, DateTime timeCreated)
        {
            _message = message;
            _type = type;
            _timeCreated = timeCreated;
        }

        public override string ToString()
        {
            return $"[ {_type} ] [ {_timeCreated} ] {_message}\n";
        }

        public string ToCsv()
        {
            return $"{_type},{_timeCreated},{_message.Replace(',', ' ')}";
        }
    }

    private NewGameManager GM;
    private readonly List<LogObject> _logObjects = new List<LogObject>();
    private string _logMsgs = "";
    private string _logMsgsTemp = "";
    [SerializeField] private TextMeshProUGUI logDisplay;
    private string _sessionName;
    private string _reportDirectory;

    private void Start()
    {
        GM = NewGameManager.Instance;
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
        var logObject = new LogObject(type.Equals(LogType.Exception) ? stackTrace : logString, type, DateTime.Now);
        
        _logObjects.Add(logObject);
        
        _logMsgs = logObject + _logMsgs;
        _logMsgsTemp = logObject + _logMsgsTemp;
        logDisplay.text = _logMsgsTemp;
    }

    public void ClearLogDisplay()
    {
        _logMsgsTemp = "";
        logDisplay.text = _logMsgsTemp;
    }

    public void SaveLogsToDisk()
    {
        _reportDirectory = Path.Combine(GM.RootFolder, "Reports");
        if (!Directory.Exists(_reportDirectory))
        {
            Debug.Log("Creating reports folder...");
            Directory.CreateDirectory(_reportDirectory);
        }

        Debug.Log("Saving logs...");
        
        var writer = new StreamWriter( $"{Path.Combine(_reportDirectory, _sessionName)}.csv");
        writer.Write(string.Join('\n', _logObjects.Select(s => s.ToCsv())));
        writer.Close();
    }
}