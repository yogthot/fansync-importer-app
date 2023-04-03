using FanSync.HTTP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Res = FanSync.Properties.Resources;

namespace FanSync
{
    public class PledgeImporter
    {
        private enum Signal
        {
            Stop,
            Update
        }

        public static int SecondsBetweenUpdate = 3600;

        private Settings settings;
        private FanboxClient fanbox;
        private FansyncClient fansync;
        private Thread thread;
        private SemaphoreSlim signalThread;
        private ConcurrentQueue<Signal> signalQueue;

        public bool IsRunning { get; private set; }

        public event Action<object, ImporterStatus> OnStatus;

        public PledgeImporter(Settings settings)
        {
            this.settings = settings;

            fanbox = new FanboxClient(settings);
            fansync = new FansyncClient(settings);

            signalThread = new SemaphoreSlim(0);
            signalQueue = new ConcurrentQueue<Signal>();

            thread = new Thread(this.Run);
            IsRunning = false;
        }

        public void Start()
        {
            thread.Start();
            IsRunning = true;
        }

        private void SendSignal(Signal signal)
        {
            try
            {
                signalQueue.Enqueue(signal);
                signalThread.Release();
            }
            catch (SemaphoreFullException)
            {
                // ignore
            }
        }

        public void ForceUpdate()
        {
            SendSignal(Signal.Update);
        }

        public void Stop()
        {
            SendSignal(Signal.Stop);
        }

        // waits for next command, or updates on timeout
        private async Task<bool> Wait(bool first = false)
        {
            TimeSpan waitTime = TimeSpan.FromSeconds(SecondsBetweenUpdate);
            if (first)
            {
                if (!settings.last_update_time.HasValue)
                    // don't wait at all
                    return true;

                double difference = (DateTimeOffset.Now - settings.last_update_time.Value).TotalSeconds;
                if (difference > SecondsBetweenUpdate)
                    // don't wait at all
                    return true;

                waitTime = TimeSpan.FromSeconds(SecondsBetweenUpdate - difference);
            }

            
            if (await signalThread.WaitAsync(waitTime))
            {
                if (signalQueue.TryDequeue(out Signal signal))
                {
                    // received a signal
                    switch (signal)
                    {
                        case Signal.Stop:
                            IsRunning = false;
                            return false;

                        case Signal.Update:
                            // proceed normally
                            break;
                    }
                }
            }

            return true;
        }

        private async void Run()
        {
            // initial wait to offset possible update in previous execution
            if (!await Wait(true))
                return;

            int consecutiveFanboxErrors = 0;
            int consecutiveFansyncErrors = 0;
            bool networkError = false;

            while (true)
            {
                try
                {
                    string month = DateTimeOffset.Now.ToString("yyyy-MM");

                    string pledgeData;
                    try
                    {
                        pledgeData = await fanbox.GetPledges(month);
                    }
                    catch (HttpRequestException)
                    {
                        networkError = true;
                        throw;
                    }
                    catch
                    {
                        consecutiveFanboxErrors++;
                        throw;
                    }
                    consecutiveFanboxErrors = 0;

                    try
                    {
                        bool success = await fansync.SubmitPledges(month, pledgeData);
                    }
                    catch (HttpRequestException)
                    {
                        networkError = true;
                        throw;
                    }
                    catch
                    {
                        consecutiveFansyncErrors++;
                        throw;
                    }
                    consecutiveFansyncErrors = 0;
                }
                catch (Exception)
                {
                    // if anything fails, just try again later
                    // maybe warn the user if it fails for more than 3 times
                    // TODO log?
                }

                if (consecutiveFanboxErrors > 0 && consecutiveFanboxErrors % 3 == 0)
                {
                    Notification.Show(Res.title_error, Res.exc_fanbox_cookie, Notification.Action("settings"));
                }
                if (consecutiveFansyncErrors > 0 && consecutiveFansyncErrors % 3 == 0)
                {
                    Notification.Show(Res.title_error, Res.exc_fansync_token, Notification.Action("settings"));
                }

                DateTimeOffset now = DateTimeOffset.Now;
                settings.last_update_time = now;
                OnStatus?.Invoke(this, new ImporterStatus(now, consecutiveFanboxErrors == 0, consecutiveFansyncErrors == 0, !networkError));
                
                if (!await Wait())
                    return;
            }
        }
    }

    public class ImporterStatus : EventArgs
    {
        public DateTimeOffset Timestamp { get; private set; }
        public bool FanboxStatus { get; private set; }
        public bool FansyncStatus { get; private set; }
        public bool NetworkStatus { get; private set; }

        public ImporterStatus(DateTimeOffset timestamp, bool fanboxStatus, bool fansyncStatus, bool networkStatus)
        {
            Timestamp = timestamp;
            FanboxStatus = fanboxStatus;
            FansyncStatus = fanboxStatus;
            NetworkStatus = networkStatus;
        }
    }
}
