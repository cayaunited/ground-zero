using UnityEngine;
using UnityEngine.UIElements;

namespace GroundZero
{
    [RequireComponent(typeof(UIDocument))]
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private int _secondsLeftWhenWarningStarts;
        [SerializeField] private Color _warningColor;
        [Tooltip("How small the menu scale is to start the open animation.")]
        [SerializeField] [Range(0, 1)] private float _menuStartScale;
        [SerializeField] [Min(0)] private float _openMenuDuration;
        [Tooltip("The music volume if the player hasn't set it yet.")]
        [SerializeField] [Range(0, 1)] private float _defaultMusicVolume;
        [Tooltip("The sound effects volume if the player hasn't set it yet.")]
        [SerializeField] [Range(0, 1)] private float _defaultSFXVolume;
        [SerializeField] private AudioManager _audioManager;
        
        private UIDocument _document;
        private Label _numberRemainingLabel;
        private Label _remainingTextLabel;
        private Label _creditsLabel;
        private Label _instructionsLabel;
        private VisualElement _statusContainer;
        private VisualElement _menuContainer;
        private VisualElement _settingsContainer;
        private Button _startEndlessButton;
        private Button _startTimedButton;
        private Button _startLimitedMovesButton;
        private Button _endGameButton;
        private Button _settingsButton;
        private Slider _musicVolumeSlider;
        private Slider _sfxVolumeSlider;
        
        private GameMode _mode;
        private float _openMenuTimer;
        private bool _isSettingsOpen;
        private bool _wereInstructionsShown;
        
        private void Update()
        {
            if (!Mathf.Approximately(_openMenuTimer, 0))
            {
                // Since we are counting down from the top of the timer to zero,
                // the time percentage we pass to the lerp method needs to be subtracted from one.
                _openMenuTimer = Mathf.Max(_openMenuTimer - Time.deltaTime, 0);
                _menuContainer.style.opacity = Vector2.Lerp(new Vector2(0, 0), new Vector2(1, 0), 1 - _openMenuTimer / _openMenuDuration).x;
                var scale = Vector2.Lerp(new Vector2(_menuStartScale, 0), new Vector2(1, 0), 1 - _openMenuTimer / _openMenuDuration).x;
                _menuContainer.style.scale = new Vector2(scale, scale);
            }
        }
        
        /// <summary>
        /// Finds the needed UI elements and initializes the buttons to be clickable.
        /// </summary>
        /// <param name="startEndlessGame"></param>
        /// <param name="startTimedGame"></param>
        /// <param name="startLimitedMovesGame"></param>
        /// <param name="endGame"></param>
        public void Initialize(System.Action startEndlessGame, System.Action startTimedGame,
            System.Action startLimitedMovesGame, System.Action endGame)
        {
            if (_document) return;
            _document = GetComponent<UIDocument>();
            var rootVisualElement = _document.rootVisualElement;
            _numberRemainingLabel = rootVisualElement.Q<Label>("NumberRemaining");
            _remainingTextLabel = rootVisualElement.Q<Label>("RemainingText");
            _creditsLabel = rootVisualElement.Q<Label>("Credits");
            _instructionsLabel = rootVisualElement.Q<Label>("Instructions");
            _statusContainer = rootVisualElement.Q<VisualElement>("StatusContainer");
            _menuContainer = rootVisualElement.Q<VisualElement>("MainMenu");
            _settingsContainer = rootVisualElement.Q<VisualElement>("SettingsMenu");
            _startEndlessButton = rootVisualElement.Q<Button>("EndlessButton");
            _startTimedButton = rootVisualElement.Q<Button>("TimedButton");
            _startLimitedMovesButton = rootVisualElement.Q<Button>("LimitedMovesButton");
            _endGameButton = rootVisualElement.Q<Button>("EndGameButton");
            _settingsButton = rootVisualElement.Q<Button>("SettingsButton");
            _musicVolumeSlider = rootVisualElement.Q<Slider>("MusicVolume");
            _sfxVolumeSlider = rootVisualElement.Q<Slider>("SFXVolume");
            
            // Call the given functions whenever the corresponding buttons are clicked.
            _startEndlessButton.clicked += startEndlessGame;
            _startTimedButton.clicked += startTimedGame;
            _startLimitedMovesButton.clicked += startLimitedMovesGame;
            _endGameButton.clicked += endGame;
            _settingsButton.clicked += ToggleSettingsVisibility;
            
            // Set the correct volume whenever the value of the sliders are changed.
            _musicVolumeSlider.RegisterValueChangedCallback((e) => _audioManager.SetMusicVolume(e.newValue));
            _sfxVolumeSlider.RegisterValueChangedCallback((e) => _audioManager.SetSFXVolume(e.newValue));
            // If the volume settings have been saved, load them. Otherwise, give a default.
            // This will set the actual volume because of the callbacks set above.
            _musicVolumeSlider.value = PlayerPrefs.GetFloat("Music Volume", _defaultMusicVolume);
            _sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFX Volume", _defaultSFXVolume);
        }
        
