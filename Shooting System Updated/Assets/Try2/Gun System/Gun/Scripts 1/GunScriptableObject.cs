using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "Gun", menuName = "Guns/Gun", order = 0)]
public class GunScriptableObject : ScriptableObject
{
    [Header("Combat Stats")]
    public int Damage = 25; // WE ADDED THE DAMAGE VARIABLE HERE!

    public ImpactType ImpactType;
    public GunType Type;
    public string Name;

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
                
                // 1. Teleport to the muzzle
                instance.transform.position = StartPoint;
                
                // 2. CRITICAL FIX: Erase the geometry from the previous bullet!
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

                // --- THE CRITICAL FIXES ARE HERE ---
                if (Hit.collider != null)
                {
                    // 1. Send the damage to the Hitbox!
                    HitboxRegion hitRegion = Hit.collider.GetComponent<HitboxRegion>();
                    if (hitRegion != null)
                    {
                        hitRegion.ReceiveHit(Damage);
                    }

                    // 2. Safe check for the SurfaceManager to prevent the crash!
                    if (SurfaceManager.Instance != null)
                    {
                        SurfaceManager.Instance.HandleImpact(
                            Hit.transform.gameObject,
                            EndPoint,
                            Hit.normal,
                            ImpactType,
                            0
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