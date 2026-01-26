using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    public AudioSource musicSource; // Drag the Looping Source here
    public AudioSource sfxSource;   // Drag the Non-Looping Source here

    [Header("Music Clips")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;

    [Header("SFX Clips")]
    public AudioClip kickSound;
    public AudioClip superShotSound; // Optional
    public AudioClip goalSound;
    public AudioClip curseSound; // The explosion sound
    public AudioClip winSound;
    public AudioClip clickSound;

    void Awake()
    {
        // Singleton pattern: simple version for each scene
        instance = this;
    }

    void Start()
    {
        // Automatically play the right music based on the scene name
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "MainMenu")
        {
            PlayMusic(menuMusic);
        }
        else // We are in the Game
        {
            PlayMusic(gameMusic);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip != null)
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            // PlayOneShot lets sounds overlap (e.g. rapid kicks)
            sfxSource.PlayOneShot(clip);
        }
    }

    // Helper function for Buttons to use directly
    public void PlayClickSound()
    {
        PlaySFX(clickSound);
    }
}