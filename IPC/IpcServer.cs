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
    public class IpcServer
    {
        private string name;
        private App app;

        public IpcServer(string name, App app)
        {
            this.name = name;
            this.app = app;
        }

        public void Start()
        {
            Thread serverThread = new Thread(this.ServerLoop);
            serverThread.IsBackground = true;
            serverThread.Start();
        }

        public async Task Stop()
        {
            try
            {
                using (IpcClient client = new IpcClient(name))
                {
                    await client.RawCommand(Command.Stop);
                }
            }
            catch (Exception)
            { }
        }

        public void ServerLoop(object param)
        {
            var pipeSecurity = new PipeSecurity();
            pipeSecurity.SetAccessRule(
                new PipeAccessRule(
                    new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
                    PipeAccessRights.ReadWrite,
                    AccessControlType.Allow
                )
            );

            bool running = true;
            while (running)
            {
                using (var pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough, 512, 512, pipeSecurity))
                {
                    pipe.WaitForConnection();
                    int commandNum = pipe.ReadByte();

                    // parse command
                    Command command = Command.None;
                    if (Enum.IsDefined(typeof(Command), commandNum))
                    {
                        command = (Command)commandNum;
                    }

                    switch (command)
                    {
                        case Command.Focus:
                            app.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                app.FocusSettings();
                            }));
                            break;

                        case Command.Stop:
                            running = false;
                            break;

                        case Command.Exit:
                            app.Dispatcher.BeginInvoke(new Action(async () =>
                            {
                                await app.ExitAsync();
                            }));
                            break;

                        default:
                            break;
                    }
                }
            }
        }
    }
}
