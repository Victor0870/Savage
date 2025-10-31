using OctoberStudio.Easing;
using OctoberStudio.Enemy;
using OctoberStudio.Extensions;
using OctoberStudio.Timeline;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System.Collections;
using System.Runtime.CompilerServices;

namespace OctoberStudio
{
    public class EnemyBehavior : MonoBehaviour
    {
        // HẰNG SỐ GỐC DÙNG CHO SHADER CỦA LEGACY ENEMY
        protected static readonly int _Overlay = Shader.PropertyToID("_Overlay");
        protected static readonly int _Disolve = Shader.PropertyToID("_Disolve");

        private static readonly int HIT_HASH = "Hit".GetHashCode();

        [Header("Settings")]
        [Tooltip("The speed of the enemy")]
        [SerializeField] protected float speed;
        public float Speed { get; protected set; }

        [Tooltip("The LevelData's 'Enemy Damage' is multiplied by this value to determine the damage of the enemy on each level")]
        [SerializeField] float damage = 1f;

        [Tooltip("The LevelData's 'Enemy HP' is multiplied by this value to determine the HP of the enemy on each level")]
        [SerializeField] float hp;

        [FormerlySerializedAs("canBekickedBack")]
        [SerializeField] bool canBeKickedBack = true;

        [SerializeField] bool shouldFadeIn;

        [Header("References (Legacy)")]
        [SerializeField] Rigidbody2D rb;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] DissolveSettings dissolveSettings;
        [SerializeField] SpriteRenderer shadowSprite;

        // --- BỔ SUNG CHO HERO4D VÀ TẤN CÔNG ---
        [Header("Visuals (Hero4D)")]
        [Tooltip("Adapter xử lý visuals và animation (Boss/Enemy Hero4D Adapter)")]
        [SerializeField] OctoberStudio.ICharacterBehavior characterVisuals; // <--- DÙNG CHUNG

        [Header("Attack on Contact")]
        [Tooltip("Bật tính năng chơi animation tấn công khi chạm Player (chỉ Hero4D)")]
        [SerializeField] bool attackOnContact = false;
        [SerializeField] float attackAnimationDuration = 0.5f;

        private Coroutine _attackRoutine;
        private bool _isAttacking = false;
        // --- KẾT THÚC BỔ SUNG ---

        [SerializeField] Collider2D enemyCollider;

        public Vector2 Center => enemyCollider.bounds.center;

        [Header("Hit")]
        [SerializeField] float hitScaleAmount = 0.2f;
        [SerializeField] Color hitColor = Color.white;

        public EnemyData Data { get; private set; }
        public WaveOverride WaveOverride { get; protected set; }

        // SỬA: Logic kiểm tra hiển thị
        public bool IsVisible => characterVisuals != null
            ? characterVisuals.Transform.GetComponentInChildren<SpriteRenderer>()?.isVisible ?? true
            : (spriteRenderer != null ? spriteRenderer.isVisible : true);

        public bool IsAlive => HP > 0;
        public bool IsInvulnerable { get; protected set; }

        public float HP { get; private set; }
        public float MaxHP { get; private set; }

        public bool ShouldSpawnChestOnDeath { get; set; }

        IEasingCoroutine fallBackCoroutine;

        private Dictionary<EffectType, List<Effect>> appliedEffects = new Dictionary<EffectType, List<Effect>>();

        protected bool IsMoving { get; set; }
        public bool IsMovingToCustomPoint { get; protected set; }
        public Vector2 CustomPoint { get; protected set; }

        public float LastTimeDamagedPlayer { get; set; }

        // LEGACY FIELDS
        private Material sharedMaterial;
        private Material effectsMaterial;
        private float shadowAlpha;

        public event UnityAction<EnemyBehavior> onEnemyDied;
        public event UnityAction<float, float> onHealthChanged;

        private float lastTimeSwitchedDirection = 0;

        IEasingCoroutine damageCoroutine;
        protected IEasingCoroutine scaleCoroutine;
        IEasingCoroutine fadeInCoroutine;

        private float damageTextValue;
        private float lastTimeDamageText;

        private static int lastFrameHitSound;
        private float lastTimeHitSound;

