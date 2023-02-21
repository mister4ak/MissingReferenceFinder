using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Scene = UnityEngine.SceneManagement.Scene;

namespace MissingReferenceFinder.Editor
{
    public class MissingReferencesFinderEditor : EditorWindow
    {
        private const int AssetCountPerUpdate = 20;
    
        private readonly Dictionary<string, List<MissingReferenceData>> _missingReferencesByAssetPath = 
            new Dictionary<string, List<MissingReferenceData>>();

        private MissingReferencesFinder _missingReferencesFinder;
        private GameObject[] _currentSceneRootObjects;
        private Scene _currentScene;
    
        private Vector2 _goScrollPosition = Vector2.zero;
        private Vector2 _messageScrollPosition = Vector2.zero;

        private string[] _scenes;
        private string[] _assetGuids;
        private string _selectedObject;
        private float _searchProgress;
        private int _missingReferencesCounter;
        private int _currentAssetIndex;
        private int _currentSceneIndex;
        private int _currentRootObjectIndex;
        private int _assetsCount;
        private int _searchedAssetsCounter;
        private bool _isSearching;
        private bool _allScenesSearched;
        private bool _isStartSearching;

        [MenuItem("Tools/Missing References Finder")]
        public static void ShowMyEditor()
        {
            EditorWindow window = GetWindow<MissingReferencesFinderEditor>();
            window.titleContent = new GUIContent("Missing References Finder");
            window.minSize = new Vector2(900, 500);
        }

        private void OnGUI()
        {
            if (_isSearching)
                ShowSearchProgress();
            else
                ShowSearchButtonAndSettings();

            TryShowMissingReferencesCounter();
            TryShowCompleteSearching();
        
            GUILayout.Space(20);
        
            GUILayout.BeginHorizontal();
            ShowButtonsWithMissingReferenceObjects();
            ShowAllMissingReferencesOfSelectedObject();
            GUILayout.EndHorizontal();
        }

        private void ShowSearchProgress()
        {
            GUILayout.Label("Searching for missing references...");
            GUILayout.Space(10);
            GUILayout.Label($"Progress: {(int) (_searchProgress * 100)}%");
        }

        private void ShowSearchButtonAndSettings()
        {
            GUILayout.Label("Click the button below to search for missing references in your project.");
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Space(position.width / 2 - 100f);
            if (GUILayout.Button("Search for Missing References", GUILayout.Width(200f), GUILayout.Height(30f)))
            {
                StartSearch();
            }

            GUILayout.EndHorizontal();
        }

        private void TryShowCompleteSearching()
        {
            if (!_isSearching && _isStartSearching) GUILayout.Label("Searching complete");
        }

        private void TryShowMissingReferencesCounter()
        {
            if (_isStartSearching) GUILayout.Label($"Current missing references count: {_missingReferencesCounter}");
        }

        private void ShowButtonsWithMissingReferenceObjects()
        {
            var objectButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft
            };
            _goScrollPosition = GUILayout.BeginScrollView(_goScrollPosition, GUILayout.Width(300f));
            foreach (var rootGameObject in _missingReferencesByAssetPath.Keys)
            {
                if (GUILayout.Button(Path.GetFileNameWithoutExtension(rootGameObject), objectButtonStyle))
                {
                    _selectedObject = rootGameObject;
                    Selection.objects = new[] { AssetDatabase.LoadMainAssetAtPath(_selectedObject) };
                }
            }
            GUILayout.EndScrollView();
        }

        private void ShowAllMissingReferencesOfSelectedObject()
        {
            _messageScrollPosition = GUILayout.BeginScrollView(_messageScrollPosition);
            if (_selectedObject != null)
            {
                GUILayout.Label($"Missing References in {Path.GetFileName(_selectedObject)}:", EditorStyles.boldLabel);
                GUILayout.Space(20f);

                if (_missingReferencesByAssetPath.ContainsKey(_selectedObject))
                {
                    for (var i = 0; i < _missingReferencesByAssetPath[_selectedObject].Count; i++)
                    {
                        var missingReferenceData = _missingReferencesByAssetPath[_selectedObject][i];
                        if (missingReferenceData.PropertyName == string.Empty &&
                            missingReferenceData.ComponentName == string.Empty)
                        {
                            GUILayout.Label($"{i + 1}. Missing Component in {missingReferenceData.AssetName}");
                            GUILayout.Label($"Path: {missingReferenceData.AssetPath}");
                        }
                        else
                        {
                            GUILayout.Label($"{i + 1}. Missing Reference in {missingReferenceData.AssetName}");
                            GUILayout.Label($"Path: {missingReferenceData.AssetPath}");
                            GUILayout.Label($"Component: {missingReferenceData.ComponentName}");
                            GUILayout.Label($"Property: {missingReferenceData.PropertyName}");
                        }

                        GUILayout.Space(10f);
                    }
                }
            }
            GUILayout.EndScrollView();
        }

