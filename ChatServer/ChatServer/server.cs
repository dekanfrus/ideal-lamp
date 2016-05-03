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
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Security.Cryptography;
using System.Linq;
using System.Data.SqlClient;
using System.Configuration;


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

        // Salt and Initialization Vector values for encryption of data
        private static byte[] Salt = {2, 123,  61, 217, 205, 133, 176, 171, 164, 248, 215, 129, 232, 210, 145, 56, 
                                      45, 133,  55, 137,  95, 174, 245, 179, 205, 140, 190, 215, 110, 122, 169, 95 };
        private static byte[] IV = {9,  90,  56,  18, 127, 245, 101, 112,  72, 133, 248, 224,  73,  12,  96,  24, };

        private static ICryptoTransform Encryptor, Decryptor;
        private static System.Text.UTF8Encoding Encoder;

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
                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                    logWriter.WriteLine("[+] Server Program Running!");
                }
                
                //Attempt to establish a connection with the database
                server.ConnectToDB();

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
                // Add the client to the client list then create a new state object to store
                // the data about the client and received messages.
                clientList.Add(clientHandler);
                ClientData clientState = new ClientData();
                clientState.activeListener = clientHandler;

                // Add a log entry for the connected client
                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                {
                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                    logWriter.WriteLine("[+] Connection Received");
                }
                Console.WriteLine("[+] Connection Received");

                // Send updated users list to all clients
                BroadcastUsers();

                // Begin receiving data from the client
                clientHandler.BeginReceive(clientState.buffer, 0, ClientData.BufferSize, 0, new AsyncCallback(ReceiveData), clientState);
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }

        }// End of Accept function

        //***************************************************************************************
        // Function Name: ConnectToDB
        // Description: 
        // Attempts to establish a connection with the EC2 MSSQL database 
        // 
        //
        //
        //***************************************************************************************
        public static bool ConnectToDB()
        {
            //Check to see if the server can initiate a connection to the database server - ADU
            Console.WriteLine("[+] Checking to see if the database can connect");
            try //only using this portion to see how to create sql queries on passwords
            {
                using (SqlConnection dbConnection = new SqlConnection())
                {
                    //String that contains connection info for database... Encryption option is not supported. Need to check connection string to see how to implement it
                    dbConnection.ConnectionString = @"Server=ec2-52-4-79-59.compute-1.amazonaws.com, 1433; Database=chatserver; User Id= Administrator; Password=U%GT4nDTZk|dX-A\ZrS*%Imm,A";

                    //Open up connection to database
                    dbConnection.Open();

                    //just for debugging purposes will remove once code is in production - ADU
                    Console.WriteLine("[+] DB connected!");

                    //return true; 
                    //more debugging here to see if I can query items on that database.
                    SqlCommand command = new SqlCommand("SELECT * FROM information_schema.tables", dbConnection); // Need to verify how this will work....

                    
                    //just for debugging purposes to read queried response will modify code - ADU
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine("FirstColumn\tSecond Column\t\tThird Column\t\tFourth Column\t");
                        int count = 0;
                        while (reader.Read())
                        {
                            Console.WriteLine(reader[count++]);
                        }
                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
                return false; //database did not connect successfully....cannot authenticate users
            }
                        
            return true;
        }// End of ConnectToDB Function

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
                    // Clear any existing data in the client object string
                    clientState.clientString.Clear();
                    // Append received data to the client object string
                    clientState.clientString.Append(Encoding.ASCII.GetString(clientState.buffer, 0, dataReceived));

                    // Convert the message from a StringBuilder to string
                    string EncryptedMessage = clientState.clientString.ToString();

                    // Decrypt the client message
                    clientMessage = DecryptData(EncryptedMessage);

                    // Handle Login code
                    if (clientMessage.StartsWith("1000"))
                    {
                        // Call Login function to query the database and verify credentials
                        int success = Login(clientMessage);

                        // Depending on authentication results, log and proceed
                        switch (success)
                        {
                            case 1:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                {
                                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                                    logWriter.WriteLine("[+] Login Successful!");
                                }
                                Console.WriteLine("[+] Login Successful!");
                                break;
                            case 2:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                {
                                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                                    logWriter.WriteLine("[+] Login Failed!  Username/Password combination.");
                                }
                                Console.WriteLine("[+] Login Failed!  Username/Password combination.");
                                break;
                            default:
                                break;
                        }
                    } // End of Login handler

                    // Handle Register Code
                    else if (clientMessage.StartsWith("2000"))
                    {
                        // Call Register function and receive result code
                        int success = Register(clientMessage);

                        // Depending on the results from Register, log and proceed
                        switch (success)
                        {
                            case 1:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                {
                                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                                    logWriter.WriteLine("[+] Registration Successful!");
                                }
                                Console.WriteLine("[+] Registration Successful!");
                                break;
                            case 2:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                {
                                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                                    logWriter.WriteLine("[+] Registration Failed!  Invalid Email Address.");
                                }
                                Console.WriteLine("[+] Registration Failed!  Invalid Email Address.");
                                break;
                            case 3:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                {
                                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                                    logWriter.WriteLine("[+] Registration Failed! Username already exists.");
                                }
                                Console.WriteLine("[+] Registration Failed! Username already exists.");
                                break;
                            case 4:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                {
                                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                                    logWriter.WriteLine("[+] Registration Failed! Invalid Password.");
                                }
                                Console.WriteLine("[+] Registration Failed! Invalid Password.");
                                break;
                            default:
                                break;
                        }
                    } // End of Register handler

                    // Placeholder for additional codes
                    else if (clientMessage.StartsWith("3"))
                    {
                        // Do something else
                    }

                    // Broadcast Message Handler
                    else
                    {
                        // Log the message then call the BroadcastMessage function
                        using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                        {
                            logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                            logWriter.Write(clientMessage);
                        }
                        BroadcastMessage(clientMessage);
                    }// End of Broadcast Message Hanler
                }// End of receive data if statement
            }// End of try block
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }

            // If the client socket is still connected, call the function recursively
            // This is necessary otherwise the thread will exit and the client will no longer be able to send data
            if (clientHandler.Connected)
            {
                try
                {
                    clientHandler.BeginReceive(clientState.buffer, 0, ClientData.BufferSize, 0, new AsyncCallback(ReceiveData), clientState);
                }
                catch (Exception error)
                {
                    using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                    {
                        logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                        logWriter.Write(error);
                    }
                    Console.WriteLine(error.ToString());
                }
            }

            if (!clientHandler.Connected)
            {
                // Remove clientState.activeListener from the linked list
                // Then send new Users data to the client
                BroadcastUsers();
            }

        }// End of ReceiveData Function

        //*******************************************************************************************
        // Function Name: BroadcastMessage                                                         **
        // Description:  
        //               
        //                                                                                         **
        //*******************************************************************************************
        public static void BroadcastMessage(string recvdMessage)
        {
            // Output to server console.  Primarily for testing purposes.
            Console.Write("Plain Text Message: ");
            Console.Write(recvdMessage);
            recvdMessage = EncryptData(recvdMessage);
            Console.Write("Encrypted Message: ");
            Console.WriteLine(recvdMessage);

            // Convert the message string to a byte array and then send
            // all data to all sockets in the client list (list<socket>)
            byte[] data = Encoding.ASCII.GetBytes(recvdMessage);
            foreach (Socket current in clientList)
            {
                current.BeginSend(data, 0, data.Length, 0, new AsyncCallback(OnBroadcast), current);
            }
        }// End of BroadcastMessage function

        //*******************************************************************************************
        // Function Name: BroadcastUsers                                                           **
        // Description:  
        //               
        //                                                                                         **
        //*******************************************************************************************
        public static void BroadcastUsers()
        {
            //Convert List<socket> to a string with ":" delimiter and code 3000
            //Send string to encryptData() function
            //Once encrypted, broadcast to all sockets

            //recvdMessage = encryptData(recvdMessage);

            //byte[] data = Encoding.ASCII.GetBytes(clientList);
            //foreach (Socket current in clientList)
            //{
            //    current.BeginSend(data, 0, data.Length, 0, new AsyncCallback(OnBroadcast), current);
            //}
        }// End of BroadcastUsers function

        //*******************************************************************************************
        // Function Name: OnBroadcast                                                              **
        // Description:  
        //               
        //                                                                                         **
        //*******************************************************************************************
        public static void OnBroadcast(IAsyncResult AsyncResult)
        {
            Socket clientSocket = (Socket)AsyncResult.AsyncState;
            clientSocket.EndSend(AsyncResult);
        }

        //*******************************************************************************************
        // Function Name: Login                                                                    **
        // Description:  
        //               
        //                                                                                         **
        //*******************************************************************************************
        public static int Login(string loginInfo)
        {
            // Split the string based on ":" and store in a string array
            // 
            string[] creds = loginInfo.Split(':');

            string userName = creds[1];
            string userPassword = creds[2];
            
            //string PasswordHashed = CreatePasswordHash(userPassword, salt);

            // If credentials matched and auth
            // was successful, then return true
            // otherwise return error code
            return 1;
        }// End of Login function

        //*******************************************************************************************
        // Function Name: Register                                                                 **
        // Description:  
        //               
        //                                                                                         **
        //*******************************************************************************************
        public static int Register(string RegisterInfo)
        {
            string[] creds = RegisterInfo.Split(':');

            string userName = creds[1];
            string userPassword = creds[2];
            string userEmail = creds[3];
            string userFirstName = creds[4];
            string userLastName = creds[5];

            // If signup was successful,
            // return true
            // Otherwise return error code
            
            return 1;
        }

        //*******************************************************************************************
        // Function Name: EncryptData                                                              **
        // Description:  
        //               
        //                                                                                         **
        //*******************************************************************************************
        public static string EncryptData(string message)
        {
            // Create a new object with the RM (AES) algorithm
            RijndaelManaged AESEncrypt = new RijndaelManaged();

            // Populate the encryptor/decryptor with the salt and initialization vector
            Encryptor = AESEncrypt.CreateEncryptor(Salt, IV);
            Decryptor = AESEncrypt.CreateDecryptor(Salt, IV);

            // Specify the type of encoding we want to use
            Encoder = new System.Text.UTF8Encoding();

            try
            {
                // Convert the message to a byte array
                Byte[] MessageBytes = Encoding.UTF8.GetBytes(message);
                
                // Create a memory stream object to stream the message to the crypto stream object
                // The Crypto stream object requires a stream object - cannot use a byte array directly
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, Encryptor, CryptoStreamMode.Write);

                // Write the message to the Crypto Stream object encrypting the data
                cStream.Write(MessageBytes, 0, MessageBytes.Length);
                cStream.FlushFinalBlock();

                // Read the encrypted data back from the Crypto Stream object
                mStream.Position = 0;
                byte[] EncryptedMessage = new byte[mStream.Length];
                mStream.Read(EncryptedMessage, 0, EncryptedMessage.Length);

                // Close the streams because it's a good idea
                cStream.Close();
                mStream.Close();

                // Return the encrypted string back to the calling function
                return System.Text.Encoding.UTF8.GetString(EncryptedMessage);
            }
            catch (Exception error)
            {
                // Basic logging.  Send the error to the console and the server log, then return an empty string
                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                {
                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                    logWriter.Write(error.ToString());
                }
                Console.WriteLine(error.ToString());
                return " ";
            }
        }

        //*******************************************************************************************
        // Function Name: DecryptData                                                              **
        // Description:  
        //               
        //                                                                                         **
        //*******************************************************************************************
        public static string DecryptData(string message)
        {
            Byte[] MessageBytes = Encoding.UTF8.GetBytes(message);
            return System.Text.Encoding.UTF8.GetString(MessageBytes);
        }

        //*******************************************************************************************
        // Function Name: DecryptData                                                              **
        // Description:  
        //               
        //                                                                                         **
        //*******************************************************************************************
        /*
        private static string CreatePasswordHash(string userPassword, string salt)
        {
            string SaltAndPassword = String.Concat(userPassword, salt);
            string hashedPassword = FormsAuthentication.HashPasswordForStoringInConfigFile(SaltAndPassword, "SHA1");
            hashedPassword = String.Concat(hashedPassword, salt);
            return hashedPassword;
        }*/
        //*******************************************************************************************
        // Function Name: Main                                                                     **
        // Description:  Main function of the server program, however it just dynamically creates  **
        //               a new instance of the server() function.                                  **
        //                                                                                         **
        //*******************************************************************************************
        public static void Main() { server cServer = new server(); }
    } // End server class
}// End class
