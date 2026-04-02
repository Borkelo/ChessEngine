public static class GameLogic
{
    public static void ApplyMove(Board board, Move move, char promotionPieceSymbol = 'q') //THIS METHOD APPLIES MOVE EVEN IF ITS ILLEGAL
    {

        Board.GenerateMoveHinstoryEntry(board, move);                  
        Piece pieceToMove = board.pieces[move.From.Col, move.From.Row]!;
        Piece? pieceToCapture = board.pieces[move.To.Col, move.To.Row];
        int pawnDirection = board.CurrentTurn == PieceColor.White ? 1 : -1; //Direction the current side pawns move
        
        if(board.CurrentTurn == PieceColor.Black)
            board.FullmoveCounter++;
            
        if(pieceToCapture == null && pieceToMove is not Pawn)
            board.HalfmoveClock++;     
        else
            board.HalfmoveClock = 0;
        
        //Check if move is an en passant
        if(move.To == board.EnPassantCoordinate && pieceToMove is Pawn)
        {
            Coordinate pieceToCaptureCoord = new(move.To.Col, move.To.Row - pawnDirection);
            board.pieces[pieceToCaptureCoord.Col, pieceToCaptureCoord.Row] = null;
        }
        
        if(CheckEnPassantOpportunity(pieceToMove, move, board, pawnDirection, out Coordinate target))
            board.EnPassantCoordinate = target;
        else
            board.EnPassantCoordinate = null;
            
        board.pieces[move.To.Col, move.To.Row] = pieceToMove;
        board.pieces[move.From.Col, move.From.Row] = null;
        pieceToMove.HasMoved = true;
        
        //Castling
        if(pieceToMove is King && Math.Abs(move.From.Col - move.To.Col) > 1)
        {
            if(move.To.Col == 6) //Kingside
            {
                board.pieces[5, move.To.Row] = board.pieces[7, move.From.Row];
                board.pieces[7, move.From.Row] = null;
                board.pieces[5, move.To.Row]!.HasMoved = true; 
            }
            else if(move.To.Col == 2) //Queenside
            {
                board.pieces[3, move.To.Row] = board.pieces[0, move.From.Row];
                board.pieces[0, move.From.Row] = null;
                board.pieces[3, move.To.Row]!.HasMoved = true;
            }
        }                
        //Promotion
        if(pieceToMove is Pawn && (move.To.Row == 0 || move.To.Row == 7)) 
        {
            Piece promotionPiece = Piece.GetPieceFromSymbol(promotionPieceSymbol); //todo: constructor for piece
            promotionPiece.Color = board.CurrentTurn;
            board.pieces[move.To.Col, move.To.Row] = promotionPiece;         
        }

        board.CurrentTurn = Piece.OppositeColor(board.CurrentTurn);   
               
    }
    
    public static bool IsLegalMove(Board board, Move move)
    {
        if (move.From == move.To)
            return false;
        
        if(!(IsOnBoard(move.From) && IsOnBoard(move.To)))
            return false;
    
        if(board.pieces[move.From.Col, move.From.Row] == null)
            return false;     

        Piece piece = board.pieces[move.From.Col, move.From.Row]!;

        if (piece.Color != board.CurrentTurn)
            return false;
        
        if (!piece.GetLegalMoves(board, move.From).Contains(move))
            return false;

        return true;
    }
    
    public static bool IsLegalPosition(Board board)
    {
        return true;
    }
    
    //returns if given side is checked
    public static bool IsCheck(Board board, PieceColor sideChecked)//todo check from kings position towards sliding direction + knight positions
    {
        PieceColor sideChecking = Piece.OppositeColor(sideChecked);
        foreach(Coordinate attackedSquare in GetAllAttackedSquares(board, sideChecking))
        {
            if(board.pieces[attackedSquare.Col, attackedSquare.Row] is King king && king.Color == sideChecked)
            {
                return true;
            }
        }
        return false;
    }
    
    public static bool IsStalemate(Board board)
    {
        return !GetAllLegalMoves(board).Any() && !IsCheck(board, board.CurrentTurn);
    }
    
    public static bool IsCheckmate(Board board)
    {
        return IsCheck(board, board.CurrentTurn) && !GetAllLegalMoves(board).Any();
    }
    
    public static bool IsGameOver(Board board)
    {
        return IsStalemate(board) || IsCheckmate(board);
    }
    
    public static bool IsDraw(Board board)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if(board.pieces[i, j] is Piece piece && piece is not King)
                {
                    return false;
                }                           
            }
        }
        return true;
    }
    
    public static bool IsSquareAttacked(Board board, Coordinate coordinate, PieceColor attackingSide)
    {
        foreach(Coordinate attackedSquare in GetAllAttackedSquares(board, attackingSide))
        {
            if(attackedSquare == coordinate)
            {
                return true;
            }
        }
        return false;
    }
    
    public static IEnumerable<Coordinate> GetAllAttackedSquares(Board board, PieceColor attackingSide)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if(board.pieces[i, j] is Piece piece && piece.Color == attackingSide)
                {
                    Coordinate from = new(i, j);
                    
                    foreach(Move move in piece.GetAttackingSquares(board, from))
                    {
                        yield return move.To;
                    }
                }                           
            }
        }
    }
    
    public static IEnumerable<Move> GetAllLegalMoves(Board board)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if(board.pieces[i, j] is Piece piece && piece.Color == board.CurrentTurn)
                {
                    foreach(Move move in piece.GetLegalMoves(board, new(i, j)))
                    {
                        yield return move;
                    }   
                }                           
            }
        }
    }
    
    public static float GetMaterialBalance(Board board)
    {
        float num = 0;
        
        for(int i = 0; i < 8; i++)
        {
            for(int j = 0; j < 8; j++)
            {
                if(board.pieces[i, j] == null)
                    continue;

                num += board.pieces[i, j]!.Value;
            }
        }

        return num;
    }
    
    static bool CheckEnPassantOpportunity(Piece pieceToMove, Move move, Board board, int pawnDirection, out Coordinate target)
    {
        target = default;
        
        if(pieceToMove is not Pawn)
            return false;
        
        if(Math.Abs(move.From.Row - move.To.Row) != 2)
            return false;
                
        int[] adjacentCols = [move.To.Col - 1, move.To.Col + 1];

        foreach (int col in adjacentCols)
        {
            if (col < 0 || col >= 8)
                continue;

            if (board.pieces[col, move.To.Row] is Pawn adjacentPawn && adjacentPawn.Color != board.CurrentTurn)
            {
                int targetRow = move.To.Row - pawnDirection;
                target = new Coordinate(move.To.Col, targetRow);
                return true;
            }
        }
            
        return false;
    }
    public static bool IsOnBoard(Coordinate coord)
    {
        return coord.Col is >= 0 and <= 7 && coord.Row is >= 0 and <= 7;
    }

    public static IEnumerable<Move> GetSlidingMoves(Board board, Coordinate from, Coordinate[] directions, int maxDistance)
    {
        PieceColor color = board.pieces[from.Col, from.Row]!.Color;
        
        foreach(var direction in directions)
        {
            int distance = 1;     
            while(distance <= maxDistance)
            {
                Coordinate to = from + direction * distance;
                
                if(!IsOnBoard(to))
                    break;
                       
                if(board.pieces[to.Col, to.Row] is Piece piece)
                {
                    if(piece.Color == color)
                        break;

                    yield return new(from, to);      
                    break;
                }
                
                yield return new(from, to); 
                distance++;
            }
        }
    }
}