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
            _client.Connect(adress, 65432);
            _stream = _client.GetStream();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
            return false;
        }
    }

    public bool CheckConnection()
    {
        if (_client == null)
            return false;
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


