using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// มินิเกมเปิดกุญแจแบบ Pin Tumbler (เวอร์ชันสมบูรณ์ - เติม logic ที่เป็น TODO ให้ครบ)
///
/// วิธีเล่น:
/// - กด A/D หรือ ←/→ เพื่อเลื่อนเลือก Pin
/// - กด W หรือ ↑ ค้างไว้เพื่อดัน Pin ขึ้น
/// - แต่ละ Pin มี "Sweet Spot" ตำแหน่งที่ต่างกัน — ถ้าปล่อยมือตรงจุด Pin จะล็อกค้างไว้
/// - ถ้าดันค้างจนสุดโดยไม่ตรง Sweet Spot = กิ๊ฟหัก (Snap)
/// - ล็อก Pin ครบทุกตัว = สำเร็จ (NotifySuccess), กิ๊ฟแตกครบจำนวน = ล้มเหลว (NotifyFailed)
///
/// หมายเหตุสำคัญเรื่อง MinigameBase เวอร์ชันนี้:
/// NotifySuccess()/NotifyFailed() จะ "ปิดเกมทันที" ทั้งคู่ (เรียก EndMinigame() ข้างใน)
/// ดังนั้นกิ๊ฟหักแค่ 1 ครั้ง (ยังมีกิ๊ฟเหลือ) จะ "ไม่" เรียก NotifyFailed() — แค่รีเซ็ต Pin แล้วเล่นต่อ
/// จะเรียก NotifyFailed() ก็ตอนกิ๊ฟหมดจริงๆเท่านั้น (ดูเมธอด SnapPick ด้านล่าง)
/// </summary>
public class LockpickMinigame : MinigameBase
{
    // ── UI References ─────────────────────────────
    [Header("UI References")]
    [Tooltip("Parent ของ PinUI ทั้งหมด (ไม่ได้ใช้คำนวณตรงๆ แต่เก็บไว้เผื่อจัด layout ใน Editor)")]
    [SerializeField] private RectTransform pinParent;

    [Tooltip("Image กิ๊ฟ (RectTransform จะขยับซ้าย-ขวาไปยืนตรง Pin ที่เลือก)")]
    [SerializeField] private RectTransform pickImage;

    [Tooltip("Slider หรือ Image fill แสดงแรงดัน (0 = ไม่ดัน, 1 = ดันสุด)")]
    [SerializeField] private Slider tensionBar;

    [Tooltip("Text แสดง 'UNLOCKED' / 'SNAP!'")]
    [SerializeField] private Text resultText;

    [Tooltip("RectTransform ของ Pin แต่ละตัว — ต้องมีจำนวนเท่ากับ pinCount เรียงจากซ้ายไปขวา")]
    [SerializeField] private RectTransform[] pinRects;

    [Tooltip("Image ของ Pin แต่ละตัว (index ตรงกับ pinRects) — ใช้เปลี่ยนสีตามสถานะ")]
    [SerializeField] private Image[] pinImages;

    // ── สี Pin ────────────────────────────────────
    [Header("Pin Colors")]
    [SerializeField] private Color colorIdle   = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color colorActive = new Color(1f,   0.85f, 0.2f);
    [SerializeField] private Color colorSet    = new Color(0.2f, 0.9f,  0.3f);
    [SerializeField] private Color colorSnap   = new Color(0.9f, 0.2f,  0.2f);

    // ── Game Settings ────────────────────────────
    [Header("Game Settings")]
    [Tooltip("จำนวน Pin ทั้งหมด")]
    public int pinCount = 5;

    [Tooltip("ความสูงสูงสุดที่ Pin ขยับได้ (pixels)")]
    [SerializeField] private float pinMaxHeight = 120f;

    [Tooltip("ความกว้างของ Sweet Spot แต่ละ Pin (0-1, เช่น 0.15 = กว้าง 15%)")]
    [SerializeField] private float sweetSpotWidth = 0.15f;

    [Tooltip("ความเร็วที่ Pin ขึ้นเมื่อกด W")]
    [SerializeField] private float pushSpeed = 180f;

