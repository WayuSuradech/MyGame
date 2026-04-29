using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    public class TopDownCharacterController : MonoBehaviour
    {
        public float speed = 5f;
        public float dashSpeed = 15f;
        public float dashCooldown = 1.0f; // ระยะเวลาคูลดาวน์ (วินาที)
        public LayerMask obstacleLayer;

        private Animator animator;
        private bool isMoving = false;
        private bool isDashCooldown = false; // สถานะคูลดาวน์
        private Vector3 targetPos;

        private void Start()
        {
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (isMoving) return;

            // ตรวจสอบการกด Dash และต้องไม่อยู่ในช่วงคูลดาวน์
            if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashCooldown)
            {
                Vector3 dashDir = GetDirectionFromAnimator();
                if (dashDir != Vector3.zero)
                {
                    StartCoroutine(DashRoutine(dashDir));
                    return;
                }
            }

            Vector2 input = Vector2.zero;
            if (Input.GetKey(KeyCode.A)) { input.x = -1; animator.SetInteger("Direction", 3); }
            else if (Input.GetKey(KeyCode.D)) { input.x = 1; animator.SetInteger("Direction", 2); }
            else if (Input.GetKey(KeyCode.W)) { input.y = 1; animator.SetInteger("Direction", 1); }
            else if (Input.GetKey(KeyCode.S)) { input.y = -1; animator.SetInteger("Direction", 0); }

            if (input != Vector2.zero)
            {
                targetPos = transform.position + new Vector3(input.x, input.y, 0);
                if (CanMove(targetPos))
                {
                    StartCoroutine(MoveRoutine(targetPos, speed));
                }
            }
        }

        private Vector3 GetDirectionFromAnimator()
        {
            int dir = animator.GetInteger("Direction");
            switch (dir)
            {
                case 0: return Vector3.down;
                case 1: return Vector3.up;
                case 2: return Vector3.right;
                case 3: return Vector3.left;
                default: return Vector3.zero;
            }
        }

        private bool CanMove(Vector3 target)
        {
            return !Physics2D.OverlapCircle(target, 0.2f, obstacleLayer);
        }

        private IEnumerator MoveRoutine(Vector3 target, float moveSpeed)
        {
            isMoving = true;
            animator.SetBool("IsMoving", true);

            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = target;
            isMoving = false;
            animator.SetBool("IsMoving", false);
        }

        private IEnumerator DashRoutine(Vector3 direction)
        {
            isMoving = true;
            animator.SetBool("IsMoving", true);

            Vector3 finalDestination = transform.position;

            for (int i = 0; i < 2; i++)
            {
                Vector3 nextCheck = finalDestination + direction;
                if (CanMove(nextCheck))
                {
                    finalDestination = nextCheck;
                }
                else break;
            }

            if (finalDestination != transform.position)
            {
                while (Vector3.Distance(transform.position, finalDestination) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, finalDestination, dashSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = finalDestination;
            }

            isMoving = false;
            animator.SetBool("IsMoving", false);

            // เริ่มนับคูลดาวน์หลังจาก Dash เสร็จสิ้น
            StartCoroutine(DashCooldownRoutine());
        }

        private IEnumerator DashCooldownRoutine()
        {
            isDashCooldown = true;
            // ท่าน Lord สามารถใส่ Effect หรือเปลี่ยนสีตัวละครตรงนี้เพื่อให้รู้ว่าติดคูลดาวน์ได้ครับ
            yield return new WaitForSeconds(dashCooldown);
            isDashCooldown = false;
        }
        public void Teleport(Vector3 newPosition)
        {
            // หยุด Coroutine ทั้งหมดที่กำลังวิ่งอยู่
            StopAllCoroutines();

            // รีเซ็ต state
            isMoving = false;
            animator.SetBool("IsMoving", false);
            isDashCooldown = false;

            // ย้ายตำแหน่ง
            transform.position = newPosition;
        }
    }
}