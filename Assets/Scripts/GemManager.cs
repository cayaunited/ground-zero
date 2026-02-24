using System.Collections.Generic;
using UnityEngine;

namespace GroundZero
{
    public class GemManager : MonoBehaviour
    {
        [SerializeField] [Min(3)] private int _gridSize;
        [SerializeField] [Min(0)] private float _spaceBetweenGems;
        [Tooltip("The y position of the bottom of the screen, at which gem pieces are no longer visible.")]
        [SerializeField] private float _screenBottom;
        [Tooltip("How far off the top of the grid to spawn new gems to make sure they spawn above the screen.")]
        [SerializeField] [Min(0)] private int _spawnPositionOffset;
        [SerializeField] private Gem[] _gemPrefabs;
        [SerializeField] private Transform _selectionCursor;
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _gemLayer;
        
        private GemGrid _grid;
        /// <summary>
        /// The list of gems connected to the grid of gems.
        /// </summary>
        private readonly List<Gem> _activeGems = new();
        /// <summary>
        /// The list of gems that are no longer in the active grid,
        /// but are still running animations and are therefore not inactive.
        /// </summary>
        private readonly List<Gem> _animatingGems = new();
        /// <summary>
        /// The dictionary of stacks of inactive gems,
        /// used to spawn in new gems once enough of that type have been created.
        /// </summary>
        private readonly Dictionary<int, Stack<Gem>> _inactiveGems = new();
        private bool _isAGemSelected;
        private Vector2Int _selectedGemPosition;
        // The grid should start off ready for the player to swap gems.
        private GridState _gridState = GridState.WaitingForInput;
        
        private void Awake()
        {
            // Initialize the grid and create empty stacks of each type of gem.
            var gemTypeCount = _gemPrefabs.Length;
            _grid = new GemGrid(_gridSize, gemTypeCount);
            
            for (int i = 0; i < gemTypeCount; i++)
            {
                _inactiveGems.Add(i, new());
            }
            
            FillGrid();
        }
        
        private void FixedUpdate()
        {
            var isAGemAnimating = false;
            
            // Update all the active gems and determine if any of them are busy animating,
            // like in the middle of a swap or falling down.
            for (int i = _activeGems.Count - 1; i >= 0; i--)
            {
                var gem = _activeGems[i];
                // Some spots in the grid may be empty, so don't try animating them.
                if (!gem) continue;
                gem.OnFixedUpdate();
                if (gem.IsAnimating && !isAGemAnimating) isAGemAnimating = true;
            }
            
            // Update all the animating gems that are no longer in the grid.
            for (int i = _animatingGems.Count - 1; i >= 0; i--)
            {
                var gem = _animatingGems[i];
                gem.OnFixedUpdate();
            }
            
            // Make sure we don't operate on the grid if any gems are actively in a swap or drop animation.
            if (isAGemAnimating) return;
            
            // Both destruction, falling, and spawning new gems can animate at the same time,
            // so long as the data is modified in the correct order.
            if (_gridState == GridState.Swapping)
            {
                DestroyAnyMatches();
                DropRemainingGems();
                SpawnReplacementGems();
            }
            else if (_gridState == GridState.Matching)
            {
                DropRemainingGems();
                SpawnReplacementGems();
            }
            else if (_gridState == GridState.Dropping) SpawnReplacementGems();
            // Once the empty spots have been replaced by new gems, try destroying any newly made matches.
            else if (_gridState == GridState.Replacing) DestroyAnyMatches();
        }
        
