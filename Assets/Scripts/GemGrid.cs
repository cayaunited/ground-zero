using System.Collections.Generic;
using UnityEngine;

namespace GroundZero
{
    /// <summary>
    /// Responsible for storing and manipulating the indexes tracking which gems are where.
    /// </summary>
    public class GemGrid
    {
        public static int MIN_MATCH_LENGTH = 3;
        public static int MAX_MATCH_LENGTH = 5;
        
        /// <summary>
        /// The horizontal and vertical size of the grid in units.
        /// Should not change so it's readonly, meaning it can only be set when making a GemGrid.
        /// </summary>
        public readonly int Size;
        /// <summary>
        /// The indexes of gem types in an array, where -1 means the spot is empty.
        /// 0 is the first gem type, 1 is the second, and so on.
        /// </summary>
        public readonly List<List<int>> GemIndexes;
        /// <summary>
        /// The positions of any gems that were matched with others after calling FindMatches.
        /// </summary>
        public readonly List<Vector2Int> MatchedGems;
        
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
        /// Initializes the grid to be empty of gems.
        /// </summary>
        /// <param name="size">The horizontal and vertical size of the grid in units.</param>
        public GemGrid(int size)
        {
            Size = size;
            
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
            
            _potentialHorizontalMatch = new List<Vector2Int>(capacity: MAX_MATCH_LENGTH);
            _potentialVerticalMatch = new List<Vector2Int>(capacity: MAX_MATCH_LENGTH);
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
    }
}
