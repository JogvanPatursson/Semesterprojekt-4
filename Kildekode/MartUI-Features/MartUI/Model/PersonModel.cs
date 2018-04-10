﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Prism.Mvvm;

namespace MartUI.Model
{
    public class PersonModel : BindableBase
    {
        private string _username;
        private string _password;

        public PersonModel()
        { }
        public PersonModel(string name , string pass)
        {
            Username = name;
            Password = pass;
        }
        public string Username
        {
            get { return _username; }
            set { SetProperty(ref _username, value); } // If username != value, notify
        }

        public string Password
        {
            get { return _password; }
            set { SetProperty(ref _password, value); } // If username != value, notify
        }
    }
}