        /// <summary>
        /// Attempts to swap the gems at the given positions.
        /// If the given positions are the same, it just selects or unselects the gem at that position.
        /// If a gem is clicked while another is selected, then it tries to swap them.
        /// </summary>
        /// <param name="startingScreenPosition"></param>
        /// <param name="endingScreenPosition"></param>
        public void TrySwappingGems(Vector2 startingScreenPosition, Vector2 endingScreenPosition)
        {
            // Gems can only be swapped if there are no actions (like matching or dropping) are taking place.
            if (_gridState != GridState.WaitingForInput) return;
            
            // Determine if a gem was clicked or tapped based on how close the start and end of the swipe on the screen was.
            var distanceBetweenPositions = Vector2.Distance(startingScreenPosition, endingScreenPosition);
            var wasGemTapped = Mathf.Approximately(distanceBetweenPositions, 0);
            
            if (wasGemTapped)
            {
                // Use the camera to convert the screen position to a point / position in the game world.
                var worldPosition = ScreenToWorldPosition(startingScreenPosition);
                // Then, from that position, see if any gem's box collider contains that point.
                var gemCollider = Physics2D.OverlapPoint(worldPosition, _gemLayer);
                // Ignore any clicks outside of a gem.
                if (!gemCollider) return;
                var gem = gemCollider.GetComponent<Gem>();
                SelectGem(gem.GridPosition);
            }
            else
            {
                // Similarly to the code above, find the gems corresponding to where the mouse was pressed and released.
                var startingWorldPosition = ScreenToWorldPosition(startingScreenPosition);
                var endingWorldPosition = ScreenToWorldPosition(endingScreenPosition);
                var gemCollider1 = Physics2D.OverlapPoint(startingWorldPosition, _gemLayer);
                var gemCollider2 = Physics2D.OverlapPoint(endingWorldPosition, _gemLayer);
                var gem1 = gemCollider1 ? gemCollider1.GetComponent<Gem>() : null;
                var gem2 = gemCollider2 ? gemCollider2.GetComponent<Gem>() : null;
                
                if (gem1 && !gem2) SelectGem(gem1.GridPosition);
                else if (!gem1 && gem2) SelectGem(gem2.GridPosition);
                else if (gem1 && gem2 && gem1 == gem2) SelectGem(gem1.GridPosition);
                else if (gem1 && gem2 && gem1 != gem2) SwapGems(gem1.GridPosition, gem2.GridPosition);
            }
        }
        
        /// <summary>
        /// Moves any active or animating gems to the inactive lists,
        /// then randomly fills up the grid with new or recycled gems.
        /// </summary>
        private void FillGrid()
        {
            // Clean up any active or animating gems and mark them as inactive.
            foreach (var gem in _activeGems)
            {
                if (!gem) continue;
                gem.gameObject.SetActive(false);
                _inactiveGems[gem.TypeIndex].Push(gem);
            }
            
            foreach (var gem in _animatingGems)
            {
                gem.gameObject.SetActive(false);
                _inactiveGems[gem.TypeIndex].Push(gem);
            }
            
            _activeGems.Clear();
            _animatingGems.Clear();
            
            // Fill the grid data, then create or reuse visuals based on that data.
            _grid.FillGrid();
            
            for (int y = 0; y < _gridSize; y++)
            {
                for (int x = 0; x < _gridSize; x++)
                {
                    var gemType = _grid.GemIndexes[y][x];
                    var position = new Vector2Int(x, y);
                    // First, get a gem of the correct type from either the pool / list of inactive gems of that type,
                    // or create a new gem of that type if there aren't any available in the pool.
                    var gem = GetGem(gemType);
                    // Initialize the gem, passing in needed data for the animations to work properly.
                    gem.Initialize(gemType, position, GridToWorldPosition(position), _screenBottom, RecycleGem);
                    // Turn on the special gem visual effects if need be, based on the grid data.
                    if (_grid.SpecialGems.ContainsKey(position)) gem.MakeSpecial(_grid.SpecialGems[position]);
                    _activeGems.Add(gem);
                }
            }
        }
        
        /// <summary>
        /// If there is an inactive gem of the given type, pop it off the top of the stack and return it.
        /// Otherwise, create / instantiate a new gem of the given type.
        /// </summary>
        /// <param name="gemType"></param>
        /// <returns></returns>
        private Gem GetGem(int gemType)
        {
            return _inactiveGems[gemType].Count > 0 ? _inactiveGems[gemType].Pop()
                : Instantiate(_gemPrefabs[gemType]);
        }
        
