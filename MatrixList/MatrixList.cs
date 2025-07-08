using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MatrixList
{
    public class MatrixList : ListView
    {
        /// <summary>
        /// Holds a list of columns that should copy formatting from another column, the first value is the source column index and the second value is the target column index
        /// </summary>
        private List<Tuple<int, int>> _copyFormatting = new List<Tuple<int, int>>();

        private IController _controller;

        public MatrixList()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            VirtualMode = true;
            View = View.Details;
            OwnerDraw = true;
        }

        public MatrixListController<T> Initialize<T>(Dictionary<int, Predicate<T>> predicates = null)
        {
            var t = typeof(T);
            Columns.Clear();
            var _columns = new Dictionary<int, MColumn<T>>();
            //_columns.Clear();

            _copyFormatting.Clear();

            //var members = t.GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var properties = t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            int colCount = 0;

            // maps the column ID from attributes to the "real" column index
            var columnIdIndexMap = new Dictionary<int, int>();

            // first pass to find all column IDs
            foreach (var property in properties)
            {
                var columnattrib = property.GetCustomAttributes(typeof(MatrixColumnAttribute), false);
                if (columnattrib.Length == 1)
                {
                    var attr = (MatrixColumnAttribute)columnattrib[0];
                    var theCurrentColumnCount = colCount++;

                    var col = new MColumn<T>(property, attr);
                    _columns[theCurrentColumnCount] = col;

                    var columnIdAttrib = property.GetCustomAttributes(typeof(MatrixColumnId), false);

                    if (columnIdAttrib.Length == 1)
                    {
                        var idAttrib = (MatrixColumnId)columnIdAttrib[0];
                        columnIdIndexMap[idAttrib.Id] = theCurrentColumnCount;
                        col.UserId = idAttrib.Id;
                    }
                }
            }

            colCount = 0;

            foreach (var indexColumnPair in _columns)
            {
                var mColumn = indexColumnPair.Value;

                var transformattrib = mColumn.PropertyInfo.GetCustomAttributes(typeof(MatrixTransformAttribute), false);
                var copyFormatAttrib = mColumn.PropertyInfo.GetCustomAttributes(typeof(MatrixCopyFormat), false);
                var highlightChangeAttribs = mColumn.PropertyInfo.GetCustomAttributes(typeof(MatrixHighlightValueChanges), false);
                if (highlightChangeAttribs.Length > 0)
                {
                    mColumn.HighlightChanges = true;
                }

                var theCurrentColumnCount = colCount++;

                Columns.Add(mColumn.Name, mColumn.Width, mColumn.HorizontalAlignment);

                if (predicates != null)
                {
                    foreach (var p in predicates)
                    {
                        var targetColumn = _columns.FirstOrDefault(c => c.Value.UserId == p.Key).Value;
                        if (targetColumn != null)
                        {
                            targetColumn.DisplayPredicate = p.Value;
                        }
                    }
                }

                if (transformattrib.Length == 0)
                    mColumn.Transformer = new PropertyTransformer<T>(mColumn.PropertyInfo.Name);
                else
                {
                    var dic = new Dictionary<object, MatrixCell>();
                    foreach (MatrixTransformAttribute tratt in transformattrib)
                    {
                        dic.Add(tratt.Input, tratt.Output);
                    }
                    mColumn.Transformer = new ValueTransformer<T>(mColumn.PropertyInfo.Name, dic);
                }

                if (copyFormatAttrib.Length == 1)
                {
                    var copyFormat = (MatrixCopyFormat)copyFormatAttrib[0];
                    var sourceColumnIndex = columnIdIndexMap[copyFormat.Id];
                    _copyFormatting.Add(new Tuple<int, int>(sourceColumnIndex, theCurrentColumnCount));
                }
            }

            var mlc = new MatrixListController<T>(this);
            _controller = mlc;

            this.RetrieveVirtualItem += (sender, e) =>
            {
                if (mlc == null)
                {
                    e.Item = new ListViewItem("List has not been initialized");
                    return;
                }
                if (mlc.DataSource == null)
                {
                    e.Item = new ListViewItem("DataSource is null");
                    return;
                }
                if (mlc.DataSource.Count == 0)
                {
                    e.Item = new ListViewItem("DataSource is empty");
                    return;
                }
                if (e.ItemIndex >= mlc.DataSource.Count)
                {
                    e.Item = new ListViewItem("An item has been requested that is outside the bounds of the list");
                    return;
                }

                var item = mlc.DataSource[e.ItemIndex];
                if (item == null)
                {
                    e.Item = new ListViewItem("null");
                    return;
                }
                var itemType = item.GetType();

                for (int i = 0; i < Columns.Count; i++)
                {
                    var col = _columns[i];

                    T previous = default;
                    bool display = true;
                    if (col.DisplayPredicate != null)
                    {
                        if (col.DisplayPredicate.Invoke(item))
                        {
                            var lookup = e.ItemIndex - 1;
                            while (lookup >= 0)
                            {
                                var candidate = mlc.DataSource[lookup];
                                var success = col.DisplayPredicate.Invoke(candidate);
                                if (!success)
                                {
                                    lookup--;
                                    continue;
                                }
                                else
                                {
                                    previous = candidate;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            display = false;
                        }
                    }
                    else if(col.HighlightChanges && e.ItemIndex > 0)
                    {
                        previous = mlc.DataSource[e.ItemIndex - 1];
                    }

                        var mCell = col.Transformer.Transform(item);
                    mCell.Font = this.Font;

                    if (!display)
                        mCell.Text = "";
                    else if (col.HighlightChanges && previous != null)
                    {
                        var prev = col.Transformer.Transform(previous);
                        if (mCell.Text != prev.Text)
                        {
                            mCell.ForeColor = mlc.HighlightForeColor;
                            mCell.BackColor = mlc.HighlightBackColor;
                            mCell.Font = mlc.HighlightFont;
                        }
                    }

                    if (i == 0)
                    {
                        e.Item = new ListViewItem(mCell.Text) { ForeColor = mCell.ForeColor, BackColor = mCell.BackColor, Font = mCell.Font };
                    }
                    else
                    {
                        e.Item.SubItems.Add(mCell.Text, mCell.ForeColor, mCell.BackColor, mCell.Font);
                    }
                }

                foreach (var copyFormat in _copyFormatting)
                {
                    var sourceColumnIndex = copyFormat.Item1;
                    var targetColumnIndex = copyFormat.Item2;

                    var sourceSubItem = e.Item.SubItems[sourceColumnIndex];
                    var targetSubItem = e.Item.SubItems[targetColumnIndex];
                    targetSubItem.ForeColor = sourceSubItem.ForeColor;
                    targetSubItem.BackColor = sourceSubItem.BackColor;
                    targetSubItem.Font = sourceSubItem.Font;
                }
            };

            this.DrawColumnHeader += (sender, e) =>
            {
                e.DrawDefault = true;
            };

            this.DrawSubItem += (sender, e) =>
            {
                //e.DrawDefault = true;
                //return;
                var font = e.SubItem.Font;
                var text = e.SubItem.Text;
                var textbrush = new SolidBrush(e.SubItem.ForeColor);

                //var boldFont = new Font(this.Font, FontStyle.Bold);
                var location = new PointF(e.Bounds.Location.X, e.Bounds.Location.Y);

                if (e.SubItem.BackColor != Color.White)
                    e.Graphics.FillRectangle(new SolidBrush(e.SubItem.BackColor), e.Bounds);
                //e.Graphics.DrawRectangle(new Pen(new SolidBrush(e.SubItem.BackColor), 3), e.Bounds);

                e.Graphics.DrawString(text, font, textbrush, location);
                //var size = e.Graphics.MeasureString("Somefilename/", this.Font);

                //location.X += size.Width;
                //e.Graphics.DrawString("boldText", boldFont, Brushes.Black, location);
                //size = e.Graphics.MeasureString("boldText", boldFont);

                //location.X += size.Width;
                //e.Graphics.DrawString("/etc", this.Font, Brushes.Black, location);
            };

            return mlc;
        }

        public new void Invalidate()
        {
            this.VirtualListSize = _controller.getListCount();
            base.Invalidate();
        }

        [Browsable(false)]
        [Bindable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void Refresh()
        {
            base.Refresh();
        }
    }

    public class MatrixListController<T>(MatrixList matrixList) : IController
    {
        public IList<T> DataSource { get; set; }

        public Color HighlightBackColor { get; set; } = Color.Lavender;
        public Color HighlightForeColor { get; set; } = Color.Black;
        public Font HighlightFont { get; set; } = new Font(matrixList.Font, FontStyle.Bold);

        int IController.getListCount()
        {
            if (DataSource == null)
                return 0;
            return DataSource.Count;
        }

        /// <summary>
        /// Add predicate to enable reverse value lookup for a column using the column id attribute and a predicate to find the value, the predicate is applied to each item backwards in the list until it returns true, this value is then used for any reverse lookup functions
        /// </summary>
        //public Dictionary<int, Predicate<T>> ColumnReverseLookup { get; set; } = new Dictionary<int, Predicate<T>>();
    }

    public class MColumn<T>
    {
        public MColumn(PropertyInfo pInfo, MatrixColumnAttribute attr)
        {
            PropertyInfo = pInfo;
            Name = attr.Name;
            Width = attr.ColumnWidth;
            HorizontalAlignment = attr.HorizontalAlignment;
        }

        public string Name { get; set; }
        public int Width { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; }

        public iTransformer Transformer { get; set; }
        public int UserId { get; set; }
        public Predicate<T>? DisplayPredicate { get; set; }
        public bool HighlightChanges { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
    }

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

        public string Text { get; set; }
        public Color ForeColor { get; set; }
        public Color BackColor { get; set; }

        public Font Font { get; set; }
    }

    public interface iTransformer
    {
        public MatrixCell Transform(object rowItem);
    }

    public interface IController
    {
        int getListCount();
    }
}