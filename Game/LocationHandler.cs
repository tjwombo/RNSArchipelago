using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

using RnSArchipelago.Connection;
using RnSArchipelago.Utils;

using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;

namespace RnSArchipelago.Game
{
    internal unsafe class LocationHandler
    {
        private readonly WeakReference<IRNSReloaded> rnsReloadedRef;
        private readonly Random rand;
        private readonly ILogger logger;
        private readonly InventoryHandler inventoryHandler;
        private readonly ShopItemsHandler shopItemsHandler;
        private readonly ScoutHandler scoutHandler;
        private readonly Config.Config modConfig;
        private readonly ArchipelagoConnection conn;

        internal IHook<ScriptDelegate>? notchCompleteHook;
        internal IHook<ScriptDelegate>? chestOpenHook;

        internal IHook<ScriptDelegate>? setupItemsHook;
        internal IHook<ScriptDelegate>? enableModHook;
        internal IHook<ScriptDelegate>? itemAmtHook;
        internal IHook<ScriptDelegate>? itemGetHook;
        internal IHook<ScriptDelegate>? itemSetHook;
        internal IHook<ScriptDelegate>? itemSetDescriptionHook;
        //internal IHook<ScriptDelegate>? itemSetUpgradeDescriptionHook;
        internal IHook<ScriptDelegate>? takeItemHook;
        internal IHook<ScriptDelegate>? RemoveAPItemTieHook;
        internal IHook<ScriptDelegate>? spawnTreasuresphereOnStartHook;
        internal IHook<ScriptDelegate>? readyCheckHook;

        internal IHook<ScriptDelegate>? openShopHook;
        internal IHook<ScriptDelegate>? closeShopHook;
        internal IHook<ScriptDelegate>? restockShopHook;

        private long baseItemId = -1;
        private int treasurespheresToSpawn = 0;

        internal static readonly string[] CHEST_POSITIONS = ["Top Left", "Bottom Left", "Middle", "Bottom Right", "Top Right"];
        internal static readonly string[] SHOP_POSITIONS = ["Full Heal Potion Slot", "Level Up Slot", "Potion 1 Slot", "Potion 2 Slot", "Potion 3 Slot",
                  "Primary Upgrade Slot", "Secondary Upgrade Slot", "Special Upgrade Slot", "Defensive Upgrade Slot"];

        internal LocationHandler(WeakReference<IRNSReloaded> rnsReloadedRef, Random rand, ILogger logger, InventoryHandler inventoryHandler, ShopItemsHandler shopItemsHandler, ScoutHandler scoutHandler, Config.Config modConfig, ArchipelagoConnection conn)
        {
            this.rnsReloadedRef = rnsReloadedRef;
            this.rand = rand;
            this.logger = logger;
            this.inventoryHandler = inventoryHandler;
            this.shopItemsHandler = shopItemsHandler;
            this.scoutHandler = scoutHandler;
            this.modConfig = modConfig;
            this.conn = conn;

            this.inventoryHandler.AddChest += AddChestToNotch;
            this.inventoryHandler.SendGoal += SendGoal;
        }

        // Send the location for completing an encounter
        internal RValue* SendNotchComplete(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.notchCompleteHook != null)
            {
                returnValue = this.notchCompleteHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call notch complete hook", System.Drawing.Color.Red);
            }

            if (this.inventoryHandler.isActive)
            {
                CompleteNotch();
            }
            return returnValue;
        }

        // Send the location for opening a chest
        internal RValue* SendChestOpen(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.chestOpenHook != null)
            {
                returnValue = this.chestOpenHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call chest open hook", System.Drawing.Color.Red);
            }

            if (this.inventoryHandler.isActive)
            {
                CompleteNotch();
            }

