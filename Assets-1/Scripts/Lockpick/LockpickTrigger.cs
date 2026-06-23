using UnityEngine;

/// <summary>
/// LockpickTrigger.cs
/// ติดสคริปต์นี้กับวัตถุในฉาก (ประตู, ตู้) ที่ต้องการให้สะเดาะกลอน
///
/// วิธีทำงาน:
/// - ต้องมี Collider ที่ติ๊ก "Is Trigger" บน GameObject นี้ (หรือ child)
/// - เมื่อผู้เล่น (ที่มี Tag = "Player") เดินเข้ามาในระยะ -> แสดง prompt และรอกด E
/// - กด E -> เรียก LockpickMinigame.StartMinigame()
/// - สมัครรับ event OnLockpickSuccess เพื่อสั่งเปิดประตูเมื่อสำเร็จ
/// </summary>
[RequireComponent(typeof(Collider))]
public class LockpickTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก GameObject ที่มี component LockpickMinigame มาใส่ (ตัวมินิเกมตัวกลางที่ใช้ร่วมกันได้หลายประตู)")]
    [SerializeField] private LockpickMinigame minigame;

    [Tooltip("ข้อความ prompt บนจอ เช่น 'กด E เพื่อสะเดาะกลอน' (Optional, ลาก TMP_Text มาใส่ได้)")]
    [SerializeField] private GameObject promptUI;

    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Tag ของผู้เล่น")]
    [SerializeField] private string playerTag = "Player";

    // เก็บ reference ของผู้เล่นที่อยู่ในระยะ เพื่อเอาไปหา IPlayerInputLock ตอนกด E
    private IPlayerInputLock playerInRangeLock;
    private bool playerInRange;

    private void Awake()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // ดึง interface IPlayerInputLock จาก player controller โดยอัตโนมัติ
        // (สคริปต์ player ของคุณต้อง implement IPlayerInputLock ไว้ ตามที่อธิบายในไฟล์ IPlayerInputLock.cs)
        playerInRangeLock = other.GetComponent<IPlayerInputLock>();
        playerInRange = true;

        if (promptUI != null)
            promptUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = false;
        playerInRangeLock = null;

        if (promptUI != null)
            promptUI.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange) return;
        if (minigame == null) return;

        if (Input.GetKeyDown(interactKey))
        {
            // ซ่อน prompt ระหว่างเล่นมินิเกม
            if (promptUI != null)
                promptUI.SetActive(false);

            // สมัครรับ event ก่อนเริ่ม (ใช้ += แบบนี้ทุกครั้งอาจสมัครซ้ำได้หลายรอบถ้าเปิดเกมหลายครั้ง
            // จึงต้อง -= ออกก่อนเสมอ เพื่อป้องกัน event ยิงซ้ำหลายครั้งในรอบถัดไป)
            minigame.OnMinigameSuccess -= HandleSuccess;
            minigame.OnMinigameSuccess += HandleSuccess;

            minigame.StartMinigame(playerInRangeLock);
        }
    }

    /// <summary>
    /// เรียกอัตโนมัติเมื่อมินิเกมสำเร็จ - ใส่ลอจิกเปิดประตู/ตู้ของคุณตรงนี้
    /// </summary>
    private void HandleSuccess()
    {
        Debug.Log($"{name}: ปลดล็อกสำเร็จ! ใส่โค้ดเปิดประตู/ตู้ตรงนี้ เช่น GetComponent<Animator>().SetTrigger(\"Open\");");

        // เลิกสมัครรับ event เพื่อไม่ให้ trigger นี้ตอบสนองซ้ำกับมินิเกมตัวอื่นที่ใช้ component เดียวกัน
        minigame.OnMinigameSuccess -= HandleSuccess;

        // ถ้าต้องการให้ปลดล็อกได้ครั้งเดียวแล้ว trigger นี้ใช้งานไม่ได้อีก ให้เพิ่ม:
        // enabled = false;
    }
}