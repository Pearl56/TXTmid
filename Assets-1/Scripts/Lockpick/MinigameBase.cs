using System;
using UnityEngine;

/// <summary>
/// คลาสฐานกลางสำหรับมินิเกมทุกตัว (Lockpick, Struggle, Wire, ฯลฯ)
/// </summary>
public abstract class MinigameBase : MonoBehaviour
{
    [Header("Base References (ทุกมินิเกมต้องมี)")]
    [Tooltip("Panel หลักของมินิเกมตัวนี้ ที่จะถูก SetActive(true/false)")]
    [SerializeField] protected GameObject panelRoot;

    [Header("Base Settings")]
    [SerializeField] protected KeyCode cancelKey = KeyCode.Escape;

    /// <summary>เรียกเมื่อมินิเกมสำเร็จ</summary>
    public event Action OnMinigameSuccess;

    /// <summary>เรียกเมื่อมินิเกมล้มเหลว</summary>
    public event Action OnMinigameFailed;

    /// <summary>เรียกเมื่อผู้เล่นกด Cancel ออกกลางคัน</summary>
    public event Action OnMinigameCancelled;

    /// <summary>true เมื่อมินิเกมกำลังเล่นอยู่</summary>
    public bool IsActive { get; private set; }

    private IPlayerInputLock playerLock;

    protected virtual void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    protected virtual void Update()
    {
        if (IsActive && Input.GetKeyDown(cancelKey))
            Cancel();
    }

    // ──────────────────────────────────────────────
    //  Public API — เรียกจากภายนอก
    // ──────────────────────────────────────────────

    /// <summary>เริ่มมินิเกม — ส่ง inputLock เป็น null ได้ถ้าไม่ต้องการล็อก Input ผู้เล่น</summary>
    public void StartMinigame(IPlayerInputLock inputLock = null)
    {
        playerLock = inputLock;
        IsActive   = true;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        playerLock?.LockInput();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        ResetMinigame();
    }

    // ──────────────────────────────────────────────
    //  Protected — เรียกจากมินิเกมลูก
    // ──────────────────────────────────────────────

    /// <summary>มินิเกมลูกเรียกตอนทำสำเร็จ</summary>
    protected void NotifySuccess()
    {
        if (!IsActive) return;
        EndMinigame();
        OnMinigameSuccess?.Invoke();
    }

    /// <summary>มินิเกมลูกเรียกตอนล้มเหลว</summary>
    protected void NotifyFailed()
    {
        if (!IsActive) return;
        EndMinigame();
        OnMinigameFailed?.Invoke();
    }

    // ──────────────────────────────────────────────
    //  Abstract — มินิเกมลูกต้อง implement
    // ──────────────────────────────────────────────

    /// <summary>รีเซ็ตค่าทั้งหมดให้พร้อมเล่นใหม่ — เรียกอัตโนมัติตอน StartMinigame()</summary>
    protected abstract void ResetMinigame();

    // ──────────────────────────────────────────────
    //  Private
    // ──────────────────────────────────────────────

    private void Cancel()
    {
        EndMinigame();
        OnMinigameCancelled?.Invoke();
    }

    private void EndMinigame()
    {
        IsActive = false;

        playerLock?.UnlockInput();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}