using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class TogglePopulator : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    private Toggle _toggle;
    private RectTransform _rectTransform;
    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
        _rectTransform = GetComponent<RectTransform>();
        _toggle.group = transform.parent.GetComponent<ToggleGroup>();
        _rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height/14);
    }
    public void Populate(string text)
    {
        _text.text = text;
    }

}