        private void StartSearch()
        {
            ClearData();
        
            _assetGuids = AssetDatabase.FindAssets("t:prefab", new[] {"Assets"});
            _scenes = AssetDatabase.FindAssets("t:Scene", new[] {"Assets"});
        
            _assetsCount += _assetGuids.Length;
        
            LoadNextScene();
        
            _isSearching = true;
            _isStartSearching = true;
        
            _missingReferencesFinder = new MissingReferencesFinder();
        
            EditorApplication.update += SearchInNextAssets;
        }

        private void ClearData()
        {
            _currentAssetIndex = 0;
            _currentSceneIndex = 0;
            _currentRootObjectIndex = 0;
            _assetsCount = 0;
            _searchedAssetsCounter = 0;
            _missingReferencesCounter = 0;
            _allScenesSearched = false;
            _missingReferencesByAssetPath.Clear();
        }

        private void SearchInNextAssets()
        {
            if (_allScenesSearched && _currentAssetIndex >= _assetGuids.Length)
            {
                SearchingComplete();
                return;
            }

            if (_allScenesSearched)
            {
                for (int i = 0; i < AssetCountPerUpdate; i++)
                {
                    if (_currentAssetIndex >= _assetGuids.Length) return;
                    FindAssetInProject();
                }
            }
            else
            {
                TryLoadNextScene();
                for (int i = 0; i < AssetCountPerUpdate; i++)
                {
                    if (_currentRootObjectIndex >= _currentSceneRootObjects.Length) return;
                    FindAssetInLoadedScene();
                }
            }

            _searchProgress = (float) _searchedAssetsCounter / _assetsCount;
            Repaint();
        }

        private void SearchingComplete()
        {
            _isSearching = false;
            EditorApplication.update -= SearchInNextAssets;
        }

        private void FindAssetInProject()
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(_assetGuids[_currentAssetIndex]);
            if (!string.IsNullOrEmpty(assetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (asset != null)
                {
                    if (_missingReferencesFinder.TryGetMissingReferencesInGameObject(asset, 
                        out List<MissingReferenceData> missingReferences))
                    {
                        _missingReferencesCounter += missingReferences.Count;
                        _missingReferencesByAssetPath.Add(assetPath, missingReferences);
                    }
                }
            }

            _searchedAssetsCounter++;
            _currentAssetIndex++;
        }

        private void TryLoadNextScene()
        {
            if (_currentRootObjectIndex >= _currentSceneRootObjects.Length)
            {
                if (_currentScene.isLoaded)
                    EditorSceneManager.CloseScene(_currentScene, true);
            
                if (_currentSceneIndex >= _scenes.Length)
                {
                    _allScenesSearched = true;
                }
                else
                {
                    LoadNextScene();
                }
            }
        }

        private void LoadNextScene()
        {
            var scenePath = AssetDatabase.GUIDToAssetPath(_scenes[_currentSceneIndex]);
            _currentScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            _currentSceneRootObjects = _currentScene.GetRootGameObjects();
            _assetsCount += _currentSceneRootObjects.Length;
            _currentRootObjectIndex = 0;
            _currentSceneIndex++;
        }

        private void FindAssetInLoadedScene()
        {
            if (_missingReferencesFinder.TryGetMissingReferencesInGameObject(_currentSceneRootObjects[_currentRootObjectIndex],
                out List<MissingReferenceData> missingReferences))
            {
                _missingReferencesCounter += missingReferences.Count;
                if (_missingReferencesByAssetPath.ContainsKey(_currentScene.path))
                    _missingReferencesByAssetPath[_currentScene.path].AddRange(missingReferences);
                else
                    _missingReferencesByAssetPath.Add(_currentScene.path, missingReferences);
            }
        
            _searchedAssetsCounter++;
            _currentRootObjectIndex++;
        }
    }
}