using UnityEngine;

public class DrawingBoardSounds : MonoBehaviour
{
    AudioSource soundPlayer;
    [SerializeField] private AudioClip _popSound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        soundPlayer = GetComponent<AudioSource>();
    }

    void PlayPopSound(float pitch)
    {
        soundPlayer.generator = _popSound;
        soundPlayer.pitch = pitch;
        soundPlayer.Play();
    }
}
