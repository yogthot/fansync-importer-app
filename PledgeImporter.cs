using FanSync.HTTP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

        private Logger logger;

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

            string folder = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            logger = new Logger(Path.Combine(folder, "importer.log"));
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

            bool networkError = false;

            while (true)
            {
                networkError = false;

                //string month = DateTimeOffset.Now.ToString("yyyy-MM");
                Tuple<FanboxStatus, string> planData = new Tuple<FanboxStatus, string>(FanboxStatus.Unknown, null);
                Tuple<FanboxStatus, string> supporterData;
                bool fansyncError = false;
                try
                {
                    try
                    {
                        planData = await fanbox.GetPlans();
                        if (planData.Item1 != FanboxStatus.LoggedIn)
                            throw new Exception($"FanboxStatus: {planData.Item1}");

                        supporterData = await fanbox.GetSupporters();
                    }
                    catch (HttpRequestException)
                    {
                        networkError = true;
                        throw;
                    }

                    try
                    {
                        await fansync.SubmitPlans(planData.Item2);
                        await fansync.SubmitSupporters(supporterData.Item2);
                    }
                    catch (HttpRequestException)
                    {
                        networkError = true;
                        throw;
                    }
                    catch (Exception)
                    {
                        fansyncError = true;
                        throw;
                    }
                }
                catch (FanboxAPIError e)
                {
                    logger.Error(e.Content);
                    logger.Error(e.ToString());
                }
                catch (Exception e)
                {
                    logger.Error(e.ToString());
                }

                DateTimeOffset now = DateTimeOffset.Now;
                settings.last_update_time = now;
                OnStatus?.Invoke(this, new ImporterStatus(now, planData.Item1, fansyncError, !networkError));
                
                if (!await Wait())
                    return;
            }
        }
    }

    public class ImporterStatus : EventArgs
    {
        public DateTimeOffset Timestamp { get; }
        public FanboxStatus FanboxStatus { get; }
        public bool FansyncStatus { get; }
        public bool NetworkStatus { get; }

        public ImporterStatus(DateTimeOffset timestamp, FanboxStatus fanboxStatus, bool fansyncStatus, bool networkStatus)
        {
            Timestamp = timestamp;
            FanboxStatus = fanboxStatus;
            FansyncStatus = fansyncStatus;
            NetworkStatus = networkStatus;
        }
    }
}
