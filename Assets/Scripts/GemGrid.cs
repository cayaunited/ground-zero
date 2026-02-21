using System.Collections.Generic;
using UnityEngine;

namespace GroundZero
{
    /// <summary>
    /// Responsible for storing and manipulating the indexes tracking which gems are where.
    /// </summary>
    public class GemGrid
    {
        // Mark these constants as readonly and public static because
        // they should never change and should be accessible from anywhere.
        public static readonly int MIN_MATCH_LENGTH = 3;
        public static readonly int MAX_MATCH_LENGTH = 5;
        
        /// <summary>
        /// The horizontal and vertical size of the grid in units.
        /// Should not change so it's readonly, meaning it can only be set when making a GemGrid.
        /// </summary>
        public readonly int Size;
        /// <summary>
        /// The indexes of gem types in a list, where -1 means the spot is empty.
        /// 0 is the first gem type, 1 is the second, and so on.
        /// Does not need to be set to a new list, so it should be readonly.
        /// </summary>
        public readonly List<List<int>> GemIndexes;
        /// <summary>
        /// The positions of any gems that were matched with others after calling FindMatches.
        /// </summary>
        public readonly List<Vector2Int> MatchedGems;
        /// <summary>
        /// The initial and final positions of any dropped gems after calling DropGems.
        /// The key is the initial position, and the value is the final position.
        /// </summary>
        public readonly Dictionary<Vector2Int, Vector2Int> DroppedGems;
        /// <summary>
        /// The positions of any new gems that were spawned in after calling SpawnNewGems.
        /// </summary>
        public readonly List<Vector2Int> SpawnedGems;
        /// <summary>
        /// The positions and types of any special gems created after a match.
        /// The key is the position of the newly created special gem,
        /// and the value is the length of the match, which dictates the type.
        /// </summary>
        public readonly Dictionary<Vector2Int, SpecialGemType> SpecialGemsCreated;
        /// <summary>
        /// The positions and types of all special gems in the grid.
        /// </summary>
        public readonly Dictionary<Vector2Int, SpecialGemType> SpecialGems;
        /// <summary>
        /// The positions of any gems that were destroyed in after calling DestroyMatches.
        /// </summary>
        public readonly List<Vector2Int> DestroyedGems;
        
        /// <summary>
        /// The positions of the two gems that were swapped most recently.
        /// </summary>
        private readonly Vector2Int[] _lastSwapPositions = new Vector2Int[2];
        /// <summary>
        /// The types of the two gems that were swapped most recently.
        /// </summary>
        private readonly int[] _lastSwapTypes = new int[2];
        /// <summary>
        /// The positions in a row of matching gems to potentially be counted as matched.
        /// Only used in finding matches and can be private so other classes don't see it.
        /// </summary>
        private readonly List<Vector2Int> _potentialHorizontalMatch;
        /// <summary>
        /// The positions in a column of matching gems to potentially be counted as matched.
        /// </summary>
        private readonly List<Vector2Int> _potentialVerticalMatch;
        /// <summary>
        /// How many types of gems there are.
        /// Only used internally and doesn't change, so private and readonly.
        /// </summary>
        private readonly int _gemTypeCount;
        /// <summary>
        /// How many of each kind of special gems there are.
        /// </summary>
        private readonly Dictionary<SpecialGemType, int> _specialGemCountByType = new();
        
        /// <summary>
        /// Initializes the grid to be empty of gems.
        /// </summary>
        /// <param name="size">The horizontal and vertical size of the grid in units.</param>
        /// <param name="typeCount">How many types of gems there are.</param>
        public GemGrid(int size, int typeCount)
        {
            Size = size;
            var maxGemCount = size * size;
            _gemTypeCount = typeCount;
            
            // Create a list with a capacity equal to the number of rows in the grid.
            // Since there won't be any more items in the list than the number of rows,
            // set the capacity to help the list know how many items there will be ahead of time,
            // before we add rows to the list.
            GemIndexes = new List<List<int>>(capacity: Size);
            
            for (int y = 0; y < Size; y++)
            {
                // Create a list with a capacity equal to the number of columns in the grid.
                var row = new List<int>(capacity: Size);
                
                for (int x = 0; x < Size; x++)
                {
                    row.Add(-1);
                }
                
                GemIndexes.Add(row);
            }
            
            // Create a list for the position of matched gems with a capacity of
            // the number of rows times the number of columns,
            // since we know that at most all gems can be matched.
            MatchedGems = new List<Vector2Int>(capacity: maxGemCount);
            // The max number of gems that can drop is the whole grid, minus the bottom row.
            // We don't need to set a capacity for lists or dictionaries,
            // but it helps the computer if we already know the max size of the list or dictionary.
            DroppedGems = new Dictionary<Vector2Int, Vector2Int>(capacity: maxGemCount - Size);
            
            SpawnedGems = new List<Vector2Int>(capacity: maxGemCount);
            SpecialGemsCreated = new Dictionary<Vector2Int, SpecialGemType>(capacity: maxGemCount);
            SpecialGems = new Dictionary<Vector2Int, SpecialGemType>(capacity: maxGemCount);
            DestroyedGems = new List<Vector2Int>(capacity: maxGemCount);
            
            _potentialHorizontalMatch = new List<Vector2Int>(capacity: MAX_MATCH_LENGTH);
            _potentialVerticalMatch = new List<Vector2Int>(capacity: MAX_MATCH_LENGTH);
        }
        
