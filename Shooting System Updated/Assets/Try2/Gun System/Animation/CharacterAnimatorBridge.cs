using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections;

public class CharacterAnimatorBridge : MonoBehaviour
{
    [Header("IK Rigging References")]
    [Tooltip("Drag your Master AimRig here so we can drop procedural tracking completely during a reload")]
    public Rig masterAimRig;

    [Tooltip("Drag your Constraint_LeftArm here so we can fade it off during reloads")]
    public TwoBoneIKConstraint leftArmIK;
    
    [Header("Timing Configurations")]
    public float ikFadeSpeed = 8f; // Speed increased for faster responsiveness
    
    [Tooltip("How long should the hand detach to play the Mixamo animation?")]
    public float reloadDetachTime = 1.5f;

    public bool IsReloading { get; private set; } = false;
    public bool IsHealing { get; private set; } = false;

    private Animator animator;
    private Coroutine actionRoutineInstance; // Combined single instance to stop duplicate overlaps!

    private static readonly int IsFiring = Animator.StringToHash("IsFiring");
    private static readonly int ReloadTrigger = Animator.StringToHash("Reload");
    private static readonly int DieTrigger = Animator.StringToHash("Die");
    private static readonly int HealTrigger = Animator.StringToHash("Heal");
    private static readonly int DeathType = Animator.StringToHash("DeathType");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SetFiring(bool value)
    {
        animator.SetBool(IsFiring, value);
    }

    public void PlayReload()
    {
        // INTERLOCK: Cannot reload if already dead or actively healing
        if (IsHealing) return;

        animator.SetTrigger(ReloadTrigger);

        int aimingLayerIndex = animator.GetLayerIndex("AimingLayer");
        if (aimingLayerIndex != -1)
        {
            animator.SetLayerWeight(aimingLayerIndex, 1f);
        }
        
        if (actionRoutineInstance != null) StopCoroutine(actionRoutineInstance);
        actionRoutineInstance = StartCoroutine(ReloadIKRoutine());
    }

    private IEnumerator ReloadIKRoutine()
    {
        IsReloading = true;

        // 1. Smoothly fade out constraints so the hand can grab the magazine
        while ((leftArmIK != null && leftArmIK.weight > 0.01f) || (masterAimRig != null && masterAimRig.weight > 0.01f))
        {
            if (leftArmIK != null) leftArmIK.weight = Mathf.MoveTowards(leftArmIK.weight, 0f, Time.deltaTime * ikFadeSpeed);
            if (masterAimRig != null) masterAimRig.weight = Mathf.MoveTowards(masterAimRig.weight, 0f, Time.deltaTime * ikFadeSpeed);
            yield return null;
        }
        if (leftArmIK != null) leftArmIK.weight = 0f;
        if (masterAimRig != null) masterAimRig.weight = 0f;

        yield return new WaitForSeconds(reloadDetachTime);

        // 2. Smoothly fade constraints back in to hold the gun barrel again
        while ((leftArmIK != null && leftArmIK.weight < 0.99f) || (masterAimRig != null && masterAimRig.weight < 0.99f))
        {
            if (leftArmIK != null) leftArmIK.weight = Mathf.MoveTowards(leftArmIK.weight, 1f, Time.deltaTime * ikFadeSpeed);
            if (masterAimRig != null) masterAimRig.weight = Mathf.MoveTowards(masterAimRig.weight, 1f, Time.deltaTime * ikFadeSpeed);
            yield return null;
        }
        if (leftArmIK != null) leftArmIK.weight = 1f;
        if (masterAimRig != null) masterAimRig.weight = 1f;
        
        int aimingLayerIndex = animator.GetLayerIndex("AimingLayer");
        if (aimingLayerIndex != -1 && !animator.GetBool("IsAiming"))
        {
            animator.SetLayerWeight(aimingLayerIndex, 0f);
        }

        IsReloading = false;
        actionRoutineInstance = null;
    }

    public void PlayHeal()
    {
        if (IsReloading || IsHealing) return;

        // Reset the trigger first to prevent double-firing!
        animator.ResetTrigger(HealTrigger);
        animator.SetTrigger(HealTrigger);

        int aimingLayerIndex = animator.GetLayerIndex("AimingLayer");
        if (aimingLayerIndex != -1)
        {
            animator.SetLayerWeight(aimingLayerIndex, 1f);
        }

        if (actionRoutineInstance != null) StopCoroutine(actionRoutineInstance);
        actionRoutineInstance = StartCoroutine(HealIKRoutine());
    }

    private IEnumerator HealIKRoutine()
    {
        IsHealing = true;

        if (leftArmIK != null) leftArmIK.weight = 0f;
        if (masterAimRig != null) masterAimRig.weight = 0f;

        yield return new WaitForSeconds(2.0f);

        // THE FIX: Only try to restore the IK weights if we actually have a gun drawn!
        // Otherwise, leave them at 0 so the script doesn't get stuck in an infinite loop.
        if (animator.GetBool("HasWeapon"))
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * (ikFadeSpeed / 2f); 
                if (leftArmIK != null) leftArmIK.weight = Mathf.Lerp(0f, 1f, t);
                if (masterAimRig != null) masterAimRig.weight = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
            if (leftArmIK != null) leftArmIK.weight = 1f;
            if (masterAimRig != null) masterAimRig.weight = 1f;
        }

        int aimingLayerIndex = animator.GetLayerIndex("AimingLayer");
        if (aimingLayerIndex != -1 && !animator.GetBool("IsAiming"))
        {
            animator.SetLayerWeight(aimingLayerIndex, 0f);
        }

        IsHealing = false;
        actionRoutineInstance = null;
    }
    
    public void PlayDeath(int typeID)
    {
        if (actionRoutineInstance != null) StopCoroutine(actionRoutineInstance);
        animator.SetInteger(DeathType, typeID);
        animator.SetTrigger(DieTrigger);
    }
}
