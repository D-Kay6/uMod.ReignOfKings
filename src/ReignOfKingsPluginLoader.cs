using System;
using uMod.Plugins;

namespace uMod.ReignOfKings
{
    /// <summary>
    /// Responsible for loading the core Reign of Kings plugin
    /// </summary>
    public class ReignOfKingsPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(ReignOfKings) };
    }
}
