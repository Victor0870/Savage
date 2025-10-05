// File: HeroCharacterAdapter.cs (FINAL FIX CHO SCALE & 4-SPRITE)

using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEngine;
using OctoberStudio;
using UnityEngine.Events;

public class HeroCharacterAdapter : MonoBehaviour, OctoberStudio.ICharacterBehavior
{
    [Header("Hero Editor 4D")]
    [Tooltip("Kéo component Character4D từ prefab vào đây")]
    public Character4D Character4D;

    private AnimationManager _animationManager;
    private CharacterData _characterData;
    private float _fixedScaleFactor; // LƯU TRỮ KÍCH THƯỚC GỐC (ví dụ: 0.4)

    // --- Triển khai ICharacterBehavior ---

    public Transform Transform => transform;
    public Transform CenterTransform => Character4D != null && Character4D.Active != null ? Character4D.Active.AnchorBody : null;

    void Awake()
    {
        if (Character4D == null)
        {
            Character4D = GetComponent<Character4D>();
        }
        _animationManager = Character4D.AnimationManager;

        // LƯU TRỮ KÍCH THƯỚC GỐC TỪ PREFAB (ví dụ: 0.4)
        _fixedScaleFactor = transform.localScale.x;

        // BẮT BUỘC: Buộc Hero 4D khởi tạo Sprite
        if (Character4D != null)
        {
            Character4D.Initialize();
            // FIX 4-SPRITE BUG: Đặt hướng ban đầu để chỉ hiển thị mô hình Front (xuống)
            Character4D.SetDirection(Vector2.down);
        }
    }

    public void SetCharacterData(CharacterData data)
    {
        _characterData = data;
    }

    // FIX LỖI KÍCH THƯỚC VÀ LẬT KÉP (DOUBLE-FLIPPING)
    public void SetLocalScale(Vector3 scale)
    {
        // scale.x là giá trị flip từ PlayerBehavior (1 hoặc -1)
        float flipFactor = scale.x;

        // Nếu đang dùng mô hình Left/Right, chúng ta hủy lệnh flip từ PlayerBehavior (scale.x = -1)
        // bằng cách buộc flipFactor về 1 (không lật)
        if (Character4D != null && Character4D.Active != null)
        {
            var activeCharacter = Character4D.Active;

            // Kiểm tra nếu đang sử dụng mô hình Left (hướng di chuyển chéo sẽ kích hoạt Left/Right)
            if (activeCharacter == Character4D.Left || activeCharacter == Character4D.Right)
            {
                // Buộc flipFactor = 1 để hủy lật kép, chỉ giữ kích thước gốc.
                flipFactor = 1;
            }
        }

        // Áp dụng scale factor đã lưu trữ
        transform.localScale = new Vector3(flipFactor * Mathf.Abs(_fixedScaleFactor), _fixedScaleFactor, _fixedScaleFactor);
    }

    // GỌI HOẠT ẢNH TẤN CÔNG CÓ ĐIỀU KIỆN
    public void PlayWeaponAttack(AbilityType abilityType)
    {
        if (_animationManager.IsAction) return;

        if (_characterData != null && _characterData.DesignatedAttackAbility != abilityType)
        {
            _animationManager.Jab();
            return;
        }

        switch (Character4D.WeaponType)
        {
            case WeaponType.Melee1H:
            case WeaponType.Melee2H:
                _animationManager.Slash(twoHanded: Character4D.WeaponType == WeaponType.Melee2H);
                break;
            case WeaponType.Bow:
                _animationManager.ShotBow();
                break;
            case WeaponType.Firearm1H:
            case WeaponType.Firearm2H:
                _animationManager.Fire();
                break;
            default:
                _animationManager.Jab();
                break;
        }
    }

    // LOGIC CHUYỂN ĐỘNG
    public void SetSpeed(float speed)
    {
        CharacterState state = speed > 0.01f ? CharacterState.Run : CharacterState.Idle;
        _animationManager.SetState(state);

        if (PlayerBehavior.Player != null && speed > 0.01f)
        {
            Vector2 rawDirection = PlayerBehavior.Player.LookDirection;
            Vector2 cardinalDirection;

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

    // Các phương thức ICharacterBehavior khác
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
}