using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// มินิเกมดิ้นรน Q/E — กด Q และ E สลับกันเพื่อดันเกจ, รักษาให้อยู่ใน Sweet Spot ครบเวลา
/// แนบที่: StrugglePanel (child ของ MinigameCanvas)
///
/// การเชื่อม UI ใน Inspector:
/// - panelRoot      → StrugglePanel
/// - gaugeSlider    → Slider หลัก (Min=0, Max=1)
/// - sweetSpotRect  → SweetSpot (Image กรอบโซนเป้าหมายตรงกลางแถบ)
/// - fillImage      → Fill Area/Fill (optional — เปลี่ยนสีตามโซน)
/// </summary>
public class StruggleMinigame : MinigameBase
{
    [Header("UI References")]
    [Tooltip("Slider เกจพลังงาน — ค่า 0-1")]
    [SerializeField] private Slider gaugeSlider;

    [Tooltip("กรอบ Sweet Spot ตรงกลาง — ใช้คำนวณว่า Fill อยู่ในโซนหรือไม่")]
    [SerializeField] private RectTransform sweetSpotRect;

    [Tooltip("RectTransform ของ Track/Background Slider — ใช้แปลงตำแหน่ง Sweet Spot")]
    [SerializeField] private RectTransform sliderTrackRect;

    [Header("Gauge Logic")]
    [Tooltip("อัตราที่เกจลดลงเองต่อวินาที (Gauge Decay)")]
    [SerializeField] private float decayRate = 0.15f;

    [Tooltip("พลังงานที่เพิ่มเมื่อกด Q/E สลับถูกต้อง")]
    [SerializeField] private float mashBoost = 0.12f;

    [Tooltip("เวลาที่ต้องอยู่ใน Sweet Spot ต่อเนื่องเพื่อชนะ (วินาที)")]
    [SerializeField] private float requiredHoldTime = 5f;

    [Tooltip("ถ้าเกจตกถึง 0 จะล้มเหลวทันที")]
    [SerializeField] private bool failOnEmpty = true;

    [Header("Input Keys")]
    [Tooltip("ปุ่มฝั่งซ้าย — สลับกับ E")]
    [SerializeField] private KeyCode leftMashKey = KeyCode.Q;

    [Tooltip("ปุ่มฝั่งขวา — สลับกับ Q")]
    [SerializeField] private KeyCode rightMashKey = KeyCode.E;

    private float timeInSweetSpot;
    private enum LastMashKey { None, Left, Right }
    private LastMashKey lastMash = LastMashKey.None;

    // ──────────────────────────────────────────────
    //  MinigameBase — Abstract Implementation
    // ──────────────────────────────────────────────

    protected override void ResetMinigame()
    {
        if (gaugeSlider != null)
            gaugeSlider.value = 0.5f;

        timeInSweetSpot = 0f;
        lastMash = LastMashKey.None;
    }

    // ──────────────────────────────────────────────
    //  Update Loop
    // ──────────────────────────────────────────────

    /// <summary>
    /// override Update() จาก MinigameBase ซึ่งเป็น virtual
    /// ต้องเรียก base.Update() เพื่อให้ cancelKey (Escape) ยังทำงานได้
    /// </summary>
    protected override void Update()
    {
        base.Update(); // จัดการ cancelKey ใน MinigameBase

        if (!IsActive || gaugeSlider == null) return;

        // --- Gauge Decay ---
        gaugeSlider.value = Mathf.Max(0f, gaugeSlider.value - decayRate * Time.deltaTime);

        // --- Mash Q/E สลับกัน ---
        HandleMashInput();

        // --- ตรวจ Sweet Spot ---
        if (IsGaugeInSweetSpot())
        {
            timeInSweetSpot += Time.deltaTime;
            if (timeInSweetSpot >= requiredHoldTime)
                NotifySuccess();
        }
        else
        {
            timeInSweetSpot = 0f;
        }

        // --- ล้มเหลวถ้าเกจหมด ---
        if (failOnEmpty && gaugeSlider.value <= 0f)
            NotifyFailed();
    }

    // ──────────────────────────────────────────────
    //  Private Methods
    // ──────────────────────────────────────────────

    private void HandleMashInput()
    {
        if (Input.GetKeyDown(leftMashKey))
        {
            if (lastMash != LastMashKey.Left)
            {
                gaugeSlider.value = Mathf.Clamp01(gaugeSlider.value + mashBoost);
                lastMash = LastMashKey.Left;
            }
        }

        if (Input.GetKeyDown(rightMashKey))
        {
            if (lastMash != LastMashKey.Right)
            {
                gaugeSlider.value = Mathf.Clamp01(gaugeSlider.value + mashBoost);
                lastMash = LastMashKey.Right;
            }
        }
    }

    /// <summary>
    /// เปรียบเทียบตำแหน่ง Fill ของ Slider กับช่วง Sweet Spot บน Track
    /// </summary>
    private bool IsGaugeInSweetSpot()
    {
        if (sweetSpotRect == null || sliderTrackRect == null) return false;

        float trackWidth = sliderTrackRect.rect.width;
        if (trackWidth <= 0f) return false;

        // แปลง Sweet Spot เป็นช่วง 0-1 บน Slider
        float spotCenter = sweetSpotRect.anchoredPosition.x - sliderTrackRect.anchoredPosition.x;
        float spotHalfWidth = sweetSpotRect.rect.width * 0.5f;

        float normalizedCenter = (spotCenter + trackWidth * 0.5f) / trackWidth;
        float normalizedHalf = spotHalfWidth / trackWidth;

        float min = normalizedCenter - normalizedHalf;
        float max = normalizedCenter + normalizedHalf;

        return gaugeSlider.value >= min && gaugeSlider.value <= max;
    }
}