            return returnValue;
        }

        // Update the archipelago items mod data
        internal RValue* SetupArchipelagoItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.setupItemsHook != null)
            {
                returnValue = this.setupItemsHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call setup items hook", System.Drawing.Color.Red);
            }

            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                var modInfo = rnsReloaded.utils.GetGlobalVar("modInfo");
                var foundMod = false;
                var modInfoLength = modInfo->ArrayLength();
                for (var i = 0; i < modInfoLength; i++)
                {
                    var entry = modInfo->Get(i);
                    if (entry->Get(0)->ToString() == "ArchipelagoItems")
                    {
                        *entry->Get(4) = "Archipelago Items";

                        *entry->Get(5) = "Loot Items,";

                        *entry->Get(8) = 1; // Enabled
                        *entry->Get(10) = 0; // 'Workshop'

                        foundMod = true;

                        break;
                    }
                }

                if (!foundMod)
                {
                    this.logger.PrintMessage("Unable to find archipelago items mod", System.Drawing.Color.Red);
                }
            }

            return returnValue;
        }

        // After applying mod settings in game, ensure the archipelago items mod is enabled
        internal RValue* EnableArchipelagoItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.enableModHook != null)
            {
                returnValue = this.enableModHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call enable mod hook", System.Drawing.Color.Red);
            }

            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                var modInfo = rnsReloaded.utils.GetGlobalVar("modInfo");
                var modInfoLength = modInfo->ArrayLength();
                for (var i = 0; i < modInfoLength; i++)
                {
                    var entry = modInfo->Get(i);
                    if (entry->Get(0)->ToString() == "ArchipelagoItems")
                    {
                        *entry->Get(8) = 1; // Enabled

                        return returnValue;
                    }
                }
            }

            return returnValue;
        }

        // Set the amount of items in the chest to be 5
        internal RValue* SetAmountOfItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.itemAmtHook != null)
            {
                returnValue = this.itemAmtHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call item amount hook", System.Drawing.Color.Red);
            }
            if (this.inventoryHandler.isActive)
            {
                returnValue->Real = 5;
            }

            return returnValue;
        }

        // Get the item info of AP shop items
        internal void GetUnclaimedShopItems(int position, out ScoutedItemInfo? info, out long archipelagoItem, out bool useArchipelagoItem, out long id)
        {
            if (conn.session != null)
            {
                if (this.inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Global)
                {
                    id = conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, SHOP_POSITIONS[position]);
                    if (!conn.session.Locations.AllLocationsChecked.Contains(id))
                    {
                        info = scoutHandler.shopContents["global"][id];
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
                else if (this.inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Regional)
                {
                    id = conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, LocationUtil.GetBaseLocation() + " " + SHOP_POSITIONS[position]);
                    if (!conn.session.Locations.AllLocationsChecked.Contains(id))
                    {
                        info = scoutHandler.shopContents[LocationUtil.GetBaseLocation()][id];

                        if (info.Flags.HasFlag(ItemFlags.Advancement))
                        {
                            archipelagoItem = baseItemId + 1;
                        }
                        else
                        {
                            archipelagoItem = baseItemId;
                        }
                        useArchipelagoItem = true;
                        return;
                    }
                }
            }

            info = null;
            archipelagoItem = baseItemId;
            useArchipelagoItem = false;
            id = -1;
        }

        // Set the item inside the chest to the proper item
        internal RValue* SetItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (this.inventoryHandler.isActive)
                {
                    var location = LocationUtil.GetLocationType();

                    // If the item that is being created is a chest loot item
                    if (location == LocationUtil.LocationType.Chest)
                    {
                        // Set the item to the archipelago item
                        if (this.inventoryHandler.checksPerItemInChest)
                        {
                            for (var i = 0; i < 5; i++)
                            {
                                // Determine which slot item we are at, which should be the first -1
                                if (rnsReloaded.FindValue(self, "slots")->Get(1)->Get(i)->Get(1)->ToInt() == -1)
                                {
                                    var locationId = GetChestPositionLocationId(SlotIdToChestPos(i));
                                    var info = scoutHandler.chestContents[LocationUtil.GetBaseLocation()][locationId];

                                    if (conn.session != null && MessageHandler.HintConfigIsOn(modConfig, info.Flags))
                                    {
                                        conn.session.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, locationId);
                                    }

                                    // If the location is checked
                                    if (conn.session != null && conn.session.Locations.AllLocationsChecked.Contains(locationId))
                                    {
                                        *argv[0] = baseItemId + 2;
                                    }
                                    // If the item is progression
                                    else if (info.Flags.HasFlag(ItemFlags.Advancement))
                                    {
                                        *argv[0] = baseItemId + 1;
                                    }
                                    else
                                    {
                                        *argv[0] = baseItemId;
                                    }
                                    break;
                                }
                            }
                        }
                        else if (this.inventoryHandler.ItemSanity != InventoryHandler.ItemSetting.None)
                        {
                            if (this.inventoryHandler.AvailableItems.Count == 0)
                            {
                                *argv[0] = 0;
                            }
                            else
                            {
                                var index = this.rand.Next(this.inventoryHandler.AvailableItems.Count);
                                *argv[0] = this.inventoryHandler.AvailableItems[index];
                            }
                        }
                    }
                    // If the item that is being created is a shop loot item
                    else if (location == LocationUtil.LocationType.Shop)
                    {
                        // Determine which slot item we are at, which should be the first -1
                        for (var i = 0; i < 9; i++)
                        {
                            if (rnsReloaded.FindValue(self, "slots")->Get(2)->Get(i)->Get(1)->ToInt() == -1)
                            {
                                GetUnclaimedShopItems(i, out ScoutedItemInfo? info, out long archipelagoItem, out bool useArchipelagoItem, out long locationId);

                                if (conn.session != null && info != null && MessageHandler.HintConfigIsOn(modConfig, info.Flags))
                                {
                                    conn.session.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, locationId);
                                }

                                switch (i)
                                {
                                    case 0:
                                        this.shopItemsHandler.SetHpPotion(argv, archipelagoItem, useArchipelagoItem);
  
                                        break;
                                    case 1:
                                        this.shopItemsHandler.SetLevelPotion(argv, archipelagoItem, useArchipelagoItem);
 
                                        break;
                                    case 2:
                                    case 3:
                                    case 4:
                                        this.shopItemsHandler.SetPotion(argv, archipelagoItem, useArchipelagoItem);
    
                                        break;
                                    case 5:
                                        this.shopItemsHandler.SetPrimaryUpgrade(argv, archipelagoItem, useArchipelagoItem);

                                        break;
                                    case 6:
                                        this.shopItemsHandler.SetSecondaryUpgrade(argv, archipelagoItem, useArchipelagoItem);
  
                                        break;
                                    case 7:
                                        this.shopItemsHandler.SetSpecialUpgrade(argv, archipelagoItem, useArchipelagoItem);
  
                                        break;
                                    case 8:
                                        this.shopItemsHandler.SetDefensiveUpgrade(argv, archipelagoItem, useArchipelagoItem);

                                        break;
                                }

                                if (this.itemSetHook != null)
                                {
                                    returnValue = this.itemSetHook.OriginalFunction(self, other, returnValue, argc, argv);
                                }
                                else
                                {
                                    this.logger.PrintMessage("Unable to call item set hook", System.Drawing.Color.Red);
                                }

                                return returnValue;
                            }
                        }
                    }
                    else if (location == LocationUtil.LocationType.SpecialChest)
                    {
                        if (this.inventoryHandler.ItemSanity != InventoryHandler.ItemSetting.None)
                        {
                            if (this.inventoryHandler.AvailableItems.Count == 0)
                            {
                                *argv[0] = 0;
                            }
                            else
                            {
                                var index = rand.Next(this.inventoryHandler.AvailableItems.Count);
                                *argv[0] = this.inventoryHandler.AvailableItems[index];
                            }

                            // TODO: Trying to force the icon to show when its a chest after the intro room, but its not working (probably will fix itself if/when i can properly start the adventure at the beginning
                            //HookUtil.FindElementInLayer(rnsReloaded, "RunMenu_Blocker", "xSubimg", out var element);
                            //var instance = ((CLayerInstanceElement*)element)->Instance;
                            /*rnsReloaded.FindValue(instance, "yScale")->Real = 1;
                            rnsReloaded.FindValue(instance, "yScale")->Real = 1;*/

                            this.logger.PrintMessage(HookUtil.PrintHook("mod", self, returnValue, argc, argv), System.Drawing.Color.DarkOrange);
                        }
                    }
                }
            }

            if (this.itemSetHook != null)
            {
                returnValue = this.itemSetHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call item set hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }

        // Get the ingame item id for the first archipelago item
        internal RValue* GetItems(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (rnsReloaded.GetString(argv[0]).Contains("ArchipelagoItems"))
                {
                    baseItemId = rnsReloaded.FindValue(self, "item_data_entry_max")->ToLong() + 1;
                }
            }

            if (this.itemGetHook != null)
            {
                returnValue = this.itemGetHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call item get hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }

        // Set the description for archipelago items to reflect their actual item
        internal RValue* SetItemsDescription(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.itemSetDescriptionHook != null)
            {
                returnValue = this.itemSetDescriptionHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call item set description hook", System.Drawing.Color.Red);
            }

            if (this.inventoryHandler.isActive)
            {

                //if an archipelago item, set the description to the real item
                if (argv[0]->ToLong() == baseItemId || argv[0]->ToLong() == baseItemId + 1 || argv[0]->ToLong() == baseItemId + 2)
                {
                    ScoutedItemInfo? info = null;
                    //TODO: Look into making the better
                    RValue safeSelf = new(self);

                    if (this.inventoryHandler.checksPerItemInChest && LocationUtil.GetLocationType() == LocationUtil.LocationType.Chest)
                    {
                        info = scoutHandler.chestContents[LocationUtil.GetBaseLocation()][GetChestPositionLocationId(SlotIdToChestPos(safeSelf["slotId"]->ToInt()))];
                    }
                    else if (this.inventoryHandler.ShopSanity != InventoryHandler.ShopSetting.None && LocationUtil.GetLocationType() == LocationUtil.LocationType.Shop)
                    {
                        if (conn.session != null)
                        {
                            if (this.inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Global)
                            {
                                info = scoutHandler.shopContents["global"][conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, SHOP_POSITIONS[safeSelf["slotId"]->ToInt()])];
                            }
                            else if (this.inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Regional)
                            {
                                info = scoutHandler.shopContents[LocationUtil.GetBaseLocation()][conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, LocationUtil.GetBaseLocation() + " " + SHOP_POSITIONS[safeSelf["slotId"]->ToInt()])];
                            }
                        }
                    }

                    if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
                    {
                        if (info != null)
                        {
                            var player = info.Player.Slot == this.conn.messageHandler.slot ? "your" : info.Player.Name + "'s";

                            rnsReloaded.CreateString(returnValue, info.ItemDisplayName + " for " + player + " world");
                        }
                        else
                        {
                            rnsReloaded.CreateString(returnValue, "Unable to fetch archipelago item data");
                        }
                    }
                }
            }

            return returnValue;
        }

        // Set the description for archipelago items to reflect their actual item
        internal RValue* SetUpgradeDescription(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            /*if (this.itemSetUpgradeDescriptionHook != null)
            {
                returnValue = this.itemSetUpgradeDescriptionHook.OriginalFunction(self, other, returnValue, argc, argv);
            } else
            {
                this.logger.PrintMessage("Unable to call item set upgrade description hook", System.Drawing.Color.Red);
            }*/

            // TODO: probably should make a function that tells me what notch room type we are in
            var notchType = LocationUtil.GetLocationType();

            if (notchType == LocationUtil.LocationType.Shop)
            {
                //this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "shop", self, returnValue, argc, argv), System.Drawing.Color.DarkOrange);

                /*HookUtil.FindElementInLayer(rnsReloaded, "InventoryInfo", "itemId", out var layer);
                if (layer != null)
                {
                    var element = layer->Layer->Elements.First;

                    while (element != null)
                    {
                        var instance = (CInstance*)element;
                        var shopSlot = rnsReloaded.FindValue(instance, "slotId")->toLong();
                        RValue a = instance;
                        this.logger.PrintMessage(shopSlot + "", System.Drawing.Color.DarkOrange);
                        //this.logger.PrintMessage(shopSlot + " " + rnsReloaded.FindValue(instance, "slotId")->toLong() + " " + Marshal.PtrToStringAnsi((nint)element->Layer->Name), System.Drawing.Color.DarkOrange);
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
                //RValue a = new(self);
                //this.logger.PrintMessage(a.ToString(), System.Drawing.Color.DarkOrange);
                /*this.logger.PrintMessage(rnsReloaded.FindValue(self, "slotId")->toLong() + "", System.Drawing.Color.DarkOrange);
                var shopSlot = rnsReloaded.FindValue(self, "slotId")->toInt();
                if (shopSlot >= 5 && shopSlot <= 8)
                {
                    rnsReloaded.FindValue(self, "itemId")->Real = 94;

                }*/
            }
            //this.logger.PrintMessage(HookUtil.PrintHook(rnsReloaded, "uh", self, returnValue, argc, argv), System.Drawing.Color.DarkOrange);
            RValue a = new(self);
            this.logger.PrintMessage(a.ToString(), System.Drawing.Color.DarkOrange);
            /*if (this.itemSetUpgradeDescriptionHook != null)
            {
                returnValue = this.itemSetUpgradeDescriptionHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call item set upgrade description hook", System.Drawing.Color.Red);
            }*/
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
                default: return "";
            }
        }

        // Get the archipelago location id for the current chest's item at chestPos
        private long GetChestPositionLocationId(string chestPos)
        {
            if (conn.session != null)
            {
                return conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, LocationUtil.GetBaseLocation() + chestPos);
            }
            return -1;
        }

        // Remove AP items from the selection after the tie reroll
        internal RValue* RemoveAPItemTie(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                var step = rnsReloaded.FindValue(self, "step");
                var time = rnsReloaded.FindValue(self, "timect_current");
                if (step->ToInt() == 1 && time->ToInt() >= 1000) // TODO: Maybe find a better way to get the item to be redrawn at the end which is step 0, rather than remove it midway
                {
                    HookUtil.FindElementInLayer("InventoryItems", "slots", out var instance);

                    var playerCount = rnsReloaded.FindValue(self, "playerCount")->ToArray();
                    for (var i = 0; i < playerCount.Length; i++)
                    {
                        if (playerCount[i].ToInt() > 1)
                        {
                            *rnsReloaded.FindValue(((CLayerInstanceElement*)instance)->Instance, "slots")->Get(1)->Get(i)->Get(1) = -1;
                        }
                    }
                    this.logger.PrintMessage("done", System.Drawing.Color.Red);
                }
            }

            if (this.RemoveAPItemTieHook != null)
            {
                returnValue = this.RemoveAPItemTieHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call item tie hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }

        // Prevent 'fake' items from actually being taken
        internal RValue* TakeItem(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                
                var itemPos = argv[2]->ToLong();
                CLayerElementBase* instance = null;

                if (LocationUtil.GetLocationType() == LocationUtil.LocationType.Chest)
                {
                    HookUtil.FindElementInLayer("LootInfo", "slotId", itemPos + "", out instance);
                }
                else if (LocationUtil.GetLocationType() == LocationUtil.LocationType.Shop)
                {
                    HookUtil.FindElementInLayer("InventoryInfo", "slotId", itemPos + "", out instance);
                }

                if (instance != null)
                {
                    var element = ((CLayerInstanceElement*)instance)->Instance;
                    var itemId = rnsReloaded.FindValue(element, "itemId");

                    // Take the item if its not an ap item
                    if (itemId->ToLong() != baseItemId && itemId->ToLong() != baseItemId + 1 && itemId->ToLong() != baseItemId + 2)
                    {
                        if (this.takeItemHook != null)
                        {
                            returnValue = this.takeItemHook.OriginalFunction(self, other, returnValue, argc, argv);
                        }
                        else
                        {
                            this.logger.PrintMessage("Unable to call take item hook", System.Drawing.Color.Red);
                        }

                        return returnValue;
                    }

                    if (this.inventoryHandler.isActive)
                    {
                        // Send the AP item
                        if (this.inventoryHandler.checksPerItemInChest && LocationUtil.GetLocationType() == LocationUtil.LocationType.Chest)
                        {
                            var locationPacket = new LocationChecksPacket { Locations = [GetChestPositionLocationId(SlotIdToChestPos(rnsReloaded.FindValue(element, "slotId")->ToInt()))] };
                            if (conn.session != null)
                            {
                                if (conn.session.Locations.AllMissingLocations.Contains(locationPacket.Locations[0]))
                                {
                                    MessageHandler.messageId.Enqueue(argv[0]->ToLong());
                                }
                            }
                            conn.session?.Socket.SendPacketAsync(locationPacket);
                        }
                        else if (this.inventoryHandler.ShopSanity != InventoryHandler.ShopSetting.None && LocationUtil.GetLocationType() == LocationUtil.LocationType.Shop)
                        {
                            if (conn.session != null)
                            {
                                LocationChecksPacket locationPacket = new();
                                if (this.inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Global)
                                {
                                    locationPacket = new LocationChecksPacket { Locations = [conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, SHOP_POSITIONS[argv[2]->ToInt()])] };
                                    
                                }
                                else if (this.inventoryHandler.ShopSanity == InventoryHandler.ShopSetting.Regional)
                                {
                                    locationPacket = new LocationChecksPacket { Locations = [conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, LocationUtil.GetBaseLocation() + " " + SHOP_POSITIONS[argv[2]->ToInt()])] };
                                }

                                if (conn.session.Locations.AllMissingLocations.Contains(locationPacket.Locations[0]))
                                {
                                    MessageHandler.messageId.Enqueue(argv[0]->ToLong());
                                }
                                
                                conn.session?.Socket.SendPacketAsync(locationPacket);

                                // TODO: Wait until the location is sent to do this (has to be on the main thread)
                                // Set the item cache to -1, so we create the right type of item, and then place it
                                RValue items = new(self);
                                *items["slots"]->Get(2)->Get(argv[2]->ToInt())->Get(1) = -1;

                                // Make it so the item hasn't consumed their one per customer purchase
                                *items["storeSlotOPCBin"]->Get(argv[2]->ToInt()) = 0;

                            }

                            // TODO: Fix the width of item names
                            // TODO: Subtract user gold, and set price of AP items
                        }
                    }

                    return returnValue;
                }
            }

            if (this.takeItemHook != null)
            {
                returnValue = this.takeItemHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call take item hook", System.Drawing.Color.Red);
            }

            return returnValue;
        }

        // Activate the restock shop hook when the shop opens
        internal RValue* OpenShop(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.openShopHook != null)
            {
                returnValue = this.openShopHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call open shop hook", System.Drawing.Color.Red);
            }

            if (this.inventoryHandler.isActive)
            {
                restockShopHook!.Enable();
            }
            return returnValue;
        }

        // Deactivate the restock shop hook when the shop closes
        internal RValue* CloseShop(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.closeShopHook != null)
            {
                returnValue = this.closeShopHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call close shop hook", System.Drawing.Color.Red);
            }

            if (this.inventoryHandler.isActive)
            {
                restockShopHook!.Disable();
            }
            return returnValue;
        }

        // Creates and set a new item when an AP shop item was bought
        internal RValue* RestockShop(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.inventoryHandler.isActive)
            {
                if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
                {
                    HookUtil.FindElementInLayer("InventoryItems", "slots", out var element);
                    var instance = ((CLayerInstanceElement*)element)->Instance;
                    for (var i = 0; i < 9; i++)
                    {
                        if (rnsReloaded.FindValue(instance, "slots")->Get(2)->Get(i)->Get(1)->ToInt() == -1)
                        {
                            var newItemId = rnsReloaded.ExecuteScript("scr_itemsys_create_item", instance, other, [0, 0, 2]);
                            rnsReloaded.ExecuteScript("scr_itemsys_place_item_in_row", instance, other, [newItemId!.Value, 2]);
                            rnsReloaded.ExecuteScript("scr_itemsys_populate_loot_end", instance, other, []); // Makes it so the new item is highlightable
                        }
                    }
                }
            }
            if (this.restockShopHook != null)
            {
                returnValue = this.restockShopHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call restock shop hook", System.Drawing.Color.Red);
            }
            return returnValue;
        }

        // After 'completing' the notch, send the location and then spawn any AP treasurespheres
        internal void CompleteNotch()
        {
            SendNotchLoctaion();

            SpawnTreasuresphere();
        }

        // Send the location for finishing a notch if there is a generic location for it, i.e. battle, chest, or boss (not shop)
        internal void SendNotchLoctaion()
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                var baseLocation = LocationUtil.GetBaseLocation();

                var characters = HookUtil.GetClass();

                if (conn.session != null)
                {
                    long[] locations = [conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, baseLocation)];

                    if (conn.session.Locations.AllMissingLocations.Contains(locations[0]))
                    {
                        MessageHandler.messageId.Enqueue(0);
                    }

                    for (var i = 0; i < 4; i++)
                    {
                        if (characters[i] == null || characters[i].Equals(""))
                        {
                            continue;
                        }
                        var newLocation = conn.session.Locations.GetLocationIdFromName(ArchipelagoConnection.GAME, baseLocation + " - " + characters[i]);
                        locations = [.. locations, newLocation];
                        if (conn.session.Locations.AllMissingLocations.Contains(newLocation))
                        {
                            MessageHandler.messageId.Enqueue(i);
                        }
                    }
                    conn.session.Locations.CompleteLocationChecksAsync(locations);
                }
            }
        }

        // Make the next notch be an ingame only chest
        private void AddChestToNotch()
        {
            if (conn.isHost)
            {
                treasurespheresToSpawn++;
            }
        }

        // Spawn a treasuresphere on the next notch if there is any to spawn
        internal void SpawnTreasuresphere()
        {
            bool added = false;

            while (treasurespheresToSpawn > 0)
            {
                if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded) && conn.isHost)
                {
                    HookUtil.FindElementInLayer("RunMenu_Blocker", "xSubimg", out var element);
                    var instance = ((CLayerInstanceElement*)element)->Instance;

                    var currentPos = rnsReloaded.FindValue(instance, "currentPos")->ToLong();
                    if (currentPos == -1)
                    {
                        currentPos = 0;
                    }
                    var notches = rnsReloaded.FindValue(instance, "notches");
                    var notch = new RValue([5, "", 0, 0]);

                    // Actually increase things
                    rnsReloaded.ExecuteCodeFunction("array_insert", instance, null, [*notches, currentPos + 1, notch]);
                    var notchNumber = rnsReloaded.FindValue(instance, "notchNumber")->ToLong() + 1;
                    rnsReloaded.FindValue(instance, "notchNumber")->Real = notchNumber;

                    var subimg = rnsReloaded.FindValue(instance, "xSubimg");
                    if (subimg->ArrayLength() >= notchNumber)
                    {
                        for (int i = (int) notchNumber - 1; i > currentPos + 1; i--)
                        {
                            *subimg->Get(i) = *subimg->Get(i -1); 
                        }
                        *subimg->Get((int) currentPos + 1) = 5;
                    } else
                    {
                        rnsReloaded.ExecuteCodeFunction("array_insert", instance, null, [*rnsReloaded.FindValue(instance, "xSubimg"), currentPos + 1, 5]);
                    }
                    
                    treasurespheresToSpawn--;

                    added = true;
                } else if (!conn.isHost)
                {
                    // Clients shouldn't be populating their own treasuresphere notch
                    break;
                }
            }

            if (added)
            {
                conn.SendClientNotch();
            }
        }

        // Upon loading the lobby, reset the number of treasurespheres that are needed to spawn
        internal RValue* SpawnTreasuresphereOnStart(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            this.rnsReloadedRef.TryGetTarget(out var rnsReloaded);

            if (this.spawnTreasuresphereOnStartHook != null)
            {
                returnValue = this.spawnTreasuresphereOnStartHook.OriginalFunction(self, other, returnValue, argc, argv);
            }
            else
            {
                this.logger.PrintMessage("Unable to call spawn trasuresphere on start n hook", System.Drawing.Color.Red);
            }

            if (rnsReloaded != null)
            {
                if (conn.isHost)
                {
                    var kingdomName = rnsReloaded.FindValue(self, "stageName")->ToString();
                    kingdomName = kingdomName.Replace(Environment.NewLine, " ");

                    // If we are in the starting kingdom and generate the hallway, add the treasuresphere notches
                    if (kingdomName.Equals("Kingdom Outskirts") || kingdomName.Equals("Crack in the Geode"))
                    {
                        treasurespheresToSpawn = this.inventoryHandler.AvailableTreasurespheres;
                    }
                }
            }

            return returnValue;
        }

        // Remove the go to next screen loader if disconnected
        internal RValue* StopReadyCheck(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
        {
            if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded))
            {
                if (!this.inventoryHandler.isActive && rnsReloaded.utils.GetGlobalVar("obLobbyType")->ToInt() == 3)
                {
                    HookUtil.FindElementInLayer("PlayerField", "percent", out var fieldElement);
                    if (fieldElement != null)
                    {
                        RValue fieldInstance = ((CLayerInstanceElement*)fieldElement)->Instance;
                        *fieldInstance["percent"] = -0.1;
                    }
                }
            }

            if (this.readyCheckHook != null)
            {
                returnValue = this.readyCheckHook.OriginalFunction(self, other, returnValue, argc, argv);
            }

            return returnValue;
        }

        // Send the goal packet
        private void SendGoal()
        {
            conn.session?.SetGoalAchieved();
        }
    }
}