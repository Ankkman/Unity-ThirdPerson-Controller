using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(CharacterController), typeof(Animator), typeof(CharacterAnimatorBridge))]
public class CharacterMotor : MonoBehaviour
{
    [Header("References")]
    public Rig aimRig;
    public WeaponComponent equippedWeapon;
    public GameObject weaponMesh;

    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float sprintSpeed = 7f;
    public float aimMoveSpeed = 2f;
    public float rotationSpeed = 10f;
    public float jumpHeight = 1.5f;
    
    private float gravity = -9.81f;
    private float verticalVelocity;

    private Animator animator;
    private CharacterController characterController;
    private CharacterAnimatorBridge animatorBridge;

    // --- INTERNAL STATE DRIVEN BY THE BRAIN ---
    private Vector3 currentMoveDirection;
    private Vector3 currentLookPosition; // Replaces camera forward
    private bool wantsToAim;
    private bool wantsToFire;
    private bool wantsToSprint;
    public bool hasWeapon = true;

    private float currentAnimX, currentAnimY, currentSpeed;
    private float aimLayerWeight;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        animatorBridge = GetComponent<CharacterAnimatorBridge>();
    }

    private void Update()
    {
        HandleMovement();
        HandleAnimations();
        HandleShooting();
    }

    // ==========================================
    // PUBLIC API (THE "PUPPET STRINGS")
    // Player Input or Enemy AI will call these!
    // ==========================================

    public void Move(Vector3 direction, bool sprint)
    {
        currentMoveDirection = direction;
        wantsToSprint = sprint;
    }

    public void SetAimTarget(Vector3 worldPosition)
    {
        currentLookPosition = worldPosition;
    }

    public void SetCombatState(bool aiming, bool firing)
    {
        wantsToAim = aiming;
        wantsToFire = firing;
    }

    public void Jump()
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
        }
    }

    public void TriggerHeal() { if (animatorBridge != null) animatorBridge.PlayHeal(); }
    
    public void TriggerReload() 
    { 
        // FIX: We now tell the WEAPON to reload. 
        // The WeaponComponent will handle the math AND tell the Animator to play the clip!
        if (equippedWeapon != null && hasWeapon)
        {
            equippedWeapon.TryReload(); 
        }
    }
    public void ToggleWeapon() { hasWeapon = !hasWeapon; }

    // ==========================================
    // INTERNAL PHYSICAL EXECUTION
    // ==========================================
    private void HandleMovement()
    {
        // Checks the bridge booleans AND asks the Animator if the Reload state is currently playing
        bool isPerformingAction = (animatorBridge != null && (animatorBridge.IsReloading || animatorBridge.IsHealing)) || 
                                animator.GetCurrentAnimatorStateInfo(1).IsName("Reload") || 
                                animator.GetNextAnimatorStateInfo(1).IsName("Reload");

        // --- PUBG TACTICAL LOCOMOTION LOGIC ---
        bool wantsToCombatStrafe = false;

        if (!isPerformingAction && hasWeapon)
        {
            if (wantsToAim || wantsToFire)
            {
                // Hard lock when aiming/shooting
                wantsToCombatStrafe = true;
                wantsToSprint = false; // Force sprint off
            }
            else if (!wantsToSprint)
            {
                // Tactical walking: Always face forward to backpedal and side-step!
                wantsToCombatStrafe = true;
            }
        }
        // --------------------------------------

        if (!wantsToCombatStrafe && !isPerformingAction)
        {
            if (currentMoveDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(currentMoveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }
        else if (wantsToCombatStrafe)
        {
            // Face the aim target physically
            Vector3 lookDir = (currentLookPosition - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * rotationSpeed * 1.2f);
            }
        }

        if (characterController.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
        else verticalVelocity += gravity * Time.deltaTime;

        float speedLimit = moveSpeed;
        if (isPerformingAction)
        {
            speedLimit = 0f;
            currentMoveDirection = Vector3.zero;
        }
        else if (wantsToCombatStrafe) speedLimit = aimMoveSpeed;
        else if (wantsToSprint && currentMoveDirection.magnitude > 0.1f) speedLimit = sprintSpeed;

        Vector3 velocity = currentMoveDirection * speedLimit;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleAnimations()
    {
        // Checks the bridge booleans AND asks the Animator if the Reload state is currently playing
        bool isPerformingAction = (animatorBridge != null && (animatorBridge.IsReloading || animatorBridge.IsHealing)) || 
                                animator.GetCurrentAnimatorStateInfo(1).IsName("Reload") || 
                                animator.GetNextAnimatorStateInfo(1).IsName("Reload");

        // --- PUBG TACTICAL LOCOMOTION LOGIC ---
        bool wantsToCombatStrafe = false;

        if (!isPerformingAction && hasWeapon)
        {
            if (wantsToAim || wantsToFire)
            {
                // Hard lock when aiming/shooting
                wantsToCombatStrafe = true;
                wantsToSprint = false; // Force sprint off
            }
            else if (!wantsToSprint)
            {
                // Tactical walking: Always face forward to backpedal and side-step!
                wantsToCombatStrafe = true;
            }
        }
        // --------------------------------------

        // Convert world movement into local animation space for the Blend Tree
        Vector3 localMove = transform.InverseTransformDirection(currentMoveDirection);
        Vector2 structuralInput = isPerformingAction ? Vector2.zero : new Vector2(localMove.x, localMove.z);

        if (structuralInput.sqrMagnitude > 1f) structuralInput.Normalize();

        float targetSpeedValue = structuralInput.magnitude;
        if (wantsToSprint && structuralInput.y > 0.1f && !isPerformingAction) targetSpeedValue *= 2f;

        currentAnimX = Mathf.Lerp(currentAnimX, structuralInput.x, Time.deltaTime * 10f);
        currentAnimY = Mathf.Lerp(currentAnimY, structuralInput.y, Time.deltaTime * 10f);
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeedValue, Time.deltaTime * 10f);

        animator.SetBool("IsAiming", wantsToCombatStrafe);
        animator.SetFloat("Speed", currentSpeed);
        animator.SetFloat("MoveX", currentAnimX);
        animator.SetFloat("MoveY", currentAnimY);
        animator.SetBool("IsGrounded", characterController.isGrounded);
        animator.SetBool("HasWeapon", hasWeapon);

        if (!hasWeapon)
        {
            aimLayerWeight = Mathf.MoveTowards(aimLayerWeight, 0f, Time.deltaTime * 8f);
            animator.SetLayerWeight(1, aimLayerWeight);
            if (aimRig != null) aimRig.weight = 0f;
            if (weaponMesh != null) weaponMesh.SetActive(false);
        }
        else
        {
            if (weaponMesh != null && !weaponMesh.activeSelf) weaponMesh.SetActive(true);

            if (isPerformingAction)
            {
                aimLayerWeight = 1f;
                animator.SetLayerWeight(1, 1f);
            }
            else
            {
                float targetWeight = wantsToCombatStrafe ? 1f : 0f;
                aimLayerWeight = Mathf.MoveTowards(aimLayerWeight, targetWeight, Time.deltaTime * 8f);
                animator.SetLayerWeight(1, aimLayerWeight);
                if (aimRig != null) aimRig.weight = aimLayerWeight;
            }
        }
    }


    private void HandleShooting()
        {
            // 1. Are we holding the fire button, holding a gun, and aiming?
            if (wantsToFire && equippedWeapon != null && hasWeapon && aimLayerWeight >= 0.9f)
            {
                // 2. Are we currently busy with an animation? If yes, ignore the fire button.
                if (animatorBridge != null && (animatorBridge.IsReloading || animatorBridge.IsHealing)) return;
                
                // 3. STRICT MANUAL FIRE: Only shoot if we actually have bullets!
                if (equippedWeapon.Model.CurrentAmmo > 0) 
                {
                    equippedWeapon.PullTrigger(currentLookPosition);
                }
                else
                {
                    // 4. Out of ammo! Do not auto-reload. Just stop the firing animation.
                    // (In the future, you can play an empty "click" sound effect right here).
                    equippedWeapon.StopFiring();
                }
            }
            else if (equippedWeapon != null)
            {
                // 5. Button released or dropped aim.
                equippedWeapon.StopFiring();
            }
        }
    }