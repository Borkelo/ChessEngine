#pragma warning disable CS8618, CA1050
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ChessEngine.Properties;

// TODO
// Migrate Image types to Bitmap
// Change Resource images to SVG
// XML summaries

/// <summary>
/// A simple WinForms program that displays a graphical user interface for the program.
/// </summary>
public partial class GraphicalBoard : Form
{
    private readonly Board board;
    private Label[,] squares;
    private const int squareSize = 70;
    private Coordinate? selectedSquare;
    private List<Move> legalMoves = [];

    private readonly Dictionary<char, Image> whitePieces = [];
    private readonly Dictionary<char, Image> blackPieces = [];

    private Button undo;
    private Button end;

    private readonly Program.Mode mode;
    private readonly PieceColor? humanSide;
    private readonly bool flipBoard;
    private bool isEngineCalculating;
    private Point? drag = null;
    private Label status;
    private TextBox balance;

    private readonly Color primaryColor = Color.Wheat;
    private readonly Color secondaryColor = Color.BurlyWood;

    private Coordinate ToInternalCoordinate(Coordinate gui) => GetCoordinate(gui);
    private Coordinate ToGraphicCoordinate(Coordinate @internal) => GetCoordinate(@internal);
    private Coordinate GetCoordinate(Coordinate coord)
    {
        if (!flipBoard)
            return new Coordinate(7 - coord.Col, 7 - coord.Row);
        return coord;
    }

    public GraphicalBoard(Board _board, Program.Mode _mode, PieceColor? _humanSide = null)
    {
        board = _board;
        mode = _mode;
        humanSide = _humanSide;

        flipBoard = IsPlayingAsBlack();

        GeneratePieces();
        GenerateBoard();
        
        foreach (var square in squares!)
        {
            square.MouseDown += InputMouseDown;
            square.MouseMove += InputMouseMove;
            square.DragOver += InputDragOver;
            square.DragDrop += InputDragDrop;
            square.MouseUp += InputMouseUp;
            square.AllowDrop = true;
        }

        if (ShouldEngineMove())
            HandleCreated += (s, e) 
            => Task.Run(MakeEngineMove);
    }

    private bool IsPlayingAsBlack() => mode == Program.Mode.pVe && humanSide == PieceColor.Black;
    private bool ShouldEngineMove() => mode == Program.Mode.eVe || (mode == Program.Mode.pVe && board.CurrentTurn != humanSide);
    protected override void OnHandleCreated(EventArgs e) => base.OnHandleCreated(e);
    private static FontFamily GetFont() => SystemFonts.DefaultFont.FontFamily;
    private void End(object? sender, EventArgs e) => Close();

    private void GenerateBoard()
    {
        ClientSize = new(680, 800);
        MinimumSize = new(400, 400);
        Text = "Chess Board";
        BackColor = Color.White;

        undo = new Button { 
            Text = "Undo", 
            Size = new(80, 30) };
        undo.Click += Undo;
        Controls.Add(undo);

        end = new Button { 
            Text = "End Game", 
            Size = new(80, 30) };
        end.Click += End;
        Controls.Add(end);

        balance = new()
        {
            ReadOnly = true,
            Text = "",
            Font = new Font(family: GetFont(), 14),
            Size = new(80, 30),
            ForeColor = Color.Black,
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
        };
        Controls.Add(balance);

        status = new()
        {
            Text = "",
            Font = new Font(family: GetFont(), 36, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter,
            Anchor = AnchorStyles.None,
            BackColor = Color.White
        };
        Controls.Add(status);
        status.BringToFront();

        squares = new Label[8, 8];
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Label square = new()
                {
                    Size = new(10, 10),
                    Tag = new Coordinate(col, row),
                    BackColor = (row + col) % 2 == 0 ? primaryColor : secondaryColor,
                    Font = new(GetFont(), 20),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                squares[col, row] = square;
                Controls.Add(square);
                square.BringToFront();
            }
        }

        OnResize(EventArgs.Empty);
        UpdateBoard();
    }

    private void GeneratePieces()
    {
        if (whitePieces.Count != 0)
            return;

        // TODO: See if VS allows Bitmap or Image resources directly.
        whitePieces['P'] = FromByteArray(Resources.WhitePawn) ?? CreateTextImage('P');
        whitePieces['R'] = FromByteArray(Resources.WhiteRook) ?? CreateTextImage('R');
        whitePieces['N'] = FromByteArray(Resources.WhiteKnight) ?? CreateTextImage('N');
        whitePieces['B'] = FromByteArray(Resources.WhiteBishop) ?? CreateTextImage('B');
        whitePieces['Q'] = FromByteArray(Resources.WhiteQueen) ?? CreateTextImage('Q');
        whitePieces['K'] = FromByteArray(Resources.WhiteKing) ?? CreateTextImage('K');

        blackPieces['p'] = FromByteArray(Resources.BlackPawn) ?? CreateTextImage('p');
        blackPieces['r'] = FromByteArray(Resources.BlackRook) ?? CreateTextImage('r');
        blackPieces['n'] = FromByteArray(Resources.BlackKnight) ?? CreateTextImage('n');
        blackPieces['b'] = FromByteArray(Resources.BlackBishop) ?? CreateTextImage('b');
        blackPieces['q'] = FromByteArray(Resources.BlackQueen) ?? CreateTextImage('q');
        blackPieces['k'] = FromByteArray(Resources.BlackKing) ?? CreateTextImage('k');
    }

