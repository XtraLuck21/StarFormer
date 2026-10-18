using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuild
{
    private const string BuildPathArgument = "-customBuildPath";

    [MenuItem("Build/Build WebGL")]
    public static void Build()
    {
        string outputPath = GetArgument(BuildPathArgument);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = "Builds/WebGL";
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes are configured for the WebGL build.");
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log($"Starting WebGL build at {outputPath} with {scenes.Length} scenes.");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log(
            $"WebGL build finished: result={summary.result}, " +
            $"size={summary.totalSize}, duration={summary.totalTime}.");

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"WebGL build failed with result {summary.result}.");
        }
    }

    private static string GetArgument(string argumentName)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int index = 0; index < arguments.Length - 1; index++)
        {
            if (string.Equals(arguments[index], argumentName, StringComparison.Ordinal))
            {
                return arguments[index + 1];
            }
        }

        return string.Empty;
    }
}
