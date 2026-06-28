using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "Gun", menuName = "Guns/Gun", order = 0)]
public class GunScriptableObject : ScriptableObject
{
    [Header("Combat Stats")]
    public int Damage = 25; 

    public ImpactType ImpactType;
    public GunType Type;
    public string Name;

    [Header("Weapon Audio")]
    [Tooltip("The audio clip played from the gun muzzle point whenever it fires.")]
    public AudioClip fireSFX; // New: Unique gun sound slot

    [Header("Configurations")]
    public ShootConfigurationScriptableObject ShootConfig;
    public TrailConfigScriptableObject TrailConfig;

    private MonoBehaviour ActiveMonoBehaviour;
    private ParticleSystem MuzzleFlash;
    private Transform MuzzleTransform;
    
    private float LastShootTime;
    private ObjectPool<TrailRenderer> TrailPool;

    public void Initialize(MonoBehaviour activeMono, Transform muzzle, ParticleSystem flash)
    {
        ActiveMonoBehaviour = activeMono;
        MuzzleTransform = muzzle;
        MuzzleFlash = flash;
        
        LastShootTime = 0;
        
        if (TrailPool != null) TrailPool.Clear();
        TrailPool = new ObjectPool<TrailRenderer>(CreateTrail);
    }

    public void Shoot(Vector3 targetPoint)
    {
        if (Time.time > ShootConfig.FireRate + LastShootTime)
        {
            LastShootTime = Time.time;

            if (MuzzleFlash != null) MuzzleFlash.Play();

            // NEW: Instantly play the gunshot audio from the muzzle position
            if (fireSFX != null && MuzzleTransform != null)
            {
                AudioSource.PlayClipAtPoint(fireSFX, MuzzleTransform.position, 1f);
            }

            Vector3 rawDirection = (targetPoint - MuzzleTransform.position).normalized;
            Vector3 shootDirection = rawDirection + new Vector3(
                Random.Range(-ShootConfig.Spread.x, ShootConfig.Spread.x),
                Random.Range(-ShootConfig.Spread.y, ShootConfig.Spread.y),
                Random.Range(-ShootConfig.Spread.z, ShootConfig.Spread.z)
            );
            shootDirection.Normalize();

            Vector3 origin = MuzzleTransform.position;

            if (Physics.Raycast(origin, shootDirection, out RaycastHit hit, float.MaxValue, ShootConfig.HitMask))
            {
                ActiveMonoBehaviour.StartCoroutine(PlayTrail(origin, hit.point, hit));
            }
            else
            {
                ActiveMonoBehaviour.StartCoroutine(PlayTrail(origin, origin + (shootDirection * TrailConfig.MissDistance), new RaycastHit()));
            }
        }
    }

    private IEnumerator PlayTrail(Vector3 StartPoint, Vector3 EndPoint, RaycastHit Hit)
    {
        TrailRenderer instance = TrailPool.Get();
        instance.gameObject.SetActive(true);
        instance.transform.position = StartPoint;
        instance.Clear(); 

        yield return null; 

        instance.emitting = true;
        float distance = Vector3.Distance(StartPoint, EndPoint);
        float remainingDistance = distance;
        
        while (remainingDistance > 0)
        {
            instance.transform.position = Vector3.Lerp(
                StartPoint,
                EndPoint,
                Mathf.Clamp01(1 - (remainingDistance / distance))
            );
            remainingDistance -= TrailConfig.SimulationSpeed * Time.deltaTime;
            yield return null;
        }

        instance.transform.position = EndPoint;

        // --- PROCESSED HIT RESULTS ---
        if (Hit.collider != null)
        {
            GameObject hitTarget = Hit.transform.gameObject;
            float impactSizeMultiplier = 1.0f; 
            BodyPart detectedPart = BodyPart.Body; // New: Tracks part identity for audio loops

            // 1. Deliver Damage and Detect specialized Body Parts
            HitboxRegion hitRegion = Hit.collider.GetComponent<HitboxRegion>();
            if (hitRegion != null)
            {
                hitRegion.ReceiveHit(Damage);

                Transform rootTransform = hitRegion.transform.root; 
                hitTarget = rootTransform.gameObject;
                detectedPart = hitRegion.partType; // Pass structural enum data

                if (hitRegion.partType == BodyPart.Head)
                {
                    impactSizeMultiplier = 2.0f; 
                    DeathHandler targetDeath = hitTarget.GetComponent<DeathHandler>();
                    if (targetDeath != null)
                    {
                        targetDeath.killedByHeadshot = true;
                    }
                }
                else if (hitRegion.partType == BodyPart.Body)
                {
                    impactSizeMultiplier = 0.5f; 
                }
            }

            // 2. Call SurfaceManager passing our structural size and bone part identity
            if (SurfaceManager.Instance != null)
            {
                SurfaceManager.Instance.HandleImpact(
                    hitTarget,
                    Hit.point, 
                    Hit.normal,
                    impactSizeMultiplier,
                    detectedPart // Injected parameter triggers unique sound overrides
                );
            }
        }

        yield return new WaitForSeconds(TrailConfig.Duration);
        
        instance.emitting = false;
        instance.gameObject.SetActive(false);
        TrailPool.Release(instance);
    }

    private TrailRenderer CreateTrail()
    {
        GameObject instance = new GameObject("Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>();
        trail.colorGradient = TrailConfig.Color;
        trail.material = TrailConfig.Material;
        trail.widthCurve = TrailConfig.WidthCurve;
        trail.time = TrailConfig.Duration;
        trail.minVertexDistance = TrailConfig.MinVertexDistance;
        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return trail;
    }
}
