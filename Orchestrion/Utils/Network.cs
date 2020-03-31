using Machina;
using Machina.FFXIV;
using NetFwTypeLib;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
namespace Orchestrion.Utils
{

    public class Network : FFXIVNetworkMonitor
    {
        public bool IsListening { get; set; } = false;
        public delegate void NetPlayEvent(int mode, int interval, int timestamp);
        public NetPlayEvent OnReceived;
        public NetPlayEvent OnSent;
        public event Action<bool> OnStatusChanged;
        //NLog
        static Logger Logger = LogManager.GetCurrentClassLogger();

        //Ping
        public uint Ping { get; set; } = 0;
        private Dictionary<uint, DateTime> timePairs;

        public Network(uint processId)
            :base()
        {
            MonitorType = TCPNetworkMonitor.NetworkMonitorType.RawSocket;
            MessageReceived = MessageReceivedFunc;
            MessageSent = MessageSentFunc;
            ProcessID = processId;
        }
        public new void Start()
        {
            base.Start();
            IsListening = true;
            OnStatusChanged?.Invoke(IsListening);
        }
        public new void Stop()
        {
            base.Stop();
            IsListening = false;
            OnStatusChanged?.Invoke(IsListening);
        }

        private void MessageSentFunc(long epoch, byte[] message, int set, ConnectionType connectionType)
        {
            //Ping Analysis

        }

        private void MessageReceivedFunc(long epoch, byte[] message, int set, ConnectionType connectionType)
        {
            var res = Parse(message);

            if (res.header.MessageType == Config.config.OpCode[ConfigObject.OpCodeEnum.Countdown])//CountDown
            {
                Logger.Info("CountDown Receive");
                var countDownTime = res.data[36];
                var nameBytes = new byte[18];
                var timeStampBytes = new byte[4];
                Array.Copy(res.data, 41, nameBytes, 0, 18);
                Array.Copy(res.data, 24, timeStampBytes, 0, 4);
                var name = Encoding.UTF8.GetString(nameBytes) ?? "";
                OnReceived?.Invoke(0,0,BitConverter.ToInt32(timeStampBytes, 0));
            }

            if (res.header.MessageType == Config.config.OpCode[ConfigObject.OpCodeEnum.EnsembleReceive]) //ensemble
            {
                Logger.Info("Ensemble_ready Receive");
                var timeStampBytes = new byte[4];
                Array.Copy(res.data, 24, timeStampBytes, 0, 4);

                OnReceived?.Invoke(1,5, BitConverter.ToInt32(timeStampBytes, 0));
            }

            if (res.header.MessageType == Config.config.OpCode[ConfigObject.OpCodeEnum.Ping]) //Ping
            {
                
            }
        }
        private static ParseResult Parse(byte[] data)
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            FFXIVMessageHeader head = (FFXIVMessageHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(FFXIVMessageHeader));
            handle.Free();

            ParseResult result = new ParseResult();
            result.header = head;
            result.data = data;

            return result;
        }
        internal class ParseResult
        {
            public FFXIVMessageHeader header;
            public byte[] data;
        }

        public static void RegisterToFirewall()
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule.FileName;
                INetFwPolicy2 firewallPolicy = Utils.GetInstance<INetFwPolicy2>("HNetCfg.FwPolicy2");
                INetFwRule firewallRule = Utils.GetInstance<INetFwRule>("HNetCfg.FWRule");
                bool isExists = false;

                foreach (INetFwRule item in firewallPolicy.Rules)
                {
                    if (item.ApplicationName == exePath) isExists = true;
                }
                if (!isExists)
                {
                    firewallRule.Name = "Orchestrion";
                    firewallRule.ApplicationName = exePath;
                    firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    firewallRule.Description = "Orchestrion";
                    firewallRule.Enabled = true;
                    firewallRule.InterfaceTypes = "All";

                    firewallPolicy.Rules.Add(firewallRule);
                }
            }
            catch (UnauthorizedAccessException)
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Process.GetCurrentProcess().MainModule.FileName;
                psi.Verb = "runas";

                try
                {
                    Process.Start(psi);
                    Environment.Exit(0);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("防火墙注册失败");
            }
        }

    }
}
