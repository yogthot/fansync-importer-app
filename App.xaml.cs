﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using FanSync.HTTP;
using FanSync.IPC;
using Microsoft.Toolkit.Uwp.Notifications;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using Res = FanSync.Properties.Resources;

namespace FanSync
{
    public partial class App : Application
    {
        public static string PipeName = "FanSync";
        public static string PidFileName = "fansync.pid";

        public static string PidPath => $"{AppDomain.CurrentDomain.BaseDirectory}/{PidFileName}";

        public Settings settings;
        public MainWindow window;
        public FileStream pidFile;
        public NotifyIcon trayIcon;
        public IpcServer pipeServer;
        public PledgeImporter importer;

        private bool? handleNotificationThenExit;

        public bool isUpToDate;

        // cli flags
        private bool isInitialSetup;
        private bool isStop;
        private bool isUninstall;


        private async void AppStartup(object sender, StartupEventArgs e)
        {
            await Start();
        }
        private async Task Start()
        {
            isUpToDate = true;

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                isInitialSetup = args[1] == "-setup";
                isStop = args[1] == "-stop";
                isUninstall = args[1] == "-uninstall";
            }

            if (!isUninstall && !handleNotificationThenExit.HasValue)
            {
                ToastNotificationManagerCompat.OnActivated += OnNotificationAction;

                if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
                {
                    handleNotificationThenExit = true;

                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Tick += ExitTimer;
                    timer.Interval = new TimeSpan(0, 0, 5);
                    timer.Start();
                    return;
                }
            }

            if (isUninstall)
            {
                // uninstall notifications
                // this cleans up any existing notifications for this program, among other things
                ToastNotificationManagerCompat.Uninstall();
            }

            int pid = await CheckLock();
            if (pid != 0)
            {
                using (IpcClient ipc = new IpcClient(PipeName))
                {
                    if (!isStop && !isUninstall)
                    {
                        await ipc.Focus();
                    }
                    else
                    {
                        await ipc.Exit();
                        // wait for process to exit for 5 seconds
                        Process other = Process.GetProcessById(pid);
                        if (!other.WaitForExit(5000))
                        {
                            // if it can't exit safely, kill it
                            other.Kill();
                        }
                        
                    }
                }
                await this.ExitAsync();
                return;
            }

            // if we got the lock but still need to stop
            if (isStop || isUninstall)
            {
                await this.ExitAsync();
                return;
            }

            pipeServer = new IpcServer(PipeName, this);
            pipeServer.Start();

            if (!await LoadSettings())
                return;

            importer = new PledgeImporter(settings);

            window = new MainWindow(this, settings);
            window.OnChanged += Settings_Changed;

            if (settings.IsValid())
            {
                // check and handle the service status
                FansyncClient fansync = new FansyncClient(settings);
                FansyncStatus status = await fansync.GetStatus();

                if (status != null)
                {
                    switch (status.status)
                    {
                        case 1:
                            Notification.Show(Res.title_out_of_service, Res.msg_out_of_service, Notification.Action("uninstall"));
                            await this.ExitAsync();
                            return;

                        default:
                            break;
                    }

                    if (!string.IsNullOrEmpty(status.latest_version))
                    {
                        Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                        Version latestVersion = new Version(status.latest_version);
                        if (latestVersion > currentVersion)
                        {
                            isUpToDate = false;
                            Notification.Show(Res.title_update, Res.msg_update, Notification.Action("update"));
                        }
                    }
                }

                importer.Start();
            }
            else
            {
                if (isInitialSetup)
                {
                    FocusSettings();
                }
                else
                {
                    Notification.Show(Res.title_setup, Res.msg_setup, Notification.Action("settings"));
                }
            }

            // tray icon
            trayIcon = new NotifyIcon();

            // what the hell, Microsoft
            trayIcon.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);

            trayIcon.ContextMenu = new System.Windows.Forms.ContextMenu();
            trayIcon.ContextMenu.MenuItems.Add(Res.notify_settings, MenuSettings_Click);
            trayIcon.ContextMenu.MenuItems.Add(Res.notify_exit, MenuExit_Click);

            // default operation
            trayIcon.DoubleClick += this.Notify_DblClick;

            trayIcon.Visible = true;
        }
        public async Task ExitAsync()
        {
            if (settings != null)
            {
                await settings.Save();
            }

            if(importer != null)
            {
                importer.Stop();
            }

            if (pipeServer != null)
            {
                await pipeServer.Stop();
                pipeServer = null;
            }

            if (pidFile != null)
            {
                pidFile.Close();
                pidFile.Dispose();
                pidFile = null;
            }

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
            }

            this.Shutdown();
        }

        private void Notify_DblClick(object sender, EventArgs e)
        {
            this.FocusStatus();
        }
        private void MenuSettings_Click(object sender, EventArgs e)
        {
            this.FocusSettings();
        }
        private async void MenuExit_Click(object sender, EventArgs e)
        {
            await ExitAsync();
        }

        private async Task<bool> LoadSettings()
        {
            try
            {
                settings = await Settings.Load();
            }
            catch
            {
                Notification.Show(
                    Res.title_error,
                    Res.exc_settings,
                    actions: new Dictionary<string, string>()
                    {
                        { Res.lbl_reset, "reset_config" },
                        { Res.lbl_exit, "exit" },
                    }
                );
                await ExitAsync();
                return false;
            }
            return true;
        }
        private void Settings_Changed()
        {
            if (!importer.IsRunning && settings.IsValid())
                importer.Start();
        }

        public void FocusStatus()
        {
            window.WindowState = WindowState.Normal;
            window.Tabs.SelectedIndex = 0;
            window.Show();
            window.Activate();
        }
        public void FocusSettings()
        {
            window.WindowState = WindowState.Normal;
            window.Tabs.SelectedIndex = 1;
            window.Show();
            window.Activate();
        }

        private async Task<int> CheckLock()
        {
            try
            {
                pidFile = new FileStream(PidPath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                StreamWriter writer = new StreamWriter(pidFile, Encoding.UTF8);
                int pid = Process.GetCurrentProcess().Id;
                await writer.WriteAsync(Convert.ToString(pid));
                await writer.FlushAsync();

                return 0;
            }
            catch (IOException)
            {
                using (FileStream fs = File.Open(PidPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    StreamReader reader = new StreamReader(fs, Encoding.UTF8);
                    return Convert.ToInt32(await reader.ReadToEndAsync());
                }
            }
        }

        private async void ExitTimer(object sender, EventArgs e)
        {
            if (handleNotificationThenExit == true)
            {
                DispatcherTimer timer = (DispatcherTimer)sender;
                timer.Stop();
                await ExitAsync();
            }
        }

        public void OnNotificationAction(ToastNotificationActivatedEventArgsCompat eventArgs)
        {
            Dispatcher.BeginInvoke(new Action(async() =>
            {
                ToastArguments args = ToastArguments.Parse(eventArgs.Argument);
                string action = args.Get("action");

                switch (action)
                {
                    case "settings":
                        FocusSettings();
                        break;

                    case "reset_config":
                        settings = Settings.DefaultSettings;
                        await settings.Save();
                        handleNotificationThenExit = false;
                        await Start();
                        break;

                    case "update":
                        // open update url
                        break;

                    case "exit":
                        await ExitAsync();
                        break;

                    case "uninstall":
                        // run uninstaller?
                        break;

                    default:
                        break;
                }

                if (handleNotificationThenExit == true)
                    await this.ExitAsync();
            }));
        }
    }
}