        protected virtual void Awake()
        {
            // LOGIC CŨ: Khởi tạo Material/Shader cho Legacy Enemy
            if (spriteRenderer != null)
            {
                sharedMaterial = spriteRenderer.sharedMaterial;
                effectsMaterial = Instantiate(sharedMaterial);
            }
            if (shadowSprite != null)
            {
                shadowAlpha = shadowSprite.color.a;
            }

            // BỔ SUNG: TÌM KIẾM ADAPTER
            if (characterVisuals == null)
            {
                characterVisuals = GetComponentInChildren<OctoberStudio.ICharacterBehavior>();
            }
        }

        public void SetData(EnemyData data)
        {
            Data = data;
        }

        public void SetWaveOverride(WaveOverride waveOverride)
        {
            WaveOverride = waveOverride;
        }

        public virtual void Play()
        {
            MaxHP = StageController.Stage.EnemyHP * hp;
            Speed = speed;
            if (WaveOverride != null)
            {
                MaxHP = WaveOverride.ApplyHPOverride(MaxHP);
                Speed = WaveOverride.ApplySpeedOverride(Speed);
            }

            HP = MaxHP;
            IsMoving = true;

            if (shadowSprite != null) shadowSprite.SetAlpha(shadowAlpha);

            // BỔ SUNG: Kiểm tra an toàn cho Collider
            if (enemyCollider != null)
            {
                enemyCollider.enabled = true;
            }

            if (shouldFadeIn)
            {
                // MỚI: Kích hoạt Hero4D hoặc Sprite Renderer cũ
                if (characterVisuals != null)
                {
                    characterVisuals.Transform.gameObject.SetActive(true);
                }
                else if (spriteRenderer != null)
                {
                    spriteRenderer.SetAlpha(0);
                    fadeInCoroutine = spriteRenderer.DoAlpha(1, 0.2f);
                }
            }
        }

        protected virtual void Update()
        {
            if (!IsAlive || !IsMoving || PlayerBehavior.Player == null) return;

            Vector3 target = IsMovingToCustomPoint ? CustomPoint : PlayerBehavior.Player.transform.position;

            Vector3 direction = (target - transform.position).normalized;

            float speed = Speed;

            if (appliedEffects.TryGetValue(EffectType.Speed, out var speedEffects))
            {
                for (int i = 0; i < speedEffects.Count; i++)
                {
                    Effect effect = speedEffects[i];
                    speed *= effect.Modifier;
                }
            }

            transform.position += direction * Time.deltaTime * speed;

            // --- LOGIC CHUYỂN ĐỘNG & FLIP (CHỐNG NHẤP NHÁY) ---
            if (!scaleCoroutine.ExistsAndActive())
            {
                // MỚI: Dùng Adapter Hero4D
                if (characterVisuals != null)
                {
                    var currentScaleX = transform.localScale.x;

                    // Logic chống nhấp nháy
                    if ((direction.x > 0 && currentScaleX < 0) || (direction.x < 0 && currentScaleX > 0))
                    {
                        if (Time.unscaledTime - lastTimeSwitchedDirection > 0.1f)
                        {
                            characterVisuals.SetLocalScale(new Vector3(direction.x > 0 ? 1 : -1, 1, 1));
                            lastTimeSwitchedDirection = Time.unscaledTime;
                        }
                    }
                    else if (direction.x != 0)
                    {
                        // Vẫn update scale để áp dụng Player.SizeMultiplier & duy trì hướng
                        characterVisuals.SetLocalScale(new Vector3(currentScaleX > 0 ? 1 : -1, 1, 1));
                    }

                    // Cập nhật tốc độ và hướng cho Adapter
                    if (characterVisuals is EnemyHeroCharacterAdapter enemyAdapter)
                    {
                        enemyAdapter.SetMovementDirection(direction.XY());
                    }
                    characterVisuals.SetSpeed(direction.magnitude * speed);
                }
                // CŨ: Dùng transform.localScale trực tiếp (Legacy)
                else
                {
                    var scale = transform.localScale;

                    if (direction.x > 0 && scale.x < 0 || direction.x < 0 && scale.x > 0)
                    {
                        if (Time.unscaledTime - lastTimeSwitchedDirection > 0.1f)
                        {
                            scale.x *= -1;
                            transform.localScale = scale;

                            lastTimeSwitchedDirection = Time.unscaledTime;
                        }
                    }
                }
            }
        }

