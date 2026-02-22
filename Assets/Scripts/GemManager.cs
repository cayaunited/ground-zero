using System.Collections.Generic;
using UnityEngine;

namespace GroundZero
{
    public class GemManager : MonoBehaviour
    {
        [SerializeField] [Min(3)] private int _gridSize;
        [SerializeField] [Min(0)] private float _spaceBetweenGems;
        [SerializeField] private float _screenBottom;
        [SerializeField] [Min(0)] private int _spawnPositionOffset;
        [SerializeField] private Gem[] _gemPrefabs;
        [SerializeField] private Transform _selectionCursor;
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _gemLayer;
        
        private GemGrid _grid;
        private readonly List<Gem> _activeGems = new();
        private readonly List<Gem> _animatingGems = new();
        private readonly Dictionary<int, Stack<Gem>> _inactiveGems = new();
        private Vector2Int _selectedGemPosition;
        private bool _isAGemSelected;
        private GridState _gridState = GridState.WaitingForInput;
        
        private void Awake()
        {
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
            
            for (int i = _activeGems.Count - 1; i >= 0; i--)
            {
                var gem = _activeGems[i];
                if (!gem) continue;
                gem.OnFixedUpdate();
                if (gem.IsAnimating && !isAGemAnimating) isAGemAnimating = true;
            }
            
            for (int i = _animatingGems.Count - 1; i >= 0; i--)
            {
                var gem = _animatingGems[i];
                if (!gem) continue;
                gem.OnFixedUpdate();
            }
            
            if (!isAGemAnimating)
            {
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
                else if (_gridState == GridState.Replacing) DestroyAnyMatches();
            }
        }
        
