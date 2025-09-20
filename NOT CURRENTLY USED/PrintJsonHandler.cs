/*using System.Text.Json.Serialization;
using System.Text.Json;
using RnSArchipelago.Utils.NetworkUtil;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Enums;
using System.Drawing;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Handler
{
    internal sealed class PrintJsonHandler : AMessageHandler
    {
        internal static readonly PrintJsonHandler Instance = new PrintJsonHandler();
        private PrintJsonHandler() { }

        internal unsafe override ArchipelagoPacketBase[]? Consume(ArchipelagoPacketBase obj, ILoggerV1 logger, IRNSReloaded rnsReloaded, Config.Config config)
        {
            var jsonInfo = (PrintJsonPacket)obj;
            logger.PrintMessage(jsonInfo.MessageType.ToString(), Color.Cyan);
            string fullMessage = "";
            int messageIcon = -1;
            int messageName = 0;
            foreach (var part in jsonInfo.Data)
            {
                switch (part.Type)
                {
                    case JsonMessagePartType.PlayerId:
                        if (ArchipelagoConfig.Instance.ids_to_players.TryGetValue(long.Parse(part.Text), out var player)) {
                            if (player.Slot == ArchipelagoConfig.Instance.player_id)
                            {
                                messageIcon = 0; // Can techinically be different if we're playing multiplayer
                            }
                            else
                            {
                                fullMessage += player.Alias;
                            }
                        }
                        break;
                    case JsonMessagePartType.ItemId:
                        if (ArchipelagoConfig.Instance.ids_to_items.TryGetValue(long.Parse(part.Text), out var item))
                        {
                            fullMessage += item;
                        }
                        break;
                    case JsonMessagePartType.LocationId:
                        if (ArchipelagoConfig.Instance.ids_to_locations.TryGetValue(long.Parse(part.Text), out var location))
                        {
                            if (location.Contains("Starting") && messageIcon == 0)
                            {
                                messageName = -1000;
                            }
                            fullMessage += location;
                        }
                        break;
                    default:
                        fullMessage += part.Text;
                        break;
                }
                Console.WriteLine(part.Text + " " + part.Type + " p" + part.Flags);
                
            }
            RValue message = new RValue();
            switch (jsonInfo.MessageType)
            {
                case JsonMessageType.AdminCommandResult:
                    break;
                case JsonMessageType.Chat:
                    break;
                case JsonMessageType.Collect:
                    break;
                case JsonMessageType.CommandResult:
                    break;
                case JsonMessageType.Countdown:
                    break;
                case JsonMessageType.Goal:
                    break;
                case JsonMessageType.Hint:
                    break;
                case JsonMessageType.ItemCheat:
                    break;
                case JsonMessageType.ItemSend:
                    rnsReloaded.CreateString(&message, fullMessage.Trim());
                    rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(messageIcon), new(messageName), new(0), message, new(0)]);
                    logger.PrintMessage(fullMessage, Color.Cyan);
                    break;
                case JsonMessageType.Join:
                    rnsReloaded.CreateString(&message, fullMessage);
                    rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(-1), new(0), new(0), message, new(0)]);
                    logger.PrintMessage(fullMessage, Color.White);
                    var locations = new LocationChecksPacket
                    {
                        Locations = [1, 2, 3, 4, 5]
                    };
                    return [locations];
                case JsonMessageType.Part:
                    break;
                case JsonMessageType.Release:
                    break;
                case JsonMessageType.ServerChat:
                    break;
                case JsonMessageType.TagsChanged:
                    break;
                case JsonMessageType.Tutorial:
                    rnsReloaded.CreateString(&message, fullMessage);
                    rnsReloaded.ExecuteScript("scr_chat_add_message", null, null, [new RValue(-1), new(0), new(0), message, new(0)]);
                    logger.PrintMessage(fullMessage, Color.White);
                    break;
            }
            return null;
        }
    }
}
*/