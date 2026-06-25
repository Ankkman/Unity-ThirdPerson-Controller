using UnityEngine;

public class GunController : MonoBehaviour
{
    [SerializeField] private GunScriptableObject gun;
    [SerializeField] private Transform firePoint;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private Animator animator;

    private float lastShootTime;
    private Vector3 pendingDirection;

    private void Start()
    {
        gun.Initialize(this);
    }

    // Called by AI (NPCShooter)
    public void TriggerShoot(Vector3 direction)
    {
        float fireRate = Mathf.Max(0.05f, gun.shootConfig.fireRate);

        if (Time.time < lastShootTime + fireRate)
            return;

        lastShootTime = Time.time;

        pendingDirection = direction;

        // Trigger animation
        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }
    }

    // Called by Animation Event
    public void FireFromAnimation()
    {
        Debug.Log("Animation Event Fired");
        
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        gun.TryShoot(firePoint.position, pendingDirection);
    }





    // Optional public access
    public Transform FirePoint => firePoint;
}
