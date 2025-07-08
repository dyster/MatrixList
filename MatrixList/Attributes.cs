using System;
using System.Drawing;
using System.Windows.Forms;

namespace MatrixList
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class MatrixColumnAttribute : Attribute
    {
        public string Name { get; }

        public MatrixColumnAttribute(string name, int columnWidth = -1, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left)
        {
            Name = name;
            ColumnWidth = columnWidth;
            HorizontalAlignment = horizontalAlignment;
        }

        public int ColumnWidth { get; }
        public HorizontalAlignment HorizontalAlignment { get; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class MatrixTransformAttribute : Attribute
    {
        public object Input { get; }
        public MatrixCell Output { get; }

        public MatrixTransformAttribute(object input, string text, KnownColor foreColor)
        {
            Input = input;
            Output = new MatrixCell(text, Color.FromKnownColor(foreColor));
        }
    }

    /// <summary>
    /// This attribute is used to mark a column in the matrix list with a specific ID so it can be referenced from other columns
    /// </summary>
    /// <param name="id">A unique identifying number</param>
    public class MatrixColumnId(int id) : Attribute
    {
        public int Id { get; } = id;
    }

    /// <summary>
    /// All formatting will be copied from the column of the specified ID, note that the column must have the MatrixColumnId attribute set.
    /// </summary>
    /// <param name="id">The id of the column to copy formatting from</param>
    public class MatrixCopyFormat(int id) : Attribute
    {
        public int Id { get; } = id;
    }

    /// <summary>
    /// This attribute is used to mark a property that should be highlighted when its value changes from one row to the next, if a display predicate is installed that will be used.
    /// </summary>
    public class MatrixHighlightValueChanges : Attribute
    {
        
    }
}