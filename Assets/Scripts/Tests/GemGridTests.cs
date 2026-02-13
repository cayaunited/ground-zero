using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.VersionControl;
using UnityEngine;

namespace GroundZero.Tests
{
    public class GemGridTests
    {
        [Test]
        public void Constructor_CreatesAnEmptyArrayOfGivenSize()
        {
            const int size = 2;
            GemGrid grid = A.GemGrid.WithSize(size);
            
            // Ensure the correct number of rows are created.
            Assert.AreEqual(size, grid.GemIndexes.Count);
            // Ensure the correct number of columns are created.
            Assert.AreEqual(size, grid.GemIndexes[0].Count);
            // Ensure cells are filled with -1.
            Assert.AreEqual(-1, grid.GemIndexes[0][0]);
            Assert.AreEqual(-1, grid.GemIndexes[0][1]);
            Assert.AreEqual(-1, grid.GemIndexes[1][0]);
            Assert.AreEqual(-1, grid.GemIndexes[1][1]);
        }
        
        [Test]
        [TestCase(0, 0, 0, 1)]
        [TestCase(0, 0, 1, 0)]
        public void SwapGems_WithDifferentGemTypesAndAdjacentPositions_SwapsPositions(int x1, int y1, int x2, int y2)
        {
            // Remember, since [0, 1] is the first item in the list,
            // it's the first row and therefore y = 0.
            List<List<int>> gems = new()
            {
                new() { 0, 1 },
                new() { 2, 3 }
            };
            
            GemGrid grid = A.GemGrid.WithGems(gems);
            
            var wereSwapped = grid.SwapGems(new(x1, y1), new(x2, y2));
            
            Assert.IsTrue(wereSwapped);
            Assert.AreEqual(gems[y2][x2], grid.GemIndexes[y1][x1]);
            Assert.AreEqual(gems[y1][x1], grid.GemIndexes[y2][x2]);
        }
        
        [Test]
        [TestCase(0, 0, 1, 1)]
        [TestCase(0, 1, 1, 0)]
        [TestCase(0, 0, 0, 2)]
        [TestCase(0, 0, 2, 0)]
        public void SwapGems_WithDifferentGemTypesAndNonAdjacentPositions_DoesNotSwapPositions(int x1, int y1, int x2, int y2)
        {
            List<List<int>> gems = new()
            {
                new() { 0, 1, 2 },
                new() { 2, 3, 0 },
                new() { 1, 2, 3 }
            };
            
            GemGrid grid = A.GemGrid.WithGems(gems);
            
            var wereSwapped = grid.SwapGems(new(x1, y1), new(x2, y2));
            
            Assert.IsFalse(wereSwapped);
            Assert.AreEqual(gems[y1][x1], grid.GemIndexes[y1][x1]);
            Assert.AreEqual(gems[y2][x2], grid.GemIndexes[y2][x2]);
        }
        
        [Test]
        [TestCase(0, 0, 0, 1)]
        [TestCase(0, 0, 1, 0)]
        public void SwapGems_WithSameGemTypesAndAdjacentPositions_DoesNotSwapPositions(int x1, int y1, int x2, int y2)
        {
            List<List<int>> gems = new()
            {
                new() { 0, 0 },
                new() { 0, 0 }
            };
            
            GemGrid grid = A.GemGrid.WithGems(gems);
            
            var wereSwapped = grid.SwapGems(new(x1, y1), new(x2, y2));
            
            Assert.IsFalse(wereSwapped);
        }
        
        [Test]
        [TestCase(0, 0, 0, 0)]
        [TestCase(1, 1, 1, 1)]
        public void SwapGems_WithDifferentGemTypesAndSamePositions_DoesNotSwapPositions(int x1, int y1, int x2, int y2)
        {
            List<List<int>> gems = new()
            {
                new() { 0, 1 },
                new() { 2, 3 }
            };
            
            GemGrid grid = A.GemGrid.WithGems(gems);
            
            var wereSwapped = grid.SwapGems(new(x1, y1), new(x2, y2));
            
            Assert.IsFalse(wereSwapped);
            Assert.AreEqual(gems[y1][x1], grid.GemIndexes[y1][x1]);
            Assert.AreEqual(gems[y2][x2], grid.GemIndexes[y2][x2]);
        }
        
