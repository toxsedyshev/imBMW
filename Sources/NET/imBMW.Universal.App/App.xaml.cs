using imBMW.Clients;
using imBMW.iBus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using imBMW.Universal.App.Views;
using Windows.UI.Core;
using Windows.Foundation.Metadata;
using System.Threading.Tasks;
using Windows.System.Display;
using Windows.ApplicationModel.Core;

namespace imBMW.Universal.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private DisplayRequest displayRequest;

        public DisplayRequest DisplayRequest
        {
            get
            {
                if (displayRequest == null)
                {
                    displayRequest = new DisplayRequest();
                }
                return displayRequest;
            }
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            BluetoothClient.Current.InternalMessageReceived += BluetoothClient_InternalMessageReceived;

            Logger.Logged += Logger_Logged;

            Manager.InitRealDevices();
            Manager.AfterMessageReceived += Manager_AfterMessageReceived;
            Manager.MessageEnqueued += Manager_MessageEnqueued;

            //Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
            //    Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
            //    Microsoft.ApplicationInsights.WindowsCollectors.Session);
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.Resuming += OnResuming;
        }
        
        void Logger_Logged(LogItem log)
        {
            try
            {
                var s = log.Timestamp.ToString() + " [" + log.PriorityLabel + "] " + log.Message;
                if (log.Exception != null)
                {
                    s += ": " + log.Exception.Message + " Stack trace:\n" + log.Exception.StackTrace;
                }
                Debug.WriteLine(s);
                if (log.Priority == LogPriority.Error)
                {
                    ShowToast(log.Message, log.Exception != null ? log.Exception.Message : "");

                    /*Window.Current.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        new MessageDialog(s, "Error").ShowAsync();
                    });*/
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Logger error: " + ex + "\r\r" + ex.StackTrace);
            }
        }

        void Manager_MessageEnqueued(MessageEventArgs e)
        {
            Logger.Info(e.Message.ToString(), " >");
        }

        void Manager_AfterMessageReceived(iBus.MessageEventArgs e)
        {
            Logger.Info(e.Message.ToString(), "< ");
        }

        void BluetoothClient_InternalMessageReceived(SocketClient sender, InternalMessage message)
        {
            Logger.Info(message.ToString(), "<I");
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {

//#if DEBUG
//            if (System.Diagnostics.Debugger.IsAttached)
//            {
//                this.DebugSettings.EnableFrameRateCounter = true;
//            }
//#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += (s, a) =>
                {
                    if (rootFrame.CanGoBack)
                    {
                        rootFrame.GoBack();
                        a.Handled = true;
                    }
                };

                /*if (ApiInformation.IsTy‌​pePresent("Windows.Ph‌​one.UI.Input.Hardware‌​Buttons")))
                {
                    Windows.Phone.UI.Input.HardwareButtons.BackPressed += (s, a) =>
                    {
                        Debug.WriteLine("BackPressed");
                        if (Frame.CanGoBack)
                        {
                            Frame.GoBack();
                            a.Handled = true;
                        }
                    };
                }*/

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();

            if (e.PreviousExecutionState != ApplicationExecutionState.Running)
            {
                await Connect();
            }
        }

        private async void BluetoothClient_Disconnected()
        {
            Logger.Info("BT Disconnected");
            ShowToast("imBMW Client Disconnected");
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    DisplayRequest.RequestRelease();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Sleep mode enable error.");
                }
            });
        }

        private void BluetoothClient_Connecting()
        {
            Logger.Info("BT Connecting...");
        }

        private async void BluetoothClient_Connected()
        {
            Logger.Info("BT Connected");
            ShowToast("imBMW Client Connected", "Bluetooth Device: " + BluetoothClient.Current.DeviceName);
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    DisplayRequest.RequestActive();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Sleep mode disable error.");
                }
            });
        }

        private static void ShowToast(string title, string description = null)
        {
            try
            {
                var toastTemplate = ToastTemplateType.ToastText02;
                var toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
                toastXml.GetElementsByTagName("text")[0].AppendChild(toastXml.CreateTextNode(title));
                if (!string.IsNullOrWhiteSpace(description))
                {
                    toastXml.GetElementsByTagName("text")[1].AppendChild(toastXml.CreateTextNode(description));
                }
                var toast = new ToastNotification(toastXml);

                //var toastNode = toastXml.SelectSingleNode("/toast");
                //((XmlElement)toastNode).SetAttribute("launch", "{\"type\":\"toast\",\"param1\":\"12345\",\"param2\":\"67890\"}");
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
            catch { }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private async void OnResuming(object sender, object e)
        {
            await Connect();
        }

        private async Task Connect()
        {
            try
            {
                await BluetoothClient.Current.Connect();
                BluetoothClient_Connected();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "BT Not Connected");
            }

            BluetoothClient.Current.Connected += BluetoothClient_Connected;
            BluetoothClient.Current.Connecting += BluetoothClient_Connecting;
            BluetoothClient.Current.Disconnected += BluetoothClient_Disconnected;
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            try
            {
                BluetoothClient.Current.Disconnect();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "BT Disconnecting");
            }
            BluetoothClient.Current.Connected -= BluetoothClient_Connected;
            BluetoothClient.Current.Connecting -= BluetoothClient_Connecting;
            BluetoothClient.Current.Disconnected -= BluetoothClient_Disconnected;

            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
