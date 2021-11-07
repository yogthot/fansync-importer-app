using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanSync.IPC
{
    public enum Command
    {
        None,
        // internal command to stop the server
        Stop,

        Focus,
        Exit
    }
}
