using System.Drawing;

namespace MatrixList
{
    public class MatrixCell
    {
        public MatrixCell(string text)
        {
            Text = text;
        }

        public MatrixCell(string text, Color foreColor)
        {
            Text = text;
            ForeColor = foreColor;
        }

        public Color BackColor { get; set; }
        public Font Font { get; set; }
        public Color ForeColor { get; set; }
        public string Text { get; set; }
    }
}