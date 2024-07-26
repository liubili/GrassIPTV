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
        public static Process RunCommand(string Command, string sys)
        {
            Process bash = new();
            bash.StartInfo.UseShellExecute = false;
            bash.StartInfo.RedirectStandardOutput = true;
            bash.StartInfo.RedirectStandardInput = true;

            switch (sys)
            {
                case "powershell":
                case "win":
                    bash.StartInfo.FileName = "powershell";
                    bash.StartInfo.Arguments = Command;
                    break;
                case "cmd":
                    bash.StartInfo.FileName = "cmd";
                    bash.StartInfo.Arguments = Command;
                    break;
                case "linux":
                    bash.StartInfo.FileName = "/bin/bash";
                    bash.StartInfo.Arguments = "-c \"" + Command + "\""; // 替换为你的命令
                    break;
                default:
                    Console.WriteLine("Command启动错误 关闭程序");
                    return null;
            }

            
            bash.Start(); // 启动进程

            return bash;
        }

    }
}
