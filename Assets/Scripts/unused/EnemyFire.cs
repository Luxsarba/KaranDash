using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFire : MonoBehaviour
{
    [Header("Префаб патрона")] public GameObject ammoPref;
    [Header("Расстотяние атаки")][Range(3, 100)] public float fireDistance = 20f;
    [Header("Активность бота")] public bool botActivity = true;
    [Header("Дуло")] public GameObject gun;
    [Header("Скорость стрельбы")]
    [Range(0.01f, 10)]
    public float fireSpeed = 5;

    [Header("Урон")] [Range(0, 20)] public float damage = 2f;
    private GameObject ammo;
    
    void Create(GameObject temp, int a)
    {
        Instantiate(ammoPref, temp.transform.position + 0.65f * a * temp.transform.forward, Quaternion.identity).GetComponent<Rigidbody>().AddForce(temp.transform.forward * 5 * a, ForceMode.Impulse);
    }
    

    void CreateAmmo()
    {
        if (botActivity && Vector3.Distance(GameManager.player.transform.position, transform.position) <= fireDistance)
        {
            //transform.LookAt(new Vector3(GameManager.player.transform.position.x, transform.position.y, GameManager.player.transform.position.z));
            //Create(gun, 1);
            ammo = Instantiate(ammoPref, gun.transform.position + 0.65f * gun.transform.forward, Quaternion.identity);
            ammo.GetComponent<AmmoDamage>().dmg = damage;
            ammo.GetComponent<Rigidbody>().AddForce(gun.transform.forward * 5, ForceMode.Impulse);


        }
    }
    
    void Look()
    {
        if (Vector3.Distance(GameManager.player.transform.position, transform.position) <= fireDistance)
            transform.LookAt(new Vector3(GameManager.player.transform.position.x, transform.position.y, GameManager.player.transform.position.z));
    }

    private void Start()
    {
        //InvokeRepeating("Look", 1, 0.05f);
        InvokeRepeating("CreateAmmo", 2, fireSpeed);
    }
}
