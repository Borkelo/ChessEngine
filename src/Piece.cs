public abstract class Piece
{
    public PieceColor Color { get; set; }
    public bool HasMoved { get; set; } = false; //Only relevant for pawns, kings and rooks
    public abstract char UnicodeSymbol { get; }
    public abstract char AsciiSymbol { get; }
    public abstract int Value { get; }
    
    //These moves can be illegal in the sense that they leave the king in check
    public abstract IEnumerable<Move> GetPseudoLegalMoves(Board board, Coordinate from);
    public abstract IEnumerable<Move> GetAttackingSquares(Board board, Coordinate from);
    
    //This method checks if each pseudolegal move causes a check to the moving side, making it illegal
    public IEnumerable<Move> GetLegalMoves(Board board, Coordinate from)
    {
        PieceColor sideThatMoved = board.CurrentTurn;

        foreach(Move move in GetPseudoLegalMoves(board, from))
        {            
            GameLogic.ApplyMove(board, move);
                       
            if(!GameLogic.IsCheck(board, sideThatMoved))
            {
                board.Undo();
                yield return move;
            }
            else
            {
                board.Undo(); 
            }                     
        }
    }
    
    public static Piece GetPieceFromSymbol(char symbol)
    {
        return symbol.ToString().ToLower() switch
        {
            "p" => new Pawn(),
            "n" => new Knight(),
            "b" => new Bishop(),
            "r" => new Rook(),
            "q" => new Queen(),
            "k" => new King(),
            _ => throw new Exception($"Invalid piece symbol: {symbol}")
        };
    }
    
    public static PieceColor OppositeColor(PieceColor color)
    {
        return color == PieceColor.White ? PieceColor.Black : PieceColor.White;
    }
    
    public abstract Piece Clone();
          
}
public enum PieceColor
{
    White,
    Black
}