        // PHƯƠNG THỨC GỐC: Chỉ xử lý Projectile (đạn)
        private void OnTriggerEnter2D(Collider2D other)
        {
            ProjectileBehavior projectile = other.GetComponent<ProjectileBehavior>();

            if (projectile != null)
            {
                TakeDamage(PlayerBehavior.Player.Damage * projectile.DamageMultiplier);

                if (HP > 0)
                {
                    if (projectile.KickBack && canBeKickedBack)
                    {
                        KickBack(PlayerBehavior.CenterPosition);
                    }

                    if (projectile.Effects != null && projectile.Effects.Count > 0)
                    {
                        AddEffects(projectile.Effects);
                    }
                }
            }
        }

        // --- BỔ SUNG: LOGIC TẤN CÔNG KHI CHẠM PLAYER (ĐƯỢC GỌI TỪ PLAYERBEHAVIOR) ---
        public void CheckTriggerEnter2D(Collider2D collision)
        {
            if (!attackOnContact) return;
            if (_isAttacking) return;

            // Kích hoạt clip tấn công nếu chạm Player
            if (collision.GetComponent<PlayerBehavior>() != null)
            {
                // CHỈ KÍCH HOẠT ANIMATION nếu là Hero4D Enemy
                if (characterVisuals != null)
                {
                    if (_attackRoutine != null) StopCoroutine(_attackRoutine);
                    _attackRoutine = StartCoroutine(AttackClipCoroutine());
                }
            }
        }

        private IEnumerator AttackClipCoroutine()
        {
            _isAttacking = true;

            if (characterVisuals != null) // <--- ĐẢM BẢO CHỈ CHẠY NẾU CÓ ADAPTER
            {
                // Kích hoạt animation tấn công (sử dụng logic PlayWeaponAttack trong Adapter)
                characterVisuals.PlayWeaponAttack(AbilityType.SteelSword); 
            }

            yield return new WaitForSeconds(attackAnimationDuration);

            _isAttacking = false;
        }
        // --- KẾT THÚC BỔ SUNG ---

        public float GetDamage()
        {
            var damage = this.damage;
            if (WaveOverride != null) damage = WaveOverride.ApplyDamageOverride(damage);

            var baseDamage = StageController.Stage.EnemyDamage * damage;

            if (appliedEffects.ContainsKey(EffectType.Damage))
            {
                var damageEffects = appliedEffects[EffectType.Damage];

                for (int i = 0; i < damageEffects.Count; i++)
                {
                    var effect = damageEffects[i];

                    baseDamage *= effect.Modifier;
                }
            }

            return baseDamage;
        }

        public List<EnemyDropData> GetDropData()
        {
            if (WaveOverride != null) return WaveOverride.ApplyDropOverride(Data.EnemyDrop);
            return Data.EnemyDrop;
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;
            if (IsInvulnerable) return;

            HP -= damage;

            onHealthChanged?.Invoke(HP, MaxHP);

            // Showing Damage Text
            damageTextValue += damage;
            if (Time.unscaledTime - lastTimeDamageText > 0.2f && damageTextValue >= 1)
            {
                var damageText = Mathf.RoundToInt(damageTextValue).ToString();
                StageController.WorldSpaceTextManager.SpawnText(transform.position + new Vector3(Random.Range(-0.1f, 0.1f), Random.value * 0.1f), damageText);

                damageTextValue = 0;
                lastTimeDamageText = Time.unscaledTime;
            }
            else
            {
                damageTextValue += damage;
            }

            // Playing Damage Sound
            if (Time.frameCount != lastFrameHitSound && Time.unscaledTime - lastTimeHitSound > 0.2f)
            {
                GameController.AudioManager.PlaySound(HIT_HASH);

                lastFrameHitSound = Time.frameCount;
                lastTimeHitSound = Time.unscaledTime;
            }

            if (HP <= 0)
            {
                Die(true);
            }
            else
            {
                // LOGIC FLASH ON HIT
                if (characterVisuals != null)
                {
                    characterVisuals.FlashHit(); // MỚI: Dùng Adapter
                }
                else if (spriteRenderer != null)
                {
                    // CŨ: Dùng SpriteRenderer
                    if (!damageCoroutine.ExistsAndActive())
                    {
                        FlashHit(true);
                    }
                }

                // Scaling on Hit (Giữ nguyên)
                if (!scaleCoroutine.ExistsAndActive())
                {
                    var x = transform.localScale.x;

                    scaleCoroutine = transform.DoLocalScale(new Vector3(x * (1 - hitScaleAmount), (1 + hitScaleAmount), 1), 0.07f).SetEasing(EasingType.SineOut).SetOnFinish(() =>
                    {
                        scaleCoroutine = transform.DoLocalScale(new Vector3(x, 1, 1), 0.07f).SetEasing(EasingType.SineInOut);
                    });
                }
            }
        }

