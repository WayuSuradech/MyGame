using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float _moveSpeed = 5f;
    public Transform _movePoint;
    public LayerMask _whatStopMovement;   // กำแพง/สิ่งกีดขวางถาวร (เดิน + dash ทะลุไม่ได้)
    public LayerMask _whatStopWalk;       // Monster layer (เดินทะลุไม่ได้ แต่ dash ผ่านได้)

    [Header("Dash")]
    public int _dashGrids = 2;            // กระโดดกี่ grid
    public float _dashCooldown = 1f;      // cooldown ก่อน dash ได้อีก
    public float _dashSpeed = 20f;        // ความเร็วตอน dash

    [Header("Push")]
    public KeyCode _pushKey = KeyCode.E;
    public float _pushCheckDistance = 1.1f;
    public LayerMask _pushableLayer;      // Layer ของ Block ที่ผลักได้

    [Header("Animation")]
    public Animator _anim;

    // ── private ───────────────────────────────────────────────
    private float _dashTimer = 0f;
    private bool _isDashing = false;
    private Vector3 _lastDir = Vector3.right;  // ทิศล่าสุดที่กด
    private bool _dashQueued = false;           // รอ dash เมื่อถึง movePoint

    private void Start()
    {
        _movePoint.parent = null;
    }

    private void Update()
    {
        // cooldown นับถอยหลัง
        if (_dashTimer > 0f) _dashTimer -= Time.deltaTime;

        // กด E เพื่อผลัก Block
        if (Input.GetKeyDown(_pushKey))
            TryPushBlock();

        // จับ Shift ทันทีทุก frame
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool moving = Mathf.Abs(h) == 1f || Mathf.Abs(v) == 1f;

        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            && moving && _dashTimer <= 0f)
        {
            // เก็บทิศล่าสุดก่อน dash
            if (Mathf.Abs(h) == 1f) _lastDir = new Vector3(h, 0f, 0f);
            else if (Mathf.Abs(v) == 1f) _lastDir = new Vector3(0f, v, 0f);

            // วาง movePoint ที่ grid ที่ player ยืนอยู่ตอนนี้ (round ให้ตรง grid)
            // แล้วคำนวณ destination dash จากจุดนั้น
            Vector3 snappedPos = new Vector3(
                Mathf.Round(transform.position.x),
                Mathf.Round(transform.position.y),
                transform.position.z);
            _movePoint.position = snappedPos;
            TryDash(_lastDir);
        }

        // ความเร็วเดิน: ปกติ หรือเร็วขึ้นตอน dash
        float speed = _isDashing ? _dashSpeed : _moveSpeed;
        transform.position = Vector3.MoveTowards(
            transform.position, _movePoint.position, speed * Time.deltaTime);

        // ถึง movePoint แล้ว
        if (Vector3.Distance(transform.position, _movePoint.position) <= 0.05f)
        {
            _isDashing = false;

            // ตรวจว่าซ้อนกับ monster ไหม → ถ้าใช่ดัน +1 ทิศล่าสุดทันที
            if (BlockedByMonster(transform.position))
            {
                Vector3 pushed = transform.position + _lastDir;
                if (!BlockedByWall(pushed))
                {
                    _movePoint.position = pushed;
                    return; // รอเดินไปถึงก่อนค่อย HandleInput
                }
            }

            HandleInput();
        }

        // _anim?.SetBool("isWalking", Vector3.Distance(transform.position, _movePoint.position) > 0.05f);
    }

    void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // เก็บทิศล่าสุด (เฉพาะตอนกดทิศอยู่)
        if (Mathf.Abs(h) == 1f) _lastDir = new Vector3(h, 0f, 0f);
        else if (Mathf.Abs(v) == 1f) _lastDir = new Vector3(0f, v, 0f);

        // ── เดินปกติ ──────────────────────────────────────────
        if (Mathf.Abs(h) == 1f)
            TryMove(new Vector3(h, 0f, 0f));
        else if (Mathf.Abs(v) == 1f)
            TryMove(new Vector3(0f, v, 0f));
    }

    // ── เดิน 1 grid (เช็คกำแพง + monster) ───────────────────
    void TryMove(Vector3 dir)
    {
        Vector3 next = _movePoint.position + dir;
        if (!BlockedByWall(next) && !BlockedByMonster(next))
            _movePoint.position = next;
    }

    // ── Dash หลาย grid (เช็คแค่กำแพง ผ่าน monster ได้) ─────
    void TryDash(Vector3 dir)
    {
        Vector3 destination = _movePoint.position;
        int moved = 0;

        for (int i = 0; i < _dashGrids; i++)
        {
            Vector3 next = destination + dir;
            if (BlockedByWall(next)) break;   // ชนกำแพง → หยุด

            if (BlockedByMonster(next))
            {
                // ชน monster → ข้ามผ่านไปอีก 1 grid (+ dir อีกครั้ง)
                Vector3 over = next + dir;
                if (!BlockedByWall(over))
                {
                    destination = over;
                    moved += 2;
                }
                // ถ้า grid ถัดไปชนกำแพง → หยุดตรงนั้น ข้ามไม่ได้
                break;
            }

            destination = next;
            moved++;
        }

        if (moved == 0) return;   // ขยับไม่ได้เลย → ไม่ใช้ cooldown

        _movePoint.position = destination;
        _isDashing = true;
        _dashTimer = _dashCooldown;

        // TODO: _anim?.SetTrigger("Dash");
        Debug.Log($"[Player] Dash → {destination}");
    }

    // ── ผลัก Block ───────────────────────────────────────────
    void TryPushBlock()
    {
        Vector2 dir2D = new Vector2(_lastDir.x, _lastDir.y);

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            dir2D,
            _pushCheckDistance,
            _pushableLayer
        );

        if (hit.collider == null) return;

        PushableBlock block = hit.collider.GetComponent<PushableBlock>();
        block?.TryPush(dir2D);
    }

    // ── ตรวจ collision ────────────────────────────────────────
    bool BlockedByWall(Vector3 pos)
    {
        return Physics2D.OverlapCircle(pos, 0.2f, _whatStopMovement);
    }

    bool BlockedByMonster(Vector3 pos)
    {
        return Physics2D.OverlapCircle(pos, 0.2f, _whatStopWalk);
    }

    public void Teleport(Vector3 newPosition)
    {
        // หยุด dash และรีเซ็ต state
        _isDashing = false;
        _dashTimer = 0f;

        // ย้ายทั้ง player และ movePoint ไปพร้อมกัน
        transform.position = newPosition;
        _movePoint.position = newPosition;
    }
}
