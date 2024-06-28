
using System;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using TMPro;

public class SocketComs : MonoBehaviour
{
    [SerializeField] private TMP_InputField connectionAddress;
    [SerializeField] private TMP_InputField message;
    
    private TcpClient _client;
    private NetworkStream _stream;

    public void InitializeSocketConnection()
    {
        _client = new TcpClient(connectionAddress.text, 65432);
        _stream = _client.GetStream();
    }
    
    public void SendMessageToServer()
    {
        var data = Encoding.ASCII.GetBytes(message.text);
        _stream.Write(data, 0, data.Length);
    }

    private void OnApplicationQuit()
    {
        _stream.Close();
        _client.Close();
    }
}
