using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RnSArchipelago.Connection;
using RnSArchipelago.Utils;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System.Collections.Generic;
using System.Linq;

namespace RnSArchipelago.Game
{
    internal unsafe class LocationHandler
    {
        private readonly IRNSReloaded rnsReloaded;
        private readonly ILoggerV1 logger;

        internal IHook<ScriptDelegate>? notchCompleteHook;
        internal IHook<ScriptDelegate>? chestOpenHook;

        internal IHook<ScriptDelegate>? setupItemsHook;
        internal IHook<ScriptDelegate>? enableModHook;
        internal IHook<ScriptDelegate>? itemGetHook;
        internal IHook<ScriptDelegate>? itemScoutHook;
        internal IHook<ScriptDelegate>? itemSetHook;
        internal IHook<ScriptDelegate>? itemSetDescriptionHook;
        internal IHook<ScriptDelegate>? takeItemHook;

        internal ArchipelagoConnection conn;
        private long baseItemId;

        private Random rand = new Random();

        private static readonly string GAME = "Rabbit and Steel";
        private static readonly string[] STARTING_LOCATIONS = ["Starting Class", "Starting Kingdom", "Starting Primary", "Starting Secondary", "Starting Special", "Starting Defensive"];
        private static readonly string[] CHEST_POSITIONS = ["Top Left", "Bottom Left", "Middle", "Bottom Right", "Top Right"];

        private Task<Dictionary<long, ScoutedItemInfo>> chestContents; 

        internal LocationHandler(IRNSReloaded rnsReloaded, ILoggerV1 logger)
        {
            this.rnsReloaded = rnsReloaded;
            this.logger = logger;
        }

        // Send the starting locations
        internal void SendStartLocation()
        {
            long[] locations = STARTING_LOCATIONS.Select(x => conn.session!.Locations.GetLocationIdFromName(GAME, x)).ToArray();
            var locationPacket = new LocationChecksPacket { Locations = locations };
            conn.session!.Socket.SendPacketAsync(locationPacket);
        }

        // Send the location for completing an encounter
        internal RValue* SendNotchComplete(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.notchCompleteHook!.OriginalFunction(self, other, returnValue, argc, argv);
            SendNotchLoctaion();
            return returnValue;
        }

        // Send the location for opening a chest
        internal RValue* SendChestOpen(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.chestOpenHook!.OriginalFunction(self, other, returnValue, argc, argv);
            SendNotchLoctaion();
            return returnValue;
        }

        // Update the archipelago items mod data
        internal RValue* SetupArchipelagoItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.setupItemsHook!.OriginalFunction(self, other, returnValue, argc, argv);

            var modInfo = rnsReloaded.utils.GetGlobalVar("modInfo");
            for (var i = 0; i < rnsReloaded.ArrayGetLength(modInfo)!.Value.Real; i++)
            {
                var entry = rnsReloaded.ArrayGetEntry(modInfo, i);
                if (rnsReloaded.ArrayGetEntry(entry, 0)->ToString() == "ArchipelagoItems") {
                    var name = new RValue();
                    rnsReloaded.CreateString(&name, "Archipelago Items");
                    *rnsReloaded.ArrayGetEntry(entry, 4) = name;

                    var tags = new RValue();
                    rnsReloaded.CreateString(&tags, "Loot Items,");
                    *rnsReloaded.ArrayGetEntry(entry, 5) = tags;

                    *rnsReloaded.ArrayGetEntry(entry, 8) = new(1); // Enabled
                    *rnsReloaded.ArrayGetEntry(entry, 10) = new(0); // 'Workshop'

                    break;
                }
            }

            return returnValue;
        }

        // After applying mod settings in game, ensure the archipelago items mod is enabled
        internal RValue* EnableArchipelagoItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.enableModHook!.OriginalFunction(self, other, returnValue, argc, argv);

            var modInfo = rnsReloaded.utils.GetGlobalVar("modInfo");
            for (var i = 0; i < rnsReloaded.ArrayGetLength(modInfo)!.Value.Real; i++)
            {
                var entry = rnsReloaded.ArrayGetEntry(modInfo, i);
                if (rnsReloaded.ArrayGetEntry(entry, 0)->ToString() == "ArchipelagoItems")
                {
                    *rnsReloaded.ArrayGetEntry(entry, 8) = new(1); // Enabled

                    return returnValue;
                }
            }

