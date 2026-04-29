using UnityEngine;

public class PushableBlock : MonoBehaviour
{
    [Header("Grid Settings")]
    public float gridSize = 1f;
    public float moveSpeed = 8f;
    public LayerMask obstacleLayer;  // ใส่ Wall layer + Pushable layer

    private bool isMoving = false;
    private Vector3 targetPosition;
    public bool IsMoving() => isMoving;
    void Start() => SnapToGrid();

    void Update()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) <= 0.001f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }

    public bool TryPush(Vector2 direction)
    {
        if (isMoving) return false;

        Vector3 destination = transform.position + new Vector3(direction.x, direction.y, 0f) * gridSize;

        // เช็คว่าตำแหน่งปลายทางมีสิ่งกีดขวางไหม
        if (Physics2D.OverlapCircle(destination, 0.2f, obstacleLayer)) return false;

        targetPosition = destination;
        isMoving = true;
        return true;
    }
    public bool IsOnPosition(Vector3 pos)
    {
        return Vector3.Distance(transform.position, pos) <= 0.1f;
    }

    void SnapToGrid()
    {
        transform.position = new Vector3(
            Mathf.Round(transform.position.x / gridSize) * gridSize,
            Mathf.Round(transform.position.y / gridSize) * gridSize,
            transform.position.z);
    }
}
