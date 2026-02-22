using UnityEngine;

namespace GroundZero
{
    public class SelectionCursor : MonoBehaviour
    {
        [SerializeField] [Min(0)] private float _rotationSpeed;
        [SerializeField] [Min(0)] private float _scaleSpeed;
        [SerializeField] [Min(1)] private float _maxScale;
        
        private float _animationTime;
        
        private void OnEnable()
        {
            _animationTime = 0;
            transform.localEulerAngles = new Vector3(0, 0, 0);
            transform.localScale = new Vector3(1, 1, 1);
        }
        
        private void Update()
        {
            _animationTime += Time.deltaTime;
            transform.localEulerAngles -= new Vector3(0, 0, _rotationSpeed * Time.deltaTime);
            var maxScaleDelta = _maxScale - 1;
            var scale = 1 + maxScaleDelta / 2 + maxScaleDelta / 2 * Mathf.Sin(_scaleSpeed * _animationTime);
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
