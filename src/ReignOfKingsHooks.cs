using CodeHatch.Common;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Players;
using uMod.Configuration;
using uMod.Libraries.Universal;
using uMod.Plugins;

namespace uMod.ReignOfKings
{
    /// <summary>
    /// Game hooks and wrappers for the core Reign of Kings plugin
    /// </summary>
    public partial class ReignOfKings
    {
        #region Player Hooks

        /// <summary>
        /// Called when the player is attempting to connect
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(Player player)
        {
            string id = player.Id.ToString();
            string ip = player.Connection.IpAddress;

            // Let universal know player is joining
            Universal.PlayerManager.PlayerJoin(player.Id, player.Name); // TODO: Handle this automatically

            // Call universal hook
            object canLogin = Interface.Call("CanPlayerLogin", player.Name, id, ip);
            if (canLogin is string || canLogin is bool && !(bool)canLogin)
            {
                // Reject the player with message
                player.ShowPopup("Disconnected", canLogin is string ? canLogin.ToString() : "Connection was rejected"); // TODO: Localization
                player.Connection.Close();
                return ConnectionError.NoError;
            }

            // Let plugins know
            Interface.Call("OnPlayerApproved", player.Name, id, ip);

            return null;
        }

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(PlayerMessageEvent evt)
        {
            // Ignore the server player
            if (evt.PlayerId == 9999999999)
            {
                return null;
            }

            // Call game and universal hooks
            object chatSpecific = Interface.Call("OnPlayerChat", evt);
            object chatUniversal = Interface.Call("OnPlayerChat", evt.Player.IPlayer, evt.Message);
            if (chatSpecific != null || chatUniversal != null)
            {
                // Cancel chat message event
                evt.Cancel();
                return true;
            }

            return null;
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="rokPlayer"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(Player rokPlayer)
        {
            // Ignore the server player
            if (rokPlayer.Id == 9999999999)
            {
                return;
            }

            if (permission.IsLoaded)
            {
                string id = rokPlayer.Id.ToString();

                // Update player's stored username
                permission.UpdateNickname(id, rokPlayer.Name);

                // Set default groups, if necessary
                uModConfig.DefaultGroups defaultGroups = Interface.uMod.Config.Options.DefaultGroups;
                if (!permission.UserHasGroup(id, defaultGroups.Players))
                {
                    permission.AddUserGroup(id, defaultGroups.Players);
                }
                if (rokPlayer.HasPermission("admin") && !permission.UserHasGroup(id, defaultGroups.Administrators))
                {
                    permission.AddUserGroup(id, defaultGroups.Administrators);
                }
            }

            // Let universal know player connected
            Universal.PlayerManager.PlayerConnected(rokPlayer);

            // Call game-specific hook
            Interface.Call("OnPlayerConnected", rokPlayer);

            // Find universal player
            IPlayer player = Universal.PlayerManager.FindPlayerById(rokPlayer.Id.ToString());
            if (player != null)
            {
                // Set IPlayer object on Player
                rokPlayer.IPlayer = player;

                // Call universal hook
                Interface.Call("OnPlayerConnected", player);
            }
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="rokPlayer"></param>
        [HookMethod("IOnPlayerDisconnected")]
        private void IOnPlayerDisconnected(Player rokPlayer)
        {
            // Ignore the server player
            if (rokPlayer.Id == 9999999999)
            {
                return;
            }

            // Let universal know
            Universal.PlayerManager.PlayerDisconnected(rokPlayer);

            // Call game-specific hook
            Interface.Call("OnPlayerDisconnected", rokPlayer);

            // Call universal hook
            Interface.Call("OnPlayerDisconnected", rokPlayer.IPlayer, lang.GetMessage("Unknown", this, rokPlayer.IPlayer.Id));
        }

        /// <summary>
        /// Called when the player is spawning
        /// </summary>
        /// <param name="evt"></param>
        [HookMethod("OnPlayerSpawn")]
        private void OnPlayerSpawn(PlayerFirstSpawnEvent evt)
        {
            // Call universal hook
            Interface.Call("OnUserSpawn", evt.Player.IPlayer);
        }

        /// <summary>
        /// Called when the player has spawned
        /// </summary>
        /// <param name="evt"></param>
        [HookMethod("OnPlayerSpawned")]
        private void OnPlayerSpawned(PlayerPreSpawnCompleteEvent evt)
        {
            // Call universal hook
            Interface.Call("OnUserSpawned", evt.Player.IPlayer);
        }

        /// <summary>
        /// Called when the player is respawning
        /// </summary>
        /// <param name="evt"></param>
        [HookMethod("OnPlayerRespawn")] // Not being called every time?
        private void OnPlayerRespawn(PlayerRespawnEvent evt)
        {
            // Call universal hook
            Interface.Call("OnUserRespawn", evt.Player.IPlayer);
        }

        #endregion Player Hooks
    }
}
