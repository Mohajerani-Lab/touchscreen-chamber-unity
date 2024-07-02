using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionHandler : MonoBehaviour
{
    public static ConnectionHandler instance;

    [Header("TCP Address")]
    public string m_TCPAddress = "raspberrypi.local";
    
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
    public bool Connect()
    {
        bool connected = false;
        foreach (var connection in m_Connections)
        {
            switch (connection)
            {
                case TCPSocket tcpConnection:
                    connected=tcpConnection.Connect(m_TCPAddress);
                    break;
                default:
                    Debug.LogError("Connection type not supported");
                    break;
            }
        }
        return connected;
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
    public void SendRewardAndPunishDisable()
    {
        Send("reward_and_punish_disable");
    }
    public void SendIREnable(){
        Send("ir_enable");
    }
    public void SendIRDisable(){
        Send("ir_disable");
    }
    public void SendStartRecording(){
        Send("start_recording");
    }
    public void SendStopRecording(){
        Send("stop_recording");
    }

}