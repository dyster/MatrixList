using System;

using System.Drawing;

using System.Windows.Forms;

namespace MatrixList
{
    public class HeaderControl : NativeWindow
    {
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_RBUTTONUP = 0x205;

        private ListView _parent;

        public HeaderControl(ListView lv)
        {
            IntPtr hndHeader = NativeMethods.GetHeaderControl(lv);
            _parent = lv;
            AssignHandle(hndHeader);
        }

        public event EventHandler<int> ColumnRightClick;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_RBUTTONDOWN)
            {
                var hScroll = NativeMethods.GetScrollPosition(_parent, true);
                var colcount = _parent.Columns.Count;
                for (int i = 0; i < colcount; i++)
                {
                    Rectangle oHeaderRect = NativeMethods.GetHeaderItemRect(this.Handle, i);
                    if (hScroll > 0)
                    {
                        oHeaderRect = new Rectangle(oHeaderRect.X - hScroll, oHeaderRect.Y, oHeaderRect.Width, oHeaderRect.Height);
                    }
                    
                    var cursorPos = Cursor.Position;
                    var relativePos = _parent.PointToClient(cursorPos);

                    if (oHeaderRect.Contains(relativePos))
                    {
                        OnColumnRightClick(i);
                        break;
                    }
                }
            }
            base.WndProc(ref m);
        }

        protected virtual void OnColumnRightClick(int columnIndex)
        {
            ColumnRightClick?.Invoke(this, columnIndex);
        }
    }
}