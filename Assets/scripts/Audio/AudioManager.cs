using UnityEngine;
using System.Collections;
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
        public bool isMusic;
    }

    public List<Sound> sounds;
    public Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

    private float musicVolume = 1f;
    private float sfxVolume   = 1f;

    public const string PREF_MUSIC_VOL = "MusicVolume";
    public const string PREF_SFX_VOL   = "SFXVolume";

    private Coroutine playlistCoroutine;
    private string[] currentPlaylistNames;

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

        foreach (var snd in sounds)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.clip = snd.clip;
            src.volume = snd.volume;
            src.loop = snd.loop;
            audioSources[snd.name] = src;
        }

        musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 20f);
        sfxVolume   = PlayerPrefs.GetFloat(PREF_SFX_VOL,   20f);
        ApplyVolumesToAll();
    }

    private void ApplyVolumesToAll()
    {
        foreach (var snd in sounds)
        {
            var src = audioSources[snd.name];
            src.volume = snd.isMusic
                ? snd.volume * musicVolume
                : snd.volume * sfxVolume;
        }
    }

    public void SetMusicVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat(PREF_MUSIC_VOL, musicVolume);
        PlayerPrefs.Save();
        ApplyVolumesToAll();
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat(PREF_SFX_VOL, sfxVolume);
        PlayerPrefs.Save();
        ApplyVolumesToAll();
    }

    public void PlaySound(string name)
    {
        if (audioSources.TryGetValue(name, out var src))
        {
            src.loop = false;
            src.Play();
        }
    }

    public void StopSound(string name)
    {
        if (audioSources.TryGetValue(name, out var src))
        {
            src.Stop();
        }
    }

    public void PlayPlaylist(params string[] names)
    {
        StopPlaylist();
        currentPlaylistNames = names;
        playlistCoroutine = StartCoroutine(PlaySequence(names));
    }

    public void StopPlaylist()
    {
        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
            playlistCoroutine = null;
        }
        if (currentPlaylistNames != null)
        {
            foreach (var n in currentPlaylistNames)
                StopSound(n);
            currentPlaylistNames = null;
        }
    }

    private IEnumerator PlaySequence(string[] names)
    {
        foreach (var name in names)
        {
            if (audioSources.TryGetValue(name, out var src))
            {
                src.loop = false;
                src.Play();
                yield return new WaitForSeconds(src.clip.length);
            }
            else
            {
                Debug.LogWarning($"AudioManager: No sound named '{name}' found.");
            }
        }
        playlistCoroutine = null;
        currentPlaylistNames = null;
    }
}