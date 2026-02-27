using System.Collections.Generic;
using UnityEngine;

namespace GroundZero
{
    public class GemGrid
    {
        // The minimum and maximum match lengths should never change,
        // and we don't want "magic numbers" floating around the code,
        // so we can create static readonly integers that tell us the min / max lengths.
        // We mark them as static because they are the same for any GemGrid instance.
        private static readonly int MIN_MATCH_LENGTH = 3;
        private static readonly int MAX_MATCH_LENGTH = 5;
        
        // After the GemGrid is created, we won't need to change any of these fields,
        // so we can mark them all as readonly. The contents of the lists and dictionaries will change,
        // but we won't replace the lists or dictionaries.
        public readonly int Size;
        /// <summary>
        /// The main data for the GemGrid, which stores the location and type of each gem in the grid.
        /// In this case, type refers to the color and shape of the gem.
        /// Also, an index of -1 means that position in the grid is currently empty.
        /// </summary>
        public readonly List<List<int>> GemIndexes;
        /// <summary>
        /// The positions of any gems that were just found to be in a match.
        /// Stores the results of FindMatches.
        /// </summary>
        public readonly List<Vector2Int> MatchedGems;
        /// <summary>
        /// The initial and final positions of any gems that just fell / dropped to a lower position.
        /// The key is the initial position, and the value is the final position.
        /// Stores the results of DropGems.
        /// </summary>
        public readonly Dictionary<Vector2Int, Vector2Int> DroppedGems;
        /// <summary>
        /// The positions of any gems that were just spawned in from the top of the grid.
        /// Stores the results of SpawnNewGems.
        /// </summary>
        public readonly List<Vector2Int> SpawnedGems;
        /// <summary>
        /// The positions and special types of any special gems that were just created by a match.
        /// The key is the position, and the value is the special type (either explosive or targeting).
        /// </summary>
        public readonly Dictionary<Vector2Int, SpecialGemType> SpecialGemsCreated;
        /// <summary>
        /// The positions and gem types of any special gems that were just created by a match.
        /// The key is the position, and the value is the gem type (same indexes used for color and shape).
        /// </summary>
        public readonly Dictionary<Vector2Int, int> SpecialGemTypesCreated;
        /// <summary>
        /// The positions and special types of any special gems that are currently in the grid.
        /// The key is the position, and the value is the special type (either explosive or targeting).
        /// </summary>
        public readonly Dictionary<Vector2Int, SpecialGemType> SpecialGems;
        /// <summary>
        /// The positions of any gems that were just destroyed and therefore removed from the grid.
        /// Stores the results of DestroyMatches.
        /// </summary>
        public readonly List<Vector2Int> DestroyedGems;
        /// <summary>
        /// The positions of any gems that were just destroyed by an explosion,
        /// meaning a special explosion animation should play.
        /// </summary>
        public readonly List<Vector2Int> GemsDestroyedByExplosions;
        
        /// <summary>
        /// The positions of the two gems that were most recently swapped.
        /// </summary>
        private readonly Vector2Int[] _lastSwapPositions = new Vector2Int[2];
        /// <summary>
        /// The gem types of the two gems that were most recently swapped.
        /// </summary>
        private readonly int[] _lastSwapTypes = new int[2];
        /// <summary>
        /// The list of positions in a potential horizontal match when finding matches.
        /// </summary>
        private readonly List<Vector2Int> _potentialHorizontalMatch;
        /// <summary>
        /// The list of positions in a potential vertical match when finding matches.
        /// </summary>
        private readonly List<Vector2Int> _potentialVerticalMatch;
        /// <summary>
        /// How many types of gems there are, meaning color and shape combinations.
        /// </summary>
        private readonly int _gemTypeCount;
        /// <summary>
        /// The total number of each type of special gem currently in the grid.
        /// Ensures the same number of each is spawned in if the grid regenerates.
        /// </summary>
        private readonly Dictionary<SpecialGemType, int> _specialGemCountByType = new();
        /// <summary>
        /// Whether or not this round of destruction is the first,
        /// to prevent accidentally destroying newly created targeting gems after they fall
        /// just because they were in a position that was swapped.
        /// </summary>
        private bool _isFirstDestructionRound = true;
        
