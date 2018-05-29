﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using ProfileConsole.Core.ServerCommunication;
using TLSNetworking;

namespace Server
{
    class Friendlist
    {
        public static void HandleGetFriendlist(string login, SslStream sslStream)
        {
            var friends = GetFriends.GetFriendList(login);
            var stringBuilder = new StringBuilder();

            foreach (var friend in friends)
            {
                stringBuilder.Append(friend);
                stringBuilder.Append(Constants.DataDelimiter);
            }

            string messageToSend = Constants.GetFriendList + Constants.GroupDelimiter + stringBuilder.ToString();

            Sender.SendString(sslStream, messageToSend);
        }
    }
}
