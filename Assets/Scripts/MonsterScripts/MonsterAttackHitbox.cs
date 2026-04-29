using System;
using System.Collections.Generic;
using UnityEngine;

// ═════════════════════════════════════════════════════════════
//  MonsterAttackHitbox  — Multi-Hit Edition
//
//  รองรับ 2 โหมด (เลือกใน Inspector):
//
//  [AnimationEvent]
//    ใส่ Event ใน Animation Clip ตรงเฟรมที่ต้องการ
//    เฟรมเริ่ม hit N → ShowHitFromEvent(N)   (int = index ใน _hits)
//    เฟรมสิ้นสุด    → HideHit(N)  หรือ HideAllHits
//
//  [FrameData]
//    กำหนด startup/active/recovery ของแต่ละ hit ใน Inspector
//    เรียก TriggerCombo(dir) จาก MonsterController แล้วจัดการเอง
//
//  FaceDirection: 0=right  1=left  2=up  3=down
// ═════════════════════════════════════════════════════════════

[RequireComponent(typeof(BoxCollider2D))]
public class MonsterAttackHitbox : MonoBehaviour
{
    public enum FaceDirection { right, left, up, down }

    public enum HitboxMode { AnimationEvent, FrameData }

    // ── Hit Keyframe ─────────────────────────────────────────
    [Serializable]
    public class HitKeyframe
    {
        [Tooltip("ชื่อ hit เช่น Hit1, Uppercut (แสดงใน Gizmos)")]
        public string label = "Hit";

        [Header("Shape — Horizontal (left / right)")]
        public Vector2 sizeH = new Vector2(1f, 0.6f);   // ขนาดตอนตีซ้าย/ขวา
        public Vector2 offsetH = new Vector2(1f, 0f);     // offset ตอนตีซ้าย/ขวา

        [Header("Shape — Vertical (up / down)")]
        public Vector2 sizeV = new Vector2(0.6f, 1f);   // ขนาดตอนตีบน/ล่าง
        public Vector2 offsetV = new Vector2(0f, 1f);     // offset ตอนตีบน/ล่าง

        [Header("Frame Data (ใช้เมื่อ Mode = FrameData)")]
        [Tooltip("เฟรมก่อน hitbox เปิด")]
        public int startupFrames = 2;
        [Tooltip("เฟรมที่ hitbox ติดอยู่")]
        public int activeFrames = 3;
        [Tooltip("เฟรมหลัง hitbox ปิด (ก่อน hit ถัดไปเริ่ม)")]
        public int recoveryFrames = 3;

        [Header("Animation Event (ใช้เมื่อ Mode = AnimationEvent)")]
        [Tooltip("hitbox ติดอยู่กี่วินาที (สำรองถ้าไม่มี HideHit event)")]
        public float activeTime = 0.15f;

        // runtime
        [HideInInspector] public BoxCollider2D col;
        [HideInInspector] public bool isActive = false;
        [HideInInspector] public float timer = 0f;
    }

    // ── Inspector ─────────────────────────────────────────────
    [Header("Mode")]
    public HitboxMode _mode = HitboxMode.AnimationEvent;

    [Header("Hits — เพิ่มได้เท่าที่ต้องการ")]
    public List<HitKeyframe> _hits = new List<HitKeyframe>();

    [Header("Frame Data — Global")]
    [Tooltip("FPS ของ animation ใช้คำนวณวินาทีใน FrameData mode")]
    public int _animFPS = 12;

    // ── private ───────────────────────────────────────────────
    private FaceDirection _faceDir = FaceDirection.right;

    // FrameData runtime
    private int _comboIndex = -1;   // hit ที่กำลังทำงานอยู่
    private float _phaseTimer = 0f;
    private enum FramePhase { Idle, Startup, Active, Recovery }
    private FramePhase _phase = FramePhase.Idle;

    // ── Unity ─────────────────────────────────────────────────
    private void Awake()
    {
        // สร้าง BoxCollider2D แยกสำหรับแต่ละ hit
        // hit[0] ใช้ collider หลักบน GameObject นี้
        // hit[1+] สร้าง child GameObject ให้อัตโนมัติ
        for (int i = 0; i < _hits.Count; i++)
        {
            if (i == 0)
            {
                _hits[i].col = GetComponent<BoxCollider2D>();
            }
            else
            {
                GameObject child = new GameObject($"Hitbox_{_hits[i].label}");
                child.transform.SetParent(transform, false);
                child.layer = gameObject.layer;
                _hits[i].col = child.AddComponent<BoxCollider2D>();
            }

            _hits[i].col.isTrigger = true;
            _hits[i].col.enabled = false;
        }
    }

