using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// สร้าง Canvas, มินิเกม UI ทั้ง 3 ชนิด, MinigameManager และ Trigger ทดสอบอัตโนมัติ
/// เมนู: Midnight School > Setup Step 2 (Minigame UI + Triggers)
/// </summary>
public static class MidnightSchoolMinigameSetup
{
    private const string CanvasName = "MinigameCanvas";
    private const string ManagerName = "MinigameManager";

    [MenuItem("Midnight School/Setup Step 2 (Minigame UI + Triggers)")]
    public static void SetupStep2()
    {
        if (!EditorUtility.DisplayDialog(
                "Midnight School — Setup Step 2",
                "สคริปต์นี้จะสร้าง Minigame Canvas, UI ทั้ง 3 ชนิด, MinigameManager และ Trigger ทดสอบ 3 จุด\n\n" +
                "แนะนำ: รัน Setup Step 1 ก่อนเพื่อให้มี Player\n\nดำเนินการต่อ?",
                "Setup",
                "Cancel"))
            return;

        GameObject existingCanvas = GameObject.Find(CanvasName);
        if (existingCanvas != null)
        {
            if (!EditorUtility.DisplayDialog(
                    "พบ MinigameCanvas อยู่แล้ว",
                    "มี MinigameCanvas ใน Scene แล้ว\nต้องการลบและสร้างใหม่?",
                    "สร้างใหม่",
                    "ยกเลิก"))
                return;

            Object.DestroyImmediate(existingCanvas);
        }

        GameObject existingManager = GameObject.Find(ManagerName);
        if (existingManager != null)
            Object.DestroyImmediate(existingManager);

        Undo.SetCurrentGroupName("Midnight School Setup Step 2");
        int undoGroup = Undo.GetCurrentGroup();

        EnsureEventSystem();
        EnsurePlayerTag();

        GameObject canvasGo = CreateCanvas();
        Transform canvasTransform = canvasGo.transform;

        GameObject lockpickPanel = CreateLockpickPanel(canvasTransform);
        GameObject strugglePanel = CreateStrugglePanel(canvasTransform);
        GameObject wirePanel = CreateWirePanel(canvasTransform);

        GameObject managerGo = new GameObject(ManagerName);
        Undo.RegisterCreatedObjectUndo(managerGo, "Create MinigameManager");
        MinigameManager manager = Undo.AddComponent<MinigameManager>(managerGo);

        WireManagerReferences(
            manager,
            canvasGo.GetComponent<Canvas>(),
            lockpickPanel.GetComponent<LockpickMinigame>(),
            strugglePanel.GetComponent<StruggleMinigame>(),
            wirePanel.GetComponent<WireMinigame>());

        CreateTestTriggers();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGo;
        EditorGUIUtility.PingObject(canvasGo);

        Debug.Log("[Midnight School] Setup Step 2 เสร็จสมบูรณ์ — เดินเข้า Trigger สีฟ้าเพื่อทดสอบมินิเกม");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static void EnsurePlayerTag()
    {
        FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
        if (player == null)
        {
            Debug.LogWarning("[Setup Step 2] ไม่พบ Player — รัน Setup Step 1 ก่อน หรือสร้าง Player เอง");
            return;
        }

        if (!player.CompareTag("Player"))
        {
            Undo.RecordObject(player.gameObject, "Set Player Tag");
            player.tag = "Player";
        }
    }

    private static GameObject CreateCanvas()
    {
        GameObject go = new GameObject(CanvasName);
        Undo.RegisterCreatedObjectUndo(go, "Create Minigame Canvas");

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static GameObject CreateLockpickPanel(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "LockpickPanel", new Color(0f, 0f, 0f, 0.75f));
        LockpickMinigame minigame = panel.AddComponent<LockpickMinigame>();

        GameObject lockBg = CreateUIImage(panel.transform, "LockBackground",
            new Vector2(0f, 40f), new Vector2(280f, 420f), new Color(0.35f, 0.32f, 0.28f));

        GameObject leftBound = CreateRect(panel.transform, "LeftBoundary",
            new Vector2(-70f, 40f), new Vector2(10f, 400f));
        GameObject rightBound = CreateRect(panel.transform, "RightBoundary",
            new Vector2(70f, 40f), new Vector2(10f, 400f));

        GameObject targetZone = CreateRect(panel.transform, "TargetZone",
            new Vector2(0f, 220f), new Vector2(80f, 40f));
        var targetImg = targetZone.AddComponent<Image>();
        targetImg.color = new Color(0.2f, 0.9f, 0.3f, 0.25f);
        targetImg.raycastTarget = false;

        GameObject startAnchor = CreateRect(panel.transform, "StartAnchor",
            new Vector2(0f, -160f), new Vector2(20f, 20f));

        GameObject hairClip = CreateUIImage(panel.transform, "HairClip",
            new Vector2(0f, -160f), new Vector2(18f, 140f), new Color(0.75f, 0.75f, 0.8f));

        GameObject hint = CreateText(panel.transform, "HintText",
            new Vector2(0f, -280f), "กด ↑ ค้างเพื่อดันกิ๊ฟผม — อย่าให้ชนขอบ!");

        WireLockpick(minigame, panel, hairClip, lockBg, leftBound, rightBound, targetZone, startAnchor);
        panel.SetActive(false);
        return panel;
    }

    private static void WireLockpick(
        LockpickMinigame minigame, GameObject panel, GameObject hairClip,
        GameObject lockBg, GameObject leftBound, GameObject rightBound,
        GameObject targetZone, GameObject startAnchor)
    {
        SerializedObject so = new SerializedObject(minigame);
        so.FindProperty("panelRoot").objectReferenceValue = panel;
        so.FindProperty("hairClipRect").objectReferenceValue = hairClip.GetComponent<RectTransform>();
        so.FindProperty("startAnchor").objectReferenceValue = startAnchor.GetComponent<RectTransform>();
        so.FindProperty("leftBoundary").objectReferenceValue = leftBound.GetComponent<RectTransform>();
        so.FindProperty("rightBoundary").objectReferenceValue = rightBound.GetComponent<RectTransform>();
        so.FindProperty("targetZone").objectReferenceValue = targetZone.GetComponent<RectTransform>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateStrugglePanel(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "StrugglePanel", new Color(0f, 0f, 0f, 0.75f));
        StruggleMinigame minigame = panel.AddComponent<StruggleMinigame>();

        CreateText(panel.transform, "Title", new Vector2(0f, 200f), "ดิ้นรน! กด Q / E สลับกัน");

        GameObject sliderGo = CreateSlider(panel.transform, "StruggleSlider", new Vector2(0f, 0f), new Vector2(600f, 40f));
        Slider slider = sliderGo.GetComponent<Slider>();

        GameObject sweetSpot = CreateRect(sliderGo.transform, "SweetSpot",
            new Vector2(0f, 0f), new Vector2(80f, 50f));
        var sweetImg = sweetSpot.AddComponent<Image>();
        sweetImg.color = new Color(0.2f, 0.9f, 0.3f, 0.35f);
        sweetImg.raycastTarget = false;

        RectTransform track = sliderGo.transform.Find("Background")?.GetComponent<RectTransform>();
        if (track == null)
            track = sliderGo.GetComponent<RectTransform>();

        SerializedObject so = new SerializedObject(minigame);
        so.FindProperty("panelRoot").objectReferenceValue = panel;
        so.FindProperty("gaugeSlider").objectReferenceValue = slider;
        so.FindProperty("sweetSpotRect").objectReferenceValue = sweetSpot.GetComponent<RectTransform>();
        so.FindProperty("sliderTrackRect").objectReferenceValue = track;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateWirePanel(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "WirePanel", new Color(0f, 0f, 0f, 0.8f));
        WireMinigame minigame = panel.AddComponent<WireMinigame>();

        CreateText(panel.transform, "Title", new Vector2(0f, 240f), "ต่อสายไฟ — ลากจากซ้ายไปสีเดียวกันทางขวา");

        GameObject connectionRoot = CreateRect(panel.transform, "ConnectionRoot", Vector2.zero, Vector2.zero);
        connectionRoot.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        connectionRoot.GetComponent<RectTransform>().anchorMax = Vector2.one;
        connectionRoot.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        connectionRoot.GetComponent<RectTransform>().offsetMax = Vector2.zero;

        GameObject dragLine = CreateUIImage(panel.transform, "DragLine", Vector2.zero, new Vector2(100f, 6f), Color.white);
        dragLine.SetActive(false);
        dragLine.GetComponent<Image>().raycastTarget = false;
        dragLine.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);

        WireColor[] sourceColors = { WireColor.Red, WireColor.Blue, WireColor.Green, WireColor.Yellow };
        WireColor[] shuffledTargets = { WireColor.Yellow, WireColor.Red, WireColor.Blue, WireColor.Green };

        var sources = new List<WireNode>();
        var targets = new List<WireNode>();
        float[] sourceY = { 120f, 40f, -40f, -120f };
        float[] targetY = { 100f, 20f, -60f, -140f };

        for (int i = 0; i < 4; i++)
        {
            sources.Add(CreateWireNode(panel.transform, $"Source_{sourceColors[i]}", WireNode.WireSide.Source,
                sourceColors[i], new Vector2(-320f, sourceY[i]), minigame));
            targets.Add(CreateWireNode(panel.transform, $"Target_{shuffledTargets[i]}", WireNode.WireSide.Target,
                shuffledTargets[i], new Vector2(320f, targetY[i]), minigame));
        }

        SerializedObject so = new SerializedObject(minigame);
        so.FindProperty("panelRoot").objectReferenceValue = panel;
        so.FindProperty("dragLineRect").objectReferenceValue = dragLine.GetComponent<RectTransform>();
        so.FindProperty("connectionRoot").objectReferenceValue = connectionRoot.GetComponent<RectTransform>();

        var srcProp = so.FindProperty("sourceNodes");
        srcProp.ClearArray();
        for (int i = 0; i < sources.Count; i++)
        {
            srcProp.InsertArrayElementAtIndex(i);
            srcProp.GetArrayElementAtIndex(i).objectReferenceValue = sources[i];
        }

        var tgtProp = so.FindProperty("targetNodes");
        tgtProp.ClearArray();
        for (int i = 0; i < targets.Count; i++)
        {
            tgtProp.InsertArrayElementAtIndex(i);
            tgtProp.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        panel.SetActive(false);
        return panel;
    }

    private static WireNode CreateWireNode(Transform parent, string name, WireNode.WireSide side,
        WireColor color, Vector2 position, WireMinigame minigame)
    {
        GameObject node = CreateUIImage(parent, name, position, new Vector2(48f, 48f), WireNode.GetWireUnityColor(color));
        WireNode wireNode = node.AddComponent<WireNode>();

        SerializedObject so = new SerializedObject(wireNode);
        so.FindProperty("side").enumValueIndex = (int)side;
        so.FindProperty("wireColor").enumValueIndex = (int)color;
        so.FindProperty("wireMinigame").objectReferenceValue = minigame;
        so.ApplyModifiedPropertiesWithoutUndo();

        return wireNode;
    }

    private static void WireManagerReferences(
        MinigameManager manager, Canvas canvas,
        LockpickMinigame lockpick, StruggleMinigame struggle, WireMinigame wire)
    {
        FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("lockpickMinigame").objectReferenceValue = lockpick;
        so.FindProperty("struggleMinigame").objectReferenceValue = struggle;
        so.FindProperty("wireMinigame").objectReferenceValue = wire;
        so.FindProperty("playerController").objectReferenceValue = player;
        so.FindProperty("minigameCanvas").objectReferenceValue = canvas;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateTestTriggers()
    {
        CreateTriggerCube("Trigger_Lockpick", new Vector3(3f, 1f, 2f), MinigameType.Lockpick);
        CreateTriggerCube("Trigger_Struggle", new Vector3(-3f, 1f, 2f), MinigameType.Struggle);
        CreateTriggerCube("Trigger_Wire", new Vector3(0f, 1f, 5f), MinigameType.Wire);
    }

    private static void CreateTriggerCube(string name, Vector3 position, MinigameType type)
    {
        if (GameObject.Find(name) != null) return;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, "Create Trigger");
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = new Vector3(2f, 2f, 2f);

        Object.DestroyImmediate(go.GetComponent<Collider>());
        BoxCollider box = Undo.AddComponent<BoxCollider>(go);
        box.isTrigger = true;

        var renderer = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.2f, 0.7f, 1f, 0.5f);
        renderer.sharedMaterial = mat;

        MinigameTrigger trigger = Undo.AddComponent<MinigameTrigger>(go);
        SerializedObject so = new SerializedObject(trigger);
        so.FindProperty("minigameType").enumValueIndex = (int)type;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // --- UI Helpers ---

    private static GameObject CreatePanel(Transform parent, string name, Color bgColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bgColor;
        return go;
    }

    private static GameObject CreateUIImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = CreateRect(parent, name, pos, size);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static GameObject CreateRect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    private static GameObject CreateText(Transform parent, string name, Vector2 pos, string text)
    {
        GameObject go = CreateRect(parent, name, pos, new Vector2(800f, 60f));
        Text uiText = go.AddComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = 28;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.color = Color.white;
        uiText.raycastTarget = false;
        return go;
    }

    private static GameObject CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderGo.name = name;
        sliderGo.transform.SetParent(parent, false);
        RectTransform rt = sliderGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Slider slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        slider.interactable = false;

        return sliderGo;
    }
}
