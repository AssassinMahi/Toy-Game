using UnityEngine;

/// <summary>Plays a looping background track with an Inspector volume control.</summary>
public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic activeMusic;

    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.35f;

    private AudioSource musicSource;

    private void Awake()
    {
        if (activeMusic != null && activeMusic != this)
        {
            Destroy(gameObject);
            return;
        }

        activeMusic = this;
        DontDestroyOnLoad(gameObject);

        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = volume;

        if (musicClip != null)
            musicSource.Play();
        else
            Debug.LogWarning("Assign a music clip to BackgroundMusic.", this);
    }

    private void OnValidate()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (musicSource != null)
            musicSource.volume = volume;
    }

    private void OnDestroy()
    {
        if (activeMusic == this)
            activeMusic = null;
    }
}
