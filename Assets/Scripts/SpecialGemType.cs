namespace GroundZero
{
    /// <summary>
    /// Special gems are created with a match that is 4 or 5 gems long.
    /// Explosive gems are created from a 4-gem match, and targeting gems from a 5-gem match.
    /// SpecialGemType is an enum used to store this information in one place,
    /// rather than typing 4 or 5 everywhere we need to know the type of special gem.
    /// </summary>
    public enum SpecialGemType
    {
        Explosive = 4,
        Targeting = 5,
    }
}
