using System;
using System.Linq;
using Noise;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Noise
{
    public class NoiseLoader : MonoBehaviour
    {
        [Serializable]
        public struct Settings
        {
            [Tooltip("To high numbers of resolution can cause sum of temp registers shader warning.")]public Vector2Int Resolution;
            [Tooltip("Max amount of 6")] public NoiseGenerator.NoiseConfig[] _Config;
        }
    
        [SerializeField] private Image _Image;
        [SerializeField] private Settings _Settings;

        private void OnValidate()
        {
            if (_Settings._Config.Length > 6) _Settings._Config = _Settings._Config.Take(6).ToArray();
        }

        /// <summary>
        /// Load a new noise texture into the referenced image sprite
        /// </summary>
        public void Load()
        {
             var noiseTexture = NoiseGenerator.GenerateNoise(
                 _Settings.Resolution.x, 
                 _Settings.Resolution.y,
                 _Settings._Config);
            
             if(!noiseTexture) return;
            
             _Image.sprite = Sprite.Create(
                 noiseTexture, new Rect(0, 0, noiseTexture.width, noiseTexture.height), Vector2.one * 0.5f);
        }
    }
}


#if UNITY_EDITOR

[CustomEditor(typeof(NoiseLoader))]
public class NoiseLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        var noiseLoader = (NoiseLoader) target;
        if(!noiseLoader) return;
        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        if(GUILayout.Button("Generate Noise", new GUIStyle(GUI.skin.button), GUILayout.Width(170), GUILayout.Height(30)))
        {
            noiseLoader.Load();
        }
        
        GUILayout.EndHorizontal();
    }
}

#endif
