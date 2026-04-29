using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [Header("Monster Movement Setup")]
    public float _moveSpeed = 5f;
    public Transform _movePoint;
    public float _gridSize = 1f;

    public enum MoveDirection { up, down, left, right, idle }  // ← เพิ่ม idle เป็น step ได้เลย
    public List<MoveDirection> _movementPath;
    public float _waitTime = 0.5f;
    public float _idleWaitTime = 1f;   // ระยะเวลายืนนิ่งต่อ 1 idle step

    [Header("Detect and Hunt")]
    public float _detectRange = 3f;
    public float _huntWaitTime = 0.2f;
    public float _returnFromHunting = 2f;

    [Header("Attack")]
    public float _attackRange = 1f;
    public float _attackCooldown = 1f;

    [Header("Animation")]
    public Animator _anim;

    [Header("Collision")]
    public LayerMask _whatStopMovement;  // Layer กำแพง
    public LayerMask _playerLayer;       // Layer Player (monster ไม่เดินทับ)

    [Header("Attack Hitbox")]
    public MonsterAttackHitbox _attackHitbox;   // ลาก AttackHitbox GameObject มาใส่

    // ── private ──────────────────────────────────────────────
    private Transform _target;
    private Vector3 _startPosition;
    private float _returnTimer;
    private float _currentWaitTime;
    private float _attackTimer;
    private float _idleTimer;
    private bool _isIdleStep = false;
    private int _currentStep = 0;
    private bool _returningToStart = false;

    // ทิศที่ monster หันล่าสุด
    private MonsterAttackHitbox.FaceDirection _faceDir = MonsterAttackHitbox.FaceDirection.right;

    private enum MonsterState { Patrolling, Hunting, Attacking, Returning }
    private MonsterState _state = MonsterState.Patrolling;

    // ── Unity ─────────────────────────────────────────────────
    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _target = player.transform;
        else
            Debug.LogWarning("[Monster] ไม่พบ GameObject ที่ Tag 'Player'");

        if (_movePoint != null)
            _movePoint.parent = null;

        _startPosition = transform.position;
        _currentWaitTime = _waitTime;
    }

    private void Update()
    {
        if (_movePoint == null) return;

        transform.position = Vector3.MoveTowards(
            transform.position, _movePoint.position, _moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _movePoint.position) <= 0.05f)
        {
            UpdateAIState();
            HandleMovementLogic();
        }

        if (_attackTimer > 0f)
            _attackTimer -= Time.deltaTime;
    }

    // ── State Machine ─────────────────────────────────────────
    void UpdateAIState()
    {
        if (_target == null) return;

        float distX = Mathf.Abs(_target.position.x - transform.position.x);
        float distY = Mathf.Abs(_target.position.y - transform.position.y);
        bool inDetectRange = distX <= _detectRange && distY <= _detectRange;

        // aligned = อยู่แถวหรือคอลัมน์เดียวกันกับ player (snap ครบแล้ว)
        bool alignedH = distY < _gridSize * 0.6f;  // Y ตรงกัน → ตีซ้าย/ขวาได้
        bool alignedV = distX < _gridSize * 0.6f;  // X ตรงกัน → ตีบน/ล่างได้
        bool isAligned = alignedH || alignedV;

        bool inAttackRange = distX <= _attackRange && distY <= _attackRange && isAligned;

        switch (_state)
        {
            case MonsterState.Patrolling:
                if (inDetectRange)
                {
                    // ออกจาก idle step กลางทาง → ไป Hunt เลย
                    _isIdleStep = false;
                    _idleTimer = 0f;
                    _state = MonsterState.Hunting;
                    _returnTimer = 0f;
                    Debug.Log("[Monster] → Hunting");
                }
                break;

            case MonsterState.Hunting:
                if (inAttackRange)
                {
                    _state = MonsterState.Attacking;
                    Debug.Log("[Monster] → Attacking");
                }
                else if (!inDetectRange)
                {
                    _returnTimer += Time.deltaTime;
                    if (_returnTimer >= _returnFromHunting)
                    {
                        _state = MonsterState.Returning;
                        Debug.Log("[Monster] → Returning");
                    }
                }
                else
                {
                    _returnTimer = 0f;
                }
                break;

            case MonsterState.Attacking:
                if (!inAttackRange)
                {
                    _state = inDetectRange ? MonsterState.Hunting : MonsterState.Returning;
                    _returnTimer = 0f;
                    Debug.Log($"[Monster] → {_state}");
                }
                break;

            case MonsterState.Returning:
                if (inDetectRange)
                {
                    _state = MonsterState.Hunting;
                    _returnTimer = 0f;
                    Debug.Log("[Monster] → Hunting (เจอระหว่างกลับ)");
                }
                break;
        }
    }

    void HandleMovementLogic()
    {
        switch (_state)
        {
            // ── Hunt: เดินเข้าหา player ───────────────────────
            case MonsterState.Hunting:
                if (_currentWaitTime > 0f) { _currentWaitTime -= Time.deltaTime; return; }
                if (_target != null)
                {
                    StepTowards(_target.position);
                    _currentWaitTime = _huntWaitTime;
                }
                break;

            // ── Attack: หยุดนิ่ง + Debug Attack ───────────────
            case MonsterState.Attacking:
                PerformDebugAttack();
                break;

            // ── Returning: เดินกลับ Start หลังหลุดจาก Hunt ───
            case MonsterState.Returning:
                if (_currentWaitTime > 0f) { _currentWaitTime -= Time.deltaTime; return; }

                if (Vector3.Distance(transform.position, _startPosition) <= _gridSize * 0.5f)
                {
                    // ถึง Start แล้ว → resume path ต่อจาก step เดิม
                    _movePoint.position = _startPosition;
                    _currentStep = 0;
                    _returningToStart = false;
                    _isIdleStep = false;
                    _currentWaitTime = _waitTime;
                    _state = MonsterState.Patrolling;
                    Debug.Log("[Monster] → Patrolling (resume)");
                }
                else
                {
                    // ไม่เดินทับ player ตอนกลับ Start
                    if (CanStep(_movePoint.position + (_startPosition - _movePoint.position).normalized * _gridSize))
                        StepTowards(_startPosition);
                    _currentWaitTime = _huntWaitTime;
                }
                break;

            // ── Patrolling: เดิน path (รวม idle step) ─────────
            case MonsterState.Patrolling:

                // กำลังอยู่ใน idle step → นับเวลา
                if (_isIdleStep)
                {
                    _idleTimer += Time.deltaTime;
                    // TODO: _anim?.SetBool("IsIdle", true);
                    if (_idleTimer < _idleWaitTime) return;

                    // idle ครบ → ไป step ถัดไป
                    _isIdleStep = false;
                    _idleTimer = 0f;
                    // TODO: _anim?.SetBool("IsIdle", false);
                    AdvanceStep();
                    return;
                }

                if (_currentWaitTime > 0f) { _currentWaitTime -= Time.deltaTime; return; }

                if (_movementPath != null && _movementPath.Count > 0)
                {
                    if (_returningToStart)
                    {
                        // เดินครบ path → เดินกลับ Start จริงๆ แล้วเริ่มใหม่
                        if (Vector3.Distance(transform.position, _startPosition) <= _gridSize * 0.5f)
                        {
                            _movePoint.position = _startPosition;
                            _currentStep = 0;
                            _returningToStart = false;
                            _currentWaitTime = _waitTime;
                            Debug.Log("[Monster] ถึง Start → เริ่ม path ใหม่");
                        }
                        else
                        {
                            if (CanStep(_movePoint.position + (_startPosition - _movePoint.position).normalized * _gridSize))
                                StepTowards(_startPosition);
                            _currentWaitTime = _huntWaitTime;
                        }
                    }
                    else
                    {
                        StepToNextGrid();
                        _currentWaitTime = _waitTime;
                    }
                }
                break;
        }
    }

    // ── Attack ────────────────────────────────────────────────
    // ── Attack ────────────────────────────────────────────────
    void PerformDebugAttack()
    {
        // รอให้ combo จบก่อน → ไม่ให้ interrupt animation กลางคัน
        if (_attackHitbox != null && _attackHitbox.IsComboActive) return;
        if (_attackTimer > 0f) return;

        // คำนวณทิศโจมตีจากตำแหน่ง player จริงๆ ทุกครั้ง
        if (_target != null)
            _faceDir = CalcFaceDir(_target.position - transform.position);

        _attackTimer = _attackCooldown;
        Debug.Log($"[Monster] ⚔️  โจมตี! ทิศ: {_faceDir} (cooldown {_attackCooldown}s)");

        if (_attackHitbox != null)
        {
            switch (_attackHitbox._mode)
            {
                case MonsterAttackHitbox.HitboxMode.AnimationEvent:
                    // TODO: _anim?.SetTrigger("Attack");
                    break;
                case MonsterAttackHitbox.HitboxMode.FrameData:
                    _attackHitbox.TriggerCombo(_faceDir);
                    break;
            }
        }

        // TODO: _target?.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
    }

    // ── Helpers ───────────────────────────────────────────────

    /// คำนวณ FaceDirection จาก diff vector (เลือกแกนที่ห่างกว่า)
    MonsterAttackHitbox.FaceDirection CalcFaceDir(Vector3 diff)
    {
        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
            return diff.x >= 0 ? MonsterAttackHitbox.FaceDirection.right
                               : MonsterAttackHitbox.FaceDirection.left;
        else
            return diff.y >= 0 ? MonsterAttackHitbox.FaceDirection.up
                               : MonsterAttackHitbox.FaceDirection.down;
    }

    void StepTowards(Vector3 targetPos)
    {
        Vector3 diff = targetPos - _movePoint.position;
        if (diff.magnitude < _gridSize * 0.8f) return;

        float absX = Mathf.Abs(diff.x);
        float absY = Mathf.Abs(diff.y);

        // ──────────────────────────────────────────────────────
        // หาทางที่ไวที่สุด:
        // เปรียบ steps ที่ต้องเดินถ้า snap X ก่อน vs snap Y ก่อน
        // แล้วเลือกทางที่ใช้ steps น้อยกว่า
        //
        // snap X ก่อน → เดิน X (absX/grid steps) แล้วค่อยเดิน Y
        // snap Y ก่อน → เดิน Y (absY/grid steps) แล้วค่อยเดิน X
        //
        // จริงๆ total steps เท่ากัน แต่ระยะทางถึง attack position
        // ต่างกัน — เลือก snap แกนที่ "ใกล้ attack range" กว่า
        // ──────────────────────────────────────────────────────

        bool alignedH = absY < _gridSize * 0.6f;  // Y ตรงแล้ว
        bool alignedV = absX < _gridSize * 0.6f;  // X ตรงแล้ว

        Vector3 offset = Vector3.zero;

        if (alignedH)
        {
            // Y ตรงแล้ว → เดิน X เข้าหาตรงๆ
            float sign = Mathf.Sign(diff.x);
            offset = new Vector3(sign * _gridSize, 0f, 0f);
            _faceDir = sign > 0 ? MonsterAttackHitbox.FaceDirection.right
                                : MonsterAttackHitbox.FaceDirection.left;
        }
        else if (alignedV)
        {
            // X ตรงแล้ว → เดิน Y เข้าหาตรงๆ
            float sign = Mathf.Sign(diff.y);
            offset = new Vector3(0f, sign * _gridSize, 0f);
            _faceDir = sign > 0 ? MonsterAttackHitbox.FaceDirection.up
                                : MonsterAttackHitbox.FaceDirection.down;
        }
        else
        {
            // ยังไม่ตรงแกนไหนเลย → เลือก snap แกนที่ใกล้กว่า
            // (ต้องเดินน้อย steps กว่าเพื่อ align)
            // stepsToAlignX = absY/grid, stepsToAlignY = absX/grid
            // เลือก snap แกนที่ใช้ steps น้อยกว่า
            bool snapYFirst = absY <= absX;

            if (snapYFirst)
            {
                // snap Y ก่อน (Y ใกล้กว่า → align เร็วกว่า → ตีได้เร็วกว่า)
                float sign = Mathf.Sign(diff.y);
                offset = new Vector3(0f, sign * _gridSize, 0f);
                _faceDir = sign > 0 ? MonsterAttackHitbox.FaceDirection.up
                                    : MonsterAttackHitbox.FaceDirection.down;
            }
            else
            {
                // snap X ก่อน
                float sign = Mathf.Sign(diff.x);
                offset = new Vector3(sign * _gridSize, 0f, 0f);
                _faceDir = sign > 0 ? MonsterAttackHitbox.FaceDirection.right
                                    : MonsterAttackHitbox.FaceDirection.left;
            }
        }

        _movePoint.position += offset;
    }

    // เช็คก่อนเดิน patrol/returning ว่าไม่ทับ player
    bool CanStep(Vector3 nextPos)
    {
        return !Physics2D.OverlapCircle(nextPos, 0.2f, _playerLayer);
    }

    void StepToNextGrid()
    {
        if (_movementPath == null || _movementPath.Count == 0) return;

        MoveDirection dir = _movementPath[_currentStep];

        if (dir == MoveDirection.idle)
        {
            // เจอ idle step → เริ่มนับเวลา ไม่ขยับ movePoint
            _isIdleStep = true;
            _idleTimer = 0f;
            Debug.Log($"[Monster] idle step {_currentStep}");
            // TODO: _anim?.SetBool("IsIdle", true);
            return;
        }

        Vector3 offset = dir switch
        {
            MoveDirection.up => new Vector3(0f, _gridSize, 0f),
            MoveDirection.down => new Vector3(0f, -_gridSize, 0f),
            MoveDirection.left => new Vector3(-_gridSize, 0f, 0f),
            MoveDirection.right => new Vector3(_gridSize, 0f, 0f),
            _ => Vector3.zero
        };

        // track ทิศ
        _faceDir = dir switch
        {
            MoveDirection.up => MonsterAttackHitbox.FaceDirection.up,
            MoveDirection.down => MonsterAttackHitbox.FaceDirection.down,
            MoveDirection.left => MonsterAttackHitbox.FaceDirection.left,
            MoveDirection.right => MonsterAttackHitbox.FaceDirection.right,
            _ => _faceDir
        };

        // เช็คก่อนว่า grid ถัดไปไม่มีผู้เล่นอยู่
        if (!BlockedForMonster(_movePoint.position + offset))
            _movePoint.position += offset;
        AdvanceStep();
    }

    bool BlockedForMonster(Vector3 pos)
    {
        // เช็คกำแพงและ player — monster ไม่เดินทับทั้งคู่ตอน patrol
        return Physics2D.OverlapCircle(pos, 0.2f, _whatStopMovement)
            || Physics2D.OverlapCircle(pos, 0.2f, _playerLayer);
    }

    void AdvanceStep()
    {
        _currentStep++;
        if (_currentStep >= _movementPath.Count)
        {
            _currentStep = _movementPath.Count - 1;
            _returningToStart = true;
            Debug.Log("[Monster] เดินครบ path → กลับ Start");
        }
    }

    // ── Gizmos ────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position,
            new Vector3(_detectRange * 2, _detectRange * 2, 0f));

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position,
            new Vector3(_attackRange * 2, _attackRange * 2, 0f));
    }
}