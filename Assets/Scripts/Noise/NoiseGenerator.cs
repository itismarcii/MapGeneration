using System;
using Extra;
using UnityEngine;

namespace Noise
{
    public static class NoiseGenerator
    {
        public const int NOISE_CONFIG_SIZE = sizeof(uint) + sizeof(uint) + sizeof(int) * 2 + sizeof(float) + sizeof(float) + sizeof(uint) ;
        
        [Serializable]
        public enum NoiseType : uint
        {
            Simplex = 0,
            Perlin = 1,
            Voro = 2,
            VoroSmoothed = 3
        }
        
        [Serializable]
        public class NoiseConfig
        {
            public uint Seed;
            public NoiseType NoiseType;
            public Vector2Int Offset;
            public float ScaleMultiplier = 1;
            [Range(0,2)] public float Weight = 1;
            [Tooltip("Warping a noise field is more expensive and will take longer to calculate.")]public bool Warp;
        }
        
        public struct NoiseConfigHLSL
        {
            public uint Seed;
            public NoiseType NoiseType;
            public Vector2Int Offset;
            public float ScaleMultiplier;
            public float Weight;
            public int Warp;
            
            public NoiseConfigHLSL(in NoiseConfig config)
            {
                Seed = config.Seed;
                NoiseType = config.NoiseType;
                Offset = config.Offset;
                ScaleMultiplier = config.ScaleMultiplier;
                Weight = config.Weight;
                Warp = config.Warp ? 1 : 0;
            }
        }

        #region Global Paramters

        private static ComputeShader _NoiseShader;
        
        // Shader IDs
        private static readonly int 
            NoiseTextureID = Shader.PropertyToID("NoiseTexture"),
            ScaleMultiplierID = Shader.PropertyToID("scale_multiplier"),
            OffsetID = Shader.PropertyToID("offset"),
            SeedID = Shader.PropertyToID("seed"),
            WarpingID = Shader.PropertyToID("warping"),
            NoiseTypeID = Shader.PropertyToID("noise_type"),
            ConfigID = Shader.PropertyToID("NoiseConfigArray"),
            ConfigSizeID = Shader.PropertyToID("config_size");


        #endregion

        /// <summary>
        /// Generates a specific noise type with given values and returns a texture 2D of the noise field.
        /// Note: Will be a texture 3D in the future 
        /// </summary>
        /// <param name="width">width of the noise field</param>
        /// <param name="height">height of the noise field</param>
        /// <param name="depth">depth of the noise field</param>
        /// <param name="scaleMultiplier">zoom in or out factor of the noise field</param>
        /// <param name="offset">offset of the center of the noise field in 3D</param>
        /// <param name="noiseType">Noise type [Perlin, Simplex, Voro, Cellular, Cellular Smoothed]</param>
        /// <param name="seed">seed for reproduction of a noise field</param>
        /// <param name="warping"></param>
        /// <returns>Returns a texture 2D of the noise field</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Texture2D GenerateNoise(in int width, in int height, in float scaleMultiplier,
            in Vector2Int offset, in NoiseType noiseType, in int seed = 0, bool warping = false)
        {
            var renderTexture = TextureHelper.GetRenderTexture(width, height, out var texture);
            
            // Load the Compute Shader if not loaded already
            _NoiseShader = _NoiseShader ? _NoiseShader : Resources.Load<ComputeShader>("Shader/NoiseGenerator");
            
            // Set the target texture for the shader
            _NoiseShader.SetTexture(0, NoiseTextureID, renderTexture);
            
            // Dispatch the compute shader
            _NoiseShader.SetInt(NoiseTypeID, (int) noiseType);
            _NoiseShader.SetFloat(SeedID, seed);
            _NoiseShader.SetBool(WarpingID, warping);
            _NoiseShader.SetFloat(ScaleMultiplierID, scaleMultiplier);
            _NoiseShader.SetVector(OffsetID, new Vector4(offset.x, offset.y, 0.0f, 1.0f));
            _NoiseShader.Dispatch(0, width, height, 1);
    
            // Read data from RenderTexture to Texture2D
            TextureHelper.ReadAndRelease(ref renderTexture, ref texture);

            return texture;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width">Width of the noise field and the X dimension of the texture</param>
        /// <param name="height">Height of the noise field and the Y dimension of the texture</param>
        /// <param name="noiseConfigs">Array of noise parameters that will be added together to form a single noise field</param>
        /// <returns>Returns a texture 2D containing the noise field values</returns>
        public static Texture2D GenerateNoise(in int width, in int height, in NoiseConfig[] noiseConfigs)
        {
            // Transform the noise configs to the hlsl format
            var noiseConfigHlsl = new NoiseConfigHLSL[noiseConfigs.Length];

            for (var i = 0; i < noiseConfigs.Length; i++)
            {
                noiseConfigHlsl[i] = new NoiseConfigHLSL(noiseConfigs[i]);
            }
            
            var renderTexture = TextureHelper.GetRenderTexture(width, height, out var texture);
            
            // Load the Compute Shader if not loaded already
            _NoiseShader = _NoiseShader ? _NoiseShader : Resources.Load<ComputeShader>("Shader/NoiseGenerator");
            
            // Set the target texture for the shader
            _NoiseShader.SetTexture(0, NoiseTextureID, renderTexture);
            
            // Set noise config buffer
            var configBuffer = new ComputeBuffer(noiseConfigHlsl.Length, NOISE_CONFIG_SIZE);
            configBuffer.SetData(noiseConfigHlsl);
            
            // Dispatch the compute shader
            _NoiseShader.SetBuffer(0, ConfigID, configBuffer);
            _NoiseShader.SetInt(ConfigSizeID, noiseConfigs.Length);
            _NoiseShader.Dispatch(0, width, height, 1);
            
            // Read data from RenderTexture to Texture2D
            TextureHelper.ReadAndRelease(ref renderTexture, ref texture);
            configBuffer.Release();

            return texture;
        }
    }
}
