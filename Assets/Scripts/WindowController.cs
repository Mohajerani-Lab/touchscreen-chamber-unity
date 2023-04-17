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
    public Image BgImage;
    
    private bool _isBlinking;
    private long _blinkToggleDuration;
    private long _blinkToggleTime;


    private void Start()
    {
        GM = GameManager.Instance;
        FM = FeedbackManager.Instance;
        _isBlinking = false;
        BgImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (!_isBlinking)
            return;

        var currentTime = Utils.CurrentTimeMillis();

        if (currentTime < _blinkToggleTime + _blinkToggleDuration)
            return;
        
        _blinkToggleTime = currentTime;
        
        BgImage.color = new Color(BgImage.color.r, BgImage.color.g, BgImage.color.b, 1 - BgImage.color.a);
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
                if (GM.SectionCount == 2) break;
                // GM.experimentStarted = true;
                if (GM.InTwoPhaseBlink)
                {
                    FM.IsBlinkPhaseOneReward = !FM.IsBlinkPhaseOneReward;
                }
                GM.ExperimentPhase = ExperimentPhase.Reward;
                break;
            case ObjectType.Punish:
                if (GM.SectionCount == 2) break;
                // GM.experimentStarted = true;
                GM.ExperimentPhase = ExperimentPhase.Punish;
                break;
            case ObjectType.Neutral:
                if (!GM.PunishOnEmpty) break;
                // GM.experimentStarted = true;
                GM.ExperimentPhase = ExperimentPhase.Punish;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
