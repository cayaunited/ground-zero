using System.Collections.Generic;
using UnityEngine;

namespace GroundZero
{
    // Make sure all Gem GameObjects have a SpriteRenderer and Rigidbody2D.
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class Gem : MonoBehaviour
    {
        // Since we have a lot of fields exposed in the Unity inspector,
        // it's a good idea to group the related ones together for more clarity and ease of use.
        [Header("Regular Mechanics")]
        [SerializeField] [Min(0)] private float _swapDuration;
        [SerializeField] [Min(0)] private float _shrinkDuration;
        [SerializeField] [Min(0)] private float _gravityScale;
        
        [Header("Explosions and Explosive Gems")]
        [SerializeField] [Min(0)] private float _explosionStrength;
        [Tooltip("How far from the starting position a piece of the gem can move to when shaking.")]
        [SerializeField] [Min(0)] private float _pieceShakeDistance;
        [Tooltip("Determines how quickly the gem pieces move when shaking, based on linear interpolation. "
            + "0 means they don't move, and 1 means they are instantly in a new position. "
            + "Somewhere inbetween will smoothly interpolate their positions.")]
        [SerializeField] [Range(0, 1)] private float _pieceShakeSpeed;
        [SerializeField] [Min(0)] private int _centerPieceIndex;
        [SerializeField] private Rigidbody2D[] _explosivePieces;
        
        [Header("Targeting Gems")]
        [SerializeField] [Min(0)] private float _shineEffectDuration;
        [SerializeField] [Min(0)] private float _shineWaitDuration;
        [SerializeField] [Min(0)] private float _shineScaleAmount;
        [SerializeField] [Min(0)] private float _shineScaleSpeed;
        [SerializeField] private Transform _shineEffect;
        
        private SpriteRenderer _renderer;
        private Rigidbody2D _rigidbody;
        
        private bool _isSpecial;
        private SpecialGemType _specialType;
        
        // For explosive gems.
        private List<Vector2> _pieceStartingPositions;
        private List<float> _pieceDistances;
        
        // For targeting gems.
        private Vector2 _shineStartPosition;
        private float _shineEffectTimer;
        private float _shineWaitTimer;
        private float _animationTime;
        
        // For swapping and dropping.
        private Vector2 _positionBeforeSwap;
        private Vector2 _moveTargetPosition;
        private bool _isSwapping;
        private float _swapTimer;
        private bool _isDropping;
        private float _yVelocity;
        
        // For matches and explosions.
        private bool _isShrinking;
        private float _shrinkTimer;
        private bool _isExploding;
        private float _screenBottom;
        private System.Action<Gem> _recycleGem;
        
        public int TypeIndex { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public bool IsAnimating { get; private set; }
        
        /// <summary>
        /// Initializes the gem by resetting needed variables and animations,
        /// and recording effect positions to use when resetting the gem.
        /// </summary>
        /// <param name="typeIndex">The color and shape of the gem, represented by an index in the GemManager's list of types.</param>
        /// <param name="gridPosition"></param>
        /// <param name="worldPosition"></param>
        /// <param name="screenBottom">The y position of the bottom of the screen. Used to determine when all exploded pieces are no longer visible.</param>
        /// <param name="recycleGem">The callback function used to recycle this gem when its animations finish.</param>
        public void Initialize(int typeIndex, Vector2Int gridPosition, Vector2 worldPosition, float screenBottom, System.Action<Gem> recycleGem)
        {
            // Set / reset all needed fields to either the given values or initial values depending on the field.
            TypeIndex = typeIndex;
            GridPosition = gridPosition;
            _isSpecial = false;
            
            transform.position = worldPosition;
            transform.localScale = Vector3.one;
            
            _screenBottom = screenBottom;
            _recycleGem = recycleGem;
            IsAnimating = false;
            _animationTime = 0;
            
            // If we haven't already, get the SpriteRenderer and Rigidbody2D attached to this GameObject.
            // We know they both exist on this GameObject because of the RequireComponent attribute from above.
            if (!_renderer) _renderer = GetComponent<SpriteRenderer>();
            if (!_rigidbody) _rigidbody = GetComponent<Rigidbody2D>();
            _renderer.enabled = true;
            gameObject.SetActive(true);
            
            // Next, we need to figure out where the positions of our effects start out,
            // that way we can reset them properly later.
            var startingPositionsWereRecorded = _pieceStartingPositions != null;
            
            // If we haven't recorded the starting positions yet, we'll need to create the lists that store the starting positions.
            // We know how many pieces there are based on the list assigned in the Unity inspector,
            // so we can set the capacity of the list, meaning the computer doesn't have to worry about growing the max size of the list as we add to it.
            if (!startingPositionsWereRecorded)
            {
                _pieceStartingPositions = new List<Vector2>(capacity: _explosivePieces.Length);
                _pieceDistances = new List<float>(capacity: _explosivePieces.Length);
            }
            
            for (int i = 0; i < _explosivePieces.Length; i++)
            {
                var explosivePiece = _explosivePieces[i];
                
                // Not only do we need to record the starting position of the explosive pieces (relative to the gem's position),
                // but we also need to reset the explosive pieces to their starting positions if the gem was exploded before.
                if (startingPositionsWereRecorded) explosivePiece.transform.position = (Vector2)transform.position + _pieceStartingPositions[i];
                else
                {
                    _pieceStartingPositions.Add(explosivePiece.transform.localPosition);
                    _pieceDistances.Add(0);
                }
                
                explosivePiece.transform.localEulerAngles = Vector3.zero;
                explosivePiece.gravityScale = 0;
                explosivePiece.gameObject.SetActive(false);
            }
            
            // Make sure to record the starting position of the shine effect as well,
            // and reset it to not be visible.
            if (startingPositionsWereRecorded) _shineEffect.localPosition = _shineStartPosition;
            else _shineStartPosition = _shineEffect.localPosition;
            _shineEffect.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Animates the gem on every FixedUpdate frame,
        /// whether for idle visual effects, swapping, dropping, shrinking, or exploding.
        /// </summary>
        public void OnFixedUpdate()
        {
            // Use else if chains, because only one animation will occur at a time,
            // and this allows us to stop checking once we update the correct animation.
            if (_isSwapping) UpdateSwapAnimation();
            else if (_isDropping) UpdateDropAnimation();
            else if (_isShrinking) UpdateShrinkAnimation();
            else if (_isExploding) UpdateExplodeAnimation();
            
            // As long as the gem is special and not in the process of being destroyed, update its visual effects.
            if (_isSpecial && !_isShrinking && !_isExploding)
            {
                if (_specialType == SpecialGemType.Explosive) UpdateExplosiveVisuals();
                else if (_specialType == SpecialGemType.Targeting) UpdateTargetingVisuals();
            }
        }
        
        /// <summary>
        /// Marks this gem as special and enables its corresponding visual effects.
        /// </summary>
        /// <param name="specialType"></param>
        public void MakeSpecial(SpecialGemType specialType)
        {
            _isSpecial = true;
            _specialType = specialType;
            
            if (specialType == SpecialGemType.Explosive)
            {
                // Hide the regular gem sprite and instead reset / show the explosive pieces.
                _renderer.enabled = false;
                
                for (int i = 0; i < _explosivePieces.Length; i++)
                {
                    _pieceDistances[i] = 0;
                    var explosivePiece = _explosivePieces[i];
                    explosivePiece.position = (Vector2)transform.position + _pieceStartingPositions[i];
                    explosivePiece.transform.localEulerAngles = Vector3.zero;
                    explosivePiece.gameObject.SetActive(true);
                }
            }
            else if (_specialType == SpecialGemType.Targeting)
            {
                // Reset the timers and the shine effect, then show it.
                _shineEffectTimer = 0;
                _shineWaitTimer = 0;
                _shineEffect.localPosition = _shineStartPosition;
                _shineEffect.gameObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// Updates the gem's grid position and starts the swap animation.
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="worldPosition"></param>
        public void SwapTo(Vector2Int gridPosition, Vector2 worldPosition)
        {
            GridPosition = gridPosition;
            // Store the position before the swap started,
            // since it's used in linear interpolation between the start and the target position.
            _positionBeforeSwap = transform.position;
            _moveTargetPosition = worldPosition;
            IsAnimating = true;
            _isSwapping = true;
            _swapTimer = 0;
        }
        
        /// <summary>
        /// Updates the gem's grid position and starts the drop animation.
        /// Requires both a final and initial position so that it knows where to drop from,
        /// since some gems will drop from a point above the screen to replace destroyed gems,
        /// whereas others will just drop to a lower position in the grid.
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
            _yVelocity = 0;
        }
        
        /// <summary>
        /// If the gem should explode, enables explosive pieces and sends them flying.
        /// Otherwise, starts the shrink animation.
        /// </summary>
        /// <param name="shouldExplode"></param>
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
                    explosivePiece.gameObject.SetActive(true);
                    explosivePiece.MovePositionAndRotation((Vector2)transform.position + startingPosition, 0);
                    explosivePiece.gravityScale = _gravityScale;
                    // Add a force in the direction from the center to the explosive piece,
                    // plus a random direction to make sure the center piece also goes flying instead of just falling straight down.
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
        /// Uses linear interpolation to move the gem from its starting position
        /// to the target position smoothly over time.
        /// </summary>
        private void UpdateSwapAnimation()
        {
            _swapTimer = Mathf.Min(_swapTimer + Time.fixedDeltaTime, _swapDuration);
            _rigidbody.MovePosition(Vector2.Lerp(_positionBeforeSwap, _moveTargetPosition, _swapTimer / _swapDuration));
            
            if (Mathf.Approximately(_swapTimer, _swapDuration))
            {
                IsAnimating = false;
                _isSwapping = false;
            }
        }
        
        /// <summary>
        /// Uses custom gravity physics to drop smoothly
        /// without ever passing the target position and having to teleport back to it,
        /// which would create a jarring effect.
        /// </summary>
        private void UpdateDropAnimation()
        {
            // Physics2D.gravity.y is usually -9.81, unless set otherwise.
            // Use the kinematic equations to figure out how the velocity and position need to change over time.
            _yVelocity += _gravityScale * Physics2D.gravity.y * Time.fixedDeltaTime;
            // Decrease the y position based on the y velocity,
            // making sure to clamp it and prevent it from going below the target.
            var newYPosition = Mathf.Max(_rigidbody.position.y + _yVelocity * Time.fixedDeltaTime, _moveTargetPosition.y);
            _rigidbody.MovePosition(new Vector2(_rigidbody.position.x, newYPosition));
            
            if (Mathf.Approximately(_rigidbody.position.y, _moveTargetPosition.y))
            {
                IsAnimating = false;
                _isDropping = false;
                _yVelocity = 0;
            }
        }
        
        /// <summary>
        /// Uses linear interpolation to shrink the gem from a scale of 1 to a scale of 0 smoothly over time.
        /// </summary>
        private void UpdateShrinkAnimation()
        {
            _shrinkTimer = Mathf.Min(_shrinkTimer + Time.fixedDeltaTime, _shrinkDuration);
            // There is no lerp method for floats, so cheat and use the x component of a vector.
            var scale = Vector2.Lerp(new Vector2(1, 0), new Vector2(0, 0), _shrinkTimer / _shrinkDuration).x;
            // Detailed more in the SelectionCursor animation code,
            // but we don't use z-axis scale in 2D, so it can stay 1.
            transform.localScale = new Vector3(scale, scale, 1);
            
            if (Mathf.Approximately(_shrinkTimer, _shrinkDuration))
            {
                IsAnimating = false;
                _isShrinking = false;
                _recycleGem?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Ends the explosion animation if all pieces are no longer visible,
        /// because of being below the bottom of the screen.
        /// </summary>
        private void UpdateExplodeAnimation()
        {
            var isPieceAboveBottom = false;
            
            foreach (var explosivePiece in _explosivePieces)
            {
                if (explosivePiece.transform.position.y > _screenBottom)
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
        
        /// <summary>
        /// Shakes the outer explosive pieces back and forth in a line from
        /// the center piece to the outer piece.
        /// </summary>
        private void UpdateExplosiveVisuals()
        {
            for (int i = 0; i < _explosivePieces.Length; i++)
            {
                var explosivePiece = _explosivePieces[i];
                var startingPosition = _pieceStartingPositions[i];
                var newPosition = (Vector2)transform.position + startingPosition;
                
                if (i != _centerPieceIndex)
                {
                    // Linearly interpolate between the current position and the new one
                    // so that it's not as jarring moving around from random point to random point.
                    _pieceDistances[i] = Vector2.Lerp(new Vector2(_pieceDistances[i], 0),
                        new Vector2(Random.Range(0, _pieceShakeDistance), 0), _pieceShakeSpeed).x;
                    newPosition += _pieceDistances[i] * startingPosition.normalized;
                }
                
                explosivePiece.MovePosition(newPosition);
            }
        }
        
        /// <summary>
        /// Periodically moves a shine effect across the gem,
        /// and makes it pulse in and out with scale over time.
        /// </summary>
        private void UpdateTargetingVisuals()
        {
            // While the shine effect isn't waiting to move,
            // linearly interpolate it from one corner of the gem to the other.
            if (Mathf.Approximately(_shineWaitTimer, 0))
            {
                _shineEffect.localPosition = Vector2.Lerp(_shineStartPosition, -_shineStartPosition, _shineEffectTimer / _shineEffectDuration);
                _shineEffectTimer = Mathf.Min(_shineEffectTimer + Time.fixedDeltaTime, _shineEffectDuration);
                // Start waiting when done shining.
                if (Mathf.Approximately(_shineEffectTimer, _shineEffectDuration)) _shineWaitTimer = _shineWaitDuration;
            }
            else
            {
                _shineWaitTimer = Mathf.Max(_shineWaitTimer - Time.fixedDeltaTime, 0);
                // Reset the shine effect position when done waiting.
                if (Mathf.Approximately(_shineWaitTimer, 0)) _shineEffectTimer = 0;
            }
            
            _animationTime += Time.fixedDeltaTime;
            var scale = 1 + _shineScaleAmount * Mathf.Sin(_shineScaleSpeed * _animationTime);
            transform.localScale = new Vector3(scale, scale, 1);
        }
    }
}
