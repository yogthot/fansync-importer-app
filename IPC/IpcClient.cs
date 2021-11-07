using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FanSync.IPC
{
    public class IpcClient : IDisposable
    {
        private bool connected;
        private NamedPipeClientStream stream;

        public IpcClient(string name)
        {
            stream = new NamedPipeClientStream(".", name, PipeDirection.InOut);
            connected = false;
        }

        public async Task Connect()
        {
            if (!connected)
            {
                await stream.ConnectAsync(5);
                connected = true;
            }
        }

        public void Close()
        {
            if (connected)
            {
                stream.Close();
                connected = false;
            }
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        public async Task Focus()
        {
            await RawCommand(Command.Focus);
        }
        public async Task Exit()
        {
            await RawCommand(Command.Exit);
        }

        public async Task RawCommand(Command command)
        {
            await Connect();
            await stream.WriteAsync(new byte[] { (byte)command }, 0, 1);
        }
    }
}
