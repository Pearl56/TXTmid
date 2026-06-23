using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ตัวกลางควบคุมมินิเกมทั้ง 3 ชนิด – เปิด/ปิด UI, ล็อกผู้เล่น, ปลดล็อกเมาส์
/// </summary>
public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance { get; private set; }

    [Header("Minigame Panels")]
    [SerializeField] private LockpickMinigame lockpickMinigame;
    [SerializeField] private StruggleMinigame struggleMinigame;
    [SerializeField] private WireMinigame wireMinigame;

    [Header("Player Reference")]
    [Tooltip("ลาก Player ที่มี FirstPersonController มาใส่")]
    [SerializeField] private FirstPersonController playerController;

    [Header("Canvas (Optional)")]
    [Tooltip("Canvas หลักของมินิเกม")]
    [SerializeField] private Canvas minigameCanvas;

    [Header("Events")]
    public UnityEvent onAnyMinigameOpened;
    public UnityEvent onAnyMinigameClosed;

    // ──────────────────────────────────────────────
    //  Public Properties — ใช้ใน MinigameTrigger
    // ──────────────────────────────────────────────

    /// <summary>
    /// true ถ้ามินิเกมใดกำลังเปิดอยู่ — ใช้ป้องกัน Trigger ซ้อนกัน
    /// </summary>
    public bool IsMinigameActive
    {
        get
        {
            return (lockpickMinigame != null && lockpickMinigame.IsActive)
                || (struggleMinigame  != null && struggleMinigame.IsActive)
                || (wireMinigame      != null && wireMinigame.IsActive);
        }
    }

    // ──────────────────────────────────────────────
    //  Awake
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // ──────────────────────────────────────────────
    //  OpenMinigame — เรียกจาก MinigameTrigger
    // ──────────────────────────────────────────────

    /// <summary>
    /// เปิดมินิเกมตาม MinigameType พร้อม callback onSuccess / onFail
    /// </summary>
    public void OpenMinigame(MinigameType type, Action onSuccess = null, Action onFail = null)
    {
        MinigameBase target = type switch
        {
            MinigameType.Lockpick => lockpickMinigame as MinigameBase,
            MinigameType.Struggle => struggleMinigame as MinigameBase,
            MinigameType.Wire     => wireMinigame     as MinigameBase,
            _                     => null
        };

        if (target == null)
        {
            Debug.LogWarning($"[MinigameManager] ไม่พบมินิเกมประเภท {type} — ตรวจสอบ Inspector");
            return;
        }

        // ลงทะเบียน callback ชั่วคราว แล้วถอดออกเมื่อรับเหตุการณ์แล้ว
        if (onSuccess != null)
        {
            Action successHandler = null;
            successHandler = () =>
            {
                target.OnMinigameSuccess -= successHandler;
                onSuccess();
                onAnyMinigameClosed?.Invoke();
            };
            target.OnMinigameSuccess += successHandler;
        }

        if (onFail != null)
        {
            Action failHandler = null;
            failHandler = () =>
            {
                target.OnMinigameFailed -= failHandler;
                onFail();
                onAnyMinigameClosed?.Invoke();
            };
            target.OnMinigameFailed += failHandler;
        }

        // จัดการ Cancel — ปิด event โดยไม่ trigger onSuccess/onFail
        {
            Action cancelHandler = null;
            cancelHandler = () =>
            {
                target.OnMinigameCancelled -= cancelHandler;
                onAnyMinigameClosed?.Invoke();
            };
            target.OnMinigameCancelled += cancelHandler;
        }

        target.StartMinigame(playerController as IPlayerInputLock);
        onAnyMinigameOpened?.Invoke();
    }

    // ──────────────────────────────────────────────
    //  เปิดมินิเกมแบบตรง (ไม่ต้องการ callback)
    // ──────────────────────────────────────────────

    public void OpenLockpick() => OpenMinigame(MinigameType.Lockpick);
    public void OpenStruggle() => OpenMinigame(MinigameType.Struggle);
    public void OpenWire()     => OpenMinigame(MinigameType.Wire);

    // ──────────────────────────────────────────────
    //  ปิดมินิเกม (force-close จากภายนอก)
    // ──────────────────────────────────────────────

    public void CloseAll()
    {
        if (lockpickMinigame != null && lockpickMinigame.IsActive)
            lockpickMinigame.gameObject.SetActive(false);
        if (struggleMinigame != null && struggleMinigame.IsActive)
            struggleMinigame.gameObject.SetActive(false);
        if (wireMinigame != null && wireMinigame.IsActive)
            wireMinigame.gameObject.SetActive(false);

        onAnyMinigameClosed?.Invoke();
    }
}