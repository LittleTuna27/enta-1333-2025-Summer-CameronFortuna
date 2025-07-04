using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class SoundPracticePlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public SoundEntry[] sounds;

    // Dictionary for fast lookup
    private Dictionary<SoundEnum, AudioClip> soundDictionary;
    public static SoundPracticePlayer Instance { get; private set; }

    void Awake()
    {
        // Singleton setup
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

        // Initialize audio source
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Build dictionary for fast lookup
        BuildSoundDictionary();
    }

    void BuildSoundDictionary()
    {
        soundDictionary = new Dictionary<SoundEnum, AudioClip>();

        foreach (SoundEntry sound in sounds)
        {
            if (!soundDictionary.ContainsKey(sound.soundType))
            {
                soundDictionary.Add(sound.soundType, sound.audioClip);
            }
            else
            {
                Debug.LogWarning($"Duplicate sound type found: {sound.soundType}");
            }
        }
    }

    // Main method to play sounds
    public void PlaySound(SoundEnum soundType)
    {
        if (soundDictionary.TryGetValue(soundType, out AudioClip clip))
        {
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning($"Audio clip for {soundType} is null!");
            }
        }
        else
        {
            Debug.LogWarning($"Sound type {soundType} not found!");
        }
    }
}

[System.Serializable]
public class SoundEntry
{
    public SoundEnum soundType;
    public AudioClip audioClip;
}