        public void TrySwappingGems(Vector2 startingScreenPosition, Vector2 endingScreenPosition)
        {
            if (_gridState != GridState.WaitingForInput) return;
            
            var distanceBetweenPositions = Vector2.Distance(startingScreenPosition, endingScreenPosition);
            var wasGemTapped = Mathf.Approximately(distanceBetweenPositions, 0);
            
            if (wasGemTapped)
            {
                var worldPosition = ScreenToWorldPosition(startingScreenPosition);
                var gemCollider = Physics2D.OverlapPoint(worldPosition, _gemLayer);
                if (!gemCollider) return;
                var gem = gemCollider.GetComponent<Gem>();
                SelectGem(gem.GridPosition);
            }
            else
            {
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
        
        private void FillGrid()
        {
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
            
            _grid.FillGrid();
            
            for (int y = 0; y < _gridSize; y++)
            {
                for (int x = 0; x < _gridSize; x++)
                {
                    var gemType = _grid.GemIndexes[y][x];
                    var gem = GetGem(gemType);
                    var position = new Vector2Int(x, y);
                    gem.Initialize(gemType, position, GridToWorldPosition(x, y), _screenBottom, RecycleGem);
                    if (_grid.SpecialGems.ContainsKey(position)) gem.MakeSpecial(_grid.SpecialGems[position]);
                    _activeGems.Add(gem);
                }
            }
        }
        
        private Gem GetGem(int gemType)
        {
            return _inactiveGems[gemType].Count > 0 ? _inactiveGems[gemType].Pop() : Instantiate(_gemPrefabs[gemType]);
        }
        
        private Vector2 GridToWorldPosition(int x, int y)
        {
            var worldSize = _gridSize * _spaceBetweenGems;
            return new Vector2(x * _spaceBetweenGems - worldSize / 2 + _spaceBetweenGems / 2,
                y * _spaceBetweenGems - worldSize / 2 + _spaceBetweenGems / 2);
        }
        
        private Vector2 ScreenToWorldPosition(Vector2 screenPosition)
        {
            return _camera.ScreenToWorldPoint(screenPosition);
        }
        
        private int GridPositionToIndex(int x, int y)
        {
            return y * _gridSize + x;
        }
        
        private void SelectGem(Vector2Int position)
        {
            if (_isAGemSelected && position != _selectedGemPosition)
            {
                if (_grid.ArePositionsAdjacent(_selectedGemPosition, position))
                    SwapGems(_selectedGemPosition, position);
                else
                {
                    _selectedGemPosition = position;
                    _isAGemSelected = false;
                }
            }
            else if (!_isAGemSelected) _selectedGemPosition = position;
            
            _isAGemSelected = !_isAGemSelected;
            
            _selectionCursor.gameObject.SetActive(_isAGemSelected);
            if (_isAGemSelected) _selectionCursor.transform.position = GridToWorldPosition(_selectedGemPosition.x, _selectedGemPosition.y);
        }
        
        private void SwapGems(Vector2Int position1, Vector2Int position2)
        {
            var wereSwapped = _grid.SwapGems(position1, position2);
            if (!wereSwapped) return;
            _gridState = GridState.Swapping;
            
            var index1 = GridPositionToIndex(position1.x, position1.y);
            var index2 = GridPositionToIndex(position2.x, position2.y);
            
            var gem1 = _activeGems[index1];
            var gem2 = _activeGems[index2];
            _activeGems[index1] = gem2;
            _activeGems[index2] = gem1;
            
            gem1.SwapTo(position2, GridToWorldPosition(position2.x, position2.y));
            gem2.SwapTo(position1, GridToWorldPosition(position1.x, position1.y));
        }
        
        private void DestroyAnyMatches()
        {
            var wereMatchesCreated = _grid.FindMatches(createSpecialGems: true) > 0;
            
            if (!wereMatchesCreated)
            {
                if (!_grid.AreTherePossibleMatches()) FillGrid();
                _gridState = GridState.WaitingForInput;
                return;
            }
            
            _gridState = GridState.Matching;
            
            _grid.DestroyMatches();
            
            foreach (var position in _grid.DestroyedGems)
            {
                var index = GridPositionToIndex(position.x, position.y);
                var gem = _activeGems[index];
                gem.Destroy(shouldExplode: _grid.GemsDestroyedByExplosions.Contains(position));
                _animatingGems.Add(gem);
                _activeGems[index] = null;
            }
            
            foreach (var (position, specialType) in _grid.SpecialGemsCreated)
            {
                var gemType = _grid.SpecialGemTypesCreated[position];
                var gem = GetGem(gemType);
                gem.Initialize(gemType, position, GridToWorldPosition(position.x, position.y), _screenBottom, RecycleGem);
                gem.MakeSpecial(specialType);
                var index = GridPositionToIndex(position.x, position.y);
                _activeGems[index] = gem;
            }
        }
        
        private void DropRemainingGems()
        {
            _gridState = GridState.Dropping;
            _grid.DropGems();
            
            foreach (var (initialPosition, finalPosition) in _grid.DroppedGems)
            {
                var initialIndex = GridPositionToIndex(initialPosition.x, initialPosition.y);
                var finalIndex = GridPositionToIndex(finalPosition.x, finalPosition.y);
                var gem = _activeGems[initialIndex];
                _activeGems[finalIndex] = gem;
                _activeGems[initialIndex] = null;
                gem.DropTo(finalPosition, GridToWorldPosition(initialPosition.x, initialPosition.y), GridToWorldPosition(finalPosition.x, finalPosition.y));
            }
        }
        
        private void SpawnReplacementGems()
        {
            _gridState = GridState.Replacing;
            _grid.SpawnNewGems();
            
            int minYPosition = _gridSize;
            
            foreach (var position in _grid.SpawnedGems)
            {
                if (position.y < minYPosition) minYPosition = position.y;
            }
            
            foreach (var position in _grid.SpawnedGems)
            {
                var gemType = _grid.GemIndexes[position.y][position.x];
                var gem = GetGem(gemType);
                var spawnPosition = GridToWorldPosition(position.x, position.y - minYPosition + _gridSize + _spawnPositionOffset);
                gem.Initialize(gemType, position, spawnPosition, _screenBottom, RecycleGem);
                gem.DropTo(position, spawnPosition, GridToWorldPosition(position.x, position.y));
                var index = GridPositionToIndex(position.x, position.y);
                _activeGems[index] = gem;
            }
        }
        
        private void RecycleGem(Gem gem)
        {
            gem.gameObject.SetActive(false);
            _inactiveGems[gem.TypeIndex].Push(gem);
            _animatingGems.Remove(gem);
        }
    }
}
