public class Rook : Piece
{
    public override char UnicodeSymbol => '♜';
    public override char AsciiSymbol => Color == PieceColor.White ? 'R' : 'r';
}