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
        
        public void OnSelectGem(InputAction.CallbackContext context)
        {
            if (context.started) _swipeStartPosition = _mouseScreenPosition;
            else if (context.canceled) _gemManager.TrySwappingGems(_swipeStartPosition, _mouseScreenPosition);
        }
        
        public void OnMoveMouse(InputAction.CallbackContext context)
        {
            _mouseScreenPosition = context.ReadValue<Vector2>();
        }
    }
}