            return returnValue;
        }

        // Scout the network items in the chest ahead of time so once we need the results the task has finished
        internal RValue* ScoutItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            GetArchipelagoItemInfo();

            returnValue = this.itemScoutHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // Set the item inside the chest to the proper item
        internal RValue* SetItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            // If the item that is being created is a chest loot item
            if (argv[2]->Int32 == 1)
            {
                // Determine if the chest is an archipelago chest or not
                HookUtil.FindElementInLayer(rnsReloaded, "RunMenu_Blocker", "xSubimg", out var element);
                var instance = ((CLayerInstanceElement*)element)->Instance;
                var currentPos = rnsReloaded.FindValue(instance, "currentPos")->Real;
                var currentXImg = rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(instance, "xSubimg"), (int)currentPos);

                // Set the item to the archipelago item
                if (InventoryUtil.Instance.checksPerItemInChest && (currentXImg->Real == 1 || currentXImg->Int32 == 1))
                {
                    for (var i = 0; i < 5; i++)
                    {
                        // Determine which slot item we are at, which should be the first -1
                        if (rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "slots"), 1), i), 1)->Real == -1)
                        {
                            var info = chestContents.Result[GetChestPositionLocationId(SlotIdToChestPos(i))];

                            // If the location is checked
                            if (conn.session!.Locations.AllLocationsChecked.Contains(GetChestPositionLocationId(SlotIdToChestPos(i))))
                            {
                                *argv[0] = new RValue(baseItemId + 2);
                            } 
                            // If the item is progression
                            else if (info.Flags.HasFlag(ItemFlags.Advancement))
                            {
                                *argv[0] = new RValue(baseItemId + 1);
                            }
                            else
                            {
                                *argv[0] = new RValue(baseItemId);
                            }
                            break;
                        }
                    }
                }
                else if (InventoryUtil.Instance.shuffleItemsets)
                {
                    var index = rand.Next(InventoryUtil.Instance.AvailableItems.Count);
                    *argv[0] = new RValue(InventoryUtil.Instance.AvailableItems[index]);

                    this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "mod", self, returnValue, argc, argv), System.Drawing.Color.Red);
                }
            }

            returnValue = this.itemSetHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // Get the ingame item id for the first archipelago item
        internal RValue* GetItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (rnsReloaded.GetString(argv[0]).Contains("ArchipelagoItems"))
            {
                baseItemId = (long) rnsReloaded.FindValue(self, "item_data_entry_max")->Real + 1;
            }
            returnValue = this.itemGetHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // Set the description for archipelago items to reflect their actual item
        internal RValue* SetItemsDescription(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.itemSetDescriptionHook!.OriginalFunction(self, other, returnValue, argc, argv);
            
            //if an archipelago item, set the description to the real item
            if (argv[0]->Real == baseItemId || argv[0]->Real == baseItemId+1 || argv[0]->Real == baseItemId+2)
            {
                var info = chestContents.Result[GetChestPositionLocationId(SlotIdToChestPos((int)rnsReloaded.FindValue(self, "slotId")->Real))];
                var player = info.Player.Slot == MessageHandler.Instance.slot ? "your" : info.Player.Name + "'s";

                rnsReloaded.CreateString(returnValue, info.ItemDisplayName + " for " + player + " world");
            }

            return returnValue;
        }

        // Convert the ingame slotId to the archipelago chest location name suffix
        private string SlotIdToChestPos(int slotId)
        {
            switch (slotId)
            {
                case 0: return " Top Left";
                case 1: return " Bottom Left";
                case 2: return " Middle";
                case 3: return " Bottom Right";
                case 4: return " Top Right";
            }
            return "";
        }

        // Get the archipelago location id for the current chest's item at chestPos
        private long GetChestPositionLocationId(string chestPos)
        {
            return conn.session!.Locations.GetLocationIdFromName(GAME, GetBaseLocation() + chestPos);
        }

        // Scout all the items in the current chest
        private void GetArchipelagoItemInfo()
        {
            var locations = CHEST_POSITIONS.Select(x => conn.session!.Locations.GetLocationIdFromName(GAME, GetBaseLocation() + " " + x)).ToArray();

            chestContents = conn.session!.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, locations);
        }

        // Prevent 'fake' items from actually being taken
        internal RValue* TakeItem(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            var chestPos = argv[2]->Real;
            HookUtil.FindElementInLayer(rnsReloaded, "LootInfo", "slotId", chestPos + "", out var instance);
            if (instance != null)
            {
                var element = ((CLayerInstanceElement*)instance)->Instance;
                var itemId = rnsReloaded.FindValue(element, "itemId");
                if (itemId->Real != baseItemId && itemId->Real != baseItemId+1 && itemId->Real != baseItemId + 2)
                {
                    returnValue = this.takeItemHook!.OriginalFunction(self, other, returnValue, argc, argv);
                }

                var locationPacket = new LocationChecksPacket { Locations = [GetChestPositionLocationId(SlotIdToChestPos((int)rnsReloaded.FindValue(element, "slotId")->Real))] };
                conn.session!.Socket.SendPacketAsync(locationPacket);

            }
            else
            {
                returnValue = this.takeItemHook!.OriginalFunction(self, other, returnValue, argc, argv);
            }

            return returnValue;
        }

        // Send the location for finishing a notch if there is a generic location for it, i.e. battle, chest, or boss (not shop)
        internal void SendNotchLoctaion()
        {
            var baseLocation = GetBaseLocation();

            HookUtil.FindElementInLayer(rnsReloaded, "Ally", "allyId", out var instance);
            var element = ((CLayerInstanceElement*)instance)->Instance;
            var characterId = (int)rnsReloaded.FindValue(element, "allyId")->Real;
            var character = InventoryUtil.Instance.GetClass(characterId);

            long[] locations = [conn.session!.Locations.GetLocationIdFromName(GAME, baseLocation), conn.session!.Locations.GetLocationIdFromName(GAME, character + " " + baseLocation)];
            var locationPacket = new LocationChecksPacket { Locations = locations };
            conn.session!.Socket.SendPacketAsync(locationPacket);
        }

        // Get the base location name, ex. Kingdom Outskirts Chest 1
        private string GetBaseLocation()
        {
            HookUtil.FindElementInLayer(rnsReloaded, "RunMenu_Blocker", "currentPos", out var instance);
            var element = ((CLayerInstanceElement*)instance)->Instance;

            var kingdomName = rnsReloaded.FindValue(element, "stageName")->ToString();
            kingdomName = kingdomName.Replace(Environment.NewLine, " ");
            return kingdomName + GetNotchName(element);
        }

        // Get the name of the location for the notch based on its image and number of occurence
        private string GetNotchName(CInstance* element)
        {
            var notchPos = (int)rnsReloaded.FindValue(element, "currentPos")->Real;
            var notches = rnsReloaded.FindValue(element, "xSubimg");

            var notchType = rnsReloaded.ArrayGetEntry(notches, notchPos)->Real;

            if (notchType == 4)
            {
                return " Boss";
            }

            int count = 1;


            for (var i = 0; i < notchPos; i++)
            {
                if (rnsReloaded.ArrayGetEntry(notches, i)->Real == notchType)
                {
                    count++;
                }
            }

            if (notchType == 1)
            {
                return " Chest " + count;
            }

            if (notchType == 0)
            {
                return " Battle " + count;
            }

            return "NAN";
        }

        // Make the next notch be an ingame only chest
        private void AddChestToNotch()
        {
            HookUtil.FindElementInLayer(rnsReloaded, "RunMenu_Blocker", "xSubimg", out var element);
            var instance = ((CLayerInstanceElement*)element)->Instance;

            var currentPos = rnsReloaded.FindValue(instance, "currentPos")->Real;
            var notches = rnsReloaded.FindValue(instance, "notches");
            var emptyString = new RValue();
            rnsReloaded.CreateString(&emptyString, "");

            var notch = rnsReloaded.ExecuteCodeFunction("array_create", null, null, [new(4)])!.Value;
            *notch[0] = new(5);
            *notch[1] = emptyString;
            *notch[2] = new(0);
            *notch[3] = new(0);

            // Actually increase things
            rnsReloaded.ExecuteCodeFunction("array_insert", instance, null, [*notches, new RValue(currentPos+1), notch]);
            rnsReloaded.FindValue(instance, "notchNumber")->Real = rnsReloaded.FindValue(instance, "notchNumber")->Real + 1;
            rnsReloaded.ExecuteCodeFunction("array_insert", instance, null, [*rnsReloaded.FindValue(instance, "xSubimg"), new RValue(currentPos + 1), new(5)]);


        }
    }
}
