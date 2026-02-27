using TMPro;
using UnityEngine;

namespace GroundZero
{
    [RequireComponent(typeof(TextMeshPro))]
    public class PointsEffect : MonoBehaviour
    {
        [SerializeField] private float _scaleDuration;
        [SerializeField] private float _fadeDuration;
        
        private TextMeshPro _label;
        private System.Action<PointsEffect> _recycleEffect;
        private float _scaleTimer;
        private float _fadeTimer;
        
        /// <summary>
        /// Initializes the points effect at the given position for the given amount of points.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="amount"></param>
        /// <param name="recycleEffect"></param>
        public void Initialize(Vector2 position, int amount, System.Action<PointsEffect> recycleEffect)
        {
            if (!_label) _label = GetComponent<TextMeshPro>();
            _label.text = $"{amount}";
            _label.alpha = 1;
            transform.position = position;
            transform.localScale = new Vector3(0, 0, 1);
            gameObject.SetActive(true);
            _scaleTimer = 0;
            _fadeTimer = 0;
            _recycleEffect = recycleEffect;
        }
        
        /// <summary>
        /// Scales up when becoming visible, then fades away.
        /// </summary>
        public void OnUpdate()
        {
            // We can use the Update method because this animation isn't really
            // using anything physics related, like movement or rotation.
            
            if (_scaleTimer < _scaleDuration)
            {
                _scaleTimer = Mathf.Min(_scaleTimer + Time.deltaTime, _scaleDuration);
                var scale = Vector2.Lerp(new Vector2(0, 0), new Vector2(1, 0), _scaleTimer / _scaleDuration).x;
                transform.localScale = new Vector3(scale, scale, 1);
            }
            else if (_fadeTimer < _fadeDuration)
            {
                _fadeTimer = Mathf.Min(_fadeTimer + Time.deltaTime, _fadeDuration);
                _label.alpha = Vector2.Lerp(new Vector2(1, 0), new Vector2(0, 0), _fadeTimer / _fadeDuration).x;
            }
            else _recycleEffect(this);
        }
    }
}
