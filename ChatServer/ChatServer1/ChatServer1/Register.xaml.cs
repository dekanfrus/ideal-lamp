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
        int registrationValue;
        int regSuccess = 2300;
        //int regFail = 2700;
        int register = 2000;
        Socket s;
        public Register()
        {
            InitializeComponent();
            register_submit.IsEnabled = false;
            register_password_mismatch.Visibility = Visibility.Collapsed;
            register_submit_error.Visibility = Visibility.Collapsed;
            registration_username_error.Visibility = Visibility.Collapsed;
            IPHostEntry host = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = host.AddressList[0];
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(ipAddress, port);
        }

        private void submit_button_enable_disable()
        {
            if (userName != null && email != null && firstName != null && lastName != null && password != null && passwordVerify != null && password == passwordVerify)
                register_submit.IsEnabled = true;
            else
                register_submit.IsEnabled = false;
        }

        private void password_mismatch()
        {
            if (password != passwordVerify)
            {
                register_password_mismatch.Visibility = Visibility.Visible;
                registrationStatus = false;
                // register_submit.IsEnabled = false;
            }
            else
            {
                register_password_mismatch.Visibility = Visibility.Collapsed;
                registrationStatus = true;
            }
        }

        private void registration_form_fill_all()
        {
            if (userName == null || email == null || firstName == null || lastName == null || password == null || passwordVerify == null)
            {
                register_submit_error.Visibility = Visibility.Visible;
                registrationStatus = false;
            }
            else
            {
                registrationStatus = true;
                register_submit_error.Visibility = Visibility.Collapsed;
            }
        }

        private void register_submit_Click(object sender, RoutedEventArgs e)
        {
            //check for values in all fields
            register_submit_error.Visibility = Visibility.Collapsed;
            register_password_mismatch.Visibility = Visibility.Collapsed;
            
            //send data to server
            if (registrationStatus)
            {
                //create data stream to send registration information
                registrationInformation = register + ":" + userName + ":" + password + ":" + email + ":" + firstName + ":" + lastName;
                //send registration data through socket s
                s.Send(Encoding.UTF8.GetBytes(registrationInformation));
                
                //wait for response
                byte[] buffer = new byte[300];
                s.Receive(buffer);
                //Console.WriteLine(Encoding.UTF8.GetString(buffer));
                registrationValue = Int32.Parse(Encoding.UTF8.GetString(buffer));
                //Console.WriteLine(registrationValue);
            }

            //if registration good proceed to login page else show error
            if (registrationValue == regSuccess)
            {
                s.Shutdown(SocketShutdown.Both);
                s.Close();
                Login login = new Login();
                login.Show();
                this.Close();
            }
            else
                registration_username_error.Visibility = Visibility.Visible;
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
            registration_username_error.Visibility = Visibility.Collapsed;
            registration_form_fill_all();
            submit_button_enable_disable();
        }

        private void first_name_text_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            firstName = textBox.Text;
            registration_form_fill_all();
            submit_button_enable_disable();
        }

        private void last_name_text_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            lastName = textBox.Text;
            registration_form_fill_all();
            submit_button_enable_disable();
        }

        private void register_passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var pwdBox = sender as PasswordBox;
            password = pwdBox.Password;
            password_mismatch();
            submit_button_enable_disable();
        }

        private void register_passwordBox_verify_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var pwdBox = sender as PasswordBox;
            passwordVerify = pwdBox.Password;
            password_mismatch();
            submit_button_enable_disable();
        }

        private void email_text_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            email = textBox.Text;
            registration_form_fill_all();
            submit_button_enable_disable();
        }
    }
}