        public GemGrid(int size, int typeCount)
        {
            // Track the maximum number of possible gems in the grid at once,
            // and how many types of gems there could be in it.
            Size = size;
            var maxGemCount = size * size;
            _gemTypeCount = typeCount;
            
            // Create all the needed lists and dictionaries.
            // We know how long each of them will be at most,
            // so we'll set the capacity when making the list.
            // Capacity is how long the list will be at most,
            // whereas count is how long the list currently is.
            // If we don't know the capacity ahead of time,
            // we can just ignore it and let the list increase the capacity
            // if needed as we add more items.
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
        
        /// <summary>
        /// Randomly fills up the grid and carries over any
        /// remaining special gems from last time, if it should do so.
        /// </summary>
        /// <param name="shouldCarrySpecialGems"></param>
        public void FillGrid(bool shouldCarrySpecialGems = true)
        {
            int matchedCount;
            bool canMatchesBeMade;
            
            // Randomly fill up the grid with different types of gems
            // until there are no matches currently but at least one can be made.
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
            
            // If we aren't carrying over the list of special gems,
            // then clear it and skip carrying over gems.
            if (!shouldCarrySpecialGems)
            {
                SpecialGems.Clear();
                return;
            }
            
            // Count how many of each type of special gem is in the grid.
            foreach (var (_, type) in SpecialGems)
            {
                if (_specialGemCountByType.ContainsKey(type)) _specialGemCountByType[type]++;
                else _specialGemCountByType.Add(type, 1);
            }
            
            // Now that we are done looping through the list of special gems, we can clear it.
            SpecialGems.Clear();
            
            // Create the correct number of each type of special gem
            // in random positions that don't yet have a special gem.
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
        
        /// <summary>
        /// Returns if swapping any two gems in the grid will result in a match.
        /// </summary>
        /// <returns></returns>
        public bool AreTherePossibleMatches()
        {
            // Starting from the bottom left and see if the gem can be swapped with
            // the gem to its right or above it. If so, then there's a possible match.
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // Once we get to the last gem in the row, we can't swap it with the next gem in the row.
                    // Same for columns, so only swap if it's the second to last item in the row / column.
                    if (x < Size - 1 && DoesSwapCreateMatch(new Vector2Int(x, y), new Vector2Int(x + 1, y))) return true;
                    if (y < Size - 1 && DoesSwapCreateMatch(new Vector2Int(x, y), new Vector2Int(x, y + 1))) return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// If possible, swaps the gems at the given positions and returns true.
        /// </summary>
        /// <param name="position1"></param>
        /// <param name="position2"></param>
        /// <returns></returns>
        public bool SwapGems(Vector2Int position1, Vector2Int position2)
        {
            if (!DoesSwapCreateMatch(position1, position2)) return false;
            
            // Once we know a swap will create a match, actually swap the gems.
            var type1 = GemIndexes[position1.y][position1.x];
            var type2 = GemIndexes[position2.y][position2.x];
            GemIndexes[position2.y][position2.x] = type1;
            GemIndexes[position1.y][position1.x] = type2;
            
            // Then, make sure to track what gems were swapped.
            _lastSwapPositions[0] = position1;
            _lastSwapPositions[1] = position2;
            _lastSwapTypes[0] = type1;
            _lastSwapTypes[1] = type2;
            
            // Make sure to update the dictionary tracking what kind of special gems are where.
            var isFirstSpecial = SpecialGems.ContainsKey(position1);
            var isSecondSpecial = SpecialGems.ContainsKey(position2);
            
            if (isFirstSpecial && isSecondSpecial)
            {
                // Since both gems are special, we can just swap them in the dictionary.
                var specialType1 = SpecialGems[position1];
                var specialType2 = SpecialGems[position2];
                SpecialGems[position2] = specialType1;
                SpecialGems[position1] = specialType2;
            }
            else if (isFirstSpecial)
            {
                // Since only one gem is special, make sure the dictionary tracks the new location and removes the previous.
                SpecialGems.Add(position2, SpecialGems[position1]);
                SpecialGems.Remove(position1);
            }
            else if (isSecondSpecial)
            {
                SpecialGems.Add(position1, SpecialGems[position2]);
                SpecialGems.Remove(position2);
            }
            
            _isFirstDestructionRound = true;
            return true;
        }
        
        /// <summary>
        /// Finds any matches in the grid, from the min to the max length.
        /// Returns how many gems were found in a match, not the number of matches.
        /// </summary>
        /// <param name="createSpecialGems">True if special gems should be created and taken into account.</param>
        /// <param name="stopAfterFindingOne">True if we only need to know that a match is possible, not how many matches.</param>
        /// <returns></returns>
        public int FindMatches(bool createSpecialGems = false, bool stopAfterFindingOne = false)
        {
            MatchedGems.Clear();
            SpecialGemsCreated.Clear();
            SpecialGemTypesCreated.Clear();
            
            // If we are handling special gems and at least one of the swapped gems is a targeting gem,
            // mark both the swapped gems as having been matched, because the targeting gem should destroy them both.
            // Also, make sure that we ignore this logic if this search for matches occurs before
            // all gems are done falling but after the first few are matched.
            if (createSpecialGems && _isFirstDestructionRound)
            {
                var swapPosition1 = _lastSwapPositions[0];
                var swapPosition2 = _lastSwapPositions[1];
                
                if (SpecialGems.ContainsKey(swapPosition1) && SpecialGems[swapPosition1] == SpecialGemType.Targeting
                    || SpecialGems.ContainsKey(swapPosition2) && SpecialGems[swapPosition2] == SpecialGemType.Targeting)
                {
                    if (!MatchedGems.Contains(swapPosition1)) MatchedGems.Add(swapPosition1);
                    if (!MatchedGems.Contains(swapPosition2)) MatchedGems.Add(swapPosition2);
                }
            }
            
            // For each gem in the grid, determine if it's in a horizontal or vertical match,
            // and create a special gem if the match is long enough.
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // First, look for matches starting from this gem and moving to the right.
                    // Then, look for matches starting from this gem and moving up.
                    var position = new Vector2Int(x, y);
                    var horizontalMatchLength = FindMatchInDirection(position, Vector2Int.right);
                    var verticalMatchLength = FindMatchInDirection(position, Vector2Int.up);
                    
                    // If the length of both potential matches is too low, continue with checking the next gem.
                    if (horizontalMatchLength < MIN_MATCH_LENGTH && verticalMatchLength < MIN_MATCH_LENGTH) continue;
                    
                    // For each gem in the horizontal and / or vertical matches, add the position to the list of matched gems.
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
                    
                    // If any match length is long enough, try creating a special gem out of it.
                    if (createSpecialGems && horizontalMatchLength > MIN_MATCH_LENGTH) CreateSpecialGem(_potentialHorizontalMatch);
                    if (createSpecialGems && verticalMatchLength > MIN_MATCH_LENGTH) CreateSpecialGem(_potentialVerticalMatch);
                    
                    if (stopAfterFindingOne) return MatchedGems.Count;
                }
            }
            
            return MatchedGems.Count;
        }
        
        /// <summary>
        /// Destroys and matched gems, triggers their special effects to destroy more gems,
        /// and then creates any new special gems that came as a result of the matches.
        /// </summary>
        public void DestroyMatches()
        {
            DestroyedGems.Clear();
            GemsDestroyedByExplosions.Clear();
            
            // Destroy any matched gems, marking them as destroyed by a match and not an explosion.
            foreach (var position in MatchedGems)
            {
                DestroyGem(position, shouldExplode: false);
            }
            
            MatchedGems.Clear();
            
            // Update the special gems list to include newly created gems,
            // and update the grid to include them as well.
            foreach (var (position, specialType) in SpecialGemsCreated)
            {
                if (SpecialGems.ContainsKey(position)) SpecialGems[position] = specialType;
                else SpecialGems.Add(position, specialType);
                GemIndexes[position.y][position.x] = SpecialGemTypesCreated[position];
            }
            
            _isFirstDestructionRound = false;
        }
        
        /// <summary>
        /// Drops any gems that have empty space below them,
        /// dropping them as far as possible.
        /// </summary>
        public void DropGems()
        {
            DroppedGems.Clear();
            
            // Start looking for gems to drop from one above the bottom row,
            // since the bottom row can drop any further.
            for (int y = 1; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var gemTypeIndex = GemIndexes[y][x];
                    // If this grid spot is empty, there's nothing to drop, so continue on to the next spot.
                    if (gemTypeIndex < 0) continue;
                    
                    // For each gem, we want to find the lowest possible position to drop to.
                    var currentPosition = new Vector2Int(x, y);
                    var dropPosition = FindLowestDropPosition(currentPosition);
                    
                    // If the gem can drop, then update the grid and list of special gems for the gem's new position.
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
        
        /// <summary>
        /// Spawns in new gems to fill in any empty spots.
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
        /// Returns if two positions are right next to each other,
        /// in the same column or row with no space between them.
        /// </summary>
        /// <param name="position1"></param>
        /// <param name="position2"></param>
        /// <returns></returns>
        public bool ArePositionsAdjacent(Vector2Int position1, Vector2Int position2)
        {
            var positionDifference = position1 - position2;
            return position1.x == position2.x && Mathf.Abs(positionDifference.y) == 1
                || position1.y == position2.y && Mathf.Abs(positionDifference.x) == 1;
        }
        
        /// <summary>
        /// Returns if two gems can be swapped because they are different types of gems
        /// in different, but adjacent positions.
        /// </summary>
        /// <param name="position1"></param>
        /// <param name="position2"></param>
        /// <returns></returns>
        private bool CanSwapGems(Vector2Int position1, Vector2Int position2)
        {
            var type1 = GemIndexes[position1.y][position1.x];
            var type2 = GemIndexes[position2.y][position2.x];
            if (type1 == type2 || position1 == position2) return false;
            
            return ArePositionsAdjacent(position1, position2);
        }
        
        /// <summary>
        /// Returns if a match would be made by swapping the gems at the two given positions.
        /// </summary>
        /// <param name="position1"></param>
        /// <param name="position2"></param>
        /// <returns></returns>
        private bool DoesSwapCreateMatch(Vector2Int position1, Vector2Int position2)
        {
            if (!CanSwapGems(position1, position2)) return false;
            
            // A match isn't technically made by swapping a targeting gem with another gem,
            // but we still want to count it as such so they are destroyed.
            if (SpecialGems.ContainsKey(position1) && SpecialGems[position1] == SpecialGemType.Targeting
                || SpecialGems.ContainsKey(position2) && SpecialGems[position2] == SpecialGemType.Targeting) return true;
            
            // First, swap the gems.
            var type1 = GemIndexes[position1.y][position1.x];
            var type2 = GemIndexes[position2.y][position2.x];
            GemIndexes[position2.y][position2.x] = type1;
            GemIndexes[position1.y][position1.x] = type2;
            
            // Then, determine if there are any matches, making sure to only check for one.
            var wasMatchFound = FindMatches(stopAfterFindingOne: true) > 0;
            
            // Lastly, make sure to undo the swap since this method should just check if a match is possible,
            // rather than actually perform the swap.
            GemIndexes[position1.y][position1.x] = type1;
            GemIndexes[position2.y][position2.x] = type2;
            
            return wasMatchFound;
        }
        
        /// <summary>
        /// Looking from the starting point and in the given direction,
        /// determines if there is a row or column of at least three of the same type of gem (color and shape).
        /// </summary>
        /// <param name="startingPoint"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        private int FindMatchInDirection(Vector2Int startingPoint, Vector2Int direction)
        {
            // First, determine which list to use, since this data is being used in another method (FindMatches).
            var potentialMatch = direction == Vector2Int.right ? _potentialHorizontalMatch : _potentialVerticalMatch;
            potentialMatch.Clear();
            potentialMatch.Add(startingPoint);
            
            var position = startingPoint;
            var gemTypeIndex = GemIndexes[position.y][position.x];
            int nextGemTypeIndex;
            var matchLength = 1;
            
            // While we are still inside of the grid and the match length hasn't passed the max,
            // check if the gem at the next position in the row / column is the same type.
            while (position.x + direction.x < Size && position.y + direction.y < Size && matchLength < MAX_MATCH_LENGTH)
            {
                position += direction;
                nextGemTypeIndex = GemIndexes[position.y][position.x];
                
                // If it's not the same type, then stop searching.
                if (nextGemTypeIndex != gemTypeIndex) return matchLength;
                
                // Otherwise, track that position and continue on to the next one.
                matchLength++;
                potentialMatch.Add(position);
            }
            
            return matchLength;
        }
        
        /// <summary>
        /// Tries to create a special gem from the given match,
        /// based on its length and the positions of the swapped gems.
        /// </summary>
        /// <param name="match"></param>
        private void CreateSpecialGem(List<Vector2Int> match)
        {
            Vector2Int position;
            var matchLength = match.Count;
            
            // If one of the gems that was swapped is a part of this match,
            // then use that gem's position to create the new special gem.
            if (match.Contains(_lastSwapPositions[0])) position = _lastSwapPositions[0];
            else if (match.Contains(_lastSwapPositions[1])) position = _lastSwapPositions[1];
            // Otherwise, pick a random gem in the match to replace with a special gem.
            else position = match[Random.Range(0, matchLength)];
            
            // Use the length of the match to determine what kind of special gem will be made.
            // Cast the match length variable from an integer to the SpecialGemType enum (which just gives names for numbers).
            var specialType = (SpecialGemType)matchLength;
            // Make sure to not overwrite anything just made,
            // then record the location and gem type (color / shape) of the special gem to create.
            if (SpecialGemsCreated.ContainsKey(position)) return;
            SpecialGemsCreated.Add(position, specialType);
            SpecialGemTypesCreated.Add(position, GemIndexes[position.y][position.x]);
        }
        
        /// <summary>
        /// Finds the lowest possible position for a gem to drop to,
        /// keeping the column the same but changing the row.
        /// </summary>
        /// <param name="currentPosition"></param>
        /// <returns></returns>
        private Vector2Int FindLowestDropPosition(Vector2Int currentPosition)
        {
            // Start checking from one position below the current,
            // since we already know the current position is occupied and not a space to drop to.
            for (int y = currentPosition.y - 1; y >= 0; y--)
            {
                // If we do find a gem, then return the position right above that gem.
                if (GemIndexes[y][currentPosition.x] >= 0) return new Vector2Int(currentPosition.x, y + 1);
            }
            
            // Otherwise, we've hit the bottom.
            return new Vector2Int(currentPosition.x, 0);
        }
        
        /// <summary>
        /// Destroys the gem at the given position, marking it for explosion as need be.
        /// Triggers any special gem effects as well, leading to further DestroyGem calls.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="shouldExplode"></param>
        private void DestroyGem(Vector2Int position, bool shouldExplode)
        {
            // Don't destroy non-existent or already destroyed gems.
            var gemType = GemIndexes[position.y][position.x];
            if (gemType < 0) return;
            
            GemIndexes[position.y][position.x] = -1;
            DestroyedGems.Add(position);
            if (shouldExplode) GemsDestroyedByExplosions.Add(position);
            
            // Make sure to update the list of special gems as needed.
            var isSpecial = SpecialGems.ContainsKey(position);
            if (!isSpecial) return;
            var specialType = SpecialGems[position];
            SpecialGems.Remove(position);
            
            if (specialType == SpecialGemType.Explosive) DestroyExplosiveGem(position);
            else if (specialType == SpecialGemType.Targeting) DestroyTargetingGem(position, gemType);
        }
        
        /// <summary>
        /// Destroys all gems next to the gem at the given position.
        /// That includes all gems directly touching this one,
        /// whether adjacent or diagonal to it.
        /// </summary>
        /// <param name="position"></param>
        private void DestroyExplosiveGem(Vector2Int position)
        {
            // Don't try to explode gems at positions outside of the grid.
            var isLeftExplodable = position.x > 0;
            var isRightExplodable = position.x < Size - 1;
            var isBelowExplodable = position.y > 0;
            var isAboveExplodable = position.y < Size - 1;
            
            if (isLeftExplodable) DestroyGem(new Vector2Int(position.x - 1, position.y), shouldExplode: true);
            if (isRightExplodable) DestroyGem(new Vector2Int(position.x + 1, position.y), shouldExplode: true);
            if (isBelowExplodable) DestroyGem(new Vector2Int(position.x, position.y - 1), shouldExplode: true);
            if (isAboveExplodable) DestroyGem(new Vector2Int(position.x, position.y + 1), shouldExplode: true);
            if (isLeftExplodable && isBelowExplodable) DestroyGem(new Vector2Int(position.x - 1, position.y - 1), shouldExplode: true);
            if (isRightExplodable && isBelowExplodable) DestroyGem(new Vector2Int(position.x + 1, position.y - 1), shouldExplode: true);
            if (isLeftExplodable && isAboveExplodable) DestroyGem(new Vector2Int(position.x - 1, position.y + 1), shouldExplode: true);
            if (isRightExplodable && isAboveExplodable) DestroyGem(new Vector2Int(position.x + 1, position.y + 1), shouldExplode: true);
        }
        
        /// <summary>
        /// Destroys all gems of one type. If this gem is one of the swapped gems,
        /// destroy all gems of the other swapped gem's type.
        /// Otherwise, destroy all gems of the targeting gem's type.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="type">The targeting gem's type.</param>
        private void DestroyTargetingGem(Vector2Int position, int type)
        {
            // Since the gems were swapped, the gem's current position
            // will be the original position of the other swapped gem.
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
                    // Destroy all gems other than this one that are of the targeted type.
                    if ((x != position.x || y != position.y) && GemIndexes[y][x] == typeToDestroy)
                        DestroyGem(new Vector2Int(x, y), true);
                }
            }
        }
    }
}
