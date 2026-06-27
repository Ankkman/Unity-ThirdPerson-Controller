using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct SurfaceEffect
{
    public string surfaceTag; 
    public GameObject vfxPrefab;
    public AudioClip sfxClip; // New: Unique sound per surface type!
}

public class SurfaceManager : MonoBehaviour
{
    public static SurfaceManager Instance;

    [Header("Impact Configurations")]
    public GameObject defaultImpactVFX;
    public AudioClip defaultImpactSFX; // New: Default wall ricochet audio clip
    public float globalImpactScale = 0.03f; 
    public List<SurfaceEffect> surfaceEffects;

    [Header("Special Flesh Audio Overrides")]
    [Tooltip("Sound played for standard body or limb hits on characters.")]
    public AudioClip bodyHitSFX;
    [Tooltip("Highly distinct sound played strictly for critical headshots.")]
    public AudioClip headshotSFX;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // UPDATED: Now takes an explicit BodyPart parameter to dynamically swap audio pitches
    public void HandleImpact(GameObject hitObject, Vector3 pos, Vector3 normal, float sizeMultiplier = 1f, BodyPart hitPart = BodyPart.Body)
    {
        string hitTag = hitObject.tag;
        GameObject vfxToSpawn = defaultImpactVFX;
        AudioClip sfxToPlay = defaultImpactSFX;

        // 1. Process standard tag checking for visuals and audio
        foreach (var effect in surfaceEffects)
        {
            if (effect.surfaceTag == hitTag)
            {
                vfxToSpawn = effect.vfxPrefab;
                sfxToPlay = effect.sfxClip;
                break;
            }
        }

        // 2. CRITICAL AUDIO OVERRIDE: Swap out generic flesh sounds for custom body/headshot tracking
        if (hitTag == "Player" || hitTag == "Enemy")
        {
            if (hitPart == BodyPart.Head)
            {
                sfxToPlay = headshotSFX;
            }
            else
            {
                sfxToPlay = bodyHitSFX;
            }
        }

        // 3. Play the 3D spatial sound effect at the exact bullet contact pixel
        if (sfxToPlay != null)
        {
            // AudioSource.PlayClipAtPoint automatically creates a temporary 3D audio object and handles cleanup
            AudioSource.PlayClipAtPoint(sfxToPlay, pos, 1f); 
        }

        // 4. Handle Visual Spawning (Preserved from your working setup)
        if (vfxToSpawn != null)
        {
            Vector3 spawnPosition = pos + (normal * 0.01f);
            Quaternion spawnRotation = Quaternion.LookRotation(normal, Vector3.up);

            float surfaceAlignment = Mathf.Abs(Vector3.Dot(normal, Vector3.up));
            if (surfaceAlignment > 0.7f) 
            {
                spawnRotation = Quaternion.LookRotation(normal, Vector3.up);
            }

            GameObject spawnedVFX = Instantiate(vfxToSpawn, spawnPosition, spawnRotation);
            spawnedVFX.transform.SetParent(hitObject.transform);
            spawnedVFX.transform.localScale = Vector3.one * globalImpactScale * sizeMultiplier;

            Destroy(spawnedVFX, 1.5f); 
        }
    }
}
