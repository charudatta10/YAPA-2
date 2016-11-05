﻿using System.Collections.Generic;
using System.Linq;
using YAPA.Contracts;
using YAPA.WPF;

namespace YAPA.Shared
{
    public class PluginManager : IPluginManager
    {
        private readonly IDependencyInjector _container;
        private readonly PluginManagerSettings _settings;
        private IEnumerable<IPlugin> _pluginInstances;
        private IEnumerable<string> _disabledPlugins;
        private bool _initialised = false;

        public PluginManager(IDependencyInjector container, IEnumerable<IPluginMeta> metas, PluginManagerSettings settings)
        {
            _container = container;
            _settings = settings;

            _disabledPlugins = _settings.DisabledPlugins;
            Plugins = metas;
        }

        public IEnumerable<IPluginMeta> Plugins { get; }

        public IEnumerable<IPluginMeta> BuiltInPlugins
        {
            get
            {
                return ActivePlugins
                .Where(x => x.GetType().GetCustomAttributes(typeof(BuiltInPluginAttribute), false).Any())
                .OrderBy(x => ((BuiltInPluginAttribute)x.GetType().GetCustomAttributes(typeof(BuiltInPluginAttribute), false).FirstOrDefault()).Order);
            }
        }

        public IEnumerable<IPluginMeta> CustomPlugins
        {
            get
            {
                return ActivePlugins.Where(_ => _.GetType().GetCustomAttributes(false).FirstOrDefault(y => y.GetType() == typeof(BuiltInPluginAttribute)) == null);
            }
        }

        public IEnumerable<IPluginMeta> ActivePlugins
        {
            get { return Plugins.Where(x => !_disabledPlugins.Contains(x.Title)); }
        }

        public object ResolveSettingWindow(IPluginMeta plugin)
        {
            if (plugin.SettingEditWindow == null)
            {
                return plugin.SettingEditWindow;
            }
            return _container.Resolve(plugin.SettingEditWindow);
        }

        public void InitPlugins()
        {
            if (_initialised)
            {
                return;
            }
            RegisterPluginSettings(_container);
            RegisterPluginSettingsWindows(_container);
            _pluginInstances = RegisterPlugins(_container);
            _initialised = true;
        }

        private IEnumerable<IPlugin> RegisterPlugins(IDependencyInjector container)
        {
            foreach (var plugin in Plugins.Where(x => x.Plugin != null))
            {
                container.Register(plugin.Plugin, true);
            }

            return ActivePlugins.Where(x => x.Plugin != null).Select(plugin => (IPlugin)container.Resolve(plugin.Plugin)).ToList();
        }

        private void RegisterPluginSettings(IDependencyInjector container)
        {
            foreach (var plugin in Plugins.Where(x => x.Settings != null))
            {
                container.Register(plugin.Settings);
            }
        }

        private void RegisterPluginSettingsWindows(IDependencyInjector container)
        {
            foreach (var plugin in Plugins.Where(x => x.SettingEditWindow != null))
            {
                container.Register(plugin.SettingEditWindow);
            }
        }
    }


    public class PluginManagerSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public List<string> DisabledPlugins
        {
            get { return _settings.Get(nameof(DisabledPlugins), new List<string>()); }
            set { _settings.Update(nameof(DisabledPlugins), value); }
        }

        public PluginManagerSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(nameof(PluginManager));
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}
