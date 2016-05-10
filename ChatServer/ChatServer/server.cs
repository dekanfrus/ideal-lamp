//************************************************************************************************************************************
// Alex Urest, Jeff Hayslet, John Alves                                                                                             **
// Computer Networks COSC 4342 - Dr. Kar                                                                                            **
// Semester Project - Secure Chat Application                                                                                       **
// Spring 2016                                                                                                                      **
//                                                                                                                                  **
// Brief Description:                                                                                                               **
// This program is the server application for a chat program.  It utilizes asynchronous sockets in order to provide service to      **
// multiple clients.  It also implements login and register functionality.  The user data is stored on an SQL server hosted by      **
// Amazon Web Services.  The user passwords are hashed with a SHA256 algorithim before they are sent to the database, and a salt    **
// is also generated and stored for each user.  Once login is successful, the client data that is sent back and forth is            **
// encrypted in transit.  The server decrypts the data in order to determine how to handle the messages, logs it, then encrypts it  **
// again so it can be sent to all client connections unless it is a login, register, or exit message.  The server also maintains    **
// a list of authenticated clients in the chat room and will update that list as users join and leave.                              **
//                                                                                                                                  **
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
    // Class Name: ClientData                                                              **
    // Description:  This is a state object that is created each time a new client is      **
    //               connected.  It stores various data relevant to each specific client.  **
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

    //************************************************************************************************
    // Class Name: server                                                                           **
    // Description:  This is the main class for the program.  It contains all of the functionality  **
    //               necessary for the program to run, with the only exception being the ClientData **
    //               state object.                                                                  **
    //************************************************************************************************
    class server
    {
        // Thread signaler of a particular state (System.Threading)
        public static ManualResetEvent completed = new ManualResetEvent(false);

        // Create a list of clients
        public static List<Socket> clientList = new List<Socket>();

        // Create a list to store the names of currently logged in users
        public static List<string> clientName = new List<string>();

        //*********************************************************************************************
        // Function Name: server                                                                     **
        // Description: This is the primary function for the server class.  It creates a listening   **
        //              socket, accepts connections and data, and sends data to the connected        **
        //              clients.                                                                     **
        //*********************************************************************************************
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
                    logWriter.Close();
                }

                Console.WriteLine("[+] Awaiting Connection...");

                // Infinite loop to make the server continually run and wait for connections
                while (true)
                {
                    try
                    {
                        completed.Reset();

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
        // Function Name: Accept Connection
        // Description: When a new connection is initiated with the server this function will 
        // create the necessary socket connections to make sure the client will be able to talk to 
        // other clients and the server.
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
                    logWriter.Close();
                }
                Console.WriteLine("[+] Connection Received");

                // Begin receiving data from the client
                clientHandler.BeginReceive(clientState.buffer, 0, ClientData.BufferSize, 0, new AsyncCallback(ReceiveData), clientState);
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                {
                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                    logWriter.WriteLine(error.ToString());
                    logWriter.Close();
                }
            }

        }// End of Accept function

        //*****************************************************************************************
        // Function Name: ConnectToDB                                                            **
        // Description:                                                                          **
        // Attempts to establish a connection with the RDS MSSQL database   
        // Used with the login function. Checks to see user exists and that password matches     **
        //*****************************************************************************************
        public static bool ConnectToDB(string userName, string userPassword)
        {

            //Check to see if the server can initiate a connection to the database server - ADU
            Console.WriteLine("[+] Checking to see if the database is connected...");

            SqlConnection dbConnection = new SqlConnection();

            try
            {

                //String that contains connection info for database
                dbConnection.ConnectionString = @"Server=ec2-52-4-79-59.compute-1.amazonaws.com, 1433; Database=chatserver; User Id= Administrator; Password=U%GT4nDTZk|dX-A\ZrS*%Imm,A";

                //Open up connection to database
                dbConnection.Open();

                //just for debugging purposes - ADU
                Console.WriteLine("[+] DB connected!");


                //Checks to see if we can pull down the salt from the user Salt
                SqlCommand conn = new SqlCommand("SELECT userSalt FROM [User] WHERE username = @User", dbConnection);

                conn.Parameters.Add("@User", SqlDbType.VarChar);
                conn.Parameters["@User"].Value = userName;
                string userSalt = (string)conn.ExecuteScalar();

                //Create a hash and send see if it matches the database
                string hashRetrievedUserPass = CreatePasswordHash(userPassword, userSalt);

                //The sql input to check if records exist
                string sqlUserCommand = "SELECT COUNT(*) FROM [User] WHERE username=@User AND userpassword=@Password";
                SqlCommand command = new SqlCommand(sqlUserCommand, dbConnection);

                //Paramterizing SQL input - ADU
                command.Parameters.Add("@User", SqlDbType.VarChar);
                command.Parameters["@User"].Value = userName;

                command.Parameters.Add("@Password", SqlDbType.VarChar);
                command.Parameters["@Password"].Value = hashRetrievedUserPass;

                int userCount = (int)command.ExecuteScalar();

                if (userCount > 0) //Checks to see if a match was found for the requested login attempt
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
                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                {
                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                    logWriter.WriteLine(error.ToString());
                    logWriter.Close();
                }
                return false;
            }
            finally
            {
                dbConnection.Close();
            }
        }// End of ConnectToDB Function

        //*****************************************************************************************
        // Function Name: ReceiveData                                                            **
        // Description: This function is responsible for receiving all data from the client.     **
        //              Each client socket will spin up a new thread for that connection which   **
        //              allows the server to accept multiple connections at a time.              **
        //*****************************************************************************************
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

                        // Depending on authentication results, log and return a code back to the client
                        switch (success)
                        {
                            case 1:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                {
                                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                                    logWriter.WriteLine("[+] Login Successful!");
                                    logWriter.Close();
                                }
                                Console.WriteLine("[+] Login Successful!");

                                // Sends a message to the client that the login was successful
                                clientReturnInt = Encoding.ASCII.GetBytes("1300");
                                clientHandler.Send(clientReturnInt);

                                // Send updated users list to all clients
                                BroadcastUsers();
                                break;
                            case 2:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                {
                                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                                    logWriter.WriteLine("[+] Login Failed!  Username/Password combination.");
                                    logWriter.Close();
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

                        // Depending on the results from Register, log and return a code back to the client
                        switch (success)
                        {
                            case 1:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                {
                                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                                    logWriter.WriteLine("[+] Registration Successful!");
                                    logWriter.Close();
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
                                    logWriter.Close();
                                }
                                Console.WriteLine("[+] Registration Failed!  Invalid Email Address.");
                                break;
                            case 3:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                {
                                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                                    logWriter.WriteLine("[+] Registration Failed! Username already exists.");
                                    logWriter.Close();
                                }
                                Console.WriteLine("[+] Registration Failed! Username already exists.");
                                break;
                            case 4:
                                using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                                {
                                    logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                                    logWriter.WriteLine("[+] Registration Failed! Invalid Password.");
                                    logWriter.Close();
                                }
                                Console.WriteLine("[+] Registration Failed! Invalid Password.");
                                break;
                            default:
                                break;
                        }
                    } // End of Register handler

                    // If the data received is preceded with the 3015 code, that means a user has left the chat room.
                    // therefore we split that string, grab the username and remove that user from the connected client list
                    else if (clientMessage.StartsWith("3015"))
                    {
                        string[] message = clientMessage.Split(':');

                        string removeUser = message[1];

                        clientName.Remove(removeUser);

                        // Rebroadcast the user list to all connected clients
                        BroadcastUsers();
                    } // End of user removal process
                    else
                    {
                        // Log the message then call the BroadcastMessage function
                        using (StreamWriter logWriter = File.AppendText("ServerLog.txt"))
                        {
                            logWriter.Write("{0} {1}:  ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                            logWriter.Write(clientMessage);
                            logWriter.Close();
                        }
                        BroadcastMessage(clientMessage);
                    }// End of Broadcast Message Hanler
                }// End of receive data if statement
            }// End of try block

            catch (Exception) { }

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
                        logWriter.Close();
                    }
                    Console.WriteLine(error.ToString());
                }
            }
        }// End of ReceiveData Function

        //*******************************************************************************************
        // Function Name: BroadcastMessage                                                         **
        // Description:  Messages received will use this function to broadcast any incoming messages
        //  to each of the connected users in the clientList. Plaintext and Encrypted text is shown             
        //  to highlight how the encrption works on the server side.                               **
        //*******************************************************************************************
        public static void BroadcastMessage(string recvdMessage)
        {
            // Output the unencrypted message to the console
            Console.Write("Plaintext Message: ");
            Console.Write(recvdMessage);

            // Output the encrypted message to the console (used simply for presentation purposes)
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
        // Description:  This function is responsible for sending an updated user list to all      **
        //               connected clients.  It first adds a user list code to the front of the    **
        //               string, then converts the List contents to a string with the : delimiter  **
        //*******************************************************************************************
        public static void BroadcastUsers()
        {
            string userList = string.Join(":", clientName.ToArray());
            string newUserList = "3000:" + userList;

            byte[] EncryptedUserList = Encoding.ASCII.GetBytes(EncryptData(newUserList));

            foreach (Socket current in clientList)
            {
                current.BeginSend(EncryptedUserList, 0, EncryptedUserList.Length, 0, new AsyncCallback(OnBroadcast), current);
            }
        }// End of BroadcastUsers function

        //*******************************************************************************************
        // Function Name: OnBroadcast                                                              **
        // Description:  This function simply ends the asynchronous send operation.                **
        //*******************************************************************************************
        public static void OnBroadcast(IAsyncResult AsyncResult)
        {
            Socket clientSocket = (Socket)AsyncResult.AsyncState;
            clientSocket.EndSend(AsyncResult);
        }

        //*******************************************************************************************
        // Function Name: Login                                                                    **
        // Description:  Once a user attempts to login this function will use the ConnectToDB function
        //  to see if record of user exists in the database. Splits the login credential info into two               
        //  pieces.                                                                                **
        //*******************************************************************************************
        public static int Login(string loginInfo)
        {
            // Split the string based on ":" and store in a string array
            // 
            string[] creds = loginInfo.Split(':');

            string userName = creds[1];
            string userPassword = creds[2];

            //Need to parameterize the sqlCommand with @symbol to read only as string
            //should prevent SQLi

            if (ConnectToDB(userName, userPassword))
            {
                //Console.WriteLine("Username and password verified"); - For debugging if you need to see if it's being authenticated on server side
                clientName.Add(userName);
                return 1;
            }
            else
            {
                //Console.WriteLine("Username and password combo is bad"); - For debugging if you need to see if it's being authenticated on server side
                return 2;
            }
        }// End of Login function

        //*******************************************************************************************
        // Function Name: Register                                                                 **
        // Description:  If a user wishes to register their username with the chatserver they are sent
        //  to this function to have credentials split up.              
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

            int result; // sent back to calling function to determine if registration was successful or not

            result = ConnectToDB(userName, userPassword, userEmail, userFirstName, userLastName); //overloaded function

            return result;
        }

        //*******************************************************************************************
        // Function Name: EncryptData                                                              **
        // Description:  Encrypts the outgoing messages to the clients
        //               
        //                                                                                         **
        //*******************************************************************************************
        public static string EncryptData(string message)
        {
            string eMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(message.Split('\0').First()));
            return eMessage;
        }

        //*******************************************************************************************
        // Function Name: DecryptData                                                              **
        // Description: Decrypts the incoming messages from the clients  
        //               
        //                                                                                         **
        //*******************************************************************************************
        public static string DecryptData(string message)
        {
            string dMessage = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(message.Split('\0').First()));
            return dMessage;
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
                    //Console.WriteLine("[+] Username already in use"); - debugging on server side only
                    return 3;
                }
                else if (!ValidEmailAddr(userMail))
                {
                    dbConnection.Close();
                    //Console.WriteLine("[+] Bad Email"); - debugging on server side only
                    return 2;
                }
                else if (userPassword.Length < 3)
                {
                    dbConnection.Close();
                    //Console.WriteLine("[+] Bad password length"); - debugging on server side only
                    return 4;
                }

                // Send user info to the chatserver database
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = System.Data.CommandType.Text;

                cmd.CommandText = "INSERT INTO [User] (username, userpassword, userSalt, userEmail, UserFirstName, UserLastName) VALUES ('" + userName + "','" + passwordHash + "','" + userSalt + "','" + userMail + "','" +
                userFirst + "','" + userLast + "')";

                //uses the above sql input to update info in database
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
                //Attempts to enter the email address in the .Net standard. If it is accepted the code will return as true
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

            //Creates a hash in byte form using the SHA256 hashing algorithm
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(userPassword), 0, Encoding.ASCII.GetByteCount(userPassword));

            //for each byte in cryptio add it to the hashedPassword string
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
}// End of program
