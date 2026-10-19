using UnityEditor;

namespace ChastainRush
{
    // Round 51: import settings for Dan's title art (Assets/Resources/Title/, copied unaltered from SourceArt/Title/; the two
    // sounds are the MP3s decoded to WAV so the theme loops with no MP3 padding gap). The cover is 1672 x 941: kept at that size
    // (no power-of-two rescale, no mipmaps, uncompressed, bilinear), as Racer's Resources/Title/Artwork. The sounds load
    // whole, uncompressed, as Racer's title voice and theme.
    public sealed class TitleArtImport : AssetPostprocessor
    {
        const string Folder = "Assets/Resources/Title/";
        const string Icon = "Assets/HandMade/Title/GameIcon.png";   // the exe's icon (PlayerBuild), kept sharp

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) && assetPath != Icon) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Default; t.npotScale = TextureImporterNPOTScale.None; t.mipmapEnabled = false;
            t.sRGBTexture = true; t.alphaIsTransparency = false; t.maxTextureSize = 2048;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.wrapMode = UnityEngine.TextureWrapMode.Clamp; t.filterMode = UnityEngine.FilterMode.Bilinear;
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var a = (AudioImporter)assetImporter;
            var s = a.defaultSampleSettings;
            s.loadType = UnityEngine.AudioClipLoadType.DecompressOnLoad; s.compressionFormat = UnityEngine.AudioCompressionFormat.PCM; s.preloadAudioData = true;
            a.defaultSampleSettings = s; a.forceToMono = false; a.loadInBackground = true;
        }
    }
}
