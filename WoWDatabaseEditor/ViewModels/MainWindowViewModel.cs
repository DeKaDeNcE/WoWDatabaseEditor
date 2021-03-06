﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Windows;
using WDE.Common.Providers;
using WoWDatabaseEditor.Managers;
using WoWDatabaseEditor.Utils;
using System.Diagnostics;
using WDE.Common.Menu;

namespace WoWDatabaseEditor.ViewModels
{
    public class MainWindowViewModel : BindableBase, ILayoutViewModelResolver, ICloseAwareViewModel
    {
        private readonly IMessageBoxService messageBoxService;

        private string title = "Visual Database Editor 2018";
        private readonly Dictionary<string, ITool> toolById = new();

        public MainWindowViewModel(IDocumentManager documentManager,
            IStatusBar statusBar,
            IMessageBoxService messageBoxService,
            TasksViewModel tasksViewModel,
            IEnumerable<IMainMenuItem> menuItemProviders)
        {
            DocumentManager = documentManager;
            StatusBar = statusBar;
            this.messageBoxService = messageBoxService;
            OpenDocument = new DelegateCommand<IDocument>(ShowDocument);

            TasksViewModel = tasksViewModel;

            MenuItemProviders = PrepareMenuItems(menuItemProviders);

            foreach (var window in documentManager.AllTools)
                toolById[window.UniqueId] = window;

            ShowAbout();
        }

        public IStatusBar StatusBar { get; }
        public IDocumentManager DocumentManager { get; }

        public TasksViewModel TasksViewModel { get; }
        
        public List<IMainMenuItem> MenuItemProviders { get; }

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }
        
        public DelegateCommand<IDocument> OpenDocument { get; }

        private void ShowAbout()
        {
            DocumentManager.OpenDocument(new AboutViewModel());
        }

        private void ShowDocument(IDocument document)
        {
            DocumentManager.OpenDocument(document);
        }

        public ITool? ResolveViewModel(string id)
        {
            if (toolById.TryGetValue(id, out var tool))
            {
                DocumentManager.OpenedTools.Add(tool);
                return tool;
            }

            return null;
        }

        public void LoadDefault()
        {
            foreach (var tool in toolById.Values)
            {
                if (tool.OpenOnStart)
                    DocumentManager.OpenTool(tool.GetType());
            }
        }

        public bool CanClose()
        {
            var modifiedDocuments = DocumentManager.OpenedDocuments.Where(d => d.IsModified).ToList();

            if (modifiedDocuments.Count > 0)
            {
                while (modifiedDocuments.Count > 0)
                {
                    var editor = modifiedDocuments[^1];
                    var message = new MessageBoxFactory<MessageBoxButtonType>().SetTitle("Document is modified")
                        .SetMainInstruction("Do you want to save the changes of " + editor.Title + "?")
                        .SetContent("Your changes will be lost if you don't save them.")
                        .SetIcon(MessageBoxIcon.Warning)
                        .WithYesButton(MessageBoxButtonType.Ok)
                        .WithNoButton(MessageBoxButtonType.No)
                        .WithCancelButton(MessageBoxButtonType.Cancel);

                    if (modifiedDocuments.Count > 1)
                    {
                        message.SetExpandedInformation("Other modified documents:\n" +
                                                       string.Join("\n",
                                                           modifiedDocuments.SkipLast(1).Select(d => d.Title)));
                        message.WithButton("Yes to all", MessageBoxButtonType.CustomA)
                            .WithButton("No to all", MessageBoxButtonType.CustomB);
                    }

                    MessageBoxButtonType result = messageBoxService.ShowDialog(message.Build());

                    if (result == MessageBoxButtonType.Cancel)
                        return false;

                    if (result == MessageBoxButtonType.Yes)
                    {
                        editor.Save.Execute(null);
                        modifiedDocuments.RemoveAt(modifiedDocuments.Count - 1);
                        DocumentManager.OpenedDocuments.Remove(editor);
                    }
                    else if (result == MessageBoxButtonType.No)
                    {
                        modifiedDocuments.RemoveAt(modifiedDocuments.Count - 1);
                        DocumentManager.OpenedDocuments.Remove(editor);
                    }
                    else if (result == MessageBoxButtonType.CustomA)
                    {
                        foreach (var m in modifiedDocuments)
                            m.Save.Execute(null);
                        modifiedDocuments.Clear();
                    }
                    else if (result == MessageBoxButtonType.CustomB)
                    {
                        modifiedDocuments.Clear();
                    }
                }
            }

            DocumentManager.OpenedDocuments.Clear();

            return true;
        }

        private List<IMainMenuItem> PrepareMenuItems(IEnumerable<IMainMenuItem> menuItemProviders)
        {
            var itemsDict = new Dictionary<string, IMainMenuItem>();
            foreach (var menuItem in menuItemProviders)
            {
                if (itemsDict.ContainsKey(menuItem.ItemName) &&
                    itemsDict[menuItem.ItemName] is MainMenuSubItemsAggregator aggregator)
                    aggregator.AddSubItems(menuItem.SubItems);
                else
                    itemsDict.Add(menuItem.ItemName,
                        new MainMenuSubItemsAggregator(menuItem.ItemName, menuItem.SortPriority, menuItem.SubItems.ToList()));
            }

            var list = itemsDict.Values.ToList();
            list.Sort(new MainMenuItemComparer());
            return list;
        }
    }
    
    internal class MainMenuSubItemsAggregator: IMainMenuItem
    {
        public string ItemName { get; }
        private List<IMenuItem> subItems;
        public MainMenuItemSortPriority SortPriority { get; }

        internal MainMenuSubItemsAggregator(string itemName, MainMenuItemSortPriority sortPriority, List<IMenuItem> subItems)
        {
            this.subItems = subItems;
            SortPriority = sortPriority;
            ItemName = itemName; 
        }

        public void AddSubItems(List<IMenuItem> subItems) => this.subItems.AddRange(subItems);

        public List<IMenuItem> SubItems => subItems;
    }
}