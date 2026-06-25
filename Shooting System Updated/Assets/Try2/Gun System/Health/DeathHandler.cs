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
        // 1. Play the Mixamo death fall
        animatorBridge.PlayDeath();

        // 2. Instantly kill the procedural IK so the spine goes limp
        if (masterRig != null)
        {
            masterRig.weight = 0f;
        }

        // 3. Shut down all combat and movement capabilities
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