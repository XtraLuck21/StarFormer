using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared static collection used by Navigation and scene entry buttons.
public sealed class CollectionBookOverview : IDisposable
{
    private sealed class Card
    {
        public HelpBookUI.HelpPage Page;
        public Image Background, Icon, Footer;
        public TextMeshProUGUI Title, Status, Quote;
        public CollectionBookGlyph Badge, Progress;
        public GameObject Controls, LockedHint;
        public Sprite Portrait;
        public Color Tint;
    }
    private readonly List<Card> cards = new List<Card>();
    private readonly List<Sprite> ownedSprites = new List<Sprite>();
    private readonly TextMeshProUGUI progress;
    private readonly Sprite paper, mystery;
    private readonly TMP_FontAsset font;
    private static readonly Color Ink = new Color32(78, 49, 26, 255);
    private static readonly Color Gold = new Color32(246, 181, 41, 255);
    private static readonly Color[] Tints = {
        new Color32(255, 239, 185, 255), new Color32(255, 224, 194, 255),
        new Color32(212, 234, 255, 255), new Color32(237, 216, 251, 255),
        new Color32(255, 229, 187, 255), new Color32(215, 240, 223, 255)
    };

    public CollectionBookOverview(RectTransform parent, TMP_FontAsset font,
        HelpBookUI.HelpPage[] pages, Texture2D art, Texture2D paperTexture, Texture2D spiderArt)
    {
        this.font = font;
        if (paperTexture != null)
        {
            paper = Own(Sprite.Create(paperTexture, new Rect(0, 0, paperTexture.width, paperTexture.height),
                Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect,
                new Vector4(paperTexture.width * .10f, paperTexture.height * .12f,
                    paperTexture.width * .10f, paperTexture.height * .12f)));
        }
        Sprite Portrait(int index)
        {
            // The orange portrait matches Spider.prefab; retain the original atlas for other forms.
            if (index == 4 && spiderArt != null)
                return Own(Sprite.Create(spiderArt, new Rect(0, 0, spiderArt.width, spiderArt.height),
                    Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect));
            if (art == null) return null;
            float cellWidth = art.width / 4f, cellHeight = art.height / 2f;
            // Grid slicing keeps the original generated art intact.
            return Own(Sprite.Create(art, new Rect((index % 4 + .10f) * cellWidth, (1 - index / 4 + .10f) * cellHeight,
                cellWidth * .80f, cellHeight * .80f), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect));
        }
        mystery = Portrait(6);
        var frameImage = parent.GetComponent<Image>();
        if (frameImage != null) { frameImage.sprite = paper; frameImage.type = Image.Type.Sliced; frameImage.color = Color.white; frameImage.pixelsPerUnitMultiplier = 2.5f; }
        var root = Rect("Collection Overview", parent, Vector2.zero, Vector2.one);
        Text("Title", root, "COLLECTION BOOK", 43, .035f, .89f, .84f, .975f, FontStyles.Bold);
        progress = Text("Discovery Count", root, "", 23, .038f, .835f, .35f, .895f);
        Glyph("Title underline", root, CollectionBookGlyph.Symbol.Rule, .035f, .891f, .56f, .896f).color = Gold;
        var grid = Rect("Forms", root, new Vector2(.018f, .028f), new Vector2(.982f, .825f));
        if (pages == null) return;
        int rows = Mathf.Max(1, Mathf.CeilToInt(pages.Length / 3f));
        for (int i = 0; i < pages.Length; i++)
        {
            var page = pages[i];
            if (page == null) continue;
            int column = i % 3, row = i / 3;
            var tile = Rect(page.unlockId + " Card", grid,
                new Vector2(column / 3f, 1f - (row + 1f) / rows),
                new Vector2((column + 1f) / 3f, 1f - row / (float)rows));
            tile.offsetMin = new Vector2(6, 6);
            tile.offsetMax = new Vector2(-6, -6);
            var bg = Paper(tile, Color.white);
            var icon = Rect("Form Icon", tile, new Vector2(.035f, .635f), new Vector2(.30f, .965f)).gameObject.AddComponent<Image>();
            icon.preserveAspect = true; icon.raycastTarget = false;
            var title = Text("Form Name", tile, "", 31, .32f, .76f, .95f, .93f, FontStyles.Bold);
            var badge = Glyph("Status Badge", tile, CollectionBookGlyph.Symbol.Lock, .32f, .65f, .365f, .72f);
            var status = Text("Unlock Status", tile, "", 18, .38f, .64f, .96f, .73f, FontStyles.Bold);
            Glyph("Divider", tile, CollectionBookGlyph.Symbol.Rule, .06f, .615f, .94f, .622f).color = new Color32(156, 124, 82, 160);
            var controls = Rect("Controls", tile, new Vector2(.065f, .25f), new Vector2(.935f, .60f));
            BuildControls(controls, page.collectionControls);
            Text("Passive Note", controls, page.collectionNote ?? "", 17, 0, -.24f, 1, -.03f, FontStyles.Italic).alignment = TextAlignmentOptions.Center;
            var hint = Text("Locked Hint", tile, "Possess a new form in a level\nto discover its abilities.", 23, .09f, .30f, .91f, .55f);
            hint.alignment = TextAlignmentOptions.Center;
            var footerRect = Rect("Footer", tile, new Vector2(.055f, .05f), new Vector2(.945f, .17f));
            var footer = Paper(footerRect, Color.white); footer.pixelsPerUnitMultiplier = 10;
            var quote = Text("Flavor", footerRect, "", 19, .04f, .06f, .96f, .94f, FontStyles.Italic);
            quote.alignment = TextAlignmentOptions.Center;
            var dot = Glyph("Progress " + i, root, CollectionBookGlyph.Symbol.Dot,
                .36f + i * .024f, .854f, .373f + i * .024f, .878f);
            int spriteIndex = SpriteIndex(page.unlockId);
            cards.Add(new Card { Page = page, Background = bg, Icon = icon, Title = title, Status = status,
                Badge = badge, Controls = controls.gameObject, LockedHint = hint.gameObject, Quote = quote,
                Footer = footer, Progress = dot, Portrait = spriteIndex < 0 ? page.icon : Portrait(spriteIndex),
                Tint = Tints[Mathf.Clamp(spriteIndex, 0, Tints.Length - 1)] });
        }
        Refresh();
    }

