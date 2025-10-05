// File: Player/ICharacterBehavior.cs

using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio
{
    public interface ICharacterBehavior
    {
        // Thuộc tính để lấy đối tượng Transform của nhân vật
        Transform Transform { get; }
        // Thuộc tính để lấy vị trí trung tâm (Center) của nhân vật
        Transform CenterTransform { get; }

        // Được gọi bởi PlayerBehavior để truyền CharacterData
        void SetCharacterData(CharacterData data); // <--- DÒNG BỔ SUNG

        // Đặt tốc độ hoạt ảnh (dùng cho Run/Idle)
        void SetSpeed(float speed);

        // Đặt localScale (dùng để lật nhân vật - flip)
        void SetLocalScale(Vector3 scale);

        // Đặt Sorting Order (dùng khi hồi sinh hoặc chết)
        void SetSortingOrder(int order);

        // Chơi hoạt ảnh hồi sinh
        void PlayReviveAnimation();
        // Chơi hoạt ảnh thất bại/chết
        void PlayDefeatAnimation();

        // Kích hoạt hiệu ứng nhấp nháy khi bị trúng đòn
        void FlashHit(UnityAction onFinish = null);

        // PHƯƠNG THỨC MỚI: Kích hoạt hoạt ảnh tấn công có điều kiện
        void PlayWeaponAttack(AbilityType abilityType); // <--- DÒNG BỔ SUNG
    }
}