using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameManager
{
    public static bool isPlayerInputBlocked = false;
    public static Player player;
    public static PlayerInventory inventory;
    public static int currentAmmo = 10, maxAmmo = 10;

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
