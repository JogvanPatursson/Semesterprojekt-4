﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using MartUI.Chat;
using MartUI.Events;
using MartUI.FriendNotification;
using MartUI.Group;
using MartUI.Main;
using MartUI.Me;
using MartUI.Tag;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace MartUI.Friend
{
    public class FriendViewModel : BindableBase, IViewModel
    {
        private readonly IEventAggregator _eventAggregator = GetEventAggregator.Get();
        public string ReferenceName => "FriendViewModel"; // Returns "FriendViewModel"
        private ObservableCollection<FriendModel> _friendList;
        private FriendModel _selectedFriend;
        private ICommand _chooseFriendCommand;
        private ICommand _addFriendCommand;
        private ICommand _removeFriendCommand;
        private ICommand _showNotificationsCommand;
        private ICommand _viewProfileCommand;
        private ICommand _showTagCommand;
        private string _username;
        private MyData _userData;
        public MyData UserData => _userData ?? (_userData = MyData.GetInstance());

        private bool _notificionReceived;
        public string Username
        {
            get { return _username; }
            set
            {
                _username = value;
                RaisePropertyChanged();
            }
        }

        public bool NotificationReceived
        {
            get => _notificionReceived;
            set => SetProperty(ref _notificionReceived, value);
        }

        //public Brush Background => NotificationReceived ? Brushes.White : Brushes.Red;
        public ICommand ChooseFriendCommand => _chooseFriendCommand ?? (_chooseFriendCommand = new DelegateCommand<FriendModel>(SelectFriend));
        public ICommand ViewProfileCommand => _viewProfileCommand ?? (_viewProfileCommand = new DelegateCommand<FriendModel>(HandleViewProfile));
        public ICommand ShowTagCommand => _showTagCommand ?? (_showTagCommand = new DelegateCommand(HandleShowTag));

        private void HandleShowTag()
        {
            _eventAggregator.GetEvent<ChangeFocusPage>().Publish(new TagViewModel());
        }

        public FriendViewModel()
        {
            Username = "Enter Username!";

            //NotificationReceived = true;
            _eventAggregator.GetEvent<ReceiveMessageFromServerEvent>().Subscribe(HandleNewMessage);
            _eventAggregator.GetEvent<NewMessageEvent>().Subscribe(HandleNewMessage);
            _eventAggregator.GetEvent<NotificationReceivedChangeColor>().Subscribe(() => NotificationReceived = true);
            _eventAggregator.GetEvent<AcceptedFriendRequestEvent>().Subscribe(AcceptedFriendRequest);
            _eventAggregator.GetEvent<RemoveFriendReceivedEvent>().Subscribe(HandleRemoveFriendReceived);
            _eventAggregator.GetEvent<AddFriendFromTagEvent>().Subscribe(HandleAddFriendFromTag);
            _eventAggregator.GetEvent<GetFriendListEvent>().Subscribe(HandleGetFriendList);

        }

        private void HandleGetFriendList(string friendlist)
        {
            FriendList.Clear();
            string[] temp = friendlist.Split(Constants.DataDelimiter);
            foreach (var f in temp)
            {
                if(f != "")
                FriendList.Add(new FriendModel(){Username = f});
            }

            var message = Constants.GetOldMessages + Constants.GroupDelimiter;
            _eventAggregator.GetEvent<SendMessageToServerEvent>().Publish(message);
        }

        private void HandleNewMessage(ChatModel message)
        {
            foreach (var friend in FriendList)
            {
                if (message.Sender == UserData.Username && friend.Username == message.Receiver)
                {
                    message.MessagePosition = "Right";
                    friend.MessageList.Add(message);
                    break;
                }
                else if (message.Sender == friend.Username && UserData.Username == message.Receiver)
                {
                    message.MessagePosition = "Left";
                    friend.MessageList.Add(message);
                    break;
                }
            }
        }

        // Mangler at tilføje en filtrering eller en anden liste som kun indeholder online venner

        // All friends in friend list
        public ObservableCollection<FriendModel> FriendList
        {
            get
            {
                if (_friendList == null)
                    _friendList = new ObservableCollection<FriendModel>();
                return _friendList;
            }
            set
            {
                _friendList = value;
                RaisePropertyChanged();
            }
        }

        public FriendModel SelectedFriend
        {
            get => _selectedFriend;
            set => SetProperty(ref _selectedFriend, value);
        }

        private void SelectFriend(FriendModel friend)
        {
            _eventAggregator.GetEvent<SelectedFriendEvent>().Publish(friend);
            _eventAggregator.GetEvent<ChangeFocusPage>().Publish(friend.Chat);
        }

        public void AddFriend()
        {
            var friendInList = false;
            foreach (var f in FriendList)
            {
                if (f.Username == Username && UserData.Username != Username)
                {
                    MessageBox.Show("This user is already on your friendlist");
                    friendInList = true;
                    break;
                }

                if (Username == "")
                    friendInList = true;
            }

            if (!friendInList)
            {
                var message = Constants.SendFriendRequest + Constants.GroupDelimiter + Username;
                _eventAggregator.GetEvent<SendMessageToServerEvent>().Publish(message);
            }

            Username = ""; //Clears the AddFriendTextbox after pressing enter
        }

        public void HandleAddFriendFromTag(string username)
        {
            var friendInList = false;
            foreach (var f in FriendList)
            {
                if (f.Username == username)
                {
                    MessageBox.Show("This user is already on your friendlist");
                    friendInList = true;
                    break;
                }

                if (username == "")
                    friendInList = true;
            }

            if (!friendInList)
            {
                var message = Constants.SendFriendRequest + Constants.GroupDelimiter + username;
                _eventAggregator.GetEvent<SendMessageToServerEvent>().Publish(message);
            }

            Username = ""; //Clears the AddFriendTextbox after pressing enter
        }

        private void HandleViewProfile(FriendModel friend)
        {
            _eventAggregator.GetEvent<ShowOtherUserProfile>().Publish(friend.Username);
        }

        public void AcceptedFriendRequest(string username)
        {
            var friendInList = false;
            foreach (var f in FriendList)
            {
                if (f.Username == username)
                {
                    MessageBox.Show("This user is already on your friendlist");
                    friendInList = true;
                    break;
                }

                if (username == "")
                    friendInList = true;
            }
            if (!friendInList)
            {
                Application.Current.Dispatcher.Invoke(() => { FriendList.Add(new FriendModel { Username = username }); });
            }
        }

        public void RemoveFriend(FriendModel friend)
        {
            if (FriendList.Contains(friend))
            {
                FriendList.Remove(friend);
                var message = Constants.RemoveFriend + Constants.GroupDelimiter + friend.Username;
                _eventAggregator.GetEvent<SendMessageToServerEvent>().Publish(message);
            }
            else
                MessageBox.Show("This user is not on your friendlist!");

            //Skal kommunikere med database/server
        }

        public void HandleRemoveFriendReceived(string username)
        {
            var isInList = false;
            var friend = new FriendModel();
            foreach (var f in FriendList)
            {
                if (f.Username == username)
                {
                    isInList = true;
                    friend = f;
                }
            }

            if (isInList)
            {
                FriendList.Remove(friend);
                var message = username + " has removed you from their friendlist!";
                _eventAggregator.GetEvent<NotificationReceivedEvent>().Publish(message);
            }
        }

        public ICommand ShowNotificationsCommand => _showNotificationsCommand ??
                                                    (_showNotificationsCommand = new DelegateCommand(ChangeToNotifications));

        public void ChangeToNotifications()
        {
            _eventAggregator.GetEvent<ChangeFocusPage>().Publish(new FriendNotificationViewModel());
            NotificationReceived = false;
        }
        public ICommand AddFriendCommand => _addFriendCommand ?? (_addFriendCommand = new DelegateCommand(AddFriend));
        public ICommand RemoveFriendCommand => _removeFriendCommand ?? (_removeFriendCommand = new DelegateCommand<FriendModel>(RemoveFriend));
    }

    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }

    public class Converter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var text1 = values[0].ToString();
            var text2 = values[1].ToString();

            MessageBox.Show(text1 + " " + text2);
            //if (text1 == text2)
            //    return Visibility.Hidden;
            //else
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
