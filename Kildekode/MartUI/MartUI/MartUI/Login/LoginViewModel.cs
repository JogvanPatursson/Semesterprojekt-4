﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MartUI.Chat;
using MartUI.CreateUser;
using MartUI.Events;
using MartUI.Friend;
using MartUI.Helpers;
using MartUI.Main;
using MartUI.Me;
using MartUI.Settings.BlankSetting;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace MartUI.Login
{
    public class LoginViewModel : BindableBase, IViewModel // Using BindableBase from PRISM instead of INotifyPropertyChanged
    {
        private DatabaseDummy _databaseDummy;

        private readonly IEventAggregator _eventAggregator;

        private ICommand _createUserCommand;
        private ICommand _loginCommand;

        private string _password;
        private MyData _userData;

        public string ReferenceName => "Login";
        public MyData UserData => _userData ?? (_userData = MyData.GetInstance());
        public DatabaseDummy DatabaseDummy => _databaseDummy ?? (_databaseDummy = DatabaseDummy.GetInstance());

        // Made these in here since it will be created either way because observing these - no need for model
        public string Username
        {
            get => UserData.Username;
            set
            {
                if (UserData.Username == value) return;
                UserData.Username = value;
                RaisePropertyChanged();
            } // if username != value, notify
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
            // if username != value, notify
        }

        public bool Send { get; set; }

        // Navigate to CreateUserView
        public ICommand CreateUserCommand => _createUserCommand ?? (_createUserCommand = new DelegateCommand(() =>
                                                     _eventAggregator.GetEvent<ChangeFullPage>().Publish(new CreateUserViewModel())));
        //Observes Username and Password to check LoginCanExecute, call LoginExecute
        public ICommand LoginCommand => _loginCommand ?? (_loginCommand
                                            = new DelegateCommand(LoginExecute, LoginCanExecute)
                                                .ObservesProperty(() => Username)
                                                .ObservesProperty(() => Password));
        public LoginViewModel()
        {
            //MessageBox.Show("subs");
            Send = false;
            _eventAggregator = GetEventAggregator.Get();
            _eventAggregator.GetEvent<PasswordChangedInLogin>().Subscribe(paraPass => Password = paraPass);
            //_eventAggregator.GetEvent<LogoutPublishLoginEvent>().Subscribe(HandleLogoutPublishLogin);
            _eventAggregator.GetEvent<LoginResponseEvent>().Subscribe(HandleLogin);
            _eventAggregator.GetEvent<GetProfile>().Subscribe(ProfileInfo);
            _eventAggregator.GetEvent<GetFriendList>().Subscribe(FriendListInfo);
        }

        private void SubscribeToEvents()
        {
            _eventAggregator.GetEvent<GetProfile>().Subscribe(ProfileInfo);
            _eventAggregator.GetEvent<GetFriendList>().Subscribe(FriendListInfo);
        }


        private void HandleLogin(string response)
        {
            switch (response)
            {
                case "OK":
                    // Fullpage is null, show friendlist and initialize chatview
                    _eventAggregator.GetEvent<SendMessageToServerEvent>().Publish(Constants.RequestProfile 
                                                                                  + Constants.GroupDelimiter
                                                                                  + Username);
                    break;
                case "NOK":
                    MessageBox.Show("Cannot login! Wrong username or password!");
                    break;
            }
        }

        private void FriendListInfo(string s)
        {
            //if (Send == false)
            //    return;
            //MessageBox.Show("her");

            //Send = true;
            _eventAggregator.GetEvent<GetFriendListEvent>().Publish(s);

            _eventAggregator.GetEvent<GetProfile>().Unsubscribe(ProfileInfo);
            _eventAggregator.GetEvent<GetFriendList>().Unsubscribe(FriendListInfo);

            // Change view to "Main View"
            _eventAggregator.GetEvent<ChangeFullPage>().Publish(null);
            _eventAggregator.GetEvent<ChangeFriendPage>().Publish(new FriendViewModel());
            _eventAggregator.GetEvent<ChangeFocusPage>().Publish(new BlankSettingViewModel());

            // Unsubscribe events to be able to handle new login request
        }

        //private void HandleLogoutPublishLogin()
        //{
        //    _eventAggregator.GetEvent<GetProfile>().Subscribe(ProfileInfo);
        //    _eventAggregator.GetEvent<GetFriendList>().Subscribe(FriendListInfo);
        //}

        private void ProfileInfo(string profile)
        {
            // Get full profile info (description + tags)
            var fullProfile = profile.Split(Constants.GroupDelimiter).ToList();

            UserData.Description = fullProfile[1];

            // Split possible tags into list
            if (fullProfile.Count == 3)
            {
                var tagsOnly = fullProfile[2].Split(Constants.DataDelimiter).ToList();
                UserData.Tags = tagsOnly;
            }

            _eventAggregator.GetEvent<SendMessageToServerEvent>().Publish(Constants.RequestFriendList
                                                                          + Constants.GroupDelimiter
                                                                          + UserData.Username);
        }

        private bool LoginCanExecute()
        {
            return !string.IsNullOrWhiteSpace(Username) && Username.Length > 1
                                                        && !string.IsNullOrWhiteSpace(Password) && Password.Length > 1;
        }

        private void LoginExecute()
        {
            var msg = Constants.RequestLogin + Constants.GroupDelimiter + UserData.Username + Constants.GroupDelimiter + Password;

            _eventAggregator.GetEvent<SendMessageToServerEvent>().Publish(msg);
        }
    }
}