        private void FlashHit(bool resetMaterial, UnityAction onFinish = null)
        {
            // Kiểm tra an toàn cho Legacy Enemy
            if (spriteRenderer == null || effectsMaterial == null) return;

            spriteRenderer.material = effectsMaterial;

            var transparentColor = hitColor;
            transparentColor.a = 0;

            effectsMaterial.SetColor(_Overlay, transparentColor);

            damageCoroutine = effectsMaterial.DoColor(_Overlay, hitColor, 0.05f).SetOnFinish(() =>
            {
                damageCoroutine = effectsMaterial.DoColor(_Overlay, transparentColor, 0.05f).SetOnFinish(() =>
                {
                    if (resetMaterial) spriteRenderer.material = sharedMaterial;
                    onFinish?.Invoke();
                });
            });
        }

        public void Kill()
        {
            HP = 0;

            Die(false);
        }

        protected virtual void Die(bool flash)
        {
            if (enemyCollider != null) enemyCollider.enabled = false;

            damageCoroutine.StopIfExists();

            onEnemyDied?.Invoke(this);
            fallBackCoroutine.StopIfExists();
            rb.simulated = true;

            fadeInCoroutine.StopIfExists();

            // LOGIC DIE
            if (characterVisuals != null)
            {
                // MỚI: Dùng Adapter
                characterVisuals.PlayDefeatAnimation();

                // Ẩn gameObject sau 2s (thời gian chạy animation chết)
                EasingManager.DoAfter(2f, () => gameObject.SetActive(false));
            }
            else if (spriteRenderer != null && dissolveSettings != null && effectsMaterial != null)
            {
                // CŨ: Dùng Dissolve Shader
                spriteRenderer.material = effectsMaterial;

                if (flash)
                {
                    FlashHit(false, () =>
                    {
                        effectsMaterial.SetColor(_Overlay, Color.clear);
                        effectsMaterial.DoColor(_Overlay, dissolveSettings.DissolveColor, dissolveSettings.Duration - 0.1f);
                    });
                }
                else
                {
                    effectsMaterial.SetColor(_Overlay, Color.clear);
                    effectsMaterial.DoColor(_Overlay, dissolveSettings.DissolveColor, dissolveSettings.Duration);
                }

                effectsMaterial.SetFloat(_Disolve, 0);
                effectsMaterial.DoFloat(_Disolve, 1, dissolveSettings.Duration + 0.02f).SetEasingCurve(dissolveSettings.DissolveCurve).SetOnFinish(() =>
                {
                    effectsMaterial.SetColor(_Overlay, Color.clear);
                    effectsMaterial.SetFloat(_Disolve, 0);

                    gameObject.SetActive(false);
                    spriteRenderer.material = sharedMaterial;
                });

                if (shadowSprite != null) shadowSprite.DoAlpha(0, dissolveSettings.Duration);
            }
            else
            {
                // Fallback an toàn
                gameObject.SetActive(false);
            }

            appliedEffects.Clear();
            WaveOverride = null;
        }

        public void KickBack(Vector3 position)
        {
            var direction = (transform.position - position).normalized;
            rb.simulated = false;
            fallBackCoroutine.StopIfExists();
            fallBackCoroutine = transform.DoPosition(transform.position + direction * 0.6f, 0.15f).SetEasing(EasingType.ExpoOut).SetOnFinish(() => rb.simulated = true);
        }

        public void AddEffects(List<Effect> effects)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                AddEffect(effects[i]);
            }
        }

        public void AddEffect(Effect effect)
        {
            if (!appliedEffects.ContainsKey(effect.EffectType))
            {
                appliedEffects.Add(effect.EffectType, new List<Effect>());
            }

            List<Effect> effects = appliedEffects[effect.EffectType];

            if (!effects.Contains(effect))
            {
                effects.Add(effect);
            }
        }

        public void RemoveEffect(Effect effect)
        {
            if (!appliedEffects.ContainsKey(effect.EffectType)) return;

            List<Effect> effects = appliedEffects[effect.EffectType];

            if (effects.Contains(effect))
            {
                effects.Remove(effect);
            }
        }
    }
}
