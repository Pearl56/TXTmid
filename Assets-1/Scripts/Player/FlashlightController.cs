using UnityEngine;

/// <summary>
/// เปิด-ปิดไฟฉายด้วยปุ่ม F
/// แนบที่: Flashlight GameObject (ลูกของ Main Camera, มี Light แบบ Spot)
/// </summary>
[RequireComponent(typeof(Light))]
public class FlashlightController : MonoBehaviour
{
    [Header("Flashlight Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F;
    [SerializeField] private bool startEnabled = false;  // เริ่มเกมปิดไฟ (Horror mood)

    [Header("Optional — Battery (Step 2+)")]
    [SerializeField] private bool useBattery = false;
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float drainRate = 2f;       // ลดแบตต่อวินาที

    public bool IsOn { get; private set; }
    public float BatteryLevel { get; private set; }

    private Light spotLight;

    private void Awake()
    {
        spotLight = GetComponent<Light>();
        spotLight.type = LightType.Spot;  // บังคับเป็น Spotlight

        BatteryLevel = maxBattery;
        IsOn = startEnabled;
        spotLight.enabled = IsOn;
    }

    private void Update()
    {
        // กด F สลับเปิด/ปิด
        if (Input.GetKeyDown(toggleKey))
            ToggleFlashlight();

        // ระบบแบตเตอรี่ (ปิดไว้ก่อนใน Step 1)
        if (useBattery && IsOn)
        {
            BatteryLevel -= drainRate * Time.deltaTime;
            if (BatteryLevel <= 0f)
            {
                BatteryLevel = 0f;
                TurnOff();
            }
        }
    }

    public void ToggleFlashlight()
    {
        if (IsOn)
            TurnOff();
        else
            TurnOn();
    }

    public void TurnOn()
    {
        // ถ้าใช้แบตและหมด เปิดไม่ได้
        if (useBattery && BatteryLevel <= 0f) return;

        IsOn = true;
        spotLight.enabled = true;
    }

    public void TurnOff()
    {
        IsOn = false;
        spotLight.enabled = false;
    }
}