using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SimpleClient
{
    class Client
    {
        private static Socket _socket;
        private static IPEndPoint _endpoint;
        private static ManualResetEvent allDone = new ManualResetEvent(false);

        public static void Start() 
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            _endpoint = new IPEndPoint(address, 9000);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ConnectAsync(null);
            allDone.WaitOne();
        }
        private static void ConnectAsync(SocketAsyncEventArgs e) 
        {
            if (e == null) 
            {
                e = new SocketAsyncEventArgs();
                e.Completed += IO_Completed;
                e.RemoteEndPoint = _endpoint;
            }
            if (!_socket.ConnectAsync(e))
            {
                ProcessConnect(e);
            }
        }

        private static void ProcessConnect(SocketAsyncEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.White;
            string msg = Console.ReadLine();
            byte[] bytes = Encoding.UTF8.GetBytes(msg);
            e.SetBuffer(bytes, 0, bytes.Length);
            e.SendPacketsSendSize = bytes.Length;
            bool isRaiseEvent = _socket.SendAsync(e);
            if (!isRaiseEvent)
            {
                ProcessSend(e);
            }
        }
        private static void ProcessSend(SocketAsyncEventArgs e)
        {
            e.SetBuffer(new byte[8192], 0, 8192);
            bool isRaiseEvent = _socket.ReceiveAsync(e);
            if (!isRaiseEvent)
            {
                ProcessReceive(e);
            }
        }
        private static void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {

                if (_socket.Available == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"{Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred)}");

                    ProcessConnect(e);
                }
                else
                {
                    if (!_socket.ReceiveAsync(e))
                    {
                        ProcessReceive(e);
                    }
                }
            }
            else
            {
                CloseClient();

            }
        }

        private static void IO_Completed(object s, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                default:
                    break;
            }
        }

        private static void CloseClient()
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("colsed");
        }
    }
}
