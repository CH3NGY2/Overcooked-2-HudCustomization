using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UI;

// Only Unity native object/transform calls are replaced in a temporary copy of the built HUD DLL.
// The production Measure and HudRowLayout.Place methods run unchanged against supplied geometry.
public static class RecipeBoundsBoundaryTests
{
    private sealed class Identity : IEqualityComparer<object>
    {
        public new bool Equals(object a, object b) { return ReferenceEquals(a, b); }
        public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
    }
    private static readonly Dictionary<object, RectTransform> transforms = new Dictionary<object, RectTransform>(new Identity());
    private static readonly Dictionary<object, GameObject> objects = new Dictionary<object, GameObject>(new Identity());
    private static readonly Dictionary<object, Vector3[]> corners = new Dictionary<object, Vector3[]>(new Identity());
    private static readonly Dictionary<object, List<Graphic>> descendants = new Dictionary<object, List<Graphic>>(new Identity());
    private static readonly Dictionary<object, bool> active = new Dictionary<object, bool>(new Identity());
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static int checks;
    private static Vector2 pixelOffset;

    public static bool Alive(UnityEngine.Object value) { return !ReferenceEquals(value, null); }
    public static Transform TransformOf(Component value) { return transforms[value]; }
    public static GameObject ObjectOf(Component value) { return objects[value]; }
    public static bool Active(GameObject value) { return active[value]; }
    public static void SetActive(GameObject value, bool visible) { active[value] = visible; }
    public static Vector3 ToLocal(Transform space, Vector3 value) { return value; }
    public static void Corners(RectTransform rect, Vector3[] output) { corners[rect].CopyTo(output, 0); }
    public static void Collect(GameObject root, bool includeInactive, List<Graphic> output)
    { output.Clear(); output.AddRange(descendants[root]); }
    public static bool GraphicActive(Behaviour value) { return active[objects[value]]; }
    public static RectTransform GraphicRect(Graphic value) { return transforms[value]; }
    private static Graphic Draw(RectTransform rect)
    {
        Image graphic = Bare<Image>(); transforms.Add(graphic, rect); objects.Add(graphic, objects[rect]); return graphic;
    }
    public static T ComponentOf<T>(GameObject value) { return default(T); }
    public static int AnimationLayer(Animator value, string name) { return -1; }
    public static AnimatorStateInfo AnimationState(Animator value, int layer) { return new AnimatorStateInfo(); }
    public static Vector2 GetOffset(RectTransformExtension extension) { return pixelOffset; }
    public static void SetOffset(RectTransformExtension extension, Vector2 value) { pixelOffset = value; }

