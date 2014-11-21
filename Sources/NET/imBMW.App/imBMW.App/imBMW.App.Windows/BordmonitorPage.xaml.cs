using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using imBMW.App.Common;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using imBMW.iBus.Devices.Real;

// The Item Detail Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234232

namespace imBMW.App
{
    /// <summary>
    /// A page that displays details for a single item within a group while allowing gestures to
    /// flip through other items belonging to the same group.
    /// </summary>
    public sealed partial class BordmonitorPage : Page
    {
        class ObservableObject : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            protected void Set<T>(ref T obj, T value, [CallerMemberName]string property = "")
            {
                if (obj == null && value != null || obj != null && obj.Equals(value))
                {
                    obj = value;
                    var e = PropertyChanged;
                    if (e != null)
                    {
                        e(this, new PropertyChangedEventArgs(property));
                    }
                }
            }
        }

        class BordmonitorScreen : ObservableObject
        {
            public BordmonitorScreen()
            {
                Items = new Dictionary<int, BordmonitorText>();
            }

            public string Title { get; set; }

            public string Status { get; set; }

            public Dictionary<int, BordmonitorText> Items { get; set; }
        }

        private static BordmonitorScreen currentScreen = new BordmonitorScreen { Title = "imBMW" };

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public BordmonitorPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            Bordmonitor.TextReceived += Bordmonitor_TextReceived;
            Bordmonitor.ScreenCleared += Bordmonitor_ScreenCleared;
            Bordmonitor.ScreenRefreshed += Bordmonitor_ScreenRefreshed;

            RefreshScreen();
        }

        void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            Bordmonitor.TextReceived -= Bordmonitor_TextReceived;
            Bordmonitor.ScreenCleared -= Bordmonitor_ScreenCleared;
            Bordmonitor.ScreenRefreshed -= Bordmonitor_ScreenRefreshed;
        }

        void RefreshScreen()
        {
            DefaultViewModel["Title"] = currentScreen.Title;
            DefaultViewModel["Status"] = currentScreen.Status;
            for (byte i = 0; i < 10; i++)
            {
                var item = currentScreen.Items.Keys.Contains(i) ? currentScreen.Items[i] : null;
                if (item != null && string.IsNullOrWhiteSpace(item.Text))
                {
                    item = null;
                }
                DefaultViewModel["Item" + i] = item;
            }
        }

        void Bordmonitor_ScreenRefreshed()
        {
            RefreshScreen();
        }

        void Bordmonitor_ScreenCleared()
        {
            currentScreen.Items.Clear();
            RefreshScreen();
        }

        void Bordmonitor_TextReceived(BordmonitorText args)
        {
            switch (args.Field)
            {
                case BordmonitorFields.Title:
                    currentScreen.Title = args.Text;
                    break;
                case BordmonitorFields.Status:
                    currentScreen.Status = args.Text;
                    break;
                case BordmonitorFields.Item:
                    var items = args.ParseItems();
                    foreach (var i in items)
                    {
                        currentScreen.Items[i.Index] = i;
                    }
                    break;
            }
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var b = (Button)sender;
            var index = byte.Parse((string)b.Tag);
            Bordmonitor.PressItem(index);
        }
    }
}
