//***************************************************************************************
// Alex Urest, Jeff Hayslet, John Alves
// Computer Networks COSC 4342 - Dr. Kar
// Semester Project - Secure Chat Application
// Spring 2016
//***************************************************************************************

using System;
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
        public static Hashtable clientList = new Hashtable();
        //***************************************************************************************
        // Function Name: Main
        // Description:
        //
        //
        //
        //
        //
        //***************************************************************************************
        public static void Main() { server cServer = new server(); }

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
            try
            {
                // Retrieve the IP address of the local machine
                IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];

                // Create a listener and bind it to the local IP address and port 4444
                TcpListener ServerListener = new TcpListener(ipAddress, 4444);
                ServerListener.Start();
                Console.WriteLine("[+] Server Program Running!");

                //Byte[] bytes = new Byte[1024];
                //String data = null;

                // Infinite loop to make the server continually run and wait for connections
                while (true)
                {
                    Console.WriteLine("[+] Awaiting Connection...");
                    
                    // Connects to any pending client requests
                    ServerListener.BeginAcceptTcpClient(new AsyncCallback(AcceptConnection), ServerListener);

                    // Signal parent thread to wait for a connection
                    completed.WaitOne();
                }// End of while loop
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
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
            Socket clientListener = (Socket) AsyncResult.AsyncState;
            Socket clientHandler = clientListener.EndAccept(AsyncResult);

            ClientData clientState = new ClientData();
            clientState.activeListener = clientHandler;
            clientHandler.BeginReceive(clientState.buffer, 0, ClientData.BufferSize, 0, new AsyncCallback(ReceiveData), clientState);

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

        }// End of ReceiveData Function
    }
}
