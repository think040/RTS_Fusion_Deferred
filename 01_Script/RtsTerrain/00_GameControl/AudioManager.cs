using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    
    [Header("Effect")]
    public GameObject prefab_audio;
    public int _count = 32;
    public static int count { get; set; }

    static AudioSource[] audioSource;

    [Header("BGM")]
    public AudioSource audioSource_bgm;
    public AudioClip[] clip_bgm;
    public float[] volume_bgm;

    int cCount_bgm;
    int cIdx_bgm = 0;

    public bool bBGM_test = true;

    public void Init()
    {
        instance = this;

        //Effect
        {
            count = _count;
            audioSource = new AudioSource[count];
            for (int i = 0; i < count; i++)
            {
                audioSource[i] = GameObject.Instantiate(prefab_audio, Vector3.zero, Quaternion.identity).GetComponent<AudioSource>();
            }
        }

        //BGM
        {
            cCount_bgm = clip_bgm.Length;
            cIdx_bgm = 0;
            //audioSource_bgm.clip = clip_bgm[cIdx_bgm];

            BGM_Play();
        }
        
    }

    #region Effect
    public static void Play(float3 pos, AudioClip clip, float volume = 1.0f)
    {
        var ad = GetAudio();

        if (ad != null)
        {
            ad.transform.position = pos;
            ad.clip = clip;
            ad.mute = false;
            ad.volume = math.clamp(volume, 0.0f, 1.0f);
            ad.Play();
        }
    }

    public static void PauseAll()
    {
        for(int i = 0; i < count; i++)
        {
            audioSource[i].Pause();
        }
    }

    public static void UnPauseAll()
    {
        for (int i = 0; i < count; i++)
        {
            audioSource[i].UnPause();
        }
    }

    public static void MuteAll(bool value)
    {
        for (int i = 0; i < count; i++)
        {
            audioSource[i].mute = value;
        }
    }

    static AudioSource GetAudio()
    {
        for (int i = 0; i < count; i++)
        {
            AudioSource a = audioSource[i];
            if(!a.isPlaying)
            {
                return audioSource[i];
            }
        }

        return null;
    }
    #endregion


    #region BGM
    public void BGM_List_Down()
    {
        cIdx_bgm = (cIdx_bgm + 1) % cCount_bgm;
        audioSource_bgm.clip = clip_bgm[cIdx_bgm];
        audioSource_bgm.volume = volume_bgm[cIdx_bgm];

        audioSource_bgm.Play();
    }

    public void BGM_List_Up()
    {
        cIdx_bgm = (cIdx_bgm - 1 < 0) ? (cCount_bgm - 1) : (cIdx_bgm - 1);
        audioSource_bgm.clip = clip_bgm[cIdx_bgm];
        audioSource_bgm.volume = volume_bgm[cIdx_bgm];

        audioSource_bgm.Play();
    }

    public void BGM_Play()
    {
        audioSource_bgm.clip = clip_bgm[cIdx_bgm];
        audioSource_bgm.volume = volume_bgm[cIdx_bgm];

        audioSource_bgm.Play();
    }

    public void BGM_Stop()
    {
        audioSource_bgm.Stop();
    }

    public void BGM_Pause()
    {
        audioSource_bgm.Pause();        
    }

    public void BGM_UnPause()
    {
        audioSource_bgm.UnPause();
    }

    public void BGM_Pause_UnPause()
    {
        if (audioSource_bgm.isPlaying)
        {
            audioSource_bgm.Pause();
        }
        else
        {
            audioSource_bgm.UnPause();
        }       
    }

    public void BGM_Volume_Up()
    {
        var v = audioSource_bgm.volume;

        v += 0.05f;
        v = Mathf.Clamp(v, 0.0f, 1.0f);

        audioSource_bgm.volume = v;
    }

    public void BGM_Volume_Down()
    {
        var v = audioSource_bgm.volume;

        v -= 0.05f;
        v = Mathf.Clamp(v, 0.0f, 1.0f);

        audioSource_bgm.volume = v;
    }

    public void BGM_Mute(bool value)
    {
        audioSource_bgm.mute = value;
    }


    #endregion

    public void Enable()
    {
        
    }

    public void Disable()
    {
       
    }

    public void Begin()
    {

    }

   

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Y))
        {
            BGM_List_Up();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            BGM_List_Down();
        }


        if (Input.GetKeyDown(KeyCode.U))
        {
            BGM_Pause_UnPause();
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            BGM_Mute(!audioSource_bgm.mute);
        }


        if (Input.GetKeyDown(KeyCode.I))
        {
            BGM_Volume_Up();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            BGM_Volume_Down();
        }


        if (Input.GetKeyDown(KeyCode.O))
        {
            BGM_Play();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            BGM_Stop();
        }       

    }
}
