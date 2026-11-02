using System;

// Exported alongside the rendered map. Coordinates describe the complete image,
// including padding; no camera-follow or browser-size adjustment is required.
[Serializable]
public sealed class AnalyticsMapCatalog
{
    public AnalyticsMapEntry[] maps;

    public AnalyticsMapEntry Find(string sceneName)
    {
        if (maps == null) return null;
        foreach (AnalyticsMapEntry map in maps)
            if (map.level_name == sceneName) return map;
        return null;
    }
}

[Serializable]
public sealed class AnalyticsMapEntry
{
    public string level_name;
    public string map_version;
    public string image_file;
    public string source_scene_hash;
    public string image_data_url;
    public float min_x, max_x, min_y, max_y;
    public int width, height;
}
