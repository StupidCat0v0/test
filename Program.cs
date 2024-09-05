using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace TCPTransfer
{
    class HelloWorld
    {
        public struct FileStruct
        {
            public string Name { get; set; }
            public byte[] FS { get; set; }
        }
        static void Main(string[] args)
        {


            Console.WriteLine("输入0为服务器，1为客户端");
            string mode = Console.ReadLine();
            if (mode == "1")
                client();
            else
                server();

            Console.WriteLine("done!");

            Console.ReadKey();
        }
        static int Locality;
        static int size = 8192;
        static TcpClient Sclient;

        public static void server()
        {
            // IP地址和端口号
            int port = 14514;

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    Console.WriteLine("本地IPv4地址: {0}", ip);
            }

            // 创建一个新的TcpListener实例
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();

            Console.WriteLine("服务端启动，等待连接...");

            // 等待客户端连接
            Sclient = server.AcceptTcpClient();
            Console.WriteLine("客户端已连接");


            Console.Write("连接client方成功\n输入文件地址(将文件拖至此处)\n<<");

            string FileAddress = Console.ReadLine();

            // 移除两端的引号
            FileAddress = FileAddress.Trim('"');

            Locality = Path.GetDirectoryName(FileAddress).Length + 1;

            try
            {
                if (Directory.Exists(FileAddress))
                {
                    Console.WriteLine("这是一个存在的文件夹。");
                    try
                    {
                        Send(Path.GetFileName(FileAddress), FileAddress);
                        ListAllFilesAndDirectories(FileAddress);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine("没有权限访问该文件夹或其部分内容。");
                        return;
                    }
                }
                else if (File.Exists(FileAddress))
                {
                    Console.WriteLine("这是一个文件。");
                    Send(FileAddress.Substring(Locality), FileAddress);
                }
                else
                {
                    Console.WriteLine("这不是一个有效的路径，或者路径不存在。");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生错误：" + ex.Message);
                return;
            }

            // 获取网络流
            NetworkStream stream = Sclient.GetStream();
            // 发送给客户端
            byte[] responseData = Encoding.UTF8.GetBytes("over");
            stream.Write(responseData, 0, responseData.Length);

            // 清理资源
            stream.Close();
            Sclient.Close();
            server.Stop();
        }
        private static void ListAllFilesAndDirectories(string path)
        {
            // 列出所有文件
            foreach (var file in Directory.EnumerateFiles(path))
            {
                Send(file.Substring(Locality), file);
            }
            // 列出所有子文件夹
            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                Send(directory.Substring(Locality), directory);
                // 递归地列出子文件夹中的所有文件和文件夹
                ListAllFilesAndDirectories(directory);
            }
        }
        public static byte[] ObjectToBytes(object obj)
        {
            string jsonString = JsonConvert.SerializeObject(obj);
            Console.WriteLine("aaaaaaaaaaaa:    " + jsonString);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        public static FileStruct BytesToObject(byte[] bytes)
        {
            string jsonString = Encoding.UTF8.GetString(bytes);
            Console.WriteLine("aaaaaaaaaaaa:    " + jsonString);
            return JsonConvert.DeserializeObject<FileStruct>(jsonString);
        }

        public static void Send(string FileName, string FileAddress)
        {
            NetworkStream stream = Sclient.GetStream();
            byte[] buffer = new byte[1024];
            buffer = Encoding.Default.GetBytes(FileName);
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();  // 别忘了刷新流以确保数据被发送


            byte[] bytes = new byte[size];
            stream = Sclient.GetStream();
            FileStream fs = new FileStream(FileAddress, FileMode.Open, FileAccess.Read);


            int bytesRead;
            // Read the file in chunks and send each chunk.
            while ((bytesRead = fs.Read(bytes, 0, size)) > 0)
            {
                stream.Write(bytes, 0, bytesRead);
            }
            stream.Flush();  // 别忘了刷新流以确保数据被发送



            Console.WriteLine("File received");
        }
        public static void client()
        {
            Console.Write("输入server ip地址：");
            // IP地址和端口号
            string ip = Console.ReadLine();
            int port = 14514;
            Console.WriteLine("服务端已连接");
            // 创建一个新的TcpClient实例
            TcpClient client = new TcpClient(ip, port);

            // 获取网络流
            NetworkStream stream = client.GetStream();
            //try

            byte[] buffer = new byte[1024];
            stream.Read(buffer, 0, buffer.Length);
            string FileName = Encoding.Default.GetString(buffer, 0, 1024);
            stream.Flush();  // 别忘了刷新流以确保数据被发送

            Console.WriteLine("接收到的数据: " + FileName);





            stream = client.GetStream();
            byte[] data = new byte[size];
            FileName = FileName.Trim('\0');
            string filea = "File\\" + FileName;
            if (!File.Exists(filea))
            {
                //参数1：要创建的文件路径，包含文件名称、后缀等
                FileStream fs = File.Create(filea);
                fs.Close();
                Console.WriteLine("文件创建成功！");
            }
            else
            {
                Console.WriteLine("文件已经存在！");
            }
            Console.WriteLine(Directory.GetCurrentDirectory() + "\\" + filea);


            // 接收来自服务器的响应
            FileStream fs = new FileStream(Directory.GetCurrentDirectory() + "\\" + filea, FileMode.Create, FileAccess.Write);    

            int bytesRecvd;
            while ((bytesRecvd = stream.Read(data, 0, size)) != 0)
            {
                fs.Write(data, 0, bytesRecvd);
            }
            stream.Flush();  // 别忘了刷新流以确保数据被发送





            //catch (Exception ex)
            //{
            //    Console.WriteLine("发生错误：" + ex.Message);
            //    return;
            //}


            // 清理资源
            stream.Close();
            client.Close();

        }
    }
}