using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.IO;

namespace ChatServer1
{
    //add encryption and decryption
    //encrypt data after login clicked before sending through the socket
    //decrypt response from server 
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        //bool loginGood = true;
        //string loginData;
        string userName;
        string password;
        int port = 4444;
        int loginStatus;
        int loginSuccess = 1300;
        //int loginFail = 1700;
        Socket s;
        //string serverResponse;
        //private static byte[] Salt = {2, 123,  61, 217, 205, 133, 176, 171, 164, 248, 215, 129, 232, 210, 145, 56,
        //                              45, 133,  55, 137,  95, 174, 245, 179, 205, 140, 190, 215, 110, 122, 169, 95 };
        //private static byte[] IV = { 9, 90, 56, 18, 127, 245, 101, 112, 72, 133, 248, 224, 73, 12, 96, 24, };
        //
        //private static ICryptoTransform Encryptor, Decryptor;
        //private static System.Text.UTF8Encoding Encoder;

        public Login()
        {
            InitializeComponent();
            login_submit.IsEnabled = false;
            //hide the error message label on Log In page for invalid password or user name
            login_error_message.Visibility = Visibility.Collapsed;
            //IPHostEntry host = Dns.Resolve(Dns.GetHostName());
            //IPAddress ipAddress = host.AddressList[0];
            //s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //s.Connect(ipAddress, port);
        }

        /*public static string EncryptData(string message)
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
        }*/

        public static string EncryptData(string message)
        {
            string eMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(message.Split('\0').First()));
            return eMessage;
        }

        public static string DecryptData(string message)
        {
            string deMessage = message.Split('\0').First();
            return deMessage;
        }
        private void login_send_data()
        {
            //string host = Dns.GetHostName();
            string loginInformation = "1000:" + userName + ":" + password;// EncryptData("1000:") + userName + EncryptData(":") + password;
            loginInformation = EncryptData(loginInformation);
            //encrypt loginInformation here
            s.Send(Encoding.UTF8.GetBytes(loginInformation));
        }

        private void login_cancel_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainwindow = new MainWindow();
            mainwindow.Show();
            this.Close();
        }

        private void login_submit_Click(object sender, RoutedEventArgs e)
        {
            IPHostEntry host = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = host.AddressList[0];
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(ipAddress, port);

            //hide the error message label on Log In page for invalid password or user name
            login_error_message.Visibility = Visibility.Collapsed;

            //send data to server
            login_send_data();

            //get response
            byte[] buffer = new byte[300];
            s.Receive(buffer);
            string message = Encoding.UTF8.GetString(buffer);
            //decrypt the buffer here
            message = DecryptData(message);
            loginStatus = Int32.Parse(message);

            //if status returned from server for log in is good then proceed to chat window
            //else display error message
            if (loginStatus == loginSuccess)
            {
                //create ChatWindow and close this window
                ChatWindow chatwindow = new ChatWindow(s, userName);
                chatwindow.Show();
                this.Close();
            }
            else
                login_error_message.Visibility = Visibility.Visible;

        }

        private void login_passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var pwdBox = sender as PasswordBox;
            password = pwdBox.Password;
            if (password == null || userName == null || pwdBox.Password == "")
                login_submit.IsEnabled = false;
            else
                login_submit.IsEnabled = true;
        }

        private void login_username_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            userName = textBox.Text;
            if (password == null || userName == null || textBox.Text == "")
                login_submit.IsEnabled = false;
            else
                login_submit.IsEnabled = true;
        }
    }
}
