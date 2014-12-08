using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ScienceContainer
{
    public class ScienceResourceModule : PartModule
    {
        private readonly Dictionary<string, ModuleResource> _resources = new Dictionary<string, ModuleResource>();

        public bool IsOn { get; set; }

        public override void OnAwake()
        {
            IsOn = true;
        }

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                _resources.Clear();

                var data = GameDatabase.Instance.GetConfigNode("SirDargon/ScienceContainers/Plugins/config/PROP");
                var items = data.GetNodes("DATA");
                foreach (var item in items)
                {
                    var key = item.GetValue("experimentId");
                    var value = new ModuleResource();
                    value.Load(item.GetNode("RESOURCE"));

                    _resources.Add(key, value);
                }
            }
            catch (Exception)
            {
                Debug.LogError("Error load config");
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (state == StartState.Editor || vessel == null)
                return;

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
            ModuleResource resource;

            if (!_resources.TryGetValue(module.experimentID, out resource)) return;

            var deployEvent = module.Events["DeployExperiment"];

            var prop = typeof(BaseEvent).GetProperty("onEvent", BindingFlags.Instance | BindingFlags.NonPublic);

            var propDelegate = prop.GetValue(deployEvent, null) as BaseEventDelegate;

            var resEvent = new BaseEvent(module.Events, "DeployExperiment", new BaseEventDelegate(
                () =>
                {
                    var amount = this.part.RequestResource(resource.id, resource.amount);
                    if (amount >= resource.amount)
                    {
                        if (propDelegate != null) propDelegate.Invoke();
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

