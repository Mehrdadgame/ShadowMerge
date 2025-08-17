using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    [Header("Particle Effects")]
    public ParticleSystem evaporationEffect;
    public ParticleSystem shadowMergeEffect;
    public ParticleSystem waterCollectEffect;
    public ParticleSystem winEffect;

    public static ParticleManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayEvaporation(Vector3 position)
    {
        if (evaporationEffect != null)
        {
            var effect = Instantiate(evaporationEffect, position, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }
    }

    public void PlayShadowMerge(Vector3 position)
    {
        if (shadowMergeEffect != null)
        {
            var effect = Instantiate(shadowMergeEffect, position, Quaternion.identity);
            Destroy(effect.gameObject, 1.5f);
        }
    }

    public void PlayWaterCollect(Vector3 position)
    {
        if (waterCollectEffect != null)
        {
            var effect = Instantiate(waterCollectEffect, position, Quaternion.identity);
            Destroy(effect.gameObject, 1f);
        }
    }

    public void PlayWinEffect(Vector3 position)
    {
        if (winEffect != null)
        {
            var effect = Instantiate(winEffect, position, Quaternion.identity);
            Destroy(effect.gameObject, 3f);
        }
    }
}
