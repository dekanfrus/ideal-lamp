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
    /// Interaction logic for Register.xaml
    /// </summary>
    public partial class Register : Window
    {
        bool registrationStatus = true;
        string registrationInformation;
        string userName;
        string email;
        string firstName;
        string lastName;
        string password;
        string passwordVerify;
        int port = 4444;
        public Register()
        {
            InitializeComponent();
            register_password_mismatch.Visibility = Visibility.Collapsed;
            register_submit_error.Visibility = Visibility.Collapsed;
            registration_email_error.Visibility = Visibility.Collapsed;
            registration_username_error.Visibility = Visibility.Collapsed;
        }

        private void register_submit_Click(object sender, RoutedEventArgs e)
        {
            //check for values in all fields
            register_submit_error.Visibility = Visibility.Collapsed;
            register_password_mismatch.Visibility = Visibility.Collapsed;
            if (userName == null || email == null || firstName == null || lastName == null || password == null || passwordVerify == null)
            {
                register_submit_error.Visibility = Visibility.Visible;
                registrationStatus = false;
            }
            if (password != passwordVerify)
            {
                register_password_mismatch.Visibility = Visibility.Visible;
                registrationStatus = false;
            }
            
            //send data to server
            if(registrationStatus)
            {
                registrationInformation = "2000:" + userName + ":" + password + ":" + email + ":" + firstName + ":" + lastName;
                IPHostEntry host = Dns.Resolve(Dns.GetHostName());
                IPAddress ipAddress = host.AddressList[0];
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Connect(ipAddress, port);
                s.Send(Encoding.UTF8.GetBytes(registrationInformation));
            }
           
            //wait for response


            //if registration good proceed to login page else show error
            if(registrationStatus)
            {
                ChatWindow chatwindow = new ChatWindow();
                chatwindow.Show();
                this.Close();
            }
            //else
            //{
            //    if (!invalidEmail)
            //        registration_email_error.Visibility = Visibility.Visible;
            //    if (!invalidUserName)
            //        registration_username_error.Visibility = Visibility.Visible;
            //}
        }

        private void register_cancel_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainwindow = new MainWindow();
            mainwindow.Show();
            this.Close();
        }

        private void user_name_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            userName = textBox.Text;
        }

        private void first_name_text_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            firstName = textBox.Text;
        }

        private void last_name_text_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            lastName = textBox.Text;
        }

        private void register_passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var pwdBox = sender as PasswordBox;
            password = pwdBox.Password;
        }

        private void register_passwordBox_verify_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var pwdBox = sender as PasswordBox;
            passwordVerify = pwdBox.Password;
        }

        private void email_text_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            email = textBox.Text;
        }
    }
}
