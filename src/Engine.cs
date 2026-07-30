
public class Engine
{
    const int MaxTranspositionEntries = 250_000;
    const float MateScore = 100_000;
    readonly Dictionary<ulong, TranspositionEntry> transpositionTable = new(MaxTranspositionEntries);
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

    enum Bound
    {
        Exact,
        Lower,
        Upper
    }

    readonly record struct TranspositionEntry(int Depth, float Score, Bound Bound, Move BestMove, bool HasBestMove);

    public Move GetBestMove(Board board)
    {
        transpositionTable.Clear();
        Move? bestMove = null;
        float bestScore = float.MinValue;
        
        PieceColor rootSide = board.CurrentTurn;
        List<Move> legalMoves = GameLogic.GetAllLegalMoves(board).ToList();
        OrderMoves(board, legalMoves, null);
        
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
        ulong positionKey = GetPositionKey(board);
        Move? hashMove = null;
        if (transpositionTable.TryGetValue(positionKey, out TranspositionEntry entry))
        {
            if (entry.HasBestMove)
                hashMove = entry.BestMove;

            if (entry.Depth >= depth)
            {
                if (entry.Bound == Bound.Exact)
                    return entry.Score;

                if (entry.Bound == Bound.Lower)
                    alpha = Math.Max(alpha, entry.Score);
                else
                    beta = Math.Min(beta, entry.Score);

                if (alpha >= beta)
                    return entry.Score;
            }
        }

        float originalAlpha = alpha;
        float originalBeta = beta;

        // Generate once per node. Previously, IsGameOver and Evaluate could each
        // generate the full legal-move list before the search generated it again.
        List<Move> legalMoves = GameLogic.GetAllLegalMoves(board).ToList();

        if (legalMoves.Count == 0)
        {
            float score = !GameLogic.IsCheck(board, board.CurrentTurn)
                ? 0
                // Prefer a faster mate, and delay a forced loss when possible.
                : board.CurrentTurn == rootSide ? -MateScore - depth : MateScore + depth;
            Store(positionKey, new(depth, score, Bound.Exact, default, false));
            return score;
        }

        if (depth == 0)
        {
            float score = Evaluate(board, rootSide);
            Store(positionKey, new(depth, score, Bound.Exact, default, false));
            return score;
        }

        OrderMoves(board, legalMoves, hashMove);

        Move bestMove = default;
        bool hasBestMove = false;
        float result;
        if (board.CurrentTurn == rootSide)
        {
            float maxEval = float.MinValue;

            foreach (Move move in legalMoves)
            {
                GameLogic.ApplyMove(board, move);
                float eval = AlphaBeta(board, depth - 1, alpha, beta, rootSide);
                board.Undo();
                if (eval > maxEval)
                {
                    maxEval = eval;
                    bestMove = move;
                    hasBestMove = true;
                }
                alpha = Math.Max(alpha, eval);                 
        
                if(beta <= alpha)
                    break;
                
            }

            result = maxEval;
        }
        else
        {
            float minEval = float.MaxValue;

            foreach (Move move in legalMoves)
            {
                GameLogic.ApplyMove(board, move);
                float eval = AlphaBeta(board, depth - 1, alpha, beta, rootSide);
                board.Undo();
                if (eval < minEval)
                {
                    minEval = eval;
                    bestMove = move;
                    hasBestMove = true;
                }
                beta = Math.Min(beta, eval);
                
                if(beta <= alpha)
                    break;
                  
            }

            result = minEval;
        }

        Bound bound = result <= originalAlpha ? Bound.Upper : result >= originalBeta ? Bound.Lower : Bound.Exact;
        Store(positionKey, new(depth, result, bound, bestMove, hasBestMove));
        return result;
    }
    
    float Evaluate(Board board, PieceColor side)
    {
        float finalScore = 0;
        
        //Coordinate[] squaresAttackedByWhite = GameLogic.GetAllAttackedSquares(board, PieceColor.White).ToArray();
        //Coordinate[] squaresAttackedByBlack = GameLogic.GetAllAttackedSquares(board, PieceColor.Black).ToArray();
        
        finalScore += GameLogic.GetMaterialBalance(board);
        finalScore += GetPositionalScore(board);
        finalScore += GetCenterPawnScore(board, PieceColor.White);
        finalScore -= GetCenterPawnScore(board, PieceColor.Black);
        finalScore += GetKingFafetyScore(board, PieceColor.White);
        finalScore -= GetKingFafetyScore(board, PieceColor.Black);
        
        return side == PieceColor.White ? finalScore : -finalScore; 
    }
    
    static float GetPositionalScore(Board board)
    {
        float score = 0;

        for (int col = 0; col < 8; col++)
        {
            for (int row = 0; row < 8; row++)
            {
                if (board.pieces[col, row] is not Piece piece)
                    continue;

                // Centralization is useful for minor pieces and queens, and is
                // much cheaper to evaluate than rebuilding complete attack maps.
                float centerBonus = (3.5f - Math.Abs(col - 3.5f)) + (3.5f - Math.Abs(row - 3.5f));
                float pieceBonus = piece switch
                {
                    Knight or Bishop => centerBonus * 0.015f,
                    Queen => centerBonus * 0.005f,
                    _ => 0
                };
                score += piece.Color == PieceColor.White ? pieceBonus : -pieceBonus;
            }
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

    void OrderMoves(Board board, List<Move> moves, Move? hashMove)
    {
        moves.Sort((a, b) => ScoreMove(board, b, hashMove).CompareTo(ScoreMove(board, a, hashMove)));
    }

    static int ScoreMove(Board board, Move move, Move? hashMove)
    {
        if (hashMove.HasValue && move == hashMove.Value)
            return int.MaxValue;

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

    void Store(ulong positionKey, TranspositionEntry entry)
    {
        if (transpositionTable.Count >= MaxTranspositionEntries && !transpositionTable.ContainsKey(positionKey))
            transpositionTable.Clear();

        transpositionTable[positionKey] = entry;
    }

    static ulong GetPositionKey(Board board)
    {
        const ulong offsetBasis = 14695981039346656037;
        const ulong prime = 1099511628211;
        ulong hash = offsetBasis;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Piece? piece = board.pieces[col, row];
                int pieceCode = piece == null ? 0 : GetPieceCode(piece);
                hash ^= (ulong)(pieceCode + 31 * (row * 8 + col));
                hash *= prime;
            }
        }

        hash ^= (ulong)(board.CurrentTurn == PieceColor.White ? 1 : 2);
        hash *= prime;
        hash ^= (ulong)(board.EnPassantCoordinate?.Col + 1 ?? 0);
        hash *= prime;
        hash ^= (ulong)(board.EnPassantCoordinate?.Row + 1 ?? 0);
        return hash * prime;
    }

    static int GetPieceCode(Piece piece)
    {
        int pieceType = piece switch
        {
            Pawn => 1,
            Knight => 2,
            Bishop => 3,
            Rook => 4,
            Queen => 5,
            King => 6,
            _ => throw new InvalidOperationException("Unknown piece type")
        };

        return pieceType | (piece.Color == PieceColor.Black ? 8 : 0) | (piece.HasMoved ? 16 : 0);
    }
}
