using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Маршрут")]
    [SerializeField] private Transform[] points;
    [SerializeField] private float speed = 3f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool pingPong = false;

    [Header("Поворот по направлению движения")]
    [SerializeField] private bool rotateToMoveDirection = true;
    [SerializeField] private float rotateSpeed = 180f;

    private int _index = 0;
    private int _dir = 1;

    private void Reset()
    {
        // чтобы не забыть добавить коллайдер
        if (!GetComponent<Collider>()) gameObject.AddComponent<BoxCollider>();
    }

    private void Update()
    {
        if (points == null || points.Length < 2) return;

        Vector3 target = points[_index].position;
        Vector3 before = transform.position;

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        Vector3 moveDelta = transform.position - before;
        if (rotateToMoveDirection && moveDelta.sqrMagnitude > 0.000001f)
        {
            Quaternion look = Quaternion.LookRotation(moveDelta.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, rotateSpeed * Time.deltaTime);
        }

        // дошли до точки
        if ((transform.position - target).sqrMagnitude < 0.0001f)
        {
            AdvanceIndex();
        }
    }

    private void AdvanceIndex()
    {
        if (pingPong)
        {
            if (_index == points.Length - 1) _dir = -1;
            else if (_index == 0) _dir = 1;
            _index += _dir;
            return;
        }

        _index++;

        if (_index >= points.Length)
        {
            if (loop) _index = 0;
            else _index = points.Length - 1;
        }
    }
}
