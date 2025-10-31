using OctoberStudio.Bossfight;
using UnityEngine;
using UnityEngine.Events; // Cần thiết nếu ICharacterBehavior sử dụng UnityEvents

namespace OctoberStudio
{
    /// <summary>
    /// Giao diện mở rộng cho các hoạt ảnh/logic hiển thị đặc trưng của Boss Hero4D.
    /// Kế thừa ICharacterBehavior để giữ lại các hàm cơ bản (Run, Hit, Die).
    /// </summary>
    public interface IBossCharacterBehavior : ICharacterBehavior
    {
        /// <summary>
        /// Kích hoạt hoặc hủy kích hoạt trạng thái tụ lực/charge của Boss.
        /// </summary>
        /// <param name="startCharging">True để bắt đầu, False để kết thúc.</param>
        void PlayChargeAnimation(bool startCharging);

        /// <summary>
        /// Chơi hoạt ảnh tấn công cụ thể của Boss (nếu cần).
        /// </summary>
        void PlayAttackAnimation(BossType bossType);
        
        /// <summary>
        /// Chơi hoạt ảnh dịch chuyển tức thời (Teleport).
        /// </summary>
        void PlayTeleportAnimation();

        /// <summary>
        /// Thiết lập độ trong suốt (Alpha) cho Boss khi miễn nhiễm.
        /// </summary>
        void SetVisualsAlpha(float alpha);
    }
}
