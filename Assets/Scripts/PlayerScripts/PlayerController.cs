using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float _moveSpeed = 5f;
    public Transform _movePoint;

    [Header("Isometric Settings")]
    [Tooltip("ขนาดของ Grid X และ Y ตามที่ตั้งค่าไว้ใน Unity (ปกติคือ 1 และ 0.5)")]
    public Vector2 _tileSize = new Vector2(1f, 0.5f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _movePoint.parent = null;
    }

    // Update is called once per frame
    void Update()
    {
        // ให้ตัวละครค่อยๆ เลื่อนไปยังจุด _movePoint
        transform.position = Vector3.MoveTowards(transform.position, _movePoint.position, _moveSpeed * Time.deltaTime);

        // เมื่อเดินไปถึง (หรือเกือบถึง) จุด _movePoint แล้ว จึงจะรับคำสั่งเดินครั้งต่อไป
        if (Vector3.Distance(transform.position, _movePoint.position) <= .05f)
        {
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputY = Input.GetAxisRaw("Vertical");

            // ชิเอลใช้ else if เพื่อบังคับให้เดินทีละทิศทาง ป้องกันปัญหาการกด 2 ปุ่มพร้อมกันแล้วเดินหลุดช่องค่ะ
            if (MathF.Abs(inputX) == 1f)
            {
                // เดินตามแกน X ของ Isometric (เฉียงลงขวา หรือ เฉียงขึ้นซ้าย)
                float nextX = inputX * (_tileSize.x / 2f);
                float nextY = inputX * (-_tileSize.y / 2f);

                _movePoint.position += new Vector3(nextX, nextY, 0f);
            }
            else if (MathF.Abs(inputY) == 1f)
            {
                // เดินตามแกน Y ของ Isometric (เฉียงขึ้นขวา หรือ เฉียงลงซ้าย)
                float nextX = inputY * (_tileSize.x / 2f);
                float nextY = inputY * (_tileSize.y / 2f);

                _movePoint.position += new Vector3(nextX, nextY, 0f);
            }
        }
    }
}