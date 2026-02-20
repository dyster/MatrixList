using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MatrixList
{
    // from https://stackoverflow.com/questions/17642308/are-there-anyway-to-determine-coordinates-of-top-left-corner-of-column-in-listvi
    static class NativeMethods
    {
        private const int LVM_FIRST = 0x1000;
        private const int LVM_GETHEADER = LVM_FIRST + 31;

        private const int HDM_FIRST = 0x1200;
        private const int HDM_GETITEMRECT = HDM_FIRST + 7;

        private const int SB_HORZ = 0;
        private const int SB_VERT = 1;

        private const int SIF_POS = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        public class SCROLLINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(NativeMethods.SCROLLINFO));
            public int fMask;
            public int nMin;
            public int nMax;
            public int nPage;
            public int nPos;
            public int nTrackPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageGETRECT(IntPtr hWnd, int Msg, int wParam, ref RECT lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool GetScrollInfo(IntPtr hWnd, int fnBar, SCROLLINFO scrollInfo);

        /// <summary>
        /// Return the handle to the header control on the given list
        /// </summary>
        /// <param name="list">The listview whose header control is to be returned</param>
        /// <returns>The handle to the header control</returns>
        public static IntPtr GetHeaderControl(ListView list)
        {
            return SendMessage(list.Handle, LVM_GETHEADER, 0, 0);
        }

        /// <summary>
        /// Get the scroll position of the given scroll bar
        /// </summary>
        /// <param name="lv"></param>
        /// <param name="horizontalBar"></param>
        /// <returns></returns>
        public static int GetScrollPosition(ListView lv, bool horizontalBar)
        {
            int fnBar = (horizontalBar ? SB_HORZ : SB_VERT);

            SCROLLINFO scrollInfo = new SCROLLINFO();
            scrollInfo.fMask = SIF_POS;
            if (GetScrollInfo(lv.Handle, fnBar, scrollInfo))
            {
                return scrollInfo.nPos;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Return the screen rectangle of the column header item specified
        /// </summary>
        /// <param name="handle">Handle to the header control to check</param>
        /// <param name="index">Index of the column to get</param>
        /// <returns></returns>
        public static Rectangle GetHeaderItemRect(IntPtr handle, int index)
        {
            RECT rc = new RECT();
            IntPtr result = NativeMethods.SendMessageGETRECT(handle, HDM_GETITEMRECT, index, ref rc);
            if (result != IntPtr.Zero)
            {
                return new Rectangle(rc.Left, rc.Top, rc.Right - rc.Left, rc.Bottom - rc.Top);
            }
            return Rectangle.Empty;
        }

        /// <summary>
        /// Get's the location coordinate of the specified column header's top/left corner
        /// </summary>
        /// <param name="oListView">ListView control to get the column header location from</param>
        /// <param name="iColumn">Column to find the location for</param>
        /// <param name="bAsScreenCoordinate">Whether the location returned is for the screen or the local ListView control</param>
        /// <returns>Location of column header or <see cref="Point.Empty"/> if it could not be retrieved</returns>
        public static Point GetColumnHeaderTopLeft(this ListView oListView, int iColumn, bool bAsScreenCoordinate)
        {
            if (oListView == null)
            {
                throw new ArgumentNullException();
            }
            if ((iColumn < 0) || (iColumn >= oListView.Columns.Count))
            {
                throw new ArgumentOutOfRangeException();
            }

            // Get the header control's rectangle
            IntPtr hndHeader = NativeMethods.GetHeaderControl(oListView);
            Rectangle oHeaderRect = NativeMethods.GetHeaderItemRect(hndHeader, iColumn);
            if (oHeaderRect.IsEmpty)
            {
                return Point.Empty;
            }

            // Get the scroll bar position to adjust the left
            int iScroll = NativeMethods.GetScrollPosition(oListView, true);

            // Create the local coordinate
            Point oLocation = new Point(oHeaderRect.Left - iScroll, oHeaderRect.Top);

            // Return the local or screen coordinate
            return bAsScreenCoordinate ? oListView.PointToScreen(oLocation) : oLocation;
        }

        
        
    }
}
