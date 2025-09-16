using Archipelago.MultiClient.Net.MessageLog.Messages;
using Reloaded.Mod.Interfaces;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using System.Drawing;

namespace RnSArchipelago.Connection
{
    internal class MessageHandler
    {

        private static readonly MessageHandler _instance = new MessageHandler();

        internal static MessageHandler Instance => _instance;

        private MessageHandler() { }

        internal IRNSReloaded rnsReloaded;
        internal ILoggerV1 logger;
        internal Config.Config modConfig;


        internal unsafe void OnMessageReceived(LogMessage message)
        {
            switch (message)
            {
                case HintItemSendLogMessage hintLogMessage:
                    var hintReceiver = hintLogMessage.Receiver;
                    var hintSender = hintLogMessage.Sender;
                    var hintNetworkItem = hintLogMessage.Item;
                    var hintFound = hintLogMessage.IsFound;
                    break;
                case ItemSendLogMessage itemSendLogMessage:
                    var receiver = itemSendLogMessage.Receiver;
                    var sender = itemSendLogMessage.Sender;
                    var networkItem = itemSendLogMessage.Item;

                    var messageToSend = message.ToString();
                    var sourceId = -1;
                    if (itemSendLogMessage.IsSenderTheActivePlayer)
                    {
                        sourceId = 0;
                        messageToSend = messageToSend.Remove(0, messageToSend.IndexOf(" "));
                    }

                    var itemMessage = new RValue();
                    rnsReloaded.CreateString(&itemMessage, messageToSend);
                    rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(sourceId), new(), new(0), itemMessage, new(0)]);
                    logger.PrintMessage(message.ToString(), System.Drawing.Color.Cyan);
                    break;
                case PlayerSpecificLogMessage playerLogMessage:
                    var playerMessage = new RValue();
                    rnsReloaded.CreateString(&playerMessage, message.ToString());
                    rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(-1), new(0), new(0), playerMessage, new(0)]);
                    logger.PrintMessage(message.ToString(), System.Drawing.Color.White);
                    break;
                case AdminCommandResultLogMessage:
                case CommandResultLogMessage:
                case CountdownLogMessage:
                case ServerChatLogMessage:
                case TutorialLogMessage:
                default:
                    if (modConfig.SystemLog)
                    {
                        var gameMessage = new RValue();
                        rnsReloaded.CreateString(&gameMessage, message.ToString());
                        rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(-1), new(0), new(0), gameMessage, new(0)]);
                        logger.PrintMessage(message.ToString(), System.Drawing.Color.White);
                    }
                    break;
            }
        }

        internal unsafe void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.RoomInfo:
                    break;
                case ArchipelagoPacketType.ConnectionRefused:
                    var message = "Connection refused: " + string.Join(", ", ((ConnectionRefusedPacket)packet).Errors);
                    var gameMessage = new RValue();
                    rnsReloaded.CreateString(&gameMessage, message);
                    rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(-1), new(), new(0), gameMessage, new(0)]);
                    this.logger.PrintMessage(message, Color.Red);
                    break;
                case ArchipelagoPacketType.Connected:
                case ArchipelagoPacketType.ReceivedItems:
                case ArchipelagoPacketType.LocationInfo:
                case ArchipelagoPacketType.RoomUpdate:
                    break;
                case ArchipelagoPacketType.PrintJSON: // Handled through OnMessageRecieved, so will likely never use
                    break;
                case ArchipelagoPacketType.DataPackage:
                case ArchipelagoPacketType.Bounced:
                case ArchipelagoPacketType.InvalidPacket:
                case ArchipelagoPacketType.Retrieved:
                case ArchipelagoPacketType.SetReply:
                    break;
        }
        }
    }
}
