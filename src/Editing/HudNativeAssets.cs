using System;
using System.Collections.Generic;
using AssetBundles;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OC2HudCustomization
{
    // Retain only the native artwork/font assets. No game menu, listeners, or business
    // components are cloned into the editor, and no shared asset is modified.
    internal sealed class HudNativeAssets : IDisposable
    {
        private readonly Dictionary<Object, Object> _copies = new Dictionary<Object, Object>();
        private readonly List<Object> _owned = new List<Object>();
        private bool _fontLease;
        internal Sprite Paper, Header, Button;
        internal Font BodyFont, HeaderFont;
        internal bool Ready { get { return Paper && BodyFont && HeaderFont; } }
        internal void Capture(FrontendRootMenu root)
        {
            if (Ready || !root) return;
            Transform options = root.transform.Find("FixedAspectRootCanvas/ScreenSpaceCanvas/GameOptions");
            if (!options) return;
            try
            {
                Transform body = options.Find("SettingsBody");
                Paper = CopySprite(body.GetComponent<Image>().sprite);
                Header = CopySprite(body.Find("HeaderBacker").GetComponent<Image>().sprite);
                T17Button button = body.Find("ContentPC/Viewport/Content/ScreenAdjuster").GetComponent<T17Button>();
                Image backer = button.targetGraphic as Image;
                Button = backer ? CopySprite(backer.sprite) : null;
                HeaderFont = KeepFont(body.Find("HeaderBacker/Header").GetComponent<T17Text>().font);
                BodyFont = KeepFont(button.GetComponentsInChildren<T17Text>(true)[0].font);
            }
            catch { Dispose(); throw; }
        }
        private Font KeepFont(Font font)
        {
            string path = font.name == "Header_Font" ? "assets/gui/fonts/header_font.otf"
                : font.name == "Body_Font" ? "assets/gui/fonts/body_font.ttf" : null;
            string error;
            LoadedAssetBundle bundle = AssetBundleManager.GetLoadedAssetBundle("bundle22", out error);
            if (path == null || bundle == null || bundle.m_AssetBundle.LoadAsset<Font>(path) != font)
                throw new InvalidOperationException("HUD native font identity changed: " + font.name + "; " + error);
            // Unity 2017 copies share atlas state. Keep the bundle and its fallback dependencies.
            if (!_fontLease) { AssetBundleManager.LoadAssetAsync("bundle22", path, typeof(Font)); _fontLease = true; }
            return font;
        }
        private T Own<T>(Object source, T copy) where T : Object
        { copy.name = source.name + " (HUD editor)"; _copies.Add(source, copy); _owned.Add(copy); return copy; }
        private Sprite CopySprite(Sprite source)
        {
            if (!source) return null;
            Object found; if (_copies.TryGetValue(source, out found)) return (Sprite)found;
            Rect r = source.rect;
            return Own(source, Sprite.Create(CopyTexture(source.texture), r,
                new Vector2(source.pivot.x / r.width, source.pivot.y / r.height), source.pixelsPerUnit, 0, SpriteMeshType.FullRect, source.border));
        }
        private Texture2D CopyTexture(Texture2D source)
        {
            Object found; if (_copies.TryGetValue(source, out found)) return (Texture2D)found;
            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture staging = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            try
            {
                Graphics.Blit(source, staging); RenderTexture.active = staging;
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); copy.Apply(false, true);
                copy.filterMode = source.filterMode; copy.wrapMode = source.wrapMode; return Own(source, copy);
            }
            catch { Object.Destroy(copy); throw; }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(staging); }
        }
        public void Dispose()
        {
            for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i]) Object.Destroy(_owned[i]);
            _copies.Clear(); _owned.Clear(); Paper = Header = Button = null; BodyFont = HeaderFont = null;
            if (_fontLease) { _fontLease = false; AssetBundleManager.UnloadAssetBundle("bundle22"); }
        }
    }
}
