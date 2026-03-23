using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    public float _moveSpeed = 5f;
    public Transform _movePoint;

    [Header("Isometric Settings")]
    public Vector2 _tileSize = new Vector2(1f, 0.5f);

    public enum IsoDirection
    {
        X_Positive,
        X_Negative,
        Y_Positive,
        Y_Negative
    }

    [Header("Patrol Path")]
    public List<IsoDirection> patrolPath;

    [Header("Wait Settings")]
    [Tooltip("เวลาหน่วงตอนเดินลาดตระเวนปกติ (วินาที)")]
    public float waitTime = 1f;
    [Tooltip("เวลาหน่วงตอนไล่ล่าผู้เล่น (ควรน้อยกว่า waitTime เพื่อให้เดินเร็วขึ้น)")]
    public float chaseWaitTime = 0.5f;

    [Header("Detection Settings")]
    public Transform player;
    public float detectionRadius = 3f;
    public bool isPlayerDetected = false;

    private float currentWaitTime;
    private int currentStepIndex = 0;

    void Start()
    {
        _movePoint.parent = null;
        currentWaitTime = waitTime;
    }

    void Update()
    {
        // 1. ตรวจสอบระยะห่างผู้เล่นตลอดเวลา
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            isPlayerDetected = distanceToPlayer <= detectionRadius;
        }

        // ให้ตัวละครเลื่อนไปยังจุด _movePoint
        transform.position = Vector3.MoveTowards(transform.position, _movePoint.position, _moveSpeed * Time.deltaTime);

        // เมื่อเดินถึงจุดเป้าหมาย (อยู่กึ่งกลางช่องพอดี)
        if (Vector3.Distance(transform.position, _movePoint.position) <= .05f)
        {
            // ลดเวลาหน่วงลงเรื่อยๆ
            currentWaitTime -= Time.deltaTime;

            // เมื่อหมดเวลาหน่วง ถึงจะเริ่มก้าวต่อไป
            if (currentWaitTime <= 0f)
            {
                if (isPlayerDetected)
                {
                    // โหมดไล่ล่า
                    ChasePlayer();
                    currentWaitTime = chaseWaitTime; // ใช้เวลาหน่วงที่เร็วขึ้น
                }
                else if (patrolPath != null && patrolPath.Count > 0)
                {
                    // โหมดเดินลาดตระเวนปกติ
                    CalculateNextStep();
                    currentWaitTime = waitTime; // ใช้เวลาหน่วงปกติ
                }
            }
        }
    }

    // ฟังก์ชันสำหรับเดินตามเส้นทางที่กำหนดไว้
    void CalculateNextStep()
    {
        IsoDirection currentDirection = patrolPath[currentStepIndex];
        Vector3 stepOffset = Vector3.zero;

        switch (currentDirection)
        {
            case IsoDirection.X_Positive:
                stepOffset = new Vector3(_tileSize.x / 2f, -_tileSize.y / 2f, 0f);
                break;
            case IsoDirection.X_Negative:
                stepOffset = new Vector3(-_tileSize.x / 2f, _tileSize.y / 2f, 0f);
                break;
            case IsoDirection.Y_Positive:
                stepOffset = new Vector3(_tileSize.x / 2f, _tileSize.y / 2f, 0f);
                break;
            case IsoDirection.Y_Negative:
                stepOffset = new Vector3(-_tileSize.x / 2f, -_tileSize.y / 2f, 0f);
                break;
        }

        _movePoint.position += stepOffset;
        currentStepIndex++;

        if (currentStepIndex >= patrolPath.Count)
        {
            currentStepIndex = 0;
        }
    }

    // ฟังก์ชันใหม่: สำหรับไล่ล่าผู้เล่นทีละช่อง
    void ChasePlayer()
    {
        // สร้างอาเรย์เก็บทิศทางที่เป็นไปได้ทั้ง 4 ทิศในระบบ Isometric
        Vector3[] possibleMoves = new Vector3[]
        {
            new Vector3(_tileSize.x / 2f, -_tileSize.y / 2f, 0f), // X_Positive
            new Vector3(-_tileSize.x / 2f, _tileSize.y / 2f, 0f), // X_Negative
            new Vector3(_tileSize.x / 2f, _tileSize.y / 2f, 0f),  // Y_Positive
            new Vector3(-_tileSize.x / 2f, -_tileSize.y / 2f, 0f)  // Y_Negative
        };

        Vector3 bestMove = Vector3.zero;
        float shortestDistance = Mathf.Infinity;

        // จำลองการก้าวไปทั้ง 4 ทิศ แล้วดูว่าทิศไหนอยู่ใกล้ผู้เล่นมากที่สุด
        foreach (Vector3 move in possibleMoves)
        {
            Vector3 potentialPosition = _movePoint.position + move;
            float distanceToPlayer = Vector3.Distance(potentialPosition, player.position);

            if (distanceToPlayer < shortestDistance)
            {
                shortestDistance = distanceToPlayer;
                bestMove = move;
            }
        }

        // เลื่อนเป้าหมายไปยังทิศทางที่ดีที่สุด
        _movePoint.position += bestMove;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}