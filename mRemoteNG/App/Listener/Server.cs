using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using mRemoteNG.UI.Forms;

namespace mRemoteNG.App.Listener
{
    public class Server
    {
        private IPHostEntry ipHost;
        private IPAddress ipAddress;
        private IPEndPoint endPoint;
       
        public Server(int port)
        {
            BuildEndpoint(port);
        }

        private void BuildEndpoint(int port)
        {
            ipHost = Dns.GetHostEntry("localhost");

            // We want IPV4 address
            ipAddress = Array.Find(ipHost.AddressList, ip => ip.AddressFamily == AddressFamily.InterNetwork);

            endPoint = new IPEndPoint(ipAddress, port);
        }

        public int InitServer()
        {
            int returnCode = 0;
            try
            {
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(endPoint);

                listener.Listen(10);

                while (true)
                {
                    Debug.Print($"mRemoteNG socket server active on port {endPoint.Port}.");

                    Socket clientSocket = listener.Accept();

                    // timeout reads and writes in 5 seconds
                    clientSocket.ReceiveTimeout = 5000;
                    clientSocket.SendTimeout = 5000;

                    HandleClientAsync(clientSocket);
                }
            }
            catch (SocketException)
            {
                returnCode = 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                returnCode = 2;
            }

            return returnCode;
        }

        private void HandleClientAsync(Socket clientSocket)
        {
            Thread handlerThread = new Thread(() =>
            {
                try
                {
                    Debug.Print("Reading from socket.");
                    string messageReceived = readFromSocket(clientSocket);

                    Debug.Print($"Message received [{messageReceived}].");

                    string response = HandleMessage(messageReceived);
                    byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                    int byteSent = clientSocket.Send(responseBytes);

                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
                catch (Exception e)
                {
                   Console.WriteLine(e);
                }
            });
            handlerThread.Start();
        }

        /* 
            Do not send null response from here.
        */
        private string HandleMessage(string message)
        {
            int status = 1;

            try
            {
                if (message != null)
                {
                    JObject messageUnserialized = (JObject)JsonConvert.DeserializeObject(message);

                    // process my message, and modify response as needed
                    string messageType = messageUnserialized.GetValue("messageType")?.Value<string>() ?? "";
                    switch (messageType)
                    {
                        case "test":

                            // DO STUFF HERE
                            
                            status = 0;

                            break;
                        case "command":

                            /*
                                Plan to support following command names
                                1. resetLayout
                                2. establishConnection
                            */

                            string commandName = messageUnserialized.GetValue("name")?.Value<string>() ?? "";
                            switch (commandName)
                            {
                                case "resetLayout":

                                    FrmMain.Default.SetDefaultLayout();

                                    break;
                                default:
                                    break;
                            }

                            break;
                        default:

                            // type not supported

                            break;
                    }

                    status = 0;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            JObject response = new JObject
            {
                {"status", status }
            };

            var responseSerialized = JsonConvert.SerializeObject(response);
            return responseSerialized;
        }

        private static string readFromSocket(Socket socket)
        {
            try
            {
                string messageReceived = "";

                int maxBufferSize = 1024;
                int bufferSize = maxBufferSize;
                byte[] buffer = new byte[maxBufferSize];

                while (bufferSize >= maxBufferSize)
                {
                    bufferSize = socket.Receive(buffer);
                    if (bufferSize > 0)
                    {
                        messageReceived += Encoding.ASCII.GetString(buffer, 0, bufferSize);
                    }
                }

                return messageReceived;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }
    }
}
