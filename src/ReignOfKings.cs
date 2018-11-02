using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;
using CodeHatch.Permissions;
using System;
using System.Collections.Generic;
using System.Text;
using uMod.Libraries;
using uMod.Libraries.Universal;
using uMod.Logging;
using uMod.Plugins;
using Permission = uMod.Libraries.Permission;

namespace uMod.ReignOfKings
{
    /// <summary>
    /// The core Reign of Kings plugin
    /// </summary>
    public partial class ReignOfKings : CSPlugin
    {
        #region Initialization

        /// <summary>
        /// Initializes a new instance of the ReignOfKings class
        /// </summary>
        public ReignOfKings()
        {
            // Set plugin info attributes
            Title = "Reign of Kings";
            Author = ReignOfKingsExtension.AssemblyAuthors;
            Version = ReignOfKingsExtension.AssemblyVersion;

            CommandManager.OnRegisterCommand += attribute =>
            {
                foreach (string command in attribute.Aliases.InsertItem(attribute.Name, 0))
                {
                    if (Universal.CommandSystem.registeredCommands.TryGetValue(command, out ReignOfKingsCommands.RegisteredCommand universalCommand))
                    {
                        Universal.CommandSystem.registeredCommands.Remove(universalCommand.Command);
                        Universal.CommandSystem.RegisterCommand(universalCommand.Command, universalCommand.Source, universalCommand.Callback);
                    }
                }
            };
        }

        // Libraries
        internal readonly Lang lang = Interface.uMod.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.uMod.GetLibrary<Permission>();

        // Instances
        internal static readonly ReignOfKingsProvider Universal = ReignOfKingsProvider.Instance;
        internal readonly PluginManager pluginManager = Interface.uMod.RootPluginManager;
        internal readonly IServer Server = Universal.CreateServer();

        // The RoK permission library
        private CodeHatch.Permissions.Permission rokPerms;

        private bool serverInitialized;

        // Track 'load' chat commands
        private readonly Dictionary<string, Player> loadingPlugins = new Dictionary<string, Player>();

        #endregion Initialization

        #region Core Hooks

        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote error logging
            RemoteLogger.SetTag("game", Title.ToLower());
            RemoteLogger.SetTag("game version", Server.Version);
        }

        /// <summary>
        /// Called when another plugin has been loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (serverInitialized)
            {
                // Call OnServerInitialized for hotloaded plugins
                plugin.CallHook("OnServerInitialized", false);
            }
        }

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (!serverInitialized)
            {
                // Setup default permission groups
                rokPerms = CodeHatch.Engine.Networking.Server.Permissions;
                if (permission.IsLoaded)
                {
                    int rank = 0;
                    List<PermissionGroup> rokGroups = rokPerms.GetGroups();
                    foreach (string defaultGroup in Interface.uMod.Config.Options.DefaultGroups)
                    {
                        if (!permission.GroupExists(defaultGroup))
                        {
                            permission.CreateGroup(defaultGroup, defaultGroup, rank++);
                        }
                    }

                    permission.RegisterValidate(s =>
                    {
                        if (ulong.TryParse(s, out ulong temp))
                        {
                            int digits = temp == 0 ? 1 : (int)Math.Floor(Math.Log10(temp) + 1);
                            return digits >= 17;
                        }

                        return false;
                    });

                    permission.CleanUp();
                }

                Analytics.Collect();

                // Show the server console, if enabled
                ReignOfKingsExtension.ServerConsole();

                serverInitialized = true;

                // Let plugins know server startup is complete
                Interface.CallHook("OnServerInitialized", serverInitialized);
            }
        }

        /// <summary>
        /// Called when the server is saved
        /// </summary>
        [HookMethod("OnServerSave")]
        private void OnServerSave()
        {
            Interface.uMod.OnSave();

            // Save groups, users, and other data
            Universal.PlayerManager.SavePlayerData();
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown()
        {
            Interface.uMod.OnShutdown();

            // Save groups, users, and other data
            Universal.PlayerManager.SavePlayerData();
        }

        #endregion Core Hooks

        #region Command Handling

        /// <summary>
        /// Parses the specified command
        /// </summary>
        /// <param name="argstr"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        private void ParseCommand(string argstr, out string cmd, out string[] args)
        {
            List<string> arglist = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool inlongarg = false;
            foreach (char c in argstr)
            {
                if (c == '"')
                {
                    if (inlongarg)
                    {
                        string arg = sb.ToString().Trim();
                        if (!string.IsNullOrEmpty(arg))
                        {
                            arglist.Add(arg);
                        }

                        sb = new StringBuilder();
                        inlongarg = false;
                    }
                    else
                    {
                        inlongarg = true;
                    }
                }
                else if (char.IsWhiteSpace(c) && !inlongarg)
                {
                    string arg = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(arg))
                    {
                        arglist.Add(arg);
                    }

                    sb = new StringBuilder();
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0)
            {
                string arg = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(arg))
                {
                    arglist.Add(arg);
                }
            }
            if (arglist.Count == 0)
            {
                cmd = null;
                args = null;
                return;
            }

            cmd = arglist[0];
            arglist.RemoveAt(0);
            args = arglist.ToArray();
        }

        [HookMethod("IOnServerCommand")]
        private object IOnServerCommand(ulong id, string str)
        {
            if (str.Length == 0)
            {
                return null;
            }

            // Get the full command
            string message = str.TrimStart('/');
            ParseCommand(message, out string cmd, out string[] args);
            if (cmd == null)
            {
                return null;
            }

            if (Interface.Call("OnServerCommand", cmd, args) != null)
            {
                return true;
            }

            // Check if command is from the player
            Player rokPlayer = CodeHatch.Engine.Networking.Server.GetPlayerById(id);
            if (rokPlayer == null)
            {
                return null;
            }

            // Get the universal player
            IPlayer player = Universal.PlayerManager.FindPlayerById(id.ToString());
            if (player == null)
            {
                return null;
            }

            // Is the command blocked?
            object blockedSpecific = Interface.Call("OnPlayerCommand", rokPlayer, cmd, args);
            object blockedUniversal = Interface.Call("OnUserCommand", player, cmd, args);
            if (blockedSpecific != null || blockedUniversal != null)
            {
                return true;
            }

            // Is it a chat command?
            if (str[0] != '/')
            {
                return null;
            }

            // Is it a universal command?
            if (Universal.CommandSystem.HandleChatMessage(player, str))
            {
                return true;
            }

            return null;
        }

        #endregion Command Handling
    }
}
