using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ScienceContainer.Modules
{
    public class ScienceResourceModule : PartModule
    {
        private readonly Dictionary<string, List<ModuleResource>> _resources = new Dictionary<string, List<ModuleResource>>();

        public bool IsOn;

        public override void OnAwake()
        {
            IsOn = true;
        }

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                _resources.Clear();

                string modulePath;

                try
                {
                    modulePath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(ScienceResourceModule)).Location);

                    modulePath = Path.Combine(modulePath, "config.cfg");
                }
                catch (Exception ex)
                {
                    IsOn = false;
                    Debug.LogException(ex);
                    return;
                }

                var data = ConfigNode.Load(modulePath);

                var items = data.GetNodes("DATA");
                foreach (var item in items)
                {
                    var key = item.GetValue("experimentId");
                    var resourceList = new List<ModuleResource>();

                    var resourceNodes = item.GetNodes("RESOURCE");

                    foreach (var resourceNode in resourceNodes)
                    {
                        var value = new ModuleResource();
                        value.Load(resourceNode);
                        resourceList.Add(value);
                    }

                    _resources.Add(key, resourceList);
                }
            }
            catch (Exception)
            {
                Debug.LogError("Error load config");
            }
        }

        public override void OnStart(StartState state)
        {
            if (!IsOn) return;

            base.OnStart(state);

            if (state == StartState.Editor || vessel == null)
                return;

            var currentModules = vessel.Parts.SelectMany(x => x.Modules.OfType<ScienceResourceModule>()).ToList();

            currentModules.Remove(this);

            currentModules.ForEach(x => x.IsOn = false);

            var modules = vessel.Parts.SelectMany(x => x.Modules.OfType<ModuleScienceExperiment>()).ToList();

            foreach (var module in modules)
            {
                try
                {
                    AttachModuleEvent(module);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private void AttachModuleEvent(ModuleScienceExperiment module)
        {
            List<ModuleResource> resources;

            if (!_resources.TryGetValue(module.experimentID, out resources) || !resources.Any()) return;

            var deployEvent = module.Events["DeployExperiment"];

            var prop = typeof(BaseEvent).GetProperty("onEvent", BindingFlags.Instance | BindingFlags.NonPublic);

            var propDelegate = prop.GetValue(deployEvent, null) as BaseEventDelegate;

            var resEvent = new BaseEvent(module.Events, "DeployExperiment", new BaseEventDelegate(
                () =>
                {                   
                    foreach (var resource in resources)
                    {
                        var amount = this.part.RequestResource(resource.id, resource.amount);
                        if (amount >= resource.amount)
                        {
                            if (propDelegate != null) propDelegate.Invoke();
                        }
                    }
                }
                ))
            {
                active = deployEvent.active,
                externalToEVAOnly = deployEvent.externalToEVAOnly,
                guiActive = deployEvent.guiActive,
                guiActiveEditor = deployEvent.guiActiveEditor,
                guiActiveUnfocused = deployEvent.guiActiveUnfocused,
                guiIcon = deployEvent.guiIcon,
                guiName = deployEvent.guiName,
                unfocusedRange = deployEvent.unfocusedRange
            };

            module.Events.Remove(deployEvent);
            module.Events.Add(resEvent);
        }
    }
}

