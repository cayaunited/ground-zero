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
        /// The indexes of gem types in an array, where -1 means the spot is empty.
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
        /// Initializes the grid to be empty of gems.
        /// </summary>
        /// <param name="size">The horizontal and vertical size of the grid in units.</param>
        /// <param name="typeCount">How many types of gems there are.</param>
        public GemGrid(int size, int typeCount)
        {
            Size = size;
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
            MatchedGems = new List<Vector2Int>(capacity: Size * Size);
            // The max number of gems that can drop is the whole grid, minus the bottom row.
            // We don't need to set a capacity for lists or dictionaries,
            // but it helps the computer if we already know the max size of the list or dictionary.
            DroppedGems = new Dictionary<Vector2Int, Vector2Int>(capacity: Size * (Size - 1));
            
            SpawnedGems = new List<Vector2Int>(capacity: Size * Size);
            
            _potentialHorizontalMatch = new List<Vector2Int>(capacity: MAX_MATCH_LENGTH);
            _potentialVerticalMatch = new List<Vector2Int>(capacity: MAX_MATCH_LENGTH);
        }
        
        /// <summary>
        /// Randomly fills grid based on the number of gem types.
        /// Checks for matches, and refills if there are any.
        /// </summary>
        public void FillGrid()
        {
            // There's no need to set a value here, since we'll do that below before using the value.
            int matchedCount;
            
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
                
                // Then, check for matches and refill the grid if needed,
                // repeating the process until there are no matches to start with.
                matchedCount = FindMatches();
            } while (matchedCount > 0);
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
            
            // If the indexes aren't the same, the positions aren't the same,
            // and they are adjacent, then swap them.
            if (type1 == type2 || position1 == position2
                || position1.x != position2.x && position1.y != position2.y) return false;
            
            GemIndexes[position2.y][position2.x] = type1;
            GemIndexes[position1.y][position1.x] = type2;
            return true;
        }
        
        /// <summary>
        /// Finds the positions of gems that are in a row or column with other gems of the same type.
        /// Stores the results in MatchedGems.
        /// </summary>
        /// <returns>How many gems were matched.</returns>
        public int FindMatches()
        {
            // First, clear out the list of matched gems to make sure
            // the final list only has gems that are currently matched.
            MatchedGems.Clear();
            
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
                }
            }
            
            // Return how many gems are currently in the list,
            // which is different from the capacity (the max possible number of gems in the list).
            return MatchedGems.Count;
        }
        
        /// <summary>
        /// Removes any matched gems from the array, replacing their indexes with -1.
        /// </summary>
        public void ClearMatches()
        {
            foreach (var position in MatchedGems)
            {
                GemIndexes[position.y][position.x] = -1;
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
        /// Looks to see if there is a match starting from a certain point and in a certain direction.
        /// Stores the results in a potential match array to pick the longest between horizontal and vertical matches.
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
    }
}
