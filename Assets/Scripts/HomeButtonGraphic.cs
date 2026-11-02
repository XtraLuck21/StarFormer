using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Simple geometry keeps the house/star readable without imported artwork or font glyphs.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class HomeButtonGraphic : MaskableGraphic, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public GameObject Tooltip { get; set; }
    private bool available = true;
    private bool hovered;
    private bool selected;
    private bool pressed;

    public void SetAvailable(bool value)
    {
        if (available == value) return;
        available = value;
        if (!value) hovered = selected = pressed = false;
        Refresh();
    }
    private void Refresh()
    {
        if (Tooltip != null) Tooltip.SetActive(available && (hovered || selected));
        rectTransform.localScale = Vector3.one * (pressed ? .94f : 1f);
        SetVerticesDirty();
    }
    public void OnPointerEnter(PointerEventData e) { hovered = available; Refresh(); }
    public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; Refresh(); }
    public void OnPointerDown(PointerEventData e) { pressed = available && e.button == PointerEventData.InputButton.Left; Refresh(); }
    public void OnPointerUp(PointerEventData e) { pressed = false; Refresh(); }
    public void OnSelect(BaseEventData e) { selected = available; Refresh(); }
    public void OnDeselect(BaseEventData e) { selected = false; Refresh(); }
    protected override void OnDisable() { hovered = selected = pressed = false; Refresh(); base.OnDisable(); }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Color pale = available ? new Color(.94f, .95f, .96f) : new Color(.45f, .48f, .51f);
        Color gold = available ? new Color(1, .82f, .25f) : new Color(.5f, .43f, .22f);
        Quad(vh, new Vector2(-.5f,-.5f), new Vector2(.5f,-.5f), new Vector2(.5f,.5f), new Vector2(-.5f,.5f), available && (hovered || selected) ? gold : pale);
        Quad(vh, new Vector2(-.465f,-.465f), new Vector2(.465f,-.465f), new Vector2(.465f,.465f), new Vector2(-.465f,.465f), new Color(.08f,.1f,.13f));
        Line(vh, new Vector2(-.31f,.03f), new Vector2(0,.3f), pale);
        Line(vh, new Vector2(0,.3f), new Vector2(.31f,.03f), pale);
        Line(vh, new Vector2(-.235f,.09f), new Vector2(-.235f,-.28f), pale);
        Line(vh, new Vector2(-.235f,-.28f), new Vector2(.235f,-.28f), pale);
        Line(vh, new Vector2(.235f,-.28f), new Vector2(.235f,.09f), pale);
        Vector2 center = new Vector2(0,-.075f);
        for (int i=0;i<10;i++)
        {
            float a=(90+i*36)*Mathf.Deg2Rad, b=(90+(i+1)*36)*Mathf.Deg2Rad;
            Vector2 p=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?.135f:.06f);
            Vector2 q=center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(i%2==0?.06f:.135f);
            int start=vh.currentVertCount;Vertex(vh,center,gold);Vertex(vh,p,gold);Vertex(vh,q,gold);vh.AddTriangle(start,start+1,start+2);
        }
    }
    private void Line(VertexHelper vh, Vector2 a, Vector2 b, Color tint)
    {
        Vector2 d=(b-a).normalized;Vector2 n=new Vector2(-d.y,d.x)*.023f;
        Quad(vh,a-n,b-n,b+n,a+n,tint);
    }
    private void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        int start=vh.currentVertCount;Vertex(vh,a,tint);Vertex(vh,b,tint);Vertex(vh,c,tint);Vertex(vh,d,tint);
        vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
    }
    private void Vertex(VertexHelper vh, Vector2 point, Color tint)
    {
        Rect r=rectTransform.rect;
        vh.AddVert(new Vector3(r.center.x+point.x*r.width,r.center.y+point.y*r.height),tint,Vector2.zero);
    }
}
