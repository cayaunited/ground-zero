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
        [SerializeField] private AudioManager _audioManager;
        
        private UIDocument _document;
        private Label _numberRemainingLabel;
        private Label _remainingTextLabel;
        private VisualElement _statusContainer;
        private VisualElement _menuContainer;
        private Button _startEndlessButton;
        private Button _startTimedButton;
        private Button _startLimitedMovesButton;
        private Button _endGameButton;
        
        private GameMode _mode;
        private float _openMenuTimer;
        
        private void Update()
        {
            if (!Mathf.Approximately(_openMenuTimer, 0))
            {
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
            _numberRemainingLabel = _document.rootVisualElement.Q<Label>("NumberRemaining");
            _remainingTextLabel = _document.rootVisualElement.Q<Label>("RemainingText");
            _statusContainer = _document.rootVisualElement.Q<VisualElement>("StatusContainer");
            _menuContainer = _document.rootVisualElement.Q<VisualElement>("MainMenu");
            _startEndlessButton = _document.rootVisualElement.Q<Button>("EndlessButton");
            _startTimedButton = _document.rootVisualElement.Q<Button>("TimedButton");
            _startLimitedMovesButton = _document.rootVisualElement.Q<Button>("LimitedMovesButton");
            _endGameButton = _document.rootVisualElement.Q<Button>("EndGameButton");
            
            // Call the given functions whenever the corresponding buttons are clicked.
            _startEndlessButton.clicked += startEndlessGame;
            _startTimedButton.clicked += startTimedGame;
            _startLimitedMovesButton.clicked += startLimitedMovesGame;
            _endGameButton.clicked += endGame;
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
            _numberRemainingLabel.style.color = Color.white;
            
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
            _openMenuTimer = _openMenuDuration;
        }
    }
}
