using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using Random = UnityEngine.Random;

public class ObjectController : MonoBehaviour
{
    // private Vector3 _rotationDir;
    
    // public bool RotateEnabled { get; set; } = true;
    public ObjectType Type { get; set; } = ObjectType.Neutral;

    // private MeshRenderer renderer;
    // private Vector3 pos;

    private void Start()
    {
        // Find a random direction to rotate the object
        // _rotationDir = Random.insideUnitSphere.normalized;
        // Scale boundaries of object's clickable areas for the clumsy hands of our mice
        // GetComponent<BoxCollider>().size *= 1.5f;

    }

    private void Update()
    {
        // Rotate objects
        // if (!RotateEnabled) return;
        // gameObject.transform.Rotate(_rotationDir * (GameManager.Instance.ObjectRotationSpeed * Time.deltaTime));
        // CheckMouseClick();
    }
}