    private void Update()
    {
        switch (_mode)
        {
            case HitboxMode.AnimationEvent: TickAnimationEvent(); break;
            case HitboxMode.FrameData: TickFrameData(); break;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  Animation Event Mode
    // ══════════════════════════════════════════════════════════

    void TickAnimationEvent()
    {
        for (int i = 0; i < _hits.Count; i++)
        {
            var h = _hits[i];
            if (!h.isActive) continue;
            h.timer -= Time.deltaTime;
            if (h.timer <= 0f) HideHit(i);
        }
    }

    /// <summary>เรียกจาก Animation Event — hitIndex = index ใน _hits list</summary>
    public void ShowHitFromEvent(int hitIndex)
    {
        if (!ValidIndex(hitIndex)) return;
        _mode = HitboxMode.AnimationEvent;

        var h = _hits[hitIndex];
        ApplyShape(h);
        h.col.enabled = true;
        h.isActive = true;
        h.timer = h.activeTime;
    }

    /// <summary>ซ่อน hit ที่ระบุ — ใส่ใน Animation Event เฟรมสุดท้ายของ hit นั้นได้เลย</summary>
    public void HideHit(int hitIndex)
    {
        if (!ValidIndex(hitIndex)) return;
        var h = _hits[hitIndex];
        h.col.enabled = false;
        h.isActive = false;
        h.timer = 0f;
    }

    /// <summary>ซ่อนทุก hit พร้อมกัน</summary>
    public void HideAllHits()
    {
        for (int i = 0; i < _hits.Count; i++) HideHit(i);
    }

    // ══════════════════════════════════════════════════════════
    //  Frame Data Mode
    // ══════════════════════════════════════════════════════════

    void TickFrameData()
    {
        if (_phase == FramePhase.Idle) return;

        _phaseTimer -= Time.deltaTime;
        if (_phaseTimer > 0f) return;

        var h = _hits[_comboIndex];

        switch (_phase)
        {
            case FramePhase.Startup:
                // เปิด hitbox
                ApplyShape(h);
                h.col.enabled = true;
                h.isActive = true;
                _phase = FramePhase.Active;
                _phaseTimer = ToSeconds(h.activeFrames);
                break;

            case FramePhase.Active:
                // ปิด hitbox เข้า recovery
                h.col.enabled = false;
                h.isActive = false;
                _phase = FramePhase.Recovery;
                _phaseTimer = ToSeconds(h.recoveryFrames);
                break;

            case FramePhase.Recovery:
                // ดู hit ถัดไป
                _comboIndex++;
                if (_comboIndex < _hits.Count)
                {
                    // มี hit ถัดไป → เริ่ม startup ของมัน
                    _phase = FramePhase.Startup;
                    _phaseTimer = ToSeconds(_hits[_comboIndex].startupFrames);
                }
                else
                {
                    // จบ combo แล้ว
                    _phase = FramePhase.Idle;
                    _comboIndex = -1;
                }
                break;
        }
    }

    /// <summary>
    /// เรียกจาก MonsterController — เริ่ม combo ทั้งหมดตามลำดับใน _hits
    /// </summary>
    /// <summary>true ขณะ combo ยังไม่จบ (Startup/Active/Recovery)</summary>
    public bool IsComboActive => _phase != FramePhase.Idle || _comboIndex >= 0;

    public void TriggerCombo(FaceDirection dir)
    {
        if (_hits.Count == 0) return;
        _mode = HitboxMode.FrameData;
        _faceDir = dir;
        HideAllHits();

        _comboIndex = 0;
        _phase = FramePhase.Startup;
        _phaseTimer = ToSeconds(_hits[0].startupFrames);
    }

    /// <summary>เรียกจาก Animation Event (int) สำหรับ FrameData mode</summary>
    public void TriggerComboFromEvent(int dirInt) => TriggerCombo((FaceDirection)dirInt);

    // ── Helpers ───────────────────────────────────────────────

    void ApplyShape(HitKeyframe h)
    {
        bool vertical = (_faceDir == FaceDirection.up || _faceDir == FaceDirection.down);

        if (vertical)
        {
            // ตีบน/ล่าง → ใช้ sizeV / offsetV
            h.col.size = h.sizeV;
            float oy = h.offsetV.y;
            h.col.offset = _faceDir == FaceDirection.up
                ? new Vector2(0f, oy)
                : new Vector2(0f, -oy);
        }
        else
        {
            // ตีซ้าย/ขวา → ใช้ sizeH / offsetH
            h.col.size = h.sizeH;
            float ox = h.offsetH.x;
            h.col.offset = _faceDir == FaceDirection.right
                ? new Vector2(ox, 0f)
                : new Vector2(-ox, 0f);
        }
    }

    bool ValidIndex(int i) => i >= 0 && i < _hits.Count;

    float ToSeconds(int frames) => frames / (float)_animFPS;

    // ── Gizmos ────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        for (int i = 0; i < _hits.Count; i++)
        {
            var h = _hits[i];
            if (h.col == null) continue;

            // สีตาม phase (FrameData) หรือ isActive (AnimationEvent)
            Color c;
            if (_mode == HitboxMode.FrameData && _comboIndex == i)
            {
                c = _phase switch
                {
                    FramePhase.Startup => new Color(1f, 1f, 0f, 0.5f),  // เหลือง
                    FramePhase.Active => new Color(1f, 0f, 0f, 0.5f),  // แดง
                    FramePhase.Recovery => new Color(0f, 0.5f, 1f, 0.3f),// น้ำเงิน
                    _ => new Color(1f, 0.5f, 0f, 0.2f) // ส้มจาง
                };
            }
            else
            {
                c = h.isActive
                    ? new Color(1f, 0f, 0f, 0.5f)      // แดง = active
                    : new Color(1f, 0.5f, 0f, 0.15f);  // ส้มจาง = inactive
            }

            Vector3 center = (h.col.transform.position) + (Vector3)h.col.offset;
            Vector3 size = new Vector3(h.col.size.x, h.col.size.y, 0f);

            Gizmos.color = c;
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(c.r, c.g, c.b, 1f);
            Gizmos.DrawWireCube(center, size);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                center + Vector3.up * (h.col.size.y * 0.5f + 0.1f),
                h.label);
#endif
        }
    }
}