using UnityEngine;

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
            PlayerController controller = other.GetComponent<PlayerController>();
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