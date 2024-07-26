using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GrassIPTV
{
    public class Run
    {
        public static void RunCommand(string Command, string sys)
        {
           



            Process bash = new();
            switch (sys)
            {
                case "powershell":
                case "win":
                    bash.StartInfo.FileName = "powershell";
                    break;
                case "cmd":
                    bash.StartInfo.FileName = "cmd";
                    break;
                case "linux":
                    bash.StartInfo.FileName = "bash";
                    break;
                default:
                    Console.WriteLine("Command启动错误 关闭程序");
                    break;
            }
            bash.StartInfo.Arguments = Command; // 替换为你的命令
            bash.Start(); // 启动进程
            bash.WaitForExit();
            bash.Close();

        }
    }
}
