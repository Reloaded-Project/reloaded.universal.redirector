using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Reloaded.Hooks;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using Reloaded.Universal.Redirector.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace Reloaded.Universal.Redirector
{
    public class Program : IMod, IExports
    {
        /// <summary>
        /// Reports our controller as an exportable interface.
        /// </summary>
        public Type[] GetTypes() => new[] { typeof(IRedirectorController) };

        /// <summary>
        /// Allows access to the mod loader API.
        /// </summary>
        public static IModLoader ModLoader { get; set; }

        private FileAccessServer _server;
        private RedirectorController _redirectorController;
        private Redirector _redirector;
        
        public static void Main() { }
        public void Start(IModLoaderV1 loader)
        {
            #if DEBUG
            Debugger.Launch();
            #endif
            ModLoader = (IModLoader)loader;
            ModLoader.GetController<IReloadedHooks>().TryGetTarget(out var hooks);

            /* Your mod code starts here. */
            var modConfigs  = ModLoader.GetActiveMods().Select(x => x.Generic);
            _redirector           = new Redirector(modConfigs);
            _redirectorController = new RedirectorController(_redirector);
            _server               = new FileAccessServer(hooks, _redirector, _redirectorController);

            ModLoader.AddOrReplaceController<IRedirectorController>(this, _redirectorController);
            ModLoader.ModLoading   += ModLoading;
            ModLoader.ModUnloading += ModUnloading;
        }

        private void ModLoading(IModV1 mod, IModConfigV1 config)   => _redirector.Add(config);
        private void ModUnloading(IModV1 mod, IModConfigV1 config) => _redirector.Remove(config);

        public void Suspend()   => _server.Disable();
        public void Resume()    => _server.Enable();
        public void Unload()    => Suspend();

        public bool CanUnload()  => true;
        public bool CanSuspend() => true;
        public Action Disposing { get; }
    }
}
