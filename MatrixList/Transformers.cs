using System;
using System.Collections.Generic;
using System.Drawing;

namespace MatrixList
{
    public class ValueTransformer<T> : iTransformer
    {
        private string _propertyName;
        private Type _itemType;
        private Dictionary<object, MatrixCell> _lookup;

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

    public class PropertyTransformer<T> : iTransformer
    {
        private string _propertyName;
        private Type _itemType;

        public PropertyTransformer(string propertyName)
        {
            _propertyName = propertyName;
            _itemType = typeof(T);
        }

        public MatrixCell Transform(object rowItem)
        {
            var propValue = _itemType.GetProperty(_propertyName)?.GetValue(rowItem, null);
            var text = string.Empty;
            //var propType = propValue.GetType();
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
}