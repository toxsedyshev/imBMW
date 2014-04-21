using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;
using imBMW.iBus;
using imBMW.Tools;
using System.Threading;

namespace imBMW.Features.Menu
{
    public class BordmonitorMenu : MenuBase
    {
        static BordmonitorMenu instance;
        static Timer refreshingScreenTimeoutTimer;
        const int refreshingScreenTimeout = 100;

        bool updating;
        bool screenSwitched;

        private BordmonitorMenu()
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.Radio, ProcessToRadioMessage);
        }

        protected void ProcessRadioMessage(Message m)
        {
            if (!updating)
            {
                if (m.Data.Compare(Bordmonitor.MessageClearScreen.Data) || m.Data.Compare(Bordmonitor.MessageRefreshScreen.Data))
                {
                    // TODO test "INFO" button
                    UpdateScreen();
                    if (screenSwitched)
                    {
                        screenSwitched = false;
                        ScreenWakeup();
                    }
                }
            }
            // 0x46 0x02 - switched to BC. 0x46 0x01 - switched to menu, after 0x45 0x91 from nav
            if (m.Data.Length == 2 && m.Data[0] == 0x46 && (m.Data[1] == 0x01 || m.Data[1] == 0x02))
            {
                screenSwitched = true;
                ScreenSuspend();
            }
        }

        protected void ProcessToRadioMessage(Message m)
        {
            if (IsEnabled)
            {
                if (m.Data.Length == 4 && m.Data.StartsWith(0x31, 0x60, 0x00))
                {
                    var index = GetItemIndex(m.Data[3]);
                    var item = CurrentScreen.GetItem(index);
                    if (item != null)
                    {
                        item.Click();
                    }
                }
            }
        }

        protected override void DrawScreen()
        {
            updating = true;
            base.DrawScreen();

            Bordmonitor.ShowText(CurrentScreen.Status ?? String.Empty, BordmonitorFields.Status);
            Bordmonitor.ShowText(CurrentScreen.Title ?? String.Empty, BordmonitorFields.Title);
            for (byte i = 0; i < 10; i++)
            {
                var item = CurrentScreen.GetItem(i);
                var s = item == null ? String.Empty : item.Text;
                Bordmonitor.ShowText(s ?? String.Empty, 
                    BordmonitorFields.Item,
                    GetItemIndex(i), 
                    item != null && item.IsChecked);
            }
            Bordmonitor.RefreshScreen();
            UpdatingDelay();
        }

        byte GetItemIndex(byte index, bool back = false)
        {
            // TODO also try 1-3 & 6-8
            var smallscreenOffset = CurrentScreen.ItemsCount > 6 ? 0 : 2;
            if (back)
            {
                if (index > 7)
                {
                    index -= 0x40;
                }
                smallscreenOffset *= -1;
            }
            return (byte)(index <= 2 ? index : index + smallscreenOffset);
        }

        void UpdatingDelay()
        {
            if (refreshingScreenTimeoutTimer != null)
            {
                refreshingScreenTimeoutTimer.Dispose();
                refreshingScreenTimeoutTimer = null;
            }
            refreshingScreenTimeoutTimer = new Timer(delegate
            {
                updating = false;
            }, null, refreshingScreenTimeout, 0);
        }

        public static BordmonitorMenu Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BordmonitorMenu();
                }
                return instance;
            }
        }
    }
}
