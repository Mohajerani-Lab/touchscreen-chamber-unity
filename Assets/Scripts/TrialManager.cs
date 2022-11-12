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
        CheckClick();
    }

    private void CheckClick()
    {
        // Debug.Log("Click");
        if (!Input.GetMouseButtonDown(0)) return;
        if (!Physics.Raycast(GameManager.Instance.mainCamera.ScreenPointToRay(Input.mousePosition), out var raycastHitInfo)) return;

        var goType = raycastHitInfo.collider.gameObject.GetComponent<ObjectController>().Type;

        GameManager.Instance.experimentStarted = true;

        StartCoroutine(goType switch
        {
            ObjectController.ObjectType.Reward => GameManager.Instance.IssueReward(),
            ObjectController.ObjectType.Punish => GameManager.Instance.IssuePunish(),
            ObjectController.ObjectType.Neutral => throw new ArgumentOutOfRangeException(),
            _ => throw new ArgumentOutOfRangeException()
        });
    }
}
