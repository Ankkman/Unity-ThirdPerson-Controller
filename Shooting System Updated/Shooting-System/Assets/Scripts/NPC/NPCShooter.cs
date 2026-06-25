using UnityEngine;

public class NPCShooter : MonoBehaviour
{
    public Transform target;
    public GunController gunController;
    public float shootingRange = 20f;
    public float rotationSpeed = 5f;

    private void Update()
    {
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > shootingRange) return;

        Vector3 directionToTarget = (target.position - transform.position).normalized;

        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);

        gunController.TriggerShoot(directionToTarget);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootingRange);
    }
}
