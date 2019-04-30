using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unity.QuickSearch {
    namespace Providers {

        [UsedImplicitly]
        static class ComradeSCSearchProvider {
            
            internal static string type = "comradeSC";
            internal static string displayName = "ComradeSC";
            
            [UsedImplicitly, SearchItemProvider]
            internal static SearchProvider CreateProvider() {

                SearchProvider provider = new SearchProvider(type, displayName) {
                    priority = 100,
                    filterId = "sc:",
                    
                    fetchItems = (context, items, _provider) => {
                        string filter = "t:Scene " + context.searchQuery;
                        
                        items.AddRange(AssetDatabase.FindAssets(filter)
                                                        .Select(AssetDatabase.GUIDToAssetPath)
                                                        .Where(path => !AssetDatabase.IsValidFolder(path))
                                                        .Take(1001)
                                                        .Select(path => _provider.CreateItem(path, Path.GetFileName(path))));
                    },
                    
                    fetchDescription = (item, context) => {
                        if (AssetDatabase.IsValidFolder(item.id))
                            return item.id;
                        long fileSize = new FileInfo(item.id).Length;
                        item.description = $"{item.id} ({EditorUtility.FormatBytes(fileSize)})";
                        return item.description;
                    },
                    
                    fetchThumbnail = (item, context) => {
                        if (item.thumbnail)
                            return item.thumbnail;

                        if (context.totalItemCount < 200) {
                            var obj = AssetDatabase.LoadAssetAtPath<Object>(item.id);
                            if (obj != null)
                                item.thumbnail = AssetPreview.GetAssetPreview(obj);
                            if (item.thumbnail)
                                return item.thumbnail;
                        }
                        item.thumbnail = AssetDatabase.GetCachedIcon(item.id) as Texture2D;
                        if (item.thumbnail)
                            return item.thumbnail;

                        item.thumbnail = UnityEditorInternal.InternalEditorUtility.FindIconForFile(item.id);
                        return item.thumbnail;
                    }
                };
                
                return provider;
            }

            [UsedImplicitly, SearchActionsProvider]
            internal static IEnumerable<SearchAction> ActionHandlers() {
                
                return new[] {
                    new SearchAction(type, "open") {
                        handler = (item, context) => {
                            EditorSceneManager.OpenScene(item.id);
                        }
                    }
                };
            }

        }
        
        [UsedImplicitly]
        static class ComradeCommandsSearchProvider {
            
            internal static string type = "comradeCommands";
            internal static string displayName = "ComradeCommands";

            internal static Dictionary<string, string> CommandsDict = new Dictionary<string, string>() {
                {"help", "Help"},
                {"cpp", "Clear Player Prefs"},
                {"cl", "Clear Logs Console"},
            };
            
            [UsedImplicitly, SearchItemProvider]
            internal static SearchProvider CreateProvider() {
                return new SearchProvider(type, displayName) {
                    priority = 101,
#if UNITY_EDITOR_OSX
                    filterId = "§",
#else
                    filterId = "~",
#endif

                    fetchItems = (context, items, _provider) => {
                        string query = context.searchQuery;

                        if (!CommandsDict.ContainsKey(query))
                            return;

                        if(query != "help")
                            items.Add(_provider.CreateItem(query, query, CommandsDict[query], Icons.settings));
                        else {
                            items.AddRange(CommandsDict.Where(item => item.Key != "help")
                                                       .Select(item => _provider.CreateItem(item.Key, item.Key, item.Value, Icons.settings)));
                        }
                    }
                };
            }

            [UsedImplicitly, SearchActionsProvider]
            internal static IEnumerable<SearchAction> ActionHandlers() {
                
                return new[] {
                    new SearchAction(type, "Execute") {
                        handler = (item, context) => {

                            switch(item.label) {
                                case "rpp":
                                    PlayerPrefs.DeleteAll();
                                    break;

                                case "cl":
                                    ClearLog();
                                    break;
                            }
                            
                        }
                    }
                };
            }
            
            internal static void ClearLog() {
                var assembly = Assembly.GetAssembly(typeof(Editor));
                var aType = assembly.GetType("UnityEditor.LogEntries");
                var method = aType.GetMethod("Clear");
                method.Invoke(new object(), null);
            }

        }
    
    }
}