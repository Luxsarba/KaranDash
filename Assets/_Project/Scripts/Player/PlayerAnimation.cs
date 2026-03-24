using UnityEngine;

/// <summary>
/// Анимации игрока (бег, прыжки).
/// </summary>
public class PlayerAnimation : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Animator animator;

    private void Update()
    {
        HandleRunAnimation();
    }

    private void HandleRunAnimation()
    {
        if (animator == null)
            return;

        bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) ||
                        Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) ||
                        Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
                        Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);

        animator.SetBool("IsRunning", isMoving);

        if (!isMoving)
        {
            animator.SetInteger("Run", 0);
            return;
        }

        // Определяем направление
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A))
            animator.SetInteger("Run", 5);
        else if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D))
            animator.SetInteger("Run", 6);
        else if (Input.GetKey(KeyCode.W))
            animator.SetInteger("Run", 1);
        else if (Input.GetKey(KeyCode.D))
            animator.SetInteger("Run", 4);
        else if (Input.GetKey(KeyCode.A))
            animator.SetInteger("Run", 3);
        else if (Input.GetKey(KeyCode.S))
            animator.SetInteger("Run", 2);
    }
}
