// File: Scripts/Boss/BossHeroCharacterAdapter.cs (FIX: Ổn định Flipping khi dùng 4 hướng)

using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using OctoberStudio.Bossfight;
using OctoberStudio.Extensions;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using OctoberStudio;
using OctoberStudio.Abilities;
using OctoberStudio.Easing;

/// <summary>
/// Adapter Hero4D dành cho Boss/Minion Boss, hỗ trợ logic 4-hướng.
/// Logic SetLocalScale được chỉnh sửa để chỉ áp dụng Flip khi hiển thị
/// sprite Front/Back, tránh xung đột với sprite Left/Right của Hero4D.
/// </summary>
public class BossHeroCharacterAdapter : MonoBehaviour, OctoberStudio.IBossCharacterBehavior
{
    [Header("Hero Editor 4D")]
    public Character4D Character4D;

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

        // Lưu trữ kích thước gốc
        _fixedScaleFactor = transform.localScale.x;

        if (Character4D != null)
        {
            Character4D.Initialize();
            // Bắt đầu ở hướng Down/Front
            Character4D.SetDirection(Vector2.down);
        }
    }

    public void SetCharacterData(CharacterData data) { }

    public void SetMovementDirection(Vector2 direction) { _currentMovementDirection = direction.normalized; }

    public void SetSpeed(float speed)
    {
        CharacterState state = speed > 0.01f ? CharacterState.Run : CharacterState.Idle;
        _animationManager.SetState(state);

        if (speed > 0.01f)
        {
            Vector2 cardinalDirection;
            Vector2 rawDirection = _currentMovementDirection;

            // Chuyển đổi hướng di chuyển thô sang 4 hướng chính (Cardinal)
            if (Mathf.Abs(rawDirection.x) >= Mathf.Abs(rawDirection.y))
            {
                cardinalDirection = rawDirection.x > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                cardinalDirection = rawDirection.y > 0 ? Vector2.up : Vector2.down;
            }

            Character4D.SetDirection(cardinalDirection);
        }
    }

    public void SetLocalScale(Vector3 scale)
    {

            float flipFactor = 1f;
            transform.localScale = new Vector3(flipFactor * Mathf.Abs(_fixedScaleFactor), _fixedScaleFactor, _fixedScaleFactor);
    }

    public void SetSortingOrder(int order)
    {
        var layerManager = Character4D.GetComponent<LayerManager>();
        if (layerManager != null)
        {
            layerManager.SetSortingGroupOrder(order);
        }
        else
        {
            foreach (var renderer in Character4D.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingOrder = order;
            }
        }
    }
    public void PlayReviveAnimation() { _animationManager.SetState(CharacterState.Ready); }
    public void PlayDefeatAnimation() { _animationManager.Die(); }
    public void FlashHit(UnityAction onFinish = null) { _animationManager.Hit(); onFinish?.Invoke(); }
    public void PlayWeaponAttack(AbilityType abilityType) { /* N/A */ }

    // Triển khai IBossCharacterBehavior (Phần đặc trưng Boss)

    public void PlayChargeAnimation(bool startCharging)
    {
        if (startCharging)
        {
            _animationManager.SetState(CharacterState.Ready);
        }
        else
        {
            _animationManager.SetState(CharacterState.Idle);
        }
    }

    public void PlayAttackAnimation(BossType bossType)
    {
        _animationManager.Jab();
    }

    public void PlayTeleportAnimation()
    {
        _animationManager.SetState(CharacterState.Ready);
    }

    public void SetVisualsAlpha(float alpha)
    {
        foreach (var renderer in Character4D.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.DoAlpha(alpha, 0.2f);
        }
    }
}