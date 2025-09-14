using RnSArchipelago.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }
}
