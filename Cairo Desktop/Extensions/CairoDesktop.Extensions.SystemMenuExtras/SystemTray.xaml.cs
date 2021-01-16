﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Specialized;
using System.ComponentModel;
using CairoDesktop.Configuration;
using CairoDesktop.SupportingClasses;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using ManagedShell.WindowsTray;

namespace CairoDesktop.Extensions.SystemMenuExtras
{
    public partial class SystemTray
    {
        private readonly CairoAppBarWindow MenuBar;
        private readonly NotificationArea _notificationArea;

        public SystemTray(CairoAppBarWindow menuBar, NotificationArea notificationArea)
        {
            InitializeComponent();

            _notificationArea = notificationArea;
            DataContext = _notificationArea;
            MenuBar = menuBar;

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;

            ((INotifyCollectionChanged)PinnedItems.Items).CollectionChanged += PinnedItems_CollectionChanged;
            ((INotifyCollectionChanged)UnpinnedItems.Items).CollectionChanged += UnpinnedItems_CollectionChanged;

            if (Settings.Instance.SysTrayAlwaysExpanded)
            {
                UnpinnedItems.Visibility = Visibility.Visible;
            }

            // Don't allow showing both the Windows TaskBar and the Cairo tray
            if (Settings.Instance.EnableSysTray && (Settings.Instance.EnableTaskbar || EnvironmentHelper.IsAppRunningAsShell) && _notificationArea.Handle == IntPtr.Zero)
            {
                _notificationArea.Initialize();
            }
        }

        private void PinnedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (PinnedItems.Items.Count > 0)
            {
                PinnedItems.Margin = new Thickness(16, 0, 0, 0);
            }
            else
            {
                PinnedItems.Margin = new Thickness(0, 0, 0, 0);
            }
        }

        private void UnpinnedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SetToggleVisibility();
        }

        private void btnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (UnpinnedItems.Visibility == Visibility.Visible)
            {
                UnpinnedItems.Visibility = Visibility.Collapsed;
            }
            else
            {
                UnpinnedItems.Visibility = Visibility.Visible;
            }
        }

        private void SetToggleVisibility()
        {
            if (!Settings.Instance.SysTrayAlwaysExpanded)
            {
                if (UnpinnedItems.Items.Count > 0)
                    btnToggle.Visibility = Visibility.Visible;
                else
                    btnToggle.Visibility = Visibility.Collapsed;
            }
        }

        public TrayHostSizeData GetMenuBarSizeData()
        {
            return new TrayHostSizeData { edge = MenuBar.AppBarEdge, rc = new NativeMethods.Rect { Top = (int)(MenuBar.Top * MenuBar.DpiScale), Left = (int)(MenuBar.Left * MenuBar.DpiScale), Bottom = (int)((MenuBar.Top + MenuBar.Height) * MenuBar.DpiScale), Right = (int)((MenuBar.Left + MenuBar.Width) * MenuBar.DpiScale) } };
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var trayIcon = (sender as Decorator).DataContext as NotifyIcon;

            if (MenuBar != null)
            {
                // set current menu bar to return placement for ABM_GETTASKBARPOS message
                _notificationArea.SetTrayHostSizeData(GetMenuBarSizeData());
            }
            trayIcon?.IconMouseClick(e.ChangedButton, MouseHelper.GetCursorPositionParam(), System.Windows.Forms.SystemInformation.DoubleClickTime);
        }

        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            Decorator sendingDecorator = sender as Decorator;
            var trayIcon = sendingDecorator.DataContext as NotifyIcon;

            if (trayIcon != null)
            {
                // update icon position for Shell_NotifyIconGetRect
                Point location = sendingDecorator.PointToScreen(new Point(0, 0));
                double dpiScale = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;

                trayIcon.Placement = new NativeMethods.Rect { Top = (int)location.Y, Left = (int)location.X, Bottom = (int)(sendingDecorator.ActualHeight * dpiScale), Right = (int)(sendingDecorator.ActualWidth * dpiScale) };
                trayIcon.IconMouseEnter(MouseHelper.GetCursorPositionParam());
            }
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            var trayIcon = (sender as Decorator).DataContext as NotifyIcon;

            trayIcon?.IconMouseLeave(MouseHelper.GetCursorPositionParam());
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            var trayIcon = (sender as Decorator).DataContext as NotifyIcon;

            trayIcon?.IconMouseMove(MouseHelper.GetCursorPositionParam());
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.PropertyName))
            {
                return;
            }

            if (e.PropertyName != "SysTrayAlwaysExpanded")
            {
                return;
            }

            if (Settings.Instance.SysTrayAlwaysExpanded)
            {
                btnToggle.Visibility = Visibility.Collapsed;
                UnpinnedItems.Visibility = Visibility.Visible;
            }
            else
            {
                btnToggle.IsChecked = false;
                UnpinnedItems.Visibility = Visibility.Collapsed;
                SetToggleVisibility();
            }
        }
    }
}