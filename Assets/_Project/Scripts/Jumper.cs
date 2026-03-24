using Seagull.Interior_01;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jumper : MonoBehaviour
{
    [SerializeField] public GameObject forceField;
    AudioSource spring_audio;

    private void Start()
    {
        spring_audio = GetComponentInParent<AudioSource>();
    }

    private void DisableField()
    {
        forceField.SetActive(false);
    }
    
    public void EnableField(float time)
    {
        forceField.SetActive(true);
        Invoke("DisableField", time);
        spring_audio.Play();

    }

}
