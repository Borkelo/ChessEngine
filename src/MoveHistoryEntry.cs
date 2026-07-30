// Stored for every make/unmake operation during search. A value type keeps the
// search from allocating one heap object per pseudo-legal move.
public struct MoveHistoryEntry
{
    public Move Move { get; set; }
    public Piece? CapturedPiece { get; set; } //This is null in en passant captures
    public bool IsPromotion { get; set; }
    public bool OriginalHasMoved { get; set; }
    public Coordinate? PreviousEnPassant { get; set; }
    public int PreviousHalfmoveClock { get; set; }
    public int PreviousFullmoveCounter { get; set; }
    public PieceColor PreviousTurn { get; set; }
    
}
