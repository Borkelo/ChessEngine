
public class Engine
{
    int Depth { get; set; }

    public Engine(int depth)
    {
        Depth = depth;
    }
    static readonly Coordinate[] centerSquares =
    {
        new(3, 3),
        new(3, 4),
        new(4, 3),
        new(4, 4)
    };
    
    static readonly int[] postCastleKingColumns = [0, 1, 2, 6, 7];

    public Move GetBestMove(Board board)
    {
        Move? bestMove = null;
        float bestScore = float.MinValue;
        
        PieceColor rootSide = board.CurrentTurn;
        List<Move> legalMoves = GameLogic.GetAllLegalMoves(board).ToList();
        OrderMoves(board, legalMoves);
        
        foreach(Move move in legalMoves)
        {
            //Board tempBoard = board.Clone();
            //GameLogic.ApplyMove(tempBoard, move);
            GameLogic.ApplyMove(board, move);

            float score = AlphaBeta(board, Depth - 1, float.MinValue, float.MaxValue, rootSide);
            //Console.WriteLine($"Move {move} scored {score}");
            if(score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                Console.WriteLine($"New best move: {move} with score {score}");
            }
            board.Undo();
        }
        
        Console.WriteLine($"Chosen move: {bestMove} with score {bestScore}");
        return bestMove ?? throw new Exception("No legal moves found");
    }
    
    float AlphaBeta(Board board, int depth, float alpha, float beta, PieceColor rootSide)
    {
        // Generate once per node. Previously, IsGameOver and Evaluate could each
        // generate the full legal-move list before the search generated it again.
        List<Move> legalMoves = GameLogic.GetAllLegalMoves(board).ToList();

        if (legalMoves.Count == 0)
        {
            if (!GameLogic.IsCheck(board, board.CurrentTurn))
                return 0;

            // Prefer a faster mate, and delay a forced loss when possible.
            const float mateScore = 100000;
            return board.CurrentTurn == rootSide ? -mateScore - depth : mateScore + depth;
        }

        if (depth == 0)
            return Evaluate(board, rootSide);

        OrderMoves(board, legalMoves);

        if (board.CurrentTurn == rootSide)
        {
            float maxEval = float.MinValue;

            foreach (Move move in legalMoves)
            {
                GameLogic.ApplyMove(board, move);
                float eval = AlphaBeta(board, depth - 1, alpha, beta, rootSide);
                board.Undo();
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);                 
        
                if(beta <= alpha)
                    break;
                
            }

            return maxEval;
        }
        else
        {
            float minEval = float.MaxValue;

            foreach (Move move in legalMoves)
            {
                GameLogic.ApplyMove(board, move);
                float eval = AlphaBeta(board, depth - 1, alpha, beta, rootSide);
                board.Undo();
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                
                if(beta <= alpha)
                    break;
                  
            }

            return minEval;
        }
    }
    
    float Evaluate(Board board, PieceColor side)
    {
        float finalScore = 0;
        
        //Coordinate[] squaresAttackedByWhite = GameLogic.GetAllAttackedSquares(board, PieceColor.White).ToArray();
        //Coordinate[] squaresAttackedByBlack = GameLogic.GetAllAttackedSquares(board, PieceColor.Black).ToArray();
        
        finalScore += GameLogic.GetMaterialBalance(board);
        finalScore += GetAttackedSquaresScore(board, PieceColor.White);
        finalScore -= GetAttackedSquaresScore(board, PieceColor.Black);
        finalScore += GetCenterPawnScore(board, PieceColor.White);
        finalScore -= GetCenterPawnScore(board, PieceColor.Black);
        finalScore += GetKingFafetyScore(board, PieceColor.White);
        finalScore -= GetKingFafetyScore(board, PieceColor.Black);
        
        return side == PieceColor.White ? finalScore : -finalScore; 
    }
    
    float GetAttackedSquaresScore(Board board, PieceColor side)
    {
        float score = 0;
        
        foreach(Coordinate square in GameLogic.GetAllAttackedSquares(board, side))
        {
            score += 0.001f;
        }
        return score;
    }
    
    float GetCenterPawnScore(Board board, PieceColor side)
    {
        float score = 0;
        
        foreach (var coord in centerSquares)
        {
            if (board.pieces[coord.Col, coord.Row] is Pawn pawn && pawn.Color == side)
                score += 0.02f;
        }
        
        return score;
    }
    
    float GetKingFafetyScore(Board board, PieceColor side)
    {
        if(side == PieceColor.White)
        {
            foreach(int col in postCastleKingColumns)
            {
                if(board.pieces[col, 0] is King king && king.Color == PieceColor.White)
                    return 0.15f;
            }
        }
        else
        {
            foreach(int col in postCastleKingColumns)
            {
                if(board.pieces[col, 7] is King king && king.Color == PieceColor.Black)
                    return 0.15f;
            }
        }

        return 0;
    }

    void OrderMoves(Board board, List<Move> moves)
    {
        moves.Sort((a, b) => ScoreMove(board, b).CompareTo(ScoreMove(board, a)));
    }

    static int ScoreMove(Board board, Move move)
    {
        Piece movingPiece = board.pieces[move.From.Col, move.From.Row]!;
        Piece? capturedPiece = board.pieces[move.To.Col, move.To.Row];

        // En passant captures a pawn although the destination square is empty.
        int capturedValue = capturedPiece == null
            ? movingPiece is Pawn && board.EnPassantCoordinate == move.To ? 1 : 0
            : Math.Abs(capturedPiece.Value);

        int score = capturedValue == 0 ? 0 : capturedValue * 100 - Math.Abs(movingPiece.Value);

        if (movingPiece is Pawn && move.To.Row is 0 or 7)
            score += 800;

        return score;
    }
}
