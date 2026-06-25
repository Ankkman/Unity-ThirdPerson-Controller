using UnityEngine;
using System;

public class HealthComponent : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float healThresholdPercent = 0.7f; // 70% threshold

    public HealthModel Model { get; private set; }
    public Action OnDeathEvent;

    public bool IsHealing => Model != null && Model.IsHealing;

    private void Awake()
    {
        // Initialize your pure C# data model
        Model = new HealthModel(maxHealth);
        Model.OnDeath += HandleDeath;
    }

    // The Hitboxes will call this method directly!
    public void TakeDamage(int amount)
    {
        if (Model.IsDead) return;

        Model.TakeDamage(amount);
        
        // Let's make the debug log very clear for testing
        Debug.Log($"<color=red>HIT!</color> {gameObject.name} took {amount} damage. HP remaining: {Model.CurrentHealth}/{Model.MaxHealth}");
    }

    public void StartHeal(int amount, float duration)
    {
        float currentPercent = (float)Model.CurrentHealth / Model.MaxHealth;
        
        if (Model.IsDead || currentPercent >= healThresholdPercent) 
        {
            Debug.Log("Health too high to heal or entity is dead.");
            return;
        }

        StartCoroutine(HealRoutine(amount, duration));
    }

    private System.Collections.IEnumerator HealRoutine(int amount, float duration)
    {
        Model.StartHeal();
        yield return new WaitForSeconds(duration);
        Model.FinishHeal(amount);
    }

    private void HandleDeath()
    {
        Debug.Log($"<color=black>{gameObject.name} HAS DIED.</color>");
        OnDeathEvent?.Invoke();
    }

    public void ResetHealthComponent()
    {
        StopAllCoroutines();
        Model.ResetHealth();
    }
}