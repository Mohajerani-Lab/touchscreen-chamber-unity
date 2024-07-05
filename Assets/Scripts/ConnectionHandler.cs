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
    [SerializeField] private Image m_PauseImage;
    private int connectionTrial = 0;
    private List<IConnection> m_Connections = new List<IConnection>();
    private bool m_HasConnectedBefore = false;

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
        StartCoroutine(CheckConnectionInGame());
    }

    private IEnumerator CheckConnectionInGame()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            if (m_HasConnectedBefore)
            {
                CheckConnectionAndHandle();
            }
        }
    }

    private void CheckConnectionAndHandle()
    {
        if (GameManager.Instance.ExperimentPhase != ExperimentPhase.Preprocess)
        {
            if (!CheckConnection())
            {
                StartCoroutine(AttemptReconnect());
            }
            else
            {
                ResetConnectionTrial();
            }
        }
    }

    private IEnumerator AttemptReconnect()
    {
        m_PauseImage.gameObject.SetActive(true);
        Connect();
        yield return new WaitForSeconds(3);
        connectionTrial++;
        Debug.Log("Connection trial: " + connectionTrial);
        if (connectionTrial > 5)
        {
            Debug.Log("Connection failed. Exiting application.");
            m_Logger.SaveLogsToDisk();
            Application.Quit();
        }
    }

    private void ResetConnectionTrial()
    {
        m_PauseImage.gameObject.SetActive(false);
        connectionTrial = 0;
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
        if (m_HasConnectedBefore == false)
            m_HasConnectedBefore = connected;
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
        if (!CheckConnection()) return;
        Send("reward_enable");
    }

    public void SendPunishEnable()
    {
        if (!CheckConnection()) return;
        Send("punish_enable");
    }

    public void SendRewardAndPunishDisable()
    {
        if (!CheckConnection()) return;
        Send("reward_and_punish_disable");
    }
    public void SendIREnable()
    {
        if (!CheckConnection()) return;
        Send("ir_enable");
    }
    public void SendIRDisable()
    {
        if (!CheckConnection()) return;
        Send("ir_disable");
    }
    public void SendStartRecording()
    {
        if (!CheckConnection()) return;
        Send("start_recording");
    }
    public void SendStopRecording()
    {
        if (!CheckConnection()) return;
        Send("stop_recording");
    }

}