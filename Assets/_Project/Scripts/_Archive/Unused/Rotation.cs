using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotation : MonoBehaviour
{
    [Header("Скорость вращения X")] 
    [Range(-100, 100)]
    public float speedX = 3;
    [Header("Скорость вращения Y")] 
    [Range(-100, 100)]
    public float speedY = 3;
    [Header("Скорость вращения Z")] 
    [Range(-100, 100)]
    public float speedZ = 3;

    [Header("Случайно скорость X")] public bool randomX = false;
    [Header("Случайно скорость Y")] public bool randomY = false;
    [Header("Случайно скорость Z")] public bool randomZ = false;

    [Header("X Ось")] public bool is_X = false;
    [Header("Y Ось")] public bool is_Y = true;
    [Header("Z Ось")] public bool is_Z = false;
    
    private System.Random rnd = new System.Random();
    
    void FixedUpdate()
    {
        if (is_X)transform.rotation *= Quaternion.Euler(speedX, 0, 0);
        if (is_Y)transform.rotation *= Quaternion.Euler(0, speedY, 0);
        if (is_Z)transform.rotation *= Quaternion.Euler(0, 0, speedZ);
    }
}
