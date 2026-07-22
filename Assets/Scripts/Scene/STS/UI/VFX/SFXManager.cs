using UnityEngine;
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }
    public AudioSource audioSource;
    [Range(0f, 0.2f)] public float pitchRandomRange = 0.05f;
    public AudioClip GetSound(string soundName)
    {
        return Resources.Load<AudioClip>($"STS/SFX/{soundName}");
    }
    public int activeSoundsCount = 0;
    public bool activeSounds => activeSoundsCount > 0;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void PlaySound(string soundName)
    {
        if (audioSource == null)
            return;

        AudioClip clip = GetSound(soundName);
        if (clip != null)
        {
            float minPitch = Mathf.Max(0.01f, 1f - pitchRandomRange);
            float maxPitch = 1f + pitchRandomRange;
            float randomizedPitch = Random.Range(minPitch, maxPitch);

            // Use a dedicated source per sound so pitch randomization is applied reliably.
            AudioSource oneShotSource = gameObject.AddComponent<AudioSource>();
            oneShotSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            oneShotSource.volume = audioSource.volume;
            oneShotSource.spatialBlend = audioSource.spatialBlend;
            oneShotSource.panStereo = audioSource.panStereo;
            oneShotSource.reverbZoneMix = audioSource.reverbZoneMix;
            oneShotSource.dopplerLevel = audioSource.dopplerLevel;
            oneShotSource.rolloffMode = audioSource.rolloffMode;
            oneShotSource.minDistance = audioSource.minDistance;
            oneShotSource.maxDistance = audioSource.maxDistance;
            oneShotSource.priority = audioSource.priority;
            oneShotSource.pitch = randomizedPitch;
            oneShotSource.PlayOneShot(clip);

            float cleanupDelay = Mathf.Max(0.05f, clip.length / randomizedPitch) + 0.05f;
            StartCoroutine(DestroyOneShotSourceAfterDelay(oneShotSource, cleanupDelay));
        }
    }

    private System.Collections.IEnumerator DestroyOneShotSourceAfterDelay(AudioSource source, float delay)
    {
        activeSoundsCount++;
        yield return new WaitForSeconds(delay);
        if (source != null)
        {
            Destroy(source);
        }
        activeSoundsCount--;
    }
}