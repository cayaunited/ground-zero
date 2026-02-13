using UnityEngine;
using UnityEngine.InputSystem;

namespace GroundZero
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private GemManager _gemManager;
        
        private Vector2 _mouseScreenPosition;
        private Vector2 _swipeStartPosition;
        
        /// <summary>
        /// Updates swiping position and tries to select or swap gems.
        /// </summary>
        /// <param name="context"></param>
        public void OnSelectGem(InputAction.CallbackContext context)
        {
            // If the mouse button was pressed, then start swiping.
            if (context.started) _swipeStartPosition = _mouseScreenPosition;
            // If the mouse button was released, then stop swiping
            // and try selecting or swapping gems.
            else if (context.canceled) _gemManager.TrySwappingGems(_swipeStartPosition, _mouseScreenPosition);
        }
        
        /// <summary>
        /// Update's the mouse's position when receiving movement input.
        /// </summary>
        /// <param name="context"></param>
        public void OnMoveMouse(InputAction.CallbackContext context)
        {
            _mouseScreenPosition = context.ReadValue<Vector2>();
        }
    }
}
