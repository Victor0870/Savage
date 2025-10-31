using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using OctoberStudio.Bossfight;
using OctoberStudio.Extensions; // Cần cho DoAlpha
using UnityEngine;
using UnityEngine.Events;
using OctoberStudio; 
using OctoberStudio.Abilities;

/// <summary>
/// Adapter Hero4D riêng cho Boss, hỗ trợ các animation và hiệu ứng đặc biệt (Alpha).
/// </summary>
public class BossHeroCharacterAdapter : MonoBehaviour, OctoberStudio.IBossCharacterBehavior
{
    [Header("Hero Editor 4D")]
    public Character4D Character4D;
    private SpriteRenderer _rootRenderer; // SpriteRenderer của nhân vật chính Hero4D

    private AnimationManager _animationManager;
    private float _fixedScaleFactor;
    private Vector2 _currentMovementDirection = Vector2.down; 

    // Triển khai ICharacterBehavior (Phần dùng chung)
    public Transform Transform => transform;
    public Transform CenterTransform => Character4D != null && Character4D.Active != null ? Character4D.Active.AnchorBody : null;

    void Awake()
    {
        if (Character4D == null)
        {
            Character4D = GetComponent<Character4D>();
        }
        _animationManager = Character4D.AnimationManager;
        _rootRenderer = Character4D.GetComponent<SpriteRenderer>(); // Lấy SpriteRenderer gốc Hero4D

        // LƯU Ý: Hero4D có nhiều SpriteRenderer con. Việc thay đổi alpha ở đây
        // có thể chỉ áp dụng cho SpriteRenderer gốc. Cần kiểm tra nếu hiệu ứng
        // không hoạt động, bạn phải chạy vòng lặp qua các SpriteRenderer con.

        _fixedScaleFactor = transform.localScale.x;

        if (Character4D != null)
        {
            Character4D.Initialize();
            Character4D.SetDirection(_currentMovementDirection);
        }
    }

    public void SetCharacterData(CharacterData data) { }
    public void SetMovementDirection(Vector2 direction) { _currentMovementDirection = direction.normalized; }
    
    public void SetSpeed(float speed) 
    { 
        CharacterState state = speed > 0.01f ? CharacterState.Run : CharacterState.Idle;
        _animationManager.SetState(state);
        // ... (Logic SetDirection chi tiết tương tự Enemy Adapter)
    } 
    public void SetLocalScale(Vector3 scale) { /* ... Logic lật ... */ }
    public void SetSortingOrder(int order) { /* ... Logic sorting ... */ }
    public void PlayReviveAnimation() { _animationManager.SetState(CharacterState.Ready); }
    public void PlayDefeatAnimation() { _animationManager.Die(); }
    public void FlashHit(UnityAction onFinish = null) { _animationManager.Hit(); onFinish?.Invoke(); }
    public void PlayWeaponAttack(AbilityType abilityType) { /* N/A */ }

    // Triển khai IBossCharacterBehavior (Phần đặc trưng Boss)

    public void PlayChargeAnimation(bool startCharging)
    {
        if (startCharging)
        {
            // Sử dụng animation Magic/Ready để mô phỏng trạng thái "đẻ quái"
            _animationManager.SetState(CharacterState.Magic);
        }
        else
        {
            _animationManager.SetState(CharacterState.Idle); 
        }
    }

    public void PlayAttackAnimation(BossType bossType)
    {
        _animationManager.Cross(); 
    }

    public void PlayTeleportAnimation()
    {
        _animationManager.SetState(CharacterState.Jumping);
    }
    
    /// <summary>
    /// Thiết lập Alpha cho toàn bộ mô hình Hero4D (dùng cho miễn nhiễm).
    /// </summary>
    public void SetVisualsAlpha(float alpha)
    {
        // Chạy Easing cho tất cả SpriteRenderer con
        foreach (var renderer in Character4D.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.DoAlpha(alpha, 0.2f);
        }
    }
}