    [Tooltip("ความเร็วที่ Pin ตกลงเมื่อปล่อย")]
    [SerializeField] private float fallSpeed = 220f;

    [Tooltip("จำนวนครั้งที่กิ๊ฟแตกได้ก่อน Game Over")]
    [SerializeField] private int maxPickBreaks = 3;

    [Header("Input Keys")]
    [SerializeField] private KeyCode keyLeft  = KeyCode.A;
    [SerializeField] private KeyCode keyRight = KeyCode.D;
    [SerializeField] private KeyCode keyPush  = KeyCode.W;

    // ── Private State ────────────────────────────
    private float[] pinHeights;       // ความสูงปัจจุบันของแต่ละ Pin (0-1)
    private float[] sweetSpotCenter;  // ตำแหน่งกึ่งกลาง Sweet Spot ของแต่ละ Pin (0-1)
    private bool[]  pinIsSet;         // true = Pin นี้ล็อคสำเร็จแล้ว
    private int     currentPinIndex;
    private int     pinsSet;
    private int     picksLeft;
    private bool    isPushing;

    // ──────────────────────────────────────────────
    //  MinigameBase — Abstract Implementation
    // ──────────────────────────────────────────────

    /// <summary>รีเซ็ตสถานะทั้งหมดให้พร้อมเล่นใหม่ — เรียกอัตโนมัติโดย MinigameBase.StartMinigame()</summary>
    protected override void ResetMinigame()
    {
        pinHeights      = new float[pinCount];
        sweetSpotCenter = new float[pinCount];
        pinIsSet        = new bool[pinCount];
        pinsSet         = 0;
        picksLeft       = maxPickBreaks;
        currentPinIndex = 0;
        isPushing       = false;

        float half = sweetSpotWidth * 0.5f;
        for (int i = 0; i < pinCount; i++)
        {
            sweetSpotCenter[i] = Random.Range(half + 0.05f, 1f - half - 0.05f);
            pinHeights[i] = 0f;
            pinIsSet[i] = false;
        }

        if (tensionBar != null)
            tensionBar.value = 0f;

        if (resultText != null)
            resultText.text = string.Empty;

        RefreshAllPinUI();
        MovePickToCurrentPin();
    }

    // ──────────────────────────────────────────────
    //  Update Loop
    // ──────────────────────────────────────────────

    /// <summary>override Update() จาก MinigameBase ซึ่งเป็น virtual — ต้องเรียก base.Update() เพื่อให้ cancelKey ยังทำงาน</summary>
    protected override void Update()
    {
        base.Update(); // จัดการ cancelKey (Escape) ใน MinigameBase

        if (!IsActive) return;
        if (pinHeights == null) return; // กันเหนียวกรณี Update มาก่อน ResetMinigame ตั้งค่าเสร็จ

        HandleNavigationInput();
        HandlePushInput();
        RefreshAllPinUI();
    }

    private void HandleNavigationInput()
    {
        if (Input.GetKeyDown(keyLeft))
            MovePin(-1);
        if (Input.GetKeyDown(keyRight))
            MovePin(+1);
    }

    private void MovePin(int direction)
    {
        int next = currentPinIndex + direction;
        // ข้าม Pin ที่ Set แล้ว
        while (next >= 0 && next < pinCount && pinIsSet[next])
            next += direction;

        if (next < 0 || next >= pinCount) return;

        currentPinIndex = next;
        MovePickToCurrentPin();
    }

