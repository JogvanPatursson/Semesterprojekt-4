﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Mime;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Microsoft.Win32;
using TLSNetworking;
using ProfileConsole;
using ProfileConsole.Core.Domain;
using ProfileConsole.Core.ServerCommunication;
using Salt_And_Hash;

//Taken from: https://msdn.microsoft.com/en-us/library/system.net.security.sslstream.aspx?cs-save-lang=1&cs-lang=csharp#code-snippet-2

namespace Examples.System.Net
{
    public sealed class SslTcpServer
    {
        private static Mutex _mutex = new Mutex();
        public LoginRequest loginRequest = new LoginRequest();

        public static Receiver receiver = new Receiver();
        public static Sender sender = new Sender();

        public static Dictionary<string, string> userID = new Dictionary<string, string>();
        public static Dictionary<string, SslStream> userStreams = new Dictionary<string, SslStream>();
        public static TcpListener listener = new TcpListener(IPAddress.Any, 443);
        static X509Certificate serverCertificate = null;
        // The certificate parameter specifies the name of the file 
        // containing the machine certificate.
        public static void RunServer(string certificate, TcpClient client)
        {
            serverCertificate = X509Certificate.CreateFromCertFile(certificate);

            ProcessClient(client);
        }
        static void ProcessClient(TcpClient client)
        {
            // A client has connected. Create the 
            // SslStream using the client's network stream.
            SslStream sslStream = new SslStream(
                client.GetStream(), false);
            // Authenticate the server but don't require the client to authenticate.
            try
            {
                sslStream.AuthenticateAsServer(serverCertificate,
                    false, SslProtocols.Tls, true);
                // Display the properties and settings for the authenticated stream.
                DisplaySecurityLevel(sslStream);
                DisplaySecurityServices(sslStream);
                DisplayCertificateInformation(sslStream);
                DisplayStreamProperties(sslStream);
                Console.WriteLine();

                //string IPId = ((IPEndPoint)client.Client.RemoteEndPoint).Address + "," + ((IPEndPoint)client.Client.RemoteEndPoint).Port;

                // Set timeouts for the read and write to 5 seconds.
                //sslStream.ReadTimeout = 5000;
                sslStream.WriteTimeout = 5000;

                while (true)
                {
                    string messageData;
                    // Read a message from the client.   
                    Console.WriteLine("Waiting for client message...");
                    try
                    {
                        messageData = receiver.ReceiveString(sslStream);
                        //Console.WriteLine((char)7);   //Makes bell sound
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        HandleLogout(sslStream);
                        return;
                    }

                    string[] parsedMessage = ParseMessage(messageData);
                    StringHandler(parsedMessage, sslStream);
                    Console.WriteLine();
                }

                //Console.ReadLine();
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
                sslStream.Close();
                client.Close();
                return;
            }
            finally
            {
                // The client stream will be closed with the sslStream
                // because we specified this behavior when creating
                // the sslStream.
                sslStream.Close();
                client.Close();
            }
        }

        static string[] ParseMessage(string message)
        {
            Console.WriteLine(message);
            return message.Split(Constants.GroupDelimiter);
        }

        static string[] ParseData(string dataCollection)
        {
            return dataCollection.Split(Constants.DataDelimiter);
        }

