using System.Collections.Generic;
using UnityEngine;

namespace GroundZero
{
    public class GemManager : MonoBehaviour
    {
        // Since the grid size isn't needed anywhere else, make it private.
        // However, we want to see and edit it in the Unity inspector, so add the SerializeField attribute.
        // We also don't want any grid size below 3 (the minimum match length),
        // so set the minimum to 3.
        [SerializeField] [Min(3)] private int _gridSize;
        [SerializeField] [Min(0)] private float _spaceBetweenGems;
        /// <summary>
        /// The array of different prefabs used, based on the gem type.
        /// The gem's type index is used in this array.
        /// </summary>
        [SerializeField] private Gem[] _gemPrefabs;
        [SerializeField] private Transform _selectionCursor;
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _gemLayer;
        
        /// <summary>
        /// The data used to represent the grid of gems,
        /// containing the positions and types of each gem.
        /// </summary>
        private GemGrid _grid;
        /// <summary>
        /// The gems that are currently visible and in the grid.
        /// The order of the list is maintained in such a way that
        /// the first item is the first column (x = 0) of the first row (y = 0),
        /// and the last item is the last column of the last row.
        /// </summary>
        private readonly List<Gem> _activeGems = new();
        /// <summary>
        /// The gems that are no longer visible and used to replace matched gems.
        /// Uses a Stack instead of a List because the order doesn't matter here.
        /// Also, use a dictionary to find a pool based on type.
        /// </summary>
        private readonly Dictionary<int, Stack<Gem>> _inactiveGems = new();
        private Vector2Int _selectedGemPosition;
        private bool _isAGemSelected;
        private bool _areGemsMatching;
        
        private void Awake()
        {
            // When the player starts the game, create an empty grid.
            var gemTypeCount = _gemPrefabs.Length;
            _grid = new GemGrid(_gridSize, gemTypeCount);
            
            // Create a new inactive gem pool for each gem type.
            for (int i = 0; i < gemTypeCount; i++)
            {
                _inactiveGems.Add(i, new());
            }
            
            // TODO: REMOVE
            FillGrid();
        }
        
        private void Update()
        {
            foreach (var gem in _activeGems)
            {
                gem.OnUpdate();
            }
        }
        
        /// <summary>
        /// Tries either selecting or swapping gems based on the given mouse screen positions.
        /// </summary>
        /// <param name="startingScreenPosition">The mouse's screen position at the start of the swipe.</param>
        /// <param name="endingScreenPosition">The mouse's screen position at the end of the swipe.</param>
        public void TrySwappingGems(Vector2 startingScreenPosition, Vector2 endingScreenPosition)
        {
            // Prevent swapping while matching.
            if (_areGemsMatching) return;
            
            var distanceBetweenPositions = Vector2.Distance(startingScreenPosition, endingScreenPosition);
            // If the distance between the starting and ending positions is zero,
            // then the screen was tapped so we should try to select / deselect a gem.
            var wasGemTapped = Mathf.Approximately(distanceBetweenPositions, 0);
            
            if (wasGemTapped)
            {
                var worldPosition = ScreenToWorldPosition(startingScreenPosition);
                // Ask the physics system if there's a collider at the given position on the gem layer.
                var gemCollider = Physics2D.OverlapPoint(worldPosition, _gemLayer);
                // If there is, then select this gem.
                if (!gemCollider) return;
                var gem = gemCollider.GetComponent<Gem>();
                SelectGem(gem.GridPosition);
            }
            // Otherwise, we should figure out which gems to swap.
            else
            {
                var startingWorldPosition = ScreenToWorldPosition(startingScreenPosition);
                var endingWorldPosition = ScreenToWorldPosition(endingScreenPosition);
                var gemCollider1 = Physics2D.OverlapPoint(startingWorldPosition, _gemLayer);
                var gemCollider2 = Physics2D.OverlapPoint(endingWorldPosition, _gemLayer);
                var gem1 = gemCollider1 ? gemCollider1.GetComponent<Gem>() : null;
                var gem2 = gemCollider2 ? gemCollider2.GetComponent<Gem>() : null;
                
                // If only one gem was tapped, then try selecting it or swapping with it.
                if (gem1 && !gem2) SelectGem(gem1.GridPosition);
                else if (!gem1 && gem2) SelectGem(gem2.GridPosition);
                else if (gem1 && gem2 && gem1 == gem2) SelectGem(gem1.GridPosition);
                // If both gems were tapped, then try swapping them.
                else if (gem1 && gem2 && gem1 != gem2) SwapGems(gem1.GridPosition, gem2.GridPosition);
            }
        }
        
