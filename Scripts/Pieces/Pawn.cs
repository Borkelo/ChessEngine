public class Pawn : Piece
{
    public override char UnicodeSymbol => '♟';
    public override char AsciiSymbol => Color == PieceColor.White ? 'P' : 'p';
}