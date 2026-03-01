using UnityEngine;

namespace GroundZero
{
    public class GameManager : MonoBehaviour
    {
        [Tooltip("How long in seconds the game lasts while in timed mode.")]
        [SerializeField] private int _gameDuration;
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private GemManager _gemManager;
        [SerializeField] private PointsManager _pointsManager;
        [SerializeField] private GameUI _gameUI;
        
        private bool _wasGameStarted;
        private GameMode _mode;
        private float _gameTimer;
        
        private void Awake()
        {
            // Give the UI the functions it needs for the buttons to start a game with a certain mode.
            _gameUI.Initialize(() => StartGame(GameMode.Endless), () => StartGame(GameMode.Timed),
                () => StartGame(GameMode.LimitedMoves), EndGame);
        }
        
        private void Update()
        {
            _pointsManager.OnUpdate();
            if (!_wasGameStarted) return;
            
            if (_mode == GameMode.Endless)
            {
                // Increase the timer and round it properly,
                // so it only shows one second elapsed right as the whole second passes.
                var timeBeforeChange = Mathf.FloorToInt(_gameTimer);
                _gameTimer += Time.deltaTime;
                var timeAfterChange = Mathf.FloorToInt(_gameTimer);
                if (timeBeforeChange != timeAfterChange) _gameUI.UpdateTime(timeAfterChange);
            }
            else if (_mode == GameMode.Timed && _gameTimer > 0)
            {
                // Decrease the timer and round it properly,
                // so it only shows zero seconds remaining right as the game ends.
                var timeBeforeChange = Mathf.CeilToInt(_gameTimer);
                _gameTimer = Mathf.Max(_gameTimer - Time.deltaTime, 0);
                var timeAfterChange = Mathf.CeilToInt(_gameTimer);
                if (timeBeforeChange != timeAfterChange) _gameUI.UpdateTime(timeAfterChange);
            }
        }
        
        private void FixedUpdate()
        {
            _gemManager.OnFixedUpdate();
            if (!_wasGameStarted) return;
            // Make sure the game ends when the timer runs out if there's no action going on.
            if (_mode == GameMode.Timed && Mathf.Approximately(_gameTimer, 0)
                && _gemManager.GridState == GridState.WaitingForInput) EndGame();
        }
        
        private void StartGame(GameMode mode)
        {
            _wasGameStarted = true;
            _mode = mode;
            _gameTimer = mode == GameMode.Timed ? _gameDuration : 0;
            _gameUI.StartGame(mode, _gameDuration);
            _inputManager.OnGameStarted();
            _gemManager.Initialize(OnDoneMatching, OnNoMovesLeft);
            _pointsManager.Initialize();
        }
        
        private void EndGame()
        {
            _wasGameStarted = false;
            _inputManager.OnGameEnded();
            _gemManager.OnGameEnded();
            _gameUI.OnGameEnded();
        }
        
        /// <summary>
        /// If the gems are done falling and matching, and the game mode is timed,
        /// then ends the game if the timer is done, and returns true. Otherwise, returns false.
        /// </summary>
        /// <returns>Returns true if the game ended.</returns>
        private bool OnDoneMatching()
        {
            var shouldEnd = _mode == GameMode.Timed && Mathf.Approximately(_gameTimer, 0);
            if (shouldEnd) EndGame();
            return shouldEnd;
        }
        
        /// <summary>
        /// If there's no moves left, and the game mode is limited moves,
        /// then ends the game and returns true. Otherwise, returns false.
        /// </summary>
        /// <returns>Returns true if the game ended.</returns>
        private bool OnNoMovesLeft()
        {
            var shouldEnd = _mode == GameMode.LimitedMoves;
            if (shouldEnd) EndGame();
            return shouldEnd;
        }
    }
}
