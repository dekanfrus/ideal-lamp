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
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        string[] chatWindowMessage;
        byte[] buffer = new byte[300];
        Socket s;
        string messageRecvd = "this is a test";
       
        public ChatWindow(Socket x)
        {
            InitializeComponent();
            s = x;
        }
        //make a custom event based on s.Poll(-1, SelectMode.SelectRead)
        //try following link solution in a ContentRendered event.
        //http://stackoverflow.com/questions/11559999/how-do-i-create-a-timer-in-wpf 
        public async Task SocketListen()
        {
            s.Receive(buffer);
            if (buffer[0] != 0)
            {
                messageRecvd = Encoding.UTF8.GetString(buffer);
                chatWindowMessage = messageRecvd.Split('\0');
                chat_window_text_box.AppendText(chatWindowMessage.First());
                Array.Clear(buffer, 0, buffer.Length);
            }
                // return 0;
        }
        private void chat_window_submit_message_Click(object sender, RoutedEventArgs e)
        {
            s.Send(Encoding.UTF8.GetBytes(messageRecvd));
            chat_window_recv_message();
        }

        private void chat_window_recv_message()
        {
            s.Receive(buffer);
            messageRecvd = Encoding.UTF8.GetString(buffer);
            chatWindowMessage = messageRecvd.Split('\0');
            chat_window_text_box.AppendText(chatWindowMessage.First());
            Array.Clear(buffer, 0, buffer.Length);
        }
        
        //private async void Window_ContentRendered(object sender, EventArgs e)
        //{
        //    if (s.Poll(-1, SelectMode.SelectRead))
        //        await SocketListen();
        //}

    }
}
