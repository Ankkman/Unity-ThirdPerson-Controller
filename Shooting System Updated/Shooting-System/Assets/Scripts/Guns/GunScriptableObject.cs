using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

[CreateAssetMenu(fileName = "Gun", menuName = "Guns/Gun")]
public class GunScriptableObject : ScriptableObject
{
    public GunType type;
    public ShootConfigScriptableObject shootConfig;
    public TrailConfigScriptableObject trailConfig;

    //Impact Effect

    public GameObject impactEffectPrefab;


 
    private ObjectPool<TrailRenderer> trailPool;
    private MonoBehaviour activeMonoBehaviour;

    public void Initialize(MonoBehaviour mono)
    {
        activeMonoBehaviour = mono;

        trailPool = new ObjectPool<TrailRenderer>(
            CreateTrail,
            OnTakeTrailFromPool,
            OnReturnTrailToPool
        );
    }

    public void TryShoot(Vector3 origin, Vector3 direction)
    {
        

        direction = ApplySpread(direction);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, trailConfig.missDistance, shootConfig.hitMask))
        {
            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(shootConfig.damage);
                }

                if (impactEffectPrefab != null)
                {
                    GameObject impact = Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(impact, 1f);
                }

        }
        else
        {
            Vector3 missPoint = origin + direction * trailConfig.missDistance;
            activeMonoBehaviour.StartCoroutine(PlayTrail(origin, missPoint));
        }
    }

    private Vector3 ApplySpread(Vector3 direction)
    {
        direction += new Vector3(
            Random.Range(-shootConfig.spread.x, shootConfig.spread.x),
            Random.Range(-shootConfig.spread.y, shootConfig.spread.y),
            Random.Range(-shootConfig.spread.z, shootConfig.spread.z)
        );

        return direction.normalized;
    }

    private IEnumerator PlayTrail(Vector3 start, Vector3 end)
    {
        TrailRenderer trail = trailPool.Get();

        trail.gameObject.SetActive(false);
        trail.Clear();

        yield return null; // wait one frame to fully reset

        trail.transform.position = start;
        trail.gameObject.SetActive(true);

        trail.AddPosition(start);
        trail.AddPosition(end);

        yield return new WaitForSeconds(trailConfig.duration);

        trail.gameObject.SetActive(false);
        trailPool.Release(trail);
    }




    private TrailRenderer CreateTrail()
    {
        GameObject obj = new GameObject("BulletTrail");
        TrailRenderer trail = obj.AddComponent<TrailRenderer>();

        trail.material = trailConfig.material;
        
        // trail.widthCurve = trailConfig.widthCurve;
        trail.startWidth = 0.05f;
        trail.endWidth = 0.01f;
        trail.time = trailConfig.duration;


        trail.colorGradient = trailConfig.colorGradient;
        trail.time = trailConfig.duration;
        trail.minVertexDistance = trailConfig.minVertexDistance;

        obj.SetActive(false);
        return trail;
    }

    private void OnTakeTrailFromPool(TrailRenderer trail) { }

    private void OnReturnTrailToPool(TrailRenderer trail) { }
}
