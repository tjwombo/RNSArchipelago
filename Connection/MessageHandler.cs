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
using Reloaded.Hooks.Definitions;
using System.Collections.Concurrent;

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

        internal IHook<ScriptDelegate>? addMessageHook;
        internal readonly ConcurrentQueue<LogMessage> messages = new();
        internal string errorMessage = "";

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
            this.logger?.PrintMessage("Unable to find rnsReloaded in MessageHandler", System.Drawing.Color.Red);
            rnsReloaded = null;
            return false;
        }

        internal void OnMessageReceived(LogMessage message)
        {
            switch (message)
            {
                case HintItemSendLogMessage hintLogMessage:
                    if (modConfig?.SystemLog ?? false)
                    {
                        messages.Enqueue(hintLogMessage);
                    }
                    logger?.PrintMessage(hintLogMessage.ToString(), System.Drawing.Color.Cyan);
                    break;
                case ItemSendLogMessage itemSendLogMessage:
                    if (modConfig != null &&
                        (modConfig.OtherLog || itemSendLogMessage.IsRelatedToActivePlayer) &&
                        ((modConfig.ProgressionLog && itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.Advancement)) ||
                        (modConfig.UsefulLog && itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.NeverExclude)) ||
                        (modConfig.FillerLog && !itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.Advancement) && !itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.NeverExclude) && !itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.Trap)) ||
                        (modConfig.TrapLog && itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.Trap))))
                    {
                        messages.Enqueue(itemSendLogMessage);
                    }
                    logger?.PrintMessage(itemSendLogMessage.ToString(), System.Drawing.Color.Cyan);
                    break;
                case PlayerSpecificLogMessage:
                case AdminCommandResultLogMessage:
                case CommandResultLogMessage:
                case CountdownLogMessage:
                case ServerChatLogMessage:
                case TutorialLogMessage:
                default:
                    if (modConfig?.SystemLog ?? false)
                    {
                        messages.Enqueue(message);
                    }
                    logger?.PrintMessage(message.ToString(), System.Drawing.Color.White);
                    break;
            }
        }

        internal void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            if (IsReady(out var rnsReloaded))
            {
                switch (packet.PacketType)
                {
                    case ArchipelagoPacketType.RoomInfo:
                        // Save the seed so we can have a static random
                        var room = (RoomInfoPacket)packet;
                        this.data?.SetValue<object>(DataContext.Options, "seed", room.SeedName);
                        break;
                    case ArchipelagoPacketType.ConnectionRefused:
                        var message = "Connection refused: " + string.Join(", ", ((ConnectionRefusedPacket)packet).Errors);
                        errorMessage = message;
                        this.logger?.PrintMessage(message, Color.Red);
                        
                        break;
                    case ArchipelagoPacketType.Connected: // Get the options the user selected
                        var connected = (ConnectedPacket)packet;
                        slot = connected.Slot;
                        foreach (var option in connected.SlotData)
                        {
                            this.logger?.PrintMessage(option.Key + " " + option.Value, System.Drawing.Color.DarkOrange);
                            this.data?.SetValue<object>(DataContext.Options, option.Key, option.Value);
                        }
                        InventoryUtil.Instance.logger = this.logger;
                        InventoryUtil.Instance.GetOptions();
                        break;
                    case ArchipelagoPacketType.ReceivedItems: // Actual printing message handled through OnMessageReceived, but actual mod use of items will be handled here
                        var itemPacket = (ReceivedItemsPacket)packet;
                        InventoryUtil.Instance.ReceiveItem(itemPacket);
                        // Maybe have a subscriber pattern here or in inventoryutil to invoke a method for each received item type
                        break;
                    case ArchipelagoPacketType.LocationInfo:
                    case ArchipelagoPacketType.RoomUpdate:
                        break;
                    case ArchipelagoPacketType.PrintJSON: // Handled through OnMessageReceived, so will likely never use
                        break;
                    case ArchipelagoPacketType.DataPackage:
                        if (((DataPackagePacket)packet).DataPackage.Games.TryGetValue(GAME, out var gameData))
                        {
                            var itemId = gameData.ItemLookup;
                            foreach (var item in itemId)
                            {
                                this.data?.SetValue<string>(DataContext.IdToItem, item.Value, item.Key);
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

        internal unsafe RValue* AddMessage(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (IsReady(out var rnsReloaded))
            {
                if (errorMessage != "")
                {
                    var message = new RValue();
                    rnsReloaded.CreateString(&message, errorMessage);
                    rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(-1), new(), new(0), message, new(0)]);

                    // Return to lobby in a safe thread if there was a connection error
                    if (errorMessage.StartsWith("Connection refused: "))
                    {
                        rnsReloaded.ExecuteScript("scr_runmenu_disband_disband", null, null, []);
                    }
                    errorMessage = "";
                }
                else if (messages.TryDequeue(out var message))
                {
                    var sourceId = -1;
                    var typedMessage = new RValue();

                    switch (message)
                    {
                        case ItemSendLogMessage itemSendLogMessage:
                            var messageToSend = itemSendLogMessage.ToString();
                            
                            if (itemSendLogMessage.IsSenderTheActivePlayer)
                            {
                                sourceId = 0;
                                messageToSend = messageToSend.Remove(0, messageToSend.IndexOf(" "));
                            }

                            rnsReloaded.CreateString(&typedMessage, messageToSend);

                            break;
                        default:
                            if (modConfig?.SystemLog ?? false)
                            {
                                rnsReloaded.CreateString(&typedMessage, message.ToString());
                            }
                            break;
                    }

                    rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(sourceId), new(), new(0), typedMessage, new(0)]);

                }
            }
            if (this.addMessageHook != null)
            {
                returnValue = this.addMessageHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger?.PrintMessage("Unable to call fix end icons hook", System.Drawing.Color.Red);
            }
            return returnValue;
        }
    }
}
