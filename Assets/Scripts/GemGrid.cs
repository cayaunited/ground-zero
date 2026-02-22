using System.Collections.Generic;
using UnityEngine;

namespace GroundZero
{
    public class GemGrid
    {
        public static readonly int MIN_MATCH_LENGTH = 3;
        public static readonly int MAX_MATCH_LENGTH = 5;
        
        public readonly int Size;
        public readonly List<List<int>> GemIndexes;
        public readonly List<Vector2Int> MatchedGems;
        public readonly Dictionary<Vector2Int, Vector2Int> DroppedGems;
        public readonly List<Vector2Int> SpawnedGems;
        public readonly Dictionary<Vector2Int, SpecialGemType> SpecialGemsCreated;
        public readonly Dictionary<Vector2Int, int> SpecialGemTypesCreated;
        public readonly Dictionary<Vector2Int, SpecialGemType> SpecialGems;
        public readonly List<Vector2Int> DestroyedGems;
        public readonly List<Vector2Int> GemsDestroyedByExplosions;
        
        private readonly Vector2Int[] _lastSwapPositions = new Vector2Int[2];
        private readonly int[] _lastSwapTypes = new int[2];
        private readonly List<Vector2Int> _potentialHorizontalMatch;
        private readonly List<Vector2Int> _potentialVerticalMatch;
        private readonly int _gemTypeCount;
        private readonly Dictionary<SpecialGemType, int> _specialGemCountByType = new();
        
        public GemGrid(int size, int typeCount)
        {
            Size = size;
            var maxGemCount = size * size;
            _gemTypeCount = typeCount;
            
            GemIndexes = new List<List<int>>(capacity: Size);
            
            for (int y = 0; y < Size; y++)
            {
                var row = new List<int>(capacity: Size);
                
                for (int x = 0; x < Size; x++)
                {
                    row.Add(-1);
                }
                
                GemIndexes.Add(row);
            }
            
            MatchedGems = new List<Vector2Int>(capacity: maxGemCount);
            DroppedGems = new Dictionary<Vector2Int, Vector2Int>(capacity: maxGemCount - Size);
            
            SpawnedGems = new List<Vector2Int>(capacity: maxGemCount);
            SpecialGemsCreated = new Dictionary<Vector2Int, SpecialGemType>(capacity: maxGemCount);
            SpecialGemTypesCreated = new Dictionary<Vector2Int, int>(capacity: maxGemCount);
            SpecialGems = new Dictionary<Vector2Int, SpecialGemType>(capacity: maxGemCount);
            DestroyedGems = new List<Vector2Int>(capacity: maxGemCount);
            GemsDestroyedByExplosions = new List<Vector2Int>(capacity: maxGemCount);
            
            _potentialHorizontalMatch = new List<Vector2Int>(capacity: MAX_MATCH_LENGTH);
            _potentialVerticalMatch = new List<Vector2Int>(capacity: MAX_MATCH_LENGTH);
        }
        
        public void FillGrid()
        {
            int matchedCount;
            bool canMatchesBeMade;
            
            do
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        GemIndexes[y][x] = Random.Range(0, _gemTypeCount);
                    }
                }
                
