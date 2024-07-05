using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionHandler : MonoBehaviour
{
    public static ConnectionHandler instance;

    [Header("TCP Address")]
    public string m_TCPAddress = "raspberrypi.local";
    [SerializeField] private TMP_InputField m_AddressInputField;
    [SerializeField] private Logger m_Logger;
    private int connectionTrial = 0;
    private List<IConnection> m_Connections = new List<IConnection>();
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        m_Connections.Add(new TCPSocket());
        Debug.Log(m_Connections[0].GetType());
    }
    private IEnumerator Update()
    {
        print("Update");
        if (GameManager.Instance.ExperimentPhase != ExperimentPhase.Preprocess)
        {
            if (!CheckConnection())
            {
                Connect();
                yield return new WaitForSeconds(3);
                connectionTrial++;
                if (connectionTrial > 5) 
                {
                    Debug.Log("Connection failed. Exiting application.");
                    m_Logger.SaveLogsToDisk();
                    Application.Quit();
                }
            }
            else
            {
                connectionTrial = 0;
            }

        }
    }
    public bool Connect()
    {
        bool connected = false;
        m_TCPAddress = m_AddressInputField.text;
        foreach (var connection in m_Connections)
        {
            switch (connection)
            {
                case TCPSocket tcpConnection:
                    connected = tcpConnection.Connect(m_TCPAddress);
                    break;
                default:
                    Debug.LogError("Connection type not supported");
                    break;
            }
        }
        return connected;
    }
    public bool CheckConnection()
    {
        foreach (var connection in m_Connections)
        {
            switch (connection)
            {
                case TCPSocket tcpConnection:
                    return tcpConnection.CheckConnection();
                default:
                    Debug.LogError("Connection type not supported");
                    break;
            }
        }
        return false;
    }

    private void Send(string message)
    {
        print("Sending message: " + message);
        foreach (var connection in m_Connections)
        {
            switch (connection)
            {
                case TCPSocket tcpConnection:
                    tcpConnection.Send(message);

                    break;
                default:
                    Debug.LogError("Connection type not supported");
                    break;
            }
        }
    }
    public void SendRewardEnable()
    {
        Send("reward_enable");
    }

    public void SendPunishEnable()
    {
        Send("punish_enable");
    }

    public void SendRewardAndPunishDisable()
    {
        Send("reward_and_punish_disable");
    }
    public void SendIREnable()
    {
        Send("ir_enable");
    }
    public void SendIRDisable()
    {
        Send("ir_disable");
    }
    public void SendStartRecording()
    {
        Send("start_recording");
    }
    public void SendStopRecording()
    {
        Send("stop_recording");
    }

}