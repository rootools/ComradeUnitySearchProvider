using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.QuickSearch {
    namespace Providers {

        [UsedImplicitly]
        static class ComradeSearchProvider {
            
            internal static string type = "comrade";
            internal static string displayName = "Comrade";
            
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
    
    }
}