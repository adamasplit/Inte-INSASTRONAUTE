using UnityEngine;
using UnityEngine.UI;

public class AudioButtonUI : MonoBehaviour
{
    private Button button;
    [SerializeField] private AudioClip audioClip;

    private void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(PlayAudio);
    }

    private void PlayAudio()
    {
        if (audioClip == null) return;

        GameObject go = new GameObject("OneShotAudio");
        AudioSource src = go.AddComponent<AudioSource>();
        src.pitch = Random.Range(0.9f, 1.1f);
        src.PlayOneShot(audioClip, 0.5f);
        Destroy(go, audioClip.length / src.pitch);
    }
}