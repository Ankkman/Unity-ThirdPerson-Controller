using UnityEngine;

public enum BodyPart 
{ 
    Head, 
    Body, 
    Limb 
}

public class HitboxRegion : MonoBehaviour
{
    [Tooltip("What body part is this?")]
    public BodyPart partType;
    
    [Tooltip("Damage multiplier (e.g., 2.0 for Head, 1.0 for Body)")]
    public float damageMultiplier = 1f;

    private HealthComponent rootHealth;

    private void Awake()
    {
        // MAGIC TRICK: This searches all the way up the parent hierarchy 
        // until it finds the HealthComponent on the root Player/Enemy object!
        rootHealth = GetComponentInParent<HealthComponent>();
        
        if (rootHealth == null)
        {
            Debug.LogWarning($"HitboxRegion on {gameObject.name} cannot find a HealthComponent on its parent!");
        }
    }

    // The Gun script will call this method when a bullet raycast hits this specific bone
    public void ReceiveHit(int baseDamage)
    {
        if (rootHealth != null)
        {
            // Calculate the multiplier (e.g., 25 base damage * 2.0 Head multiplier = 50 damage)
            int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
            rootHealth.TakeDamage(finalDamage);
        }
    }
}