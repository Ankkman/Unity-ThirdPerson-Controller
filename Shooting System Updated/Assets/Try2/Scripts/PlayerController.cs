using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging; 

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public GameObject aimVirtualCamera; // Handles the zoom camera
    public Transform mainCamera; 
    public GameObject cameraTarget; 
    public Transform worldAimTarget; 
    
    [Tooltip("Drag your AimRig GameObject here (the one with the Rig component)")]
    public Rig aimRig; 

    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float sprintSpeed = 7f; 
    public float aimMoveSpeed = 2f;
    public float jumpHeight = 1.5f; 
    public float rotationSpeed = 10f;
    private float gravity = -9.81f;
    private float verticalVelocity;

    [Header("Camera Look Settings")]
    public float lookSensitivity = 0.2f;
    public float topClamp = 70.0f;
    public float bottomClamp = -30.0f;
    private float cinemachineTargetYaw;
    private float cinemachineTargetPitch;

    [Header("Raycast Targeting Settings")]
    public LayerMask aimColliderMask;
    public float maxAimDistance = 100f;

    private Animator animator;
    private PlayerControls inputActions;
    private CharacterController characterController;
    private CharacterAnimatorBridge animatorBridge; 
    
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isAiming; // Tracks Right Click/ADS Zoom
    private bool isSprinting; 
    private float currentAnimX, currentAnimY, currentSpeed;
    private float aimLayerWeight;


    [Header("UI References")]
    public GameObject crosshairUI;

    [Header("Inventory Status")]
    public bool hasWeapon = true; // Set to false in Inspector if you want to start unarmed!

    [Header("Combat Settings")]
    public WeaponComponent equippedWeapon;
    public GameObject weaponMesh; // ADD THIS! We will use this to hide the gun.
    private bool isFiring;

    


    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        animatorBridge = GetComponent<CharacterAnimatorBridge>(); 
        inputActions = new PlayerControls();

        inputActions.Player.Aim.performed += ctx => isAiming = true;
        inputActions.Player.Aim.canceled += ctx => isAiming = false;

        inputActions.Player.Fire.performed += ctx => isFiring = true;
        inputActions.Player.Fire.canceled += ctx => isFiring = false;

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.Sprint.performed += ctx => isSprinting = true;
        inputActions.Player.Sprint.canceled += ctx => isSprinting = false;
        
        inputActions.Player.Jump.performed += ctx => PerformJump();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
        // 1. INPUT BINDING TO TEST HOLSTERING / DRAWING
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            hasWeapon = !hasWeapon;
            Debug.Log($"<color=cyan>Weapon Toggled. Equipped = {hasWeapon}</color>");
        }

        // 2. INPUT BINDING TO TEST HEALING PAUSE SYSTEM
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (animatorBridge != null)
            {
                Debug.Log("<color=green>H Key Pressed! Triggering PlayHeal...</color>");
                animatorBridge.PlayHeal();
            }
        }

        // Keep your original processing updates intact below
        HandleRaycastTarget();
        HandleMovement();
        HandleAnimations();
        HandleCameraToggle();
        HandleShooting();
    }


    private void LateUpdate()
    {
        CameraRotation();
    }

    private void HandleRaycastTarget()
    {
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(screenCenterPoint);

        if (Physics.Raycast(ray, out RaycastHit raycastHit, maxAimDistance, aimColliderMask))
        {
            worldAimTarget.position = raycastHit.point;
        }
        else
        {
            worldAimTarget.position = ray.GetPoint(maxAimDistance);
        }
        
        Debug.DrawLine(mainCamera.position, worldAimTarget.position, Color.blue);
        if (equippedWeapon != null && equippedWeapon.muzzlePoint != null)
        {
            Debug.DrawLine(equippedWeapon.muzzlePoint.position, worldAimTarget.position, Color.red);
            Debug.DrawRay(equippedWeapon.muzzlePoint.position, equippedWeapon.muzzlePoint.forward * maxAimDistance, Color.green);
        }
    }

    private void CameraRotation()
    {
        cinemachineTargetYaw += lookInput.x * lookSensitivity;
        cinemachineTargetPitch -= lookInput.y * lookSensitivity;
        cinemachineTargetPitch = Mathf.Clamp(cinemachineTargetPitch, bottomClamp, topClamp);

        cameraTarget.transform.rotation = Quaternion.Euler(cinemachineTargetPitch, cinemachineTargetYaw, 0.0f);
    }

    private void HandleMovement()
    {
        Vector3 forward = mainCamera.forward;
        Vector3 right = mainCamera.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

        // MASTER DETECTOR: Are we currently locked in an action?
        bool isPerformingAction = animatorBridge != null && (animatorBridge.IsReloading || animatorBridge.IsHealing);
        
        // Are we trying to perform ANY combat action (Aiming OR Hip-Firing)?
        bool wantsToCombatStrafe = (isAiming || isFiring) && hasWeapon && !isPerformingAction;

        if (wantsToCombatStrafe) 
        {
            isSprinting = false;
        }

        if (!wantsToCombatStrafe && !isPerformingAction) 
        {
            // FREE ROAM: Character physically turns their body
            if (moveDirection != Vector3.zero) 
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        } 
        else if (wantsToCombatStrafe)
        {
            // COMBAT STRAFING: Lock back to camera
            float yawCamera = mainCamera.eulerAngles.y;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, yawCamera, 0), Time.deltaTime * rotationSpeed * 1.2f);
        }

        if (characterController.isGrounded && verticalVelocity < 0) 
        {
            verticalVelocity = -2f;
        }
        else 
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        float currentSpeedLimit = moveSpeed;
        
        if (isPerformingAction) 
        {
            // STRICT LOCK: Kill physical movement speed completely while healing/reloading!
            currentSpeedLimit = 0f; 
            moveDirection = Vector3.zero; 
        }
        else if (wantsToCombatStrafe) 
        {
            currentSpeedLimit = aimMoveSpeed; 
        }
        else if (isSprinting && moveInput.y > 0.1f) 
        {
            currentSpeedLimit = sprintSpeed; 
        }

        Vector3 velocity = moveDirection * currentSpeedLimit;
        velocity.y = verticalVelocity;
        
        characterController.Move(velocity * Time.deltaTime);
    }

    private void PerformJump()
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump"); 
        }
    }

    private void HandleAnimations()
    {
        bool isPerformingAction = animatorBridge != null && (animatorBridge.IsReloading || animatorBridge.IsHealing);

        Vector2 structuralInput = moveInput;

        // STRICT ANIMATION LOCK: Force Animator to 'Idle' (0,0) while healing/reloading
        if (isPerformingAction)
        {
            structuralInput = Vector2.zero;
        }
        else if (structuralInput.sqrMagnitude > 1f) 
        {
            structuralInput.Normalize();
        }

        float targetSpeedValue = structuralInput.magnitude;
        if (isSprinting && moveInput.y > 0.1f && !isPerformingAction) targetSpeedValue *= 2f; 

        currentAnimX = Mathf.Lerp(currentAnimX, structuralInput.x, Time.deltaTime * 10f);
        currentAnimY = Mathf.Lerp(currentAnimY, structuralInput.y, Time.deltaTime * 10f);
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeedValue, Time.deltaTime * 10f);

        bool wantsToCombatStrafe = (isAiming || isFiring) && hasWeapon && !isPerformingAction;

        animator.SetBool("IsAiming", wantsToCombatStrafe);
        animator.SetFloat("Speed", currentSpeed);
        animator.SetFloat("MoveX", currentAnimX);
        animator.SetFloat("MoveY", currentAnimY);
        animator.SetBool("IsGrounded", characterController.isGrounded);
        animator.SetBool("HasWeapon", hasWeapon); 

        if (!hasWeapon)
        {
            // Smoothly fade the AimingLayer to 0 so the Base Layer takes full control!
            aimLayerWeight = Mathf.MoveTowards(aimLayerWeight, 0f, Time.deltaTime * 8f);
            animator.SetLayerWeight(1, aimLayerWeight);
            
            if (aimRig != null) aimRig.weight = 0f; 
            if (weaponMesh != null) weaponMesh.SetActive(false); 
        }
        else
        {
            // Restore weapon model visibility
            if (weaponMesh != null && !weaponMesh.activeSelf) 
            {
                weaponMesh.SetActive(true);
            }

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

                if (aimRig != null)
                {
                    aimRig.weight = aimLayerWeight;
                }
            }
        }
    }


    private void HandleCameraToggle()
    {
        // CAM FIXED: The camera virtual object zooming strictly checks isAiming (Right-click only!)
        aimVirtualCamera.SetActive(isAiming);
        
        if (crosshairUI != null) 
        {
            crosshairUI.SetActive(true); 
            UnityEngine.UI.Image crosshairImg = crosshairUI.GetComponent<UnityEngine.UI.Image>();
            if (crosshairImg != null)
            {
                Color c = crosshairImg.color;
                // High alert crosshair layout if aiming or shooting
                c.a = (isAiming || isFiring) ? 1f : 0.7f; 
                crosshairImg.color = c;
            }
        }
    }

    private void HandleShooting()
    {
        // INTERLOCK: Only allow shooting if we have a weapon AND our arms are actually raised!
        // We check if aimLayerWeight is close to 1f (arms are up)
        if (isFiring && equippedWeapon != null && hasWeapon && aimLayerWeight >= 0.9f)
        {
            // Do not allow shooting if we are in the middle of reloading or healing
            if (animatorBridge != null && (animatorBridge.IsReloading || animatorBridge.IsHealing)) return;

            equippedWeapon.PullTrigger(worldAimTarget.position);
        }
        else if (equippedWeapon != null)
        {
            // Always tell the weapon to stop firing if we let go of the button OR drop our arms
            equippedWeapon.StopFiring();
        }
    }

}