        static void StringHandler(string[] input, SslStream sslStream)
        {
            //Check if client is logged in
            //Client that is not logged in, should only be able to get to HandleLogin
            if (userStreams.FirstOrDefault(x => x.Value == sslStream).Key == null)  //If client isn't logged in
            {
                switch (input[0])
                {
                    case Constants.Signup:
                        if (input.Length == 3)
                        {
                            HandleSignup(input, sslStream);
                        }
                        break;
                    case Constants.RequestLogin:
                        if (input.Length == 3)
                        {
                            Console.WriteLine(input.Length);
                            HandleLogin(input, sslStream);
                        }
                        break;
                    default:
                        Console.WriteLine("Client is not logged in, and string is not recognized");
                        break;
                }
            }
            else  //if client is logged in
            {
                switch (input[0])
                {
                    case Constants.Write:   //Write message
                        HandleMessage(input, userStreams.FirstOrDefault(x => x.Value == sslStream).Key, sslStream);
                        break;
                    case "RP":   //Profile get
                        Console.WriteLine("GetProfile");
                        HandleGetProfile(input, sslStream);
                        break;
                    case "U":   //Update profile
                        HandleUpdateProfile(input, sslStream);
                        break;
                    case Constants.RequestLogin:   //Login
                        Console.WriteLine("User is already logged in");
                        sender.SendString(sslStream, "You are already logged in");
                        break;
                    case Constants.Logout:   //Logout
                        HandleLogout(sslStream);
                        break;
                    case Constants.SendFriendRequest:
                        HandleSendFriendRequest(input, userStreams.FirstOrDefault(x => x.Value == sslStream).Key, sslStream);
                        break;
                    case Constants.AcceptFriendRequest:
                        HandleFriendRequestAccept(input, userStreams.FirstOrDefault(x => x.Value == sslStream).Key, sslStream);
                        break;
                    case Constants.GetOldMessages:
                        HandleGetOldMessages(userStreams.FirstOrDefault(x => x.Value == sslStream).Key, sslStream);
                        break;
                    case "RFL":
                        HandleGetFriendlist(userStreams.FirstOrDefault(x => x.Value == sslStream).Key, sslStream);
                        break;
                    case Constants.GetUsernamesByTag:
                        HandleGetUserNamesByTag(input, sslStream);
                        break;
                    default:
                        Console.WriteLine("String is not recognized");
                        break;
                }

            }
        }

        private static void HandleGetUserNamesByTag(string[] input, SslStream sslStream)
        {
            var tag = SearchByTags.RequestTag(input[1].ToUpper());

            try
            {
                var tags = tag.UserInformation;
                var stringBuilder = new StringBuilder();

                foreach (var userInformation in tags)
                {
                    stringBuilder.Append(userInformation.UserName);
                    stringBuilder.Append(Constants.DataDelimiter);
                }

                sender.SendString(sslStream, Constants.GetUsernamesByTag + Constants.GroupDelimiter + stringBuilder.ToString());
            }
            catch (Exception e)
            {
                sender.SendString(sslStream, "NOK");
            }
        }

        private static void HandleGetFriendlist(string login, SslStream sslStream)
        {
            var friends = GetFriends.GetFriendList(login);
            var stringBuilder = new StringBuilder();

            foreach (var friend in friends)
            {
                stringBuilder.Append(friend);
                stringBuilder.Append(Constants.DataDelimiter);
            }

            string messageToSend = Constants.GetFriendList + Constants.GroupDelimiter + stringBuilder.ToString();

            sender.SendString(sslStream, messageToSend);
        }

        private static void HandleGetOldMessages(string login, SslStream sslStream)
        {
            var messages = GetAllMsgs.RequestAllMsgs(login);
            foreach (var message in messages)
            {
                string messageToSend;
                messageToSend = Constants.MessageReceived + Constants.GroupDelimiter + message.Sender + Constants.GroupDelimiter +
                                message.Receiver + Constants.GroupDelimiter + message.Message;
                sender.SendString(sslStream, messageToSend);
            }
        }

        static void HandleSignup(string[] input, SslStream sslStream)
        {
            var saltHash = new SaltedHash();

            string salt = saltHash.MakeSalt();
            string hashedPW = saltHash.ComputeHash(salt, input[2]);

            string result = SignUp.CreateProfile(input[1], salt, hashedPW);

            if (result == "OK")
            {
                Console.WriteLine("User: " + input[1] + " created");
                sender.SendString(sslStream, "SOK");
            }
            else
            {
                Console.WriteLine("User not created");
                sender.SendString(sslStream, "SNOK");
            }
        }

