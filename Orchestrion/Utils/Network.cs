using Machina;
using Machina.FFXIV;
using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
namespace Orchestrion.Utils
{
    public class Network
    {
        private FFXIVNetworkMonitor monitor;

        public Network(uint processId)
        {
            monitor = new FFXIVNetworkMonitor();
            //RegisterToFirewall();
            monitor.MonitorType = TCPNetworkMonitor.NetworkMonitorType.RawSocket;
            monitor.MessageReceived = MessageReceived;
            monitor.MessageSent = MessageSent;
            monitor.ProcessID = processId;
        }

        private void MessageSent(long epoch, byte[] message, int set, FFXIVNetworkMonitor.ConnectionType connectionType)
        {
            throw new NotImplementedException();
        }

        private void MessageReceived(long epoch, byte[] message, int set, FFXIVNetworkMonitor.ConnectionType connectionType)
        {
            throw new NotImplementedException();
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
