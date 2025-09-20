/*//using RnSArchipelago.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Models;

namespace RnSArchipelago
{
    internal sealed class ArchipelagoConfig
    {
        internal static readonly ArchipelagoConfig Instance = new ArchipelagoConfig();
        private ArchipelagoConfig() { }


        private string name;
        private string address;
        private int numPlayers;
        private string password;

        internal Dictionary<long, NetworkPlayer> ids_to_players;
        private Dictionary<string, long> locations_to_ids;
        private Dictionary<string, long> items_to_ids;
        internal Dictionary<long, string> ids_to_locations;
        internal Dictionary<long, string> ids_to_items;
        internal Dictionary<long, NetworkSlot> ids_to_slot;
        internal long player_id;

        private static readonly string[] KINGDOMS = ["Kingdom Outskirts", "Scholar's Nest", "King's Arsenal", "Red Darkhouse", "Churchmouse Streets", "Emerald Lakeside", "The Pale Keep", "Moonlit Pinnacle"];


        internal void setConfig(string n, string a, int num, string p)
        {
            name = n;
            address = a;
            numPlayers = num;
            password = p;
        }

        internal (string name, string address, int numPlayers, string password) getConfig()
        {
            return (name, address, numPlayers, password);
        }

        internal void setLocations(Dictionary<string, long> locations)
        {
            locations_to_ids = locations;

            ids_to_locations = new Dictionary<long, string>();
            foreach (KeyValuePair<string, long> entry in locations)
            {
                ids_to_locations.Add(entry.Value, entry.Key);
            }
        }

        internal Dictionary<string, long> getLocations()
        {
            return locations_to_ids;
        }

        internal void setItems(Dictionary<string, long> items)
        {
            items_to_ids = items;

            ids_to_items = new Dictionary<long, string>();
            foreach (KeyValuePair<string, long> entry in items)
            {
                ids_to_items.Add(entry.Value, entry.Key);
            }
        }

        internal Dictionary<string, long> getItems()
        {
            return items_to_ids;
        }

        internal bool isItemKingdom(string itemName)
        {
            return KINGDOMS.Contains(itemName);
        }

    }
}
*/