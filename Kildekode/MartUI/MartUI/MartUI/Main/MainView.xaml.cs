﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using MahApps.Metro.Controls;
using MartUI.Events;
using MartUI.Main;
using Prism.Events;

namespace MartUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainView
    {
        private bool Maximized = false;
        private double PrevLeft, PrevTop, PrevWidth, PrevHeight;
        private IEventAggregator _eventAggregator;

        public MainView()
        {
            _eventAggregator = GetEventAggregator.Get();
            InitializeComponent();
            // ------ REMOVE THIS SHIT WHEN CHECKED OUT --------
            DataContext = new MainViewModel();
            SizeChanged += MainView_SizeChanged;
        }

        //Allows the user to move the window when pressing the titlebar.
        private void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 1)
            {
                if (Maximized) //Places the middle of the titlebar at the mouse and reverts to previous window size
                {
                        Point Position = Mouse.GetPosition(TitleBar);

                        ExpandApplication(sender, e);
                        Left = Position.X - 7 - Width / 2; //Windows randomly moves the window 7 pixels to the right
                        Top = Position.Y - 9;
                }
            }
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void MainView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WindowState == WindowState.Maximized) //Makes sure the window doesn't fullscreen when maximized
            {
                WindowState = WindowState.Normal;
                PrevLeft = Left;
                PrevTop = Top;
                PrevWidth = Width;
                PrevHeight = Height;
                Width = SystemParameters.WorkArea.Width;
                Height = SystemParameters.WorkArea.Height;
                Top = SystemParameters.WorkArea.Top;
                Left = SystemParameters.WorkArea.Left;
                Maximized = true;
            }
        }

        private void ExitApplication(object sender, RoutedEventArgs e)
        {
            _eventAggregator.GetEvent<SendMessageToServerEvent>().Publish(Constants.Logout); //Logs out before exiting the application
            Environment.Exit(0);
        }

        private void MinimizeApplication(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized; //Minimizes application to taskbar
        }

        private void ExpandApplication(object sender, RoutedEventArgs e)
        {
            if (Maximized) //Return to previous size if window is maximized
            {
                Left = PrevLeft;
                Top = PrevTop;
                Width = PrevWidth;
                Height = PrevHeight;
                Maximized = false;
            }
            else //Maximizes window if window is not maximized
            {
                PrevLeft = Left;
                PrevTop = Top;
                PrevWidth = Width;
                PrevHeight = Height;
                Width = SystemParameters.WorkArea.Width;
                Height = SystemParameters.WorkArea.Height;
                Top = SystemParameters.WorkArea.Top;
                Left = SystemParameters.WorkArea.Left;
                Maximized = true;
            }
        }

        private void TitleBar_LeftMouseButtonDoubleClick(object sender, MouseButtonEventArgs e)
        {
            {
                if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2) //Enables double click on titlebar to expand application
                {
                    if (Maximized)
                    {
                        ExpandApplication(sender, e);
                        Point Position = Mouse.GetPosition(TitleBar);
                        Left = Position.X - 7 - Width / 2;
                        Top = Position.Y - 9;
                    }
                    else
                    {
                        ExpandApplication(sender, e);
                    }
                }
            }
        }
    }
}
