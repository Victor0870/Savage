// File: Scripts/Boss/Spawner/BossMinionBehavior.cs

using OctoberStudio.Extensions;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using OctoberStudio; // ✅ BỔ SUNG: Cần thiết để tham chiếu ICharacterBehavior

namespace OctoberStudio.Enemy
{
    // Chúng ta định nghĩa lại Enum ở đây để đảm bảo Minion có thể thấy nó.
    public enum EnemyAttackMode
    {
        Contact = 0,
        Proximity = 1
    }

    /// <summary>
    /// Logic cho Quái con của BossSpawner.
    /// Minion này di chuyển thẳng bằng lực vật lý (bouncing) và tấn công khi chạm Player.
    /// </summary>
    public class BossMinionBehavior : EnemyBehavior
    {
        [Header("Minion Spawner Settings")]
        [SerializeField] float initialForce = 3f;

        // CÁC TRƯỜNG _isAttacking VÀ _attackRoutine ĐƯỢC KẾ THỪA (PROTECTED) TỪ EnemyBehavior.cs

        private Vector2 _currentDirection;

        // Thuộc tính để truy cập Adapter Hero4D của Minion
        private ICharacterBehavior MinionVisuals => characterVisuals;

        public override void Play()
        {
            base.Play(); // Chạy logic khởi tạo MaxHP/Speed từ EnemyBehavior

            // THAY ĐỔI 1: BẬT LẠI PHYSICS (dùng lực)
            if (rb != null)
            {
                rb.isKinematic = false; // Quan trọng: Bật lại chế độ vật lý
                rb.linearVelocity = Vector2.zero;
            }

            IsMoving = false; // Quan trọng: Tắt cờ di chuyển bằng transform của EnemyBehavior
        }

        // THAY ĐỔI 2: ÁP DỤNG LỰC ĐẨY BAN ĐẦU VÀ ÉP KIỂU ĐỂ GỌI SetMovementDirection
        public void LaunchMinion(Vector2 direction)
        {
            _currentDirection = direction.normalized;
            if (rb != null)
            {
                // Áp dụng vận tốc ban đầu để minion di chuyển thẳng
                rb.linearVelocity = _currentDirection * initialForce;
            }

            // Thiết lập hướng và hoạt ảnh chạy ban đầu cho visuals
            if (MinionVisuals is EnemyHeroCharacterAdapter enemyAdapter)
            {
                // ✅ FIX CS1061: Ép kiểu sang lớp cụ thể để gọi phương thức
                enemyAdapter.SetMovementDirection(_currentDirection);
            }
            // Thử ép kiểu với BossHeroCharacterAdapter nếu Minion dùng nó
            else if (MinionVisuals is BossHeroCharacterAdapter bossAdapter)
            {
                // ✅ FIX CS1061: Ép kiểu sang lớp cụ thể để gọi phương thức
                bossAdapter.SetMovementDirection(_currentDirection);
            }

            MinionVisuals.SetSpeed(initialForce);
        }

        // THAY ĐỔI 3: CẬP NHẬT VISUALS DỰA TRÊN VẬN TỐC VẬT LÝ VÀ ÉP KIỂU
        protected override void Update()
        {
            base.Update(); // Giữ lại để xử lý logic cơ sở của EnemyBehavior

            if (!IsAlive || rb == null) return;

            // Lấy vận tốc hiện tại để cập nhật hướng nhìn (sẽ thay đổi sau khi nảy)
            float currentSpeed = rb.linearVelocity.magnitude;

            if (currentSpeed > 0.01f)
            {
                _currentDirection = rb.linearVelocity.normalized;

                // Cập nhật hướng di chuyển và tốc độ cho Adapter (Adapter sẽ xử lý 4 hướng)
                if (MinionVisuals is EnemyHeroCharacterAdapter enemyAdapter)
                {
                    // ✅ FIX CS1061: Ép kiểu sang lớp cụ thể để gọi phương thức
                    enemyAdapter.SetMovementDirection(_currentDirection);
                }
                else if (MinionVisuals is BossHeroCharacterAdapter bossAdapter)
                {
                    // ✅ FIX CS1061: Ép kiểu sang lớp cụ thể để gọi phương thức
                    bossAdapter.SetMovementDirection(_currentDirection);
                }

                MinionVisuals.SetSpeed(currentSpeed);
            }
            else
            {
                // Nếu minion đã dừng, đặt về Idle
                MinionVisuals.SetSpeed(0f);
            }
        }

        protected override void OnTriggerEnter2D(Collider2D collision)
        {
            // Chỉ gọi lớp cha để xử lý hit từ Projectile/Minion khác.
            base.OnTriggerEnter2D(collision);
        }

        protected override void Die(bool flash)
        {
            // Tắt Kinematic khi chết để reset Minion
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0;
            }
            base.Die(flash);
        }
    }
}