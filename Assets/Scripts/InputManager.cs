using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using Unity.VisualScripting;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private NewGameManager GM;

    private void Start()
    {
        GM = NewGameManager.Instance;
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
        if (!Physics.Raycast(GM.MainCamera.ScreenPointToRay(Input.mousePosition), out var raycastHitInfo)) return;

        var goType = raycastHitInfo.collider.gameObject.GetComponentInParent<ObjectController>().Type;

        GM.experimentStarted = true;

        switch (goType)
        {
            case ObjectType.Reward:
                GM.ExperimentPhase = ExperimentPhase.Reward;
                break;
            case ObjectType.Punish:
                GM.ExperimentPhase = ExperimentPhase.Punish;
                break;
            case ObjectType.Neutral:
                if (!GM.PunishOnEmpty) break;
                GM.ExperimentPhase = ExperimentPhase.Punish;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
