﻿using Machina;
using Machina.FFXIV;
using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
namespace Orchestrion.Utils
{
    public class Network
    {
        private FFXIVNetworkMonitor monitor;
        internal class ParseResult
        {
            public FFXIVMessageHeader header;
            public byte[] data;
        }
        public Network(uint processId)
        {
            monitor = new FFXIVNetworkMonitor();
            monitor.MonitorType = TCPNetworkMonitor.NetworkMonitorType.RawSocket;
            monitor.MessageReceived = MessageReceived;
            //monitor.MessageSent = MessageSent;
            monitor.ProcessID = processId;
        }
        public void Start() => monitor.Start();
        public void Stop() => monitor.Stop();

        private void MessageSent(long epoch, byte[] message, int set, FFXIVNetworkMonitor.ConnectionType connectionType)
        {
            //TODO: Ping Analysis
        }

        private void MessageReceived(long epoch, byte[] message, int set, FFXIVNetworkMonitor.ConnectionType connectionType)
        {
            var res = Parse(message);


            if (res.header.MessageType == 0x036B)//CountDown
            {
                Console.WriteLine("CountDown");
                var countDownTime = res.data[36];
                var nameBytes = new byte[18];
                var timeStampBytes = new byte[4];
                Array.Copy(res.data, 41, nameBytes, 0, 18);
                Array.Copy(res.data, 24, timeStampBytes, 0, 4);
                var name = Encoding.UTF8.GetString(nameBytes) ?? "";
                //Play?.Invoke(this, new PlayEvent(1, 0, BitConverter.ToInt32(timeStampBytes, 0), name));
            }


            if (res.header.MessageType == 0x02eb) //ensemble
            {
                Console.WriteLine("ensemble ready");
                var timeStampBytes = new byte[4];
                Array.Copy(res.data, 24, timeStampBytes, 0, 4);
                Console.WriteLine(BitConverter.ToInt32(timeStampBytes, 0));
                //Play?.Invoke(this, new PlayEvent(0, 5, BitConverter.ToInt32(timeStampBytes, 0), "合奏助手"));

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

        public static void RegisterToFirewall()
        {
            try
            {
                Process p = new Process();
                var exePath = Process.GetCurrentProcess().MainModule.FileName;
                //p.StartInfo.FileName = "cmd.exe"; //命令
                //p.StartInfo.UseShellExecute = false; //不启用shell启动进程
                //p.StartInfo.RedirectStandardInput = true; // 重定向输入
                //p.StartInfo.RedirectStandardOutput = true; // 重定向标准输出
                //p.StartInfo.RedirectStandardError = true; // 重定向错误输出 
                //p.StartInfo.CreateNoWindow = true; // 不创建新窗口
                //p.Start();
                //p.StandardInput.WriteLine("netsh advfirewall firewall add rule name=\"WinClient\" dir=in program=\"" + exePath + "\" action=allow localip=any remoteip=any security=notrequired description=DFAssist"); //cmd执行的语句
                //                                                                                                                                                                                                        //p.StandardOutput.ReadToEnd(); //读取命令执行信息
                //p.StandardInput.WriteLine("exit"); //退出

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
            catch(UnauthorizedAccessException)
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Process.GetCurrentProcess().MainModule.FileName;
                psi.Verb = "runas";

                try
                {
                    Process.Start(psi);
                    Application.Current.Shutdown();
                }
                catch (Exception eee)
                {
                    MessageBox.Show(eee.Message);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("防火墙注册失败");
            }
        }

    }
}
