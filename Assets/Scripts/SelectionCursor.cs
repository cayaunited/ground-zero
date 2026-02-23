using UnityEngine;

namespace GroundZero
{
    public class SelectionCursor : MonoBehaviour
    {
        [SerializeField] [Min(0)] private float _rotationSpeed;
        [SerializeField] [Min(0)] private float _scaleSpeed;
        [SerializeField] [Min(1)] private float _maxScale;
        
        /// <summary>
        /// How long, in seconds, the cursor has been shown.
        /// Although Time.time could be used in the sine function,
        /// using a custom animation time makes sure every time the cursor becomes visible
        /// it starts at the same frame in animation.
        /// </summary>
        private float _animationTime;
        
        private void OnEnable()
        {
            // Make sure to reset the cursor's animation data every time it becomes visible.
            _animationTime = 0;
            transform.localEulerAngles = Vector3.zero;
            transform.localScale = Vector3.one;
        }
        
        private void Update()
        {
            // First, update how much time has passed. This differs between Update and FixedUpdate.
            // Update uses Time.deltaTime to show how much time has passed since it was last called,
            // whereas FixedUpdate uses Time.fixedDeltaTime to show the amount of time passed since it was last called.
            _animationTime += Time.deltaTime;
            
            // Next, rotate the cursor around the z-axis. In 2D games, you will usually only ever
            // rotate objects around the z-axis, because it's perpendicular to the screen and therefore
            // the axis needed to rotate objects in the xy-plane that you see in 2D games.
            transform.localEulerAngles -= new Vector3(0, 0, _rotationSpeed * Time.deltaTime);
            
            // For the most part, we don't want the cursor to overlap with the gem, but rather surround it.
            // Therefore, the minimum scale we want is 1, and the maximum is set in the Unity inspector.
            // We'll use the sine wave function to create a smooth pulsing animation for the cursor,
            // meaning we need to know the amplitude of the sine wave, which is half the max delta (or change) in scale.
            // The center of the wave is therefore at 1 plus the amplitude, so that there is room above and below the center
            // for the sine wave when its magnitude reaches the amplitude.
            var maxScaleDelta = _maxScale - 1;
            var scale = 1 + maxScaleDelta / 2 + maxScaleDelta / 2 * Mathf.Sin(_scaleSpeed * _animationTime);
            
            // Now that we've calculated the scale, apply it in the x and y axes.
            // We don't need to scale in the x-axis, because that's not used in a 2D game for scaling.
            transform.localScale = new Vector3(scale, scale, 1);
        }
    }
}
