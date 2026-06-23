using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace MidnightSchool.Minigames
{
    /// <summary>
    /// StrugglePanel.cs
    /// ควบคุมลอจิกทั้งหมดของมินิเกม "Struggle" (รุ่นทดสอบ ใช้กับการต้านการสิงร่างในอนาคต)
    /// หน้าที่: จัดการค่าเกจ (Decay + Mash), เช็คการกด Q/E แบบสลับ, เช็คโซน Sweet Spot,
    /// นับเวลานับถอยหลัง (Survival Timer), ตัดสิน Success/Fail, อัปเดต UI และ Visual Feedback
    ///
    /// หมายเหตุการแก้ไขจากการตรวจสอบสเปก:
    /// - เอกสารต้นฉบับมีข้อขัดแย้งกันเองเรื่องเงื่อนไข Fail (หลุดโซน = ตายทันที VS ตายแค่ตอนเกจชนขอบ 0/100)
    ///   จึงทำเป็น enum FailMode ให้ปรับเลือกได้จาก Inspector โดยไม่ต้องแก้โค้ด
    /// - เวลานับถอยหลังใช้ 10 วินาทีตามสเปกหลัก (ไม่ใช่ 5 วินาทีที่เป็นแค่ตัวอย่างในพรอมต์)
    /// </summary>
    public class StrugglePanel : MonoBehaviour
    {
        /// <summary>
        /// โหมดการตัดสินความล้มเหลว — แก้ปัญหาข้อขัดแย้งในสเปกต้นฉบับ
        /// ExtremesOnly      = ตายเมื่อเกจแตะ 0 หรือ 100 เท่านั้น (ตรงกับหัวข้อ "Fail Conditions" ที่ระบุไว้ละเอียดที่สุด)
        /// LeaveSweetSpotZone = ตายทันทีที่เกจหลุดออกจากโซน Sweet Spot (ตรงกับหัวข้อ "Gameplay" ที่อธิบายภาพรวม)
        /// </summary>
        public enum FailMode
        {
            ExtremesOnly,
            LeaveSweetSpotZone
        }

        [Header("ค่าตั้งต้นของเกจ (Gauge Settings)")]
        [Tooltip("ค่าเกจเริ่มต้นตอนเปิดมินิเกม (สเปก: เริ่มที่ 50 จากช่วง 0-100)")]
        [SerializeField] private float startValue = 50f;

        [Tooltip("อัตราที่เกจลดลงเองทุกวินาที ไม่ว่าผู้เล่นจะกดอะไรหรือไม่ (สเปก: 8 ต่อวินาที)")]
        [SerializeField] private float decayRate = 8f;

        [Tooltip("ค่าที่เกจเพิ่มขึ้นต่อการกด Q หรือ E ที่ถูกต้อง (สลับคีย์) แต่ละครั้ง (สเปก: +6 ทั้ง Q และ E)")]
        [SerializeField] private float mashAmount = 6f;

        [Tooltip("ขอบล่างของโซน Sweet Spot (สเปก: 40)")]
        [SerializeField] private float sweetSpotMin = 40f;

        [Tooltip("ขอบบนของโซน Sweet Spot (สเปก: 60)")]
        [SerializeField] private float sweetSpotMax = 60f;

        [Header("ตั้งเวลา (Timer Settings)")]
        [Tooltip("เวลาที่ต้องรอดให้ครบเพื่อชนะ (สเปก: 10 วินาที — ค่า 5 วินาทีในพรอมต์เป็นแค่ตัวอย่าง ไม่ใช่ค่าจริง)")]
        [SerializeField] private float survivalTime = 10f;

        [Tooltip(
            "false (ค่าเริ่มต้น) = นาฬิกาเดินตลอดเวลาไม่ว่าเกจจะอยู่ในโซนหรือไม่ แล้วเช็คผลตอน Timer หมด (ตรงสเปก 'Fail Conditions')\n" +
            "true = นาฬิกาจะหยุดเดินถ้าเกจหลุดจากโซน Sweet Spot (ฟีลแบบ 'ต้องประคองในโซนตลอดเวลา' ตามหัวข้อ 'Gameplay')")]
        [SerializeField] private bool pauseTimerOutsideSweetSpot = false;

        [Header("รูปแบบความล้มเหลว (แก้ข้อขัดแย้งของสเปก)")]
        [SerializeField] private FailMode failMode = FailMode.ExtremesOnly;

        [Header("Reference UI")]
        [SerializeField] private Slider gaugeBar;                 // ชื่อมาตรฐานคือ "GaugeBar" (รวมชื่อให้ตรงกันทั้งระบบ แทนที่ "ProgressBar" ที่ใช้สลับกันในสเปกต้นฉบับ)
        [SerializeField] private Image gaugeFillImage;             // ลูกของ Slider (Fill Area/Fill) ใช้สำหรับเปลี่ยนสีตอน feedback
        [SerializeField] private RectTransform sweetSpotZoneRect;  // กรอบ Sweet Spot ที่จะถูกคำนวณตำแหน่งอัตโนมัติจาก sweetSpotMin/Max
        [SerializeField] private Text timerText;                   // ถ้าโปรเจกต์ใช้ TextMeshPro ให้เปลี่ยนชนิดตัวแปรนี้เป็น TMP_Text
        [SerializeField] private Text statusText;
        [SerializeField] private Text instructionText;

        [Header("Visual Feedback")]
        [Tooltip("ถ้าเกจต่ำกว่าค่านี้ แถบ UI จะสั่นเบาๆ เตือนว่ากำลังจะแตะ 0")]
        [SerializeField] private float lowShakeThreshold = 20f;
        [Tooltip("ถ้าเกจสูงกว่าค่านี้ แถบ UI จะกระพริบสีแดงเตือนว่ากำลังจะแตะ 100")]
        [SerializeField] private float highFlashThreshold = 80f;
        [SerializeField] private float shakeStrength = 6f;
        [SerializeField] private Color inZoneColor = new Color(0.4f, 1f, 0.6f); // สีเรืองแสงตอนอยู่ใน Sweet Spot
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highColor = Color.red;

        [Header("Events (ผูกเข้ากับ StruggleManager)")]
        public UnityEvent OnStruggleSuccess;
        public UnityEvent OnStruggleFail;

        // --- ตัวแปรภายในที่ใช้ทำงานจริงตอนรันเกม ---
        private float gaugeValue;          // ค่าเกจปัจจุบัน (0-100)
        private float currentTimer;        // เวลานับถอยหลังที่เหลือ
        private KeyCode lastKey = KeyCode.None; // คีย์ล่าสุดที่กดสำเร็จ ใช้เช็คการ "สลับ" Q/E
        private bool isRunning = false;    // true ระหว่างที่มินิเกมกำลังเล่นอยู่
        private Vector2 gaugeBarOriginalAnchoredPos; // ตำแหน่งเดิมของแถบเกจ ใช้คืนค่าหลังสั่น

        private void Awake()
        {
            if (gaugeBar != null)
            {
                gaugeBar.minValue = 0f;
                gaugeBar.maxValue = 100f;
                gaugeBarOriginalAnchoredPos = gaugeBar.GetComponent<RectTransform>().anchoredPosition;
            }
        }

        /// <summary>
        /// เรียกจาก StruggleManager ทุกครั้งที่เปิดมินิเกม — รีเซ็ตค่าทั้งหมดให้เริ่มใหม่สะอาดๆ
        /// </summary>
        public void StartMinigame()
        {
            gaugeValue = startValue;
            currentTimer = survivalTime;
            lastKey = KeyCode.None;
            isRunning = true;

            if (statusText != null) statusText.text = string.Empty;
            if (instructionText != null) instructionText.text = "กด Q และ E สลับกัน เพื่อประคองเกจให้อยู่ใน Sweet Spot!";

            PositionSweetSpotZone();
            UpdateUI();
        }

        private void Update()
        {
            // ถ้ามินิเกมไม่ได้ทำงานอยู่ (ปิด Panel ไว้) ไม่ต้องประมวลผลอะไรเลย ประหยัด performance
            if (!isRunning) return;

            HandleInput();          // 1) อ่านการกด Q/E และเพิ่มเกจถ้าสลับถูก
            ApplyDecay();            // 2) ลดเกจตามเวลาเสมอ
            UpdateTimer();           // 3) เดินเวลา และเช็คผลตอนเวลาหมด
            CheckFailConditions();   // 4) เช็คเงื่อนไขตายตามโหมดที่เลือก
            UpdateUI();              // 5) อัปเดตหน้าจอ + Visual Feedback
        }

        /// <summary>
        /// อ่านอินพุต Q/E — เพิ่มเกจได้เฉพาะเมื่อ "สลับ" คีย์เท่านั้น
        /// ตัวอย่าง: กด Q ติดกันสองครั้ง (Q,Q) จะไม่ได้ค่าเกจในครั้งที่สอง ต้องเป็น Q แล้ว E แล้ว Q สลับไปเรื่อยๆ
        /// </summary>
        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Q) && lastKey != KeyCode.Q)
            {
                gaugeValue += mashAmount;
                lastKey = KeyCode.Q;
            }
            else if (Input.GetKeyDown(KeyCode.E) && lastKey != KeyCode.E)
            {
                gaugeValue += mashAmount;
                lastKey = KeyCode.E;
            }

            // กันค่าเกจไม่ให้หลุดช่วง 0-100 ทันทีหลังบวกค่า
            gaugeValue = Mathf.Clamp(gaugeValue, 0f, 100f);
        }

        /// <summary>
        /// เกจลดลงตามเวลาเสมอ ไม่ว่าผู้เล่นจะกดปุ่มหรือไม่ก็ตาม (สูตร: ลด decayRate หน่วยต่อวินาที)
        /// </summary>
        private void ApplyDecay()
        {
            gaugeValue -= decayRate * Time.deltaTime;
            gaugeValue = Mathf.Clamp(gaugeValue, 0f, 100f);
        }

        /// <summary>เช็คว่าเกจปัจจุบันอยู่ในช่วง Sweet Spot หรือไม่</summary>
        private bool IsInSweetSpot()
        {
            return gaugeValue >= sweetSpotMin && gaugeValue <= sweetSpotMax;
        }

        /// <summary>
        /// เดินเวลานับถอยหลัง — ถ้า pauseTimerOutsideSweetSpot = true จะหยุดเดินตอนอยู่นอกโซน
        /// เมื่อเวลาหมด (currentTimer <= 0) จะเช็คตำแหน่งเกจ ณ ขณะนั้นทันทีว่าอยู่ในโซนหรือไม่ เพื่อตัดสิน Success/Fail
        /// </summary>
        private void UpdateTimer()
        {
            if (!isRunning) return;

            if (pauseTimerOutsideSweetSpot && !IsInSweetSpot())
            {
                return; // นาฬิกาถูกแช่แข็งไว้เพราะหลุดโซน (ใช้เฉพาะตอนเปิดโหมดนี้)
            }

            currentTimer -= Time.deltaTime;

            if (currentTimer <= 0f)
            {
                currentTimer = 0f;

                if (IsInSweetSpot())
                    Success();
                else
                    Fail(); // เวลาหมดแต่ดันไม่อยู่ในโซน = ถือว่าล้มเหลว
            }
        }

        /// <summary>
        /// เช็คเงื่อนไขตายตาม FailMode ที่เลือกไว้ใน Inspector
        /// </summary>
        private void CheckFailConditions()
        {
            if (!isRunning) return;

            if (failMode == FailMode.ExtremesOnly)
            {
                // ตายแค่ตอนเกจสุดขั้ว (0 หรือ 100) — โหมดนี้ปล่อยให้ออกนอกโซนได้ระหว่างเกม
                if (gaugeValue <= 0f || gaugeValue >= 100f)
                    Fail();
            }
            else // LeaveSweetSpotZone
            {
                // โหมดเข้ม: หลุดโซนเมื่อไหร่ตายทันที
                if (!IsInSweetSpot())
                    Fail();
            }
        }

        /// <summary>ผู้เล่นทำสำเร็จ — ยิง Event ให้ StruggleManager ไปจัดการปิด Panel/คืนคอนโทรล</summary>
        private void Success()
        {
            if (!isRunning) return;
            isRunning = false;
            if (statusText != null) statusText.text = "สำเร็จ!";
            OnStruggleSuccess?.Invoke();
        }

        /// <summary>ผู้เล่นล้มเหลว — ยิง Event ให้ StruggleManager ไปจัดการปิด Panel/คืนคอนโทรล</summary>
        private void Fail()
        {
            if (!isRunning) return;
            isRunning = false;
            if (statusText != null) statusText.text = "ล้มเหลว!";
            OnStruggleFail?.Invoke();
        }

        /// <summary>
        /// คำนวณตำแหน่งกรอบ Sweet Spot บน UI โดยอัตโนมัติจากค่า sweetSpotMin/Max (0-100)
        /// แปลงเป็นสัดส่วน anchor (0.0-1.0) บนแกน X เทียบกับความกว้างของแถบเกจ
        /// ข้อกำหนด: sweetSpotZoneRect ต้องอยู่ภายใต้ Parent ที่มีความกว้างอ้างอิงเดียวกับ GaugeBar
        /// (เช่นวางไว้ใต้ GaugeBackground ที่กว้างเท่ากับ Slider track)
        /// </summary>
        private void PositionSweetSpotZone()
        {
            if (sweetSpotZoneRect == null) return;

            float minFraction = sweetSpotMin / 100f; // เช่น 40/100 = 0.4
            float maxFraction = sweetSpotMax / 100f; // เช่น 60/100 = 0.6

            sweetSpotZoneRect.anchorMin = new Vector2(minFraction, 0f);
            sweetSpotZoneRect.anchorMax = new Vector2(maxFraction, 1f);
            sweetSpotZoneRect.offsetMin = Vector2.zero;
            sweetSpotZoneRect.offsetMax = Vector2.zero;
        }

        /// <summary>อัปเดตค่าที่แสดงบนจอทั้งหมด (เกจ, เวลา) แล้วเรียก Visual Feedback ต่อ</summary>
        private void UpdateUI()
        {
            if (gaugeBar != null) gaugeBar.value = gaugeValue;
            if (timerText != null) timerText.text = Mathf.CeilToInt(currentTimer).ToString();

            ApplyVisualFeedback();
        }

        /// <summary>
        /// ใส่ฟีดแบ็กทางสายตา (อยู่ใน Scope ของ V1.0 — แยกจาก "Camera Shake" ที่ถูกห้ามไว้ใน V1.1):
        /// - อยู่ใน Sweet Spot: แถบเรืองแสง
        /// - เกจสูงเกิน highFlashThreshold: แถบกระพริบแดง
        /// - เกจต่ำกว่า lowShakeThreshold: แถบสั่นเบาๆ (สั่นที่ตัว UI เท่านั้น ไม่แตะกล้องผู้เล่น)
        /// </summary>
        private void ApplyVisualFeedback()
        {
            if (gaugeFillImage != null)
            {
                if (IsInSweetSpot())
                {
                    gaugeFillImage.color = inZoneColor;
                }
                else if (gaugeValue >= highFlashThreshold)
                {
                    // กระพริบด้วย PingPong ระหว่างสีแดงกับสีขาว ความเร็ว 6 รอบ/วินาที
                    gaugeFillImage.color = Color.Lerp(highColor, Color.white, Mathf.PingPong(Time.time * 6f, 1f));
                }
                else
                {
                    gaugeFillImage.color = normalColor;
                }
            }

            if (gaugeBar != null)
            {
                RectTransform rt = gaugeBar.GetComponent<RectTransform>();
                if (gaugeValue <= lowShakeThreshold)
                {
                    // ใช้ Perlin Noise สร้างการสั่นที่ดูเป็นธรรมชาติ ไม่กระตุกแบบสุ่มล้วน
                    float offsetX = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * shakeStrength;
                    rt.anchoredPosition = gaugeBarOriginalAnchoredPos + new Vector2(offsetX, 0f);
                }
                else
                {
                    rt.anchoredPosition = gaugeBarOriginalAnchoredPos;
                }
            }
        }
    }
}