        static void HandleLogin(string[] input, SslStream sslStream)
        {
            var saltHash = new SaltedHash();

            //Check if username is already in logged in
            if (userStreams.ContainsKey(input[1]))
            {
                //username is in use
                Console.WriteLine("Username: " + input[1] + " is already online");
                sender.SendString(sslStream, "NOK");
            }
            else
            {
                //check if user is in database
                if (SearchByUsername.RequestUsername(input[1]) != null)
                {
                    Console.WriteLine("User: " + input[1] + " exists in database");

                    //Calculate hashed password with pw and salt
                    string salt = LoginRequest.GetSalt(input[1]);
                    string hashedPW = saltHash.ComputeHash(salt, input[2]);

                    //Make LoginRequest
                    string response = LoginRequest.Login(input[1], hashedPW);
                    if (response == "OK")
                    {
                        //Add to Dictionary
                        _mutex.WaitOne();
                        userStreams.Add(input[1], sslStream);
                        _mutex.ReleaseMutex();
                        Console.WriteLine("User logged in with username: " + input[1]);
                        sender.SendString(sslStream, "OK");
                    }
                    else
                    {
                        Console.WriteLine("Username or password was wrong");
                        sender.SendString(sslStream, "NOK");
                    }
                }
                else
                {
                    Console.WriteLine("User: " + input[1] + " does not exist in database");
                    sender.SendString(sslStream, "NOK");
                }
            }
        }

