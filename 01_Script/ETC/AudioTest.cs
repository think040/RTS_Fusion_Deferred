using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class AudioTest : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] audioClips;

    int cIdx = 0;
    int cCount;
    float volume = 0.5f;
    public bool isPlaying;

    // Start is called before the first frame update
    void Start()
    {
        cCount = audioClips.Length;
        audioSource.clip = audioClips[0];
        audioSource.volume = volume;               
    }

    IEnumerator AudioState()
    {
        while(audioSource.isPlaying)
        {
            yield return null;
        }

        Debug.Log($"audioSource.isPlaying : {audioSource.isPlaying}");
    }

    // Update is called once per frame
    void Update()
    {
        {
            isPlaying = audioSource.isPlaying;
        }

        if(Input.GetKeyDown(KeyCode.Q))
        {
            cIdx = (cIdx + 1) % cCount;
            audioSource.clip = audioClips[cIdx];

            audioSource.Play();
            StartCoroutine(AudioState());

            Debug.Log("PlayList Up");
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            cIdx = cIdx - 1 < 0 ? cCount - 1 : (cIdx - 1);
            audioSource.clip = audioClips[cIdx];

            audioSource.Play();
            StartCoroutine(AudioState());

            Debug.Log("PlayList Down");
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            audioSource.Play();
            StartCoroutine(AudioState());

            Debug.Log("Play()");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            audioSource.Stop();

            Debug.Log("Stop()");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            audioSource.Pause();

            Debug.Log("Pause()");
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            audioSource.UnPause();

            Debug.Log("UnPause()");
        }


        if (Input.GetKeyDown(KeyCode.R))
        {
            volume += 0.1f;
            volume = Mathf.Clamp(volume, 0.0f, 1.0f);
            audioSource.volume = volume;

            Debug.Log("Volume Up");
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            volume -= 0.1f;
            volume = Mathf.Clamp(volume, 0.0f, 1.0f);
            audioSource.volume = volume;

            Debug.Log("Volume Down");
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            audioSource.mute = !audioSource.mute;

            Debug.Log($"Mute : {audioSource.mute}");
        }
        
    }
}