    private void BuildControls(RectTransform parent, HelpBookUI.CollectionControl[] controls)
    {
        if (controls == null) return;
        for (int i = 0; i < controls.Length; i++)
        {
            var control = controls[i];
            float top = 1f - i / 3f;
            var row = Rect("Control " + i, parent, new Vector2(0, top - .30f), new Vector2(1, top));
            float x = 0;
            if (!string.IsNullOrEmpty(control.keys))
            {
                foreach (string key in control.keys.Split('|'))
                {
                    if (key == "WASD")
                    {
                        string[] letters = { "W", "A", "S", "D" };
                        for (int k = 0; k < letters.Length; k++)
                        {
                            float left = x + (k == 0 ? 1 : k - 1) * .09f;
                            float bottom = k == 0 ? .52f : 0f;
                            var clusterKey = Glyph(letters[k] + " key", row, CollectionBookGlyph.Symbol.Key,
                                left, bottom, left + .082f, bottom + .48f);
                            Text("Key label", clusterKey.transform, letters[k], 15, .03f, .08f, .97f, .98f, FontStyles.Bold).alignment = TextAlignmentOptions.Center;
                        }
                        x += .29f;
                        continue;
                    }
                    float width = key == "SPACE" ? .30f : .095f;
                    var cap = Glyph(key + " key", row, CollectionBookGlyph.Symbol.Key, x, .04f, x + width, .96f);
                    Text("Key label", cap.transform, key, key == "WASD" ? 19 : 22, .03f, .10f, .97f, .98f, FontStyles.Bold).alignment = TextAlignmentOptions.Center;
                    x += width + .024f;
                }
            }
            if (control.mouse >= 0)
            {
                if (x > 0) { Text("Alternative", row, "or", 16, x, .06f, x + .065f, .94f); x += .07f; }
                Glyph("Mouse binding", row, control.mouse == 2 ? CollectionBookGlyph.Symbol.Aim :
                    control.mouse == 0 ? CollectionBookGlyph.Symbol.MouseLeft : CollectionBookGlyph.Symbol.MouseRight,
                    x, -.02f, x + .08f, 1.02f);
                x += .10f;
            }
            if (control.hold)
            {
                var hold = Text("Hold", row, "HOLD", 12, x, .18f, x + .115f, .82f, FontStyles.Bold);
                hold.color = new Color32(151, 95, 20, 255);
            }
            Text("Action", row, control.action, 22, .49f, .02f, 1f, .98f);
        }
    }

