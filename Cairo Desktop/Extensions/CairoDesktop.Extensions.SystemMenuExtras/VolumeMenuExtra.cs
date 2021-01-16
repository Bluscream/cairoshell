﻿using CairoDesktop.Configuration;
using CairoDesktop.ObjectModel;
using System.Windows.Controls;
using CairoDesktop.SupportingClasses;
using ManagedShell.Common.Helpers;

namespace CairoDesktop.Extensions.SystemMenuExtras
{
    class VolumeMenuExtra : MenuExtra
    {
        private Volume _volume;

        public override UserControl StartControl(CairoAppBarWindow menuBar)
        {
            if (Settings.Instance.EnableMenuExtraVolume && EnvironmentHelper.IsWindows10OrBetter && EnvironmentHelper.IsAppRunningAsShell)
            {
                _volume = new Volume();
                return _volume;
            }

            return null;
        }
    }
}