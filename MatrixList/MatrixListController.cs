using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MatrixList
{
    public interface IController
    {
        int getListCount();
    }

    public class MatrixListController<T>(MatrixList matrixList) : IController
    {
        private IList<T> _dataSource;
        private List<T> _filteredList;

        public void SetDataSource(IList<T> dataSource)
        {
            _dataSource = dataSource;
            if (DisplayFilter != null)
                FilterList();
        }

        public T GetItem(int itemIndex)
        {
            if (DisplayFilter != null)
            {
                if (itemIndex > _filteredList.Count - 1)
                    return default;
                return _filteredList[itemIndex];
            }
            else
            {
                if (itemIndex > _dataSource.Count - 1)
                    return default;
                return _dataSource[itemIndex];
            }
        }

        public Color HighlightBackColor { get; set; } = Color.Lavender;
        public Font HighlightFont { get; set; } = new Font(matrixList.Font, FontStyle.Bold);
        public Color HighlightForeColor { get; set; } = Color.Black;

        public Predicate<T>? DisplayFilter { get; set; } = null;

        int IController.getListCount()
        {
            if (_dataSource == null)
                return 0;

            if (DisplayFilter != null)
            {
                FilterList();
                return _filteredList.Count;
            }
            else
                return _dataSource.Count;
        }

        private void FilterList()
        {
            _filteredList = _dataSource.Where(item => DisplayFilter(item)).ToList();
        }

        public void AutoAdjustColumnWidths(ColumnHeaderAutoResizeStyle resizeStyle)
        {
            if (matrixList.Columns.Count == 0)
                return;

            for (int i = 0; i < matrixList.Columns.Count; i++)
            {
                matrixList.AutoResizeColumn(i, resizeStyle);
            }
        }

        public void Invalidate()
        {
            matrixList.Invalidate();
        }        
    }
}