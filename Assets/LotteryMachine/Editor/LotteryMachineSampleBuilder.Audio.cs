using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LotteryMachine.EditorTools
{
    public static partial class LotteryMachineSampleBuilder
    {
        private const string RewardRevealSoundPath = AudioRoot + "/RewardReveal.wav";
        private const int RewardRevealSoundSampleRate = 44100;

        private static AudioClip CreateRewardRevealSoundClip()
        {
            if (!File.Exists(RewardRevealSoundPath))
            {
                File.WriteAllBytes(RewardRevealSoundPath, CreateRewardRevealWavBytes());
                AssetDatabase.ImportAsset(RewardRevealSoundPath, ImportAssetOptions.ForceSynchronousImport);
            }

            ConfigureRewardRevealAudioImporter();

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RewardRevealSoundPath);
            if (clip == null)
            {
                AssetDatabase.ImportAsset(RewardRevealSoundPath, ImportAssetOptions.ForceSynchronousImport);
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RewardRevealSoundPath);
            }

            return clip;
        }

        private static void ConfigureRewardRevealAudioImporter()
        {
            var importer = AssetImporter.GetAtPath(RewardRevealSoundPath) as AudioImporter;
            if (importer == null)
            {
                return;
            }

            importer.forceToMono = true;
            importer.loadInBackground = false;

            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static byte[] CreateRewardRevealWavBytes()
        {
            const float duration = 0.42f;
            const short channels = 1;
            const short bitsPerSample = 16;

            var sampleCount = Mathf.CeilToInt(RewardRevealSoundSampleRate * duration);
            var dataSize = sampleCount * channels * bitsPerSample / 8;
            using var stream = new MemoryStream(44 + dataSize);
            using var writer = new BinaryWriter(stream);

            writer.Write(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            writer.Write(36 + dataSize);
            writer.Write(new byte[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
            writer.Write(new byte[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(RewardRevealSoundSampleRate);
            writer.Write(RewardRevealSoundSampleRate * channels * bitsPerSample / 8);
            writer.Write((short)(channels * bitsPerSample / 8));
            writer.Write(bitsPerSample);
            writer.Write(new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            writer.Write(dataSize);

            for (var i = 0; i < sampleCount; i++)
            {
                var time = i / (float)RewardRevealSoundSampleRate;
                var envelope = Mathf.Exp(-8.5f * time) * Mathf.Clamp01(time / 0.025f);
                var sparkle = Math.Sin(2.0 * Math.PI * 1320.0 * time);
                var chime = Math.Sin(2.0 * Math.PI * 1980.0 * time);
                var lift = Math.Sin(2.0 * Math.PI * 660.0 * time) * Mathf.Clamp01(1f - time / duration);
                var sample = (sparkle * 0.48 + chime * 0.32 + lift * 0.2) * envelope;
                writer.Write((short)Mathf.Clamp(Mathf.RoundToInt((float)sample * short.MaxValue), short.MinValue, short.MaxValue));
            }

            writer.Flush();
            return stream.ToArray();
        }
    }
}
