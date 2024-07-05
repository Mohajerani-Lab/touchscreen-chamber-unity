using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FontsSizeSetter : MonoBehaviour
{
    private const float MAINFONTSIZE = 40f;
    private const float TITLEFONTSIZE = 50f;
    [SerializeField] private bool isTitle = false;
    private TextMeshProUGUI m_Text;

    void Awake()
    {
        m_Text = GetComponent<TextMeshProUGUI>();
        m_Text.enableAutoSizing = false;
        if (isTitle)
        {
            m_Text.fontSize = TITLEFONTSIZE * Screen.width / 1920f;
        }
        else
        {
            m_Text.fontSize = MAINFONTSIZE * Screen.width / 1920f;
        }
    }
}

