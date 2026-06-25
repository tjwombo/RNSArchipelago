using System.Collections.Concurrent;
using System.Drawing;

using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using RnSArchipelago.Data;
using RnSArchipelago.Utils;

using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using static RnSArchipelago.Config.Config;

namespace RnSArchipelago.Connection
{
    internal class MessageHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly ILogger logger;
        private readonly InventoryUtil inventoryUtil;
        private readonly Config.Config modConfig;
        private readonly SharedData data;

        public MessageHandler(
            WeakReference<IRNSReloaded> rnsReloadedRef,
            ILogger logger,
            InventoryUtil inventoryUtil,
            Config.Config modConfig,
            SharedData data)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.logger = logger;
            this.inventoryUtil = inventoryUtil;
            this.modConfig = modConfig;
            this.data = data;
        }

        internal IHook<ScriptDelegate>? addMessageHook;
        internal readonly ConcurrentQueue<LogMessage> messages = new();
        internal string errorMessage = "";

        private static readonly string GAME = "Rabbit and Steel";
        internal int slot = 0;

        // Handle messages that are shown to the user
        internal void OnMessageReceived(LogMessage message)
        {
            switch (message)
            {
                case HintItemSendLogMessage hintLogMessage:
                    if (modConfig.HintConfig.HintLog && hintLogMessage.IsRelatedToActivePlayer && HintConfigIsOn(modConfig, hintLogMessage.Item.Flags))
                    {
                        messages.Enqueue(hintLogMessage);
                    }
                    logger.PrintMessage(hintLogMessage.ToString(), System.Drawing.Color.Cyan);
                    break;
                case ItemSendLogMessage itemSendLogMessage:
                    if (MessageConfigIsOn(itemSendLogMessage))
                    {
                        messages.Enqueue(itemSendLogMessage);
                    }
                    logger.PrintMessage(itemSendLogMessage.ToString(), System.Drawing.Color.Cyan);
                    break;
                case PlayerSpecificLogMessage:
                case AdminCommandResultLogMessage:
                case CommandResultLogMessage:
                case CountdownLogMessage:
                case ServerChatLogMessage:
                case TutorialLogMessage:
                default:
                    if (modConfig.MessageConfig.SystemLog)
                    {
                        messages.Enqueue(message);
                    }
                    logger.PrintMessage(message.ToString(), System.Drawing.Color.White);
                    break;
            }
        }

        // Use message configs to determine if the item should send a message in game
        internal static bool HintConfigIsOn(Config.Config modConfig, ItemFlags item)
        {
            return 
                (((modConfig.HintConfig.HintSetting & ItemType.Progressive) != 0) && item.HasFlag(ItemFlags.Advancement)) ||
                (((modConfig.HintConfig.HintSetting & ItemType.Progressive_Usefull) == ItemType.Progressive_Usefull) && item.HasFlag(ItemFlags.NeverExclude)) ||
                ((modConfig.HintConfig.HintSetting == ItemType.All) && (item.HasFlag(ItemFlags.None) || item.HasFlag(ItemFlags.Trap)));
        }

        // Use message configs to determine if the item should send a message in game
        internal bool MessageConfigIsOn(ItemSendLogMessage itemSendLogMessage)
        {
            return (modConfig.MessageConfig.OtherLog || itemSendLogMessage.IsRelatedToActivePlayer) &&
                ((((modConfig.MessageConfig.MessageSetting & ItemType.Progressive) != 0) && itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.Advancement)) ||
                (((modConfig.MessageConfig.MessageSetting & ItemType.Progressive_Usefull) == ItemType.Progressive_Usefull) && itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.NeverExclude)) ||
                ((modConfig.MessageConfig.MessageSetting == ItemType.All) && itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.None) && !itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.Trap)) ||
                (modConfig.MessageConfig.TrapLog && itemSendLogMessage.Item.Flags.HasFlag(ItemFlags.Trap)));
        }

        // Handle incomming packets
        internal void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.RoomInfo:
                    // Save the seed so we can have a static random
                    var room = (RoomInfoPacket)packet;
                    this.data.options.Set("seed", room.SeedName);

                    break;
                case ArchipelagoPacketType.ConnectionRefused:
                    var message = "Connection refused: " + string.Join(", ", ((ConnectionRefusedPacket)packet).Errors);
                    errorMessage = message;
                    this.logger.PrintMessage(message, Color.Red);

                    break;
                case ArchipelagoPacketType.Connected:
                    // Get the options the user selected
                    var connected = (ConnectedPacket)packet;
                    slot = connected.Slot;

                    foreach (var option in connected.SlotData)
                    {
                        this.logger.PrintMessage(option.Key + " " + option.Value, System.Drawing.Color.DarkOrange);
                        this.data.options.Set<object>(option.Key, option.Value);
                    }

                    this.inventoryUtil.GetOptions();

                    break;
                case ArchipelagoPacketType.ReceivedItems:
                    // Actual printing message handled through OnMessageReceived, but actual mod use of items will be handled here
                    var itemPacket = (ReceivedItemsPacket)packet;
                    this.inventoryUtil.ReceiveItem(itemPacket);

                    break;
                case ArchipelagoPacketType.LocationInfo:
                case ArchipelagoPacketType.RoomUpdate:
                case ArchipelagoPacketType.PrintJSON: // Handled through OnMessageReceived, so will likely never use
                    break;
                case ArchipelagoPacketType.DataPackage:
                    // Get the ids for the items
                    var dataPacket = (DataPackagePacket)packet;

                    if (dataPacket.DataPackage.Games.TryGetValue(GAME, out var gameData))
                    {
                        var itemId = gameData.ItemLookup;
                        foreach (var item in itemId)
                        {
                            this.data.idToItem.Set<string>(item.Value, item.Key);
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

        // Add a message in game
        internal unsafe RValue* AddMessage(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (errorMessage != "")
                {
                    var message = new RValue();
                    rnsReloaded.CreateString(&message, errorMessage);
                    rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(-1), new(), new(0), message, new(0)]);

                    // Return to lobby in a safe thread if there was a connection error
                    if (errorMessage.StartsWith("Connection refused: ") || errorMessage.StartsWith("Could not connect to the multiworld"))
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
                            if (modConfig.MessageConfig.SystemLog)
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
                this.logger.PrintMessage("Unable to call fix end icons hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }
    }
}