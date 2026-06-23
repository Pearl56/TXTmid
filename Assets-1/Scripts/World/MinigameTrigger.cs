using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ตรวจจับผู้เล่นเข้า Trigger Zone แล้วเปิดมินิเกมที่กำหนด
/// แนบที่: วัตถุในฉาก (ประตู, ตู้ไฟ, กล่อง) — ต้องมี Collider แบบ Is Trigger
///
/// การตั้งค่า Inspector:
/// - minigameType     → Lockpick / Struggle / Wire
/// - triggerOnce      → true = ใช้ได้ครั้งเดียว (ปิด Collider หลังสำเร็จ)
/// - playerTag        → "Player" (ต้องตั้ง Tag ให้ Player GameObject)
/// - onMinigameSuccess → UnityEvent เรียกเมื่อชนะ (เช่น Animator.SetTrigger("OpenDoor"))
/// - onMinigameFailed  → UnityEvent เมื่อแพ้ (optional)
/// </summary>
[RequireComponent(typeof(Collider))]
public class MinigameTrigger : MonoBehaviour
{
    [Header("Minigame Settings")]
    [Tooltip("ประเภทมินิเกมที่จะเปิดเมื่อผู้เล่นเข้า Trigger")]
    [SerializeField] private MinigameType minigameType = MinigameType.Lockpick;

    [Tooltip("true = เปิดได้ครั้งเดียว แล้วปิด Collider หลังสำเร็จ")]
    [SerializeField] private bool triggerOnce = true;

    [Tooltip("Tag ของ Player — ต้องตรงกับ Tag บน Player GameObject")]
    [SerializeField] private string playerTag = "Player";

    [Header("Events")]
    [Tooltip("เรียกเมื่อผู้เล่นทำมินิเกมสำเร็จ — เชื่อมเปิดประตู/กล่องที่นี่")]
    public UnityEvent onMinigameSuccess;

    [Tooltip("เรียกเมื่อผู้เล่นล้มเหลว (Struggle เท่านั้นที่มี fail)")]
    public UnityEvent onMinigameFailed;

    private Collider triggerCollider;
    private bool hasTriggered;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;

        if (!other.CompareTag(playerTag)) return;

        // ยืนยันว่าเป็นผู้เล่นจริง (มี FirstPersonController)
        if (other.GetComponent<FirstPersonController>() == null
            && other.GetComponentInParent<FirstPersonController>() == null)
            return;

        if (MinigameManager.Instance == null)
        {
            Debug.LogError("[MinigameTrigger] ไม่พบ MinigameManager ใน Scene — รัน Setup Step 2 ก่อน");
            return;
        }

        if (MinigameManager.Instance.IsMinigameActive) return;

        MinigameManager.Instance.OpenMinigame(
            minigameType,
            onSuccess: HandleSuccess,
            onFail: HandleFailed);
    }

    private void HandleSuccess()
    {
        onMinigameSuccess?.Invoke();

        if (triggerOnce)
        {
            hasTriggered = true;
            if (triggerCollider != null)
                triggerCollider.enabled = false;
        }
    }

    private void HandleFailed()
    {
        onMinigameFailed?.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        var col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
        }
    }
#endif
}
