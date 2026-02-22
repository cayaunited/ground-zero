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
        [SerializeField] [Min(0)] private float _gravityScale;
        [SerializeField] [Min(0)] private float _pieceShakeDistance;
        [SerializeField] [Range(0, 1)] private float _pieceShakeSpeed;
        [SerializeField] [Min(0)] private float _explosionStrength;
        [SerializeField] [Min(0)] private int _centerPieceIndex;
        [SerializeField] private Rigidbody2D[] _explosivePieces;
        [SerializeField] [Min(0)] private float _shineEffectDuration;
        [SerializeField] [Min(0)] private float _shineWaitDuration;
        [SerializeField] [Min(0)] private float _shineScaleAmount;
        [SerializeField] [Min(0)] private float _shineScaleSpeed;
        [SerializeField] private Transform _shineEffect;
        
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
        
        public Vector2Int GridPosition { get; private set; }
        public int TypeIndex { get; private set; }
        public bool IsAnimating { get; private set; }
        
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
            
            if (!_renderer) _renderer = GetComponent<SpriteRenderer>();
            _renderer.enabled = true;
            if (!_rigidbody) _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0;
            gameObject.SetActive(true);
            
            var startingPositionsWereRecorded = _pieceStartingPositions != null;
            
            if (!startingPositionsWereRecorded)
            {
                _pieceStartingPositions = new List<Vector2>(capacity: _explosivePieces.Length);
                _pieceDistances = new List<float>(capacity: _explosivePieces.Length);
            }
            
            for (int i = 0; i < _explosivePieces.Length; i++)
            {
                var explosivePiece = _explosivePieces[i];
                
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
            
            _shineEffect.gameObject.SetActive(false);
            if (startingPositionsWereRecorded) _shineEffect.localPosition = _shineStartPosition;
            else _shineStartPosition = _shineEffect.localPosition;
        }
        
        public void OnFixedUpdate()
        {
            _animationTime += Time.fixedDeltaTime;
            
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
            
            if (_isShrinking)
            {
                _shrinkTimer = Mathf.Min(_shrinkTimer + Time.fixedDeltaTime, _shrinkDuration);
                var scale = Vector2.Lerp(new Vector2(1, 0), new Vector2(0, 0), _shrinkTimer / _shrinkDuration).x;
                transform.localScale = new Vector3(scale, scale, scale);
                
                if (Mathf.Approximately(_shrinkTimer, _shrinkDuration))
                {
                    IsAnimating = false;
                    _isShrinking = false;
                    _recycleGem?.Invoke(this);
                }
            }
            
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
        
        public void MakeSpecial(SpecialGemType specialType)
        {
            _isSpecial = true;
            _specialType = specialType;
            
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
        
        public void SwapTo(Vector2Int gridPosition, Vector2 worldPosition)
        {
            GridPosition = gridPosition;
            _positionBeforeSwap = transform.position;
            _moveTargetPosition = worldPosition;
            IsAnimating = true;
            _isSwapping = true;
            _swapTimer = 0;
        }
        
        public void DropTo(Vector2Int gridPosition, Vector2 initialWorldPosition, Vector2 finalWorldPosition)
        {
            GridPosition = gridPosition;
            transform.position = initialWorldPosition;
            _moveTargetPosition = finalWorldPosition;
            IsAnimating = true;
            _isDropping = true;
            _rigidbody.gravityScale = _gravityScale;
        }
        
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
        
        private void UpdateExplosiveVisuals()
        {
            for (int i = 0; i < _explosivePieces.Length; i++)
            {
                var explosivePiece = _explosivePieces[i];
                var startingPosition = _pieceStartingPositions[i];
                var newPosition = (Vector2)transform.position + startingPosition;
                
                if (i != _centerPieceIndex)
                {
                    _pieceDistances[i] = Vector2.Lerp(new Vector2(_pieceDistances[i], 0),
                        new Vector2(Random.Range(0, _pieceShakeDistance), 0), _pieceShakeSpeed).x;
                    newPosition += _pieceDistances[i] * startingPosition.normalized;
                }
                
                explosivePiece.MovePosition(newPosition);
            }
        }
        
        private void UpdateTargetingVisuals()
        {
            if (Mathf.Approximately(_shineWaitTimer, 0))
            {
                _shineEffect.localPosition = Vector2.Lerp(_shineStartPosition, -_shineStartPosition, _shineEffectTimer / _shineEffectDuration);
                _shineEffectTimer = Mathf.Min(_shineEffectTimer + Time.fixedDeltaTime, _shineEffectDuration);
                if (Mathf.Approximately(_shineEffectTimer, _shineEffectDuration)) _shineWaitTimer = _shineWaitDuration;
            }
            else
            {
                _shineWaitTimer = Mathf.Max(_shineWaitTimer - Time.fixedDeltaTime, 0);
                if (Mathf.Approximately(_shineWaitTimer, 0)) _shineEffectTimer = 0;
            }
            
            var scale = 1 + _shineScaleAmount * Mathf.Sin(_shineScaleSpeed * _animationTime);
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
