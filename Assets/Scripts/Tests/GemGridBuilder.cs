using System.Collections.Generic;

namespace GroundZero.Tests
{
    public class GemGridBuilder
    {
        private int _size;
        private int _typeCount;
        private List<List<int>> _indexes;
        
        public GemGridBuilder WithSize(int size)
        {
            _size = size;
            return this;
        }
        
        public GemGridBuilder WithTypeCount(int typeCount)
        {
            _typeCount = typeCount;
            return this;
        }
        
        public GemGridBuilder WithGems(List<List<int>> indexes)
        {
            _indexes = indexes;
            _size = indexes.Count;
            return this;
        }
        
        public GemGrid Build()
        {
            var grid = new GemGrid(_size, _typeCount);
            
            // If indexes has been set, fill the grid with the given indexes.
            if (_indexes != null)
            {
                for (int y = 0; y < _size; y++)
                {
                    for (int x = 0; x < _size; x++)
                    {
                        grid.GemIndexes[y][x] = _indexes[y][x];
                    }
                }
            }
            
            return grid;
        }
        
        public static implicit operator GemGrid(GemGridBuilder builder)
        {
            return builder.Build();
        }
    }
}
