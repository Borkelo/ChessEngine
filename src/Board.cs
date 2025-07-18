public class Board
{
    public Piece?[,] pieces = new Piece?[8, 8];
    public Stack<MoveHistoryEntry> moveHistory = new();
    public List<Coordinate> WhitePieceCoords { get; private set; } = new();
    public List<Coordinate> BlackPieceCoords { get; private set; } = new();
    public PieceColor CurrentTurn { get; set; } = PieceColor.White;
    public Coordinate? EnPassantCoordinate { get; set; } = null;
    public int HalfmoveClock { get; set; } = 0;
    public int FullmoveCounter { get; set; } = 1;
    public const string DefaultFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    
    public Board(string FEN = DefaultFEN)
    {
        SetUpFromFEN(FEN);
    }
    
    public void SetUpFromFEN(string FEN)
    {
        string[] splitFEN = FEN.Split(' ');
        
        CurrentTurn = splitFEN[1] == "w" ? PieceColor.White : PieceColor.Black;
        HalfmoveClock = int.Parse(splitFEN[4]);
        FullmoveCounter = int.Parse(splitFEN[5]);
        EnPassantCoordinate = splitFEN[3] == "-" ? null : new Coordinate(splitFEN[3]);
        
        string FENpieces = splitFEN[0];        
        string[] rows = FENpieces.Split('/');   
        
        pieces = new Piece?[8, 8];
         
        //So that white is at row 0
        int y = 7;
        
        for(int i = 0; i < rows.Length; i++)
        {
            int x = 0;
  
            for(int j = 0; j < rows[i].Length; j++)
            {
                if(int.TryParse(rows[i][j].ToString(), out int value))
                {
                    x += value;                   
                }
                else
                {
                    Piece piece = Piece.GetPieceFromSymbol(rows[i][j]);
                    piece.Color = char.IsUpper(rows[i][j]) ? PieceColor.White : PieceColor.Black;
                    
                    if(piece is Pawn && (y == 1 || y == 6))
                        piece.HasMoved = false;
                    else
                        piece.HasMoved = true;
                    
                    pieces[x, y] = piece;
                    
                    x++;
                }
            }
            
            y--;
        }

        string castleString = splitFEN[2];
        if(castleString.Contains('K') || castleString.Contains('Q'))
        {
            if(pieces[4,0] is not King whiteKing)
                throw new Exception("Invalid FEN");

            whiteKing.HasMoved = false; 
            
            if(castleString.Contains('K'))
            {
                if(pieces[7,0] is not Rook rookKingside)
                    throw new Exception("Invalid FEN");

                rookKingside.HasMoved = false;
            }
            if(castleString.Contains('Q'))
            {
                if(pieces[0,0] is not Rook rookQueenSide)
                    throw new Exception("Invalid FEN");
            
                rookQueenSide.HasMoved = false;
            }
        }
        if(castleString.Contains('k') || castleString.Contains('q'))
        {
            if(pieces[4,7] is not King blackKing)
                throw new Exception("Invalid FEN");
            
            blackKing.HasMoved = false;
            
            if(castleString.Contains('k'))
            {
                if(pieces[7,7] is not Rook rookKingside)
                    throw new Exception("Invalid FEN");
                
                rookKingside.HasMoved = false;
            }
            if(castleString.Contains('q'))
            {
                if(pieces[0,7] is not Rook rookQueenSide)
                    throw new Exception("Invalid FEN");
                
                rookQueenSide.HasMoved = false;
            }
        }
    }
       
    public string GenerateFEN()
    {
        string FENpieces = "";
            
        for (int row = 7; row >= 0; row--)
        {
            int emptyCount = 0;
            
            for (int col = 0; col < 8; col++)
            {
                Piece? piece = pieces[col, row];
                      
                if(piece == null)
                {
                    emptyCount++;
                    
                    if(col == 7)
                        FENpieces += emptyCount.ToString();
                                       
                }              
                else
                {
                    if(emptyCount > 0)
                    {
                        FENpieces += emptyCount.ToString();
                        emptyCount = 0;
                    }
                        
                    FENpieces += piece.AsciiSymbol;
                }                 
            }
            if(row > 0)
                FENpieces += "/";
        }

        string FENturn = CurrentTurn == PieceColor.White ? "w" : "b";

        string castleString = "";
        
        if(pieces[4,0] is King whiteKing && !whiteKing.HasMoved)
        {
            if(pieces[7,0] is Rook rookKingside  && !rookKingside.HasMoved)
            {
                castleString += "K";
            }
            if(pieces[0,0] is Rook rookQueenSide && !rookQueenSide.HasMoved)
            {
                castleString += "Q";
            }
        }
        if(pieces[4,7] is King blackKing && !blackKing.HasMoved)
        {
            if(pieces[7,7] is Rook rookKingside  && !rookKingside.HasMoved)
            {
                castleString += "k";
            }
            if(pieces[0,7] is Rook rookQueenSide && !rookQueenSide.HasMoved)
            {
                castleString += "q";
            }
        }
        castleString = castleString == "" ? "-" : castleString;

        string FENenPassantCoordinate = EnPassantCoordinate?.ToString() ?? "-";
 
        return $"{FENpieces} {FENturn} {castleString} {FENenPassantCoordinate} {HalfmoveClock} {FullmoveCounter}";
    }
    
    public void Undo() //maybe move to gamelogic
    {
        MoveHistoryEntry entry = moveHistory.Pop();
        pieces[entry.Move.From.Col, entry.Move.From.Row] = pieces[entry.Move.To.Col, entry.Move.To.Row];
        pieces[entry.Move.To.Col, entry.Move.To.Row] = entry.CapturedPiece;
           
        EnPassantCoordinate = entry.PreviousEnPassant;
        if(entry.PreviousEnPassant != null)
        {
            if(entry.Move.To == entry.PreviousEnPassant && pieces[entry.Move.From.Col, entry.Move.From.Row] is Pawn) 
            {
                int opponentDirection = entry.PreviousTurn == PieceColor.White ? -1 : 1;
                pieces[entry.PreviousEnPassant.Value.Col, entry.PreviousEnPassant.Value.Row + opponentDirection] = new Pawn
                {
                    Color = Piece.OppositeColor(entry.PreviousTurn),
                    HasMoved = true
                }; 
            }
        }
        if(entry.IsPromotion)
        {
            pieces[entry.Move.From.Col, entry.Move.From.Row] = new Pawn
            {
                Color = entry.PreviousTurn,
                HasMoved = entry.OriginalHasMoved
            };
        }
        else
        {
            pieces[entry.Move.From.Col, entry.Move.From.Row]!.HasMoved = entry.OriginalHasMoved;
        }
        
        if(pieces[entry.Move.From.Col, entry.Move.From.Row] is King && Math.Abs(entry.Move.From.Col - entry.Move.To.Col) == 2)
        {
            //undo castling
            if(entry.Move.To.Col > entry.Move.From.Col) //kingside
            {
                pieces[7, entry.Move.To.Row] = pieces[5, entry.Move.To.Row];
                pieces[5, entry.Move.To.Row] = null;
                pieces[7, entry.Move.To.Row]!.HasMoved = false;
            }
            else //queenside
            {
                pieces[0, entry.Move.To.Row] = pieces[3, entry.Move.To.Row];
                pieces[3, entry.Move.To.Row] = null;
                pieces[0, entry.Move.To.Row]!.HasMoved = false;
            }
        }
        
        HalfmoveClock = entry.PreviousHalfmoveClock;
        FullmoveCounter = entry.PreviousFullmoveCounter;
        CurrentTurn = entry.PreviousTurn;
    }

    public void Draw()
    {
        for (int row = 7; row >= 0; row--)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(row + 1);
            
            for (int col = 0; col < 8; col++)
            {
                Console.Write(' ');
                Piece? piece = pieces[col, row];
                      
                if(piece == null)
                {
                    Console.ForegroundColor = (row + col) % 2 == 0 ? ConsoleColor.DarkGreen : ConsoleColor.Gray;
                    Console.Write('·');
                    continue;
                }      
                
                if(piece.Color == PieceColor.Black)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }          
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }
                               
                Console.Write(Program.UseUnicodeSymbols ? piece.UnicodeSymbol : piece.AsciiSymbol);
            }
            Console.WriteLine();
        }
        Console.Write("  "); 
        
        var letters = Enumerable.Range('a', 26).Select(i => (char)i).ToList();
        Console.ForegroundColor = ConsoleColor.Green;
        for (int x = 0; x < 8; x++)
        {
            Console.Write(letters[x]);
            Console.Write(' ');
        }
        Console.ResetColor();
        Console.WriteLine();      
    }

    public Board Clone() //todo: use undo instead of this for efficiency
    {
        Board newBoard = new Board();
        
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                newBoard.pieces[i, j] = this.pieces[i, j]?.Clone();
            }
        }

        newBoard.CurrentTurn = this.CurrentTurn;
        newBoard.EnPassantCoordinate = this.EnPassantCoordinate;
        newBoard.HalfmoveClock = this.HalfmoveClock;
        newBoard.FullmoveCounter = this.FullmoveCounter;

        return newBoard;
    }
    
    //Call before applying move
    public static void GenerateMoveHinstoryEntry(Board board, Move move)
    {
        MoveHistoryEntry entry = new()
        {
            Move = move,
            CapturedPiece = board.pieces[move.To.Col, move.To.Row],
            IsPromotion = board.pieces[move.From.Col, move.From.Row] is Pawn && (move.To.Row == 0 || move.To.Row == 7),
            OriginalHasMoved = board.pieces[move.From.Col, move.From.Row]!.HasMoved,
            PreviousEnPassant = board.EnPassantCoordinate,
            PreviousHalfmoveClock = board.HalfmoveClock,
            PreviousFullmoveCounter = board.FullmoveCounter,
            PreviousTurn = board.CurrentTurn
        };

        board.moveHistory.Push(entry);
    }
  
}