using System;
using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography;
using TMPro;

public class SerialComs : MonoBehaviour
{
    // public string portName;
    public static SerialComs Instance;
    private SerialPort _arduino;
    private string _buffer;
    [SerializeField] private TMP_Dropdown serialPortDropdown;
    private AndroidJavaObject serialcomms = null;
    private AndroidJavaObject activityContext = null;


    private void Start()
    {
        if (serialcomms != null) return;
        using (var activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
        }

        using (var pluginClass = new AndroidJavaClass("com.mohajeranilab.serialcomms.Plugin"))
        {
            serialcomms = pluginClass.CallStatic<AndroidJavaObject>("instance");
            serialcomms.Call("setContext", activityContext);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        else
        {
            Instance = this;
        }
    }

    // private void FillSerialPortsDropdown()
    // {
    //     var portNames = SerialPort.GetPortNames();
    //     foreach (var name in portNames)
    //     {
    //         serialPortDropdown.options.Add(new TMP_Dropdown.OptionData(name));
    //     }
    // }

    // public void CheckSerialConnection()
    // {
    //     Debug.Log(serialPortDropdown.value);
    //     if (serialPortDropdown.value == 0) return;
    //     var selectedCOM = serialPortDropdown.options[serialPortDropdown.value].text;
    //     try
    //     {
    //         if (_arduino is { IsOpen: true })
    //         {
    //             _arduino.Close();
    //         }
    //         _arduino = new SerialPort(selectedCOM, 9600);
    //         _arduino.ReadTimeout = 1000;
    //
    //         // _arduino.DataReceived += arduino_DataReceived;
    //         _arduino.Open();
    //         // IsDeviceArduino();
    //         // SendSerialMessage("identify");
    //         Debug.Log($"Successfully connected to {selectedCOM}, name: {_arduino.PortName}");
    //     }
    //     catch (Exception e)
    //     {
    //         Debug.Log(e);
    //         Debug.Log($"Cannot connect to {selectedCOM}, check connections and try again.");
    //     }
    // }

    public void SetupArduino()
    {
        activityContext.Call("runOnUiThread", new AndroidJavaRunnable((() =>
        {
            serialcomms.Call("setupArduino");
        })));
    }

    public void SendMessageToArduino(string message)
    {
        var result = serialcomms.Call<int>("sendMessageToArduino", message);
        if (result < 0)
        {
            Debug.Log("Problem occurred in sending reward");
        }
    }
    
    // private void arduino_DataReceived(object sender, SerialDataReceivedEventArgs e)
    // {
    //     // var intBuffer = _arduino.BytesToRead;
    //     // var byteBuffer = new byte[intBuffer];
    //     // _arduino.Read(byteBuffer, 0, intBuffer);
    //     // var data = new byte[_arduino.BytesToRead];
    //     // _arduino.Read(data, 0, data.Length);
    //     // data.ToList().ForEach(d => Debug.Log(d));
    //     _buffer += _arduino.ReadExisting();
    //
    //     //test for termination character in buffer
    //     if (_buffer.Contains("\n"))
    //     {
    //         Debug.Log(_buffer);
    //     }
    //     // this.Invoke(new EventHandler(DoUpDate));
    // }

    // private bool IsDeviceArduino()
    // {
    //     SendSerialMessage("identify");
    //     
    //     try
    //     {
    //         var res = _arduino.ReadLine();
    //         if (res != "") Debug.Log(res);
    //     }
    //     catch (Exception e)
    //     {
    //         Debug.Log(e);
    //         throw;
    //     }
    //     
    //     // var response = _arduino.ReadLine();
    //     // Debug.Log(response);
    //     // return response.Equals("arduino-mega");
    //     return false;
    // }
    
    // public bool SendSerialMessage(string message)
    // {
    //     if (!_arduino.IsOpen) return false;
    //     _arduino.Write(message + "\n");
    //     return true;
    // }

    private void OnApplicationQuit()
    {
        // if (_arduino is {IsOpen: true})
        // {
        //         _arduino.Close();
        // }
        activityContext.Call("runOnUiThread", new AndroidJavaRunnable((() =>
        {
            serialcomms.Call("closeArduinoConnection");
        })));
        
    }

    public void ShowToastMessage(string message)
    {
        
        activityContext.Call("runOnUiThread", new AndroidJavaRunnable((() =>
        {
            serialcomms.Call("showToastMessage", message);
        })));
    }


    // private void Update()
    // {
    //     if (!_arduino.IsOpen) return;
    //     if (Input.GetKey("1"))
    //     {
    //         _arduino.Write("1");
    //         Debug.Log(1);
    //     }
    //     else if (Input.GetKey("0"))
    //     {
    //         _arduino.Write("0");
    //         Debug.Log(0);
    //     }
    // }
}