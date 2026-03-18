using UnityEngine;
using UnityEngine.UIElements;

namespace GroundZero
{
    [RequireComponent(typeof(UIDocument))]
    public class PointsUI : MonoBehaviour
    {
        [SerializeField] [Min(0)] private float _fadeInDuration;
        [SerializeField] [Min(0)] private float _fadeOutDuration;
        
        private UIDocument _document;
        private Label _pointsLabel;
        private Label _pointsIncreaseLabel;
        private Label _levelLabel;
        private VisualElement _levelFill;
        private VisualElement _pointsContainer;
        private VisualElement _levelContainer;
        
        private bool _shouldFadePointIncrease;
        private bool _shouldFadeLevelIncrease;
        private float _pointsFadeInTimer;
        private float _pointsFadeOutTimer;
        private float _levelFadeInTimer;
        private float _levelFadeOutTimer;
        private int _nextLevel;
        
        /// <summary>
        /// Initializes the points / level text with the correct values.
        /// Retrieves any needed components that haven't been retrieved already.
        /// </summary>
        public void Initialize()
        {
            // If the needed UI components haven't been fetched yet, then find them.
            if (!_document)
            {
                _document = GetComponent<UIDocument>();
                _pointsLabel = _document.rootVisualElement.Q<Label>("Points");
                _pointsIncreaseLabel = _document.rootVisualElement.Q<Label>("PointsIncrease");
                _levelLabel = _document.rootVisualElement.Q<Label>("Level");
                _levelFill = _document.rootVisualElement.Q<VisualElement>("LevelFill");
                _pointsContainer = _document.rootVisualElement.Q<VisualElement>("PointsContainer");
                _levelContainer = _document.rootVisualElement.Q<VisualElement>("LevelContainer");
            }
            
            // Reset the UI visuals with the starting values.
            _pointsContainer.style.display = DisplayStyle.Flex;
            _levelContainer.style.display = DisplayStyle.Flex;
            _pointsLabel.text = "0";
            _pointsIncreaseLabel.style.display = DisplayStyle.None;
            _levelLabel.text = "1";
            _levelLabel.style.opacity = 1;
            _levelFill.style.height = new Length(0, LengthUnit.Percent);
        }
        
        /// <summary>
        /// Updates any points-related animations.
        /// </summary>
        public void OnUpdate()
        {
            if (_shouldFadePointIncrease)
            {
                if (_pointsFadeInTimer < _fadeInDuration)
                {
                    _pointsFadeInTimer = Mathf.Min(_pointsFadeInTimer + Time.deltaTime, _fadeInDuration);
                    _pointsIncreaseLabel.style.opacity = Vector2.Lerp(new Vector2(0, 0), new Vector2(1, 0), _pointsFadeInTimer / _fadeInDuration).x;
                }
                else if (_pointsFadeOutTimer < _fadeOutDuration)
                {
                    _pointsFadeOutTimer = Mathf.Min(_pointsFadeOutTimer + Time.deltaTime, _fadeOutDuration);
                    _pointsIncreaseLabel.style.opacity = Vector2.Lerp(new Vector2(1, 0), new Vector2(0, 0), _pointsFadeOutTimer / _fadeOutDuration).x;
                }
                else
                {
                    _shouldFadePointIncrease = false;
                    _pointsIncreaseLabel.style.display = DisplayStyle.None;
                }
            }
            
            if (_shouldFadeLevelIncrease)
            {
                // The fade out for level occurs first and should be as short as the fade in for a point increase,
                // whereas the fade in for level occurs second and should be as long as the fade out for a point increase.
                if (_levelFadeOutTimer < _fadeInDuration)
                {
                    _levelFadeOutTimer = Mathf.Min(_levelFadeOutTimer + Time.deltaTime, _fadeInDuration);
                    _levelLabel.style.opacity = Vector2.Lerp(new Vector2(1, 0), new Vector2(0, 0), _levelFadeOutTimer / _fadeInDuration).x;
                    if (Mathf.Approximately(_levelFadeOutTimer, _fadeInDuration))
                        _levelLabel.text = FormatNumber(_nextLevel);
                }
                else if (_levelFadeInTimer < _fadeOutDuration)
                {
                    _levelFadeInTimer = Mathf.Min(_levelFadeInTimer + Time.deltaTime, _fadeOutDuration);
                    _levelLabel.style.opacity = Vector2.Lerp(new Vector2(0, 0), new Vector2(1, 0), _levelFadeInTimer / _fadeOutDuration).x;
                }
                else _shouldFadeLevelIncrease = false;
            }
        }
        
        /// <summary>
        /// Updates the points UI with a fade in and out animation.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="increaseAmount"></param>
        /// <param name="levelProgress"></param>
        public void OnPointsIncrease(int points, int increaseAmount, float levelProgress)
        {
            _pointsLabel.text = FormatNumber(points);
            _levelFill.style.height = new Length(levelProgress, LengthUnit.Percent);
            _pointsIncreaseLabel.text = $"+ {FormatNumber(increaseAmount)}";
            _pointsIncreaseLabel.style.display = DisplayStyle.Flex;
            _pointsIncreaseLabel.style.opacity = 0;
            _shouldFadePointIncrease = true;
            _pointsFadeInTimer = 0;
            _pointsFadeOutTimer = 0;
        }
        
        /// <summary>
        /// Updates the level UI with a fade in and out animation.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="levelProgress"></param>
        public void OnLevelUp(int level, float levelProgress)
        {
            _nextLevel = level;
            _levelFill.style.height = new Length(levelProgress, LengthUnit.Percent);
            _shouldFadeLevelIncrease = true;
            _levelFadeInTimer = 0;
            _levelFadeOutTimer = 0;
        }
        
        /// <summary>
        /// Formats the given number into text format.
        /// Uses decimals and K or M if the number gets too large.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private string FormatNumber(int number)
        {
            // If the number is less than 10,000, then just display it as-is.
            if (number < 10000) return $"{number}";
            // If the number is between 10,000 and one million, display it in terms of thousands with one decimal place.
            if (number < 1000000) return $"{Mathf.Round(number / 100) / 10} K";
            // Otherwise, display it in terms of millions. Not likely, but nice if need be.
            return $"{Mathf.Round(number / 100000) / 10} M";
        }
    }
}