        static void HandleLogout(SslStream sslStream)
        {
            Console.WriteLine("Handle logout");
            //remove from dictionary
            try
            {
                var keyFromValue = userStreams.FirstOrDefault(x => x.Value == sslStream).Key;
                if (keyFromValue != null)
                {
                    Logout.LogoutDB(keyFromValue);

                    _mutex.WaitOne();
                    Console.WriteLine(userStreams.FirstOrDefault(x => x.Value == sslStream).Key + " logged out");
                    userStreams.Remove(userStreams.FirstOrDefault(x => x.Value == sslStream).Key);  //If user isn't logged in, dictionary remove will crash 
                    _mutex.ReleaseMutex();
                }
                else
                {
                    Console.WriteLine("Client wasn't logged in");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            //close connection
            CloseClientThread();
        }

        static void HandleMessage(string[] input, string login, SslStream sslStream)
        {
            //Check if users are friends
            if (GetAllMsgs.AreUsersFriends(login, input[1]))
            {
                //Check if user is online
                //Send to user
                if (userStreams.ContainsKey(input[1])) //If user is online
                {
                    Console.WriteLine("From: " + login + " to " + input[1]);
                    sender.SendString(userStreams[input[1]],
                        "R" + Constants.GroupDelimiter + login + Constants.GroupDelimiter + input[1] + Constants.GroupDelimiter + input[2]);
                }
                else //User is not online
                {
                    Console.WriteLine("User " + input[1] + " isn't logged in");
                    sender.SendString(userStreams[login], "User: " + input[1] + " isn't logged in");
                }

                //Save to database
                SaveMessages.SaveIncomingMessage(login, input[1], input[2]);
            }
        }

        static void HandleGetProfile(string[] input, SslStream sslStream)
        {
            string username;

            if (input.Length == 1 || string.IsNullOrEmpty(input[1]))
            {
                username = userStreams.FirstOrDefault(x => x.Value == sslStream).Key;
            }
            else
            {
                username = input[1];
            }
            var profile = GetMyProfile.RequestOwnInformation(username);
            string messageToSend = null;

            try
            {
                messageToSend = Constants.GetProfile + Constants.GroupDelimiter + username + Constants.GroupDelimiter + profile.description + Constants.GroupDelimiter;
                var stringBuilder = new StringBuilder();
                foreach (var tag in profile.tags)
                {
                    stringBuilder.Append(tag.TagName);
                    stringBuilder.Append(Constants.DataDelimiter);
                }

                messageToSend = messageToSend + stringBuilder.ToString();
                sender.SendString(sslStream, messageToSend);
            }
            catch (Exception e)
            {
                sender.SendString(sslStream, "NOK");
            }

        }

        static void HandleUpdateProfile(string[] input, SslStream sslStream)
        {
            string[] tags = ParseData(input[3]);

            string username = userStreams.FirstOrDefault(x => x.Value == sslStream).Key;

            var tagList = new List<string>();

            //Capitalize all tags
            foreach (var tag in tags)
            {
                tagList.Add(tag.ToUpper());
            }
            //tagList.AddRange(tags);

            UpdateProfile.UpdateProfileInformation(username, input[2], tagList);
        }

        static void HandleSendFriendRequest(string[] input, string login, SslStream sslStream)
        {
            AddFriend.AddFriendRequest(login, input[1]);

            //Check if username is already in logged in
            if (userStreams.ContainsKey(input[1]))
            {
                sender.SendString(userStreams[input[1]], "FRR" + Constants.GroupDelimiter + login);
            }
            else
            {
                Console.WriteLine("Send friendrequest. User: " + input[1] + " is not online");
            }
        }

        static void HandleFriendRequestAccept(string[] input, string login, SslStream sslStream)
        {
            AcceptFriendRequest.AcceptRequest(login, input[1]);

            //Check if username is already in logged in
            if (userStreams.ContainsKey(input[1]))
            {
                sender.SendString(userStreams[input[1]], "FRA" + Constants.GroupDelimiter + login);
            }
            else
            {
                Console.WriteLine("Accept friendrequest. User: " + input[1] + " is not online");
            }
        }

        static void FailedLogin()
        {
            //send message to client
        }

        static void CloseClientThread()
        {


        }

        static void DisplaySecurityLevel(SslStream stream)
        {
            Console.WriteLine("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
            Console.WriteLine("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
            Console.WriteLine("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength);
            Console.WriteLine("Protocol: {0}", stream.SslProtocol);
        }
        static void DisplaySecurityServices(SslStream stream)
        {
            Console.WriteLine("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer);
            Console.WriteLine("IsSigned: {0}", stream.IsSigned);
            Console.WriteLine("Is Encrypted: {0}", stream.IsEncrypted);
        }
        static void DisplayStreamProperties(SslStream stream)
        {
            Console.WriteLine("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite);
            Console.WriteLine("Can timeout: {0}", stream.CanTimeout);
        }
        static void DisplayCertificateInformation(SslStream stream)
        {
            Console.WriteLine("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);

            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.",
                    localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Local certificate is null.");
            }
            // Display the properties of the client's certificate.
            X509Certificate remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                Console.WriteLine("Remote cert was issued to {0} and is valid from {1} until {2}.",
                    remoteCertificate.Subject,
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Remote certificate is null.");
            }
        }
        //private static void DisplayUsage()
        //{
        //    Console.WriteLine("To start the server specify:");
        //    Console.WriteLine("serverSync certificateFile.cer");
        //    Environment.Exit(1);
        //}
        public static int Main(string[] args)
        {
            string certificate = null;
            //if (args == null || args.Length < 1)
            //{
            //DisplayUsage();
            //}
            //certificate = args[0];
            //certificate = "D:/Users/Martin/Dropbox/IKT/4.Semester/PROJ4/Semesterprojekt-4/Feature test/SSL-Test/MartoTestCer.cer";

            Console.WriteLine("Write address to server certificate");
            certificate = Console.ReadLine();

            Console.WriteLine();

            Console.WriteLine("************This is Server program************");

            listener.Start();

            //Keep listening and start new thread when client connects
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();  //Someone has connected

                Thread newThread =
                    new Thread(
                        unused => RunServer(certificate, client)    //The method where the thread is run
                    );

                newThread.Start();
            }
            return 0;
        }
    }
}