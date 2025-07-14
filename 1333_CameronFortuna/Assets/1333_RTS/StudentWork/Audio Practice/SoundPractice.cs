using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Enum for different audio source types
public enum AudioSourceType
{
    SFX,
    UI,
    Music
}

public class SoundPracticePlayer : MonoBehaviour
{
    [Header("Audio Sources - Assign in Inspector")]
    public AudioSource sfxAudioSource;
    public AudioSource uiAudioSource;
    public AudioSource musicAudioSource;

    [Header("Sound Library - Drag Audio Clips Here")]
    public List<AudioClip> audioClips = new List<AudioClip>();

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

        // Initialize default audio source if not assigned
        if (sfxAudioSource == null)
            sfxAudioSource = GetComponent<AudioSource>();
    }

    // Main method to play sounds by index
    public void PlaySound(int clipIndex, AudioSourceType sourceType = AudioSourceType.SFX)
    {
        AudioSource targetSource = GetAudioSource(sourceType);
        if (targetSource == null)
        {
            Debug.LogWarning($"AudioSource type {sourceType} is not assigned!");
            return;
        }

        if (clipIndex >= 0 && clipIndex < audioClips.Count)
        {
            AudioClip clip = audioClips[clipIndex];
            if (clip != null)
            {
                targetSource.clip = clip;
                targetSource.loop = false; // Ensure it's not looping
                targetSource.Play();
            }
            else
            {
                Debug.LogWarning($"Audio clip at index {clipIndex} is null!");
            }
        }
        else
        {
            Debug.LogWarning($"Audio clip index {clipIndex} is out of range! Available clips: {audioClips.Count}");
        }
    }

    public void PlayOneShot(int clipIndex, AudioSourceType sourceType = AudioSourceType.SFX)
    {
        AudioSource targetSource = GetAudioSource(sourceType);
        if (targetSource == null)
        {
            Debug.LogWarning($"AudioSource type {sourceType} is not assigned!");
            return;
        }

        if (clipIndex >= 0 && clipIndex < audioClips.Count)
        {
            AudioClip clip = audioClips[clipIndex];
            if (clip != null)
            {
                targetSource.PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning($"Audio clip at index {clipIndex} is null!");
            }
        }
        else
        {
            Debug.LogWarning($"Audio clip index {clipIndex} is out of range! Available clips: {audioClips.Count}");
        }
    }

    public void PlayLoopingSound(int clipIndex, AudioSourceType sourceType = AudioSourceType.SFX)
    {
        AudioSource targetSource = GetAudioSource(sourceType);
        if (targetSource == null)
        {
            Debug.LogWarning($"AudioSource type {sourceType} is not assigned!");
            return;
        }

        if (clipIndex >= 0 && clipIndex < audioClips.Count)
        {
            AudioClip clip = audioClips[clipIndex];
            if (clip != null)
            {
                targetSource.clip = clip;
                targetSource.loop = true;
                targetSource.Play();
            }
            else
            {
                Debug.LogWarning($"Audio clip at index {clipIndex} is null!");
            }
        }
        else
        {
            Debug.LogWarning($"Audio clip index {clipIndex} is out of range! Available clips: {audioClips.Count}");
        }
    }

    // Utility methods for controlling specific audio sources
    public void StopSound(AudioSourceType sourceType)
    {
        AudioSource targetSource = GetAudioSource(sourceType);
        if (targetSource != null)
        {
            targetSource.Stop();
        }
    }

    public void PauseSound(AudioSourceType sourceType)
    {
        AudioSource targetSource = GetAudioSource(sourceType);
        if (targetSource != null)
        {
            targetSource.Pause();
        }
    }

    public void ResumeSound(AudioSourceType sourceType)
    {
        AudioSource targetSource = GetAudioSource(sourceType);
        if (targetSource != null)
        {
            targetSource.UnPause();
        }
    }

    public void SetVolume(AudioSourceType sourceType, float volume)
    {
        AudioSource targetSource = GetAudioSource(sourceType);
        if (targetSource != null)
        {
            targetSource.volume = Mathf.Clamp01(volume);
        }
    }

    public bool IsPlaying(AudioSourceType sourceType)
    {
        AudioSource targetSource = GetAudioSource(sourceType);
        return targetSource != null && targetSource.isPlaying;
    }

    public void SetLoop(AudioSourceType sourceType, bool loop)
    {
        AudioSource targetSource = GetAudioSource(sourceType);
        if (targetSource != null)
        {
            targetSource.loop = loop;
        }
    }

    // Get the appropriate AudioSource based on the enum
    private AudioSource GetAudioSource(AudioSourceType sourceType)
    {
        switch (sourceType)
        {
            case AudioSourceType.SFX:
                return sfxAudioSource;
            case AudioSourceType.UI:
                return uiAudioSource;
            case AudioSourceType.Music:
                return musicAudioSource;
            default:
                return sfxAudioSource;
        }
    }

    // Helper method to get AudioSource directly (for advanced usage)
    public AudioSource GetAudioSourceDirect(AudioSourceType sourceType)
    {
        return GetAudioSource(sourceType);
    }

    // Helper method to get total number of audio clips
    public int GetAudioClipCount()
    {
        return audioClips.Count;
    }

    // Helper method to get audio clip name by index (useful for debugging)
    public string GetAudioClipName(int index)
    {
        if (index >= 0 && index < audioClips.Count && audioClips[index] != null)
        {
            return audioClips[index].name;
        }
        return "Invalid Index";
    }
}