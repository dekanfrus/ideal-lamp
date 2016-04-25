//************************************************************************************************************************************
// Alex Urest, Jeff Hayslet, John Alves                                                                                             **
// Computer Networks COSC 4342 - Dr. Kar                                                                                            **
// Semester Project - Secure Chat Application                                                                                       **
// Spring 2016                                                                                                                      **
// References:                                                                                                                      **
// https://msdn.microsoft.com/en-us/library/system.net.sockets.tcpclient.getstream%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396   **
// https://msdn.microsoft.com/en-us/library/system.net.sockets.tcplistener(v=vs.110).aspx                                           **
// https://msdn.microsoft.com/en-us/library/system.threading.manualresetevent%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396        **
// https://msdn.microsoft.com/en-us/library/5w7b7x5f(v=vs.110).aspx                                                                 **
// https://msdn.microsoft.com/en-us/library/5w7b7x5f%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396                                 **
// https://msdn.microsoft.com/en-us/library/fx6588te%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396                                 **
//************************************************************************************************************************************

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;


namespace ChatServer
{
    //***************************************************************************************
    // Class Name: ClientData
    // Description:
    //
    //
    //
    //
    //***************************************************************************************
    public class ClientData
    {
        // Creates a state object that allows the server to receive a string of data from the client
        // State Objects are required for Asynchronous server implementations
        public Socket activeListener = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder clientString = new StringBuilder();
        public StringBuilder clientName = new StringBuilder();
    }

    //***************************************************************************************
    // Class Name: server
    // Description:
    //
    //
    //
    //
    //
    //***************************************************************************************
    class server
    {
        // Thread signaler of a particular state (System.Threading)
        public static ManualResetEvent completed = new ManualResetEvent(false);

        // Create a list of clients
        public static List<Socket> clientList = new List<Socket>();

        //***************************************************************************************
        // Function Name: server
        // Description:
        //
        //
        //
        //
        //
        //***************************************************************************************
        public server()
        {
            // Retrieve the IP address of the local machine
            IPHostEntry host = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint local = new IPEndPoint(ipAddress, 4444);

            try
            {
                // Create a listener and bind it to the local IP address and port 4444
                Socket ServerListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ServerListener.Bind(local);
                ServerListener.Listen(100);

                Console.WriteLine("[+] Server Program Running!");
                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                {
                    logWriter.WriteLine("[+] Server Program Running!");
                }
                Console.WriteLine("[+] Awaiting Connection...");

                // Infinite loop to make the server continually run and wait for connections
                while (true)
                {
                    try
                    {
                        completed.Reset();
                        //Console.WriteLine("[+] Awaiting Connection...");

                        // Connects to any pending client requests
                        ServerListener.BeginAccept(new AsyncCallback(AcceptConnection), ServerListener);
                    }
                    catch (Exception error)
                    {
                        Console.WriteLine(error.ToString());
                        Console.WriteLine("Press any key to exit");
                        Console.ReadKey();
                    }

                    // Signal parent thread to wait for a connection
                    completed.WaitOne();

                }// End of while loop
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
            }
        } // End of server function

        //***************************************************************************************
        // Function Name: Acception Connection
        // Description:
        //
        //
        //
        //
        //***************************************************************************************
        public static void AcceptConnection(IAsyncResult AsyncResult)
        {
            // Tell parent thread to continue accepting connections
            completed.Set();

            // Grab the client socket
            Socket clientListener = (Socket)AsyncResult.AsyncState;
            Socket clientHandler = clientListener.EndAccept(AsyncResult);

            try
            {
                clientList.Add(clientHandler);
                ClientData clientState = new ClientData();
                clientState.activeListener = clientHandler;

                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                {
                    logWriter.WriteLine("[+] Connection Received");
                }
                Console.WriteLine("[+] Connection Received");

                clientHandler.BeginReceive(clientState.buffer, 0, ClientData.BufferSize, 0, new AsyncCallback(ReceiveData), clientState);
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }

        }// End of Accept function

