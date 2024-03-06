using UnityEngine;

namespace Extra
{
    public static class TextureHelper
    {
        /// <summary>
        /// Get a render texture.
        /// </summary>
        /// <param name="width">width of the texture</param>
        /// <param name="height">height of the texture</param>
        /// <param name="texture2D">output a similar texture 2D</param>
        /// <returns>Returns a render texture</returns>
        public static RenderTexture GetRenderTexture(in int width, in int height, out Texture2D texture2D)
        {
            var renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                { enableRandomWrite = true };
        
            renderTexture.Create();
            texture2D = new Texture2D(width, height, TextureFormat.ARGB32, false);

            return renderTexture;
        }
    
        /// <summary>
        /// Get a render texture with the information of the provided texture 2D
        /// </summary>
        /// <param name="texture2D">Provided texture 2D used to copy the information into the render texture</param>
        /// <returns>Render texture with the information copy of the texture 2D</returns>
        public static RenderTexture GetRenderTexture(in Texture2D texture2D)
        {
            var renderTexture = new RenderTexture(texture2D.width, texture2D.height, 0, RenderTextureFormat.ARGB32)
                { enableRandomWrite = true };
        
            renderTexture.Create();
            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Graphics.Blit(texture2D, renderTexture);
            RenderTexture.active = currentActiveRT;

            return renderTexture;
        }

        /// <summary>
        /// Read the information of the render texture and load it unto the texture 2D
        /// </summary>
        /// <param name="renderTexture">Provided render texture to read from</param>
        /// <param name="texture2D">Provided texture to copy the information from the render texture</param>
        public static void ReadAndRelease(ref RenderTexture renderTexture, ref Texture2D texture2D)
        {
            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, texture2D.width, texture2D.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = currentActiveRT;
            renderTexture.Release(); 
        }
    }
}
