using System.Collections.Generic;
using System.Drawing;

namespace MatrixList
{
    public interface IController
    {
        int getListCount();
    }

    public class MatrixListController<T>(MatrixList matrixList) : IController
    {
        public IList<T> DataSource { get; set; }

        public Color HighlightBackColor { get; set; } = Color.Lavender;
        public Font HighlightFont { get; set; } = new Font(matrixList.Font, FontStyle.Bold);
        public Color HighlightForeColor { get; set; } = Color.Black;

        int IController.getListCount()
        {
            if (DataSource == null)
                return 0;
            return DataSource.Count;
        }
    }
}