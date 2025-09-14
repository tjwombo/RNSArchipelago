using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Archipelago.MultiClient.Net;

namespace RnSArchipelago.Builder
{
    internal abstract class AMessageBuilder
    {
        internal abstract ArchipelagoPacketBase Build(ArchipelagoPacketBase packet);
    }
}
