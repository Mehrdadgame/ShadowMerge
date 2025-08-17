using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip backgroundMusic;
    public AudioClip evaporationSound;
    public AudioClip shadowMergeSound;
    public AudioClip waterCollectSound;
    public AudioClip winSound;
    public AudioClip loseSound;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource effectsSource;

    public static AudioManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // تنظیم موزیک پس‌زمینه
            if (musicSource != null && backgroundMusic != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.loop = true;
                musicSource.volume = 0.3f;
                musicSource.Play();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayEffect(AudioClip clip, float volume = 1f)
    {
        if (effectsSource != null && clip != null)
        {
            effectsSource.PlayOneShot(clip, volume);
        }
    }

    public void PlayEvaporation() => PlayEffect(evaporationSound, 0.5f);
    public void PlayShadowMerge() => PlayEffect(shadowMergeSound, 0.7f);
    public void PlayWaterCollect() => PlayEffect(waterCollectSound, 0.8f);
    public void PlayWin() => PlayEffect(winSound, 1f);
    public void PlayLose() => PlayEffect(loseSound, 0.8f);
}