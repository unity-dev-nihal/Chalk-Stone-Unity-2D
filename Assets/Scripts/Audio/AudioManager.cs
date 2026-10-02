using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambienceSource;

    [Header("Background Music")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip ambienceClip;

    [Header("UI SFX")]
    [SerializeField] private AudioClip buttonClickClip;

    [Header("Gameplay SFX")]
    [SerializeField] private AudioClip gameStartClip;
    [SerializeField] private AudioClip tossFlipClip;
    [SerializeField] private AudioClip tossFallClip;
    [SerializeField] private AudioClip pawnPlacementClip;
    [SerializeField] private AudioClip pawnSlideClip;
    [SerializeField] private AudioClip invalidMoveClip;
    [SerializeField] private AudioClip drawDetectedClip;
    [SerializeField] private AudioClip drawClip;
    [SerializeField] private AudioClip victoryClip;

    [Header("Default Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultMusicVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float defaultSFXVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float defaultAmbienceVolume = 1f;

    private const string MusicVolumeKey = "Settings_MusicVolume";
    private const string SFXVolumeKey = "Settings_SFXVolume";
    private const string AmbienceVolumeKey = "Settings_AmbienceVolume";

    public float MusicVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public float AmbienceVolume { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        LoadVolumes();
        ApplyVolumes();
        ConfigureAudioSources();

        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log("AudioManager initialized.");
    }

    private void Start()
    {
        PlayAudioForCurrentScene();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        PlayAudioForCurrentScene();
    }

    private void PlayAudioForCurrentScene()
    {
        string sceneName =
            SceneManager.GetActiveScene().name;

        if (sceneName == "Main Menu")
        {
            PlayMainMenuAudio();
        }
        else if (sceneName == "GameScene")
        {
            PlayGameplayAudio();
        }
        else
        {
            Debug.Log(
                $"AudioManager: No specific music configured for scene '{sceneName}'."
            );
        }
    }

    private void ConfigureAudioSources()
    {
        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
        }

        if (sfxSource != null)
        {
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
        }

        if (ambienceSource != null)
        {
            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
            ambienceSource.spatialBlend = 0f;
        }
    }

    private void LoadVolumes()
    {
        MusicVolume =
            PlayerPrefs.GetFloat(
                MusicVolumeKey,
                defaultMusicVolume
            );

        SFXVolume =
            PlayerPrefs.GetFloat(
                SFXVolumeKey,
                defaultSFXVolume
            );

        AmbienceVolume =
            PlayerPrefs.GetFloat(
                AmbienceVolumeKey,
                defaultAmbienceVolume
            );
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = MusicVolume;

        if (sfxSource != null)
            sfxSource.volume = SFXVolume;

        if (ambienceSource != null)
            ambienceSource.volume = AmbienceVolume;
    }

    // =========================================================
    // BACKGROUND AUDIO
    // =========================================================

    public void PlayMainMenuAudio()
    {
        PlayMusic(mainMenuMusic);
        PlayAmbience(ambienceClip);

        Debug.Log("Main Menu audio started.");
    }

    public void PlayGameplayAudio()
    {
        PlayMusic(gameplayMusic);
        PlayAmbience(ambienceClip);

        Debug.Log("Gameplay audio started.");
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null)
            return;

        if (musicSource.clip == clip &&
            musicSource.isPlaying)
        {
            return;
        }

        musicSource.Stop();

        musicSource.clip = clip;
        musicSource.loop = true;

        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void PlayAmbience(AudioClip clip)
    {
        if (ambienceSource == null || clip == null)
            return;

        if (ambienceSource.clip == clip &&
            ambienceSource.isPlaying)
        {
            return;
        }

        ambienceSource.Stop();

        ambienceSource.clip = clip;
        ambienceSource.loop = true;

        ambienceSource.Play();
    }

    public void StopAmbience()
    {
        if (ambienceSource != null)
            ambienceSource.Stop();
    }

    // =========================================================
    // SFX
    // =========================================================

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(
            clip,
            SFXVolume
        );
    }

    public void PlayButtonClick()
    {
        PlaySFX(buttonClickClip);
    }

    public void PlayGameStart()
    {
        PlaySFX(gameStartClip);
    }

    public void PlayTossFlip()
    {
        PlaySFX(tossFlipClip);
    }

    public void PlayTossFall()
    {
        PlaySFX(tossFallClip);
    }

    public void PlayPawnPlacement()
    {
        PlaySFX(pawnPlacementClip);
    }

    public void PlayPawnSlide()
    {
        PlaySFX(pawnSlideClip);
    }

    public void PlayInvalidMove()
    {
        PlaySFX(invalidMoveClip);
    }

    public void PlayDrawDetected()
    {
        PlaySFX(drawDetectedClip);
    }

    public void PlayDraw()
    {
        PlaySFX(drawClip);
    }

    public void PlayVictory()
    {
        PlaySFX(victoryClip);
    }

    // =========================================================
    // VOLUME SETTINGS
    // =========================================================

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);

        if (musicSource != null)
            musicSource.volume = MusicVolume;

        PlayerPrefs.SetFloat(
            MusicVolumeKey,
            MusicVolume
        );

        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);

        if (sfxSource != null)
            sfxSource.volume = SFXVolume;

        PlayerPrefs.SetFloat(
            SFXVolumeKey,
            SFXVolume
        );

        PlayerPrefs.Save();
    }

    public void SetAmbienceVolume(float value)
    {
        AmbienceVolume = Mathf.Clamp01(value);

        if (ambienceSource != null)
            ambienceSource.volume = AmbienceVolume;

        PlayerPrefs.SetFloat(
            AmbienceVolumeKey,
            AmbienceVolume
        );

        PlayerPrefs.Save();
    }

    public float GetMusicVolume()
    {
        return MusicVolume;
    }

    public float GetSFXVolume()
    {
        return SFXVolume;
    }

    public float GetAmbienceVolume()
    {
        return AmbienceVolume;
    }
}