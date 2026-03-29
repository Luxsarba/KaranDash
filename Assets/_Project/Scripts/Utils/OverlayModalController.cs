using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared modal overlay state for dialogue, notes, and similar fullscreen UI.
/// </summary>
public static class OverlayModalController
{
    private static readonly HashSet<int> OpenOverlayIds = new HashSet<int>();
    private static bool _blockPrimaryActionUntilMouseRelease;

    public static bool HasOpenOverlay => OpenOverlayIds.Count > 0;

    public static void Show(GameObject overlay)
    {
        if (overlay == null)
            return;

        OpenOverlayIds.Add(overlay.GetInstanceID());
        overlay.SetActive(true);
        _blockPrimaryActionUntilMouseRelease = true;

        EnterModalState();
    }

    public static void Hide(GameObject overlay)
    {
        if (overlay != null)
        {
            overlay.SetActive(false);
            OpenOverlayIds.Remove(overlay.GetInstanceID());
        }

        _blockPrimaryActionUntilMouseRelease = true;

        if (HasOpenOverlay)
        {
            EnterModalState();
            return;
        }

        ExitModalState();
    }

    public static bool IsPrimaryActionBlocked()
    {
        if (HasOpenOverlay)
            return true;

        if (!_blockPrimaryActionUntilMouseRelease)
            return false;

        if (Input.GetMouseButton(0))
            return true;

        _blockPrimaryActionUntilMouseRelease = false;
        return false;
    }

    private static void EnterModalState()
    {
        GameManager.DisablePlayerInput();
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static void ExitModalState()
    {
        var pause = ResolvePlayerPause();
        if (pause != null && pause.IsPaused)
        {
            GameManager.DisablePlayerInput();
            Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        GameManager.EnablePlayerInput();
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static PlayerPause ResolvePlayerPause()
    {
        if (GameManager.player != null)
        {
            var fromPlayer = GameManager.player.GetPause();
            if (fromPlayer != null)
                return fromPlayer;

            fromPlayer = GameManager.player.GetComponent<PlayerPause>();
            if (fromPlayer != null)
                return fromPlayer;
        }

        return Object.FindObjectOfType<PlayerPause>(true);
    }
}
