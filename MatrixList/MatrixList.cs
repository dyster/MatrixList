using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace MatrixList
{
    public class MatrixList : ListView
    {
        private IController _controller;        

        /// <summary>
        /// Holds a list of columns that should copy formatting from another column, the first value is the source column index and the second value is the target column index
        /// </summary>
        private List<Tuple<int, int>> _copyFormatting = new List<Tuple<int, int>>();

        private HeaderControl _headerControl;

        private string _overlayText = "";
        private bool _overlayTextSet = false;

        public MatrixList()
        {
            DoubleBuffered = true;
            //SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            VirtualMode = true;
            View = View.Details;
            OwnerDraw = true;

        }

        /// <summary>
        /// Initialises the MatrixList for the specified type, the returned controller is then used for type specific control and setting the source
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="settings">Optional: Settings to pass to the MatrixList</param>
        /// <param name="predicates">Optional: A list of predicates (conditions) for specific columns to determine if the value should be displayed, dictionary key is the column index</param>
        /// <returns></returns>
        public MatrixListController<T> Initialize<T>(MatrixSettings settings, Dictionary<int, Predicate<T>> predicates)
        {
            if(settings == null)
                settings = new MatrixSettings();

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

            var discardedProperties = new List<PropertyInfo>();

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
                else
                    discardedProperties.Add(property);
            }

            // if this is set, we go over the remaining properties without MatrixColumnAttribute
            if(settings.AutomaticColumnGeneration)
            {
                foreach(var property in discardedProperties)
                {
                    var theCurrentColumnCount = colCount++;
                    var strsize = TextRenderer.MeasureText(property.Name, Font);
                    var attr = new MatrixColumnAttribute(property.Name, strsize.Width+10);
                    
                    
                    var col = new MColumn<T>(property, attr);
                    _columns[theCurrentColumnCount] = col;                    
                }
                
            }

            colCount = 0;

            if(_columns.Count == 0)
            {
                _overlayText = "No Columns";
                _overlayTextSet = true;
            }

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
                var ch = new ColumnHeader()
                {
                    Name = mColumn.Name,
                    Text = mColumn.Name,
                    Width = mColumn.Width,
                    TextAlign = mColumn.HorizontalAlignment
                };

                Columns.Add(ch);

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

            _headerControl = new HeaderControl(this);

            var mlc = new MatrixListController<T>(this);
            _controller = mlc;

            _headerControl.ColumnRightClick += (sender, columnIndex) =>
            {
                var column = _columns[columnIndex];

                var cm = new ContextMenuStrip();
                cm.Items.Add("Sort A->Z", null, (s, e) =>
                {
                    
                });
                cm.Items.Add("Sort Z->A", null, (s, e) =>
                {
                    
                });

                cm.Show(Cursor.Position);

            };
            
            ListViewItem doError(string message)
            {
                _overlayText = message;
                _overlayTextSet = true;
                Columns.Clear();
                var ch = new ColumnHeader()
                {
                    Name = "Error",
                    Text = "Error",
                    Width = 500,
                    TextAlign = HorizontalAlignment.Left
                };
                Columns.Add(ch);
                return new ListViewItem(message);
                
            }

            this.RetrieveVirtualItem += (sender, e) =>
            {
                if (mlc == null)
                {
                    e.Item = doError("List has not been initialized");                    
                    return;
                }
                if (mlc.DataSource == null)
                {
                    e.Item = doError("DataSource is null");                    
                    return;
                }
                if (mlc.DataSource.Count == 0)
                {
                    e.Item = doError("DataSource is empty");                    
                    return;
                }
                if (e.ItemIndex >= mlc.DataSource.Count)
                {
                    e.Item = doError("An item has been requested that is outside the bounds of the list");                    
                    return;
                }
                if (_columns.Count == 0)
                {
                    e.Item = doError("No columns have been defined for the list");                    
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
                    else if (col.HighlightChanges && e.ItemIndex > 0)
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
            if(_controller != null)
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

        private static SolidBrush blackBrush = new SolidBrush(Color.Black);
        protected override void OnPaint(PaintEventArgs e)
        {
            // this is not currently happening as userdraw is off
            base.OnPaint(e);

            if(_overlayTextSet)
            {
                var y = ClientRectangle.Bottom / 2;
                e.Graphics.DrawString(_overlayText, Font, blackBrush, new Point(5, y));
            }
            
            
            
        }
    }
        
    public class MatrixSettings
    {
        /// <summary>
        /// False (default) - Only MatrixColumn attributes on the datasource object will be used to generate columns in the list
        /// True - Any property that does not have a MatrixColumn attribute will be automatically added (attributed properties will be displayed first)
        /// </summary>
        public bool AutomaticColumnGeneration { get; set; } = false;

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

        public Predicate<T>? DisplayPredicate { get; set; }
        public bool HighlightChanges { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public string Name { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
        public iTransformer Transformer { get; set; }
        public int UserId { get; set; }
        public int Width { get; set; }
    }
}