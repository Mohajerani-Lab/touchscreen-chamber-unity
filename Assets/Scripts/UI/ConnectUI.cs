using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

public class ConnectUI : MonoBehaviour
{
    [SerializeField] private Color m_ConnectColor;
    [SerializeField] private Color m_DisconnectColor;
    private UnityEngine.UI.Image m_ConnectImage;
    private bool _connectionBuffer=false;
    public void Awake()
    {
        m_ConnectImage = GetComponent<UnityEngine.UI.Image>();
        m_ConnectImage.color = m_DisconnectColor;
    }

    private void Update()
    {
        if(_connectionBuffer!=ConnectionHandler.instance.CheckConnection())
        {
            _connectionBuffer = ConnectionHandler.instance.CheckConnection();
            if (_connectionBuffer)
                m_ConnectImage.color = m_ConnectColor;
            else
                m_ConnectImage.color = m_DisconnectColor;
        }
    }
    public void OnConnectButtonClicked()
    {
        bool isConnected = ConnectionHandler.instance.Connect();
        if (isConnected)
            m_ConnectImage.color = m_ConnectColor;
        else
            m_ConnectImage.color = m_DisconnectColor;
    }
}
