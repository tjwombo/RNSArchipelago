using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RnSArchipelago.Builder;
using Archipelago.MultiClient.Net;

namespace RnSArchipelago.Dispatcher
{
    internal sealed class ClientCommandDispatcher
    {
        private static readonly ClientCommandDispatcher _instance = new ClientCommandDispatcher();
        private readonly Dictionary<string, AMessageBuilder> _builderMap = new();

        private ClientCommandDispatcher()
        {
            _builderMap["Connect"] = ConnectBuilder.Instance;
        }

        internal static ClientCommandDispatcher Instance => _instance;

        internal ArchipelagoPacketBase BuildCommand(string cmd, ArchipelagoPacketBase packet)
        {
            if (_builderMap.TryGetValue(cmd, out var builder))
            {
                return builder.Build(packet);
            }
            return null;
        }
    }
}
