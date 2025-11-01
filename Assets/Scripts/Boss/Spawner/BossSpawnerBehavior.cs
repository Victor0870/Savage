// File: BossSpawnerBehavior.cs (Đã sửa lỗi CS1540 và CS1061)

using OctoberStudio.Bossfight;
using OctoberStudio.Enemy;
using OctoberStudio.Extensions;
using System.Collections;
using UnityEngine;
using System.Linq; 
using OctoberStudio.Easing; // <-- KHẮC PHỤC CS1061 (DoAlpha)

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
        
        [Header("Minion Data")]
        [Tooltip("EnemyType phải được định nghĩa trong EnemyDatabase và trỏ tới Prefab Minion.")]
        [SerializeField] EnemyType minionEnemyType = EnemyType.Shade; 

        private const float INVULNERABLE_ALPHA = 0.5f;
        private const float FULL_ALPHA = 1f;

        private IBossCharacterBehavior BossVisuals => characterVisuals as IBossCharacterBehavior;
        
        private Coroutine _behaviorCoroutine;

        public override void Play()
        {
            base.Play();
            
            // Ép kiểu Adapter từ lớp cha (EnemyBehavior)
            // LƯU Ý: characterVisuals là protected, nên truy cập trực tiếp (không cần tiền tố this. hoặc BossSpawnerBehavior.)
           // BossVisuals = characterVisuals as IBossCharacterBehavior;

            if (_behaviorCoroutine != null) StopCoroutine(_behaviorCoroutine);
            _behaviorCoroutine = StartCoroutine(BossBehaviorCoroutine());
        }
        
        private IEnumerator BossBehaviorCoroutine()
        {
            while (IsAlive)
            {
                yield return MoveInvulnerablePhase();
                yield return SpawnVulnerablePhase();
            }
        }

        private IEnumerator MoveInvulnerablePhase()
        {
            // 1. Kích hoạt Miễn nhiễm & Hiệu ứng mờ
            SetInvulnerability(true, INVULNERABLE_ALPHA);
            
            if (BossVisuals != null) BossVisuals.SetSpeed(1f);

            IsMoving = true;
            
            // 2. Di chuyển ngẫu nhiên
            Vector2 randomPoint = StageController.FieldManager.Fence.GetRandomPointInside(1f);
            
            IsMovingToCustomPoint = true;
            CustomPoint = randomPoint;

            float startTime = Time.time;

            while (Time.time < startTime + moveDuration)
            {
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
            
            if (BossVisuals != null) BossVisuals.PlayChargeAnimation(true);

            // 2. Đẻ quái lần lượt trong 3s
            float timeBetweenSpawns = spawnDuration / minionsToSpawn;

            for (int i = 0; i < minionsToSpawn; i++)
            {
                SpawnMinion();
                yield return new WaitForSeconds(timeBetweenSpawns);
            }
            
            float elapsedSpawnTime = minionsToSpawn * timeBetweenSpawns;
            yield return new WaitForSeconds(Mathf.Max(0, spawnDuration - elapsedSpawnTime));

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
                spriteRenderer.DoAlpha(targetAlpha, 0.2f); 
            }
            // Áp dụng cho Hero4D Boss (qua Adapter)
            if (BossVisuals != null)
            {
                BossVisuals.SetVisualsAlpha(targetAlpha);
            }
        }

        private void SpawnMinion()
        {
            Vector2 spawnOffset = Random.onUnitSphere.XY().normalized * Random.Range(spawnMinRadius, spawnMaxRadius);
            Vector2 spawnPosition = transform.position.XY() + spawnOffset;
            Vector2 initialDirection = spawnOffset.normalized;

            var minionEnemy = StageController.EnemiesSpawner.Spawn(minionEnemyType, spawnPosition);
            
            if (minionEnemy != null)
            {
                // KHẮC PHỤC CS1540: Truy cập RB thông qua Public Property minionEnemy.RB
                if (minionEnemy.RB != null)
                {
                    minionEnemy.RB.isKinematic = false;
                    minionEnemy.RB.linearVelocity = Vector2.zero; 
                }

                if (minionEnemy is BossMinionBehavior minion)
                {
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