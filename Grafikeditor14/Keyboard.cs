using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Grafikeditor14
{
    public static class Keyboard
    {
        private static HashSet<Keys> keys = new HashSet<Keys>();

        static Keyboard()
        {
            Application.AddMessageFilter(new KeyMessageFilter());
        }

        public static bool IsKeyDown(Keys key)
        {
            return keys.Contains(key);
        }

        private class KeyMessageFilter : IMessageFilter
        {
            private const int WM_KEYDOWN = 0x0100;
            private const int WM_KEYUP = 0x0101;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM_KEYDOWN)
                {
                    keys.Add((Keys)m.WParam);
                }
                else if (m.Msg == WM_KEYUP)
                {
                    keys.Remove((Keys)m.WParam);
                }
                return false;
            }
        }
    }
}