                matchedCount = FindMatches();
                canMatchesBeMade = AreTherePossibleMatches();
            } while (matchedCount > 0 || !canMatchesBeMade);
            
            _specialGemCountByType.Clear();
            
            foreach (var (_, type) in SpecialGems)
            {
                if (_specialGemCountByType.ContainsKey(type)) _specialGemCountByType[type]++;
                else _specialGemCountByType.Add(type, 1);
            }
            
            SpecialGems.Clear();
            
            foreach (var (type, count) in _specialGemCountByType)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2Int position;
                    
                    do
                    {
                        position = new Vector2Int(Random.Range(0, Size), Random.Range(0, Size));
                    } while (SpecialGems.ContainsKey(position));
                    
                    SpecialGems.Add(position, type);
                }
            }
        }
        
        public bool AreTherePossibleMatches()
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (x < Size - 1 && DoesSwapCreateMatch(new Vector2Int(x, y), new Vector2Int(x + 1, y))) return true;
                    if (y < Size - 1 && DoesSwapCreateMatch(new Vector2Int(x, y), new Vector2Int(x, y + 1))) return true;
                }
            }
            
            return false;
        }
        
        public bool SwapGems(Vector2Int position1, Vector2Int position2)
        {
            var type1 = GemIndexes[position1.y][position1.x];
            var type2 = GemIndexes[position2.y][position2.x];
            
            if (!DoesSwapCreateMatch(position1, position2)) return false;
            
            GemIndexes[position2.y][position2.x] = type1;
            GemIndexes[position1.y][position1.x] = type2;
            _lastSwapPositions[0] = position1;
            _lastSwapPositions[1] = position2;
            _lastSwapTypes[0] = type1;
            _lastSwapTypes[1] = type2;
            
            var isFirstSpecial = SpecialGems.ContainsKey(position1);
            var isSecondSpecial = SpecialGems.ContainsKey(position2);
            
            if (isFirstSpecial && isSecondSpecial)
            {
                var specialType1 = SpecialGems[position1];
                var specialType2 = SpecialGems[position2];
                SpecialGems[position2] = specialType1;
                SpecialGems[position1] = specialType2;
            }
            else if (isFirstSpecial)
            {
                SpecialGems.Add(position2, SpecialGems[position1]);
                SpecialGems.Remove(position1);
            }
            else if (isSecondSpecial)
            {
                SpecialGems.Add(position1, SpecialGems[position2]);
                SpecialGems.Remove(position2);
            }
            
            return true;
        }
        
        public int FindMatches(bool createSpecialGems = false, bool stopAfterFindingOne = false)
        {
            MatchedGems.Clear();
            SpecialGemsCreated.Clear();
            SpecialGemTypesCreated.Clear();
            
            if (createSpecialGems) {
                var swapPosition1 = _lastSwapPositions[0];
                var swapPosition2 = _lastSwapPositions[1];
                
                if (SpecialGems.ContainsKey(swapPosition1) && SpecialGems[swapPosition1] == SpecialGemType.Targeting
                    || SpecialGems.ContainsKey(swapPosition2) && SpecialGems[swapPosition2] == SpecialGemType.Targeting)
                {
                    if (!MatchedGems.Contains(swapPosition1)) MatchedGems.Add(swapPosition1);
                    if (!MatchedGems.Contains(swapPosition2)) MatchedGems.Add(swapPosition2);
                }
            }
            
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var position = new Vector2Int(x, y);
                    var horizontalMatchLength = FindMatchInDirection(position, Vector2Int.right);
                    var verticalMatchLength = FindMatchInDirection(position, Vector2Int.up);
                    
                    if (horizontalMatchLength < MIN_MATCH_LENGTH && verticalMatchLength < MIN_MATCH_LENGTH) continue;
                    
                    if (horizontalMatchLength >= MIN_MATCH_LENGTH)
                    {
                        foreach (var matchedPosition in _potentialHorizontalMatch)
                        {
                            if (!MatchedGems.Contains(matchedPosition)) MatchedGems.Add(matchedPosition);
                        }
                    }
                    
                    if (verticalMatchLength >= MIN_MATCH_LENGTH)
                    {
                        foreach (var matchedPosition in _potentialVerticalMatch)
                        {
                            if (!MatchedGems.Contains(matchedPosition)) MatchedGems.Add(matchedPosition);
                        }
                    }
                    
                    if (createSpecialGems && horizontalMatchLength > MIN_MATCH_LENGTH) CreateSpecialGem(_potentialHorizontalMatch);
                    if (createSpecialGems && verticalMatchLength > MIN_MATCH_LENGTH) CreateSpecialGem(_potentialVerticalMatch);
                    
                    if (stopAfterFindingOne) return MatchedGems.Count;
                }
            }
            
            return MatchedGems.Count;
        }
        
        public void DestroyMatches()
        {
            DestroyedGems.Clear();
            GemsDestroyedByExplosions.Clear();
            
            foreach (var position in MatchedGems)
            {
                DestroyGem(position, false);
            }
            
            MatchedGems.Clear();
            
            foreach (var (position, specialType) in SpecialGemsCreated)
            {
                if (SpecialGems.ContainsKey(position)) SpecialGems[position] = specialType;
                else SpecialGems.Add(position, specialType);
                GemIndexes[position.y][position.x] = SpecialGemTypesCreated[position];
            }
        }
        
        public void DropGems()
        {
            DroppedGems.Clear();
            
            for (int y = 1; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var gemTypeIndex = GemIndexes[y][x];
                    if (gemTypeIndex < 0) continue;
                    
                    var currentPosition = new Vector2Int(x, y);
                    var dropPosition = FindLowestDropPosition(currentPosition);
                    
                    if (dropPosition.y != currentPosition.y)
                    {
                        DroppedGems.Add(currentPosition, dropPosition);
                        GemIndexes[dropPosition.y][x] = gemTypeIndex;
                        GemIndexes[currentPosition.y][x] = -1;
                        
                        if (SpecialGems.ContainsKey(currentPosition))
                        {
                            SpecialGems.Add(dropPosition, SpecialGems[currentPosition]);
                            SpecialGems.Remove(currentPosition);
                        }
                    }
                }
            }
        }
        
        public void SpawnNewGems()
        {
            SpawnedGems.Clear();
            
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (GemIndexes[y][x] >= 0) continue;
                    GemIndexes[y][x] = Random.Range(0, _gemTypeCount);
                    SpawnedGems.Add(new Vector2Int(x, y));
                }
            }
        }
        
        public bool ArePositionsAdjacent(Vector2Int position1, Vector2Int position2)
        {
            var positionDifference = position1 - position2;
            return position1.x == position2.x && Mathf.Abs(positionDifference.y) == 1
                || position1.y == position2.y && Mathf.Abs(positionDifference.x) == 1;
        }
        
        private bool CanSwapGems(Vector2Int position1, Vector2Int position2)
        {
            var type1 = GemIndexes[position1.y][position1.x];
            var type2 = GemIndexes[position2.y][position2.x];
            if (type1 == type2 || position1 == position2) return false;
            
            return ArePositionsAdjacent(position1, position2);
        }
        
        private bool DoesSwapCreateMatch(Vector2Int position1, Vector2Int position2)
        {
            if (!CanSwapGems(position1, position2)) return false;
            
            if (SpecialGems.ContainsKey(position1) && SpecialGems[position1] == SpecialGemType.Targeting
                || SpecialGems.ContainsKey(position2) && SpecialGems[position2] == SpecialGemType.Targeting) return true;
            
            var type1 = GemIndexes[position1.y][position1.x];
            var type2 = GemIndexes[position2.y][position2.x];
            
            GemIndexes[position2.y][position2.x] = type1;
            GemIndexes[position1.y][position1.x] = type2;
            
            var wasMatchFound = FindMatches(stopAfterFindingOne: true) > 0;
            
            GemIndexes[position1.y][position1.x] = type1;
            GemIndexes[position2.y][position2.x] = type2;
            
            return wasMatchFound;
        }
        
        private int FindMatchInDirection(Vector2Int startingPoint, Vector2Int direction)
        {
            var potentialMatch = direction == Vector2Int.right ? _potentialHorizontalMatch : _potentialVerticalMatch;
            potentialMatch.Clear();
            potentialMatch.Add(startingPoint);
            
            var position = startingPoint;
            Vector2Int nextPosition;
            var gemTypeIndex = GemIndexes[position.y][position.x];
            int nextGemTypeIndex;
            var matchLength = 1;
            
            while (position.x + direction.x < Size && position.y + direction.y < Size && matchLength < MAX_MATCH_LENGTH)
            {
                nextPosition = position + direction;
                nextGemTypeIndex = GemIndexes[nextPosition.y][nextPosition.x];
                
                if (nextGemTypeIndex != gemTypeIndex) return matchLength;
                
                matchLength++;
                potentialMatch.Add(nextPosition);
                position = nextPosition;
            }
            
            return matchLength;
        }
        
        private void CreateSpecialGem(List<Vector2Int> match)
        {
            Vector2Int position;
            var matchLength = match.Count;
            
            if (match.Contains(_lastSwapPositions[0])) position = _lastSwapPositions[0];
            else if (match.Contains(_lastSwapPositions[1])) position = _lastSwapPositions[1];
            else position = match[Random.Range(0, matchLength)];
            
            var specialType = (SpecialGemType)matchLength;
            if (SpecialGemsCreated.ContainsKey(position)) return;
            SpecialGemsCreated.Add(position, specialType);
            SpecialGemTypesCreated.Add(position, GemIndexes[position.y][position.x]);
        }
        
        private Vector2Int FindLowestDropPosition(Vector2Int currentPosition)
        {
            for (int y = currentPosition.y - 1; y >= 0; y--)
            {
                if (GemIndexes[y][currentPosition.x] >= 0) return new Vector2Int(currentPosition.x, y + 1);
            }
            
            return new Vector2Int(currentPosition.x, 0);
        }
        
        private void DestroyGem(Vector2Int position, bool shouldExplode)
        {
            var gemType = GemIndexes[position.y][position.x];
            if (gemType < 0) return;
            
            GemIndexes[position.y][position.x] = -1;
            DestroyedGems.Add(position);
            if (shouldExplode) GemsDestroyedByExplosions.Add(position);
            
            var isSpecial = SpecialGems.ContainsKey(position);
            if (!isSpecial) return;
            var specialType = SpecialGems[position];
            SpecialGems.Remove(position);
            
            if (specialType == SpecialGemType.Explosive) DestroyExplosiveGem(position);
            else if (specialType == SpecialGemType.Targeting) DestroyTargetingGem(position, gemType);
        }
        
        private void DestroyExplosiveGem(Vector2Int position)
        {
            var isLeftExplodable = position.x > 0;
            var isRightExplodable = position.x < Size - 1;
            var isBelowExplodable = position.y > 0;
            var isAboveExplodable = position.y < Size - 1;
            
            if (isLeftExplodable) DestroyGem(new Vector2Int(position.x - 1, position.y), true);
            if (isRightExplodable) DestroyGem(new Vector2Int(position.x + 1, position.y), true);
            if (isBelowExplodable) DestroyGem(new Vector2Int(position.x, position.y - 1), true);
            if (isAboveExplodable) DestroyGem(new Vector2Int(position.x, position.y + 1), true);
            if (isLeftExplodable && isBelowExplodable) DestroyGem(new Vector2Int(position.x - 1, position.y - 1), true);
            if (isRightExplodable && isBelowExplodable) DestroyGem(new Vector2Int(position.x + 1, position.y - 1), true);
            if (isLeftExplodable && isAboveExplodable) DestroyGem(new Vector2Int(position.x - 1, position.y + 1), true);
            if (isRightExplodable && isAboveExplodable) DestroyGem(new Vector2Int(position.x + 1, position.y + 1), true);
        }
        
        private void DestroyTargetingGem(Vector2Int position, int type)
        {
            var isFirstSwappedGem = position == _lastSwapPositions[1];
            var isSecondSwappedGem = position == _lastSwapPositions[0];
            int typeToDestroy;
            
            if (isFirstSwappedGem || isSecondSwappedGem)
                typeToDestroy = isFirstSwappedGem ? _lastSwapTypes[1] : _lastSwapTypes[0];
            else typeToDestroy = type;
            
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if ((x != position.x || y != position.y) && GemIndexes[y][x] == typeToDestroy) {
                        DestroyGem(new Vector2Int(x, y), true);
                    }
                }
            }
        }
    }
}
