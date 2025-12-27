using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public Camera cam;
    public Transform target;
    public float speedX = 360f, speedY = 240f, limitY = 40f, minDistance = 1.5f;
    public LayerMask obstacles, noPlayer;
    private float _maxDistance;
    private Vector3 _localPosition;
    private float _currentYRotation;
    private Vector3 startPos;

    private Vector3 _position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    void Start()
    {
        _localPosition = target.InverseTransformPoint(_position);
        _maxDistance = Vector3.Distance(_position, target.position);
        startPos = new Vector3(cam.transform.localPosition.x, cam.transform.localPosition.y, cam.transform.localPosition.z);
    }

    void LateUpdate()
    {
        _position = target.TransformPoint(_localPosition);
        ObstaclesReact();
        PlayerReact();
        _localPosition = target.InverseTransformPoint(_position);
    }

    void ObstaclesReact()
    {
        var distance = Vector3.Distance(_position, target.position);
        RaycastHit hit;
        if (Physics.Raycast(target.position, transform.position - target.position, out hit, _maxDistance, obstacles))
        {
            _position = hit.point;
        }
        else if (distance < _maxDistance && !Physics.Raycast(_position, -transform.forward, .1f, obstacles))
        {
            _position -= transform.forward * .05f;
        }
    }

    void PlayerReact()
    {
        
    }
}
