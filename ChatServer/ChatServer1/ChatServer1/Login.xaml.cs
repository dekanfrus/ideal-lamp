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

namespace ChatServer1
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        bool loginGood = true;
        string loginData;
        string userName;
        string password;
        int port = 4444;
        int loginStatus;
        int loginSuccess = 1300;
        //int loginFail = 1700;
        Socket s;
        //string serverResponse;

        public Login()
        {
            InitializeComponent();
            //hide the error message label on Log In page for invalid password or user name
            login_error_message.Visibility = Visibility.Collapsed;
            IPHostEntry host = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = host.AddressList[0];
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(ipAddress, port);
        }

        private void login_send_data()
        {
            //string host = Dns.GetHostName();
            string loginInformation = "1000:" + userName + ":" + password;
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
            //hide the error message label on Log In page for invalid password or user name
            login_error_message.Visibility = Visibility.Collapsed;

            //send data to server
            login_send_data();

            //get response
            byte[] buffer = new byte[300];
            s.Receive(buffer);
            loginStatus = Int32.Parse(Encoding.UTF8.GetString(buffer));

            //if status returned from server for log in is good then proceed to chat window
            //else display error message
            if (loginStatus == loginSuccess)
            {
                //create ChatWindow and close this window
                ChatWindow chatwindow = new ChatWindow(s);
                chatwindow.Show();
                this.Close();
            }
            else
                login_error_message.Visibility = Visibility.Visible;

        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var pwdBox = sender as PasswordBox;
            password = pwdBox.Password;
        }

        private void login_username_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            userName = textBox.Text;
        }
    }
}
