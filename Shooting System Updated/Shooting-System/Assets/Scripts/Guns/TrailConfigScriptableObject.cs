using UnityEngine;

[CreateAssetMenu(fileName = "TrailConfig", menuName = "Guns/Trail Config")]
public class TrailConfigScriptableObject : ScriptableObject
{
    public Material material;
    //public AnimationCurve widthCurve;
    public Gradient colorGradient;
    public float duration = 0.2f;
    public float minVertexDistance = 0.1f;
    public float missDistance = 100f;
    //public float simulationSpeed = 100f;
}
