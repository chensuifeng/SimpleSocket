using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SimpleService
{
    class Service
    {
        private static Socket _socket = null;
        private static ManualResetEvent manual = new ManualResetEvent(false);
        public static void Start() 
        {
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(iPEndPoint);
            _socket.Listen(100);
            _socket.Blocking = false;
            AcceptAsync(null);
            manual.WaitOne();
        }

        private static void AcceptAsync(SocketAsyncEventArgs e)
        {
            if (e == null)
            {
                e = new SocketAsyncEventArgs();
                e.Completed += AcceptEvent;
            }
            else
            {
                // socket must be cleared since the context object is being reused
                e.AcceptSocket = null;
            }

            bool isAccept = _socket.AcceptAsync(e);
            if (!isAccept)
            {
                ProcessAccept(e);
            }
        }
        private static void AcceptEvent(object s, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private static void ProcessAccept(SocketAsyncEventArgs e)
        {
            Socket client = e.AcceptSocket;
            Console.WriteLine($"[{client.GetHashCode()}] connected...");
            SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs();
            readEventArgs.Completed += IO_Completed;
            readEventArgs.UserToken = client;
            readEventArgs.SetBuffer(new byte[8192], 0, 8192);
            bool willRaiseEvent = client.ReceiveAsync(readEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(readEventArgs);
            }


            AcceptAsync(e);
        }

        private static void ProcessSend(SocketAsyncEventArgs e)
        {
            Socket socket = (Socket)e.UserToken;
            e.SetBuffer(new byte[1024], 0, 1024);
            bool willRaiseEvent = socket.ReceiveAsync(e);
            if (!willRaiseEvent)
            {
                ProcessReceive(e);
            }
        }
        private static void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    Socket socket = (Socket)e.UserToken;
                    if (socket.Available == 0)
                    {
                        string str = Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{str}");

                        byte[] bytes = Encoding.UTF8.GetBytes("ok");
                        e.SetBuffer(bytes, 0, bytes.Length);
                        bool willRaiseEvent = socket.SendAsync(e);
                        if (!willRaiseEvent)
                        {
                            ProcessSend(e);
                        }
                    }
                    else
                    {
                        if (!socket.ReceiveAsync(e))
                        {
                            ProcessReceive(e);
                        }
                    }
                }
                else
                {
                    CloseClient(e);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                CloseClient(e);
                Console.ReadKey();
            }
        }


        private static void IO_Completed(object s, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    break;
                    //throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        private static void CloseClient(SocketAsyncEventArgs e)
        {
            Socket socket = (Socket)e.UserToken;
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            Console.WriteLine($"[{socket.GetHashCode()}] closed");
        }
    }
}
