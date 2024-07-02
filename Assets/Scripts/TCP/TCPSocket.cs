using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Text;

public class TCPSocket : IConnection
{
    private TcpClient _client;
    private NetworkStream _stream;
    private string _adress;

    /// <summary>
    /// connect to the server and return true if the connection is successful
    /// </summary>
    /// <returns></returns>
    /// <param name="adress">The adress of the server</param>
    public bool Connect(string adress)
    {
        _adress = adress;
        _client = new TcpClient();
        try
        {
            _client.Connect(adress, 80);
            _stream = _client.GetStream();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            return false;
        }
    }


    public bool CheckConnection()
    {
        return _client.Connected;
    }

    public bool Reconnect()
    {
        return Connect(_adress);
    }


    public void Send(string message)
    {
        var data = Encoding.ASCII.GetBytes(message);
        _stream.Write(data, 0, data.Length);
    }
}


