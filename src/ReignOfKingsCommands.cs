using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using uMod.Libraries;
using uMod.Libraries.Universal;
using uMod.Plugins;
using CommandAttribute = CodeHatch.Engine.Core.Commands.CommandAttribute;

namespace uMod.ReignOfKings
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class ReignOfKingsCommands : ICommandSystem
    {
        #region Initialization

        // The universal provider
        private readonly ReignOfKingsProvider reignOfKingsUniversal = ReignOfKingsProvider.Instance;

        // The console player
        private readonly ReignOfKingsConsolePlayer consolePlayer;

        // Command handler
        private readonly CommandHandler commandHandler;

        // All registered commands
        internal IDictionary<string, RegisteredCommand> registeredCommands;

        internal class ChatCommand
        {
            public readonly string Name;
            public readonly Plugin Plugin;
            public readonly Action<Player, string, string[]> Callback;

            public ChatCommand(string name, Plugin plugin, Action<Player, string, string[]> callback)
            {
                Name = name;
                Plugin = plugin;
                Callback = callback;
            }

            public void HandleCommand(Player sender, string name, string[] args)
            {
                Plugin?.TrackStart();
                Callback?.Invoke(sender, name, args);
                Plugin?.TrackEnd();
            }
        }

        // Registered commands
        internal class RegisteredCommand
        {
            /// <summary>
            /// The plugin that handles the command
            /// </summary>
            public readonly Plugin Source;

            /// <summary>
            /// The name of the command
            /// </summary>
            public readonly string Command;

            /// <summary>
            /// The callback
            /// </summary>
            public readonly CommandCallback Callback;

            /// <summary>
            /// The callback
            /// </summary>
            public CommandAttribute OriginalCallback;

            /// <summary>
            /// Initializes a new instance of the RegisteredCommand class
            /// </summary>
            /// <param name="source"></param>
            /// <param name="command"></param>
            /// <param name="callback"></param>
            public RegisteredCommand(Plugin source, string command, CommandCallback callback)
            {
                Source = source;
                Command = command;
                Callback = callback;
            }
        }

        /// <summary>
        /// Initializes the command system
        /// </summary>
        public ReignOfKingsCommands()
        {
            registeredCommands = new Dictionary<string, RegisteredCommand>();
            commandHandler = new CommandHandler(ChatCommandCallback, registeredCommands.ContainsKey);
            consolePlayer = new ReignOfKingsConsolePlayer();
        }

        private bool ChatCommandCallback(IPlayer caller, string cmd, string[] args)
        {
            return registeredCommands.TryGetValue(cmd, out RegisteredCommand command) && command.Callback(caller, cmd, args);
        }

        #endregion Initialization

        #region Command Registration

        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            // Convert command to lowercase and remove whitespace
            command = command.ToLowerInvariant().Trim();

            // Setup a new Universal command
            RegisteredCommand newCommand = new RegisteredCommand(plugin, command, callback);

            // Check if the command can be overridden
            if (!CanOverrideCommand(command))
            {
                throw new CommandAlreadyExistsException(command);
            }

            // Check if command already exists in another Universal plugin
            if (registeredCommands.TryGetValue(command, out RegisteredCommand cmd))
            {
                if (cmd.OriginalCallback != null)
                {
                    newCommand.OriginalCallback = cmd.OriginalCallback;
                }

                string previousPluginName = cmd.Source?.Name ?? "an unknown plugin";
                string newPluginName = plugin?.Name ?? "An unknown plugin";
                string message = $"{newPluginName} has replaced the '{command}' command previously registered by {previousPluginName}";
                Interface.uMod.LogWarning(message);
            }

            // Check if command is a vanilla Reign of Kings command
            if (CommandManager.RegisteredCommands.ContainsKey(command))
            {
                if (newCommand.OriginalCallback == null)
                {
                    newCommand.OriginalCallback = CommandManager.RegisteredCommands[command];
                }

                CommandManager.RegisteredCommands.Remove(command);
                if (cmd == null)
                {
                    string newPluginName = plugin?.Name ?? "An unknown plugin";
                    string message = $"{newPluginName} has replaced the '{command}' command previously registered by Reign of Kings";
                    Interface.uMod.LogWarning(message);
                }
            }

            // Register the command as a chat command
            registeredCommands[command] = newCommand;
            CommandManager.RegisteredCommands[command] = new CommandAttribute("/" + command, string.Empty)
            {
                Method = (Action<CommandInfo>)Delegate.CreateDelegate(typeof(Action<CommandInfo>), this, GetType().GetMethod(nameof(HandleCommand), BindingFlags.NonPublic | BindingFlags.Instance))
            };
        }

        private void HandleCommand(CommandInfo cmdInfo)
        {
            if (registeredCommands.TryGetValue(cmdInfo.Label.ToLowerInvariant(), out RegisteredCommand _))
            {
                IPlayer player = cmdInfo.Player.IPlayer ?? consolePlayer;
                HandleChatMessage(player, $"/{cmdInfo.Label} {string.Join(" ", cmdInfo.Args)}");
            }
        }

        #endregion Command Registration

        #region Command Unregistration

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        public void UnregisterCommand(string command, Plugin plugin)
        {
            if (registeredCommands.TryGetValue(command, out RegisteredCommand cmd))
            {
                // Check if the command belongs to the plugin
                if (plugin == cmd.Source)
                {
                    // Remove the chat command
                    registeredCommands.Remove(command);

                    // If this was originally a vanilla Reign of Kings command then restore it, otherwise remove it
                    if (cmd.OriginalCallback != null)
                    {
                        CommandManager.RegisteredCommands[cmd.Command] = cmd.OriginalCallback;
                    }
                    else
                    {
                        CommandManager.RegisteredCommands.Remove(cmd.Command);
                    }
                }
            }
        }

        #endregion Command Unregistration

        #region Message Handling

        /// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool HandleChatMessage(IPlayer player, string message) => commandHandler.HandleChatMessage(player, message);

        /// <summary>
        /// Handles a console message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool HandleConsoleMessage(IPlayer player, string message) => commandHandler.HandleConsoleMessage(player ?? consolePlayer, message);

        #endregion Message Handling

        #region Command Overriding

        /// <summary>
        /// Checks if a command can be overridden
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private bool CanOverrideCommand(string command)
        {
            if (registeredCommands.TryGetValue(command, out RegisteredCommand cmd))
            {
                if (cmd.Source.IsCorePlugin)
                {
                    return false;
                }
            }

            return !ReignOfKingsExtension.RestrictedCommands.Contains(command);
        }

        #endregion Command Overriding
    }
}
