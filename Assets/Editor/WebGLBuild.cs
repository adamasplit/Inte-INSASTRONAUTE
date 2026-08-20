using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class WebGLBuild
{
    public static void Build()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "build/game",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"WebGL build failed: {report.summary.result}");
    }
}
