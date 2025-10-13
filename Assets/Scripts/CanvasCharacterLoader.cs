using UnityEngine;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;

/// <summary>
/// Script này chịu trách nhiệm khởi tạo nhân vật Hero Editor 4D
/// và tải dữ liệu JSON từ PlayFab, sau đó đặt hoạt ảnh về Idle.
/// </summary>
public class CanvasCharacterLoader : MonoBehaviour
{
    // Kéo và thả prefab Character4D của bạn vào trường này trong Inspector.
    public Character4D characterPrefab;

    // Chuỗi JSON của nhân vật được lưu trữ từ PlayFab.
    [TextArea(3, 10)]
    public string characterJson = "";

    // Tùy chọn: Điều chỉnh kích thước nhân vật nếu cần.
    public float characterScale = 100f;

    private Character4D _instantiatedCharacter;

    // Ví dụ về dữ liệu JSON mẫu để bạn kiểm tra (hãy thay thế bằng JSON thật của bạn).
    // Ví dụ này chỉ là placeholder, bạn cần JSON hợp lệ từ việc lưu nhân vật Hero Editor 4D.
    private const string PlaceholderJson = "{\"Body\":\"Common.Basic.Body.HumanPants\",\"Ears\":null,\"Hair\":\"Common.Basic.Hair.BuzzCut\",\"Beard\":null,\"Helmet\":null,\"Armor\":\"Common.Basic.Armor.PlateArmor#FFFFFFFF\",\"Back\":null,\"Wings\":null,\"Shield\":null,\"WeaponType\":\"Melee1H\",\"Expression\":\"Default\",\"EquipmentTags\":\"\",\"Makeup\":null,\"Mask\":null,\"Earrings\":null,\"PrimaryWeapon\":\"Common.Basic.MeleeWeapon1H.Axe#FFFFFFFF\"}";

    void Start()
    {
        // Sử dụng PlaceholderJson nếu bạn quên gán JSON
        if (string.IsNullOrEmpty(characterJson))
        {
            Debug.LogWarning("Không có JSON được gán. Đang sử dụng JSON mẫu.");
            characterJson = PlaceholderJson;
        }

        // Tải và hiển thị nhân vật khi đối tượng được khởi tạo.
        LoadAndDisplayHero(characterJson);
    }

    /// <summary>
    /// Khởi tạo nhân vật, tải dữ liệu và đặt hoạt ảnh về Idle.
    /// </summary>
    /// <param name="json">Chuỗi JSON mô tả nhân vật.</param>
    public void LoadAndDisplayHero(string json)
    {
        if (characterPrefab == null)
        {
            Debug.LogError("Character Prefab chưa được gán. Hãy gán một prefab Character4D.");
            return;
        }

        // 1. Khởi tạo Prefab.
        // Khởi tạo Character4D làm con của GameObject này (được đặt trong Canvas).
        _instantiatedCharacter = Instantiate(characterPrefab, transform);

        // Cần điều chỉnh Local Scale để hiển thị đúng kích thước trong Canvas UI.
        _instantiatedCharacter.transform.localPosition = Vector3.zero;
        _instantiatedCharacter.transform.localScale = Vector3.one * characterScale;

        // 2. Tải dữ liệu nhân vật từ JSON (PlayFab).
        if (!string.IsNullOrEmpty(json))
        {
            // silent: false sẽ báo lỗi nếu có bất kỳ sprite nào không tìm thấy.
            _instantiatedCharacter.FromJson(json, silent: false); //
        }

        // 3. Đặt hoạt ảnh về Idle.

        // Quan trọng: Phải đặt hướng trước để Character4D biết nên hiển thị Part nào (Front/Back/Left/Right).
        // Vector2.down (0, -1) tương ứng với hướng Front (Mặt trước).
        _instantiatedCharacter.SetDirection(Vector2.down);

        // Đặt trạng thái hoạt ảnh là Idle (Đứng yên).
        _instantiatedCharacter.AnimationManager.SetState(CharacterState.Idle); //

        Debug.Log("Nhân vật đã được tải và đặt về trạng thái Idle.");
    }
}