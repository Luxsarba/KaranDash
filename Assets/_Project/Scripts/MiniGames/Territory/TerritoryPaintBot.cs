using System.Collections;
using UnityEngine;

/// <summary>
/// Bot mover for territory paint mini-game.
/// </summary>
public class TerritoryPaintBot : MonoBehaviour
{
    private TerritoryPaintPanel _panel;
    private int _ownerId;
    private float _speedTilesPerSecond;
    private float _moveAnimationDuration;
    private float _heightOffset;
    private Vector2Int _currentCell;
    private Coroutine _loopCoroutine;
    private bool _isRunning;

    public void Begin(
        TerritoryPaintPanel panel,
        int ownerId,
        Vector2Int startCell,
        float speedTilesPerSecond,
        float moveAnimationDuration,
        float heightOffset)
    {
        _panel = panel;
        _ownerId = ownerId;
        _currentCell = startCell;
        _speedTilesPerSecond = Mathf.Max(0.1f, speedTilesPerSecond);
        _moveAnimationDuration = Mathf.Max(0.01f, moveAnimationDuration);
        _heightOffset = Mathf.Max(0f, heightOffset);

        if (_panel != null)
            transform.position = _panel.GetWorldPositionForCell(startCell) + Vector3.up * _heightOffset;

        StopBot();
        _isRunning = true;
        _loopCoroutine = StartCoroutine(RunLoop());
    }

    public void StopBot()
    {
        _isRunning = false;
        if (_loopCoroutine != null)
        {
            StopCoroutine(_loopCoroutine);
            _loopCoroutine = null;
        }
    }

    private IEnumerator RunLoop()
    {
        while (_isRunning && _panel != null && !_panel.IsLocked)
        {
            if (_panel.TryGetRandomNeighborCell(_currentCell, out Vector2Int nextCell))
            {
                yield return AnimateMove(nextCell);
                _currentCell = nextCell;
                _panel.TryPaintCellAt(_ownerId, _currentCell);
            }

            if (!_isRunning || _panel == null || _panel.IsLocked)
                break;

            float stepDelay = 1f / Mathf.Max(0.1f, _speedTilesPerSecond);
            yield return new WaitForSeconds(stepDelay);
        }

        _loopCoroutine = null;
    }

    private IEnumerator AnimateMove(Vector2Int targetCell)
    {
        if (_panel == null)
            yield break;

        Vector3 start = transform.position;
        Vector3 end = _panel.GetWorldPositionForCell(targetCell) + Vector3.up * _heightOffset;
        float duration = Mathf.Max(0.01f, _moveAnimationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;
    }

    private void OnDisable()
    {
        StopBot();
    }
}
