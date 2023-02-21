using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MissingReferenceFinder
{
    public class MissingReferencesFinder
    {
        private readonly List<MissingReferenceData> _missingReferences = new List<MissingReferenceData>();

        public bool TryGetMissingReferencesInGameObject(GameObject gameObject, out List<MissingReferenceData> missingReferences)
        {
            _missingReferences.Clear();
            SearchInGameObject(gameObject, true);
        
            missingReferences = new List<MissingReferenceData>();
            missingReferences.AddRange(_missingReferences);
        
            return _missingReferences.Count > 0;
        }
    
        private void SearchInGameObject(GameObject asset, bool isFindInsideObject)
        {
            var components = asset.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null)
                {
                    var missingReferenceData = new MissingReferenceData()
                    {
                        AssetName = asset.name,
                        AssetPath = GetObjectFullPath(asset),
                        ComponentName = "Missing Component",
                        PropertyName = "N/A",
                    };
                    _missingReferences.Add(missingReferenceData);
                }
                else
                {
                    SerializedObject serializedObject = new SerializedObject(component);
                    SerializedProperty prop = serializedObject.GetIterator();
                    while (prop.NextVisible(true))
                    {
                        if (prop.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                            {
                                var missingReferenceData = new MissingReferenceData()
                                {
                                    AssetName = asset.name,
                                    AssetPath = GetObjectFullPath(asset),
                                    ComponentName = component.GetType().Name,
                                    PropertyName = prop.name,
                                };
                                _missingReferences.Add(missingReferenceData);
                            }
                        }
                    }
                }
            }

            if (!isFindInsideObject) return;
            foreach (Transform child in asset.transform) 
                SearchInGameObject(child.gameObject, true);
        }
    
        private string GetObjectFullPath(GameObject gameObject)
        {
            Transform parent = gameObject.transform.parent;
            return parent == null ? gameObject.name : GetObjectFullPath(parent.gameObject) + "/" + gameObject.name;
        }
    }
}