        /// <summary>
        /// Initializes the gameplay UI to show the correct information based on the given game mode.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="gameDuration"></param>
        public void StartGame(GameMode mode, int gameDuration)
        {
            _mode = mode;
            _menuContainer.style.display = DisplayStyle.None;
            _statusContainer.style.display = DisplayStyle.Flex;
            _endGameButton.style.display = DisplayStyle.Flex;
            _creditsLabel.style.display = DisplayStyle.None;
            _numberRemainingLabel.style.color = Color.white;
            
            // Make sure the instructions are only shown once every time the game is opened.
            if (!_wereInstructionsShown)
            {
                _wereInstructionsShown = true;
                _instructionsLabel.style.display = DisplayStyle.Flex;
            }
            
            if (mode == GameMode.Endless)
            {
                _numberRemainingLabel.text = "0:00";
                _remainingTextLabel.text = "elapsed";
            }
            else if (mode == GameMode.Timed)
            {
                UpdateTime(gameDuration);
                _remainingTextLabel.text = "remaining";
            }
            else if (mode == GameMode.LimitedMoves)
            {
                _numberRemainingLabel.text = "0";
                _remainingTextLabel.text = "possible moves";
            }
        }
        
        /// <summary>
        /// Updates the remaining / elapsed time based on the given number of seconds,
        /// padding the time to ensure the format 0:00, or 0:00:00 if hours are involved.
        /// </summary>
        /// <param name="totalSeconds"></param>
        public void UpdateTime(int totalSeconds)
        {
            if (_mode == GameMode.LimitedMoves) return;
            int hours = Mathf.FloorToInt(totalSeconds / 3600f);
            int minutes = Mathf.FloorToInt(totalSeconds / 60f) % 60;
            int seconds = totalSeconds % 60;
            
            // Pad the seconds with an extra zero at the start if needed, so that there are always two digits visible.
            _numberRemainingLabel.text = $"{minutes}:{(seconds < 10 ? "0" : "")}{seconds}";
            // Account for people playing endless for over an hour, just in case.
            if (hours > 0) _numberRemainingLabel.text = $"{hours}:{(minutes < 10 ? "0" : "")}{_numberRemainingLabel.text}";
            
            // Toggle the color shown when the time is running out, and play corresponding audio.
            if (_mode == GameMode.Timed && totalSeconds <= _secondsLeftWhenWarningStarts)
            {
                var isTimeEven = totalSeconds % 2 == 0;
                _numberRemainingLabel.style.color = isTimeEven ? _warningColor : Color.white;
                
                if (totalSeconds == _secondsLeftWhenWarningStarts) _audioManager.OnStartWarning();
                else _audioManager.PlayTickSFX();
            }
        }
        
        /// <summary>
        /// Updates the number of possible moves, formatting it to handle singular (move) vs plural (moves) correctly.
        /// </summary>
        /// <param name="possibleMoveCount"></param>
        public void UpdateMoves(int possibleMoveCount)
        {
            if (_mode != GameMode.LimitedMoves) return;
            _numberRemainingLabel.text = $"{possibleMoveCount}";
            _remainingTextLabel.text = $"possible move{(possibleMoveCount == 1 ? "" : "s")}";
        }
        
        public void OnGameEnded()
        {
            _menuContainer.style.display = DisplayStyle.Flex;
            _endGameButton.style.display = DisplayStyle.None;
            _creditsLabel.style.display = DisplayStyle.Flex;
            _instructionsLabel.style.display = DisplayStyle.None;
            _openMenuTimer = _openMenuDuration;
        }
        
        public void HideInstructions() => _instructionsLabel.style.display = DisplayStyle.None;
        
        private void ToggleSettingsVisibility()
        {
            _isSettingsOpen = !_isSettingsOpen;
            _settingsContainer.style.display = _isSettingsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            _audioManager.PlaySelectSFX();
        }
    }
}
