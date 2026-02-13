using UnityEngine;

namespace GroundZero
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Gem : MonoBehaviour
    {
        // Only the Gem class will ever need to access the SpriteRenderer,
        // so make it private and add an underscore to quickly identify
        // it later in the code as private.
        private SpriteRenderer _renderer;
        
        // We want to be able to read the grid position of this gem from anywhere,
        // but we only want this gem to modify the grid position,
        // so we used a property with a public getter and a private setter.
        public Vector2Int GridPosition { get; private set; }
        
        /// <summary>
        /// Spawns this gem with the given sprite at the given position.
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="gridPosition"></param>
        /// <param name="worldPosition"></param>
        public void Initialize(Sprite sprite, Vector2Int gridPosition, Vector2 worldPosition)
        {
            // Find the SpriteRenderer if it wasn't already found.
            // This ensures it's only ever called once (since the result never changes).
            if (!_renderer) _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            GridPosition = gridPosition;
            transform.position = worldPosition;
            gameObject.SetActive(true);
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
