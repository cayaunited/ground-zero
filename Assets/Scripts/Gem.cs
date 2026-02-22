using System.Collections.Generic;
using UnityEngine;

namespace GroundZero
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class Gem : MonoBehaviour
    {
        [SerializeField] [Min(0)] private float _swapDuration;
        [SerializeField] [Min(0)] private float _shrinkDuration;
        // Add a tooltip to the inspector so that when the mouse hovers over "Gravity Scale",
        // there is a tooltip to explain what it means.
        [Tooltip("The gravity scale for the gem and its explosive pieces when they are falling.")]
        [SerializeField] [Min(0)] private float _gravityScale;
        [Tooltip("How much the explosive pieces can shake back and forth.")]
        [SerializeField] [Min(0)] private float _pieceShakeDistance;
        [Tooltip("How quickly the explosive pieces can shake back and forth. Closer to 0 is slower, and closer to 1 is faster.")]
        [SerializeField] [Range(0, 1)] private float _pieceShakeSpeed;
        [SerializeField] [Min(0)] private float _explosionStrength;
        [Tooltip("The index of the center explosive piece, which won't move when shaking.")]
        [SerializeField] [Min(0)] private int _centerPieceIndex;
        [SerializeField] private Rigidbody2D[] _explosivePieces;
        [Tooltip("How long the shine effect of a targeting gem takes to move.")]
        [SerializeField] [Min(0)] private float _shineEffectDuration;
        [Tooltip("How long the shine effect of a targeting gem waits before moving again.")]
        [SerializeField] [Min(0)] private float _shineWaitDuration;
        [SerializeField] [Min(0)] private float _shineScaleAmount;
        [SerializeField] [Min(0)] private float _shineScaleSpeed;
        [SerializeField] private Transform _shineEffect;
        
        // Only the Gem class will ever need to access the SpriteRenderer,
        // so make it private and add an underscore to quickly identify
        // it later in the code as private.
        private SpriteRenderer _renderer;
        private Rigidbody2D _rigidbody;
        private bool _isSpecial;
        private SpecialGemType _specialType;
        private List<Vector2> _pieceStartingPositions;
        private List<float> _pieceDistances;
        private Vector2 _shineStartPosition;
        private float _shineEffectTimer;
        private float _shineWaitTimer;
        private Vector2 _positionBeforeSwap;
        private Vector2 _moveTargetPosition;
        private bool _isSwapping;
        private float _swapTimer;
        private bool _isDropping;
        private bool _isShrinking;
        private float _shrinkTimer;
        private bool _isExploding;
        private float _screenBottom;
        private System.Action<Gem> _recycleGem;
        private float _animationTime;
        
        // We want to be able to read the grid position of this gem from anywhere,
        // but we only want this gem to modify the grid position,
        // so we used a property with a public getter and a private setter.
        public Vector2Int GridPosition { get; private set; }
        public int TypeIndex { get; private set; }
        public bool IsAnimating { get; private set; }
        
        /// <summary>
        /// Spawns this gem at the given position.
        /// Resets any animation or visual components, like explosive pieces.
        /// </summary>
        /// <param name="typeIndex"></param>
        /// <param name="gridPosition"></param>
        /// <param name="worldPosition"></param>
        /// <param name="screenBottom">The y position of the bottom of the screen. Used for explosion animation.</param>
        /// <param name="recycleGem">The method needed to recycle the gem once it's done animating.</param>
        public void Initialize(int typeIndex, Vector2Int gridPosition, Vector2 worldPosition, float screenBottom, System.Action<Gem> recycleGem)
        {
            GridPosition = gridPosition;
            transform.position = worldPosition;
            transform.localScale = Vector3.one;
            TypeIndex = typeIndex;
            _isSpecial = false;
            _screenBottom = screenBottom;
            _recycleGem = recycleGem;
            _animationTime = 0;
            
            // Find the SpriteRenderer if it wasn't already found.
            // This ensures it's only ever called once (since the result never changes).
            if (!_renderer) _renderer = GetComponent<SpriteRenderer>();
            _renderer.enabled = true;
            // Make sure to find the Rigidbody if need be, and reset it.
            if (!_rigidbody) _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0;
            gameObject.SetActive(true);
            
            // Determine if the positions of the exploding pieces have been recorded yet.
            var startingPositionsWereRecorded = _pieceStartingPositions != null;
            
            if (!startingPositionsWereRecorded)
            {
                _pieceStartingPositions = new List<Vector2>(capacity: _explosivePieces.Length);
                _pieceDistances = new List<float>(capacity: _explosivePieces.Length);
            }
            
            for (int i = 0; i < _explosivePieces.Length; i++)
            {
                var explosivePiece = _explosivePieces[i];
                
                // If the exploding pieces were already used before, then reset their positions to their initial positions.
                if (startingPositionsWereRecorded) explosivePiece.transform.position = (Vector2)transform.position + _pieceStartingPositions[i];
                // Otherwise, record their initial positions relative to the gem's position.
                else
                {
                    _pieceStartingPositions.Add(explosivePiece.transform.localPosition);
                    _pieceDistances.Add(0);
                }
                
                explosivePiece.transform.localEulerAngles = Vector3.zero;
                explosivePiece.gravityScale = 0;
                explosivePiece.gameObject.SetActive(false);
            }
            
            _shineEffect.gameObject.SetActive(false);
            if (startingPositionsWereRecorded) _shineEffect.localPosition = _shineStartPosition;
            else _shineStartPosition = _shineEffect.localPosition;
        }
        
        /// <summary>
        /// Moves the gems if they are supposed to be moving.
        /// Updates the visuals for special gems.
        /// </summary>
        public void OnFixedUpdate()
        {
            _animationTime += Time.fixedDeltaTime;
            
            // While swapping, use linear interpolation to move the gem.
            if (_isSwapping)
            {
                _swapTimer = Mathf.Min(_swapTimer + Time.fixedDeltaTime, _swapDuration);
                _rigidbody.MovePosition(Vector2.Lerp(_positionBeforeSwap, _moveTargetPosition, _swapTimer / _swapDuration));
                
                if (Mathf.Approximately(_swapTimer, _swapDuration))
                {
                    IsAnimating = false;
                    _isSwapping = false;
                }
            }
            
            // While dropping, make sure the gem doesn't drop below the correct position,
            // and stop dropping once it reaches that position.
            if (_isDropping && _rigidbody.position.y <= _moveTargetPosition.y)
            {
                IsAnimating = false;
                _isDropping = false;
                _rigidbody.gravityScale = 0;
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.MovePosition(_moveTargetPosition);
            }
            
            if (_isSpecial && !_isShrinking && !_isExploding)
            {
                if (_specialType == SpecialGemType.Explosive) UpdateExplosiveVisuals();
                else if (_specialType == SpecialGemType.Targeting) UpdateTargetingVisuals();
            }
            
            // While shrinking, use linear interpolation to shrink the gem.
            if (_isShrinking)
            {
                _shrinkTimer = Mathf.Min(_shrinkTimer + Time.fixedDeltaTime, _shrinkDuration);
                // There is no lerp method for floats, so cheat and use the x component of a vector.
                var scale = Vector2.Lerp(new Vector2(1, 0), new Vector2(0, 0), _shrinkTimer / _shrinkDuration).x;
                transform.localScale = new Vector3(scale, scale, scale);
                
                if (Mathf.Approximately(_shrinkTimer, _shrinkDuration))
                {
                    IsAnimating = false;
                    _isShrinking = false;
                    _recycleGem?.Invoke(this);
                }
            }
            
            // While exploding, determine when all the pieces are done falling off the screen.
            if (_isExploding)
            {
                var isPieceAboveBottom = false;
                
                foreach (var explosivePiece in _explosivePieces)
                {
                    if (explosivePiece.position.y > _screenBottom)
                    {
                        isPieceAboveBottom = true;
                        break;
                    }
                }
                
                if (!isPieceAboveBottom)
                {
                    IsAnimating = false;
                    _isExploding = false;
                    _recycleGem?.Invoke(this);
                }
            }
        }
        
        /// <summary>
        /// Marks this gem as the given special type of gem.
        /// </summary>
        /// <param name="specialType"></param>
        public void MakeSpecial(SpecialGemType specialType)
        {
            _isSpecial = true;
            _specialType = specialType;
            
            // Start or restart any animations related to the special type.
            if (specialType == SpecialGemType.Explosive)
            {
                _renderer.enabled = false;
                
                for (int i = 0; i < _explosivePieces.Length; i++)
                {
                    var explosivePiece = _explosivePieces[i];
                    var startingPosition = _pieceStartingPositions[i];
                    explosivePiece.position = (Vector2)transform.position + startingPosition;
                    explosivePiece.transform.localEulerAngles = Vector3.zero;
                    _pieceDistances[i] = 0;
                    explosivePiece.gameObject.SetActive(true);
                }
            }
            else if (_specialType == SpecialGemType.Targeting)
            {
                _shineEffect.localPosition = _shineStartPosition;
                _shineEffectTimer = 0;
                _shineWaitTimer = 0;
                _shineEffect.gameObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// Tells the gem to move to the new position after a swap.
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="worldPosition"></param>
        public void SwapTo(Vector2Int gridPosition, Vector2 worldPosition)
        {
            GridPosition = gridPosition;
            _positionBeforeSwap = transform.position;
            _moveTargetPosition = worldPosition;
            IsAnimating = true;
            _isSwapping = true;
            _swapTimer = 0;
        }
        
        /// <summary>
        /// Tells the gem to move to the new position after a drop.
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="initialWorldPosition"></param>
        /// <param name="finalWorldPosition"></param>
        public void DropTo(Vector2Int gridPosition, Vector2 initialWorldPosition, Vector2 finalWorldPosition)
        {
            GridPosition = gridPosition;
            transform.position = initialWorldPosition;
            _moveTargetPosition = finalWorldPosition;
            IsAnimating = true;
            _isDropping = true;
            _rigidbody.gravityScale = _gravityScale;
        }
        
        /// <summary>
        /// Starts the destroy animation for the gem.
        /// </summary>
        /// <param name="shouldExplode">True if being blown up by an explosive gem.</param>
        public void Destroy(bool shouldExplode)
        {
            IsAnimating = true;
            
            if (shouldExplode || _isSpecial && _specialType == SpecialGemType.Explosive)
            {
                _isExploding = true;
                _renderer.enabled = false;
                
                for (int i = 0; i < _explosivePieces.Length; i++)
                {
                    var explosivePiece = _explosivePieces[i];
                    var startingPosition = _pieceStartingPositions[i];
                    explosivePiece.position = (Vector2)transform.position + startingPosition;
                    explosivePiece.transform.localEulerAngles = Vector3.zero;
                    explosivePiece.gameObject.SetActive(true);
                    explosivePiece.gravityScale = _gravityScale;
                    explosivePiece.AddForce(_explosionStrength * (startingPosition + Random.insideUnitCircle).normalized, ForceMode2D.Impulse);
                }
            }
            else
            {
                _isShrinking = true;
                _shrinkTimer = 0;
            }
        }
        
        /// <summary>
        /// Updates the visuals for an explosive gem.
        /// </summary>
        private void UpdateExplosiveVisuals()
        {
            for (int i = 0; i < _explosivePieces.Length; i++)
            {
                var explosivePiece = _explosivePieces[i];
                var startingPosition = _pieceStartingPositions[i];
                // Make each piece have a position anchored in the initial position,
                // plus a random amount within a circle with a radius of the shake size.
                var newPosition = (Vector2)transform.position + startingPosition;
                
                // Make sure the center piece doesn't shake,
                // and make sure it only shakes in and out of the same line towards the center of the circle / gem.
                if (i != _centerPieceIndex)
                {
                    // There is no lerp method for floats, so cheat and use the x component of a vector.
                    _pieceDistances[i] = Vector2.Lerp(new Vector2(_pieceDistances[i], 0),
                        new Vector2(Random.Range(0, _pieceShakeDistance), 0), _pieceShakeSpeed).x;
                    newPosition += _pieceDistances[i] * startingPosition.normalized;
                }
                
                // Use linear interpolation so the movement isn't as jagged and appears to be smoother.
                explosivePiece.MovePosition(newPosition);
            }
        }
        
        /// <summary>
        /// Updates the visuals for a targeting gem.
        /// </summary>
        private void UpdateTargetingVisuals()
        {
            // If not waiting, then move. Otherwise, wait.
            if (Mathf.Approximately(_shineWaitTimer, 0))
            {
                // Use linear interpolation to determine what the shine effect's position should be,
                // based on how much time has passed and how much time it takes total to move.
                _shineEffect.localPosition = Vector2.Lerp(_shineStartPosition, -_shineStartPosition, _shineEffectTimer / _shineEffectDuration);
                // Update the effect timer and start the wait timer if the effect timer is done.
                _shineEffectTimer = Mathf.Min(_shineEffectTimer + Time.fixedDeltaTime, _shineEffectDuration);
                if (Mathf.Approximately(_shineEffectTimer, _shineEffectDuration)) _shineWaitTimer = _shineWaitDuration;
            }
            else
            {
                // Decrease the wait timer, but use the max method to make sure it doesn't go below zero.
                _shineWaitTimer = Mathf.Max(_shineWaitTimer - Time.fixedDeltaTime, 0);
                if (Mathf.Approximately(_shineWaitTimer, 0)) _shineEffectTimer = 0;
            }
            
            var scale = 1 + _shineScaleAmount * Mathf.Sin(_shineScaleSpeed * _animationTime);
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
