using UnityEngine;
using UnityEngine.UI;

public class AudioButtonUI : MonoBehaviour
{
    private Button button;
    [SerializeField ] private AudioSource audioSource;
    [SerializeField ] private AudioClip audioClip;

    private void Start()
    {
        if (button == null)
            button = GetComponent<Button>();
         if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        button.onClick.AddListener(PlayAudio);
    }

    private void PlayAudio()
    {
        if (audioSource != null && audioClip != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(audioClip, 0.5f);
        }
    }
}