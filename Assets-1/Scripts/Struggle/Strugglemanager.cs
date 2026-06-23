using System;
using UnityEngine;

namespace MidnightSchool.Minigames
{
    /// <summary>
    /// StruggleManager.cs
    /// ทำหน้าที่เป็น "ตัวกลาง" คุมการเปิด-ปิดมินิเกม Struggle ทั้งหมด:
    /// - ปิด/เปิดสคริปต์ควบคุมผู้เล่น (เช่น FirstPersonController) แบบชั่วคราว (ไม่ Destroy เด็ดขาด)
    /// - ปลดล็อก/ล็อกเมาส์ให้ตรงกับโหมด UI กับโหมด FPS
    /// - เปิด/ปิด StrugglePanel และฟังผลลัพธ์ (Success/Fail) จาก UnityEvent ของมัน
    /// - คุมว่าจะให้เล่นซ้ำได้ไหมถ้าล้มเหลว และล็อกไม่ให้เล่นซ้ำอีกถ้าทำสำเร็จแล้ว
    ///
    /// ในอนาคตเมื่อเปลี่ยนจาก Cube (1) ไปใช้ Ghost AI แทน
    /// ตัว Ghost AI เพียงแค่เรียก StruggleManager.Instance.OpenStruggle() แทนที่ StruggleTrigger
    /// ไม่ต้องแก้ไขสคริปต์นี้หรือ StrugglePanel เลย
    /// </summary>
    public class StruggleManager : MonoBehaviour
    {
        // Singleton แบบง่าย เพื่อให้ระบบอื่น (เช่น Ghost AI ในอนาคต) เรียกใช้ได้จากที่ไหนก็ได้
        public static StruggleManager Instance { get; private set; }

        [Header("References")]
        [Tooltip("ลาก StrugglePanel (ตัวที่มีสคริปต์ StrugglePanel.cs) มาวางที่นี่")]
        [SerializeField] private StrugglePanel strugglePanel;

        [Tooltip("ลากสคริปต์ควบคุมผู้เล่น เช่น FirstPersonController มาวางที่นี่ — ระบบจะสั่ง .enabled = false/true เท่านั้น ไม่มีการ Destroy")]
        [SerializeField] private MonoBehaviour playerControllerScript;

        [Header("Retry Settings")]
        [Tooltip("true = ถ้าผู้เล่น Fail สามารถเดินเข้าไปลองใหม่ได้อีก / false = Fail แล้วล็อกถาวรเหมือนกับ Success")]
        [SerializeField] private bool allowRetryAfterFail = true;

        /// <summary>Event ภายนอกสำหรับสคริปต์อื่น (เช่น StruggleTrigger ใช้เปลี่ยนสี Cube ทดสอบ)</summary>
        public event Action OnMinigameSuccess;
        public event Action OnMinigameFail;

        private bool hasSucceededOnce = false;
        private bool hasFailedOnce = false;
        private bool isMinigameActive = false;

        private void Awake()
        {
            // ป้องกันการมี StruggleManager ซ้ำซ้อนในฉากเดียวกัน
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (strugglePanel != null)
            {
                strugglePanel.OnStruggleSuccess.AddListener(HandleSuccess);
                strugglePanel.OnStruggleFail.AddListener(HandleFail);
            }
        }

        private void OnDisable()
        {
            if (strugglePanel != null)
            {
                strugglePanel.OnStruggleSuccess.RemoveListener(HandleSuccess);
                strugglePanel.OnStruggleFail.RemoveListener(HandleFail);
            }
        }

        /// <summary>
        /// เช็คว่าอนุญาตให้เปิดมินิเกมตอนนี้ได้หรือไม่
        /// (กำลังเล่นอยู่แล้ว / ทำสำเร็จไปแล้ว / Fail แล้วและไม่อนุญาตให้ลองใหม่ → เปิดไม่ได้)
        /// </summary>
        public bool CanTrigger()
        {
            if (isMinigameActive) return false;
            if (hasSucceededOnce) return false;
            if (hasFailedOnce && !allowRetryAfterFail) return false;
            return true;
        }

        /// <summary>
        /// เรียกจาก StruggleTrigger (หรือ Ghost AI ในอนาคต) เพื่อเปิดมินิเกม
        /// ลำดับการทำงาน: ปิดอินพุตผู้เล่น → ปลดล็อกเมาส์ → เปิด Panel → เริ่มมินิเกม
        /// </summary>
        public void OpenStruggle()
        {
            if (!CanTrigger() || strugglePanel == null) return;

            isMinigameActive = true;

            // ปิดใช้งานสคริปต์ควบคุมผู้เล่นชั่วคราว (ไม่ Destroy เด็ดขาด เพราะต้องเปิดกลับได้)
            if (playerControllerScript != null)
                playerControllerScript.enabled = false;

            // ปลดล็อกเมาส์ให้ผู้เล่นคลิกปุ่ม UI ได้ตามปกติ
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            strugglePanel.gameObject.SetActive(true);
            strugglePanel.StartMinigame();
        }

        private void HandleSuccess()
        {
            hasSucceededOnce = true;
            CloseStruggle();
            OnMinigameSuccess?.Invoke();
        }

        private void HandleFail()
        {
            hasFailedOnce = true;
            CloseStruggle();
            OnMinigameFail?.Invoke();
        }

        /// <summary>
        /// ปิดมินิเกมและคืนการควบคุมทั้งหมดให้ผู้เล่น
        /// ลำดับการทำงาน: ปิด Panel → เปิดอินพุตผู้เล่นกลับ → ล็อกเมาส์กลับเข้าโหมด FPS
        /// (จุดนี้คือส่วนที่เอกสารสเปกต้นฉบับพูดถึงแค่ "คืนการควบคุม" แต่ไม่ได้พูดถึงการล็อกเมาส์กลับ จึงเพิ่มให้ครบ)
        /// </summary>
        private void CloseStruggle()
        {
            isMinigameActive = false;
            strugglePanel.gameObject.SetActive(false);

            if (playerControllerScript != null)
                playerControllerScript.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}