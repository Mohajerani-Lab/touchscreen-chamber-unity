using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using TMPro;
using UnityEngine;

public class Client : MonoBehaviour
{
    public static Client Instance;
    public static SocketIOUnity Socket;
    private string _id;
    private string _clientName = "LabCage1";
    [SerializeField] private string uri = "http://localhost:3000";
    [SerializeField] private PlayerController controller;

    // public Client()
    // {
    //     if (Socket )
    // }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        Socket = new SocketIOUnity(new Uri(uri), new SocketIOOptions
        {
            Query = new Dictionary<string, string>
            {
                {"token", "UNITY"}
            },
            EIO = 4,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });
        Socket.JsonSerializer = new NewtonsoftJsonSerializer();
        Socket.unityThreadScope = SocketIOUnity.UnityThreadScope.Update;
        Socket.Connect();

        Socket.OnConnected += (_, e) => { Debug.Log("Connected to server!"); };
        Socket.OnDisconnected += (_, s) => { Debug.Log("Disconnected from server!"); };

        Socket.On("connection", _ => { RegisterClient(); });
        Socket.On("broadcast", msg => { Debug.Log(msg.GetValue()); });
        Socket.On("echo", msg => { Debug.Log(msg.GetValue()); });
        Socket.On("register", msg =>
        {
            if (msg.Count <= 0) return;
            _id = (string) JObject.Parse(msg.GetValue().GetRawText())["id"];
            Debug.Log(msg.GetValue());
            Debug.Log($"Client id is {_id}.");
        });

        Socket.On("rotate", _ =>
        {
            Debug.Log("Rotate command received");
            controller.rotateEnabled = !controller.rotateEnabled;
        });
    }

    public void EmitHello(string data)
    {
        Socket.Emit("echo", data);
    }

    private void RegisterClient()
    {
        Debug.Log("Registering Client...");
        Socket.Emit("register", _clientName);
    }

    public void RenameClient(TMP_InputField newName)
    {
        _clientName = newName.text;
        newName.text = "";
        Socket.Emit("rename", _clientName);
    }

    private void OnApplicationQuit()
    {
        Socket.Disconnect();
    }
}