    public void Refresh()
    {
        int count = 0;
        foreach (var card in cards)
        {
            bool unlocked = HelpBookUnlocks.IsUnlocked(card.Page.unlockId);
            if (unlocked) count++;
            card.Background.color = unlocked ? card.Tint : new Color32(232, 225, 214, 255);
            card.Title.text = unlocked ? card.Page.title.ToUpperInvariant() : "???";
            card.Icon.sprite = unlocked ? card.Portrait : mystery;
            card.Icon.enabled = card.Icon.sprite != null;
            card.Icon.color = Color.white;
            card.Status.text = unlocked ? "DISCOVERED" : "UNDISCOVERED";
            card.Badge.Kind = unlocked ? CollectionBookGlyph.Symbol.Check : CollectionBookGlyph.Symbol.Lock;
            card.Badge.SetVerticesDirty();
            card.Controls.SetActive(unlocked);
            card.LockedHint.SetActive(!unlocked);
            card.Quote.text = unlocked ? card.Page.collectionQuote : "Find a new form to reveal it.";
            card.Footer.color = unlocked ? Color.Lerp(card.Tint, Gold, .14f) : new Color32(222, 210, 192, 255);
            card.Progress.color = unlocked ? Gold : new Color32(192, 177, 155, 255);
        }
        progress.text = $"{count} / {cards.Count} forms discovered";
    }

    public void StyleClose(Button button)
    {
        var image = button.GetComponent<Image>();
        if (image == null) return;
        image.sprite = paper;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 5;
        image.color = Color.white;
    }

    private Sprite Own(Sprite sprite) { ownedSprites.Add(sprite); return sprite; }
    public void Dispose() { foreach (var sprite in ownedSprites) if (sprite != null) UnityEngine.Object.Destroy(sprite); ownedSprites.Clear(); }
    private static int SpriteIndex(string id) => id switch { "Star" => 0, "Box" => 1, "Bubble" => 2, "BouncyBall" => 3, "Spider" => 4, "Fan" => 5, _ => -1 };
    private Image Paper(RectTransform rect, Color tint)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = paper; image.type = Image.Type.Sliced; image.color = tint;
        image.pixelsPerUnitMultiplier = 5; image.raycastTarget = false;
        return image;
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
    private CollectionBookGlyph Glyph(string name, Transform parent, CollectionBookGlyph.Symbol kind, float x0, float y0, float x1, float y1)
    {
        var glyph = Rect(name, parent, new Vector2(x0, y0), new Vector2(x1, y1)).gameObject.AddComponent<CollectionBookGlyph>();
        glyph.Kind = kind; glyph.Ink = Ink; glyph.raycastTarget = false;
        return glyph;
    }
    private TextMeshProUGUI Text(string name, Transform parent, string value, float size,
        float x0, float y0, float x1, float y1, FontStyles style = FontStyles.Normal)
    {
        var text = Rect(name, parent, new Vector2(x0, y0), new Vector2(x1, y1)).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.text = value; text.color = Ink; text.fontSize = size;
        text.enableAutoSizing = true; text.fontSizeMin = size * .80f; text.fontSizeMax = size;
        text.fontStyle = style; text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
        return text;
    }
}
