using UnityEngine;

public class PuzzleTrigger : MonoBehaviour
{
    [System.Serializable]
    public class BlockAltarPair
    {
        public PushableBlock block;     // Block ที่ต้องวางบนแท่นนี้
        public Transform altarPosition; // แท่นที่ Block ต้องไปยืน
    }

    [Header("Pairs")]
    public BlockAltarPair[] pairs;  // จับคู่ Block กับ Altar

    [Header("Door")]
    public GameObject door;

    [Header("Check Settings")]
    public float snapDistance = 0.6f; // ระยะ tolerance (ปรับได้)

    private bool _solved = false;

    private void Update()
    {
        if (_solved) return;

        // รอให้ทุก Block หยุดเคลื่อนที่ก่อน
        foreach (var pair in pairs)
            if (pair.block.IsMoving()) return;

        if (IsPuzzleSolved())
        {
            _solved = true;
            door.SetActive(false);
            Debug.Log("[Puzzle] Solved!");
        }
    }

    private bool IsPuzzleSolved()
    {
        foreach (var pair in pairs)
        {
            float dist = Vector3.Distance(
                pair.block.transform.position,
                pair.altarPosition.position
            );

            Debug.Log($"[Puzzle] {pair.block.name} → {pair.altarPosition.name} dist: {dist}");

            if (dist > snapDistance) return false;
        }
        return true;
    }
}