public class PromotionDialog : Form
{
    public char SelectedPiece { get; private set; } = 'q';

    public PromotionDialog()
    {
        Text = "Promote Pawn";
        Size = new Size(300, 100);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;

        var queen = new Button { Text = "Queen", Left = 20, Top = 20, Size = new Size(60, 30) };
        var rook = new Button { Text = "Rook", Left = 90, Top = 20, Size = new Size(60, 30) };
        var bishop = new Button { Text = "Bishop", Left = 160, Top = 20, Size = new Size(60, 30) };
        var knight = new Button { Text = "Knight", Left = 230, Top = 20, Size = new Size(60, 30) };

        queen.Click += (s, e) => { SelectedPiece = 'q'; DialogResult = DialogResult.OK; Close(); };
        rook.Click += (s, e) => { SelectedPiece = 'r'; DialogResult = DialogResult.OK; Close(); };
        bishop.Click += (s, e) => { SelectedPiece = 'b'; DialogResult = DialogResult.OK; Close(); };
        knight.Click += (s, e) => { SelectedPiece = 'n'; DialogResult = DialogResult.OK; Close(); };

        Controls.Add(queen);
        Controls.Add(rook);
        Controls.Add(bishop);
        Controls.Add(knight);
    }
}