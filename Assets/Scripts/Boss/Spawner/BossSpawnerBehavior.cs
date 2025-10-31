using OctoberStudio.Enemy;
using OctoberStudio.Extensions;
using System.Collections;
using UnityEngine;
using System.Linq; // Cần thiết cho các thao tác mảng/list

namespace OctoberStudio.Enemy
{
    /// <summary>
    /// Boss mới: Di chuyển (Miễn nhiễm) -> Đẻ Quái (Dễ tổn thương) -> Lặp lại.
    /// Kế thừa EnemyBehavior, sử dụng Adapter Boss Hero4D.
    /// </summary>
    public class BossSpawnerBehavior : EnemyBehavior
    {
        [Header("Boss Spawner Settings")]
        [SerializeField] float moveDuration = 7f;
        [SerializeField] float spawnDuration = 3f;
        [SerializeField] int minionsToSpawn = 4;
        [SerializeField] float spawnMinRadius = 1f;
        [SerializeField] float spawnMaxRadius = 2f;
        
        [Header("Minion Data (Phải là BossMinionBehavior)")]
        [Tooltip("EnemyType phải được định nghĩa trong EnemyDatabase và trỏ tới Prefab Minion.")]
        [SerializeField] EnemyType minionEnemyType = EnemyType.Shade; 

        private const float INVULNERABLE_ALPHA = 0.5f;
        private const float FULL_ALPHA = 1f;

        private IBossCharacterBehavior BossVisuals { get; set; }
        private Coroutine _behaviorCoroutine;

        public override void Play()
        {
            base.Play();
            
            // Ép kiểu Adapter từ lớp cha (EnemyBehavior)
            BossVisuals = characterVisuals as IBossCharacterBehavior;

            if (_behaviorCoroutine != null) StopCoroutine(_behaviorCoroutine);
            _behaviorCoroutine = StartCoroutine(BossBehaviorCoroutine());
        }

        private IEnumerator BossBehaviorCoroutine()
        {
            while (IsAlive)
            {
                // PHASE 1: DI CHUYỂN & MIỄN NHIỄM (7s)
                yield return MoveInvulnerablePhase();

                // PHASE 2: ĐỨNG YÊN & ĐẺ QUÁI (3s)
                yield return SpawnVulnerablePhase();
            }
        }

        private IEnumerator MoveInvulnerablePhase()
        {
            // 1. Kích hoạt Miễn nhiễm & Hiệu ứng mờ
            SetInvulnerability(true, INVULNERABLE_ALPHA);
            
            // Đảm bảo animation đang là RUN
            if (BossVisuals != null) BossVisuals.SetSpeed(1f);

            IsMoving = true;
            
            // 2. Di chuyển ngẫu nhiên
            Vector2 randomPoint = StageController.FieldManager.Fence.GetRandomPointInside(1f);
            
            IsMovingToCustomPoint = true;
            CustomPoint = randomPoint;

            float startTime = Time.time;

            while (Time.time < startTime + moveDuration)
            {
                // Cập nhật điểm đến nếu đã đến nơi
                if (Vector2.Distance(transform.position.XY(), randomPoint) < 0.2f)
                {
                    randomPoint = StageController.FieldManager.Fence.GetRandomPointInside(1f);
                    CustomPoint = randomPoint;
                }
                yield return null;
            }

            IsMovingToCustomPoint = false;
            IsMoving = false;
        }

        private IEnumerator SpawnVulnerablePhase()
        {
            // 1. Hủy Miễn nhiễm & Khôi phục Alpha
            SetInvulnerability(false, FULL_ALPHA);
            
            // Kích hoạt animation tụ lực/chuẩn bị đẻ quái
            if (BossVisuals != null) BossVisuals.PlayChargeAnimation(true);

            // 2. Đẻ quái lần lượt trong 3s
            float timeBetweenSpawns = spawnDuration / minionsToSpawn;

            for (int i = 0; i < minionsToSpawn; i++)
            {
                SpawnMinion();
                yield return new WaitForSeconds(timeBetweenSpawns);
            }
            
            // Chờ phần thời gian còn lại của 3s (nếu có)
            float elapsedSpawnTime = minionsToSpawn * timeBetweenSpawns;
            yield return new WaitForSeconds(Mathf.Max(0, spawnDuration - elapsedSpawnTime));

            // Kết thúc animation tụ lực/charge
            if (BossVisuals != null) BossVisuals.PlayChargeAnimation(false);
        }

        /// <summary>
        /// Thiết lập trạng thái miễn nhiễm và độ trong suốt của Boss.
        /// </summary>
        private void SetInvulnerability(bool isInvulnerable, float targetAlpha)
        {
            IsInvulnerable = isInvulnerable;

            // Áp dụng cho Legacy Boss (SpriteRenderer cũ)
            if (spriteRenderer != null)
            {
                spriteRenderer.DoAlpha(targetAlpha, 0.2f); //
            }
            // Áp dụng cho Hero4D Boss (qua Adapter)
            if (BossVisuals != null)
            {
                BossVisuals.SetVisualsAlpha(targetAlpha);
            }
        }

        private void SpawnMinion()
        {
            // Tính toán vị trí và hướng ngẫu nhiên
            Vector2 spawnOffset = Random.onUnitSphere.XY().normalized * Random.Range(spawnMinRadius, spawnMaxRadius);
            Vector2 spawnPosition = transform.position.XY() + spawnOffset;
            Vector2 initialDirection = spawnOffset.normalized;

            // Spawn Minion (Giả định Minion Prefab có BossMinionBehavior)
            // Minion mới sẽ kế thừa các chỉ số sát thương/HP của quái thường
            var minionEnemy = StageController.EnemiesSpawner.Spawn(minionEnemyType, spawnPosition);
            
            if (minionEnemy != null)
            {
                // Minion cần được đặt Rigidbody là Kinematic=false để di chuyển bằng vật lý
                if (minionEnemy.rb != null)
                {
                    minionEnemy.rb.isKinematic = false;
                }

                if (minionEnemy is BossMinionBehavior minion)
                {
                    // Bắn Minion ra với lực ban đầu
                    minion.LaunchMinion(initialDirection);
                }
            }
        }

        protected override void Die(bool flash)
        {
            if (_behaviorCoroutine != null) StopCoroutine(_behaviorCoroutine);
            base.Die(flash);
        }
    }
}
