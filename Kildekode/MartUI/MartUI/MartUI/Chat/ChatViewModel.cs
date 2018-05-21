﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using MartUI.Events;
using MartUI.Friend;
using MartUI.Main;
using MartUI.Me;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Application = System.Windows.Application;

namespace MartUI.Chat
{
    public class ChatViewModel : BindableBase, IViewModel
    {
        private MyData _userData;
        private IEventAggregator _eventAggregator;
        private ICommand _sendMessageCommand;
        private string _textToSend;
        private FriendModel _user;
        

        public string ReferenceName => "ChatView";

        public MyData UserData => _userData ?? (_userData = MyData.GetInstance());

        public string TextToSend
        {
            get { return _textToSend; }
            set
            {
                _textToSend = value;
                RaisePropertyChanged();
            }
        }

        public FriendModel User
        {
            get
            {
                if(_user == null)
                    _user = new FriendModel();
                return _user;
            }
            set
            {
                _user = value;
                RaisePropertyChanged();
            }
        }

        public ChatViewModel()
        {
            User = new FriendModel();

            _eventAggregator = GetEventAggregator.Get();
            _eventAggregator.GetEvent<SelectedFriendEvent>().Subscribe(HandleFriend);
            //_eventAggregator.GetEvent<ReceiveMessageFromServerEvent>().Subscribe(ReceiveMessage);

            //Receive all previous messages
        }

        private void HandleFriend(FriendModel obj)
        {
            User = obj;
        }

        private void SendMessage()
        {
            //Send TextToSend + Username/Recipient
            var message = new ChatModel();
            message.Sender = UserData.Username;
            message.Message = TextToSend;
            message.MessagePosition = "Right";
            message.Receiver = User.Username;
            _eventAggregator.GetEvent<NewMessageEvent>().Publish(message);
            Application.Current.Dispatcher.Invoke(() => { _eventAggregator.GetEvent<SendMessageToServerEvent>().Publish(message); });
            TextToSend = "";
        }

        public ICommand SendMessageCommand => _sendMessageCommand ?? (_sendMessageCommand = new DelegateCommand(SendMessage));
    }
}
