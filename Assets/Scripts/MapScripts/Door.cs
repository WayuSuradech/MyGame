using UnityEngine;
using Cainos.PixelArtTopDown_Basic;

public class Door : MonoBehaviour
{
    public Transform destination;
    public float cooldown = 1f;

    private bool canTeleport = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canTeleport) return;

        if (other.CompareTag("Player"))
        {
            // หยุด Coroutine ทั้งหมดของ Controller ก่อน แล้วค่อย teleport
            TopDownCharacterController controller = other.GetComponent<TopDownCharacterController>();
            if (controller != null)
            {
                controller.Teleport(destination.position);
            }

            StartCoroutine(TeleportCooldown());
        }
    }

    private System.Collections.IEnumerator TeleportCooldown()
    {
        canTeleport = false;
        yield return new WaitForSeconds(cooldown);
        canTeleport = true;
    }
}