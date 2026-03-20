using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using Avalonia.Threading;
using Wauncher.Utils;
using Wauncher.ViewModels;

namespace Wauncher.Services
{
    public class ServerService : IServerService
    {
        private DispatcherTimer? _serverRefreshTimer;
        private int _serverRefreshInProgress;
        private bool _started;

        public bool IsOfflineMode => !System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();

        public ObservableCollection<ServerInfo> Servers { get; } = new()
        {
            // ── None (clears selection) ──────────────────────────────────────
            new ServerInfo { Name = "None", IpPort = "", IsOnline = false },

            // ── Real servers ─────────────────────────────────────────────────
            new ServerInfo { Name = "NA | PUG | 64 Tick",   IpPort = "na.classiccounter.cc:27015",  Players = 0, MaxPlayers = 10, IsOnline = true },
            new ServerInfo { Name = "NA | PUG-2 | 64 Tick", IpPort = "na.classiccounter.cc:27016",  Players = 0, MaxPlayers = 10, IsOnline = true },
            new ServerInfo { Name = "EU | PUG | 64 Tick",   IpPort = "eu.classiccounter.cc:27016",  Players = 0, MaxPlayers = 10, IsOnline = true },
            new ServerInfo { Name = "EU | PUG | 128 Tick",  IpPort = "eu.classiccounter.cc:27015",  Players = 0, MaxPlayers = 10, IsOnline = true },
            new ServerInfo { Name = "EU | PUG-2 | 128 Tick",IpPort = "eu.classiccounter.cc:27022",  Players = 0, MaxPlayers = 10, IsOnline = true },
        };

        public ServerService()
        {
        }

        public void Start()
        {
            if (_started)
                return;

            _started = true;
            _ = RefreshServersSafeAsync();
            _serverRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _serverRefreshTimer.Tick += async (_, _) => await RefreshServersSafeAsync();
            _serverRefreshTimer.Start();
        }

        public async Task RefreshServersAsync()
        {
            if (IsOfflineMode)
            {
                foreach (var s in Servers.Where(s => !s.IsNone))
                {
                    s.IsOnline = false;
                    s.Players = 0;
                    s.MaxPlayers = 0;
                    s.Map = "";
                }
                return;
            }

            await ServerQuery.RefreshServers(Servers.Where(s => !s.IsNone));

            // Re-order by player count descending; None always stays at index 0
            var sorted = Servers.Where(s => !s.IsNone)
                                .OrderByDescending(s => s.Players)
                                .ToList();
            int insertAt = 1;
            foreach (var server in sorted)
            {
                int from = Servers.IndexOf(server);
                if (from != insertAt)
                    Servers.Move(from, insertAt);
                insertAt++;
            }
        }

        public async Task RefreshServersSafeAsync()
        {
            if (Interlocked.Exchange(ref _serverRefreshInProgress, 1) == 1)
                return;

            try
            {
                await RefreshServersAsync();
            }
            finally
            {
                Interlocked.Exchange(ref _serverRefreshInProgress, 0);
            }
        }
    }
}