        /// <summary>
        /// Randomly fills grid based on the number of gem types.
        /// Checks for matches, and refills if there are any.
        /// Checks for the potential to make a match by swapping, and refills if no swaps result in a match.
        /// Once done filling, creates the same number of the same kinds of
        /// special gems as before, in random spots.
        /// </summary>
        public void FillGrid()
        {
            // There's no need to set a value here, since we'll do that below before using the value.
            int matchedCount;
            bool canMatchesBeMade;
            
            // Randomly fill the grid first.
            do
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        // Fill each spot with a random index between 0 (inclusive)
                        // and the number of gem types (exclusive).
                        // For example, the value can be 0, 1, 2, 3, or 4 if the number of types is 5.
                        GemIndexes[y][x] = Random.Range(0, _gemTypeCount);
                    }
                }
                
                // Then, check for matches and possible matches, refilling the grid if needed,
                // and repeating the process until there are no matches to start with, but there are possible matches.
                matchedCount = FindMatches();
                canMatchesBeMade = AreTherePossibleMatches();
            } while (matchedCount > 0 || !canMatchesBeMade);
            
            _specialGemCountByType.Clear();
            
            // The key of the SpecialGems dictionary is the gem's position.
            // Since we don't need it, we can just use the discard character (underscore).
            foreach (var (_, type) in SpecialGems)
            {
                if (_specialGemCountByType.ContainsKey(type)) _specialGemCountByType[type]++;
                else _specialGemCountByType.Add(type, 1);
            }
            
            SpecialGems.Clear();
            
            // Spawn the right number of the right kinds of special gems,
            // to make sure special gems transfer over between grid refills.
            foreach (var (type, count) in _specialGemCountByType)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2Int position;
                    
                    // Find a position that doesn't have a special gem yet, then spawn a special gem there.
                    do
                    {
                        position = new Vector2Int(Random.Range(0, Size), Random.Range(0, Size));
                    } while (SpecialGems.ContainsKey(position));
                    
                    SpecialGems.Add(position, type);
                }
            }
        }
        
        /// <summary>
        /// Determines if any matches can be made by swapping any pair of gems together.
        /// </summary>
        /// <returns></returns>
        public bool AreTherePossibleMatches()
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // First check with a horizontal swap, then with a vertical.
                    // Make sure to not swap gems outside of the grid.
                    if (x < Size - 1 && DoesSwapCreateMatch(new Vector2Int(x, y), new Vector2Int(x + 1, y))) return true;
                    if (y < Size - 1 && DoesSwapCreateMatch(new Vector2Int(x, y), new Vector2Int(x, y + 1))) return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Swaps the gem indexes found in positions 1 and 2,
        /// if the gems at those positions have different types (indexes),
        /// and the two positions are adjacent.
        /// </summary>
        /// <param name="position1"></param>
        /// <param name="position2"></param>
        /// <returns>True if swapped successfully, false if not able to swap.</returns>
        public bool SwapGems(Vector2Int position1, Vector2Int position2)
        {
            // First, grab the indexes of each gem using their positions,
            // where y is the row number and x is the column number.
            var type1 = GemIndexes[position1.y][position1.x];
            var type2 = GemIndexes[position2.y][position2.x];
            
            if (!DoesSwapCreateMatch(position1, position2)) return false;
            
            GemIndexes[position2.y][position2.x] = type1;
            GemIndexes[position1.y][position1.x] = type2;
            _lastSwapPositions[0] = position1;
            _lastSwapPositions[1] = position2;
            _lastSwapTypes[0] = type1;
            _lastSwapTypes[1] = type2;
            
            // Make sure to update the positions of any special gems as they are swapped.
            var isFirstSpecial = SpecialGems.ContainsKey(position1);
            var isSecondSpecial = SpecialGems.ContainsKey(position2);
            
            // If both gems being swapped are special, swap them in the special gems dictionary.
            if (isFirstSpecial && isSecondSpecial)
            {
                var specialType1 = SpecialGems[position1];
                var specialType2 = SpecialGems[position2];
                SpecialGems[position2] = specialType1;
                SpecialGems[position1] = specialType2;
            }
            // Otherwise, move the type in the dictionary to the correct key.
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
        
        /// <summary>
        /// Finds the positions of gems that are in a row or column with other gems of the same type.
        /// Stores the results in MatchedGems.
        /// </summary>
        /// <param name="createSpecialGems">If true, create special gems while finding matches. If false, just find matches.</param>
        /// <param name="stopAfterFindingOne">If true, stop running after finding one match. If false, find all matches.</param>
        /// <returns>How many gems were matched.</returns>
        public int FindMatches(bool createSpecialGems = false, bool stopAfterFindingOne = false)
        {
            // First, clear out the list of matched gems to make sure
            // the final list only has gems that are currently matched.
            MatchedGems.Clear();
            SpecialGemsCreated.Clear();
            
            // Starting from the bottom left of the grid, look for horizontal and vertical matches.
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var position = new Vector2Int(x, y);
                    // Skip checking this position if it's already been matched.
                    if (MatchedGems.Contains(position)) continue;
                    var horizontalMatchLength = FindMatchInDirection(position, Vector2Int.right);
                    var verticalMatchLength = FindMatchInDirection(position, Vector2Int.up);
                    
                    // If the match length in either direction isn't long enough, move on to the next grid position.
                    if (horizontalMatchLength < MIN_MATCH_LENGTH && verticalMatchLength < MIN_MATCH_LENGTH) continue;
                    
                    // Pick the longest match, if any, to use, then add those gems to the matched list.
                    var match = horizontalMatchLength >= verticalMatchLength ? _potentialHorizontalMatch : _potentialVerticalMatch;
                    MatchedGems.AddRange(match);
                    
                    // If we should create special gems, and the match length is long enough,
                    // then create the special gem for this match.
                    if (createSpecialGems && match.Count > MIN_MATCH_LENGTH) CreateSpecialGem(match);
                    
                    // If we only want to check that a match is possible,
                    // then exit the function after finding one match.
                    if (stopAfterFindingOne) return MatchedGems.Count;
                }
            }
            
            // Make sure to count swapped gems as matched if at least one was a targeting gem,
            // and we are creating special gems / handling them.
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
            
            // Return how many gems are currently in the list,
            // which is different from the capacity (the max possible number of gems in the list).
            return MatchedGems.Count;
        }
        
        /// <summary>
        /// Destroys any matched gems from the list, replacing their indexes with -1.
        /// Does not remove any special gems that were created.
        /// Activates any special gems in the match, potentially causing a chain reaction.
        /// </summary>
        public void DestroyMatches()
        {
            DestroyedGems.Clear();
            
            foreach (var position in MatchedGems)
            {
                if (SpecialGemsCreated.ContainsKey(position)) continue;
                DestroyGem(position);
            }
            
            MatchedGems.Clear();
        }
        
        /// <summary>
        /// Drops any gems that have empty space below them as far as they can fall.
        /// Stores the resulting changes in DroppedGems.
        /// </summary>
        public void DropGems()
        {
            DroppedGems.Clear();
            
            // Start looking from one above the bottom, since the bottom can't fall,
            // and we want to first drop the bottom-most gems.
            for (int y = 1; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var gemTypeIndex = GemIndexes[y][x];
                    // Don't try to drop gems that don't exist.
                    if (gemTypeIndex < 0) continue;
                    
                    var currentPosition = new Vector2Int(x, y);
                    var dropPosition = FindLowestDropPosition(currentPosition);
                    
                    // If the gem dropped, make sure to add the change to the list,
                    // and actually make the change in the grid, clearing the current position.
                    if (dropPosition.y != currentPosition.y)
                    {
                        DroppedGems.Add(currentPosition, dropPosition);
                        GemIndexes[dropPosition.y][x] = gemTypeIndex;
                        GemIndexes[currentPosition.y][x] = -1;
                        
                        // Also make sure to update SpecialGems if needed.
                        if (SpecialGems.ContainsKey(currentPosition))
                        {
                            SpecialGems.Add(dropPosition, SpecialGems[currentPosition]);
                            SpecialGems.Remove(currentPosition);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Spawns new gems to fill in any empty spots.
        /// Stores the results in SpawnedGems.
        /// </summary>
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
        
        /// <summary>
        /// Returns if the gems found in positions 1 and 2 can be swapped.
        /// True if the gems at those positions have different types (indexes),
        /// and the two positions are adjacent.
        /// </summary>
        /// <param name="position1"></param>
        /// <param name="position2"></param>
        /// <returns></returns>
        private bool CanSwapGems(Vector2Int position1, Vector2Int position2)
        {
            var type1 = GemIndexes[position1.y][position1.x];
            var type2 = GemIndexes[position2.y][position2.x];
            if (type1 == type2 || position1 == position2) return false;
            
            // Make sure the positions are right next to each other.
            var positionDifference = position1 - position2;
            return position1.x == position2.x && Mathf.Abs(positionDifference.y) == 1
                || position1.y == position2.y && Mathf.Abs(positionDifference.x) == 1;
        }
        
        /// <summary>
        /// Returns if the gems found in positions 1 and 2 can be swapped,
        /// and result in a match.
        /// </summary>
        /// <param name="position1"></param>
        /// <param name="position2"></param>
        /// <returns></returns>
        private bool DoesSwapCreateMatch(Vector2Int position1, Vector2Int position2)
        {
            if (!CanSwapGems(position1, position2)) return false;
            
            // If one of the gems is targeting, then it's possible.
            if (SpecialGems.ContainsKey(position1) && SpecialGems[position1] == SpecialGemType.Targeting
                || SpecialGems.ContainsKey(position2) && SpecialGems[position2] == SpecialGemType.Targeting) return true;
            
            // If it's possible to swap the gems, swap them.
            var type1 = GemIndexes[position1.y][position1.x];
            var type2 = GemIndexes[position2.y][position2.x];
            
            GemIndexes[position2.y][position2.x] = type1;
            GemIndexes[position1.y][position1.x] = type2;
            
            // Check for matches, then make sure to unswap the gems.
            var wasMatchFound = FindMatches(stopAfterFindingOne: true) > 0;
            
            GemIndexes[position1.y][position1.x] = type1;
            GemIndexes[position2.y][position2.x] = type2;
            
            // A match was found, therefore there are possible matches.
            return wasMatchFound;
        }
        
        /// <summary>
        /// Looks to see if there is a match starting from a certain point and in a certain direction.
        /// Stores the results in a potential match list to pick the longest between horizontal and vertical matches.
        /// </summary>
        /// <param name="startingPoint">Where to start looking for a match from.</param>
        /// <param name="direction">Either to the right or up.</param>
        /// <returns>How many gems are in the match, or zero if there is no match.</returns>
        private int FindMatchInDirection(Vector2Int startingPoint, Vector2Int direction)
        {
            // Determine which list to store the potentially matched positions in.
            var potentialMatch = direction == Vector2Int.right ? _potentialHorizontalMatch : _potentialVerticalMatch;
            potentialMatch.Clear();
            // Make sure the first position in the list is the starting position.
            potentialMatch.Add(startingPoint);
            
            var position = startingPoint;
            // There's no need to assign nextPosition yet since we'll do it in the while loop below.
            // Also make sure to set a type for nextPosition. We can use var to infer the type,
            // but if we aren't giving it a value, it can't infer the type.
            Vector2Int nextPosition;
            var gemTypeIndex = GemIndexes[position.y][position.x];
            int nextGemTypeIndex;
            var matchLength = 1;
            
            // Keep looking for a match so long as the edge of the grid isn't reached by the next position
            // and the max match length isn't reached.
            while (position.x + direction.x < Size && position.y + direction.y < Size && matchLength < MAX_MATCH_LENGTH)
            {
                nextPosition = position + direction;
                nextGemTypeIndex = GemIndexes[nextPosition.y][nextPosition.x];
                
                // If the next gem in the given direction is of a different type,
                // then exit because this match search is done.
                // Also, if the next gem has already been matched, then stop.
                if (nextGemTypeIndex != gemTypeIndex || MatchedGems.Contains(nextPosition)) return matchLength;
                
                matchLength++;
                potentialMatch.Add(nextPosition);
                // Make sure to set the current position to the next position so the search loop can move forward.
                position = nextPosition;
            }
            
            return matchLength;
        }
        
        /// <summary>
        /// Creates a special gem at the swapped position in a match,
        /// or a random position if the match was created at a position outside of the swap.
        /// </summary>
        /// <param name="match">The positions of gems that were matched together.</param>
        private void CreateSpecialGem(List<Vector2Int> match)
        {
            Vector2Int position;
            var matchLength = match.Count;
            
            if (match.Contains(_lastSwapPositions[0])) position = _lastSwapPositions[0];
            else if (match.Contains(_lastSwapPositions[1])) position = _lastSwapPositions[1];
            else position = match[Random.Range(0, matchLength)];
            
            // Cast the match length to the enum SpecialGemType, because the gem type enum is dicated by the match length.
            var specialType = (SpecialGemType)matchLength;
            SpecialGemsCreated.Add(position, specialType);
            if (SpecialGems.ContainsKey(position)) SpecialGems[position] = specialType;
            else SpecialGems.Add(position, specialType);
        }
        
        /// <summary>
        /// Returns the lowest position that the gem at the given position can drop to.
        /// </summary>
        /// <param name="currentPosition">The gem's current position.</param>
        /// <returns>The position after dropping.</returns>
        private Vector2Int FindLowestDropPosition(Vector2Int currentPosition)
        {
            // Start searching from one below the current position,
            // since we know there's a gem at the current position.
            for (int y = currentPosition.y - 1; y >= 0; y--)
            {
                // If the new position is taken, return the previous position in the loop.
                if (GemIndexes[y][currentPosition.x] >= 0) return new Vector2Int(currentPosition.x, y + 1);
            }
            
            // The loop made it through without finding any gems below,
            // so drop the gem to the bottom.
            return new Vector2Int(currentPosition.x, 0);
        }
        
        /// <summary>
        /// Destroys the gem at the given position, activating any special effects as need be.
        /// </summary>
        /// <param name="position"></param>
        private void DestroyGem(Vector2Int position)
        {
            // Make sure any newly created special gems aren't destroyed.
            // This call is unnecessary for when DestroyGem is called in DestroyMatches,
            // but it's needed for when DestroyGem calls itself.
            // Also, prevent the same gem from being destroyed multiple times.
            var gemType = GemIndexes[position.y][position.x];
            if (gemType < 0 || SpecialGemsCreated.ContainsKey(position)) return;
            
            GemIndexes[position.y][position.x] = -1;
            DestroyedGems.Add(position);
            
            var isSpecial = SpecialGems.ContainsKey(position);
            if (!isSpecial) return;
            var specialType = SpecialGems[position];
            SpecialGems.Remove(position);
            
            if (specialType == SpecialGemType.Explosive) DestroyExplosiveGem(position);
            else if (specialType == SpecialGemType.Targeting) DestroyTargetingGem(position, gemType);
        }
        
        /// <summary>
        /// Destroys the gems surrounding the explosive gem at the given position.
        /// </summary>
        /// <param name="position"></param>
        private void DestroyExplosiveGem(Vector2Int position)
        {
            // Don't try exploding gems in invalid positions.
            var isLeftExplodable = position.x > 0;
            var isRightExplodable = position.x < Size - 1;
            var isBelowExplodable = position.y > 0;
            var isAboveExplodable = position.y < Size - 1;
            
            // Destroy each gem around this one, only destroying gems at positions that are actually in the grid.
            if (isLeftExplodable) DestroyGem(new Vector2Int(position.x - 1, position.y));
            if (isRightExplodable) DestroyGem(new Vector2Int(position.x + 1, position.y));
            if (isBelowExplodable) DestroyGem(new Vector2Int(position.x, position.y - 1));
            if (isAboveExplodable) DestroyGem(new Vector2Int(position.x, position.y + 1));
            if (isLeftExplodable && isBelowExplodable) DestroyGem(new Vector2Int(position.x - 1, position.y - 1));
            if (isRightExplodable && isBelowExplodable) DestroyGem(new Vector2Int(position.x + 1, position.y - 1));
            if (isLeftExplodable && isAboveExplodable) DestroyGem(new Vector2Int(position.x - 1, position.y + 1));
            if (isRightExplodable && isAboveExplodable) DestroyGem(new Vector2Int(position.x + 1, position.y + 1));
        }
        
        /// <summary>
        /// If being destroyed by swapping gems, destroys all gems of the other gem's type.
        /// If being destroyed by a special gem, destroys all gems of the same type as the gem at the given position.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="type">The type of the gem at this position.</param>
        private void DestroyTargetingGem(Vector2Int position, int type)
        {
            // This looks weird, but remember the gems were swapped,
            // so the other gem is in this gem's original position,
            // meaning the other gem's type is the type of the gem at this gem's new position.
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
                    // Make the gem doesn't destroy itself.
                    // If either the x or the y position is different, then it's not destroying itself.
                    if ((x != position.x || y != position.y) && GemIndexes[y][x] == typeToDestroy) {
                        DestroyGem(new Vector2Int(x, y));
                    }
                }
            }
        }
    }
}
