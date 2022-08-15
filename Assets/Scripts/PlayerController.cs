using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool rotateEnabled = false;
    public void RotateCubeInUpdate()
    {
        UnityThread.executeInUpdate(() =>
        {
            gameObject.transform.Rotate(0, 45, 0);
        });
    }

    private void Update()
    {
        if (rotateEnabled)
        {
            gameObject.transform.Rotate(Vector3.up * (5 * Time.deltaTime));
        }
        
    }
}
