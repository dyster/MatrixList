using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MatrixList
{
    

    public interface IController
    {
        int getListCount();
    }

    public class MatrixListController<T>(MatrixList matrixList) : IController
    {
        private IList<T> _dataSource;

        public IList<T> DataSource
        {
            get { return _dataSource; } 
            set 
            { 
                _dataSource = value; 
                if (DisplayFilter != null) 
                    FilterList(); 
            } 
        }

        public List<T> FilteredList { get; set; }

        public Color HighlightBackColor { get; set; } = Color.Lavender;
        public Font HighlightFont { get; set; } = new Font(matrixList.Font, FontStyle.Bold);
        public Color HighlightForeColor { get; set; } = Color.Black;

        public Predicate<T>? DisplayFilter { get; set; } = null;

        int IController.getListCount()
        {
            if (DataSource == null)
                return 0;

            if (DisplayFilter != null)
            {
                FilterList();
                return FilteredList.Count;
            }
            else
                return DataSource.Count;
        }

        private void FilterList()
        {
            FilteredList = DataSource.Where(item => DisplayFilter(item)).ToList();
        }
    }
}