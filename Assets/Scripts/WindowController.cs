using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.EventSystems;

public class WindowController : MonoBehaviour, IPointerClickHandler
{
    public ObjectType Type;
    private GameManager GM;

    private void Start()
    {
        GM = GameManager.Instance;
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
