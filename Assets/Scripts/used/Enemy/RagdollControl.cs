using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollControl : MonoBehaviour
{
    public Animator Animator;
    public Rigidbody[] AllRigidbodies;

    private void Awake()
    {
        for (int i = 0; i < AllRigidbodies.Length; i++)
        {
            AllRigidbodies[i].isKinematic = true;
        }
    }

    public void MakePhysical()
    {
        Animator.enabled = false;
        for (int i = 0; i < AllRigidbodies.Length; i++)
        {
            AllRigidbodies[i].isKinematic = false;
        }
    }
}
