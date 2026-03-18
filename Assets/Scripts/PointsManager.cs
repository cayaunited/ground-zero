using System.Collections.Generic;
using UnityEngine;

namespace GroundZero
{
    public class PointsManager : MonoBehaviour
    {
        [SerializeField] private int _pointsPerMatchedGem;
        [Tooltip("How much the score multiplier should increase for each round in a row of gems being destroyed / falling.")]
        [SerializeField] private float _scoreMultiplierIncrementAmount;
        [SerializeField] private int _startingPointsPerLevel;
        [Tooltip("The amount to increase the number of points needed per level every time the level increases.")]
        [SerializeField] private int _additionalPointsPerLevel;
        
        [SerializeField] private PointsEffect _effectPrefab;
        [SerializeField] private PointsUI _pointsUI;
        [SerializeField] private AudioManager _audioManager;
        
        private int _score;
        private float _scoreMultiplier = 1;
        private int _level = 1;
        private int _scoreInLevel;
        private int _pointsPerLevel;
        private readonly List<PointsEffect> _activeEffects = new();
        private readonly Stack<PointsEffect> _inactiveEffects = new();
        
        private void Update()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                _activeEffects[i].OnUpdate();
            }
            
            _pointsUI.OnUpdate();
        }
        
        /// <summary>
        /// Initializes the points system and UI, resetting data back to the start.
        /// </summary>
        public void Initialize()
        {
            _score = 0;
            _scoreMultiplier = 1;
            _level = 1;
            _scoreInLevel = 0;
            _pointsPerLevel = _startingPointsPerLevel;
            _pointsUI.Initialize();
            
            foreach (var effect in _activeEffects)
            {
                effect.gameObject.SetActive(false);
                _inactiveEffects.Push(effect);
            }
            
            _activeEffects.Clear();
        }
        
        /// <summary>
        /// Increases the score based on the number of gems destroyed and the score multiplier.
        /// </summary>
        /// <param name="positions">The positions (in world space) of each gem destroyed.</param>
        public void ScorePoints(List<Vector2> positions)
        {
            // First, increase the score, calculating the increase per gem and total based on the multiplier.
            var amountPerGem = Mathf.RoundToInt(_scoreMultiplier * _pointsPerMatchedGem);
            var totalIncreaseAmount = amountPerGem * positions.Count;
            _score += totalIncreaseAmount;
            _scoreInLevel += totalIncreaseAmount;
            
            // Then, animate text effects at each gem's position.
            foreach (var position in positions)
            {
                var pointsEffect = GetEffect();
                pointsEffect.Initialize(position, amountPerGem, RecycleEffect);
                _activeEffects.Add(pointsEffect);
            }
            
            _pointsUI.OnPointsIncrease(_score, totalIncreaseAmount, 100f * _scoreInLevel / _pointsPerLevel);
            var leveledUp = false;
            
            // Level up if enough points have been scored,
            // resetting the number of points scored in the level,
            // but allowing the points scored to overflow into the next level,
            // leveling up multiple times if needed (just in case, but not likely).
            while (_scoreInLevel > _pointsPerLevel)
            {
                leveledUp = true;
                _level++;
                _scoreInLevel -= _pointsPerLevel;
                _pointsPerLevel += _additionalPointsPerLevel;
            }
            
            // Only update the level UI after all the increases.
            if (leveledUp)
            {
                _pointsUI.OnLevelUp(_level, 100f * _scoreInLevel / _pointsPerLevel);
                _audioManager.OnLevelUp();
            }
            else _audioManager.PlayScoreSFX();
        }
        
        public void IncreaseMultiplier() => _scoreMultiplier += _scoreMultiplierIncrementAmount;
        public void ResetMultiplier() => _scoreMultiplier = 1;
        
        private PointsEffect GetEffect() => _inactiveEffects.Count > 0
            ? _inactiveEffects.Pop() : Instantiate(_effectPrefab);
        
        private void RecycleEffect(PointsEffect effect)
        {
            _activeEffects.Remove(effect);
            _inactiveEffects.Push(effect);
            effect.gameObject.SetActive(false);
        }
    }
}
