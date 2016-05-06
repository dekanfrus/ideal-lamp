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
        private static byte[] IV = { 9, 90, 56, 18, 127, 245, 101, 112, 72, 133, 248, 224, 73, 12, 96, 24, };

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

                Console.WriteLine("[+] GURU Server Program Running!");
                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                {
                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                    logWriter.WriteLine("[+] GURU Server Program Running!");
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
        public static bool ConnectToDB(string userName, string userPassword)
        {

            //Check to see if the server can initiate a connection to the database server - ADU
            Console.WriteLine("[+] Checking to see if the database is connected...");

            SqlConnection dbConnection = new SqlConnection();

            try
            {

                //String that contains connection info for database... Encryption option is not supported. Need to check connection string to see how to implement it
                dbConnection.ConnectionString = @"Server=ec2-52-4-79-59.compute-1.amazonaws.com, 1433; Database=chatserver; User Id= Administrator; Password=U%GT4nDTZk|dX-A\ZrS*%Imm,A";

                //Open up connection to database
                dbConnection.Open();

                //just for debugging purposes will remove once code is in production - ADU
                Console.WriteLine("[+] DB connected!");


                //Check to see if we can pull down the salt from the user Salt
                SqlCommand conn = new SqlCommand("SELECT userSalt FROM [User] WHERE username = @User", dbConnection);

                conn.Parameters.Add("@User", SqlDbType.VarChar);
                conn.Parameters["@User"].Value = userName;
                string userSalt = (string)conn.ExecuteScalar();

                //Create a hash and send see if it matches the database
                string hashRetrievedUserPass = CreatePasswordHash(userPassword, userSalt);

                //The sql input to check if records exist
                string sqlUserCommand = "SELECT COUNT(*) FROM [User] WHERE username=@User AND userpassword=@Password";


                //Console.WriteLine("Hashed User Pass: " + hashRetrievedUserPass); - Debugging purposes only - ADU
                // The actual command should come from the login or register function rather than being hard coded here - JA
                // However, this is the syntax.  We should also consider paramaterizing the input to prevent SQLi - JA
                //string sqlCommand = ("Select * FROM [User] WHERE username ="+userCreds);

                SqlCommand command = new SqlCommand(sqlUserCommand, dbConnection); 

                //Paramterizing SQL input - ADU
                command.Parameters.Add("@User", SqlDbType.VarChar);
                command.Parameters["@User"].Value = userName;

                command.Parameters.Add("@Password", SqlDbType.VarChar);
                command.Parameters["@Password"].Value = hashRetrievedUserPass;

                int userCount = (int)command.ExecuteScalar();

                if (userCount > 0)
                {
                    dbConnection.Close();
                    return true;
                }
                else
                {
                    dbConnection.Close();
                    return false;
                }

            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
                return false;
            }
            finally
            {
                dbConnection.Close();
            }
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
                        byte[] clientReturnInt;
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
                                clientReturnInt = Encoding.ASCII.GetBytes("1300");
                                clientHandler.Send(clientReturnInt);
                                break;
                            case 2:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                {
                                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                                    logWriter.WriteLine("[+] Login Failed!  Username/Password combination.");
                                }
                                Console.WriteLine("[+] Login Failed!  Username/Password combination.");
                                clientReturnInt = Encoding.ASCII.GetBytes("1200");
                                clientHandler.Send(clientReturnInt);
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
                        byte[] clientReturnInt;

                        // Depending on the results from Register, log and proceed
                        switch (success)
                        {
                            case 1:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                {
                                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                                    logWriter.WriteLine("[+] Registration Successful!");
                                    clientReturnInt = Encoding.ASCII.GetBytes("2300");
                                    clientHandler.Send(clientReturnInt);
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

            //Need to parameterize the sqlCommand with @symbol to read only as string
            //to prevent SQLi

            
            if (ConnectToDB(userName, userPassword))
            {
                //Console.WriteLine("Username and password verified"); - For debugging if you need to see if it's being authenticated on server side
                return 1;
            }
            else
            {
                //Console.WriteLine("Username and password combo is bad"); - For debugging if you need to see if it's being authenticated on server side
                return 2;
            }

            // If credentials matched and auth
            // was successful, then return true
            // otherwise return error code
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

            int result;

            result = ConnectToDB(userName, userPassword, userEmail, userFirstName, userLastName);

            return result;
            // If signup was successful,
            // return true
            // Otherwise return error code

            //return 1;
        }

        //*******************************************************************************************
        // Function Name: EncryptData                                                              **
        // Description:  
        //               
        //                                                                                         **
        //*******************************************************************************************
        public static string EncryptData(string message)
        {

            string eMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(message.Split('\0').First()));

            return eMessage;

            //// Create a new object with the RM (AES) algorithm
            //RijndaelManaged AESEncrypt = new RijndaelManaged();

            //// Populate the encryptor/decryptor with the salt and initialization vector
            //Encryptor = AESEncrypt.CreateEncryptor(Salt, IV);

            //// Specify the type of encoding we want to use
            //// Encoder = new System.Text.UTF8Encoding();

            //try
            //{
            //    // Convert the message to a byte array
            //    Byte[] MessageBytes = Convert.FromBase64String(message);

            //    // Create a memory stream object to stream the message to the crypto stream object
            //    // The Crypto stream object requires a stream object - cannot use a byte array directly
            //    MemoryStream mStream = new MemoryStream();
            //    CryptoStream cStream = new CryptoStream(mStream, Encryptor, CryptoStreamMode.Write);

            //    // Write the message to the Crypto Stream object encrypting the data
            //    cStream.Write(MessageBytes, 0, MessageBytes.Length);
            //    cStream.FlushFinalBlock();

            //    // Read the encrypted data back from the Crypto Stream object
            //    mStream.Position = 0;
            //    byte[] EncryptedMessage = new byte[mStream.Length];
            //    mStream.Read(EncryptedMessage, 0, EncryptedMessage.Length);

            //    // Close the streams because it's a good idea
            //    cStream.Close();
            //    mStream.Close();

            //    // Return the encrypted string back to the calling function
            //    return Convert.ToBase64String(EncryptedMessage);
            //}
            //catch (Exception error)
            //{
            //    // Basic logging.  Send the error to the console and the server log, then return an empty string
            //    using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
            //    {
            //        logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
            //        logWriter.Write(error.ToString());
            //    }
            //    Console.WriteLine(error.ToString());
            //    return " ";
            //}
                }

        //*******************************************************************************************
        // Function Name: DecryptData                                                              **
        // Description:  
        //               
        //                                                                                         **
        //*******************************************************************************************
        public static string DecryptData(string message)
        {
            string dMessage = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(message.Split('\0').First()));

            return dMessage;
            //// Create a new object with the RM (AES) algorithm
            //RijndaelManaged AESEncrypt = new RijndaelManaged();

            //// Populate the encryptor/decryptor with the salt and initialization vector
            //Decryptor = AESEncrypt.CreateDecryptor(Salt, IV);

            //// Specify the type of encoding we want to use
            //Encoder = new System.Text.UTF8Encoding();

            //try
            //{
            //    Byte[] EncryptedMessageBytes = Encoding.UTF8.GetBytes(message);

            //    MemoryStream encryptedMessage = new MemoryStream();
            //    CryptoStream decryptedMessage = new CryptoStream(encryptedMessage, Decryptor, CryptoStreamMode.Write);

            //    decryptedMessage.Write(EncryptedMessageBytes, 0, EncryptedMessageBytes.Length);
            //    //decryptedMessage.FlushFinalBlock();

            //    encryptedMessage.Position = 0;
            //    Byte[] DecryptedMessageBytes = new Byte[encryptedMessage.Length];

            //    encryptedMessage.Read(DecryptedMessageBytes, 0, DecryptedMessageBytes.Length);

            //    encryptedMessage.Close();
            //    //decryptedMessage.Close();

            //    return System.Text.Encoding.UTF8.GetString(DecryptedMessageBytes);
            //}
            //catch (Exception error)
            //{
            //    // Basic logging.  Send the error to the console and the server log, then return an empty string
            //    using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
            //    {
            //        logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
            //        logWriter.Write(error.ToString());
            //    }
            //    Console.WriteLine(error.ToString());
            //    return " ";
            //}
            
        }

        //***************************************************************************************
        // Function Name: ConnectToDB
        // Description: 
        // Attempts to establish a connection with the EC2 MSSQL database
        // Overloaded method which is used in the registration. 
        // Passwords are sent to be hashed once they are received.
        // Small checking of user input is done 
        //***************************************************************************************
        public static int ConnectToDB(string userName, string userPassword, string userMail, string userFirst, string userLast)
        {

            //Check to see if the server can initiate a connection to the database server - ADU
            Console.WriteLine("[+] Checking to see if the database is connected...");
            SqlConnection dbConnection = new SqlConnection();
            try
            {

                //String that contains connection info for database... Encryption option is not supported. Need to check connection string to see how to implement it
                dbConnection.ConnectionString = @"Server=ec2-52-4-79-59.compute-1.amazonaws.com, 1433; Database=chatserver; User Id= Administrator; Password=U%GT4nDTZk|dX-A\ZrS*%Imm,A";

                //Open up connection to database
                dbConnection.Open();

                string sqlUserCommand = "SELECT COUNT(*) FROM [User] WHERE username=@User";

                //Password Hashing Function
                int saltSize = 25;
                string userSalt = CreateSalt(saltSize); //randomly generate a salt
                string passwordHash = CreatePasswordHash(userPassword, userSalt); //send password to be hashed with a salt SHA256

                //Console.WriteLine("Password Hash in register: "+ passwordHash); - debugging purposes only - ADU

                //This part checks to see if there is more than one user account with that same name
                SqlCommand userCommand = new SqlCommand(sqlUserCommand, dbConnection); 

                userCommand.Parameters.Add("@User", SqlDbType.VarChar);
                userCommand.Parameters["@User"].Value = userName;

                int userCount = (int)userCommand.ExecuteScalar(); // if there is more than one it will increment the value here

                //Connect to see if username already exists
                sqlUserCommand = "SELECT COUNT(*) FROM [User] WHERE username=@User";

                //perform some basic checks to see if registration info is correct  
                if (userCount > 0)
                {
                    dbConnection.Close();
                    Console.WriteLine("[+] Username already in use");
                    return 3;
                }
                else if (!ValidEmailAddr(userMail))
                {
                    dbConnection.Close();
                    Console.WriteLine("[+] Bad Email");
                    return 2;
                }
                else if (userPassword.Length < 3)
                {
                    dbConnection.Close();
                    Console.WriteLine("[+] Bad password length");
                    return 4;
                }

                // Send user info to the chatserver database
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = System.Data.CommandType.Text;

                cmd.CommandText = "INSERT INTO [User] (username, userpassword, userSalt, userEmail, UserFirstName, UserLastName) VALUES ('" + userName + "','" + passwordHash + "','" + userSalt + "','" + userMail + "','" +
                userFirst + "','" + userLast + "')";

                cmd.Connection = dbConnection;
                cmd.ExecuteNonQuery();
                
                dbConnection.Close();
                return 1; //success
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
                return -2;
            }
            finally
            {
                dbConnection.Close();
            }
        }// End of ConnectToDB Function

        //***************************************************************************************
        // Function Name: ValidEmailAddr
        // Description: 
        // Does some simple checking to verify if email address is valid or not. 
        // 
        //***************************************************************************************
        public static bool ValidEmailAddr(string userEmail)
        {
            try
            {
                var emailAddr = new System.Net.Mail.MailAddress(userEmail);
                return emailAddr.Address == userEmail;
            }
            catch
            {
                return false;
            }
        }// End of IsValidEmail

        //***************************************************************************************
        // Function Name: CreateSalt
        // Description: 
        // Creates a randomly generated salt to be used with the user password hash
        // https://crackstation.net/hashing-security.htm#properhashing
        //https://msdn.microsoft.com/en-us/library/system.security.cryptography.rngcryptoserviceprovider.aspx
        //https://msdn.microsoft.com/en-us/library/ff649202.aspx
        //***************************************************************************************
        private static string CreateSalt(int size)
        {
            // Generate a cryptographic random number using the cryptographic
            // service provider
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] rngSalt = new byte[size];
            rng.GetBytes(rngSalt);

            // Return a Base64 string representation of the random number
            return Convert.ToBase64String(rngSalt);

        } //end of CreateSalt

        //***************************************************************************************
        // Function Name: CreatePasswordHash
        // Description: 
        // Hashes the password with a SHA256 hash
        // 
        // https://crackstation.net/hashing-security.htm#properhashing
        // https://msdn.microsoft.com/en-us/library/system.security.cryptography.rngcryptoserviceprovider.aspx
        // https://msdn.microsoft.com/en-us/library/ff649202.aspx
        //***************************************************************************************
        private static string CreatePasswordHash(string userPassword, string salt)
        {
            //Hash function 
            SHA256Managed crypt = new SHA256Managed();

            //string which will carry the hashed password
            string hashedPassword = String.Empty;

            //does some hashing stuff
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(userPassword), 0, Encoding.ASCII.GetByteCount(userPassword));

            //for each byte add it to the hashedPassword string
            foreach (byte theByte in crypto)
            {
                hashedPassword += theByte.ToString("x2");
            }

            //return the hashedPassword
            return hashedPassword;

        }//End of CreatePasswordHash

        //*******************************************************************************************
        // Function Name: Main                                                                     **
        // Description:  Main function of the server program, however it just dynamically creates  **
        //               a new instance of the server() function.                                  **
        //                                                                                         **
        //*******************************************************************************************
        public static void Main() { server cServer = new server(); }
    } // End server class
}// End class
