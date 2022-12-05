using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.EventSystems;

public class WindowController : MonoBehaviour, IPointerClickHandler
{
    public ObjectType Type;


    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance.menuCanvas.activeSelf) return;
        if (GameManager.Instance.NoInputRequired) return;
        if (GameManager.Instance.InputReceived) return;

        
        switch (Type)
        {
            case ObjectType.Reward:
                if (GameManager.Instance.SectionCount == 2) break;
                GameManager.Instance.experimentStarted = true;
                StartCoroutine(GameManager.Instance.IssueReward(GameManager.Instance.Reward));
                break;
            case ObjectType.Punish:
                if (GameManager.Instance.SectionCount == 2) break;
                GameManager.Instance.experimentStarted = true;
                StartCoroutine(GameManager.Instance.IssuePunish(GameManager.Instance.Punish));
                break;
            case ObjectType.Neutral:
                if (!GameManager.Instance.PunishOnEmpty) break;
                GameManager.Instance.experimentStarted = true;
                StartCoroutine(GameManager.Instance.IssuePunish(GameManager.Instance.Punish));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
