using UnityEngine;

/// <summary>
/// ควบคุมการเดิน วิ่ง หมอบคลาน หมุนกล้อง และระดับเสียงฝีเท้า (เวอร์ชันสมบูรณ์ แก้ไขบั๊กหมอบค้าง)
/// แนบที่: Player GameObject (ต้องมี CharacterController)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour, IPlayerInputLock
{
    [Header("Movement Speed")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float crouchSpeed = 1.8f;

    [Header("Crouch Settings")]
    [SerializeField] private float standingHeight = 2.0f;      // ความสูงยืนปกติ
    [SerializeField] private float crouchHeight = 1.2f;          // ความสูงตอนหมอบ
    [SerializeField] private float crouchTransitionSpeed = 8f;   // ความเร็วในการลด/ยกตัว
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Mouse Look")]
    [SerializeField] private Transform cameraTransform;          // ลาก Main Camera มาใส่
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minVerticalAngle = -80f;      // มุมก้มสูงสุด
    [SerializeField] private float maxVerticalAngle = 80f;       // มุมเงยสูงสุด

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Sound Meter (Footstep Logic)")]
    [SerializeField] private float sprintSoundLevel = 0.8f;
    [SerializeField] private float walkSoundLevel = 0.4f;
    [SerializeField] private float crouchSoundLevel = 0.0f;

    // ตัวแปรที่ระบบอื่น (AI ยาม/ผี, UI เกจเสียง) สามารถดึงไปอ่านค่าได้
    public float currentSoundLevel { get; private set; }

    // สถานะปัจจุบันของตัวละคร — สคริปต์อื่นอ่านได้
    public bool IsSprinting { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsMoving { get; private set; }

    /// <summary>
    /// false เมื่อมินิเกมเปิดอยู่ — MinigameManager จะปิดการรับอินพุตเดิน/มอง
    /// </summary>
    public bool IsInputEnabled { get; private set; } = true;

    private CharacterController characterController;
    private Vector3 velocity;           // ความเร็วแนวตั้ง (สำหรับแรงโน้มถ่วง)
    private float verticalRotation;     // มุมกล้องแนวตั้งที่สะสมขยับขึ้นลง
    private float currentHeight;        // ความสูงปัจจุบันของ CharacterController
    private float targetHeight;         // ความสูงเป้าหมายที่ต้องการ (ยืน หรือ หมอบ)

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // ตั้งค่าเริ่มต้นของตัวละครตอนเกิด
        currentHeight = standingHeight;
        targetHeight = standingHeight;
        characterController.height = standingHeight;
        characterController.center = new Vector3(0f, standingHeight / 2f, 0f);

        // ล็อกเมาส์ไว้ตรงกลางหน้าจอและซ่อนเคอร์เซอร์ไม่ให้กวนสายตา
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!IsInputEnabled)
        {
            IsMoving = false;
            IsSprinting = false;
            currentSoundLevel = 0f;
            return;
        }

        HandleMouseLook();
        HandleMovement();
        HandleCrouch();
        UpdateSoundLevel();
    }

    /// <summary>หมุนกล้องด้วยเมาส์ — ขยับเมาส์แนวนอนหมุนตัวละคร, แนวตั้งหมุนมุมกล้อง</summary>
    private void HandleMouseLook()
    {
        if (cameraTransform == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // หมุนตัวผู้เล่นแนวนอน (Yaw)
        transform.Rotate(Vector3.up * mouseX);

        // หมุนกล้องแนวตั้ง (Pitch) พร้อมจำกัดมุมมอง (Clamp) ไม่ให้กล้องหมุนตีลังกา
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    /// <summary>เคลื่อนที่ด้วยปุ่ม WASD + วิ่งเร็วด้วย Shift + คำนวณแรงโน้มถ่วงกดลงพื้น</summary>
    private void HandleMovement()
    {
        // อ่านค่า Input การกดเดิน (W, A, S, D หรือปุ่มลูกศร)
        float inputX = Input.GetAxisRaw("Horizontal"); // แกน X (ซ้าย/ขวา)
        float inputZ = Input.GetAxisRaw("Vertical");   // แกน Z (หน้า/หลัง)
        
        // คำนวณทิศทางเดินตามทิศที่ตัวละครหันหน้าไป
        Vector3 moveDirection = (transform.right * inputX + transform.forward * inputZ).normalized;

        // เช็กว่าผู้เล่นกำลังขยับตัวอยู่หรือไม่
        IsMoving = moveDirection.sqrMagnitude > 0.01f;

        // ดักจับปุ่ม Shift ทั้งซ้ายและขวา เพื่อป้องกันบั๊กคีย์บอร์ดภาษาไทย
        bool sprintKeyPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        // เงื่อนไขการวิ่ง: ต้องกด Shift + ต้องกำลังขยับ + ต้องไม่หมอบอยู่
        IsSprinting = sprintKeyPressed && IsMoving && !IsCrouching;
        
        // ตั้งค่าความเร็วเริ่มต้นเป็นเดินปกติ
        float currentSpeed = walkSpeed;

        // ปรับความเร็วตามสถานะปัจจุบัน
        if (IsCrouching)
            currentSpeed = crouchSpeed;
        else if (IsSprinting)
            currentSpeed = sprintSpeed;

        // เคลื่อนที่ในแนวราบ
        Vector3 move = moveDirection * currentSpeed;

        // ระบบแรงโน้มถ่วง (Gravity)
        if (characterController.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // กดตัวละครลงพื้นเล็กน้อยเวลาเดินบนพื้นต่างระดับ จะได้ไม่ลอย
        }

        velocity.y += gravity * Time.deltaTime;
        move.y = velocity.y;

        // สั่งให้ CharacterController ขยับตามการคำนวณทั้งหมด
        characterController.Move(move * Time.deltaTime);
    }

    /// <summary>ระบบหมอบคลาน — ยุบความสูงตัวและกล้องลงอย่าง Smooth พร้อมระบบเช็กเพดานก่อนลุก</summary>
    private void HandleCrouch()
    {
        // ถ้ากด Ctrl ค้างไว้ = สั่งให้หมอบ
        if (Input.GetKey(crouchKey))
        {
            IsCrouching = true;
            targetHeight = crouchHeight;
        }
        else
        {
            // ถ้าปล่อยปุ่ม Ctrl = จะเช็กก่อนว่าหัวชนเพดานไหมถึงจะยอมให้ลุกยืน
            // ยิงเลเซอร์จากจุดกึ่งกลางหัวตอนหมอบขึ้นไปด้านบนเป็นระยะทางสั้นๆ
            Vector3 rayOrigin = transform.position + Vector3.up * (crouchHeight - 0.05f);
            float checkDistance = (standingHeight - crouchHeight) + 0.2f;

            // ใช้ QueryTriggerInteraction.Ignore เพื่อให้เลเซอร์มองข้ามวัตถุล่องหน (Trigger Zone) ทั้งหมดในฉาก
            if (!Physics.Raycast(rayOrigin, Vector3.up, checkDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                IsCrouching = false;
                targetHeight = standingHeight;
            }
            else
            {
                // บั๊กเรดาร์เตือน: ถ้าปล่อยปุ่มแล้วตัวละครไม่ลุก โค้ดส่วนนี้จะพิมพ์บอกในหน้าต่าง Console ทันทีว่าหัวชนอะไรอยู่
                RaycastHit hit;
                if (Physics.Raycast(rayOrigin, Vector3.up, out hit, checkDistance))
                {
                    Debug.LogWarning("ตัวละครลุกไม่ได้เพราะหัวชนวัตถุฟิสิกส์ชื่อ: " + hit.collider.gameObject.name);
                }
            }
        }

        // ค่อยๆ ปรับความสูงของ Character Controller ให้นุ่มนวล (Smooth Transition) ด้วย Mathf.Lerp
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        characterController.height = currentHeight;
        characterController.center = new Vector3(0f, currentHeight / 2f, 0f);

        // ขยับมุมมองกล้อง Main Camera ลงมาให้ตรงระดับสายตาตอนหมอบ (ประมาณ 90% ของความสูงตัว)
        if (cameraTransform != null)
        {
            float targetCameraY = currentHeight * 0.9f;
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetCameraY, crouchTransitionSpeed * Time.deltaTime);
            cameraTransform.localPosition = camPos;
        }
    }

    /// <summary>คำนวณและอัปเดตระดับเสียงฝีเท้าตามความเร็ว เพื่อส่งค่าไปให้ AI ยามใช้ดักฟัง</summary>
    private void UpdateSoundLevel()
    {
        if (!IsMoving)
        {
            currentSoundLevel = 0f; // ถ้ายืนนิ่งๆ เสียงฝีเท้าเป็น 0
            return;
        }

        if (IsCrouching)
            currentSoundLevel = crouchSoundLevel; // ตอนหมอบคลาน เสียงฝีเท้าเบามาก (0.0)
        else if (IsSprinting)
            currentSoundLevel = sprintSoundLevel; // ตอนวิ่ง เสียงฝีเท้าดังที่สุด (0.8)
        else
            currentSoundLevel = walkSoundLevel;   // ตอนเดินปกติ เสียงฝีเท้าปานกลาง (0.4)
    }

    /// <summary>ฟังก์ชันเปิด-ปิดล็อกเมาส์ (เอาไว้เรียกใช้ร่วมกับเมนูหยุดเกม Pause Menu ในอนาคต)</summary>
    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    /// <summary>
    /// เปิด/ปิดการรับอินพุตเดินและมอง — MinigameManager เรียกเมื่อเปิด/ปิดมินิเกม
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        IsInputEnabled = enabled;
    }

    // ===== เพิ่มส่วนนี้: ทำให้ FirstPersonController รองรับ IPlayerInputLock =====
    // เมธอดเหล่านี้แค่ "ห่อ" SetInputEnabled ที่มีอยู่แล้วของคุณ
    // เพื่อให้ LockpickMinigame.cs เรียกใช้ผ่าน interface ได้โดยไม่ต้องรู้จักชื่อคลาสนี้ตรงๆ
    public void LockInput()
    {
        SetInputEnabled(false);
    }

    public void UnlockInput()
    {
        SetInputEnabled(true);
    }
}