        /// <summary>
        /// Returns a position in the world corresponding to the given grid position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Vector2 GridToWorldPosition(Vector2Int position)
        {
            // First, calculate the size of the grid in world space.
            var worldSize = _gridSize * _spaceBetweenGems;
            // Then, starting from the bottom left corner of the grid in the world space,
            // determine the position of the gem based on the grid position and the amount of space between the center of gems.
            return new Vector2(position.x * _spaceBetweenGems - worldSize / 2 + _spaceBetweenGems / 2,
                position.y * _spaceBetweenGems - worldSize / 2 + _spaceBetweenGems / 2);
        }
        
        /// <summary>
        /// Uses the camera to translate a position in the screen space to a position in the world space.
        /// </summary>
        /// <param name="screenPosition"></param>
        /// <returns></returns>
        private Vector2 ScreenToWorldPosition(Vector2 screenPosition)
        {
            return _camera.ScreenToWorldPoint(screenPosition);
        }
        
        /// <summary>
        /// Given a grid position, calculates the corresponding position in the list of active gems.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private int GridPositionToIndex(Vector2Int position)
        {
            return position.y * _gridSize + position.x;
        }
        
        /// <summary>
        /// Tries to select / unselect a gem at the given grid position,
        /// or swaps them if a gem adjacent to the given position is already selected.
        /// </summary>
        /// <param name="position"></param>
        private void SelectGem(Vector2Int position)
        {
            if (_isAGemSelected && position != _selectedGemPosition)
            {
                // Either the gems at the two selected positions are adjacent and we should swap them,
                // or they are far enough apart that we should just select the newly clicked one instead.
                if (_grid.ArePositionsAdjacent(_selectedGemPosition, position))
                    SwapGems(_selectedGemPosition, position);
                else
                {
                    _selectedGemPosition = position;
                    // Since we generally toggle the state of whether or not a gem is selected whenever a click on a gem happens,
                    // we need to pretend a gem isn't selected so below it says one is.
                    _isAGemSelected = false;
                }
            }
            else if (!_isAGemSelected) _selectedGemPosition = position;
            
            // Generally, clicking on a gem will either select it when nothing was previously selected,
            // or swap it, resulting in no gem being selected anymore.
            _isAGemSelected = !_isAGemSelected;
            
            // Toggle the selection cursor's visibility and move it as needed.
            _selectionCursor.gameObject.SetActive(_isAGemSelected);
            if (_isAGemSelected) _selectionCursor.transform.position = GridToWorldPosition(_selectedGemPosition);
        }
        
        /// <summary>
        /// Swaps the gems at the given positions,
        /// if they are different types of gems at different but adjacent positions,
        /// and if the swap would result in a match.
        /// </summary>
        /// <param name="position1"></param>
        /// <param name="position2"></param>
        private void SwapGems(Vector2Int position1, Vector2Int position2)
        {
            // Try swapping the gems in the data.
            var wereSwapped = _grid.SwapGems(position1, position2);
            if (!wereSwapped) return;
            // Since the swap in the data was successful, we can now swap the gem visuals.
            _gridState = GridState.Swapping;
            
            var index1 = GridPositionToIndex(position1);
            var index2 = GridPositionToIndex(position2);
            
            // Swap the gems in the manager's data.
            var gem1 = _activeGems[index1];
            var gem2 = _activeGems[index2];
            _activeGems[index1] = gem2;
            _activeGems[index2] = gem1;
            
            // Start the swap animations.
            gem1.SwapTo(position2, GridToWorldPosition(position2));
            gem2.SwapTo(position1, GridToWorldPosition(position1));
        }
        
