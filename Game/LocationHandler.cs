using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces.Internal;
using RnSArchipelago.Connection;
using RnSArchipelago.Utils;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using System.Xml.Linq;

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
        internal IHook<ScriptDelegate>? itemAmtHook;
        internal IHook<ScriptDelegate>? itemGetHook;
        internal IHook<ScriptDelegate>? itemScoutChestHook;
        internal IHook<ScriptDelegate>? itemScoutShopHook;
        internal IHook<ScriptDelegate>? itemSetHook;
        internal IHook<ScriptDelegate>? itemSetDescriptionHook;
        internal IHook<ScriptDelegate>? itemSetUpgradeDescriptionHook;
        internal IHook<ScriptDelegate>? takeItemHook;
        internal IHook<ScriptDelegate>? spawnTreasuresphereOnStartNHook;
        internal IHook<ScriptDelegate>? spawnTreasuresphereOnStartHook;

        internal ArchipelagoConnection conn = null!;
        private long baseItemId;

        private Random rand = new Random();

        private static readonly string GAME = "Rabbit and Steel";
        private static readonly string[] STARTING_LOCATIONS = ["Starting Class", "Starting Kingdom", "Starting Primary", "Starting Secondary", "Starting Special", "Starting Defensive"];
        private static readonly string[] CHEST_POSITIONS = ["Top Left", "Bottom Left", "Middle", "Bottom Right", "Top Right"];
        private static readonly string[] SHOP_POSITIONS = ["Full Heal Potion Slot", "Level Up Slot", "Potion 1 Slot", "Potion 2 Slot", "Potion 3 Slot",
                  "Primary Upgrade Slot", "Secondary Upgrade Slot", "Special Upgrade Slot", "Defensive Upgrade Slot"];

        private Task<Dictionary<long, ScoutedItemInfo>> chestContents = null!;
        private Task<Dictionary<long, ScoutedItemInfo>> shopContents = null!;

        internal LocationHandler(IRNSReloaded rnsReloaded, ILoggerV1 logger)
        {
            this.rnsReloaded = rnsReloaded;
            this.logger = logger;

            InventoryUtil.Instance.AddChest += AddChestToNotch;
            InventoryUtil.Instance.SendGoal += SendGoal;
        }

        private enum LocationType
        {
            Other,
            Start,
            Battle,
            Boss,
            Chest,
            Shop
        }

        // Get the location type of the provided notch index, default is current notch
        private LocationType GetLocationType(int index = -1)
        {
            HookUtil.FindElementInLayer(rnsReloaded, "RunMenu_Blocker", "xSubimg", out var element);
            var instance = ((CLayerInstanceElement*)element)->Instance;
            if (index == -1)
            {
                index = (int)HookUtil.GetNumeric(rnsReloaded.FindValue(instance, "currentPos"));
            }
            var currentXImg = rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(instance, "xSubimg"), index);

            if (HookUtil.IsEqualToNumeric(currentXImg, 1)) {
                return LocationType.Chest;
            }
            else if (HookUtil.IsEqualToNumeric(currentXImg, 2))
            {
                return LocationType.Shop;
            }
            else if (HookUtil.IsEqualToNumeric(currentXImg, 4))
            {
                return LocationType.Boss;
            }
            else if (HookUtil.IsEqualToNumeric(currentXImg, 0))
            {
                return LocationType.Battle;
            }
            return LocationType.Other;
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
            if (InventoryUtil.Instance.isActive)
            {
                SendNotchLoctaion();
            }
            return returnValue;
        }

        // Send the location for opening a chest
        internal RValue* SendChestOpen(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.chestOpenHook!.OriginalFunction(self, other, returnValue, argc, argv);
            if (InventoryUtil.Instance.isActive)
            {
                SendNotchLoctaion();
            }
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
        internal RValue* ScoutChestItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (InventoryUtil.Instance.isActive)
            {
                GetArchipelagoChestItemInfo();
            }
            returnValue = this.itemScoutChestHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // Scout the network items in the shop ahead of time so once we need the results the task has finished
        internal RValue* ScoutShopItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (InventoryUtil.Instance.isActive)
            {
                GetArchipelagoShopItemInfo();
            }
            returnValue = this.itemScoutShopHook!.OriginalFunction(self, other, returnValue, argc, argv);

            var instance = new RValue(self);
            long id = -1;
            for (var j = 0; j < 9; j++)
            {
                id = conn.session!.Locations.GetLocationIdFromName(GAME, SHOP_POSITIONS[j]);

                // if the item is an archipelago item, disable the purchase condition, mainly applies to hp and upgrades
                if (!conn.session!.Locations.AllLocationsChecked.Contains(id))
                {
                    *rnsReloaded.ArrayGetEntry(instance["storeSlotHeal"], j) = new RValue(0);
                    *rnsReloaded.ArrayGetEntry(instance["storeSlotUpgrade"], j) = new RValue(0);
                }
            }

            return returnValue;
        }

        //TODO: Actually determine what amount it should be
        // Set the amount of items in the chest to corrospond to the amount it should be
        internal RValue* SetAmountOfItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.itemAmtHook!.OriginalFunction(self, other, returnValue, argc, argv);
            if (InventoryUtil.Instance.isActive)
            {
                returnValue->Real = 5;
            }
            return returnValue;
        }

        internal void GetUnclaimedShopItems(int position, out ScoutedItemInfo? info, out long archipelagoItem, out bool useArchipelagoItem)
        {
            if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Global)
            {
                long id = conn.session!.Locations.GetLocationIdFromName(GAME, SHOP_POSITIONS[position]);
                if (!conn.session!.Locations.AllLocationsChecked.Contains(id))
                {
                    info = shopContents.Result[id];
                    if (info.Flags.HasFlag(ItemFlags.Advancement))
                    {
                        archipelagoItem = baseItemId + 1;
                        useArchipelagoItem = true;
                    }
                    else
                    {
                        archipelagoItem = baseItemId;
                        useArchipelagoItem = true;
                    }
                    return;
                }
            }
            else if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Regional)
            {
                long id = conn.session!.Locations.GetLocationIdFromName(GAME, GetBaseLocation() + " " + SHOP_POSITIONS[position]);
                if (!conn.session!.Locations.AllLocationsChecked.Contains(id))
                {
                    info = shopContents.Result[id];

                    if (info.Flags.HasFlag(ItemFlags.Advancement))
                    {
                        archipelagoItem = baseItemId + 1;
                        useArchipelagoItem = true;
                    }
                    else
                    {
                        archipelagoItem = baseItemId;
                        useArchipelagoItem = true;
                    }
                    return;
                }
            }

            info = null;
            archipelagoItem = baseItemId;
            useArchipelagoItem = false;
        }

        // Set the item inside the chest to the proper item
        internal RValue* SetItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (InventoryUtil.Instance.isActive)
            {
                // If the item that is being created is a chest loot item
                if (GetLocationType() == LocationType.Chest)
                {
                    // Set the item to the archipelago item
                    if (InventoryUtil.Instance.checksPerItemInChest)
                    {
                        for (var i = 0; i < 5; i++)
                        {
                            // Determine which slot item we are at, which should be the first -1
                            if (HookUtil.IsEqualToNumeric(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "slots"), 1), i), 1), -1))
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

                        // TODO: Trying to force the icon to show when its a chest after the intro room, but its not working
                        //HookUtil.FindElementInLayer(rnsReloaded, "RunMenu_Blocker", "xSubimg", out var element);
                        //var instance = ((CLayerInstanceElement*)element)->Instance;
                        /*rnsReloaded.FindValue(instance, "yScale")->Real = 1;
                        rnsReloaded.FindValue(instance, "yScale")->Real = 1;*/

                        this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "mod", self, returnValue, argc, argv), System.Drawing.Color.Red);
                    }
                }
                // If the item that is being created is a shop loot item
                else if (GetLocationType() == LocationType.Shop)
                {
                    // Determine which slot item we are at, which should be the first -1
                    for (var i = 0; i < 9; i++)
                    {
                        if (HookUtil.IsEqualToNumeric(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.FindValue(self, "slots"), 2), i), 1), -1))
                        {
                            GetUnclaimedShopItems(i, out ScoutedItemInfo? info, out long archipelagoItem, out bool useArchipelagoItem);
                            
                            switch (i)
                            {
                                case 0:
                                    ShopItemsUtil.SetHpPotion(argv, archipelagoItem, useArchipelagoItem);
                                    returnValue = this.itemSetHook!.OriginalFunction(self, other, returnValue, argc, argv);
                                    break;
                                case 1:
                                    ShopItemsUtil.SetLevelPotion(argv, archipelagoItem, useArchipelagoItem);
                                    returnValue = this.itemSetHook!.OriginalFunction(self, other, returnValue, argc, argv);
                                    break;
                                case 2:
                                case 3:
                                case 4:
                                    ShopItemsUtil.SetPotion(argv, archipelagoItem, useArchipelagoItem);
                                    returnValue = this.itemSetHook!.OriginalFunction(self, other, returnValue, argc, argv);
                                    break;
                                case 5:
                                    ShopItemsUtil.SetPrimaryUpgrade(argv, archipelagoItem, useArchipelagoItem);
                                    returnValue = this.itemSetHook!.OriginalFunction(self, other, returnValue, argc, argv);
                                    break;
                                case 6:
                                    ShopItemsUtil.SetSecondaryUpgrade(argv, archipelagoItem, useArchipelagoItem);
                                    returnValue = this.itemSetHook!.OriginalFunction(self, other, returnValue, argc, argv);
                                    break;
                                case 7:
                                    ShopItemsUtil.SetSpecialUpgrade(argv, archipelagoItem, useArchipelagoItem);
                                    returnValue = this.itemSetHook!.OriginalFunction(self, other, returnValue, argc, argv);
                                    break;
                                case 8:
                                    ShopItemsUtil.SetDefensiveUpgrade(argv, archipelagoItem, useArchipelagoItem);
                                    returnValue = this.itemSetHook!.OriginalFunction(self, other, returnValue, argc, argv);
                                    break;
                            }

                            return returnValue;
                        }
                    }
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
                baseItemId = HookUtil.GetNumeric(rnsReloaded.FindValue(self, "item_data_entry_max")) + 1;
            }
            returnValue = this.itemGetHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        }

        // Set the description for archipelago items to reflect their actual item
        internal RValue* SetItemsDescription(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.itemSetDescriptionHook!.OriginalFunction(self, other, returnValue, argc, argv);

            if (InventoryUtil.Instance.isActive)
            {
                //if an archipelago item, set the description to the real item
                if (HookUtil.IsEqualToNumeric(argv[0], baseItemId) || HookUtil.IsEqualToNumeric(argv[0], baseItemId + 1) || HookUtil.IsEqualToNumeric(argv[0], baseItemId + 2))
                {
                    ScoutedItemInfo? info = null;
                    //TODO: Look into making the better
                    var safeSelf = new RValue(self);

                    if (InventoryUtil.Instance.checksPerItemInChest && GetLocationType() == LocationType.Chest)
                    {
                        info = chestContents.Result[GetChestPositionLocationId(SlotIdToChestPos((int)HookUtil.GetNumeric(safeSelf["slotId"])))];
                    }
                    else if (InventoryUtil.Instance.ShopSanity != InventoryUtil.ShopSetting.None && GetLocationType() == LocationType.Shop)
                    {
                        if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Global)
                        {
                            info = shopContents.Result[conn.session!.Locations.GetLocationIdFromName(GAME, SHOP_POSITIONS[(int)HookUtil.GetNumeric(safeSelf["slotId"])])];
                        }
                        else if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Regional)
                        {
                            info = shopContents.Result[conn.session!.Locations.GetLocationIdFromName(GAME, GetBaseLocation() + " " + SHOP_POSITIONS[(int)HookUtil.GetNumeric(safeSelf["slotId"])])];
                        }
                    }


                    if (info != null)
                    {
                        var player = info.Player.Slot == MessageHandler.Instance.slot ? "your" : info.Player.Name + "'s";

                        rnsReloaded.CreateString(returnValue, info.ItemDisplayName + " for " + player + " world");
                    }
                    else
                    {
                        rnsReloaded.CreateString(returnValue, "Unable to fetch archipelago item data");
                    }
                }
            }

            return returnValue;
        }

        // Set the description for archipelago items to reflect their actual item
        internal RValue* SetUpgradeDescription(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.itemSetUpgradeDescriptionHook!.OriginalFunction(self, other, returnValue, argc, argv);

            // TODO: probably should make a function that tells me what notch room type we are in
            var notchType = GetLocationType();

            if (notchType == LocationType.Shop)
            {
                //this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "shop", self, returnValue, argc, argv), System.Drawing.Color.Red);

                /*HookUtil.FindElementInLayer(rnsReloaded, "InventoryInfo", "itemId", out var layer);
                if (layer != null)
                {
                    var element = layer->Layer->Elements.First;

                    while (element != null)
                    {
                        var instance = (CInstance*)element;
                        var shopSlot = HookUtil.GetNumeric(rnsReloaded.FindValue(instance, "slotId"));
                        var a = new RValue(instance);
                        this.logger.PrintMessage(shopSlot + "", System.Drawing.Color.Red);
                        //this.logger.PrintMessage(shopSlot + " " + HookUtil.GetNumeric(rnsReloaded.FindValue(instance, "slotId")) + " " + Marshal.PtrToStringAnsi((nint)element->Layer->Name), System.Drawing.Color.Red);
                        if (shopSlot >= 5 && shopSlot <= 8)
                        {
                            rnsReloaded.FindValue(instance, "itemId")->Real = 94;

                        }
                        // if 5 <= slotId <= 8

                        // itemId = 94


                        element = element->Next;
                        return returnValue;
                    }
                }*/
                //var a = new RValue(self);
                //this.logger.PrintMessage(a.ToString(), System.Drawing.Color.Red);
                /*this.logger.PrintMessage(HookUtil.GetNumeric(rnsReloaded.FindValue(self, "slotId")) + "", System.Drawing.Color.Red);
                var shopSlot = (int)HookUtil.GetNumeric(rnsReloaded.FindValue(self, "slotId"));
                if (shopSlot >= 5 && shopSlot <= 8)
                {
                    rnsReloaded.FindValue(self, "itemId")->Real = 94;

                }*/
            }
            //this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "uh", self, returnValue, argc, argv), System.Drawing.Color.Red);
            var a = new RValue(self);
            this.logger.PrintMessage(a.ToString(), System.Drawing.Color.Red);
            //returnValue = this.itemSetUpgradeDescriptionHook!.OriginalFunction(self, other, returnValue, argc, argv);
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
        private void GetArchipelagoChestItemInfo()
        {
            var locations = CHEST_POSITIONS.Select(x => conn.session!.Locations.GetLocationIdFromName(GAME, GetBaseLocation() + " " + x)).ToArray();

            chestContents = conn.session!.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, locations);
        }

        // Scout all the items in the current shop
        private void GetArchipelagoShopItemInfo()
        {
            long[] locations = [];

            if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Global)
            {
                locations = SHOP_POSITIONS.Select(x => conn.session!.Locations.GetLocationIdFromName(GAME, x)).ToArray();
            }
            else if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Regional)
            {
                locations = SHOP_POSITIONS.Select(x => conn.session!.Locations.GetLocationIdFromName(GAME, GetBaseLocation() + " " + x)).ToArray();
            }

            shopContents = conn.session!.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, locations);
            this.logger.PrintMessage("shop pos id: " + string.Join(", ", shopContents.Result.Select(pair => $"{pair.Key} => {pair.Value.ItemName}\n")), System.Drawing.Color.Red);
        }

        // Prevent 'fake' items from actually being taken
        internal RValue* TakeItem(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (InventoryUtil.Instance.isActive)
            {
                var chestPos = HookUtil.GetNumeric(argv[2]);
                CLayerElementBase* instance = null;
                if (GetLocationType() == LocationType.Chest)
                {
                    HookUtil.FindElementInLayer(rnsReloaded, "LootInfo", "slotId", chestPos + "", out instance);
                }
                else if (GetLocationType() == LocationType.Shop)
                {
                    HookUtil.FindElementInLayer(rnsReloaded, "InventoryInfo", "slotId", chestPos + "", out instance);
                }
                if (instance != null)
                {
                    var element = ((CLayerInstanceElement*)instance)->Instance;
                    var itemId = rnsReloaded.FindValue(element, "itemId");

                    if (!HookUtil.IsEqualToNumeric(itemId, baseItemId) && !HookUtil.IsEqualToNumeric(itemId, baseItemId + 1) && !HookUtil.IsEqualToNumeric(itemId, baseItemId + 2))
                    {
                        returnValue = this.takeItemHook!.OriginalFunction(self, other, returnValue, argc, argv);
                        return returnValue;
                    }

                    this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "take", self, returnValue, argc, argv), System.Drawing.Color.Red);

                    if (InventoryUtil.Instance.checksPerItemInChest && GetLocationType() == LocationType.Chest)
                    {
                        var locationPacket = new LocationChecksPacket { Locations = [GetChestPositionLocationId(SlotIdToChestPos((int)HookUtil.GetNumeric(rnsReloaded.FindValue(element, "slotId"))))] };
                        conn.session!.Socket.SendPacketAsync(locationPacket);
                    }
                    else if (InventoryUtil.Instance.ShopSanity != InventoryUtil.ShopSetting.None && GetLocationType() == LocationType.Shop)
                    {
                        if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Global)
                        {
                            var locationPacket = new LocationChecksPacket { Locations = [conn.session!.Locations.GetLocationIdFromName(GAME, SHOP_POSITIONS[(int)HookUtil.GetNumeric(argv[2])])] };
                            conn.session!.Socket.SendPacketAsync(locationPacket);
                        }
                        else if (InventoryUtil.Instance.ShopSanity == InventoryUtil.ShopSetting.Regional)
                        {
                            var locationPacket = new LocationChecksPacket { Locations = [conn.session!.Locations.GetLocationIdFromName(GAME, GetBaseLocation() + " " + SHOP_POSITIONS[(int)HookUtil.GetNumeric(argv[2])])] };
                            conn.session!.Socket.SendPacketAsync(locationPacket);
                        }

                        var slots = new RValue(self);

                        // Set the item cache to -1, so we repopulate it
                        *rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(rnsReloaded.ArrayGetEntry(slots["slots"], 2), (int)HookUtil.GetNumeric(argv[2])), 1) = new RValue(-1);

                        rnsReloaded.ExecuteScript("scr_itemsys_populate_store", self, other, [new RValue(0)]);

                        // TODO: Fix the width of item names
                        // TODO: Subtract user gold, and set price of AP items
                    }

                }
                else
                {
                    returnValue = this.takeItemHook!.OriginalFunction(self, other, returnValue, argc, argv);
                }
            } else
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
            var characterId = (int)HookUtil.GetNumeric(rnsReloaded.FindValue(element, "allyId"));
            var character = InventoryUtil.Instance.GetClass(characterId);

            long[] locations = [conn.session!.Locations.GetLocationIdFromName(GAME, baseLocation), conn.session!.Locations.GetLocationIdFromName(GAME,baseLocation + " - " + character)];
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

            var notchName = GetNotchName(element);
            if(notchName.Contains("Chest") && !kingdomName.Equals("Kingdom Outskirts"))
            {
                notchName = " Chest";
            } else if (kingdomName.Equals("Moonlit Pinnacle"))
            {
                return "Shira";
            }
            return kingdomName + notchName;
        }

        // Get the name of the location for the notch based on its image and number of occurence
        private string GetNotchName(CInstance* element)
        {
            var notchPos = (int)HookUtil.GetNumeric(rnsReloaded.FindValue(element, "currentPos"));
            var notchType = GetLocationType();

            if (notchType == LocationType.Boss)
            {
                return " Boss";
            }

            int count = 1;

            var kingdomName = rnsReloaded.FindValue(element, "stageName")->ToString();
            kingdomName = kingdomName.Replace(Environment.NewLine, " ");


            for (var i = kingdomName.Equals("Kingdom Outskirts") ? 1 : 0; i < notchPos; i++)
            {
                if (GetLocationType(i) == notchType)
                {
                    count++;
                }
            }

            if (notchType == LocationType.Chest)
            {
                return " Chest " + count;
            }

            if (notchType == LocationType.Battle)
            {
                return " Battle " + count;
            }

            if (notchType == LocationType.Shop)
            {
                return " Shop";
            }

            return "";
        }

        // Make the next notch be an ingame only chest
        private void AddChestToNotch()
        {
            HookUtil.FindElementInLayer(rnsReloaded, "RunMenu_Blocker", "xSubimg", out var element);
            var instance = ((CLayerInstanceElement*)element)->Instance;

            var currentPos = HookUtil.GetNumeric(rnsReloaded.FindValue(instance, "currentPos"));
            if (currentPos == -1)
            {
                currentPos = 0;
            }
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
            rnsReloaded.FindValue(instance, "notchNumber")->Real = HookUtil.GetNumeric(rnsReloaded.FindValue(instance, "notchNumber")) + 1;
            rnsReloaded.ExecuteCodeFunction("array_insert", instance, null, [*rnsReloaded.FindValue(instance, "xSubimg"), new RValue(currentPos + 1), new(5)]);


        }

        // On outskirts loading, besides loading into lobby, add the treasurespheres we have accumulated
        internal RValue* SpawnTreasuresphereOnStart(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            returnValue = this.spawnTreasuresphereOnStartNHook!.OriginalFunction(self, other, returnValue, argc, argv);

            var kingdomName = rnsReloaded.FindValue(self, "stageName")->ToString();
            kingdomName = kingdomName.Replace(Environment.NewLine, " ");

            if (kingdomName.Equals("Kingdom Outskirts"))
            {
                for (int i = 0; i < InventoryUtil.Instance.AvailableTreasurespheres; i++)
                {
                    AddChestToNotch();
                }
            }
            
            return returnValue;
        }

        private void SendGoal()
        {
            conn.session!.SetGoalAchieved();
        }
    }
}
