using UnityEngine;

/// <summary>A complete school-style carriage, visible before any drive is unlocked.</summary>
internal sealed class SeesawLiftAssembly
{
    private readonly Material material;
    private readonly Mesh mesh;
    public SeesawLiftAssembly(Transform platform, SeesawGear2D[] gears, Vector2 size, int layer)
    {
        // Gears supply power only; none of them queues or assembles a piece of the lift.
        foreach (SeesawGear2D gear in gears) gear.WaitingForAssemblyTurn = false;
        material = new Material(Shader.Find("Sprites/Default")) { mainTexture = Texture2D.whiteTexture,
            hideFlags = HideFlags.HideAndDontSave };
        Color ink = new Color32(57,65,65,255), timber = new Color32(213,166,103,255), metal = new Color32(207,208,184,255);
        var art = new SchoolSceneDrawing();
        art.Rect(0f,0f,size.x,size.y,ink);
        art.Rect(0f,.02f,size.x-.08f,size.y-.08f,timber);
        for(int side=-1;side<=1;side+=2)
        {
            float x=side*(size.x*.5f-.07f);
            art.Line(new Vector2(x,0f),new Vector2(x,1.6f),.12f,ink);
            art.Line(new Vector2(x,0f),new Vector2(x,1.6f),.07f,metal);
            art.Line(new Vector2(x,1.6f),new Vector2(side*.8f,1.95f),.12f,ink);
            art.Line(new Vector2(x,1.6f),new Vector2(side*.8f,1.95f),.07f,timber);
            art.Circle(new Vector2(x,.15f),.065f,metal);
        }
        art.Line(new Vector2(-.8f,1.95f),new Vector2(.8f,1.95f),.12f,ink);
        art.Line(new Vector2(-.8f,1.95f),new Vector2(.8f,1.95f),.07f,timber);
        GameObject face=art.Build("Complete elevator carriage",platform,material,4,out mesh);
        face.GetComponent<MeshRenderer>().sortingLayerID=layer;
    }
    public void Dispose()
    {
        SchoolSceneDrawing.Release(mesh);
        SchoolSceneDrawing.Release(material);
    }
}
