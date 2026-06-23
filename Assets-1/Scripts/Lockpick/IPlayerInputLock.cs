using UnityEngine;

/// <summary>
/// Interface กลางสำหรับ "ล็อก/ปลดล็อก" การควบคุมผู้เล่น
/// ทำเป็น interface แทนการ Disable component ตรงๆ เพื่อให้:
/// 1. มินิเกมไม่ต้องรู้จักชื่อคลาส FirstPersonController โดยตรง (ลด coupling)
/// 2. ใช้กับระบบ minigame อื่น (Struggle, Wire) ร่วมกันได้ในอนาคต
///
/// วิธีใช้: ให้ FirstPersonController.cs ของคุณ "implement" interface นี้
/// เช่น: public class FirstPersonController : MonoBehaviour, IPlayerInputLock
/// แล้วเขียนเมธอด LockInput() / UnlockInput() ใส่ลอจิกปิด-เปิดการเคลื่อนที่จริงของคุณเข้าไป
/// </summary>
public interface IPlayerInputLock
{
    void LockInput();
    void UnlockInput();
}