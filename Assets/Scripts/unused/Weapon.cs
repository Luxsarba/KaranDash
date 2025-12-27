using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon
{
    private int ammo, currentAmmo;
    public float damage;
    private float fireInterval;
    private Vector3 position;
    public Weapon(float damage, float fireInterval, int ammo, int currentAmmo, Vector3 position)
    {
        this.damage = damage;
        this.fireInterval = fireInterval;
        this.ammo = ammo;
        this.position = position;
        this.currentAmmo = currentAmmo;
    }

    public void SetPosition(Vector3 position)
    {
        this.position = position;
    }

    public Vector3 GetPosition()
    {
        return this.position;
    }
    
    public float GetFireInterval()
    {
        return this.fireInterval;
    }

    public int GetAmmo()
    {
        return this.ammo;
    }

    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    public void SetCurrentAmmo(int cammo)
    {
        currentAmmo = cammo;
    }
}
