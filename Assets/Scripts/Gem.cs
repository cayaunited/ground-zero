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
        public int TypeIndex { get; private set; }
        
        /// <summary>
        /// Spawns this gem with the given sprite at the given position.
        /// </summary>
        /// <param name="typeIndex"></param>
        /// <param name="gridPosition"></param>
        /// <param name="worldPosition"></param>
        public void Initialize(int typeIndex, Vector2Int gridPosition, Vector2 worldPosition)
        {
            // Find the SpriteRenderer if it wasn't already found.
            // This ensures it's only ever called once (since the result never changes).
            if (!_renderer) _renderer = GetComponent<SpriteRenderer>();
            GridPosition = gridPosition;
            transform.position = worldPosition;
            TypeIndex = typeIndex;
            gameObject.SetActive(true);
            
            // TODO: TEMPORARY SOLUTION FOR SPECIALS
            _renderer.color = Color.white;
        }
        
        /// <summary>
        /// Marks this gem as the given special type of gem.
        /// </summary>
        /// <param name="specialType"></param>
        public void MakeSpecial(SpecialGemType specialType)
        {
            // TODO: TEMPORARY SOLUTION FOR SPECIALS
            var color = Color.white;
            if (specialType == SpecialGemType.Targeting) color.a = 0.5f;
            else if (specialType == SpecialGemType.Explosive) color = Color.red;
            _renderer.color = color;
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
