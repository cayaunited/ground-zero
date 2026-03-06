using UnityEngine;

namespace GroundZero
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] [Min(0)] private float _intensityFadeDuration;
        [SerializeField] private AudioSource[] _backgroundMusicSources;
        
        private int _currentIntensity;
        private int _nextIntensity;
        private float _intensityFadeTimer;
        private bool _shouldCrossfade;
        
        private void Update()
        {
            var currentSource = _backgroundMusicSources[_currentIntensity];
            var nextSource = _backgroundMusicSources[_nextIntensity];
            
            // Only crossfade the music tracks when near the end of one and if the intensity changes.
            if (currentSource.time >= currentSource.clip.length - _intensityFadeDuration && !_shouldCrossfade)
            {
                _shouldCrossfade = true;
                _nextIntensity = _currentIntensity + 1;
                if (_nextIntensity >= _backgroundMusicSources.Length) _nextIntensity = 0;
            }
            // Once the sources change which one has volume, end the crossfade.
            else if (currentSource.time < _intensityFadeDuration && _shouldCrossfade)
            {
                _shouldCrossfade = false;
                _currentIntensity = _nextIntensity;
                _intensityFadeTimer = 0;
            }
            
            if (!_shouldCrossfade) return;
            
            _intensityFadeTimer = Mathf.Min(_intensityFadeTimer + Time.deltaTime, _intensityFadeDuration);
            var newCurrentVolume = Vector2.Lerp(new Vector2(1, 0), new Vector2(0, 0), _intensityFadeTimer / _intensityFadeDuration).x;
            
            // Crossfade the music tracks to have the opposite volume of each other.
            currentSource.volume = newCurrentVolume;
            nextSource.volume = 1 - newCurrentVolume;
        }
    }
}