        /// <summary>
        /// Spawns in a new grid of gems, clearing out any old ones.
        /// </summary>
        private void FillGrid()
        {
            // Clear out all previous gems.
            foreach (var gem in _activeGems)
            {
                // Some spots in the list may be blank, if there is no gem there.
                if (!gem) continue;
                gem.gameObject.SetActive(false);
                _inactiveGems[gem.TypeIndex].Push(gem);
            }
            
            _activeGems.Clear();
            
            // Fill up the new grid with gems,
            // spawning in each gem visual based on the type index in the grid.
            _grid.FillGrid();
            
            for (int y = 0; y < _gridSize; y++)
            {
                for (int x = 0; x < _gridSize; x++)
                {
                    // Get a new gem and initialize it with the correct sprite / prefab at the correct position,
                    // marking it as special as need be.
                    var gemType = _grid.GemIndexes[y][x];
                    var gem = GetGem(gemType);
                    var position = new Vector2Int(x, y);
                    gem.Initialize(gemType, position, GridToWorldPosition(x, y));
                    if (_grid.SpecialGems.ContainsKey(position)) gem.MakeSpecial(_grid.SpecialGems[position]);
                    _activeGems.Add(gem);
                }
            }
        }
        
        /// <summary>
        /// Returns a gem from the pool, or creates a new one if there are none in the pool.
        /// </summary>
        /// <param name="gemType"></param>
        /// <returns></returns>
        private Gem GetGem(int gemType)
        {
            return _inactiveGems[gemType].Count > 0 ? _inactiveGems[gemType].Pop() : Instantiate(_gemPrefabs[gemType]);
        }
        
        /// <summary>
        /// Returns a gem's position in the world space based on the given position in the grid.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private Vector2 GridToWorldPosition(int x, int y)
        {
            // Calculate the actual size taken up by the gems so that
            // the middle of the grid is at the middle of the screen.
            var worldSize = _gridSize * _spaceBetweenGems;
            return new Vector2(x * _spaceBetweenGems - worldSize / 2 + _spaceBetweenGems / 2,
                y * _spaceBetweenGems - worldSize / 2 + _spaceBetweenGems / 2);
        }
        
        /// <summary>
        /// Calculates a world position based on the given screen position using the camera.
        /// </summary>
        /// <param name="screenPosition"></param>
        /// <returns></returns>
        private Vector2 ScreenToWorldPosition(Vector2 screenPosition)
        {
            return _camera.ScreenToWorldPoint(screenPosition);
        }
        
        /// <summary>
        /// Returns a gem's index in the active list based on its grid position.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int GridPositionToIndex(int x, int y)
        {
            return y * _gridSize + x;
        }
        
        /// <summary>
        /// If no gem has been selected, selects the gem at the given position.
        /// If a gem has been selected and the given position is for the selected gem,
        /// unselects the selected gem.
        /// If a gem has been selected and the given position is for a different gem,
        /// tries to swap the gems.
        /// </summary>
        /// <param name="position"></param>
        private void SelectGem(Vector2Int position)
        {
            // Either swap gems or select the correct gem.
            if (_isAGemSelected && position != _selectedGemPosition)
                SwapGems(_selectedGemPosition, position);
            else if (!_isAGemSelected) _selectedGemPosition = position;
            
            // Whether or not a gem has been selected will always be toggled when calling this method.
            _isAGemSelected = !_isAGemSelected;
            
            // Update the selection cursor.
            _selectionCursor.gameObject.SetActive(_isAGemSelected);
            if (_isAGemSelected) _selectionCursor.transform.position = GridToWorldPosition(_selectedGemPosition.x, _selectedGemPosition.y);
        }
        
