using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroy : MonoBehaviour
{
    [Header ("Время существования")][Range(0.1f, 10)]public float time = 5.0f;
    void Start()
    {
        Destroy(gameObject, time);
    }
}
