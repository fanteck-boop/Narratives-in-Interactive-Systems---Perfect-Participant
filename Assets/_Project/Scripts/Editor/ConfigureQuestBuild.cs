using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using Unity.XR.Management.AndroidManifest.Editor;

namespace PerfectParticipant.Editor
{
    // Idempotent Quest build setup. Menu or -executeMethod ...ConfigureFromCommandLine.
    public static class ConfigureQuestBuild
    {
        const string AppId = "com.nis.perfectparticipant";
        const string OpenXRLoaderName = "UnityEngine.XR.OpenXR.OpenXRLoader";
        const string SimLoader = "UnityEngine.XR.Simulation.SimulationLoader";

        // ids read from PackageCache (openxr 1.18, xr.hands 1.9)
        static readonly string[] Features =
        {
            "com.unity.openxr.feature.metaquest",                   // Meta Quest Support (Android only)
            "com.unity.openxr.feature.input.handtrackingsubsystem", // Hand Tracking Subsystem
            "com.unity.openxr.feature.input.metahandtrackingaim",   // Meta Hand Tracking Aim
            "com.unity.openxr.feature.input.oculustouch",
            "com.unity.openxr.feature.input.metaquestpro",          // Touch Pro
            "com.unity.openxr.feature.input.metaquestplus",         // Touch Plus
            "com.unity.openxr.feature.input.handinteraction",       // Hand Interaction Profile
        };

        [MenuItem("Perfect Participant/Configure Quest Build")]
        public static void Configure()
        {
            Player();
            foreach (var g in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone }) Xr(g);
            AssetDatabase.SaveAssets();
            Report();
            // ponytail: switch costs a full reimport, so CLI skips it unless -switchAndroid
            if (!Application.isBatchMode || Environment.CommandLine.Contains("-switchAndroid")) Switch();
        }

        public static void ConfigureFromCommandLine()
        {
            try { Configure(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[ConfigureQuest] " + e); EditorApplication.Exit(1); }
        }

        static void Player()
        {
            PlayerSettings.companyName = "NIS";
            PlayerSettings.productName = "The Perfect Participant";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AppId);
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29; // Quest 2/3 baseline (Android 10)
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            // Meta feature validation demands these
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        }

        static void Xr(BuildTargetGroup g)
        {
            var gen = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(g);
            if (gen == null) { Debug.LogWarning($"[ConfigureQuest] no XR general settings for {g}"); return; }
            gen.InitManagerOnStart = true;
            var mgr = gen.Manager;
            XRPackageMetadataStore.AssignLoader(mgr, OpenXRLoaderName, g);
            XRPackageMetadataStore.RemoveLoader(mgr, SimLoader, g); // no-op if absent

            FeatureHelpers.RefreshFeatures(g);
            foreach (var id in Features)
            {
                var f = FeatureHelpers.GetFeatureWithIdForBuildTarget(g, id);
                if (f == null) { Debug.Log($"[ConfigureQuest] {g}: feature {id} n/a"); continue; }
                f.enabled = true;
                EditorUtility.SetDirty(f);
            }
            var s = OpenXRSettings.GetSettingsForBuildTargetGroup(g);
            s.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            if (g == BuildTargetGroup.Android) s.latencyOptimization = OpenXRSettings.LatencyOptimization.PrioritizeInputPolling;
            EditorUtility.SetDirty(s);
            EditorUtility.SetDirty(gen);
        }

        static void Switch()
        {
            var android = Path.Combine(Path.GetDirectoryName(EditorApplication.applicationPath), "Data", "PlaybackEngines", "AndroidPlayer");
            if (!Directory.Exists(android))
            {
                Debug.LogWarning("[ConfigureQuest] Android module missing: install Android Build Support + SDK/NDK/JDK via Unity Hub");
                return;
            }
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        static void Report()
        {
            var sb = new System.Text.StringBuilder("[ConfigureQuest] readback\n");
            void L(string k, object v) => sb.AppendLine($"  {k} = {v}");
            L("company/product", PlayerSettings.companyName + " / " + PlayerSettings.productName);
            L("appId Android", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            L("appId Standalone", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone));
            L("colorSpace", PlayerSettings.colorSpace);
            L("minSdk/targetSdk", $"{PlayerSettings.Android.minSdkVersion} / {PlayerSettings.Android.targetSdkVersion}");
            L("backend", PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android));
            L("arch", PlayerSettings.Android.targetArchitectures);
            L("gfx", string.Join(",", PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)));
            L("entry", PlayerSettings.Android.applicationEntry);
            L("orientation", PlayerSettings.defaultInterfaceOrientation);
            foreach (var g in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                var gen = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(g);
                var ls = new List<string>();
                if (gen != null) foreach (var l in gen.Manager.activeLoaders) ls.Add(l.GetType().Name);
                L(g + " loaders", string.Join(",", ls));
                var s = OpenXRSettings.GetSettingsForBuildTargetGroup(g);
                L(g + " renderMode", s.renderMode);
                foreach (var id in Features)
                {
                    var f = FeatureHelpers.GetFeatureWithIdForBuildTarget(g, id);
                    L(g + " " + id, f == null ? "n/a" : f.enabled ? "ON" : "off");
                }
            }
            Debug.Log(sb.ToString());
        }
    }

    // Hand tracking manifest bits: Meta OpenXR 2.6.1 / OpenXR 1.18 expose no option. Hands optional, controllers still work.
    public class HandTrackingManifest : IAndroidManifestRequirementProvider
    {
        public ManifestRequirement ProvideManifestRequirement()
        {
            ManifestElement E(string tag, string name, string req = null)
            {
                var a = new Dictionary<string, string> { { "name", name } };
                if (req != null) a["required"] = req;
                return new ManifestElement { ElementPath = new List<string> { "manifest", tag }, Attributes = a };
            }
            return new ManifestRequirement
            {
                SupportedXRLoaders = new HashSet<Type> { typeof(OpenXRLoader) },
                NewElements = new List<ManifestElement>
                {
                    E("uses-feature", "oculus.software.handtracking", "false"),
                    E("uses-permission", "com.oculus.permission.HAND_TRACKING"),
                },
            };
        }
    }
}
