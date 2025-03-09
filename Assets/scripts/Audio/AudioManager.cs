using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop;
    }

    public List<Sound> sounds;
    private Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (var sound in sounds)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = sound.clip;
            source.volume = sound.volume;
            source.loop = sound.loop;
            audioSources[sound.name] = source;
        }
    }

    public void PlaySound(string name)
    {
        if (audioSources.ContainsKey(name))
        {
            audioSources[name].Play();
        }
      
    }

    public void StopSound(string name)
    {
        if (audioSources.ContainsKey(name))
        {
            audioSources[name].Stop();
        }
    }
}
