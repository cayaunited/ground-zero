using UnityEngine;
using UnityEngine.UIElements;

namespace GroundZero
{
    [RequireComponent(typeof(UIDocument))]
    public class GameUI : MonoBehaviour
    {
        private UIDocument _document;
        private Label _modeLabel;
        private Label _numberRemainingLabel;
        private Label _remainingTextLabel;
        private GameMode _mode;
        
        /// <summary>
        /// Initializes the UI to show the correct information based on the given game mode.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="gameDuration"></param>
        public void Initialize(GameMode mode, int gameDuration)
        {
            if (!_document)
            {
                _document = GetComponent<UIDocument>();
                _modeLabel = _document.rootVisualElement.Q<Label>("Mode");
                _numberRemainingLabel = _document.rootVisualElement.Q<Label>("NumberRemaining");
                _remainingTextLabel = _document.rootVisualElement.Q<Label>("RemainingText");
            }
            
            _mode = mode;
            
            if (mode == GameMode.Endless)
            {
                _modeLabel.text = "Endless";
                _numberRemainingLabel.text = "0:00";
                _remainingTextLabel.text = "elapsed";
            }
            else if (mode == GameMode.Timed)
            {
                _modeLabel.text = "Timed";
                UpdateTime(gameDuration);
                _remainingTextLabel.text = "remaining";
            }
            else if (mode == GameMode.LimitedMoves)
            {
                _modeLabel.text = "Limited Moves";
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
    }
}
