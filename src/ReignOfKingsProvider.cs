using uMod.Libraries.Universal;

namespace uMod.ReignOfKings
{
    /// <summary>
    /// Provides Universal functionality for the game "Reign of Kings"
    /// </summary>
    public class ReignOfKingsProvider : IUniversalProvider
    {
        /// <summary>
        /// Gets the name of the game for which this provider provides
        /// </summary>
        public string GameName => "Reign of Kings";

        /// <summary>
        /// Gets the Steam app ID of the game's client, if available
        /// </summary>
        public uint ClientAppId => 344760;

        /// <summary>
        /// Gets the Steam app ID of the game's server, if available
        /// </summary>
        public uint ServerAppId => 381690;

        /// <summary>
        /// Gets the singleton instance of this provider
        /// </summary>
        internal static ReignOfKingsProvider Instance { get; private set; }

        public ReignOfKingsProvider()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the player manager
        /// </summary>
        public ReignOfKingsPlayerManager PlayerManager { get; private set; }

        /// <summary>
        /// Gets the command system provider
        /// </summary>
        public ReignOfKingsCommands CommandSystem { get; private set; }

        /// <summary>
        /// Creates the game-specific server object
        /// </summary>
        /// <returns></returns>
        public IServer CreateServer() => new ReignOfKingsServer();

        /// <summary>
        /// Creates the game-specific player manager object
        /// </summary>
        /// <returns></returns>
        public IPlayerManager CreatePlayerManager()
        {
            PlayerManager = new ReignOfKingsPlayerManager();
            PlayerManager.Initialize();
            return PlayerManager;
        }

        /// <summary>
        /// Creates the game-specific command system provider object
        /// </summary>
        /// <returns></returns>
        public ICommandSystem CreateCommandSystemProvider() => CommandSystem = new ReignOfKingsCommands();

        /// <summary>
        /// Formats the text with markup as specified in uMod.Libraries.Formatter
        /// into the game-specific markup language
        /// </summary>
        /// <param name="text">text to format</param>
        /// <returns>formatted text</returns>
        public string FormatText(string text) => Formatter.ToRoKAnd7DTD(text);
    }
}
