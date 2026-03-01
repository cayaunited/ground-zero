using UnityEngine;
using UnityEngine.InputSystem;

namespace GroundZero
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private GemManager _gemManager;
        
        private PlayerInput _input;
        private Vector2 _mouseScreenPosition;
        private Vector2 _swipeStartPosition;
        
        /// <summary>
        /// Switches the input to the gameplay map so the player can swap gems again.
        /// </summary>
        public void OnGameStarted()
        {
            if (!_input) _input = GetComponent<PlayerInput>();
            _input.SwitchCurrentActionMap("Gameplay");
        }
        
        /// <summary>
        /// Switches the input to the UI map so the player can't swap gems anymore.
        /// </summary>
        public void OnGameEnded()
        {
            if (!_input) _input = GetComponent<PlayerInput>();
            _input.SwitchCurrentActionMap("UI");
        }
        
        /// <summary>
        /// Called whenever the mouse is moved.
        /// We only need to update the position of the mouse when it's moved,
        /// so use a callback method like this to run in response to a change in input,
        /// instead of reading the mouse's position every frame.
        /// </summary>
        /// <param name="context"></param>
        public void OnMoveMouse(InputAction.CallbackContext context)
        {
            // Each input callback function is passed some context about the input event calling this function.
            // In this case, we are only interested in the input's value, which is a Vector2 for mouse position on the screen.
            _mouseScreenPosition = context.ReadValue<Vector2>();
        }
        
        /// <summary>
        /// Called whenever the mouse button is pressed or released.
        /// </summary>
        /// <param name="context"></param>
        public void OnSelectGem(InputAction.CallbackContext context)
        {
            // If the input event was just started according to the context,
            // that means the left mouse button was just pressed and is still being pressed.
            // If the player is using swipe controls to swap gems, we need to know where that swipe started,
            // which is going to be the position of their mouse when they started pressing the button.
            if (context.started) _swipeStartPosition = _mouseScreenPosition;
            
            // If the input event was just canceled, that means the player just released the mouse button.
            // Therefore, we should try swapping the gems at both the position where they started pressing the button,
            // and the position where they stopped pressing the button.
            // This case will also account for just clicking on a gem to select or unselect it.
            else if (context.canceled) _gemManager.TrySwappingGems(_swipeStartPosition, _mouseScreenPosition);
        }
    }
}
