using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameManager
{
    public static bool isPlayerInputBlocked = false;
    public static bool infiniteAmmo = true;
    public static Player player;
    public static PlayerInventory inventory;
    public static int currentAmmo = 10, maxAmmo = 10;

    public static bool HasAmmo => infiniteAmmo || currentAmmo > 0;

    public static void ConsumeAmmo(int amount = 1)
    {
        if (amount <= 0)
            return;

        if (infiniteAmmo)
        {
            if (maxAmmo > 0 && currentAmmo < maxAmmo)
                currentAmmo = maxAmmo;
            return;
        }

        currentAmmo = Mathf.Max(0, currentAmmo - amount);
    }

    public static void DisablePlayerInput()
    {
        isPlayerInputBlocked = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void EnablePlayerInput()
    {
        isPlayerInputBlocked = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

}
