using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class SoundFx : MonoBehaviour
{
    AudioSource source;

    [SerializeField]
    private float duration;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        source = GetComponent<AudioSource>();
        duration = source.clip.length;
        StartCoroutine(DestoryThis());
    }

    IEnumerator DestoryThis()
    {
        yield return new WaitForSeconds(duration);
        Destroy (gameObject);
    }
}
