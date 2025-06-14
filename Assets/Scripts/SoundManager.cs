using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private string _masterVolumeParam = "MasterVolume";
    [SerializeField] private string _bgmVolumeParam = "BGMVolume";
    [SerializeField] private string _sfxVolumeParam = "SFXVolume";
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioSource _uiSource;
    [SerializeField] private int _sfxSourcePoolSize = 10;
    
    [Header("BGM Clips")]
    [SerializeField] private AudioClip _mainMenuBGM;
    [SerializeField] private AudioClip _battleBGM;
    [SerializeField] private AudioClip _victoryBGM;
    [SerializeField] private AudioClip _defeatBGM;
    [SerializeField] private AudioClip _bossBGM;
    
    [Header("SFX Clips")]
    [SerializeField] private SoundEffect[] _soundEffects;
    
    [Header("UI Sound Clips")]
    [SerializeField] private AudioClip _buttonClickSound;
    [SerializeField] private AudioClip _purchaseSuccessSound;
    [SerializeField] private AudioClip _purchaseFailSound;
    [SerializeField] private AudioClip _levelUpSound;
    [SerializeField] private AudioClip _notificationSound;
    
    [Header("Settings")]
    [SerializeField] private float _bgmFadeTime = 1f;
    [SerializeField] private float _defaultSFXVolume = 1f;
    [SerializeField] private bool _enableSpatialSound = true;
    
    private Dictionary<string, AudioClip> _soundDictionary = new Dictionary<string, AudioClip>();
    private List<AudioSource> _sfxSourcePool = new List<AudioSource>();
    private Coroutine _bgmFadeCoroutine;
    private float _masterVolume = 1f;
    private float _bgmVolume = 1f;
    private float _sfxVolume = 1f;
    
    [System.Serializable]
    public class SoundEffect
    {
        public string soundName;
        public AudioClip clip;
        public float volume = 1f;
        public float pitch = 1f;
        public float minPitch = 0.95f;
        public float maxPitch = 1.05f;
        public bool randomizePitch = false;
        public bool loop = false;
        public AudioMixerGroup mixerGroup;
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeSoundDictionary();
        CreateSFXSourcePool();
        LoadVolumeSettings();
    }
    
    /// <summary>
    /// 사운드 딕셔너리 초기화
    /// </summary>
    private void InitializeSoundDictionary()
    {
        foreach (var soundEffect in _soundEffects)
        {
            if (!string.IsNullOrEmpty(soundEffect.soundName) && soundEffect.clip != null)
            {
                _soundDictionary[soundEffect.soundName] = soundEffect.clip;
            }
        }
    }
    
    /// <summary>
    /// SFX 소스 풀 생성
    /// </summary>
    private void CreateSFXSourcePool()
    {
        for (int i = 0; i < _sfxSourcePoolSize; i++)
        {
            GameObject sfxObject = new GameObject($"SFX Source {i}");
            sfxObject.transform.parent = transform;
            AudioSource source = sfxObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _sfxSourcePool.Add(source);
        }
    }
    
    /// <summary>
    /// 볼륨 설정 로드
    /// </summary>
    private void LoadVolumeSettings()
    {
        _masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        _bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        _sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        
        ApplyVolumeSettings();
    }
    
    /// <summary>
    /// 볼륨 설정 적용
    /// </summary>
    private void ApplyVolumeSettings()
    {
        if (_audioMixer != null)
        {
            _audioMixer.SetFloat(_masterVolumeParam, LinearToDecibel(_masterVolume));
            _audioMixer.SetFloat(_bgmVolumeParam, LinearToDecibel(_bgmVolume));
            _audioMixer.SetFloat(_sfxVolumeParam, LinearToDecibel(_sfxVolume));
        }
        
        if (_bgmSource) _bgmSource.volume = _bgmVolume;
        if (_sfxSource) _sfxSource.volume = _sfxVolume;
        if (_uiSource) _uiSource.volume = _sfxVolume;
    }
    
    /// <summary>
    /// BGM 재생
    /// </summary>
    public void PlayBGM(string bgmName, bool fade = true)
    {
        AudioClip clip = GetBGMClip(bgmName);
        if (clip == null) return;
        
        if (fade)
        {
            if (_bgmFadeCoroutine != null)
                StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = StartCoroutine(FadeBGM(clip));
        }
        else
        {
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }
    }
    
    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopBGM(bool fade = true)
    {
        if (fade)
        {
            if (_bgmFadeCoroutine != null)
                StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = StartCoroutine(FadeOutBGM());
        }
        else
        {
            _bgmSource.Stop();
        }
    }
    
    /// <summary>
    /// BGM 페이드
    /// </summary>
    private IEnumerator FadeBGM(AudioClip newClip)
    {
        // 페이드 아웃
        float startVolume = _bgmSource.volume;
        float timer = 0f;
        
        while (timer < _bgmFadeTime)
        {
            timer += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / _bgmFadeTime);
            yield return null;
        }
        
        // 클립 변경
        _bgmSource.Stop();
        _bgmSource.clip = newClip;
        _bgmSource.Play();
        
        // 페이드 인
        timer = 0f;
        while (timer < _bgmFadeTime)
        {
            timer += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(0f, _bgmVolume, timer / _bgmFadeTime);
            yield return null;
        }
        
        _bgmSource.volume = _bgmVolume;
    }
    
    /// <summary>
    /// BGM 페이드 아웃
    /// </summary>
    private IEnumerator FadeOutBGM()
    {
        float startVolume = _bgmSource.volume;
        float timer = 0f;
        
        while (timer < _bgmFadeTime)
        {
            timer += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / _bgmFadeTime);
            yield return null;
        }
        
        _bgmSource.Stop();
        _bgmSource.volume = _bgmVolume;
    }
    
    /// <summary>
    /// SFX 재생
    /// </summary>
    public void PlaySFX(string soundName, Vector3? position = null)
    {
        SoundEffect soundEffect = GetSoundEffect(soundName);
        if (soundEffect == null || soundEffect.clip == null) return;
        
        AudioSource source = GetAvailableSFXSource();
        if (source == null) return;
        
        source.clip = soundEffect.clip;
        source.volume = soundEffect.volume * _sfxVolume;
        source.pitch = soundEffect.randomizePitch ? 
            Random.Range(soundEffect.minPitch, soundEffect.maxPitch) : soundEffect.pitch;
        source.loop = soundEffect.loop;
        
        if (soundEffect.mixerGroup != null)
            source.outputAudioMixerGroup = soundEffect.mixerGroup;
        
        // 3D 사운드 설정
        if (position.HasValue && _enableSpatialSound)
        {
            source.transform.position = position.Value;
            source.spatialBlend = 1f; // 3D
        }
        else
        {
            source.spatialBlend = 0f; // 2D
        }
        
        source.Play();
    }
    
    /// <summary>
    /// UI 사운드 재생
    /// </summary>
    public void PlayUISound(UISound sound)
    {
        AudioClip clip = null;
        
        switch (sound)
        {
            case UISound.ButtonClick:
                clip = _buttonClickSound;
                break;
            case UISound.PurchaseSuccess:
                clip = _purchaseSuccessSound;
                break;
            case UISound.PurchaseFail:
                clip = _purchaseFailSound;
                break;
            case UISound.LevelUp:
                clip = _levelUpSound;
                break;
            case UISound.Notification:
                clip = _notificationSound;
                break;
        }
        
        if (clip != null && _uiSource != null)
        {
            _uiSource.PlayOneShot(clip, _sfxVolume);
        }
    }
    
    /// <summary>
    /// 원샷 사운드 재생
    /// </summary>
    public void PlayOneShot(AudioClip clip, float volume = 1f, Vector3? position = null)
    {
        if (clip == null) return;
        
        AudioSource source = GetAvailableSFXSource();
        if (source == null) return;
        
        if (position.HasValue && _enableSpatialSound)
        {
            source.transform.position = position.Value;
            source.spatialBlend = 1f;
        }
        else
        {
            source.spatialBlend = 0f;
        }
        
        source.PlayOneShot(clip, volume * _sfxVolume);
    }
    
    /// <summary>
    /// 루프 사운드 재생
    /// </summary>
    public AudioSource PlayLoopSound(string soundName, Transform target = null)
    {
        SoundEffect soundEffect = GetSoundEffect(soundName);
        if (soundEffect == null || soundEffect.clip == null) return null;
        
        GameObject loopObject = new GameObject($"Loop Sound - {soundName}");
        if (target != null)
        {
            loopObject.transform.parent = target;
            loopObject.transform.localPosition = Vector3.zero;
        }
        
        AudioSource loopSource = loopObject.AddComponent<AudioSource>();
        loopSource.clip = soundEffect.clip;
        loopSource.volume = soundEffect.volume * _sfxVolume;
        loopSource.pitch = soundEffect.pitch;
        loopSource.loop = true;
        loopSource.spatialBlend = target != null ? 1f : 0f;
        loopSource.Play();
        
        return loopSource;
    }
    
    /// <summary>
    /// 사용 가능한 SFX 소스 가져오기
    /// </summary>
    private AudioSource GetAvailableSFXSource()
    {
        foreach (var source in _sfxSourcePool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        
        // 모든 소스가 사용 중이면 첫 번째 소스 재사용
        return _sfxSourcePool[0];
    }
    
    /// <summary>
    /// BGM 클립 가져오기
    /// </summary>
    private AudioClip GetBGMClip(string bgmName)
    {
        switch (bgmName.ToLower())
        {
            case "mainmenu":
                return _mainMenuBGM;
            case "battle":
                return _battleBGM;
            case "victory":
                return _victoryBGM;
            case "defeat":
                return _defeatBGM;
            case "boss":
                return _bossBGM;
            default:
                Debug.LogWarning($"BGM '{bgmName}' not found!");
                return null;
        }
    }
    
    /// <summary>
    /// 사운드 이펙트 정보 가져오기
    /// </summary>
    private SoundEffect GetSoundEffect(string soundName)
    {
        foreach (var soundEffect in _soundEffects)
        {
            if (soundEffect.soundName == soundName)
            {
                return soundEffect;
            }
        }
        
        Debug.LogWarning($"Sound effect '{soundName}' not found!");
        return null;
    }
    
    /// <summary>
    /// 마스터 볼륨 설정
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MasterVolume", _masterVolume);
        ApplyVolumeSettings();
    }
    
    /// <summary>
    /// BGM 볼륨 설정
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("BGMVolume", _bgmVolume);
        ApplyVolumeSettings();
    }
    
    /// <summary>
    /// SFX 볼륨 설정
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", _sfxVolume);
        ApplyVolumeSettings();
    }
    
    /// <summary>
    /// 음소거 토글
    /// </summary>
    public void ToggleMute()
    {
        bool isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        isMuted = !isMuted;
        
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
        
        if (isMuted)
        {
            _audioMixer.SetFloat(_masterVolumeParam, -80f);
        }
        else
        {
            ApplyVolumeSettings();
        }
    }
    
    /// <summary>
    /// 선형 값을 데시벨로 변환
    /// </summary>
    private float LinearToDecibel(float linear)
    {
        if (linear <= 0f) return -80f;
        return 20f * Mathf.Log10(linear);
    }
    
    /// <summary>
    /// 모든 사운드 정지
    /// </summary>
    public void StopAllSounds()
    {
        _bgmSource.Stop();
        
        foreach (var source in _sfxSourcePool)
        {
            source.Stop();
        }
    }
    
    /// <summary>
    /// 일시 정지
    /// </summary>
    public void PauseAllSounds()
    {
        _bgmSource.Pause();
        
        foreach (var source in _sfxSourcePool)
        {
            if (source.isPlaying)
                source.Pause();
        }
    }
    
    /// <summary>
    /// 재개
    /// </summary>
    public void ResumeAllSounds()
    {
        _bgmSource.UnPause();
        
        foreach (var source in _sfxSourcePool)
        {
            source.UnPause();
        }
    }
    
    public enum UISound
    {
        ButtonClick,
        PurchaseSuccess,
        PurchaseFail,
        LevelUp,
        Notification
    }
}