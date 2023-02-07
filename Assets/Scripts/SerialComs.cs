using UnityEngine;

public class SerialComs : MonoBehaviour
{
    public static SerialComs Instance;
    // private SerialPort _arduino;
    private string _buffer;
    private AndroidJavaObject serialcomms = null;
    private AndroidJavaObject activityContext = null;
    public bool ArduinoConnected { get; private set; } = false;


    private void Start()
    {
        if (!Application.platform.Equals(RuntimePlatform.Android)) return;

        if (serialcomms != null) return;
        using (var activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
        }

        using (var pluginClass = new AndroidJavaClass("com.mohajeranilab.serialcomms.Plugin"))
        {
            serialcomms = pluginClass.CallStatic<AndroidJavaObject>("instance");
            serialcomms.Call("start", activityContext);
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

    public void SetupArduino()
    {
        activityContext.Call("runOnUiThread", new AndroidJavaRunnable((() =>
        {
            var res = serialcomms.Call<int>("setupArduino");
            if (res != 0) return;
            ArduinoConnected = true;
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

    private void OnApplicationQuit()
    {
        if (!ArduinoConnected) return;
        
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
}