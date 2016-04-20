#if !MF_FRAMEWORK_VERSION_V4_1

using imBMW.Features.Menu;
using imBMW.iBus;
using imBMW.Multimedia;
using imBMW.Tools;
using System;

namespace imBMW.Features
{
    public class InternalCommunications
    {
        static BluetoothWT32 client;

        public static bool HasConnection
        {
            get
            {
                return client != null && client.SPPLink != BluetoothWT32.Link.Unset;
            }
        }

        public static void Register(BluetoothWT32 wt32)
        {
            if (client != null)
            {
                throw new Exception("InternalCommunications already registered.");
            }
            client = wt32;

            var wt32Parser = new MessageParser();
            wt32Parser.MessageReceived += m =>
            {
                if (m is InternalMessage)
                {
                    ProcessInternalMessage((InternalMessage)m);
                }
                else
                {
                    Manager.EnqueueMessage(m);
                }
            };
            wt32.BTCommandReceived += (s, l, c) =>
            {
                if (l == wt32.SPPLink)
                {
                    try
                    {
                        wt32Parser.Parse(c);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "iBus from WT32", "BT <");
                    }
                }
            };
            Manager.AfterMessageReceived += (m) =>
            {
                if (HasConnection)
                {
                    try
                    {
                        wt32.SendCommand(m.Message.Packet, wt32.SPPLink, "iBus data");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "iBus to WT32", "> BT");
                    }
                }
            };
        }

        private static void ProcessInternalMessage(InternalMessage m)
        {
            Logger.Info(m.DataString, "INT <");

            switch (m.DataString)
            {
                case "GET_BM_SCREEN":
                    // TODO send last BM screen
                    if (BordmonitorMenu.Instance.IsEnabled)
                    {
                        BordmonitorMenu.Instance.UpdateScreen(MenuScreenUpdateReason.Refresh);
                    }
                    break;
            }
        }

        public static void SendMessage(Message m)
        {
            if (HasConnection)
            {
                try
                {
                    client.SendCommand(m.Packet, client.SPPLink, m.ToString());
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Internal to WT32", "> BT");
                }
            }
        }
    }
}

#endif