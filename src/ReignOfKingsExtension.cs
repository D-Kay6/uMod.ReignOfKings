using CodeHatch.Common;
using CodeHatch.Engine.Administration;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using uMod.Extensions;
using uMod.Plugins;
using uMod.Unity;
using UnityEngine;

namespace uMod.ReignOfKings
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class ReignOfKingsExtension : Extension
    {
        // Get assembly info
        internal static Assembly Assembly = Assembly.GetExecutingAssembly();
        internal static AssemblyName AssemblyName = Assembly.GetName();
        internal static VersionNumber AssemblyVersion = new VersionNumber(AssemblyName.Version.Major, AssemblyName.Version.Minor, AssemblyName.Version.Build);
        internal static string AssemblyAuthors = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(Assembly, typeof(AssemblyCompanyAttribute), false)).Company;

        /// <summary>
        /// Gets whether this extension is for a specific game
        /// </summary>
        public override bool IsGameExtension => true;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "ReignOfKings";

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => AssemblyAuthors;

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => AssemblyVersion;

        /// <summary>
        /// Gets the branch of this extension
        /// </summary>
        public override string Branch => "public"; // TODO: Handle this programmatically

        // Commands that a plugin can't override
        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            ""
        };

        /// <summary>
        /// Default game-specific references for use in plugins
        /// </summary>
        public override string[] DefaultReferences => new[]
        {
            ""
        };

        /// <summary>
        /// List of assemblies allowed for use in plugins
        /// </summary>
        public override string[] WhitelistAssemblies => new[]
        {
            "Assembly-CSharp", "mscorlib", "System", "System.Core", "uMod", "UnityEngine"
        };

        /// <summary>
        /// List of namespaces allowed for use in plugins
        /// </summary>
        public override string[] WhitelistNamespaces => new[]
        {
            "CodeHatch", "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine"
        };

        /// <summary>
        /// List of filter matches to apply to console output
        /// </summary>
        public static string[] Filter =
        {
            "9999999999 has null AuthenticationKey!",
            "<color=magenta>[Entity]",
            "<color=yellow>Specific",
            "<color=yellow>Transform",
            "A collider was found on a gameobject",
            "An error occured handling",
            "Cannot find any Entities.",
            "Cannot retrieve Entity because the component",
            "Client owned object was not found to sync with id",
            "Could not attach to bone of type",
            "Could not find any serialized data",
            "Could not find component named MainTransform.",
            "Could not find the bone",
            "Could not initialize the native Steamworks API",
            "Could not retrieve Entity with Network View ID",
            "Could not use FastAO because:",
            "Could not use effect because",
            "Dedicated mode detected.",
            "Destroying DisableWithDistance",
            "Destroying self because the given entity is",
            "Failed to apply setting to DrawDistanceQuality",
            "Finished loading...",
            "Flow controller warning:",
            "FORM IS UnityEngine.WWWForm",
            "Game has started.",
            "Handling user status UserAuthenticated:",
            "HDR RenderTexture",
            "HDR and MultisampleAntiAliasing",
            "Instantiating Base and Dedicated",
            "ItemCacheServiceServer is overwriting a live item cache.",
            "IT WORKED???",
            "Load Server GUID:",
            "Loading: ",
            "Lobby query failed.",
            "Maximum number of connections can't be higher than",
            "Multiple audio items with name",
            "No AudioListener found in the scene",
            "Otherwise billboarding/lighting will not work correctly",
            "PlayerTracker: Tracker",
            "Private RPC R was not sent",
            "Processing new connection...",
            "Registering user",
            "Registering... Success",
            "Retrieving BiomeMap from Thrones Terrain",
            "Save Server GUID:",
            "Serialization settings set successfully",
            "Server has connected.",
            "ServerLobbyModule.cs - WWW - Result failure:",
            "Setting breakpad minidump AppID",
            "Standard Deviation:",
            "SteamInitializeFailed",
            "Steam_SetMinidumpSteamID:",
            "Sync member value was null",
            "The contents affected could not be found:",
            "The exclusive layer does not match the requested layer",
            "The game ended because an error occured",
            "The image effect Main Camera",
            "The referenced script on this Behaviour is missing!",
            "There were some issues with the attached",
            "This could be due to momentary deregistration",
            "Trying to read past the buffer size",
            "Unregistering user",
            "[EAC] [Debug]",
            "[EAC] [Info]",
            "[EAC] [Warning]",
            "[WARNING] Recieved a",
            "\"string button\" is empty;",
            "armorManager == null",
            "cannot estimate angular velocity.",
            "eac_server.dll",
            "is missing a default constructor.",
            "linkedTo == null",
            "m_guiCamera == null",
            "melee == null",
            "in ServerLobbyModule",
            "online is True",
            "this is a local server",
            "with authkey System.Byte[]"
        };

        /// <summary>
        /// Initializes a new instance of the ReignOfKingsExtension class
        /// </summary>
        /// <param name="manager"></param>
        public ReignOfKingsExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            Manager.RegisterPluginLoader(new ReignOfKingsPluginLoader());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="directory"></param>
        public override void LoadPluginWatchers(string directory)
        {
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            CSharpPluginLoader.PluginReferences.UnionWith(DefaultReferences);

            if (!Interface.uMod.CheckConsole() || !Interface.uMod.EnableConsole())
            {
                return;
            }

            SocketAdminConsole socketAdminConsole = UnityEngine.Object.FindObjectOfType<SocketAdminConsole>();
            if (socketAdminConsole._server.Clients.Count > 0)
            {
                return;
            }

            socketAdminConsole.enabled = false;

            Application.logMessageReceived += HandleLog;
            Logger.DebugLogged += HandleLog;
            Logger.InfoLogged += HandleLog;
            Logger.WarningLogged += HandleLog;
            Logger.ErrorLogged += HandleLog;
            Logger.ExceptionLogged += HandleLog;
            Application.logMessageReceived += (message, stackTrace, type) =>
            {
                if (type == LogType.Exception)
                {
                    Interface.uMod.LogDebug(message + "\n" + stackTrace);
                }
            };

            Interface.uMod.ServerConsole.Input += ServerConsoleOnInput;
            Interface.uMod.ServerConsole.Completion = input =>
            {
                if (!string.IsNullOrEmpty(input))
                {
                    if (input.StartsWith("/"))
                    {
                        input = input.Remove(0, 1);
                    }
                    return CommandManager.RegisteredCommands.Keys.Where(c => c.StartsWith(input.ToLower())).ToArray();
                }

                return null;
            };
        }

        internal static void ServerConsole()
        {
            if (Interface.uMod.ServerConsole == null)
            {
                return;
            }

            Interface.uMod.ServerConsole.Title = () => $"{Server.PlayerCount} | {DedicatedServerBypass.Settings.ServerName}";

            Interface.uMod.ServerConsole.Status1Left = () => DedicatedServerBypass.Settings.ServerName;
            Interface.uMod.ServerConsole.Status1Right = () =>
            {
                TimeSpan time = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                string uptime = $"{time.TotalHours:00}h{time.Minutes:00}m{time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return $"{Mathf.RoundToInt(1f / Time.smoothDeltaTime)}fps, {uptime}";
            };

            Interface.uMod.ServerConsole.Status2Left = () =>
            {
                string players = $"{Server.PlayerCount}/{Server.PlayerLimit} players";
                int sleepers = CodeHatch.StarForge.Sleeping.PlayerSleeperObject.AllSleeperObjects.Count;
                int entities = CodeHatch.Engine.Core.Cache.Entity.GetAll().Count;
                return $"{players}, {sleepers + (sleepers.Equals(1) ? " sleeper" : " sleepers")}, {entities + (entities.Equals(1) ? " entity" : " entities")}";
            };
            Interface.uMod.ServerConsole.Status2Right = () =>
            {
                if (uLink.NetworkTime.serverTime <= 0)
                {
                    return "not connected";
                }

                double bytesReceived = 0;
                double bytesSent = 0;
                foreach (Player player in Server.AllPlayers)
                {
                    if (player.Connection.IsConnected)
                    {
                        ConnectionStatistics statistics = player.Connection.Statistics;
                        bytesReceived += statistics.BytesReceivedPerSecond;
                        bytesSent += statistics.BytesSentPerSecond;
                    }
                }
                return $"{Utility.FormatBytes(bytesReceived)}/s in, {Utility.FormatBytes(bytesSent)}/s out";
            };

            Interface.uMod.ServerConsole.Status3Left = () => $"{GameClock.Instance.TimeOfDayAsClockString()}, Weather: {Weather.Instance.CurrentWeather}";
            Interface.uMod.ServerConsole.Status3Right = () => $"uMod.ReignOfKings {AssemblyVersion}";
            Interface.uMod.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            input = input.Trim();
            if (!input.StartsWith("/"))
            {
                input = "/" + input;
            }

            Console.Messages.Clear();
            if (CommandManager.ExecuteCommand(Server.Instance.ServerPlayer.Id, input))
            {
                string output = Console.CurrentOutput.TrimEnd('\n', '\r');
                if (!string.IsNullOrEmpty(output))
                {
                    Interface.uMod.ServerConsole.AddMessage(output);
                }
            }
        }

        private static void HandleLog(Exception message, object context, Type type)
        {
            HandleLog(message.ToString(), IDUtil.GetObjectIDString(context), LogType.Error);
        }

        private static void HandleLog(string message, object context, Type type)
        {
            HandleLog(message, IDUtil.GetObjectIDString(context), LogType.Log);
        }

        private static void HandleLog(string message, string stackTrace, LogType logType)
        {
            if (!string.IsNullOrEmpty(message) && !Filter.Any(message.Contains))
            {
                Interface.uMod.RootLogger.HandleMessage(message, stackTrace, logType.ToLogType());
            }
        }
    }
}