    private void HandlePushInput()
    {
        if (pinIsSet[currentPinIndex]) return;

        isPushing = Input.GetKey(keyPush);

        if (isPushing)
        {
            pinHeights[currentPinIndex] = Mathf.Clamp01(
                pinHeights[currentPinIndex] + pushSpeed * Time.deltaTime / pinMaxHeight);

            if (tensionBar != null)
                tensionBar.value = pinHeights[currentPinIndex];

            // ดันถึงเพดานสุดแล้วไม่อยู่ใน Sweet Spot = กิ๊ฟหัก
            if (pinHeights[currentPinIndex] >= 1f && !IsInSweetSpot(currentPinIndex, pinHeights[currentPinIndex]))
            {
                SnapPick();
            }
        }
        else
        {
            float prevHeight = pinHeights[currentPinIndex];
            pinHeights[currentPinIndex] = Mathf.Clamp01(
                pinHeights[currentPinIndex] - fallSpeed * Time.deltaTime / pinMaxHeight);

            // เช็ค Sweet Spot ตอนปล่อยมือแล้ว Pin กำลังตกลง
            if (prevHeight > 0f && IsInSweetSpot(currentPinIndex, pinHeights[currentPinIndex]))
            {
                SetPin(currentPinIndex);
            }

            if (tensionBar != null)
                tensionBar.value = pinHeights[currentPinIndex];
        }
    }

    private bool IsInSweetSpot(int index, float height)
    {
        float low = sweetSpotCenter[index] - sweetSpotWidth * 0.5f;
        float high = sweetSpotCenter[index] + sweetSpotWidth * 0.5f;
        return height >= low && height <= high;
    }

    private void SetPin(int index)
    {
        pinIsSet[index] = true;
        pinHeights[index] = sweetSpotCenter[index];
        pinsSet++;

        if (pinImages != null && index < pinImages.Length && pinImages[index] != null)
            pinImages[index].color = colorSet;

        if (pinsSet >= pinCount)
        {
            if (resultText != null)
                resultText.text = "UNLOCKED";

            NotifySuccess();
            return;
        }

        MovePin(+1);
        if (currentPinIndex < pinCount && pinIsSet[currentPinIndex])
            MovePin(-1);
    }

    private void SnapPick()
    {
        picksLeft--;
        pinHeights[currentPinIndex] = 0f;
        isPushing = false;

        if (pinImages != null && currentPinIndex < pinImages.Length && pinImages[currentPinIndex] != null)
            pinImages[currentPinIndex].color = colorSnap;

        if (resultText != null)
            resultText.text = "SNAP!";

        if (picksLeft <= 0)
        {
            // กิ๊ฟหมดจริง — จบเกมแบบล้มเหลว (MinigameBase จะปิด panel/คืน input ให้)
            NotifyFailed();
            return;
        }

        // ยังมีกิ๊ฟเหลือ — รีเซ็ต Pin ที่ Set ไปแล้วทั้งหมด (กุญแจ spring กลับ) แต่ไม่ปิดเกม
        for (int i = 0; i < pinCount; i++)
        {
            if (pinIsSet[i])
            {
                pinIsSet[i] = false;
                pinHeights[i] = 0f;
                if (pinImages != null && i < pinImages.Length && pinImages[i] != null)
                    pinImages[i].color = colorIdle;
            }
        }

        pinsSet = 0;
        currentPinIndex = 0;
        MovePickToCurrentPin();
    }

    // ──────────────────────────────────────────────
    //  UI Update
    // ──────────────────────────────────────────────

    private void RefreshAllPinUI()
    {
        if (pinRects == null) return;

        for (int i = 0; i < pinRects.Length && i < pinCount; i++)
        {
            if (pinRects[i] == null) continue;

            Vector2 pos = pinRects[i].anchoredPosition;
            pos.y = pinHeights[i] * pinMaxHeight;
            pinRects[i].anchoredPosition = pos;

            if (pinImages != null && i < pinImages.Length && pinImages[i] != null && !pinIsSet[i])
            {
                pinImages[i].color = (i == currentPinIndex) ? colorActive : colorIdle;
            }
        }
    }

    private void MovePickToCurrentPin()
    {
        if (pickImage == null || pinRects == null) return;
        if (currentPinIndex >= pinRects.Length || pinRects[currentPinIndex] == null) return;

        Vector2 pos = pickImage.anchoredPosition;
        pos.x = pinRects[currentPinIndex].anchoredPosition.x;
        pickImage.anchoredPosition = pos;
    }

    // ──────────────────────────────────────────────
    //  Public — เรียกจากภายนอก
    // ──────────────────────────────────────────────

    public int PicksRemaining => picksLeft;
    public float Progress => pinCount > 0 ? (float)pinsSet / pinCount : 0f;
}