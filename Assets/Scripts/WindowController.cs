using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.TextCore;
using UnityEngine.UI;

public class WindowController : MonoBehaviour, IPointerClickHandler
{
    public ObjectType Type;
    private GameManager GM;
    private FeedbackManager FM;
    [SerializeField] private Image BgImage;
    
    private bool _isBlinking;
    private long _blinkToggleDuration;
    private long _blinkToggleTime;


    private void Start()
    {
        GM = GameManager.Instance;
        FM = FeedbackManager.Instance;
        Type = ObjectType.Neutral;
        _isBlinking = false;
    }

    private void Update()
    {
        if (!_isBlinking) return;

        if (GM.IsBlinkStatic) return;
        
        var currentTime = Utils.CurrentTimeMillis();

        if (currentTime < _blinkToggleTime + _blinkToggleDuration)
            return;
        
        _blinkToggleTime = currentTime;
        
        BgImage.color = new Color(BgImage.color.r, BgImage.color.g, BgImage.color.b, 1 - BgImage.color.a);
    }

    public void UpdateScale(float coef)
    {
        gameObject.transform.localScale = new Vector3(coef, coef, coef);
        BgImage.rectTransform.localScale = gameObject.transform.localScale.Invert();
    }

    public void UpdateBgImageSize(Vector2 minPose, Vector2 maxPose)
    {
        BgImage.rectTransform.anchorMin = minPose;
        BgImage.rectTransform.anchorMax = maxPose;

        BgImage.rectTransform.offsetMin = Vector2.zero;
        BgImage.rectTransform.offsetMax = Vector2.zero;
    }

    public void StartBlinking(float frequency, float[] color)
    {
        _blinkToggleDuration = (long) (1 / frequency * 1000);
        BgImage.color = new Color(color[0], color[1], color[2], 1);
        _blinkToggleTime = Utils.CurrentTimeMillis();
        
        _isBlinking = true;
    }

    public void StopBlinking()
    {
        BgImage.color = new Color(BgImage.color.r, BgImage.color.g, BgImage.color.b, 0);
        _isBlinking = false;
    }

    public IEnumerator BlinkOnce()
    {
        BgImage.color = new Color(BgImage.color.r, BgImage.color.g, BgImage.color.b, 1);
        yield return new WaitForSeconds((float)_blinkToggleDuration / 1000);
        BgImage.color = new Color(BgImage.color.r, BgImage.color.g, BgImage.color.b, 0);
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (GM.menuCanvas.activeSelf) return;
        if (GM.NoInputRequired) return;
        if (GM.InputReceived) return;

        switch (Type)
        {
            case ObjectType.Reward:
                if (GM.SectionCount == 2 && GM.ExperimentType != "location") break;
                // GM.experimentStarted = true;
                if (GM.InTwoPhaseBlink)
                {
                    FM.IsBlinkPhaseOneReward = !FM.IsBlinkPhaseOneReward;
                }
                GM.ExperimentPhase = ExperimentPhase.Reward;
                break;
            case ObjectType.Timeout:
                if (GM.SectionCount == 2 && GM.ExperimentType != "location") break;
                // GM.experimentStarted = true;
                GM.ExperimentPhase = ExperimentPhase.Timeout;
                ConnectionHandler.instance.SendTimeOutEnable();
                break;
            case ObjectType.Neutral:
                if (!GM.TimeOutOnEmpty) break;
                // GM.experimentStarted = true;
                GM.ExperimentPhase = ExperimentPhase.Timeout;
                ConnectionHandler.instance.SendTimeOutEnable();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
