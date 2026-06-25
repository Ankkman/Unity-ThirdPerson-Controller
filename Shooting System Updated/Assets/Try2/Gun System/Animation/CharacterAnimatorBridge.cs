using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections;

public class CharacterAnimatorBridge : MonoBehaviour
{
    [Header("IK Rigging References")]
    [Tooltip("Drag your Constraint_LeftArm here so we can fade it off during reloads")]
    public TwoBoneIKConstraint leftArmIK;
    public float ikFadeSpeed = 5f;
    
    [Tooltip("How long should the hand detach to play the Mixamo animation?")]
    public float reloadDetachTime = 1.5f;

    private Animator animator;

    // We use StringToHash for performance (much faster than passing strings)
    private static readonly int IsFiring = Animator.StringToHash("IsFiring");
    private static readonly int ReloadTrigger = Animator.StringToHash("Reload");
    private static readonly int DieTrigger = Animator.StringToHash("Die");
    private static readonly int HealTrigger = Animator.StringToHash("Heal");

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
        animator.SetTrigger(ReloadTrigger);
        
        // Temporarily detach the left hand from the gun
        if (leftArmIK != null)
        {
            StopAllCoroutines();
            StartCoroutine(ReloadIKRoutine());
        }
    }

    private IEnumerator ReloadIKRoutine()
    {
        // 1. Fade the IK weight to 0 (Hand drops off the barrel)
        while (leftArmIK.weight > 0.05f)
        {
            leftArmIK.weight = Mathf.Lerp(leftArmIK.weight, 0f, Time.deltaTime * ikFadeSpeed);
            yield return null;
        }
        leftArmIK.weight = 0f;

        // 2. Wait for the Mixamo animation to do the magazine swap
        yield return new WaitForSeconds(reloadDetachTime);

        // 3. Fade the IK weight back to 1 (Hand snaps back to the barrel)
        while (leftArmIK.weight < 0.95f)
        {
            leftArmIK.weight = Mathf.Lerp(leftArmIK.weight, 1f, Time.deltaTime * ikFadeSpeed);
            yield return null;
        }
        leftArmIK.weight = 1f;
    }

    public void PlayHeal()
    {
        animator.SetTrigger(HealTrigger);
    }

    public void PlayDeath()
    {
        animator.SetTrigger(DieTrigger);
    }
}