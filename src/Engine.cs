
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
        legalMoves.Sort((a, b) => ScoreMove(board, b).CompareTo(ScoreMove(board, a)));
        
        foreach(Move move in legalMoves)
        {
            //Board tempBoard = board.Clone();
            //GameLogic.ApplyMove(tempBoard, move);
            GameLogic.ApplyMove(board, move);

            float score = AlphaBeta(board, Depth - 1, float.MinValue, float.MaxValue, board.CurrentTurn, rootSide);
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
    
    float AlphaBeta(Board board, int depth, float alpha, float beta, PieceColor side, PieceColor rootSide)
    {
        if(depth == 0 || GameLogic.IsGameOver(board))
            return Evaluate(board, rootSide);
        
        if(side == rootSide)
        {
            float maxEval = float.MinValue;
            
            List<Move> legalMoves = GameLogic.GetAllLegalMoves(board).ToList();
            legalMoves.Sort((a, b) => ScoreMove(board, b).CompareTo(ScoreMove(board, a)));
            
            foreach(Move move in legalMoves)
            {
                //Board tempboard = board.Clone();
                GameLogic.ApplyMove(board, move);
                float eval = AlphaBeta(board, depth - 1, alpha, beta, board.CurrentTurn, rootSide);
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
            
            List<Move> legalMoves = GameLogic.GetAllLegalMoves(board).ToList();
            legalMoves.Sort((a, b) => ScoreMove(board, b).CompareTo(ScoreMove(board, a)));
            
            foreach(Move move in legalMoves)
            {
                //Board tempboard = board.Clone();
                GameLogic.ApplyMove(board, move);
                float eval = AlphaBeta(board, depth - 1, alpha, beta, board.CurrentTurn, rootSide);
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
        if(GameLogic.IsCheckmate(board))
            return board.CurrentTurn == side ? -10000 : 10000;
            
        if(GameLogic.IsStalemate(board))
            return 0;

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

    float ScoreMove(Board board, Move move)
    {
        if(board.pieces[move.To.Col, move.To.Row] == null)
            return 0;

        //example white pawn captures black knight: -1 + 5 = 4
  
        float captureBalance = -board.pieces[move.From.Col, move.From.Row]!.Value - board.pieces[move.To.Col, move.To.Row]!.Value;

        return board.CurrentTurn == PieceColor.White ? captureBalance + 1 : -captureBalance + 1; //add 1 so that equal captures score over non-captures
    }
}
