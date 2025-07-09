using System;
using System.Collections.Generic;
using System.Drawing;

namespace MatrixList
{
    public interface iTransformer
    {
        public MatrixCell Transform(object rowItem);
    }

    /// <summary>
    /// This is the default transformer, it extracts the value of the pre-specified property and calls ToString() on it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PropertyTransformer<T> : iTransformer
    {
        private Type _itemType;
        private string _propertyName;
        public PropertyTransformer(string propertyName)
        {
            _propertyName = propertyName;
            _itemType = typeof(T);
        }

        public MatrixCell Transform(object rowItem)
        {
            var propValue = _itemType.GetProperty(_propertyName)?.GetValue(rowItem, null);
            var text = string.Empty;
            
            if (propValue == null)
            {
                text = "null";
            }
            else
            {
                switch (propValue)
                {
                    case System.Byte[] b1:
                        text = BitConverter.ToString(b1);
                        break;

                    default:
                        text = propValue.ToString();
                        break;
                }
            }

            return new MatrixCell(text, Color.Black);
        }
    }

    public class ValueTransformer<T> : iTransformer
    {
        private Type _itemType;
        private Dictionary<object, MatrixCell> _lookup;
        private string _propertyName;
        public ValueTransformer(string propertyName, Dictionary<object, MatrixCell> lookup)
        {
            _propertyName = propertyName;
            _itemType = typeof(T);
            _lookup = lookup;
        }

        public MatrixCell Transform(object rowItem)
        {
            var propValue = _itemType.GetProperty(_propertyName)?.GetValue(rowItem, null);
            if (propValue == null)
            {
                return new MatrixCell("null");
            }
            if (_lookup.TryGetValue(propValue, out var cell))
            {
                return cell;
            }

            return new MatrixCell(propValue.ToString());
        }
    }
}