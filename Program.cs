using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Text.Encodings.Web;
using System.Security.Cryptography;
using System.Text;
using System.Net.Sockets;
using System;
using System.Net.NetworkInformation;

namespace GrassIPTV
{
    internal class Program
    {
        private static Process currentMpvProcess = null;
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            #region 判断系统部分
            string sys;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                sys = "win";
               
                Console.WriteLine("当前环境是windows");
                
                Run.RunCommand("winget install mpv --accept-source-agreements --silent --accept-package-agreements", sys);
                

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("当前环境是Linux");
                sys = "linux";
                Run.RunCommand("sudo apt install mpv", sys);
            }
            else
            {
                Console.WriteLine("未知环境 程序退出");
                return;
            }
            #endregion

            #region 获取列表
            string Vision = "0.0.1";
            Console.WriteLine("启动IPTV主程序");
            Console.WriteLine("版本:" + Vision);
            //获取当前播放列表

            string url = "https://raw.githubusercontent.com/suxuang/myIPTV/main/boxTV.m3u";
            string localPath = "boxTV.m3u";
            try
            {
                Console.WriteLine("正在使用github更新源");
                using WebClient client = new();
                client.DownloadFile(url, localPath);

            }
            catch (Exception)
            {
                Console.WriteLine("github失败 正在使用liubiligrass更新源");
                url = "https://liubiligrass.com/GRASSIPTV/boxTV.m3u";
                try
                {
                    using (WebClient client = new())
                    {
                        client.DownloadFile(url, localPath);
                    }

                }
                catch (Exception)
                {
                    Console.WriteLine("彻底失败 使用缓存源 能看多少看上天");
                }
            }
            // 下载文件



            // 解析m3u文件
            var lines = File.ReadAllLines(localPath);
            var channels = new List<Dictionary<string, string>>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("#EXTINF"))
                {
                    // 获取频道名称
                    var channelName = lines[i].Substring(lines[i].IndexOf(",") + 1);
                    // 获取频道URL
                    var channelUrl = lines[i + 1];
                    // 添加到频道列表
                    channels.Add(new Dictionary<string, string> { { channelName, channelUrl } });
                }
            }

            // 创建JsonSerializerOptions对象
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // 转换为json
            var json = JsonSerializer.Serialize(channels, options);
            //解析本地IP
            string localIpAddress = GetLocalIPAddress().ToString();
            Console.WriteLine("本机地址:" + localIpAddress);
            // 输出json到文件
            string list = "html/list.json";
            File.WriteAllText(list, json);
            #endregion

            #region 启动监听
            HttpListener listener = new HttpListener();
            string urls = $"http://{localIpAddress}:2545/";

            listener.Prefixes.Add(urls);
            Console.WriteLine("开始监听");
            Console.WriteLine(urls);
            listener.Start();
            #endregion
            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                #region 获取后端机IP
                if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/ip")
                {
                    // 返回IP地址
                    byte[] buffer = Encoding.UTF8.GetBytes(localIpAddress);
                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                }
                #endregion

                # region 频道接口
                if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/get-channel-info")
                {
                    // 读取你的 JSON 文件
                    var jsonf = File.ReadAllText("html/list.json");
                    // 将 JSON 文件的内容发送到前端
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonf);
                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
                #endregion

                #region HTML主页面
                if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/")
                {
                    // 读取你的 HTML 文件
                    var html = File.ReadAllText("html/index.html");
                    // 将 HTML 文件的内容发送到前端
                    byte[] buffer = Encoding.UTF8.GetBytes(html);
                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
                #endregion

                #region CSS页面
                if (request.HttpMethod == "GET" && request.Url.AbsolutePath.EndsWith(".css"))
                {
                    // 获取 CSS 文件的路径
                    var cssFilePath = Path.Combine("html", request.Url.AbsolutePath.TrimStart('/'));

                    // 检查 CSS 文件是否存在
                    if (File.Exists(cssFilePath))
                    {
                        // 读取 CSS 文件
                        var css = File.ReadAllText(cssFilePath);

                        // 将 CSS 文件的内容发送到前端
                        byte[] buffer = Encoding.UTF8.GetBytes(css);
                        response.ContentLength64 = buffer.Length;
                        Stream output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);
                        output.Close();
                    }
                    else
                    {
                        // 如果 CSS 文件不存在，返回 404 错误
                        response.StatusCode = 404;
                        response.Close();
                    }

                }
                #endregion

                #region 频道json
                if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/get-channel-name")
                {
                    // 读取请求的正文
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    var content = reader.ReadToEnd();

                    // 解析 JSON
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(content);

                    // 获取频道名称
                    var channelName = data["channelName"];

                    // 解析你的 JSON 文件
                    var channelsa = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(File.ReadAllText("html/list.json"));

                    // 查找与 channelName 对应的值
                    string channelUrl = null;
                    foreach (var channel in channelsa)
                    {
                        if (channel.ContainsKey(channelName))
                        {
                            channelUrl = channel[channelName];
                            break;
                        }
                    }


                    // 输出频道 URL
                    if (channelUrl != null)
                    {
                        Console.WriteLine(channelUrl);
                        // 如果当前有正在运行的mpv进程，结束它
                        if (currentMpvProcess != null)
                        {
                            if (!currentMpvProcess.HasExited)
                            {
                                // 使用命令行工具结束mpv进程
                                if (sys == "linux")
                                {
                                    Run.RunCommand("pkill mpv", sys);
                                }
                                else if (sys == "win")
                                {
                                    Run.RunCommand("taskkill /IM mpv.exe /F", sys);
                                }
                                System.Threading.Thread.Sleep(1000); // 等待1秒
                            }
                            currentMpvProcess = null;
                        }
                        // 启动新的mpv进程
                        currentMpvProcess = Run.RunCommand("mpv " + channelUrl, sys);
                    }
                    else
                    {
                        Console.WriteLine("频道未找到: " + channelName);
                    }




                    // 返回一个空的响应
                    response.StatusCode = 200;
                    response.Close();
                }
                #endregion

                #region OPTIONS处理
                if (request.HttpMethod == "OPTIONS")
                {
                    // 添加 CORS 头
                    response.AddHeader("Access-Control-Allow-Origin", "*");
                    response.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
                    response.StatusCode = 200;
                    response.Close();
                }
                #endregion


            }

        }

        #region 静态模块:获取本地IP地址
        static string GetLocalIPAddress()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
                {
                    continue;
                }

                if (ni.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.Address.ToString();
                    }
                }
            }

            throw new Exception("No network adapters with an IPv4 address in the system!");
            #endregion

        }
    }
}
