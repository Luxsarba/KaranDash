using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoDamage : MonoBehaviour
{
    public float dmg = 0;
    public bool isPlayer = false, isEnemy = true;
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<Enemy>().Damage(dmg);
        }
        if (other.CompareTag("Player"))
        {
            other.GetComponent<Player>().Damage(dmg);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && !isEnemy)collision.gameObject.GetComponent<Enemy>().Damage(dmg);
        if (collision.gameObject.CompareTag("Player") && !isPlayer)collision.gameObject.GetComponent<Player>().Damage(dmg);
    }
}
