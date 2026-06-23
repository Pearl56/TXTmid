using UnityEngine;

namespace MidnightSchool.Minigames
{
    [RequireComponent(typeof(SphereCollider))]
    public class Struggletrigger : MonoBehaviour
    {
        [SerializeField] private StruggleManager struggleManager;
        [SerializeField] private string playerTag = "Player";

        private void OnTriggerEnter(Collider other)
        {
            // เช็กว่าวัตถุที่เดินมาชนมี Tag คำว่า Player หรือไม่
            if (other.CompareTag(playerTag))
            {
                if (struggleManager != null)
                {
                    // เรียกใช้มินิเกม Struggle
                    struggleManager.OpenStruggle();
                }
                else
                {
                    Debug.LogError("ยังไม่ได้ลาก StruggleManager มาใส่ในช่อง struggleManager!");
                }
            }
        }
    }
}