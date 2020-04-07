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

                return new SearchProvider(type, displayName) {
                    priority = 100,
                    filterId = "sc:",
                    
                    fetchItems = (context, items, _provider) => {
                        string filter = "t:Scene " + context.searchQuery;
                        
                        items.AddRange(AssetDatabase.FindAssets(filter)
                                                        .Select(AssetDatabase.GUIDToAssetPath)
                                                        .Where(path => !AssetDatabase.IsValidFolder(path))
                                                        .Take(1001)
                                                        .Select(path => _provider.CreateItem(path, Path.GetFileName(path))));
                        return null;
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
                {"rt", "Reset Transform"},
                {"rtp", "Reset Transform Position"},
                {"rtr", "Reset Transform Rotation"},
                {"rts", "Reset Transform Scale"},
                {"sp", "Save Project"}
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
                            return null;

                        if(query != "help")
                            items.Add(_provider.CreateItem(query, query, CommandsDict[query]));
                        else {
                            items.AddRange(CommandsDict.Where(item => item.Key != "help")
                                                       .Select(item => _provider.CreateItem(item.Key, item.Key, item.Value)));
                        }

                        return null;
                    }
                };
            }

            [UsedImplicitly, SearchActionsProvider]
            internal static IEnumerable<SearchAction> ActionHandlers() {
                
                return new[] {
                    new SearchAction(type, "Execute") {
                        handler = (item, context) => {

                            switch(item.label) {
                                case "cpp":
                                    PlayerPrefs.DeleteAll();
                                    break;

                                case "cl":
                                    ClearLog();
                                    break;

                                case "rt":
                                    ResetTransform(true, true, true);
                                    break;

                                case "rtp":
                                    ResetTransform(true, false, false);
                                    break;
                                    
                                case "rtr":
                                    ResetTransform(false, true, false);
                                    break;
                                    
                                case "rts":
                                    ResetTransform(false, false, true);
                                    break;

                                case "sp":
                                    AssetDatabase.SaveAssets();
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
            
            internal static void ResetTransform(bool isResetPosition, bool isResetRotation, bool isResetScale) {
                foreach(Transform tr in Selection.transforms) {
                    if (isResetPosition) {
                        tr.localPosition = Vector3.zero;
                        
                        RectTransform rtr = tr.GetComponent<RectTransform>();
                        if(rtr != null) {
                            if (rtr.anchorMax.x != rtr.anchorMin.x) {
                                Vector2 offsetMax = rtr.offsetMax;
                                Vector2 offsetMin = rtr.offsetMin;
                                offsetMax.x = 0f;
                                offsetMin.x = 0f;
                                rtr.offsetMax = offsetMax;
                                rtr.offsetMin = offsetMin;
                            }
                                
                            if(rtr.anchorMax.y != rtr.anchorMin.y) {
                                Vector2 offsetMax = rtr.offsetMax;
                                Vector2 offsetMin = rtr.offsetMin;
                                offsetMax.y = 0f;
                                offsetMin.y = 0f;
                                rtr.offsetMax = offsetMax;
                                rtr.offsetMin = offsetMin;
                            }
                        }
                    }
                        
                    if (isResetRotation)
                        tr.localRotation = Quaternion.identity;
                    if (isResetScale)
                        tr.localScale = Vector3.one;
                }
            }

        }
    
    }
}