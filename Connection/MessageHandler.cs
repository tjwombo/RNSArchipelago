using Archipelago.MultiClient.Net.MessageLog.Messages;
using RNSReloaded.Interfaces.Structs;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using System.Drawing;
using RnSArchipelago.Utils;
using RnSArchipelago.Data;
using System.Diagnostics.CodeAnalysis;

namespace RnSArchipelago.Connection
{
    internal class MessageHandler
    {

        private static readonly MessageHandler _instance = new MessageHandler();

        internal static MessageHandler Instance => _instance;

        private MessageHandler() { }

        internal WeakReference<IRNSReloaded>? rnsReloadedRef;
        internal ILoggerV1? logger;
        internal Config.Config? modConfig;
        internal SharedData? data;

        private static readonly string GAME = "Rabbit and Steel";
        internal int slot = 0;

        private bool IsReady(
            [MaybeNullWhen(false), NotNullWhen(true)] out IRNSReloaded rnsReloaded
        )
        {
            if (this.rnsReloadedRef != null && this.rnsReloadedRef.TryGetTarget(out rnsReloaded))
            {
                return rnsReloaded != null;
            }
            this.logger!.PrintMessage("Unable to find rnsReloaded in MessageHandler", System.Drawing.Color.Red);
            rnsReloaded = null;
            return false;
        }

        internal unsafe void SendDisconnectedMessage()
        {
            if (IsReady(out var rnsReloaded))
            {
                var message = new RValue();
                rnsReloaded!.CreateString(&message, "Disconnected from the multiworld");
                rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(-1), new(0), new(0), message, new(0)]);
            }
        }

        internal unsafe void OnMessageReceived(LogMessage message)
        {
            if (IsReady(out var rnsReloaded))
            {
                switch (message)
                {
                    case HintItemSendLogMessage hintLogMessage:
                        var hintReceiver = hintLogMessage.Receiver;
                        var hintSender = hintLogMessage.Sender;
                        var hintNetworkItem = hintLogMessage.Item;
                        var hintFound = hintLogMessage.IsFound;

                        if (modConfig!.SystemLog)
                        {
                            var hintMessage = new RValue();
                            rnsReloaded!.CreateString(&hintMessage, message.ToString());
                            rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(-1), new(0), new(0), hintMessage, new(0)]);
                        }
                        logger!.PrintMessage(hintLogMessage.ToString(), System.Drawing.Color.Cyan);
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
                        if ((modConfig!.OtherLog || itemSendLogMessage.IsRelatedToActivePlayer) &&
                            ((modConfig!.ProgressionLog && itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.Advancement)) ||
                            (modConfig!.UsefulLog && itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.NeverExclude)) ||
                            (modConfig!.FillerLog && !itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.Advancement) && !itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.NeverExclude) && !itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.Trap)) ||
                            (modConfig!.TrapLog && itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.Trap))))
                        {
                            rnsReloaded!.CreateString(&itemMessage, messageToSend);
                            rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(sourceId), new(), new(0), itemMessage, new(0)]);
                        }
                        logger!.PrintMessage(message.ToString(), System.Drawing.Color.Cyan);
                        break;
                    case PlayerSpecificLogMessage playerLogMessage:
                        var playerMessage = new RValue();
                        if (modConfig!.SystemLog)
                        {
                            rnsReloaded!.CreateString(&playerMessage, message.ToString());
                            rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(-1), new(0), new(0), playerMessage, new(0)]);
                        }
                        logger!.PrintMessage(message.ToString(), System.Drawing.Color.White);
                        break;
                    case AdminCommandResultLogMessage:
                    case CommandResultLogMessage:
                    case CountdownLogMessage:
                    case ServerChatLogMessage:
                    case TutorialLogMessage:
                    default:
                        if (modConfig!.SystemLog)
                        {
                            var gameMessage = new RValue();
                            rnsReloaded!.CreateString(&gameMessage, message.ToString());
                            rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(-1), new(0), new(0), gameMessage, new(0)]);
                            logger!.PrintMessage(message.ToString(), System.Drawing.Color.White);
                        }
                        break;
                }
            }
        }

        internal unsafe void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            if (IsReady(out var rnsReloaded))
            {
                switch (packet.PacketType)
                {
                    case ArchipelagoPacketType.RoomInfo:
                        // Save the seed so we can have a static random
                        var room = (RoomInfoPacket)packet;
                        this.data!.SetValue<object>(DataContext.Options, "seed", room.SeedName);
                        break;
                    case ArchipelagoPacketType.ConnectionRefused:
                        var message = "Connection refused: " + string.Join(", ", ((ConnectionRefusedPacket)packet).Errors);
                        var gameMessage = new RValue();
                        rnsReloaded!.CreateString(&gameMessage, message);
                        rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(-1), new(), new(0), gameMessage, new(0)]);
                        this.logger!.PrintMessage(message, Color.Red);
                        break;
                    case ArchipelagoPacketType.Connected: // Get the options the user selected
                        var connected = (ConnectedPacket)packet;
                        slot = connected.Slot;
                        foreach (var option in connected.SlotData)
                        {
                            this.logger!.PrintMessage(option.Key + " " + option.Value, System.Drawing.Color.DarkOrange);
                            this.data!.SetValue<object>(DataContext.Options, option.Key, option.Value);
                        }
                        InventoryUtil.Instance.logger = this.logger;
                        InventoryUtil.Instance.GetOptions(data!);
                        break;
                    case ArchipelagoPacketType.ReceivedItems: // Actual printing message handled through OnMessageRecieved, but actual mod use of items will be handled here
                        var itemPacket = (ReceivedItemsPacket)packet;
                        InventoryUtil.Instance.ReceiveItem(itemPacket, data!);
                        // Maybe have a subscriber pattern here or in inventoryutil to invoke a method for each received item type
                        break;
                    case ArchipelagoPacketType.LocationInfo:
                    case ArchipelagoPacketType.RoomUpdate:
                        break;
                    case ArchipelagoPacketType.PrintJSON: // Handled through OnMessageRecieved, so will likely never use
                        break;
                    case ArchipelagoPacketType.DataPackage:
                        if (((DataPackagePacket)packet).DataPackage.Games.TryGetValue(GAME, out var gameData))
                        {
                            var itemId = gameData.ItemLookup;
                            foreach (var item in itemId)
                            {
                                this.data!.SetValue<string>(DataContext.IdToItem, item.Value, item.Key);
                            }
                        }
                        break;
                    case ArchipelagoPacketType.Bounced:
                    case ArchipelagoPacketType.InvalidPacket:
                    case ArchipelagoPacketType.Retrieved:
                    case ArchipelagoPacketType.SetReply:
                        break;
                }
            }
        }
    }
}