    private static T Bare<T>() { return (T)FormatterServices.GetUninitializedObject(typeof(T)); }
    private static RectTransform RectAt(Rect rect, bool visible)
    {
        RectTransform transform = Bare<RectTransform>(); GameObject go = Bare<GameObject>();
        transforms.Add(transform, transform); objects.Add(transform, go); active.Add(go, visible);
        SetRect(transform, rect); return transform;
    }
    private static void SetRect(RectTransform transform, Rect rect)
    {
        corners[transform] = new Vector3[] { new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMin, rect.yMax),
            new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMax, rect.yMin) };
    }
    private static void Check(bool passed, string message)
    { checks++; Console.WriteLine((passed ? "PASS " : "FAIL ") + message); if (!passed) throw new Exception(message); }

    private static object Card(Type type, float scale, out Rect full, out RectTransform stitch, out GameObject generated)
    { return Card(type, scale, false, out full, out stitch, out generated); }
    private static object Card(Type type, float scale, bool head, out Rect full, out RectTransform stitch, out GameObject generated)
    {
        float width = 200f * scale, height = 180f * scale;
        RectTransform tile = RectAt(new Rect(0, -height, width, height), true);
        // Native BackgroundTop has a fixed pixel size and negative Y scale: it extends above its tile.
        stitch = RectAt(new Rect(0, 0, width, 53f), true);
        RectTransform pan = RectAt(new Rect(width * .4f, -height, width * .2f, 40f * scale), true);
        RectTransform hidden = RectAt(new Rect(-5000f, -5000f, 10000f, 10000f), false);
        generated = Bare<GameObject>(); active.Add(generated, true);
        Graphic paperGraphic = Draw(tile);
        Graphic stitchGraphic = Draw(stitch);
        descendants.Add(generated, new List<Graphic> { paperGraphic, stitchGraphic, Draw(pan), Draw(hidden) });
        RecipeWidgetTile recipe = head ? Bare<TopRecipeWidgetTile>() : Bare<RecipeWidgetTile>(); transforms.Add(recipe, tile);
        if (head)
        {
            typeof(RecipeWidgetTile).GetField("m_background", Members).SetValue(recipe, paperGraphic);
            typeof(RecipeWidgetTile).GetField("m_backgroundTop", Members).SetValue(recipe, stitchGraphic);
            descendants[generated].Add(Draw(RectAt(new Rect(0, -10f, width, 30f), true)));
        }
        typeof(UISubElementContainer).GetField("m_container", Members).SetValue(recipe, generated);
        object card = FormatterServices.GetUninitializedObject(type);
        foreach (FieldInfo field in type.GetFields(Members))
        {
            if (field.FieldType == typeof(Vector3[])) field.SetValue(card, new Vector3[4]);
            else if (field.FieldType == typeof(List<RecipeWidgetTile>)) field.SetValue(card, new List<RecipeWidgetTile> { recipe });
            else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>)) field.SetValue(card, Activator.CreateInstance(field.FieldType));
            else if (field.Name == "_motion") field.SetValue(card, Activator.CreateInstance(field.FieldType, Members, null, new object[] { false }, null));
            else if (field.Name == "_container") field.SetValue(card, typeof(UISubElementContainer).GetField("m_container", Members));
            else if (field.Name == "_headPaperField") field.SetValue(card, typeof(RecipeWidgetTile).GetField("m_background", Members));
            else if (field.Name == "_topPaper") field.SetValue(card, typeof(RecipeWidgetTile).GetField("m_backgroundTop", Members));
            else if (field.Name == "_boundsDirty") field.SetValue(card, true);
            else if (field.Name == "_trimmed") field.SetValue(card, head);
        }
        full = Rect.MinMaxRect(0, -height, width, 53f); return card;
    }
    private static Rect Measure(object card, Transform space)
    {
        object[] arguments = { space, new Rect(), new Rect() };
        Check((bool)card.GetType().GetMethod("Measure", Members).Invoke(card, arguments), "production Measure found finite card bounds");
        return (Rect)arguments[1];
    }
    private static float Place(object layout, float width, float height)
    {
        object[] arguments = { width, height, 0f, 0f };
        layout.GetType().GetMethod("Place", Members).Invoke(layout, arguments);
        return (float)arguments[3];
    }
    private static void Run(string assemblyPath)
    {
        Assembly plugin = Assembly.LoadFrom(assemblyPath);
        Type cardType = plugin.GetType("OC2HudCustomization.RecipeCardState", true);
        Type layoutType = plugin.GetType("OC2HudCustomization.HudRowLayout", true);
        Transform flow = Bare<RectTransform>();
        foreach (float scale in new float[] { .5f, 1f, 2f })
        {
            Rect firstFull, secondFull; RectTransform firstStitch, secondStitch; GameObject firstRoot, secondRoot;
            object first = Card(cardType, scale, out firstFull, out firstStitch, out firstRoot);
            object second = Card(cardType, scale, out secondFull, out secondStitch, out secondRoot);
            Rect a = Measure(first, flow), b = Measure(second, flow);
            object layout = Activator.CreateInstance(layoutType, Members, null, new object[] { a.xMin, a.yMax, 1, 0, 5f, 8f, 2000f }, null);
            float firstTop = Place(layout, a.width, a.height), nextTop = Place(layout, b.width, b.height);
            float lowerPaperTop = secondFull.yMax + nextTop - b.yMax;
            float upperPanBottom = firstFull.yMin + firstTop - a.yMax;
            Console.WriteLine("scale=" + scale + " measuredY=[" + a.yMin + "," + a.yMax + "] upperPanBottom=" + upperPanBottom + " nextPaperTop=" + lowerPaperTop);
            Check(lowerPaperTop <= upperPanBottom - 8f + .001f, "next row paper clears previous cookware by configured gap at scale " + scale);
            Check(Math.Abs(a.yMax - firstFull.yMax) < .001f && Math.Abs(a.yMin - firstFull.yMin) < .001f,
                "includes actual stitched paper and cookware, excludes inactive geometry");
            // Existing cached objects move/resize: no rediscovery is required to measure fresh corners.
            SetRect(firstStitch, new Rect(0, 0, 200f * scale, 73f));
            Check(Math.Abs(Measure(first, flow).yMax - 73f) < .001f, "geometry is measured again after transform changes");
            MethodInfo invalidate = cardType.GetMethod("InvalidateBounds", Members);
            if (invalidate != null)
            {
                descendants[firstRoot].RemoveAll(delegate(Graphic graphic) { return ReferenceEquals(transforms[graphic], firstStitch); });
                descendants[firstRoot].Add(Draw(RectAt(new Rect(0, -250f * scale, 200f * scale, 30f), true)));
                invalidate.Invoke(first, null);
                Check(Math.Abs(Measure(first, flow).yMin + 250f * scale) < .001f, "refresh invalidates cached children and includes new lower recipe branch");
            }
            Rect headFull; RectTransform headStitch; GameObject headRoot;
            object head = Card(cardType, scale, true, out headFull, out headStitch, out headRoot);
            Rect trimmed = Measure(head, flow);
            Check(Math.Abs(trimmed.yMax) < .001f && Math.Abs(trimmed.yMin + 180f * scale) < .001f,
                "trimmed bounds use the main paper edge instead of projecting the branch stitch above it at scale " + scale);
            Check(!active[objects[headStitch]], "only head extension is hidden");
            System.Collections.IEnumerable owned = (System.Collections.IEnumerable)cardType.GetField("_headers", Members).GetValue(head);
            foreach (object property in owned) property.GetType().GetMethod("Restore", Members).Invoke(property, null);
            cardType.GetField("_trimmed", Members).SetValue(head, false);
            Check(active[objects[headStitch]] && Math.Abs(Measure(head, flow).yMax - 53f) < .001f,
                "restoration returns the native head paper and its original bounds");
            cardType.GetField("_trimmed", Members).SetValue(head, true);
            cardType.GetMethod("InvalidateBounds", Members).Invoke(head, null);
            Check(Math.Abs(Measure(head, flow).yMax) < .001f, "rebuild applies trimming to refreshed head");
        }
    }
    private static void Entrance(string assemblyPath)
    {
        Assembly plugin = Assembly.LoadFrom(assemblyPath);
        Type cardType = plugin.GetType("OC2HudCustomization.RecipeCardState", true);
        object card = FormatterServices.GetUninitializedObject(cardType);
        foreach (FieldInfo field in cardType.GetFields(Members))
            if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>)) field.SetValue(card, Activator.CreateInstance(field.FieldType));
        cardType.GetField("_extension", Members).SetValue(card, Bare<RectTransformExtension>());
        FieldInfo motion = cardType.GetField("_motion", Members);
        if (motion != null) motion.SetValue(card, Activator.CreateInstance(motion.FieldType, Members, null, new object[] { true }, null));
        MethodInfo move = cardType.GetMethod("Move", Members);
        object[] arguments = { new Vector2(0, -300), new Vector2(200, 0), new Rect(0, -100, 100, 100), 1f / 60f };
        move.Invoke(card, arguments);
        Console.WriteLine("new lower-row card initial Y=" + pixelOffset.y + "; expected first-row Y=0 during native horizontal entrance");
        Check(Math.Abs(pixelOffset.y) < .001f, "new card enters at first row before descending to target row");
    }
    public static int Main(string[] args)
    {
        try {
            if (args.Length > 1 && args[1] == "entrance") Entrance(args[0]); else Run(args[0]);
            Console.WriteLine("PASS " + checks + " production recipe bounds/layout assertions (Unity object calls replaced; no rendered UI)."); return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }
}
