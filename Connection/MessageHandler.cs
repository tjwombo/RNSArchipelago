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

namespace RnSArchipelago.Connection
{
    internal class MessageHandler
    {

        private static readonly MessageHandler _instance = new MessageHandler();

        internal static MessageHandler Instance => _instance;

        private MessageHandler() { }

        internal IRNSReloaded? rnsReloaded;
        internal ILoggerV1? logger;
        internal Config.Config? modConfig;
        internal SharedData? data;

        private static readonly string GAME = "Rabbit and Steel";


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
                    rnsReloaded!.CreateString(&itemMessage, messageToSend);
                    rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(sourceId), new(), new(0), itemMessage, new(0)]);
                    logger!.PrintMessage(message.ToString(), System.Drawing.Color.Cyan);
                    break;
                case PlayerSpecificLogMessage playerLogMessage:
                    var playerMessage = new RValue();
                    rnsReloaded!.CreateString(&playerMessage, message.ToString());
                    rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(-1), new(0), new(0), playerMessage, new(0)]);
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

        internal unsafe void OnPacketReceived(ArchipelagoPacketBase packet)
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
                    foreach (var option in connected.SlotData)
                    {
                        Console.WriteLine(option.Key + " " + option.Value);
                        this.data!.SetValue<object>(DataContext.Options, option.Key, option.Value);
                    }
                    InventoryUtil.Instance.GetKingdomOptions(data!);
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
                case ArchipelagoPacketType.DataPackage: // Happens when we request it becuase we dont have it in the cache
                    if (((DataPackagePacket)packet).DataPackage.Games.TryGetValue(GAME, out var gameData))
                    {
                        var locationId = gameData.LocationLookup;
                        foreach (var location in locationId)
                        {
                            this.data!.SetValue<long>(DataContext.LocationToId, location.Key, location.Value);
                            this.data.SetValue<string>(DataContext.IdToLocation, location.Value, location.Key);
                        }
                        var itemId = gameData.ItemLookup;
                        foreach (var item in itemId)
                        {
                            this.data!.SetValue<long>(DataContext.ItemToId, item.Key, item.Value);
                            this.data.SetValue<string>(DataContext.IdToItem, item.Value, item.Key);
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