        /// <summary>
        /// Looks for any matches in the grid, then destroys them.
        /// </summary>
        private void DestroyAnyMatches()
        {
            // Find matches in the grid, making sure to create any special gems,
            // since we're not just looking to see if a match is possible.
            var wereMatchesCreated = _grid.FindMatches(createSpecialGems: true) > 0;
            
            // If no matches were created, then the player can swap gems again.
            if (!wereMatchesCreated)
            {
                // Make sure to refill the grid if there are any possible matches.
                if (!_grid.AreTherePossibleMatches()) FillGrid();
                _gridState = GridState.WaitingForInput;
                return;
            }
            
            _gridState = GridState.Matching;
            
            // Destroy matches in the grid data, then update the visuals based on that.
            _grid.DestroyMatches();
            
            foreach (var position in _grid.DestroyedGems)
            {
                var index = GridPositionToIndex(position);
                var gem = _activeGems[index];
                // Show either a shrink or explosion animation based on how the gem was destroyed.
                gem.Destroy(shouldExplode: _grid.GemsDestroyedByExplosions.Contains(position));
                _animatingGems.Add(gem);
                _activeGems[index] = null;
            }
            
            // Once the old gems are destroyed, we can create visuals for the newly created special gems,
            // if any were created from the destroyed matches.
            foreach (var (position, specialType) in _grid.SpecialGemsCreated)
            {
                var gemType = _grid.SpecialGemTypesCreated[position];
                var gem = GetGem(gemType);
                gem.Initialize(gemType, position, GridToWorldPosition(position), _screenBottom, RecycleGem);
                gem.MakeSpecial(specialType);
                var index = GridPositionToIndex(position);
                _activeGems[index] = gem;
            }
        }
        
        /// <summary>
        /// Drops any gems remaining after destruction occurs.
        /// </summary>
        private void DropRemainingGems()
        {
            _gridState = GridState.Dropping;
            _grid.DropGems();
            
            foreach (var (initialPosition, finalPosition) in _grid.DroppedGems)
            {
                var initialIndex = GridPositionToIndex(initialPosition);
                var finalIndex = GridPositionToIndex(finalPosition);
                // Preserve the correct order of the active gems.
                var gem = _activeGems[initialIndex];
                _activeGems[finalIndex] = gem;
                _activeGems[initialIndex] = null;
                gem.DropTo(finalPosition, GridToWorldPosition(initialPosition), GridToWorldPosition(finalPosition));
            }
        }
        
        /// <summary>
        /// Fill in any empty spots after gems dropped with new gems,
        /// dropping them to the empty spots from above the screen.
        /// </summary>
        private void SpawnReplacementGems()
        {
            _gridState = GridState.Replacing;
            _grid.SpawnNewGems();
            
            int minYPosition = _gridSize;
            
            // Figure out what the minimum y position of any newly spawned gems is,
            // so that way the gems are only barely above the screen (allowing for a quicker animation).
            foreach (var position in _grid.SpawnedGems)
            {
                if (position.y < minYPosition) minYPosition = position.y;
            }
            
            foreach (var position in _grid.SpawnedGems)
            {
                var gemType = _grid.GemIndexes[position.y][position.x];
                var gem = GetGem(gemType);
                // Calculate how high above the grid the newly spawned gems should drop from,
                // ensuring that the bottom-most spawned gems are the first ones to appear when falling.
                var spawnPosition = GridToWorldPosition(new Vector2Int(position.x, position.y - minYPosition + _gridSize + _spawnPositionOffset));
                gem.Initialize(gemType, position, spawnPosition, _screenBottom, RecycleGem);
                gem.DropTo(position, spawnPosition, GridToWorldPosition(position));
                var index = GridPositionToIndex(position);
                _activeGems[index] = gem;
            }
        }
        
        /// <summary>
        /// Turns off the gem's GameObject, adds it to the corresponding inactive pool,
        /// and removes it from the list of gems currently being animated.
        /// </summary>
        /// <param name="gem"></param>
        private void RecycleGem(Gem gem)
        {
            gem.gameObject.SetActive(false);
            _inactiveGems[gem.TypeIndex].Push(gem);
            _animatingGems.Remove(gem);
        }
    }
}
