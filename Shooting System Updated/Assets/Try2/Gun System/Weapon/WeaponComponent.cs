using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterAnimatorBridge))]
public class WeaponComponent : MonoBehaviour
{
    [Header("Weapon Stats")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float fireRate = 0.15f;
    [SerializeField] private float reloadTime = 2.0f;

    [Header("Physical References")]
    [Tooltip("Drop your GunScriptableObject data here")]
    public GunScriptableObject gunData;
    
    [Tooltip("Drag the MuzzlePoint from your equipped gun here")]
    public Transform muzzlePoint;
    
    [Tooltip("Drag the ParticleSystem from your muzzle flash here")]
    public ParticleSystem muzzleFlash;

    public WeaponModel Model { get; private set; }
    private CharacterAnimatorBridge animatorBridge;

    private void Awake()
    {
        animatorBridge = GetComponent<CharacterAnimatorBridge>();
        
        // Initialize your pure C# data model
        Model = new WeaponModel(magazineSize, fireRate, reloadTime);
    }

    private void Start()
    {
        // Initialize the Scriptable Object logic using the physical gun already in the scene
        if (gunData != null)
        {
            gunData.Initialize(this, muzzlePoint, muzzleFlash);
        }
    }

    // The PlayerController or AI Brain calls this method to shoot
    public void PullTrigger(Vector3 targetPoint)
    {
        if (!Model.CanShoot()) return;
        
        Model.ConsumeAmmo();
        animatorBridge.SetFiring(true);

        if (gunData != null)
        {
            // CRITICAL: We pass the targetPoint, NOT muzzlePoint.forward!
            gunData.Shoot(targetPoint); 
        }
    }

    public void StopFiring()
    {
        animatorBridge.SetFiring(false);
    }

    public void TryReload()
    {
        if (Model.IsReloading || Model.CurrentAmmo == Model.MaxAmmo) return;

        Model.StartReload();
        animatorBridge.PlayReload(); // This will trigger the IK fade in Phase 3
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(reloadTime);
        Model.FinishReload();
    }

    public void ResetWeapon()
    {
        StopAllCoroutines();
        if (Model != null) Model.ResetAmmo();
        if (animatorBridge != null) animatorBridge.SetFiring(false);
    }
}