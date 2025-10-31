using OctoberStudio.Extensions;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Enemy
{
    /// <summary>
    /// Logic cho Quái con của BossSpawner. 
    /// Minion này di chuyển thẳng bằng lực vật lý (bouncing) và tấn công khi chạm Player.
    /// </summary>
    public class BossMinionBehavior : EnemyBehavior
    {
        [Header("Minion Spawner Settings")]
        [SerializeField] float initialForce = 3f;
        [SerializeField] float attackAnimationDuration = 0.5f;

        private Vector2 _currentDirection;
        private Coroutine _attackRoutine;
        private bool _isAttacking = false;
        
        // Thuộc tính để truy cập Adapter Hero4D của Minion
        private ICharacterBehavior MinionVisuals => characterVisuals; 

        public override void Play()
        {
            base.Play();

            // Cài đặt ban đầu cho vật lý (đã được làm trong SpawnMinion, nhưng làm lại an toàn)
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector2.zero;
            }
            
            IsMoving = false; // Vô hiệu hóa logic di chuyển transform của lớp cha
        }

        public void LaunchMinion(Vector2 direction)
        {
            _currentDirection = direction.normalized;
            
            // Kích hoạt di chuyển bằng vật lý
            if (rb != null)
            {
                rb.AddForce(_currentDirection * initialForce, ForceMode2D.Impulse);
                // Giả định Minion Prefab có Collider với Bounciness = 1 để nảy
            }
        }

        protected override void Update()
        {
            // GHI ĐÈ Update để Minion tự điều khiển animation/direction theo vận tốc vật lý
            if (!IsAlive) return;

            if (rb != null)
            {
                Vector2 currentMoveDir = rb.velocity.normalized;
                float speed = rb.velocity.magnitude;

                // Cập nhật hướng và animation cho Hero4D Adapter
                if (MinionVisuals != null)
                {
                    if (MinionVisuals is EnemyHeroCharacterAdapter enemyAdapter)
                    {
                        enemyAdapter.SetMovementDirection(currentMoveDir);
                    }
                    
                    MinionVisuals.SetSpeed(speed); // Speed sẽ là 0 nếu nó bị kẹt
                    
                    // Logic lật hình ảnh theo hướng X
                    if (Mathf.Abs(currentMoveDir.x) > 0.01f)
                    {
                        MinionVisuals.SetLocalScale(new Vector3(currentMoveDir.x > 0 ? 1 : -1, 1, 1));
                    }
                }
            }
        }
        
        // SỬ DỤNG OnTriggerEnter2D từ BASE
        // Logic sẽ chạy trong PlayerBehavior.CheckTriggerEnter2D
        // và gọi TakeDamage cho Player.

        // Ghi đè phương thức va chạm để xử lý hoạt ảnh tấn công khi chạm Player
        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Gọi lớp cha để xử lý sát thương
            base.OnTriggerEnter2D(collision);
            
            if (_isAttacking) return;
            
            // Kiểm tra xem có va chạm với Player không
            if (collision.GetComponent<PlayerBehavior>() != null)
            {
                if (MinionVisuals != null)
                {
                    if (_attackRoutine != null) StopCoroutine(_attackRoutine);
                    _attackRoutine = StartCoroutine(AttackClipCoroutine());
                }
            }
        }

        private IEnumerator AttackClipCoroutine()
        {
            _isAttacking = true;
            
            // Chơi clip tấn công Hero4D
            MinionVisuals.PlayWeaponAttack(AbilityType.SteelSword); // Giả định dùng ID của Sword

            yield return new WaitForSeconds(attackAnimationDuration);

            _isAttacking = false;
        }

        protected override void Die(bool flash)
        {
            // Quan trọng: Tắt Kinematic khi chết (để đảm bảo không bị lỗi trong pool)
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0;
            }
            base.Die(flash);
        }
    }
}
