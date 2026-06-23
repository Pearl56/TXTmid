using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// จุดเชื่อมสายไฟ — วงกลมสีฝั่งซ้าย (ต้นทาง) หรือขวา (ปลายทาง)
/// แนบที่: WireNode แต่ละจุดบน WirePanel
/// ต้องมี: Image (สี), WireNode component, และ WireMinigame ใน parent
/// </summary>
[RequireComponent(typeof(Image))]
public class WireNode : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public enum WireSide { Source, Target }

    [Header("Wire Settings")]
    [Tooltip("Source = ฝั่งซ้าย (ลากออก), Target = ฝั่งขวา (ปล่อยรับ)")]
    [SerializeField] private WireSide side = WireSide.Source;

    [Tooltip("สีของสาย — ต้องจับคู่กับ Target สีเดียวกัน")]
    [SerializeField] private WireColor wireColor = WireColor.Red;

    [Tooltip("WireMinigame หลัก — ถ้าว่างจะหาใน parent")]
    [SerializeField] private WireMinigame wireMinigame;

    public WireSide Side => side;

    /// <summary>สีของจุดเชื่อม — ใช้จับคู่ Source ↔ Target (ไม่ตั้งชื่อ Color เพราะชนกับ UnityEngine.Color)</summary>
    public WireColor WireColorType => wireColor;
    public RectTransform RectTransform => (RectTransform)transform;
    public bool IsConnected { get; private set; }

    private Image nodeImage;

    private void Awake()
    {
        nodeImage = GetComponent<Image>();
        if (wireMinigame == null)
            wireMinigame = GetComponentInParent<WireMinigame>();
    }

    public void SetConnected(bool connected)
    {
        IsConnected = connected;
        if (nodeImage != null)
            nodeImage.color = connected ? UnityEngine.Color.white * 0.6f : GetWireUnityColor(wireColor);
    }

    public static Color GetWireUnityColor(WireColor color)
    {
        switch (color)
        {
            case WireColor.Red: return new Color(0.9f, 0.2f, 0.2f);
            case WireColor.Blue: return new Color(0.2f, 0.4f, 0.95f);
            case WireColor.Green: return new Color(0.2f, 0.85f, 0.3f);
            case WireColor.Yellow: return new Color(0.95f, 0.85f, 0.15f);
            default: return UnityEngine.Color.white;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (side != WireSide.Source || IsConnected || wireMinigame == null) return;
        wireMinigame.BeginDragFrom(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (side != WireSide.Source || IsConnected || wireMinigame == null) return;
        wireMinigame.UpdateDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (side != WireSide.Source || IsConnected || wireMinigame == null) return;
        wireMinigame.EndDrag(eventData);
    }
}

/// <summary>สีสายไฟ 4 สี — ใช้จับคู่ Source ↔ Target</summary>
public enum WireColor
{
    Red,
    Blue,
    Green,
    Yellow
}
