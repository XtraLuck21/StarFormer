using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class Level2CompletionUI : SchoolCompletionCard
{
    private static readonly Color SeepingInk = new Color(1f, .73f, .73f);
    private static readonly Vector2 ExamRest = new Vector2(73f, -40f);
    private static readonly Vector2 ExamImprintOffset = new Vector2(0f, 30f);

    private Image exam;
    private RectTransform star;

    protected override bool AnimateIllustration => false;

    public void Initialize(Sprite starWithExam, Action backToMap)
    {
        InitializeCard(starWithExam, backToMap, "Level 2 Complete!", null, "Back to Map  \u2192",
            illustrationY: 136f);
        Sprite bareStar = Resources.Load<Sprite>("Level2Complete/ExamStar");
        Sprite heldExam = Resources.Load<Sprite>("Level2Complete/HeldExam");
        if (bareStar != null && heldExam != null)
        {
            Illustration.GetComponent<Image>().enabled = false;
            Vector2 center = Vector2.one * .5f;
            exam = CreateSprite("Exam Paper", heldExam, ExamRest, 243f, center);
            Image starImage = CreateSprite("Exam Star", bareStar, Vector2.zero, 360f, center);
            starImage.color = Color.white;
            star = starImage.rectTransform;
        }

        var colors = NextLevelButton.colors;
        colors.disabledColor = new Color(.61f, .61f, .67f);
        colors.fadeDuration = .16f;
        NextLevelButton.colors = colors;
    }

    protected override IEnumerator PlayResult()
    {
        // Imprint placement for the original combined illustration if the separate sprites are unavailable.
        Vector2 imprint = new Vector2(.202f, -.028f) * Illustration.rect.height;
        if (exam != null)
        {
            yield return PresentExam();
            imprint = exam.rectTransform.anchoredPosition + ExamImprintOffset;
        }

        Sprite ink = Resources.Load<Sprite>("Level2Complete/APlusInk");
        if (ink == null)
        {
            Debug.LogWarning("The Level 2 result is missing its A+ ink sprite.", this);
            yield break;
        }
        Image bleed = CreateSprite("A+ Ink Bleed", ink, imprint, 116f, Vector2.one * .5f);
        Image score = CreateSprite("A+ Stamp", ink, imprint, 116f, Vector2.one * .5f);
        Quaternion inkAngle = Quaternion.Euler(0f, 0f, -10f);
        bleed.rectTransform.localRotation = score.rectTransform.localRotation = inkAngle;

        Sprite wood = Resources.Load<Sprite>("Level2Complete/WoodenStamp");
        if (wood != null)
        {
            Vector2 approach = imprint + new Vector2(144f, 92f);
            // A pivot inside the red pad keeps it over the imprint while the handle squashes.
            Image tool = CreateSprite("Wooden Stamp", wood, approach, 174f, new Vector2(.49f, .22f));
            RectTransform stamp = tool.rectTransform;

            const float fallSeconds = .38f;
            for (float age = 0f; age < fallSeconds; age += Time.unscaledDeltaTime)
            {
                float t = age / fallSeconds;
                float drop = t * t * t;
                stamp.anchoredPosition = Vector2.Lerp(approach, imprint, drop);
                stamp.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-22f, 0f, drop));
                stamp.localScale = Vector3.one * Mathf.Lerp(1.27f, 1f, drop);
                tool.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t * 5f));
                yield return null;
            }

            Vector3 pressed = new Vector3(1.10f, .84f, 1f);
            stamp.anchoredPosition = imprint;
            stamp.localRotation = Quaternion.identity;
            stamp.localScale = pressed;
            tool.color = score.color = Color.white;
            ApplyBleed(0f);
            yield return new WaitForSecondsRealtime(.14f);

            const float liftSeconds = .40f;
            Vector2 departure = imprint + new Vector2(156f, 135f);
            for (float age = 0f; age < liftSeconds; age += Time.unscaledDeltaTime)
            {
                float t = age / liftSeconds;
                float rise = 1f - (1f - t) * (1f - t);
                stamp.anchoredPosition = Vector2.Lerp(imprint, departure, rise);
                stamp.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -19f, rise));
                stamp.localScale = Vector3.Lerp(pressed, Vector3.one, Mathf.Clamp01(t * 4f));
                tool.color = new Color(1f, 1f, 1f, 1f - Mathf.InverseLerp(.55f, 1f, t));
                ApplyBleed(Mathf.Clamp01(t * 2f));
                yield return null;
            }
            tool.enabled = false;
            Destroy(tool.gameObject);
        }
        else
        {
            Debug.LogWarning("The Level 2 result is missing its wooden stamp sprite.", this);
        }

        score.color = Color.white;
        ApplyBleed(1f);

        void ApplyBleed(float spread)
        {
            bleed.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.02f, 1.08f, spread);
            Color tint = SeepingInk;
            tint.a = Mathf.Lerp(.31f, .18f, spread);
            bleed.color = tint;
        }
    }

    private IEnumerator PresentExam()
    {
        RectTransform sheet = exam.rectTransform;
        Vector2 reach = new Vector2(-16f, -7f);
        const float reachSeconds = .16f;
        for (float age = 0f; age < reachSeconds; age += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, age / reachSeconds);
            star.anchoredPosition = reach * t;
            star.localRotation = Quaternion.Euler(0f, 0f, 7f * t);
            yield return null;
        }
        star.anchoredPosition = reach;
        star.localRotation = Quaternion.Euler(0f, 0f, 7f);

        // Pull the sheet clear of the star's right point before bringing it to the front.
        Vector2 tucked = new Vector2(41f, -18f);
        Vector2 clear = new Vector2(265f, -48f);
        const float pullSeconds = .35f;
        for (float age = 0f; age < pullSeconds; age += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, age / pullSeconds);
            sheet.anchoredPosition = Vector2.Lerp(tucked, clear, t);
            sheet.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-34f, -13f, t));
            sheet.localScale = new Vector3(Mathf.Lerp(.42f, 1f, t), Mathf.Lerp(.75f, 1f, t), 1f);
            exam.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t * 8f));
            yield return null;
        }

        sheet.SetAsLastSibling();
        sheet.localScale = Vector3.one;
        exam.color = Color.white;
        const float swingSeconds = .38f;
        for (float age = 0f; age < swingSeconds; age += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, age / swingSeconds);
            float arc = Mathf.Sin(t * Mathf.PI);
            sheet.anchoredPosition = Vector2.Lerp(clear, ExamRest, t) + Vector2.up * (30f * arc);
            sheet.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-13f, 0f, t) + 10f * arc);
            star.anchoredPosition = Vector2.Lerp(reach, Vector2.zero, t);
            star.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(7f, 0f, t));
            yield return null;
        }

        sheet.anchoredPosition = ExamRest;
        sheet.localRotation = Quaternion.identity;
        star.anchoredPosition = Vector2.zero;
        star.localRotation = Quaternion.identity;
        yield return new WaitForSecondsRealtime(.18f);
    }

    private Image CreateSprite(string name, Sprite sprite, Vector2 position, float size, Vector2 pivot)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.SetParent(Illustration, false);
        rect.anchorMin = rect.anchorMax = Vector2.one * .5f;
        rect.pivot = pivot;
        rect.sizeDelta = Vector2.one * size;
        rect.anchoredPosition = position;
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.clear;
        image.raycastTarget = false;
        return image;
    }
}
