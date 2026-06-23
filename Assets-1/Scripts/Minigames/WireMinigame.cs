using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// มินิเกมต่อสายไฟ — ลากจากจุดซ้ายไปจุดขวาที่สีตรงกัน ครบ 4 เส้น = ชนะ
/// แนบที่: WirePanel (child ของ MinigameCanvas)
///
/// การเชื่อม UI ใน Inspector:
/// - panelRoot       → WirePanel
/// - sourceNodes     → วงกลม 4 จุดฝั่งซ้าย (WireNode, Side=Source)
/// - targetNodes     → วงกลม 4 จุดฝั่งขวา (WireNode, Side=Target, สลับตำแหน่งสี)
/// - dragLineImage   → เส้นชั่วคราวขณะลาก (Image บางๆ สีขาว)
/// - connectionRoot  → Empty เก็บเส้นที่ต่อสำเร็จแล้ว
/// </summary>
public class WireMinigame : MinigameBase
{
    [Header("Wire Nodes")]
    [SerializeField] private List<WireNode> sourceNodes = new List<WireNode>();
    [SerializeField] private List<WireNode> targetNodes = new List<WireNode>();

    [Header("Drag Line")]
    [Tooltip("Image แสดงเส้นขณะลาก — ตั้ง Pivot (0, 0.5)")]
    [SerializeField] private RectTransform dragLineRect;

    [Tooltip("Parent สำหรับสร้างเส้นที่ต่อสำเร็จ")]
    [SerializeField] private RectTransform connectionRoot;

    [Tooltip("Prefab เส้นต่อสำเร็จ — ถ้าว่างจะ duplicate dragLineRect")]
    [SerializeField] private RectTransform connectionLinePrefab;

    [Tooltip("ความหนาเส้นสาย (pixels)")]
    [SerializeField] private float lineThickness = 6f;

    [Header("Gameplay")]
    [Tooltip("จำนวนสายที่ต้องต่อให้ครบเพื่อชนะ")]
    [SerializeField] private int requiredConnections = 4;

    private WireNode activeSource;
    private int completedConnections;
    private Canvas rootCanvas;
    private GraphicRaycaster raycaster;

    protected override void Awake()
    {
        base.Awake();
        rootCanvas = GetComponentInParent<Canvas>();
        raycaster = GetComponentInParent<GraphicRaycaster>();

        if (dragLineRect != null)
            dragLineRect.gameObject.SetActive(false);
    }

    protected override void ResetMinigame()
    {
        activeSource = null;
        completedConnections = 0;

        if (dragLineRect != null)
            dragLineRect.gameObject.SetActive(false);

        // ลบเส้นที่ต่อไว้
        if (connectionRoot != null)
        {
            for (int i = connectionRoot.childCount - 1; i >= 0; i--)
                Destroy(connectionRoot.GetChild(i).gameObject);
        }

        foreach (var node in sourceNodes)
            if (node != null) node.SetConnected(false);

        foreach (var node in targetNodes)
            if (node != null) node.SetConnected(false);
    }

    /// <summary>WireNode (Source) เรียกเมื่อกดเมาส์ค้าง</summary>
    public void BeginDragFrom(WireNode source, PointerEventData eventData)
    {
        activeSource = source;

        if (dragLineRect != null)
        {
            dragLineRect.gameObject.SetActive(true);
            SetLineBetween(dragLineRect, source.RectTransform.position, eventData.position, WireNode.GetWireUnityColor(source.WireColorType));
        }
    }

    /// <summary>WireNode เรียกขณะลากเมาส์</summary>
    public void UpdateDrag(PointerEventData eventData)
    {
        if (activeSource == null || dragLineRect == null) return;

        SetLineBetween(
            dragLineRect,
            activeSource.RectTransform.position,
            eventData.position,
            WireNode.GetWireUnityColor(activeSource.WireColorType));
    }

    /// <summary>WireNode เรียกเมื่อปล่อยเมาส์ — ตรวจว่าทับ Target สีเดียวกันหรือไม่</summary>
    public void EndDrag(PointerEventData eventData)
    {
        if (activeSource == null) return;

        WireNode hitTarget = RaycastWireNode(eventData);

        if (hitTarget != null
            && hitTarget.Side == WireNode.WireSide.Target
            && !hitTarget.IsConnected
            && hitTarget.WireColorType == activeSource.WireColorType)
        {
            ConnectNodes(activeSource, hitTarget);
            completedConnections++;

            if (completedConnections >= requiredConnections)
                NotifySuccess();
        }

        activeSource = null;

        if (dragLineRect != null)
            dragLineRect.gameObject.SetActive(false);
    }

    private void ConnectNodes(WireNode source, WireNode target)
    {
        source.SetConnected(true);
        target.SetConnected(true);

        RectTransform line = CreateConnectionLine();
        SetLineBetween(
            line,
            source.RectTransform.position,
            target.RectTransform.position,
            WireNode.GetWireUnityColor(source.WireColorType));
    }

    private RectTransform CreateConnectionLine()
    {
        RectTransform line;
        if (connectionLinePrefab != null)
            line = Instantiate(connectionLinePrefab, connectionRoot);
        else if (dragLineRect != null)
            line = Instantiate(dragLineRect, connectionRoot);
        else
        {
            var go = new GameObject("WireConnection", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(connectionRoot, false);
            line = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
        }

        line.gameObject.SetActive(true);
        return line;
    }

    /// <summary>วาดเส้นระหว่าง 2 จุดบน Canvas (Screen Space)</summary>
    private void SetLineBetween(RectTransform line, Vector2 startScreen, Vector2 endScreen, Color color)
    {
        if (line == null) return;

        RectTransform parent = line.parent as RectTransform;
        if (parent == null) return;

        Camera cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, startScreen, cam, out Vector2 localStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, endScreen, cam, out Vector2 localEnd);

        Vector2 dir = localEnd - localStart;
        float length = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        line.anchoredPosition = localStart;
        line.sizeDelta = new Vector2(length, lineThickness);
        line.localRotation = Quaternion.Euler(0f, 0f, angle);
        line.pivot = new Vector2(0f, 0.5f);

        var img = line.GetComponent<Image>();
        if (img != null)
        {
            img.color = color;
            img.raycastTarget = false;
        }
    }

    private WireNode RaycastWireNode(PointerEventData eventData)
    {
        if (raycaster == null) return null;

        var results = new List<RaycastResult>();
        raycaster.Raycast(eventData, results);

        foreach (var result in results)
        {
            var node = result.gameObject.GetComponent<WireNode>();
            if (node != null)
                return node;
        }

        return null;
    }
}