using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using Unity.VisualScripting;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private GameManager GM;

    private void Start()
    {
        GM = GameManager.Instance;
    }

    private void Update()
    {
        if (GM.menuCanvas.activeSelf) return;
        if (GM.NoInputRequired) return;
        if (GM.SectionCount == 4) return;
        if (!GM.ExperimentPhase.Equals(ExperimentPhase.Trial)) return;
        CheckClick();
    }

    private void CheckClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (GM.RewardHasFixedPlace) return;
        if (!Physics.Raycast(GM.MainCamera.ScreenPointToRay(Input.mousePosition), out var raycastHitInfo)) return;
        
        var goType = raycastHitInfo.collider.gameObject.GetComponentInParent<ObjectController>().Type;

        // GM.experimentStarted = true;

        switch (goType)
        {
            case ObjectType.Reward:
                GM.ExperimentPhase = ExperimentPhase.Reward;
                break;
            case ObjectType.Timeout:
                GM.ExperimentPhase = ExperimentPhase.Timeout;
                ConnectionHandler.instance.SendTimeOutEnable();
                break;
            case ObjectType.Neutral:
                if (!GM.TimeOutOnEmpty) break;
                GM.ExperimentPhase = ExperimentPhase.Timeout;
                ConnectionHandler.instance.SendTimeOutEnable();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
