using UnityEngine;

namespace GroundZero
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] [Min(0)] private float _intensityFadeDuration;
        [SerializeField] private float _normalPitch;
        [Tooltip("The pitch during the last bit of time in a timed game.")]
        [SerializeField] private float _warningPitch;
        [SerializeField] [Min(0)] private float _pitchFadeDuration;
        [SerializeField] [Range(0, 1)] private float _musicVolume;
        [Tooltip("The minimum volume an SFX can randomly play at.")]
        [SerializeField] [Range(0, 1)] private float _minSFXVolume;
        [Tooltip("The maximum volume an SFX can randomly play at.")]
        [SerializeField] [Range(0, 1)] private float _maxSFXVolume;
        [Tooltip("The minimum pitch an SFX can randomly play at.")]
        [SerializeField] [Min(0)] private float _minSFXPitch;
        [Tooltip("The maximum pitch an SFX can randomly play at.")]
        [SerializeField] [Min(0)] private float _maxSFXPitch;
        [SerializeField] private AudioSource[] _backgroundMusicSources;
        [SerializeField] private AudioSource _soundEffectsSource;
        [SerializeField] private AudioClip _selectSFX;
        [SerializeField] private AudioClip _swapSFX;
        [SerializeField] private AudioClip _scoreSFX;
        [SerializeField] private AudioClip _levelUpSFX;
        [SerializeField] private AudioClip _startGameSFX;
        [SerializeField] private AudioClip _endGameSFX;
        [SerializeField] private AudioClip _warningSFX;
        [SerializeField] private AudioClip[] _tickSFX;
        [SerializeField] private AudioClip[] _explosionSFX;
        [SerializeField] private AudioClip _createExplosiveSFX;
        [SerializeField] private AudioClip _createTargetingSFX;
        [SerializeField] private AudioClip _refillGridSFX;
        [SerializeField] private AudioClip _dropSFX;
        [SerializeField] private AudioClip _endDropSFX;
        
        private int _currentIntensity;
        private int _nextIntensity;
        private float _intensityFadeTimer;
        private bool _shouldCrossfadeMusic;
        private bool _shouldFadePitch;
        private float _pitchFadeTimer;
        private float _pitch;
        private float _startingPitch;
        private float _targetPitch;
        private int _currentWarningTick;
        
        private void Awake()
        {
            _pitch = _normalPitch;
            
            // Make sure the first music track is audible, and every other is muted.
            for (int i = 0; i < _backgroundMusicSources.Length; i++)
            {
                _backgroundMusicSources[i].volume = i == 0 ? _musicVolume : 0;
            }
        }
        
        private void Update()
        {
            if (_shouldCrossfadeMusic)
            {
                var currentSource = _backgroundMusicSources[_currentIntensity];
                var nextSource = _backgroundMusicSources[_nextIntensity];
                
                _intensityFadeTimer = Mathf.Min(_intensityFadeTimer + Time.deltaTime, _intensityFadeDuration);
                var newCurrentVolume = Vector2.Lerp(new Vector2(_musicVolume, 0), new Vector2(0, 0), _intensityFadeTimer / _intensityFadeDuration).x;
                
                // Crossfade the music tracks to have the opposite volume of each other.
                currentSource.volume = newCurrentVolume;
                nextSource.volume = _musicVolume - newCurrentVolume;
                
                if (Mathf.Approximately(_intensityFadeTimer, _intensityFadeDuration))
                {
                    _shouldCrossfadeMusic = false;
                    _currentIntensity = _nextIntensity;
                    _intensityFadeTimer = 0;
                }
            }
            
            // Slowly increase or decrease pitch as needed around the warning time.
            if (_shouldFadePitch)
            {
                _pitchFadeTimer = Mathf.Min(_pitchFadeTimer + Time.deltaTime, _pitchFadeDuration);
                SetPitch(Vector2.Lerp(new Vector2(_startingPitch, 0), new Vector2(_targetPitch, 0), _pitchFadeTimer / _pitchFadeDuration).x);
                if (Mathf.Approximately(_pitchFadeTimer, 0)) _shouldFadePitch = false;
            }
        }
        
        public void OnLevelUp()
        {
            var nextIntensity = _currentIntensity + 1;
            if (nextIntensity >= _backgroundMusicSources.Length) nextIntensity = 0;
            SetIntensity(nextIntensity);
            PlaySoundEffect(_levelUpSFX);
        }
        
        public void OnStartWarning()
        {
            FadePitchTo(_warningPitch);
            PlaySoundEffect(_warningSFX);
            _currentWarningTick = 0;
        }
        
        public void OnGameEnded()
        {
            SetIntensity(0);
            FadePitchTo(_normalPitch);
            PlaySoundEffect(_endGameSFX);
        }
        
        public void PlaySelectSFX() => PlaySoundEffect(_selectSFX);
        public void PlaySwapSFX() => PlaySoundEffect(_swapSFX);
        public void PlayScoreSFX() => PlaySoundEffect(_scoreSFX);
        public void PlayStartSFX() => PlaySoundEffect(_startGameSFX);
        public void PlayExplosionSFX() => PlaySoundEffect(_explosionSFX[Random.Range(0, _explosionSFX.Length)]);
        public void PlayCreateExplosiveSFX() => PlaySoundEffect(_createExplosiveSFX);
        public void PlayCreateTargetingSFX() => PlaySoundEffect(_createTargetingSFX);
        public void PlayRefillGridSFX() => PlaySoundEffect(_refillGridSFX);
        public void PlayDropSFX() => PlaySoundEffect(_dropSFX);
        public void PlayEndDropSFX() => PlaySoundEffect(_endDropSFX);
        
        public void PlayTickSFX()
        {
            PlaySoundEffect(_tickSFX[_currentWarningTick]);
            _currentWarningTick++;
            if (_currentWarningTick >= _tickSFX.Length) _currentWarningTick = 0;
        }
        
        private void SetIntensity(int intensity)
        {
            if (_currentIntensity != intensity) _shouldCrossfadeMusic = true;
            _nextIntensity = intensity;
            _intensityFadeTimer = 0;
        }
        
        private void FadePitchTo(float targetPitch)
        {
            _shouldFadePitch = true;
            _pitchFadeTimer = 0;
            _startingPitch = _pitch;
            _targetPitch = targetPitch;
        }
        
        private void SetPitch(float pitch)
        {
            _pitch = pitch;
            
            foreach (var source in _backgroundMusicSources)
            {
                source.pitch = pitch;
            }
        }
        
        private void PlaySoundEffect(AudioClip effectClip)
        {
            _soundEffectsSource.pitch = Random.Range(_minSFXPitch, _maxSFXPitch);
            _soundEffectsSource.PlayOneShot(effectClip, Random.Range(_minSFXVolume, _maxSFXVolume));
        }
    }
}
