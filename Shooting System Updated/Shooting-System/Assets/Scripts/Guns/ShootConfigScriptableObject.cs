using UnityEngine;

[CreateAssetMenu(fileName = "ShootConfig", menuName = "Guns/Shoot Config")]
public class ShootConfigScriptableObject : ScriptableObject
{
    public LayerMask hitMask;
    public float fireRate = 0.2f;
    public float damage = 20f;
    public Vector3 spread = Vector3.zero;
}
