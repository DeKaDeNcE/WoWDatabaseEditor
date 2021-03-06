﻿using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor
{
    public abstract class SmartScriptSolutionItemProvider : ISolutionItemProvider
    {
        private readonly string desc;
        private readonly ImageSource icon;
        private readonly string name;
        private readonly SmartScriptType type;

        protected SmartScriptSolutionItemProvider(string name, string desc, string icon, SmartScriptType type)
        {
            this.name = name;
            this.desc = desc;
            this.type = type;
            this.icon = new BitmapImage(new Uri($"/WDE.SmartScriptEditor;component/Resources/{icon}.png", UriKind.Relative));
        }

        public string GetName()
        {
            return name;
        }

        public ImageSource GetImage()
        {
            return icon;
        }

        public string GetDescription()
        {
            return desc;
        }
        
        public bool IsCompatibleWithCore(ICoreVersion core) => 
            core.SmartScriptFeatures.SupportedTypes.Contains(type);

        public abstract ISolutionItem CreateSolutionItem();
    }

    [AutoRegister]
    public class SmartScriptCreatureProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<ICreatureEntryProviderService> creatureEntryProvider;

        public SmartScriptCreatureProvider(Lazy<ICreatureEntryProviderService> creatureEntryProvider) : base("Creature Script",
            "Script any npc in game.",
            "SmartScriptCreatureIcon",
            SmartScriptType.Creature)
        {
            this.creatureEntryProvider = creatureEntryProvider;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            uint? entry = creatureEntryProvider.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int) entry.Value, SmartScriptType.Creature);
        }
    }

    [AutoRegister]
    public class SmartScriptGameobjectProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<IGameobjectEntryProviderService> goProvider;

        public SmartScriptGameobjectProvider(Lazy<IGameobjectEntryProviderService> goProvider) : base("Gameobject Script",
            "Create script for object, including transports.",
            "SmartScriptGameobjectIcon",
            SmartScriptType.GameObject)
        {
            this.goProvider = goProvider;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            uint? entry = goProvider.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int) entry.Value, SmartScriptType.GameObject);
        }
    }

    [AutoRegister]
    public class SmartScriptQuestProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<IQuestEntryProviderService> service;

        public SmartScriptQuestProvider(Lazy<IQuestEntryProviderService> service) : base("Quest Script",
            "Write a script for quest: on accept, on reward.",
            "SmartScriptQuestIcon",
            SmartScriptType.Quest)
        {
            this.service = service;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            uint? entry = service.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int) entry.Value, SmartScriptType.Quest);
        }
    }

    [AutoRegister]
    public class SmartScriptAuraProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<ISpellEntryProviderService> service;

        public SmartScriptAuraProvider(Lazy<ISpellEntryProviderService> service) : base("Aura Script",
            "Auras can have scripted several events: on apply, on remove, on periodic tick.",
            "SmartScriptAuraIcon",
            SmartScriptType.Aura)
        {
            this.service = service;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            uint? entry = service.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int) entry.Value, SmartScriptType.Spell);
        }
    }

    [AutoRegister]
    public class SmartScriptSpellProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<ISpellEntryProviderService> service;

        public SmartScriptSpellProvider(Lazy<ISpellEntryProviderService> service) : base("Spell Script",
            "Create a new script for spell: this includes script for any existing effect in spell.",
            "SmartScriptSpellIcon",
            SmartScriptType.Spell)
        {
            this.service = service;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            uint? entry = service.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int) entry.Value, SmartScriptType.Spell);
        }
    }

    [AutoRegister]
    public class SmartScriptTimedActionListProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<ICreatureEntryProviderService> creatureEntryProvider;

        public SmartScriptTimedActionListProvider(Lazy<ICreatureEntryProviderService> creatureEntryProvider) : base(
            "Timed action list",
            "Timed action list contains list of actions played in time, this can be used to create RP events, cameras, etc.",
            "SmartScriptTimedActionListIcon",
            SmartScriptType.TimedActionList)
        {
            this.creatureEntryProvider = creatureEntryProvider;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            uint? entry = creatureEntryProvider.Value.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int) entry.Value, SmartScriptType.TimedActionList);
        }
    }
    
    [AutoRegister]
    public class SmartScriptClientAreaTriggerEntityListProvider : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<IItemFromListProvider> itemFromListProvider;
        private readonly Lazy<IDbcStore> dbcStore;

        public SmartScriptClientAreaTriggerEntityListProvider(
            Lazy<IItemFromListProvider> itemFromListProvider,
            Lazy<IDbcStore> dbcStore
        ) : base("Client Area Trigger",
            "The script from AreaTrigger from client database (DBC)",
            "SmartScriptGeneric",
            SmartScriptType.AreaTrigger)
        {
            this.itemFromListProvider = itemFromListProvider;
            this.dbcStore = dbcStore;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            var areaTriggers =
                dbcStore.Value.AreaTriggerStore.ToDictionary(at => at.Key, at => new SelectOption($"Client areatrigger {at.Key}"));
            int? entry = itemFromListProvider.Value.GetItemFromList(areaTriggers, false);
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem(entry.Value, SmartScriptType.AreaTrigger);
        }
    }
    
    public abstract class SmartScriptAreaTriggerEntityListProviderBase : SmartScriptSolutionItemProvider
    {
        private readonly Lazy<IItemFromListProvider> itemFromListProvider;
        private readonly Lazy<IDatabaseProvider> database;
        private readonly SmartScriptType type;
        private readonly bool serverSide;

        public SmartScriptAreaTriggerEntityListProviderBase(
            Lazy<IItemFromListProvider> itemFromListProvider,
            Lazy<IDatabaseProvider> database,
            string name,
            string desc,
            string icon,
            SmartScriptType type,
            bool serverSide
        ) : base(name, desc, icon, type)
        {
            this.itemFromListProvider = itemFromListProvider;
            this.database = database;
            this.type = type;
            this.serverSide = serverSide;
        }

        public override ISolutionItem CreateSolutionItem()
        {
            var areaTriggers = database.Value.GetAreaTriggerTemplates()
                .Where(trigger => trigger.IsServerSide == serverSide)
                .ToDictionary(at => (int)at.Id, at => new SelectOption($"Area trigger {at.Id}"));

            int? entry = itemFromListProvider.Value.GetItemFromList(areaTriggers, false);
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem(entry.Value, type);
        }
    }
    
    [AutoRegister]
    public class SmartScriptAreaTriggerEntityListProvider : SmartScriptAreaTriggerEntityListProviderBase
    {
        public SmartScriptAreaTriggerEntityListProvider(
            Lazy<IItemFromListProvider> itemFromListProvider,
            Lazy<IDatabaseProvider> database
        ) : base(
            itemFromListProvider,
            database,
            "Area Trigger Entity",
            "The script from AreaTrigger defined in areatrigger_template",
            "SmartScriptGeneric",
            SmartScriptType.AreaTriggerEntity,
            false) {}
    }
    
    [AutoRegister]
    public class SmartScriptServerSideAreaTriggerEntityListProvider : SmartScriptAreaTriggerEntityListProviderBase
    {
        public SmartScriptServerSideAreaTriggerEntityListProvider(
            Lazy<IItemFromListProvider> itemFromListProvider,
            Lazy<IDatabaseProvider> database
        ) : base(
            itemFromListProvider,
            database,
            "Area Trigger Server-side Entity",
            "The script from server side AreaTrigger defined in areatrigger_template",
            "SmartScriptGeneric",
            SmartScriptType.AreaTriggerEntityServerSide,
            true) {}
    }
}