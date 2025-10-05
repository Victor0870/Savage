// File: HeroCharacterAdapter.cs

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
    private CharacterData _characterData; // Biến lưu trữ data

    // --- Triển khai ICharacterBehavior ---

    // Chú ý: Getter an toàn (tránh NRE)
    public Transform Transform => transform;
    public Transform CenterTransform => Character4D != null && Character4D.Active != null ? Character4D.Active.AnchorBody : null;

    void Awake()
    {
        if (Character4D == null)
        {
            Character4D = GetComponent<Character4D>();
        }
        _animationManager = Character4D.AnimationManager;
    }

    // Được gọi bởi PlayerBehavior.Awake()
    public void SetCharacterData(CharacterData data)
    {
        _characterData = data;
    }

    // GỌI HOẠT ẢNH TẤN CÔNG CÓ ĐIỀU KIỆN
    public void PlayWeaponAttack(AbilityType abilityType)
    {
        // 1. Kiểm tra nếu đang bận (bị đánh, chết)
        if (_animationManager.IsAction) return;

        // 2. Kiểm tra nếu đây KHÔNG phải là Ability Đặc trưng (Designated Attack)
        if (_characterData != null && _characterData.DesignatedAttackAbility != abilityType)
        {
            // Nếu là Skill Phụ, chơi animation nhẹ/giả lập và thoát
            _animationManager.Jab(); // Animation nhẹ/mặc định
            return;
        }

        // --- Nếu là SKILL ĐẶC TRƯNG, chơi animation mạnh/chính thức ---

        switch (Character4D.WeaponType)
        {
            // Cận chiến (Ưu tiên Slash)
            case WeaponType.Melee1H:
            case WeaponType.Melee2H:
                _animationManager.Slash(twoHanded: Character4D.WeaponType == WeaponType.Melee2H);
                break;

            // Bắn/Phép thuật (Ưu tiên Fire/Shot)
            case WeaponType.Bow:
                _animationManager.ShotBow();
                break;
            case WeaponType.Firearm1H:
            case WeaponType.Firearm2H:
                _animationManager.Fire();
                break;

            // Mặc định cho mọi thứ khác (Wand, Boomerang, Dagger)
            default:
                _animationManager.Jab();
                break;
        }
    }

    // LOGIC CHUYỂN ĐỘNG (Đã sửa lỗi SetDirection)
    public void SetSpeed(float speed)
    {
        CharacterState state = speed > 0.01f ? CharacterState.Run : CharacterState.Idle;
        _animationManager.SetState(state);

        if (PlayerBehavior.Player != null && speed > 0.01f)
        {
            Vector2 rawDirection = PlayerBehavior.Player.LookDirection;
            Vector2 cardinalDirection;

            // Làm tròn về hướng chính (trục mạnh hơn)
            if (Mathf.Abs(rawDirection.x) >= Mathf.Abs(rawDirection.y))
            {
                // Ngang (bao gồm cả di chuyển chéo 45 độ)
                                if (rawDirection.x > 0)
                                {
                                    cardinalDirection = Vector2.right;
                                }
                                else // rawDirection.x <= 0 (Di chuyển trái thuần túy hoặc trái chéo)
                                {
                                    cardinalDirection = Vector2.left; // <--- Đảm bảo gọi cho hướng trái
                                }
            }
            else
            {
                cardinalDirection = rawDirection.y > 0 ? Vector2.up : Vector2.down;
            }

            Character4D.SetDirection(cardinalDirection);
        }
    }

    // Các phương thức triển khai ICharacterBehavior khác
    public void SetLocalScale(Vector3 scale)
        {
            // Lấy scale gốc hiện tại (ví dụ: 0.4)
            Vector3 currentScale = transform.localScale;

            // Mô hình Left/Right đã tự lo việc lật, nên chúng ta giữ nguyên scale X
            // để vô hiệu hóa lệnh flip từ PlayerBehavior (scale.x = -1).
            if (Character4D != null && Character4D.Active != null)
            {
                var activeCharacter = Character4D.Active;

                // Kiểm tra nếu đang sử dụng mô hình Left hoặc Right
                if (activeCharacter == Character4D.Left || activeCharacter == Character4D.Right)
                {
                    // Buộc scale X phải dương (trả về giá trị tuyệt đối của scale X hiện tại)
                    transform.localScale = new Vector3(Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
                    return;
                }
            }

            // Nếu là mô hình Front/Back (nhìn thẳng/lưng), áp dụng lệnh flip từ PlayerBehavior
            // (để nó quay mặt/lưng và nhân vật vẫn lật khi di chuyển chéo)
            transform.localScale = scale;
        }
    public void SetSortingOrder(int order) { /* ... (Logic set LayerManager) ... */ }

    public void PlayReviveAnimation() { _animationManager.SetState(CharacterState.Ready); }
    public void PlayDefeatAnimation() { _animationManager.Die(); }
    public void FlashHit(UnityAction onFinish = null) { _animationManager.Hit(); onFinish?.Invoke(); }
}