        //***************************************************************************************
        // Function Name: ReceiveData
        // Description: 
        //
        //
        //
        //
        //***************************************************************************************
        public static void ReceiveData(IAsyncResult AsyncResult)
        {

            // Clear the message string and grab receiving client socket information

            ClientData clientState = (ClientData)AsyncResult.AsyncState;
            Socket clientHandler = clientState.activeListener;
            string clientMessage = string.Empty;

            try
            {
                int dataReceived = clientHandler.EndReceive(AsyncResult);

                // Receive data from client
                if (dataReceived > 0)
                {
                    clientState.clientString.Clear();
                    clientState.clientString.Append(Encoding.ASCII.GetString(clientState.buffer, 0, dataReceived));

                    clientMessage = clientState.clientString.ToString();

                    // Message Decryption Here

                    if (clientMessage.StartsWith("1"))
                    {
                        int success = Login();
                        switch (success)
                        {
                            case 1:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                    logWriter.WriteLine("[+] Login Successful!");
                                Console.WriteLine("[+] Login Successful!");
                                break;
                            case 2:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                    logWriter.WriteLine("[+] Login Failed!  Username/Password combination.");
                                Console.WriteLine("[+] Login Failed!  Username/Password combination.");
                                break;
                            default:
                                break;
                        }
                    }
                    else if (clientMessage.StartsWith("2"))
                    {
                        int success = Register();
                        switch (success)
                        {
                            case 1:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                    logWriter.WriteLine("[+] Registration Successful!");
                                Console.WriteLine("[+] Registration Successful!");
                                break;
                            case 2:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                    logWriter.WriteLine("[+] Registration Failed!  Invalid Email Address.");
                                Console.WriteLine("[+] Registration Failed!  Invalid Email Address.");
                                break;
                            case 3:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                    logWriter.WriteLine("[+] Registration Failed! Username already exists.");
                                Console.WriteLine("[+] Registration Failed! Username already exists.");
                                break;
                            case 4:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                    logWriter.WriteLine("[+] Registration Failed! Invalid Password.");
                                Console.WriteLine("[+] Registration Failed! Invalid Password.");
                                break;
                            default:
                                break;
                        }
                    }
                    else if (clientMessage.StartsWith("3"))
                    {
                        // Do something else
                    }
                    else
                    {
                        using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                        {
                            logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                            logWriter.Write(clientMessage);
                        }
                        BroadcastMessage(clientMessage);
                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }

            if (clientHandler.Connected)
            {
                clientHandler.BeginReceive(clientState.buffer, 0, ClientData.BufferSize, 0, new AsyncCallback(ReceiveData), clientState);
            }

        }// End of ReceiveData Function

        public static void BroadcastMessage(string recvdMessage)
        {
            Console.Write(recvdMessage);
            // Message encryption here

            byte[] data = Encoding.ASCII.GetBytes(recvdMessage);
            foreach (Socket current in clientList)
            {
                current.BeginSend(data, 0, data.Length, 0, new AsyncCallback(OnBroadcast), current);
            }
        }

        public static void OnBroadcast(IAsyncResult AsyncResult)
        {
            Socket clientSocket = (Socket)AsyncResult.AsyncState;
            clientSocket.EndSend(AsyncResult);
        }

        public static int Login()
        {
            // If credentials matched and auth
            // was successful, then return true
            // otherwise return error code
            return 1;
        }

        public static int Register()
        {
            // If signup was successful,
            // return true
            // Otherwise return error code

            return 1;
        }

        //*******************************************************************************************
        // Function Name: Main                                                                     **
        // Description:  Main function of the server program, however it just dynamically creates  **
        //               a new instance of the server() function.                                  **
        //                                                                                         **
        //*******************************************************************************************
        public static void Main() { server cServer = new server(); }
    } // End server class
}// End class
