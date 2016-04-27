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
    //add encryption and decryption to this window
    //encrypt data being sent after submit button selected
    //decrypt data being received from the server
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        string[] chatWindowMessage;
        string[] usernameArray;
        byte[] buffer = new byte[300];
        Socket s;
        string message = "";
        string UserName;
        string messageRecvd;
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        

        public ChatWindow(Socket x, string name)
        {
            InitializeComponent();
            s = x;
            UserName = name;
            s.ReceiveTimeout = 100;
            dispatcherTimer.Tick += new EventHandler(SocketListen);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 3);
            dispatcherTimer.Start();
            Array.Clear(buffer, 0, buffer.Length);
        }
       
        public void SocketListen(object sender, EventArgs e)
        {
            //just need to add the decrypt function to this
            try
            {
                s.Receive(buffer);
                //decrypt buffer here
                if (buffer[0] != '\0')
                {
                    messageRecvd = Encoding.UTF8.GetString(buffer);
                    chatWindowMessage = messageRecvd.Split('\0');
                    if(chatWindowMessage[0].Contains("3000:"))
                    {
                        usernameArray = chatWindowMessage[0].Split(':');
                        if (usernameArray.First() == "3000")
                        {
                            chat_window_users_list.Clear();
                            for (int i = 1; i < usernameArray.Length; i++)
                            {
                                chat_window_users_list.AppendText(usernameArray[i] + "\n");
                            }
                        }
                    }
                    else
                        chat_window_text_box.AppendText(chatWindowMessage.First());
                    Array.Clear(buffer, 0, buffer.Length);
                }
            }
            catch(SocketException f)
            { }
                // return 0;
        }
        private void chat_window_submit_message_Click(object sender, RoutedEventArgs e)
        {
            //prepend messages with username used to log in.  Will have to pass that to the client from the login page along with the socket.
            //Add the encrypt function call here
            message = UserName + ": " + message + "\n";
            //encrypt message here
            s.Send(Encoding.UTF8.GetBytes(message));
            chat_window_message_input.Clear();
        }
        
        private void chat_window_message_input_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            message = textBox.Text;
        }
    }
}