        /// <summary>
        /// Tries to swap the gems at the given positions.
        /// </summary>
        /// <param name="position1"></param>
        /// <param name="position2"></param>
        private void SwapGems(Vector2Int position1, Vector2Int position2)
        {
            var wereSwapped = _grid.SwapGems(position1, position2);
            if (!wereSwapped) return;
            
            var index1 = GridPositionToIndex(position1.x, position1.y);
            var index2 = GridPositionToIndex(position2.x, position2.y);
            
            // First, swap the gems in the list.
            var gem1 = _activeGems[index1];
            var gem2 = _activeGems[index2];
            _activeGems[index1] = gem2;
            _activeGems[index2] = gem1;
            
            // Then, tell the gems to move to their new positions.
            gem1.MoveTo(position2, GridToWorldPosition(position2.x, position2.y));
            gem2.MoveTo(position1, GridToWorldPosition(position1.x, position1.y));
            
            _areGemsMatching = true;
            DestroyAnyMatches();
        }
        
        /// <summary>
        /// Checks for any matches that were just made,
        /// and starts destroying them if there are any.
        /// Creates new special gems as needed.
        /// Refills grid if there aren't any matches made and if none are possible.
        /// </summary>
        private void DestroyAnyMatches()
        {
            // First, match and destroy the gems in the data (if there are any matches).
            var wereMatchesCreated = _grid.FindMatches(createSpecialGems: true) > 0;
            
            if (!wereMatchesCreated)
            {
                if (!_grid.AreTherePossibleMatches()) FillGrid();
                _areGemsMatching = false;
                return;
            }
            
            foreach (var (position, type) in _grid.SpecialGemsCreated)
            {
                var index = GridPositionToIndex(position.x, position.y);
                var gem = _activeGems[index];
                gem.MakeSpecial(type);
            }
            
            _grid.DestroyMatches();
            
            // Then, propogate that destruction to the visuals.
            foreach (var position in _grid.DestroyedGems)
            {
                RecycleGem(position.x, position.y);
            }
            
            DropRemainingGems();
        }
        
        /// <summary>
        /// Makes any remaining gems after a match drop to a lower position if possible.
        /// </summary>
        private void DropRemainingGems()
        {
            // First, drop the gems in the data.
            _grid.DropGems();
            
            // Then, propogate that drop to the visuals.
            foreach (var (initialPosition, finalPosition) in _grid.DroppedGems)
            {
                var initialIndex = GridPositionToIndex(initialPosition.x, initialPosition.y);
                var finalIndex = GridPositionToIndex(finalPosition.x, finalPosition.y);
                var gem = _activeGems[initialIndex];
                // Make sure to maintain the correct order in the active gems list.
                _activeGems[finalIndex] = gem;
                _activeGems[initialIndex] = null;
                gem.MoveTo(finalPosition, GridToWorldPosition(finalPosition.x, finalPosition.y));
            }
            
            SpawnReplacementGems();
        }
        
        /// <summary>
        /// Spawns new gems to fall from the top and replace the destroyed gems.
        /// </summary>
        private void SpawnReplacementGems()
        {
            _grid.SpawnNewGems();
            
            foreach (var position in _grid.SpawnedGems)
            {
                // First, create the gem (or grab one from the inactive pool if any are available).
                var gemType = _grid.GemIndexes[position.y][position.x];
                var gem = GetGem(gemType);
                gem.Initialize(gemType, position, GridToWorldPosition(position.x, position.y));
                
                // Then, track it in the correct position.
                var index = GridPositionToIndex(position.x, position.y);
                _activeGems[index] = gem;
            }
            
            // Loop back around to see if any new matches were made.
            DestroyAnyMatches();
        }
        
        /// <summary>
        /// Recycles the gem at the given position by deactivating the GameObject,
        /// removing it from the active list, and placing it in the inactive pool.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void RecycleGem(int x, int y)
        {
            var index = GridPositionToIndex(x, y);
            var gem = _activeGems[index];
            gem.gameObject.SetActive(false);
            _inactiveGems[gem.TypeIndex].Push(gem);
            _activeGems[index] = null;
        }
    }
}
