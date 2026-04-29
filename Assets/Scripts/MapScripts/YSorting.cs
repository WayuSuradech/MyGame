using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSorting : MonoBehaviour
{
    [Tooltip("ปรับให้ตรงกับ Pivot ของ Sprite (ปกติใช้ 0 / ลองปรับถ้า Sprite ใหญ่)")]
    public float yOffset = 0f;

    private SpriteRenderer _sr;

    void Awake() => _sr = GetComponent<SpriteRenderer>();

    void LateUpdate()
    {
        _sr.sortingOrder = Mathf.RoundToInt(-(transform.position.y + yOffset) * 10);
    }
}