    private static Bitmap? FromByteArray(byte[] data)
    {
        using var ms = new MemoryStream(data);
        return Image.FromStream(ms, false).Clone() as Bitmap;
    }

    private const int diff = 8;
    private static Bitmap CreateTextImage(char symbol)
    {
        var bmp = new Bitmap(squareSize - diff, squareSize - diff);
        using var g = Graphics.FromImage(bmp);
        using var font = new Font(GetFont(), 24);
         
        g.Clear(Color.Transparent);
        Brush brush = char.IsUpper(symbol) ? Brushes.LightGray : Brushes.Black;
        g.DrawString(symbol.ToString(), font, brush, new PointF(5, 5));
        return bmp;
    }

    private void UpdateBoard()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Coordinate i = ToInternalCoordinate(new(col, row));
                var piece = board.pieces[i.Col, i.Row];

                var square = squares[col, row];
                square.Image = null;
                square.Text = "";

                if (piece != null)
                {
                    char symbol = piece.AsciiSymbol;
                    var images = char.IsUpper(symbol) ? whitePieces : blackPieces;

                    if (images.TryGetValue(symbol, out Image? image))
                    {
                        int size = Math.Max(square.Width - 8, 10);
                        square.Image = ResizeImage(image, size, size);
                    }
                    else
                    {
                        square.Text = symbol.ToString();
                    }
                }
            }
        }

        UpdateTextBox();
        CheckGameStatus();
        Refresh();
    }

    /// <summary>
    /// Callback method for mouse clicks
    /// </summary>
    private void InputMouseDown(object? sender, MouseEventArgs e)
    {
        drag = e.Location;

        if (ShouldEngineMove())
        {
            return;
        }

        ClearIndicators();

        var square = sender as Label;
        if (square?.Tag == null) 
            return;

        var coord = ToInternalCoordinate((Coordinate)square.Tag);
        var piece = board.pieces[coord.Col, coord.Row];

        if (selectedSquare.HasValue)
        {
            Move move = new(selectedSquare.Value, coord);
            if (legalMoves.Contains(move))
            {
                ApplyMove(move);
                selectedSquare = null;
                ClearIndicators();
                drag = null;
                return;
            }

            if (piece != null && piece.Color == board.CurrentTurn &&
                (mode != Program.Mode.pVe || piece.Color == humanSide))
            {
                selectedSquare = coord;
                legalMoves = [.. GameLogic.GetAllLegalMoves(board).Where(m => m.From == coord)];
                ShowLegalMoves();
            }
            else
            {
                selectedSquare = null;
                ClearIndicators();
            }

            drag = null;
            return;
        }

        if (piece == null)
            return;

        if ((mode == Program.Mode.pVe && piece.Color != humanSide) || 
            (mode == Program.Mode.pVp && piece.Color != board.CurrentTurn))
        {
            return;
        }

        selectedSquare = coord;
        legalMoves = [.. GameLogic.GetAllLegalMoves(board).Where(m => m.From == coord)];

        ShowLegalMoves();
    }

    private void UpdateTextBox()
    {
        balance.Text = $"{GameLogic.GetMaterialBalance(board)}";
    }

    /// <summary>
    /// Renders translucent dots on the centers of squares with legal moves for the selected piece.
    /// </summary>
    private void ShowLegalMoves()
    {
        ClearIndicators();
        foreach (var move in legalMoves)
        {
            var to = ToGraphicCoordinate(move.To);
            var tar = squares[to.Col, to.Row];
            int size = tar.Width / 4;

            Label indicator = new()
            {
                Size = new Size(size, size),
                BackColor = Color.FromArgb(100, Color.LightBlue),
                Location = new Point(
                    tar.Left + (tar.Width - size) / 2,
                    tar.Top + (tar.Height - size) / 2
                ),
                Enabled = false
            };

            Controls.Add(indicator);
            indicator.BringToFront();
        }
    }

    private void CheckGameStatus()
    {
        if (GameLogic.IsCheckmate(board))
            status.Text = $"Checkmate - {Piece.OppositeColor(board.CurrentTurn)} wins!";
        else if (GameLogic.IsStalemate(board))
            status.Text = "Stalemate!";
        else if (GameLogic.IsDraw(board))
            status.Text = "Draw";
        else
            status.Text = "";

        status.BringToFront();
        status.Visible = !string.IsNullOrEmpty(status.Text);
    }

    /// <summary>
    /// Clears the legal move indicators from the board.
    /// </summary>
    private void ClearIndicators()
    {
        var indicators = Controls.OfType<Label>()
            .Where(l => l.BackColor == Color.FromArgb(100, Color.LightBlue))
            .ToList();

        foreach (var i in indicators)
        {
            Controls.Remove(i);
            i.Dispose();
        }
    }

    /// <summary>
    /// Callback method for moving a piece with the mouse
    /// </summary>
    private void InputMouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || !selectedSquare.HasValue || !drag.HasValue)
            return;

        if (Math.Abs(e.X - drag.Value.X) < SystemInformation.DragSize.Width &&
            (Math.Abs(e.Y - drag.Value.Y) < SystemInformation.DragSize.Height))
        {
            return;
        }

        var square = sender as Label;
        if (square?.Tag == null) 
            return;

        square.DoDragDrop(selectedSquare.Value, DragDropEffects.Move);
        drag = null;
    }

    private void InputDragOver(object? sender, DragEventArgs e) => e.Effect = DragDropEffects.Move;
    private void InputMouseUp(object? sender, MouseEventArgs e) => drag = null;

    /// <summary>
    /// Callback method for drag-and-drop of pieces
    /// </summary>
    private void InputDragDrop(object? sender, DragEventArgs e)
    {
        var tar = sender as Label;
        if (tar?.Tag == null) 
            return;

        var coord = ToInternalCoordinate((Coordinate)tar.Tag);

        if (!selectedSquare.HasValue)
            return;

        var move = new Move(selectedSquare.Value, coord);
        if (legalMoves.Contains(move))
            ApplyMove(move);

        selectedSquare = null;
        ClearIndicators();
    }

    /// <summary>
    /// Applies a move, regardless of its legality (?), first checking for and applying a pawn promotion and then updating the board.
    /// </summary>
    private void ApplyMove(Move move)
    {
        char promotion = 'q';
        if (board.pieces[move.From.Col, move.From.Row] is Pawn
            && (move.To.Row == 0 || move.To.Row == 7))
        {
            using var dialog = new PromotionDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                promotion = dialog.SelectedPiece;
            }
        }

        GameLogic.ApplyMove(board, move, promotion);
        UpdateBoard();

        if ((mode == Program.Mode.pVe && board.CurrentTurn != humanSide)
            || mode == Program.Mode.eVe)
        {
            Task.Run(MakeEngineMove);
        }
    }

    private void MakeEngineMove()
    {
        try
        {
            if (IsDisposed) 
                return;

            Invoke(() => undo.Enabled = false);
            isEngineCalculating = true;

            var engine = new Engine(4);
            var move = engine.GetBestMove(board);

            if (IsDisposed) 
                return;

            Invoke(() =>
            {
                if (IsDisposed) 
                    return;

                GameLogic.ApplyMove(board, move);
                UpdateBoard();

                undo.Enabled = true;
                isEngineCalculating = false;

                if (!GameLogic.IsGameOver(board) &&
                    (mode == Program.Mode.eVe ||
                    (mode == Program.Mode.pVe && board.CurrentTurn != humanSide)))
                {
                    Task.Run(MakeEngineMove);
                }
            });
        }
        catch (InvalidOperationException)
        {
            Invoke(() => undo.Enabled = true);
            isEngineCalculating = false;
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (squares == null) 
            return;

        int area = Math.Min(ClientSize.Width, ClientSize.Height - 100);
        int size = area / 8;
        int xPos = (ClientSize.Width - area) / 2;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var square = squares[col, row];

                square.Size = new(size, size);
                square.Location = new(
                    xPos + col * size,
                    row * size
                );
            }
        }

        undo.Location = new(10, area + 10);
        end.Location = new(100, area + 10);
        balance.Location = new(200, area + 10);
        balance.BringToFront();
        status.Location = new(xPos + (area - status.Width) / 2, (area - status.Height) / 2);
        status.BringToFront();
        
        UpdateBoard();
    }

    /// <summary>
    /// Undoes the last move by reverting to the previous move in the move history.
    /// </summary>
    private void Undo(object? sender, EventArgs e)
    {
        if (isEngineCalculating)
            return;

        if (board.moveHistory.Count <= 0)
            return;

        board.Undo();

        if (mode == Program.Mode.pVe && board.moveHistory.Count > 0)
        {
            board.Undo();
        }

        UpdateBoard();
        status.Text = "";

        if (mode == Program.Mode.eVe ||
            (mode == Program.Mode.pVe && board.CurrentTurn != humanSide))
        {
            Task.Run(MakeEngineMove);
        }
    }

    private static Bitmap ResizeImage(Image image, int width, int height)
    {
        var rect = new Rectangle(0, 0, width, height);
        var bmp = new Bitmap(width, height);

        using (var graphics = Graphics.FromImage(bmp))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using var wrapMode = new ImageAttributes();
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
        }

        return bmp;
    }
}