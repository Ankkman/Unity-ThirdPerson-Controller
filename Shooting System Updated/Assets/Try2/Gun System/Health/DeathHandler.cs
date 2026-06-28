using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections;

[RequireComponent(typeof(HealthComponent))]
public class DeathHandler : MonoBehaviour
{
    [Header("Rigging Cleanup")]
    [Tooltip("Drag the Master AimRig here so we can kill it upon death")]
    public Rig masterRig;

    private HealthComponent health;
    private CharacterAnimatorBridge animatorBridge;
    private CharacterController charController;
    private WeaponComponent weapon;

    public bool killedByHeadshot { get; set; }

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        animatorBridge = GetComponent<CharacterAnimatorBridge>();
        charController = GetComponent<CharacterController>();
        weapon = GetComponent<WeaponComponent>();

        // Subscribe to the death event!
        health.OnDeathEvent += OnDeath;
    }

    private void OnDeath()
    {
        // Determine ID: 1 for Headshot, 0 for standard Body hit
        int typeID = killedByHeadshot ? 1 : 0;

        // 1. Play the correct Mixamo death clip variation
        animatorBridge.PlayDeath(typeID);

        // 2. Instantly kill the procedural IK so the spine matches the fall
        if (masterRig != null) masterRig.weight = 0f;

        // 3. Shut down weapon and controllers
        if (weapon != null) weapon.enabled = false;
        if (charController != null) charController.enabled = false;

        StartCoroutine(DisableHitboxesAfterAnimation());
    }
    private IEnumerator DisableHitboxesAfterAnimation()
    {
        yield return new WaitForSeconds(3f); 

        // Turn off the head and body hitboxes so bullets pass through the corpse
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        Debug.Log($"{gameObject.name} is fully disabled and inert.");
    }
    
    private void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks!
        if (health != null) health.OnDeathEvent -= OnDeath;
    }
}