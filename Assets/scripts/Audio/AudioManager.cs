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
    }

    public List<Sound> sounds;
    private Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

    // Coroutine handle for playlist
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

        foreach (var sound in sounds)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = sound.clip;
            source.volume = sound.volume;
            source.loop = sound.loop;
            audioSources[sound.name] = source;
        }
    }

    /// <summary>
    /// Play a single sound instantly.
    /// </summary>
    public void PlaySound(string name)
    {
        if (audioSources.TryGetValue(name, out var src))
        {
            src.loop = false;
            src.Play();
        }
    }

    /// <summary>
    /// Stop a specific sound.
    /// </summary>
    public void StopSound(string name)
    {
        if (audioSources.TryGetValue(name, out var src))
        {
            src.Stop();
        }
    }

    /// <summary>
    /// Play multiple tracks sequentially.
    /// </summary>
    /// <param name="names">Names of sounds in order.</param>
    public void PlayPlaylist(params string[] names)
    {
        // Stop any existing playlist first
        StopPlaylist();

        currentPlaylistNames = names;
        playlistCoroutine = StartCoroutine(PlaySequence(names));
    }

    /// <summary>
    /// Stop the current playlist sequence and all its sounds.
    /// </summary>
    public void StopPlaylist()
    {
        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
            playlistCoroutine = null;
        }

        if (currentPlaylistNames != null)
        {
            foreach (var name in currentPlaylistNames)
            {
                StopSound(name);
            }
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
                // wait for clip to finish
                yield return new WaitForSeconds(src.clip.length);
            }
            else
            {
                Debug.LogWarning($"AudioManager: No sound named '{name}' found.");
            }
        }

        // Playlist finished
        playlistCoroutine = null;
        currentPlaylistNames = null;
    }
}