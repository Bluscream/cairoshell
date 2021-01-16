﻿using System.Windows.Forms;
using CairoDesktop.Configuration;
using CairoDesktop.SupportingClasses;
using ManagedShell.AppBar;
using ManagedShell.Interop;

namespace CairoDesktop.Extensions.Taskbar
{
    public class TaskbarWindowService : AppBarWindowService
    {
        private readonly DesktopManager _desktopManager;

        public TaskbarWindowService(ShellManagerService shellManagerService, WindowManager windowManager, DesktopManager desktopManager) : base(shellManagerService, windowManager)
        {
            _desktopManager = desktopManager;

            EnableMultiMon = Settings.Instance.EnableTaskbarMultiMon;
            EnableService = Settings.Instance.EnableTaskbar;

            if (EnableService)
            {
                _shellManager.ExplorerHelper.HideExplorerTaskbar = true;
                _shellManager.AppBarManager.AppBarEvent += AppBarEvent;
            }
        }

        private void AppBarEvent(object sender, AppBarEventArgs e)
        {
            if (Settings.Instance.TaskbarMode == 2)
            {
                if (sender is CairoAppBarWindow menuBar)
                {
                    var taskbar = (Taskbar) WindowManager.GetScreenWindow(Windows, menuBar.Screen);

                    if (taskbar == null)
                    {
                        return;
                    }

                    if (taskbar.AppBarEdge != menuBar.AppBarEdge)
                    {
                        return;
                    }

                    if (e.Reason == AppBarEventReason.MouseEnter)
                    {
                        taskbar.CanAutoHide = false;
                    }
                    else if (e.Reason == AppBarEventReason.MouseLeave)
                    {
                        taskbar.CanAutoHide = true;
                    }
                }
            }
        }

        protected override void OpenWindow(Screen screen)
        {
            Taskbar newTaskbar = new Taskbar(_shellManager, _windowManager, _desktopManager, screen, Settings.Instance.TaskbarPosition == 1 ? NativeMethods.ABEdge.ABE_TOP : NativeMethods.ABEdge.ABE_BOTTOM);
            Windows.Add(newTaskbar);
            newTaskbar.Show();
        }

        public override void Dispose()
        {
            if (EnableService)
            {
                _shellManager.AppBarManager.AppBarEvent -= AppBarEvent;
                _shellManager.ExplorerHelper.HideExplorerTaskbar = false;
            }
        }
    }
}
