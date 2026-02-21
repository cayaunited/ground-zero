using System.Collections.Generic;
using UnityEngine;

namespace GroundZero
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Gem : MonoBehaviour
    {
        // Add a tooltip to the inspector so that when the mouse hovers over "Piece Gravity Scale",
        // there is a tooltip to explain what it means.
        [Tooltip("The gravity scale for an explosive piece when it's falling.")]
        [SerializeField] [Min(0)] private float _pieceGravityScale;
        [Tooltip("How much the explosive pieces can shake back and forth.")]
        [SerializeField] [Min(0)] private float _pieceShakeDistance;
        [Tooltip("How quickly the explosive pieces can shake back and forth. Closer to 0 is slower, and closer to 1 is faster.")]
        [SerializeField] [Range(0, 1)] private float _pieceShakeSpeed;
        [Tooltip("The index of the center explosive piece, which won't move when shaking.")]
        [SerializeField] [Min(0)] private int _centerPieceIndex;
        [SerializeField] private Rigidbody2D[] _explosivePieces;
        [Tooltip("How long the shine effect of a targeting gem takes to move.")]
        [SerializeField] [Min(0)] private float _shineEffectDuration;
        [Tooltip("How long the shine effect of a targeting gem waits before moving again.")]
        [SerializeField] [Min(0)] private float _shineWaitDuration;
        [SerializeField] private Transform _shineEffect;
        
        // Only the Gem class will ever need to access the SpriteRenderer,
        // so make it private and add an underscore to quickly identify
        // it later in the code as private.
        private SpriteRenderer _renderer;
        private bool _isSpecial;
        private SpecialGemType _specialType;
        private List<Vector2> _pieceStartingPositions;
        private Vector2 _shineStartPosition;
        private float _shineEffectTimer;
        private float _shineWaitTimer;
        
        // We want to be able to read the grid position of this gem from anywhere,
        // but we only want this gem to modify the grid position,
        // so we used a property with a public getter and a private setter.
        public Vector2Int GridPosition { get; private set; }
        public int TypeIndex { get; private set; }
        
        /// <summary>
        /// Spawns this gem with the given sprite at the given position.
        /// Resets any animation or visual components, like explosive pieces.
        /// </summary>
        /// <param name="typeIndex"></param>
        /// <param name="gridPosition"></param>
        /// <param name="worldPosition"></param>
        public void Initialize(int typeIndex, Vector2Int gridPosition, Vector2 worldPosition)
        {
            GridPosition = gridPosition;
            transform.position = worldPosition;
            TypeIndex = typeIndex;
            _isSpecial = false;
            
            // Find the SpriteRenderer if it wasn't already found.
            // This ensures it's only ever called once (since the result never changes).
            if (!_renderer) _renderer = GetComponent<SpriteRenderer>();
            _renderer.enabled = true;
            gameObject.SetActive(true);
            
            // Determine if the positions of the exploding pieces have been recorded yet.
            var startingPositionsWereRecorded = _pieceStartingPositions != null;
            if (!startingPositionsWereRecorded) _pieceStartingPositions = new List<Vector2>(capacity: _explosivePieces.Length);
            
            for (int i = 0; i < _explosivePieces.Length; i++)
            {
                var explosivePiece = _explosivePieces[i];
                // If the exploding pieces were already used before, then reset their positions to their initial positions.
                if (startingPositionsWereRecorded) explosivePiece.transform.position = _pieceStartingPositions[i];
                // Otherwise, record their initial positions relative to the gem's position.
                else _pieceStartingPositions.Add(explosivePiece.transform.localPosition);
                explosivePiece.gravityScale = 0;
                explosivePiece.gameObject.SetActive(false);
            }
            
            _shineEffect.gameObject.SetActive(false);
            if (startingPositionsWereRecorded) _shineEffect.localPosition = _shineStartPosition;
            else _shineStartPosition = _shineEffect.localPosition;
        }
        
        /// <summary>
        /// Updates the visuals for special gems.
        /// </summary>
        public void OnUpdate()
        {
            if (!_isSpecial) return;
            
            if (_specialType == SpecialGemType.Explosive)
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
                    if (i != _centerPieceIndex) {
                        newPosition +=  Random.Range(0, _pieceShakeDistance) * startingPosition.normalized;
                    }
                    
                    // Use linear interpolation so the movement isn't as jagged and appears to be smoother.
                    explosivePiece.MovePosition(Vector2.Lerp(explosivePiece.transform.position, newPosition, _pieceShakeSpeed));
                }
            }
            else if (_specialType == SpecialGemType.Targeting)
            {
                // If not waiting, then move. Otherwise, wait.
                if (Mathf.Approximately(_shineWaitTimer, 0))
                {
                    // Use linear interpolation to determine what the shine effect's position should be,
                    // based on how much time has passed and how much time it takes total to move.
                    _shineEffect.localPosition = Vector2.Lerp(_shineStartPosition, -_shineStartPosition, _shineEffectTimer / _shineEffectDuration);
                    // Update the effect timer and start the wait timer if the effect timer is done.
                    _shineEffectTimer = Mathf.Min(_shineEffectTimer + Time.deltaTime, _shineEffectDuration);
                    if (Mathf.Approximately(_shineEffectTimer, _shineEffectDuration)) _shineWaitTimer = _shineWaitDuration;
                }
                else
                {
                    // Decrease the wait timer, but use the max method to make sure it doesn't go below zero.
                    _shineWaitTimer = Mathf.Max(_shineWaitTimer - Time.deltaTime, 0);
                    if (Mathf.Approximately(_shineWaitTimer, 0)) _shineEffectTimer = 0;
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
            
            if (specialType == SpecialGemType.Explosive)
            {
                _renderer.enabled = false;
                
                foreach (var explosivePiece in _explosivePieces)
                {
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
        /// Moves the gem to the new position.
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="worldPosition"></param>
        public void MoveTo(Vector2Int gridPosition, Vector2 worldPosition)
        {
            GridPosition = gridPosition;
            transform.position = worldPosition;
        }
    }
}
