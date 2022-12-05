using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using Unity.VisualScripting;
using UnityEngine;

public class TrialManager : MonoBehaviour
{
    private void Update()
    {
        if (GameManager.Instance.menuCanvas.activeSelf) return;
        if (GameManager.Instance.NoInputRequired) return;
        if (GameManager.Instance.SectionCount == 4) return;
        CheckClick();
    }

    private void CheckClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (!Physics.Raycast(GameManager.Instance.mainCamera.ScreenPointToRay(Input.mousePosition), out var raycastHitInfo)) return;

        var goType = raycastHitInfo.collider.gameObject.GetComponentInParent<ObjectController>().Type;

        GameManager.Instance.experimentStarted = true;

        switch (goType)
        {
            case ObjectType.Reward:
                GameManager.Instance.experimentStarted = true;
                StartCoroutine(GameManager.Instance.IssueReward(GameManager.Instance.Reward));
                break;
            case ObjectType.Punish:
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
