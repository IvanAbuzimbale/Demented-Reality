using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

    #region Singleton
        private AudioListener listener;
        public static AudioManager Instance { get; private set; }
        public List<AudioSource> activeAudioSources = new();
    #endregion

    // SFX Struct and List
    [Serializable]
    struct SFX
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
        [Range(-3f, 3f)] public float pitch;
    }
    [SerializeField] private List<SFX> audioClips;


    private void Awake()
    {
        // Singleton pattern — bail out BEFORE building the pool if a manager already
        // exists, otherwise a duplicate (e.g. on scene reload) spawns 50 throwaway
        // sources before destroying itself.
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create a pool of audio sources
        for (int i = 0; i < 50; i++) {
            AudioSource newAudioSource = new GameObject($"AudioSource_{i}").AddComponent<AudioSource>();
            newAudioSource.transform.parent = transform;
            activeAudioSources.Add(newAudioSource);
        }
    }

    // Play random SFX by names with optional randomization and spatial settings
    public void PlaySFX(string[] names, bool randomizePitch = false, (float, float)? pitchRange = null, (float, float)? volumeRange = null, bool loop = false, Vector2? position = null, float? range = null)
    {
        if (names == null || names.Length == 0) return;
        // Randomly select a name from the array and play the corresponding SFX
        PlaySFX(names[UnityEngine.Random.Range(0, names.Length)], randomizePitch, pitchRange, volumeRange, loop, position, range);
    }

    // Play single SFX by name with optional randomization and spatial settings
    public void PlaySFX(string name, bool randomizePitch = false, (float, float)? pitchRange = null, (float, float)? volumeRange = null, bool loop = false, Vector2? position = null, float? range = null)
    {
        // Find the AudioListener if not already assigned
        if (listener == null) listener = FindAnyObjectByType<AudioListener>();
        // Find the SFX by name
        SFX sfx = audioClips.Find(s => s.name == name);

        if (sfx.clip != null)
        {
            // Find an available audio source from the pool
            AudioSource audioSource = activeAudioSources.Find(a => !a.isPlaying);
            if (audioSource != null) {
                // Configure the audio source with the SFX settings
                audioSource.clip = sfx.clip;
                audioSource.volume = sfx.volume;
                audioSource.loop = loop;

                // Apply random volume and pitch if specified
                if (volumeRange != null)
                    audioSource.volume = sfx.volume +
                        UnityEngine.Random.Range(volumeRange.Value.Item1, volumeRange.Value.Item2);
                
                // Randomize pitch if specified
                if (randomizePitch)
                    audioSource.pitch = sfx.pitch + 
                        UnityEngine.Random.Range(pitchRange.Value.Item1, pitchRange.Value.Item2);
                else audioSource.pitch = sfx.pitch;

                // Set spatial settings if position is provided
                if (position != null) {
                    // Only play if within range of the listener
                    float maxDist = range.GetValueOrDefault(1f) * 2f;
                    Vector3 listenerPos = listener.transform.position;
                    if (Vector2.Distance(position.Value, listenerPos) > maxDist) return;

                    // Configure spatial settings for 2D sound
                    audioSource.transform.position = new Vector3(position.Value.x, position.Value.y, 0f);
                    audioSource.spatialBlend = 1f;
                    audioSource.rolloffMode = AudioRolloffMode.Linear;
                    audioSource.minDistance = range.GetValueOrDefault(1f);
                    audioSource.maxDistance = maxDist;
                } else {
                    audioSource.spatialBlend = 0f;
                }                
                
                audioSource.Play();
            }
            else Debug.LogWarning("No available audio sources to play the sound.");
        }
        else {
            Debug.LogWarning($"SFX with name '{name}' not found.");
        }
    }
}