        [Test]
        public void SwapGems_WithFirstGemAsSpecial_SwapsSpecialGemPosition()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 1 },
                new() { 2, 3 }
            });
            
            var position1 = new Vector2Int(0, 0);
            var position2 = new Vector2Int(0, 1);
            
            grid.SpecialGems.Add(position1, SpecialGemType.Explosive);
            
            grid.SwapGems(position1, position2);
            
            Assert.AreEqual(1, grid.SpecialGems.Count);
            Assert.IsTrue(grid.SpecialGems.ContainsKey(position2));
            Assert.AreEqual(SpecialGemType.Explosive, grid.SpecialGems[position2]);
            Assert.IsFalse(grid.SpecialGems.ContainsKey(position1));
        }
        
        [Test]
        public void SwapGems_WithSecondGemAsSpecial_SwapsSpecialGemPosition()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 1 },
                new() { 2, 3 }
            });
            
            var position1 = new Vector2Int(0, 0);
            var position2 = new Vector2Int(0, 1);
            
            grid.SpecialGems.Add(position2, SpecialGemType.Explosive);
            
            grid.SwapGems(position1, position2);
            
            Assert.AreEqual(1, grid.SpecialGems.Count);
            Assert.IsTrue(grid.SpecialGems.ContainsKey(position1));
            Assert.AreEqual(SpecialGemType.Explosive, grid.SpecialGems[position1]);
            Assert.IsFalse(grid.SpecialGems.ContainsKey(position2));
        }
        
        [Test]
        public void SwapGems_WithBothGemsAsSpecial_SwapsSpecialGemPositions()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 1 },
                new() { 2, 3 }
            });
            
            var position1 = new Vector2Int(0, 0);
            var position2 = new Vector2Int(0, 1);
            
            grid.SpecialGems.Add(position1, SpecialGemType.Explosive);
            grid.SpecialGems.Add(position2, SpecialGemType.Targeting);
            
            grid.SwapGems(position1, position2);
            
            Assert.AreEqual(2, grid.SpecialGems.Count);
            Assert.IsTrue(grid.SpecialGems.ContainsKey(position1));
            Assert.AreEqual(SpecialGemType.Targeting, grid.SpecialGems[position1]);
            Assert.IsTrue(grid.SpecialGems.ContainsKey(position2));
            Assert.AreEqual(SpecialGemType.Explosive, grid.SpecialGems[position2]);
        }
        
        [Test]
        public void FindMatches_WithNoMatches_FindsNothing()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 2, 1, 0 },
                new() { 1, 2, 1 },
                new() { 0, 1, 2 }
            });
            
            var matchCount = grid.FindMatches();
            
            Assert.Zero(matchCount);
            Assert.IsEmpty(grid.MatchedGems);
        }
        
        [Test]
        public void FindMatches_WithTooShortHorizontalMatch_FindsNothing()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 2, 2, 0 },
                new() { 1, 0, 1 },
                new() { 0, 1, 2 }
            });
            
            var matchCount = grid.FindMatches();
            
            Assert.Zero(matchCount);
            Assert.IsEmpty(grid.MatchedGems);
        }
        
        [Test]
        public void FindMatches_WithTooShortVerticalMatch_FindsNothing()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 2, 1, 0 },
                new() { 2, 0, 1 },
                new() { 0, 1, 2 }
            });
            
            var matchCount = grid.FindMatches();
            
            Assert.Zero(matchCount);
            Assert.IsEmpty(grid.MatchedGems);
        }
        
        [Test]
        public void FindMatches_WithOneHorizontalMatchThree_FindsOneMatchThree()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 0 },
                new() { 1, 2, 1 },
                new() { 0, 1, 2 }
            });
            
            var matchCount = grid.FindMatches();
            
            // First, check that three gems were matched.
            Assert.AreEqual(3, matchCount);
            Assert.AreEqual(3, grid.MatchedGems.Count);
            // Then, confirm the right gems were matched.
            Assert.AreEqual(new Vector2Int(0, 0), grid.MatchedGems[0]);
            Assert.AreEqual(new Vector2Int(1, 0), grid.MatchedGems[1]);
            Assert.AreEqual(new Vector2Int(2, 0), grid.MatchedGems[2]);
        }
        
        [Test]
        public void FindMatches_WithOneVerticalMatchThree_FindsOneMatchThree()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 1, 0 },
                new() { 0, 2, 1 },
                new() { 0, 1, 2 }
            });
            
            var matchCount = grid.FindMatches();
            
            Assert.AreEqual(3, matchCount);
            Assert.AreEqual(3, grid.MatchedGems.Count);
            Assert.AreEqual(new Vector2Int(0, 0), grid.MatchedGems[0]);
            Assert.AreEqual(new Vector2Int(0, 1), grid.MatchedGems[1]);
            Assert.AreEqual(new Vector2Int(0, 2), grid.MatchedGems[2]);
        }
        
        [Test]
        public void FindMatches_WithMultipleHorizontalMatchThrees_FindsMultipleMatchThrees()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 0 },
                new() { 1, 2, 1 },
                new() { 0, 0, 0 }
            });
            
            var matchCount = grid.FindMatches();
            
            Assert.AreEqual(6, matchCount);
            Assert.AreEqual(6, grid.MatchedGems.Count);
            Assert.AreEqual(new Vector2Int(0, 0), grid.MatchedGems[0]);
            Assert.AreEqual(new Vector2Int(1, 0), grid.MatchedGems[1]);
            Assert.AreEqual(new Vector2Int(2, 0), grid.MatchedGems[2]);
            Assert.AreEqual(new Vector2Int(0, 2), grid.MatchedGems[3]);
            Assert.AreEqual(new Vector2Int(1, 2), grid.MatchedGems[4]);
            Assert.AreEqual(new Vector2Int(2, 2), grid.MatchedGems[5]);
        }
        
        [Test]
        public void FindMatches_WithMultipleVerticalMatchThrees_FindsMultipleMatchThrees()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 1, 0 },
                new() { 0, 2, 0 },
                new() { 0, 1, 0 }
            });
            
            var matchCount = grid.FindMatches();
            
            Assert.AreEqual(6, matchCount);
            Assert.AreEqual(6, grid.MatchedGems.Count);
            Assert.AreEqual(new Vector2Int(0, 0), grid.MatchedGems[0]);
            Assert.AreEqual(new Vector2Int(0, 1), grid.MatchedGems[1]);
            Assert.AreEqual(new Vector2Int(0, 2), grid.MatchedGems[2]);
            Assert.AreEqual(new Vector2Int(2, 0), grid.MatchedGems[3]);
            Assert.AreEqual(new Vector2Int(2, 1), grid.MatchedGems[4]);
            Assert.AreEqual(new Vector2Int(2, 2), grid.MatchedGems[5]);
        }
        
        [Test]
        public void FindMatches_WithOneHorizontalMatchFour_FindsOneMatchFour()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 0, 0 },
                new() { 1, 0, 1, 2 },
                new() { 2, 1, 0, 1 },
                new() { 3, 2, 1, 0 }
            });
            
            var matchCount = grid.FindMatches();
            
            Assert.AreEqual(4, matchCount);
            Assert.AreEqual(4, grid.MatchedGems.Count);
            Assert.AreEqual(new Vector2Int(0, 0), grid.MatchedGems[0]);
            Assert.AreEqual(new Vector2Int(1, 0), grid.MatchedGems[1]);
            Assert.AreEqual(new Vector2Int(2, 0), grid.MatchedGems[2]);
            Assert.AreEqual(new Vector2Int(3, 0), grid.MatchedGems[3]);
        }
        
        [Test]
        public void FindMatches_WithOneVerticalMatchFour_FindsOneMatchFour()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 1, 2, 3 },
                new() { 0, 0, 1, 2 },
                new() { 0, 1, 0, 1 },
                new() { 0, 2, 1, 0 }
            });
            
            var matchCount = grid.FindMatches();
            
            Assert.AreEqual(4, matchCount);
            Assert.AreEqual(4, grid.MatchedGems.Count);
            Assert.AreEqual(new Vector2Int(0, 0), grid.MatchedGems[0]);
            Assert.AreEqual(new Vector2Int(0, 1), grid.MatchedGems[1]);
            Assert.AreEqual(new Vector2Int(0, 2), grid.MatchedGems[2]);
            Assert.AreEqual(new Vector2Int(0, 3), grid.MatchedGems[3]);
        }
        
        [Test]
        public void FindMatches_WithOneHorizontallyTooLongMatch_FindsOneMatchAtMaxLength()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 0, 0, 0, 0 },
                new() { 1, 0, 1, 2, 3, 4 },
                new() { 2, 1, 0, 1, 2, 3 },
                new() { 3, 2, 1, 0, 1, 2 },
                new() { 4, 3, 2, 1, 0, 1 },
                new() { 5, 4, 3, 2, 1, 0 },
            });
            
            var matchCount = grid.FindMatches();
            
            Assert.AreEqual(5, matchCount);
            Assert.AreEqual(5, grid.MatchedGems.Count);
            Assert.AreEqual(new Vector2Int(0, 0), grid.MatchedGems[0]);
            Assert.AreEqual(new Vector2Int(1, 0), grid.MatchedGems[1]);
            Assert.AreEqual(new Vector2Int(2, 0), grid.MatchedGems[2]);
            Assert.AreEqual(new Vector2Int(3, 0), grid.MatchedGems[3]);
            Assert.AreEqual(new Vector2Int(4, 0), grid.MatchedGems[4]);
        }
        
        [Test]
        public void FindMatches_WithOneVerticallyTooLongMatch_FindsOneMatchAtMaxLength()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 1, 2, 3, 4, 5 },
                new() { 0, 0, 1, 2, 3, 4 },
                new() { 0, 1, 0, 1, 2, 3 },
                new() { 0, 2, 1, 0, 1, 2 },
                new() { 0, 3, 2, 1, 0, 1 },
                new() { 0, 4, 3, 2, 1, 0 },
            });
            
            var matchCount = grid.FindMatches();
            
            Assert.AreEqual(5, matchCount);
            Assert.AreEqual(5, grid.MatchedGems.Count);
            Assert.AreEqual(new Vector2Int(0, 0), grid.MatchedGems[0]);
            Assert.AreEqual(new Vector2Int(0, 1), grid.MatchedGems[1]);
            Assert.AreEqual(new Vector2Int(0, 2), grid.MatchedGems[2]);
            Assert.AreEqual(new Vector2Int(0, 3), grid.MatchedGems[3]);
            Assert.AreEqual(new Vector2Int(0, 4), grid.MatchedGems[4]);
        }
        
        [Test]
        public void FindMatches_WithConnectedHorizontalMatchThreeAndVerticalMatchThree_FindsHorizontalMatchThree()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 0 },
                new() { 0, 2, 1 },
                new() { 0, 1, 2 }
            });
            
            var matchCount = grid.FindMatches();
            
            Assert.AreEqual(3, matchCount);
            Assert.AreEqual(3, grid.MatchedGems.Count);
            Assert.AreEqual(new Vector2Int(0, 0), grid.MatchedGems[0]);
            Assert.AreEqual(new Vector2Int(1, 0), grid.MatchedGems[1]);
            Assert.AreEqual(new Vector2Int(2, 0), grid.MatchedGems[2]);
        }
        
        [Test]
        public void FindMatches_WithConnectedLongerHorizontalMatchAndShorterVerticalMatch_FindsLongerHorizontalMatch()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 0, 0 },
                new() { 0, 0, 1, 2 },
                new() { 0, 1, 0, 1 },
                new() { 3, 2, 1, 0 }
            });
            
            var matchCount = grid.FindMatches();
            
            Assert.AreEqual(4, matchCount);
            Assert.AreEqual(4, grid.MatchedGems.Count);
            Assert.AreEqual(new Vector2Int(0, 0), grid.MatchedGems[0]);
            Assert.AreEqual(new Vector2Int(1, 0), grid.MatchedGems[1]);
            Assert.AreEqual(new Vector2Int(2, 0), grid.MatchedGems[2]);
            Assert.AreEqual(new Vector2Int(3, 0), grid.MatchedGems[3]);
        }
        
        [Test]
        public void FindMatches_WithConnectedLongerVerticalMatchAndShorterHorizontalMatch_FindsLongerVerticalMatch()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 0, 3 },
                new() { 0, 0, 1, 2 },
                new() { 0, 1, 0, 1 },
                new() { 0, 2, 1, 0 }
            });
            
            var matchCount = grid.FindMatches();
            
            Assert.AreEqual(4, matchCount);
            Assert.AreEqual(4, grid.MatchedGems.Count);
            Assert.AreEqual(new Vector2Int(0, 0), grid.MatchedGems[0]);
            Assert.AreEqual(new Vector2Int(0, 1), grid.MatchedGems[1]);
            Assert.AreEqual(new Vector2Int(0, 2), grid.MatchedGems[2]);
            Assert.AreEqual(new Vector2Int(0, 3), grid.MatchedGems[3]);
        }
        
        [Test]
        public void FindMatches_CalledTwiceWithOneMatch_ClearsMatchesAndFindsOne()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 0 },
                new() { 1, 2, 1 },
                new() { 0, 1, 2 }
            });
            
            grid.FindMatches();
            var matchCount = grid.FindMatches();
            
            Assert.AreEqual(3, matchCount);
            Assert.AreEqual(3, grid.MatchedGems.Count);
            Assert.AreEqual(new Vector2Int(0, 0), grid.MatchedGems[0]);
            Assert.AreEqual(new Vector2Int(1, 0), grid.MatchedGems[1]);
            Assert.AreEqual(new Vector2Int(2, 0), grid.MatchedGems[2]);
        }
        
        [Test]
        public void FindMatches_WithOneMatchFourAfterSwapping_CreatesSpecialGem()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 2, 0 },
                new() { 1, 0, 0, 2 },
                new() { 2, 1, 2, 1 },
                new() { 3, 2, 1, 0 }
            });
            
            grid.SwapGems(new Vector2Int(2, 0), new Vector2Int(2, 1));
            
            var matchCount = grid.FindMatches(createSpecialGems: true);
            
            Assert.AreEqual(4, matchCount);
            Assert.AreEqual(4, grid.MatchedGems.Count);
            Assert.AreEqual(new Vector2Int(0, 0), grid.MatchedGems[0]);
            Assert.AreEqual(new Vector2Int(1, 0), grid.MatchedGems[1]);
            Assert.AreEqual(new Vector2Int(2, 0), grid.MatchedGems[2]);
            Assert.AreEqual(new Vector2Int(3, 0), grid.MatchedGems[3]);
            // Ensure the right special gem was created at the right spot.
            Assert.AreEqual(1, grid.SpecialGemsCreated.Count);
            Assert.AreEqual(4, grid.SpecialGemsCreated[new Vector2Int(2, 0)]);
            Assert.AreEqual(SpecialGemType.Explosive, grid.SpecialGems[new Vector2Int(2, 0)]);
        }
        
        [Test]
        public void FillGrid_RandomlyFillsGridWithNoMatches()
        {
            const int size = 3;
            const int gemTypeCount = 3;
            GemGrid grid = A.GemGrid.WithSize(size).WithTypeCount(gemTypeCount);
            
            grid.FillGrid();
            
            // Ensure cells are not filled with -1.
            Assert.AreNotEqual(-1, grid.GemIndexes[0][0]);
            Assert.AreNotEqual(-1, grid.GemIndexes[0][1]);
            Assert.AreNotEqual(-1, grid.GemIndexes[0][2]);
            Assert.AreNotEqual(-1, grid.GemIndexes[1][0]);
            Assert.AreNotEqual(-1, grid.GemIndexes[1][1]);
            Assert.AreNotEqual(-1, grid.GemIndexes[1][2]);
            Assert.AreNotEqual(-1, grid.GemIndexes[2][0]);
            Assert.AreNotEqual(-1, grid.GemIndexes[2][1]);
            Assert.AreNotEqual(-1, grid.GemIndexes[2][2]);
            
            var matchCount = grid.FindMatches();
            Assert.Zero(matchCount);
        }
        
        [Test]
        public void FillGrid_RandomlyFillsGridWithAtLeastOnePossibleMatch()
        {
            const int size = 8;
            const int gemTypeCount = 7;
            GemGrid grid = A.GemGrid.WithSize(size).WithTypeCount(gemTypeCount);
            
            grid.FillGrid();
            
            var canMatchesBeMade = grid.AreTherePossibleMatches();
            Assert.IsTrue(canMatchesBeMade);
        }
        
        [Test]
        public void FillGrid_WithExistingSpecialGems_RandomlySpawnsNewSpecialGems()
        {
            const int size = 3;
            const int gemTypeCount = 3;
            GemGrid grid = A.GemGrid.WithSize(size).WithTypeCount(gemTypeCount);
            grid.FillGrid();
            
            grid.SpecialGems.Add(new Vector2Int(0, 0), SpecialGemType.Explosive);
            grid.SpecialGems.Add(new Vector2Int(1, 0), SpecialGemType.Explosive);
            grid.SpecialGems.Add(new Vector2Int(0, 1), SpecialGemType.Targeting);
            grid.SpecialGems.Add(new Vector2Int(1, 1), SpecialGemType.Targeting);
            grid.SpecialGems.Add(new Vector2Int(2, 1), SpecialGemType.Targeting);
            
            grid.FillGrid();
            
            Assert.AreEqual(5, grid.SpecialGems.Count);
            Assert.IsTrue(grid.SpecialGems.ContainsValue(SpecialGemType.Explosive));
            Assert.IsTrue(grid.SpecialGems.ContainsValue(SpecialGemType.Targeting));
        }
        
        [Test]
        public void AreTherePossibleMatches_WithAPossibleMatch_ReturnsTrue()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 1, 0, 0 },
                new() { 0, 2, 1 },
                new() { 0, 1, 2 }
            });
            
            var canMatchesBeMade = grid.AreTherePossibleMatches();
            
            Assert.IsTrue(canMatchesBeMade);
            // Make sure the grid wasn't changed.
            Assert.AreEqual(1, grid.GemIndexes[0][0]);
            Assert.AreEqual(0, grid.GemIndexes[0][1]);
            Assert.AreEqual(0, grid.GemIndexes[0][2]);
            Assert.AreEqual(0, grid.GemIndexes[1][0]);
            Assert.AreEqual(2, grid.GemIndexes[1][1]);
            Assert.AreEqual(1, grid.GemIndexes[1][2]);
            Assert.AreEqual(0, grid.GemIndexes[2][0]);
            Assert.AreEqual(1, grid.GemIndexes[2][1]);
            Assert.AreEqual(2, grid.GemIndexes[2][2]);
        }
        
        [Test]
        public void AreTherePossibleMatches_WithNoPossibleMatch_ReturnsFalse()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 1, 0, 0 },
                new() { 1, 2, 2 },
                new() { 0, 0, 2 }
            });
            
            var canMatchesBeMade = grid.AreTherePossibleMatches();
            
            Assert.IsFalse(canMatchesBeMade);
            // Make sure the grid wasn't changed.
            Assert.AreEqual(1, grid.GemIndexes[0][0]);
            Assert.AreEqual(0, grid.GemIndexes[0][1]);
            Assert.AreEqual(0, grid.GemIndexes[0][2]);
            Assert.AreEqual(1, grid.GemIndexes[1][0]);
            Assert.AreEqual(2, grid.GemIndexes[1][1]);
            Assert.AreEqual(2, grid.GemIndexes[1][2]);
            Assert.AreEqual(0, grid.GemIndexes[2][0]);
            Assert.AreEqual(0, grid.GemIndexes[2][1]);
            Assert.AreEqual(2, grid.GemIndexes[2][2]);
        }
        
        [Test]
        public void DestroyMatches_WithAMatch_ClearsMatchedGems()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 0 },
                new() { 1, 2, 1 },
                new() { 0, 1, 2 }
            });
            
            grid.FindMatches();
            
            grid.DestroyMatches();
            
            Assert.Zero(grid.MatchedGems.Count);
            // Confirm the matched gems were cleared / reset to -1.
            Assert.AreEqual(-1, grid.GemIndexes[0][0]);
            Assert.AreEqual(-1, grid.GemIndexes[0][1]);
            Assert.AreEqual(-1, grid.GemIndexes[0][2]);
            // Then, confirm the other gems are still there.
            Assert.AreEqual(1, grid.GemIndexes[1][0]);
            Assert.AreEqual(2, grid.GemIndexes[1][1]);
            Assert.AreEqual(1, grid.GemIndexes[1][2]);
            Assert.AreEqual(0, grid.GemIndexes[2][0]);
            Assert.AreEqual(1, grid.GemIndexes[2][1]);
            Assert.AreEqual(2, grid.GemIndexes[2][2]);
        }
        
        [Test]
        public void DestroyMatches_WithASpecialGem_ClearsMatchExceptSpecialGem()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 2, 0 },
                new() { 1, 0, 0, 2 },
                new() { 2, 1, 2, 1 },
                new() { 3, 2, 1, 0 }
            });
            
            grid.SwapGems(new Vector2Int(2, 0), new Vector2Int(2, 1));
            grid.FindMatches(createSpecialGems: true);
            
            grid.DestroyMatches();
            
            Assert.AreEqual(1, grid.SpecialGemsCreated.Count);
            Assert.AreEqual(4, grid.SpecialGemsCreated[new Vector2Int(2, 0)]);
            Assert.AreEqual(-1, grid.GemIndexes[0][0]);
            Assert.AreEqual(-1, grid.GemIndexes[0][1]);
            // Ensure the 0 is still there, because that represents the special gem.
            Assert.AreEqual(0, grid.GemIndexes[0][2]);
            Assert.AreEqual(-1, grid.GemIndexes[0][3]);
        }
        
        [Test]
        public void DestroyMatches_WithAnExplosiveGemInsideBorder_DestroysSurroundingGems()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 1, 0, 0, 1 },
                new() { 0, 0, 2, 2 },
                new() { 2, 1, 2, 1 },
                new() { 3, 2, 1, 0 }
            });
            
            grid.SpecialGems.Add(new Vector2Int(2, 0), SpecialGemType.Explosive);
            grid.SwapGems(new Vector2Int(2, 0), new Vector2Int(2, 1));
            grid.FindMatches(createSpecialGems: true);
            
            grid.DestroyMatches();
            
            Assert.AreEqual(10, grid.DestroyedGems.Count);
            Assert.Contains(new Vector2Int(1, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(0, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 2), grid.DestroyedGems);
        }
        
        [Test]
        public void DestroyMatches_WithAnExplosiveGemAtEdge_DestroysSurroundingGems()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 2, 1 },
                new() { 1, 0, 0, 2 },
                new() { 2, 1, 2, 1 },
                new() { 3, 2, 1, 0 }
            });
            
            grid.SpecialGems.Add(new Vector2Int(2, 1), SpecialGemType.Explosive);
            grid.SwapGems(new Vector2Int(2, 0), new Vector2Int(2, 1));
            grid.FindMatches(createSpecialGems: true);
            
            grid.DestroyMatches();
            
            Assert.AreEqual(7, grid.DestroyedGems.Count);
            Assert.Contains(new Vector2Int(0, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 1), grid.DestroyedGems);
        }
        
        [Test]
        public void DestroyMatches_WithExplosiveGemsNextToEachOther_StartsExplosiveChain()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 2, 1 },
                new() { 1, 0, 0, 2 },
                new() { 2, 1, 2, 1 },
                new() { 3, 2, 1, 0 }
            });
            
            grid.SpecialGems.Add(new Vector2Int(2, 0), SpecialGemType.Explosive);
            grid.SpecialGems.Add(new Vector2Int(2, 1), SpecialGemType.Explosive);
            grid.SwapGems(new Vector2Int(2, 0), new Vector2Int(2, 1));
            grid.FindMatches(createSpecialGems: true);
            
            grid.DestroyMatches();
            
            Assert.AreEqual(10, grid.DestroyedGems.Count);
            Assert.Contains(new Vector2Int(0, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 2), grid.DestroyedGems);
        }
        
        [Test]
        public void DestroyMatches_WithATargetingGem_DestroysGemsOfOtherType()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 1, 0, 0, 1 },
                new() { 0, 0, 2, 2 },
                new() { 2, 1, 2, 1 },
                new() { 3, 2, 1, 0 }
            });
            
            grid.SpecialGems.Add(new Vector2Int(2, 0), SpecialGemType.Targeting);
            grid.SwapGems(new Vector2Int(2, 0), new Vector2Int(2, 1));
            grid.FindMatches(createSpecialGems: true);
            
            grid.DestroyMatches();
            
            Assert.AreEqual(8, grid.DestroyedGems.Count);
            // Make sure the matched gems were destroyed.
            Assert.Contains(new Vector2Int(0, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 1), grid.DestroyedGems);
            // Make sure the targeted gems were destroyed.
            Assert.Contains(new Vector2Int(2, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(0, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 3), grid.DestroyedGems);
        }
        
        [Test]
        public void DestroyMatches_WhenSwappingTwoTargetingGems_DestroysGemsOfBothTypes()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 1, 0, 0, 1 },
                new() { 0, 0, 2, 2 },
                new() { 2, 1, 2, 1 },
                new() { 3, 2, 1, 0 }
            });
            
            grid.SpecialGems.Add(new Vector2Int(2, 0), SpecialGemType.Targeting);
            grid.SpecialGems.Add(new Vector2Int(2, 1), SpecialGemType.Targeting);
            grid.SwapGems(new Vector2Int(2, 0), new Vector2Int(2, 1));
            grid.FindMatches(createSpecialGems: true);
            
            grid.DestroyMatches();
            
            Assert.AreEqual(10, grid.DestroyedGems.Count);
            Assert.Contains(new Vector2Int(0, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 1), grid.DestroyedGems);
            
            Assert.Contains(new Vector2Int(1, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(0, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 3), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 3), grid.DestroyedGems);
        }
        
        [Test]
        public void DestroyMatches_WithATargetingGemDestroyingAnExplosiveGem_TriggersBothEffects()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 1, 0, 0, 1 },
                new() { 0, 0, 2, 2 },
                new() { 2, 1, 2, 1 },
                new() { 3, 2, 1, 0 }
            });
            
            grid.SpecialGems.Add(new Vector2Int(2, 0), SpecialGemType.Targeting);
            grid.SpecialGems.Add(new Vector2Int(1, 3), SpecialGemType.Explosive);
            grid.SwapGems(new Vector2Int(2, 0), new Vector2Int(2, 1));
            grid.FindMatches(createSpecialGems: true);
            
            grid.DestroyMatches();
            
            Assert.AreEqual(11, grid.DestroyedGems.Count);
            Assert.Contains(new Vector2Int(0, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 1), grid.DestroyedGems);
            
            Assert.Contains(new Vector2Int(2, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(0, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(0, 3), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 3), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 3), grid.DestroyedGems);
        }
        
        [Test]
        public void DestroyMatches_WithAnExplosiveGemDestroyingATargetingGem_TriggersBothEffects()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 1, 0, 0, 1 },
                new() { 0, 0, 2, 2 },
                new() { 2, 1, 2, 1 },
                new() { 3, 2, 1, 0 }
            });
            
            grid.SpecialGems.Add(new Vector2Int(2, 0), SpecialGemType.Explosive);
            grid.SpecialGems.Add(new Vector2Int(3, 1), SpecialGemType.Targeting);
            grid.SwapGems(new Vector2Int(2, 0), new Vector2Int(2, 1));
            grid.FindMatches(createSpecialGems: true);
            
            grid.DestroyMatches();
            
            Assert.AreEqual(12, grid.DestroyedGems.Count);
            Assert.Contains(new Vector2Int(0, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 1), grid.DestroyedGems);
            
            Assert.Contains(new Vector2Int(1, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 0), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 1), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(0, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(2, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(3, 2), grid.DestroyedGems);
            Assert.Contains(new Vector2Int(1, 3), grid.DestroyedGems);
        }
        
        [Test]
        public void DropGems_WithNoEmptySpaces_DoesNotDropGems()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 0 },
                new() { 1, 2, 1 },
                new() { 0, 1, 2 }
            });
            
            grid.DropGems();
            
            Assert.AreEqual(0, grid.DroppedGems.Count);
            Assert.AreEqual(0, grid.GemIndexes[0][0]);
            Assert.AreEqual(0, grid.GemIndexes[0][1]);
            Assert.AreEqual(0, grid.GemIndexes[0][2]);
            Assert.AreEqual(1, grid.GemIndexes[1][0]);
            Assert.AreEqual(2, grid.GemIndexes[1][1]);
            Assert.AreEqual(1, grid.GemIndexes[1][2]);
            Assert.AreEqual(0, grid.GemIndexes[2][0]);
            Assert.AreEqual(1, grid.GemIndexes[2][1]);
            Assert.AreEqual(2, grid.GemIndexes[2][2]);
        }
        
        [Test]
        public void DropGems_WithNoEmptySpacesBelow_DoesNotDropGems()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 0 },
                new() { 1, 2, 1 },
                new() { -1, -1, -1 }
            });
            
            grid.DropGems();
            
            Assert.AreEqual(0, grid.DroppedGems.Count);
            Assert.AreEqual(0, grid.GemIndexes[0][0]);
            Assert.AreEqual(0, grid.GemIndexes[0][1]);
            Assert.AreEqual(0, grid.GemIndexes[0][2]);
            Assert.AreEqual(1, grid.GemIndexes[1][0]);
            Assert.AreEqual(2, grid.GemIndexes[1][1]);
            Assert.AreEqual(1, grid.GemIndexes[1][2]);
            Assert.AreEqual(-1, grid.GemIndexes[2][0]);
            Assert.AreEqual(-1, grid.GemIndexes[2][1]);
            Assert.AreEqual(-1, grid.GemIndexes[2][2]);
        }
        
        [Test]
        public void DropGems_WithEmptySpacesBelow_DropsGemsToLowestPossiblePosition()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { -1, -1, -1 },
                new() { 1, -1, 1 },
                new() { 0, 1, 2 }
            });
            
            grid.DropGems();
            
            Assert.AreEqual(5, grid.DroppedGems.Count);
            // Confirm the correct changes were made and tracked.
            Assert.AreEqual(new Vector2Int(0, 0), grid.DroppedGems[new Vector2Int(0, 1)]);
            Assert.AreEqual(new Vector2Int(0, 1), grid.DroppedGems[new Vector2Int(0, 2)]);
            Assert.AreEqual(new Vector2Int(1, 0), grid.DroppedGems[new Vector2Int(1, 2)]);
            Assert.AreEqual(new Vector2Int(2, 0), grid.DroppedGems[new Vector2Int(2, 1)]);
            Assert.AreEqual(new Vector2Int(2, 1), grid.DroppedGems[new Vector2Int(2, 2)]);
            // Confirm the grid was correctly updated.
            Assert.AreEqual(1, grid.GemIndexes[0][0]);
            Assert.AreEqual(1, grid.GemIndexes[0][1]);
            Assert.AreEqual(1, grid.GemIndexes[0][2]);
            Assert.AreEqual(0, grid.GemIndexes[1][0]);
            Assert.AreEqual(-1, grid.GemIndexes[1][1]);
            Assert.AreEqual(2, grid.GemIndexes[1][2]);
            // The top positions should now be empty.
            Assert.AreEqual(-1, grid.GemIndexes[2][0]);
            Assert.AreEqual(-1, grid.GemIndexes[2][1]);
            Assert.AreEqual(-1, grid.GemIndexes[2][2]);
        }
        
        [Test]
        public void DropGems_AfterDropping_DoesNotDropMoreGems()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { -1, -1, -1 },
                new() { 1, -1, 1 },
                new() { 0, 1, 2 }
            });
            
            grid.DropGems();
            grid.DropGems();
            
            Assert.AreEqual(0, grid.DroppedGems.Count);
        }
        
        [Test]
        public void SpawnNewGems_WithNoEmptySpaces_DoesNotSpawnGems()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 0 },
                new() { 1, 2, 1 },
                new() { 0, 1, 2 }
            });
            
            grid.SpawnNewGems();
            
            Assert.Zero(grid.SpawnedGems.Count);
            Assert.AreEqual(0, grid.GemIndexes[0][0]);
            Assert.AreEqual(0, grid.GemIndexes[0][1]);
            Assert.AreEqual(0, grid.GemIndexes[0][2]);
            Assert.AreEqual(1, grid.GemIndexes[1][0]);
            Assert.AreEqual(2, grid.GemIndexes[1][1]);
            Assert.AreEqual(1, grid.GemIndexes[1][2]);
            Assert.AreEqual(0, grid.GemIndexes[2][0]);
            Assert.AreEqual(1, grid.GemIndexes[2][1]);
            Assert.AreEqual(2, grid.GemIndexes[2][2]);
        }
        
        [Test]
        public void SpawnNewGems_WithEmptySpaces_SpawnsGemsInEmptySpaces()
        {
            GemGrid grid = A.GemGrid.WithGems(new()
            {
                new() { 0, 0, 0 },
                new() { 1, -1, 1 },
                new() { -1, -1, -1 }
            });
            
            grid.SpawnNewGems();
            
            Assert.AreEqual(4, grid.SpawnedGems.Count);
            // Ensure gems were spawned at the right spots.
            Assert.AreEqual(new Vector2Int(1, 1), grid.SpawnedGems[0]);
            Assert.AreEqual(new Vector2Int(0, 2), grid.SpawnedGems[1]);
            Assert.AreEqual(new Vector2Int(1, 2), grid.SpawnedGems[2]);
            Assert.AreEqual(new Vector2Int(2, 2), grid.SpawnedGems[3]);
            
            Assert.AreEqual(0, grid.GemIndexes[0][0]);
            Assert.AreEqual(0, grid.GemIndexes[0][1]);
            Assert.AreEqual(0, grid.GemIndexes[0][2]);
            Assert.AreEqual(1, grid.GemIndexes[1][0]);
            // Since we can't know beforehand what gem type was selected,
            // just make sure there was one selected.
            Assert.AreNotEqual(-1, grid.GemIndexes[1][1]);
            Assert.AreEqual(1, grid.GemIndexes[1][2]);
            Assert.AreNotEqual(-1, grid.GemIndexes[2][0]);
            Assert.AreNotEqual(-1, grid.GemIndexes[2][1]);
            Assert.AreNotEqual(-1, grid.GemIndexes[2][2]);
        }
    }
}
