using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfClient
{
    public static class WinFormsExtensionMethods
    {
        /// <summary>
        /// Runs the specified action on the thread of the control.
        /// </summary>
        public static void Invoke(this Control control, Action action)
        {
            control.Invoke((Delegate)action);
        }
    }
}
