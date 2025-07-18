public class MoveHistoryEntry
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