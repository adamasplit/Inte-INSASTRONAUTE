using UnityEngine;
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }
    public AudioSource audioSource;
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
        AudioClip clip = GetSound(soundName);
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}