public class Pawn : Piece
{
    public override char UnicodeSymbol => '♟';
    public override char AsciiSymbol => Color == PieceColor.White ? 'P' : 'p';
    public override int Value => Color == PieceColor.White ? 1 : -1;
    
    public override IEnumerable<Move> GetPseudoLegalMoves(Board board, Coordinate from)
    {
        Coordinate offset = Color == PieceColor.White ? new(0, 1) : new(0, -1);
        
        Coordinate oneForward = from + offset;
        
        if(board.pieces[oneForward.Col, oneForward.Row] == null)
        {
            yield return new(from, oneForward);
            
            Coordinate twoForward = oneForward + offset;
            
            if(!board.pieces[from.Col, from.Row]!.HasMoved && board.pieces[twoForward.Col, twoForward.Row] == null)
            {
                yield return new(from, twoForward);
            }
        }

        Coordinate forwardLeft = oneForward + new Coordinate(-1, 0);
        Coordinate forwardRight = oneForward + new Coordinate(1, 0);
        
        if(GameLogic.IsOnBoard(forwardLeft))
        {
            if((board.pieces[forwardLeft.Col, forwardLeft.Row] is Piece piece && piece.Color != this.Color) || forwardLeft == board.EnPassantCoordinate)
            {
                yield return new(from, forwardLeft);
            }
        }
        if(GameLogic.IsOnBoard(forwardRight))
        {
            if((board.pieces[forwardRight.Col, forwardRight.Row] is Piece piece && piece.Color != this.Color) || forwardRight == board.EnPassantCoordinate)
            {
                yield return new(from, forwardRight);
            }
        }
    }
    
    //Pawn and king are the only piece that attacks differently than it moves, thus this method is needed
    public override IEnumerable<Move> GetAttackingSquares(Board board, Coordinate from)
    {
        Coordinate offset = Color == PieceColor.White ? new(0, 1) : new(0, -1);
        
        Coordinate oneForward = from + offset;
        
        Coordinate forwardLeft = oneForward + new Coordinate(-1, 0);
        Coordinate forwardRight = oneForward + new Coordinate(1, 0);
                
        if(GameLogic.IsOnBoard(forwardLeft))
            yield return new(from, forwardLeft);
        
        if(GameLogic.IsOnBoard(forwardRight))
             yield return new(from, forwardRight);                          
    }
    
    public override Piece Clone()
    {
        return new Pawn
        {
            Color = this.Color,
            HasMoved = this.HasMoved
        };
    }
}
