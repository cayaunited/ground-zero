namespace GroundZero
{
    /// <summary>
    /// The grid can be in one of five different states, as represented by this GridState enum.
    /// In general, these states happen in sequential order, starting from WaitingForInput (enum value of 0),
    /// and ending at Replacing (enum value of 4). However, some of these states can occur at the same time,
    /// and some states loop back to different previous states.
    /// </summary>
    public enum GridState
    {
        WaitingForInput,
        Swapping,
        Matching,
        Dropping,
        Replacing,
    }
}
