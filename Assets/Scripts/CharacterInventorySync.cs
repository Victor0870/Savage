using CBS;
using UnityEngine;
using CBS.Models;
using CBS.UI;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Data;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Lớp này đồng bộ trạng thái trang bị của nhân vật HeroEditor4D với Inventory của CBS,
/// sử dụng cache để xử lý xung đột slot trang bị.
/// </summary>
public class CharacterInventorySync : MonoBehaviour
{
    [Tooltip("Kéo thả component Character4D của bạn vào đây.")]
    public Character4D Character4D;

    // CBS Modules
    private ICBSInventory CBSInventory;

    // CACHE: Lưu trữ Item ID đang được trang bị theo slot Hero4D
    private Dictionary<EquipmentPart, string> _equippedCache = new Dictionary<EquipmentPart, string>();

    void Start()
    {
        if (Character4D == null)
        {
            Debug.LogError("Character4D chưa được gán. Vui lòng kéo thả component vào Inspector.");
            return;
        }

        CBSInventory = CBSModule.Get<CBSInventoryModule>();

        // 1. Tải trạng thái trang bị ban đầu
        LoadEquippedItemsFromCBS();

        // 2. Đăng ký lắng nghe sự kiện thay đổi của CBS
        CBSInventory.OnItemEquiped += OnCBSItemEquipped;
        CBSInventory.OnItemUnEquiped += OnCBSItemUnEquipped;

        Debug.Log(">>> [INIT] CharacterInventorySync đã khởi tạo và đang lắng nghe sự kiện CBS.");
    }

    void OnDestroy()
    {
        // Hủy đăng ký sự kiện để tránh rò rỉ bộ nhớ
        if (CBSInventory != null)
        {
            CBSInventory.OnItemEquiped -= OnCBSItemEquipped;
            CBSInventory.OnItemUnEquiped -= OnCBSItemUnEquipped;
        }
    }

    private void LoadEquippedItemsFromCBS()
    {
        CBSInventory.GetInventory(result =>
        {
            if (result.IsSuccess)
            {
                Debug.Log($"[LOAD] Bắt đầu tải trang bị. Reset nhân vật. Số item: {result.EquippedItems.Count}");
                Character4D.ResetEquipment();
                _equippedCache.Clear(); // Xóa cache trước khi tải mới

                foreach (var item in result.EquippedItems)
                {
                    // Lấy Part và Sprite
                    var mappingResult = MapItemToPartAndSprite(item);

                    if (mappingResult.ItemSprite != null)
                    {
                        // Trang bị Item lên Hero4D
                        Character4D.Equip(mappingResult.ItemSprite, mappingResult.Part, null);

                        // CẬP NHẬT CACHE
                        _equippedCache[mappingResult.Part] = item.ItemID;
                    }
                    else
                    {
                        Debug.LogWarning($"[LOAD FAIL] Không tìm thấy Hero4D Sprite ID cho CBS ItemID: {item.ItemID}");
                    }
                }

                Debug.Log("[LOAD] KẾT THÚC trang bị. Gọi Character4D.Initialize() để vẽ nhân vật.");
                Character4D.Initialize();
                Debug.Log($"[LOAD SUCCESS] Đã tải thành công {result.EquippedItems.Count} vật phẩm đã trang bị. Cache size: {_equippedCache.Count}");
            }
            else
            {
                new PopupViewer().ShowFabError(result.Error);
                Debug.LogError($"[LOAD ERROR] Lỗi khi tải trang bị từ CBS: {result.Error.Message}");
            }
        });
    }

    private void OnCBSItemEquipped(CBSInventoryItem item)
    {
        EquipItemOnHero(item);
        Debug.Log($"[CBS EVENT] Hoàn tất trang bị {item.ItemID}. Gọi Character4D.Initialize() để vẽ nhân vật.");
        Character4D.Initialize();
    }

    private void OnCBSItemUnEquipped(CBSInventoryItem item)
    {
        UnEquipItemOnHero(item);
        Debug.Log($"[CBS EVENT] Hoàn tất gỡ trang bị {item.ItemID}. Gọi Character4D.Initialize() để vẽ nhân vật.");
        Character4D.Initialize();
    }

    // Hàm trợ giúp để gỡ bỏ Item và cập nhật cache
    private void AttemptUnequipFromCache(EquipmentPart part)
    {
        if (_equippedCache.ContainsKey(part))
        {
            string equippedItemId = _equippedCache[part];
            Debug.Log($"[WEAPON CONFLICT] Gỡ bỏ Item {equippedItemId} khỏi slot {part} do xung đột vũ khí.");
            Character4D.UnEquip(part);
            _equippedCache.Remove(part);
        }
    }

    private void EquipItemOnHero(CBSInventoryItem cbsItem)
    {
        Debug.Log($"[EQUIP START] Đang xử lý Item ID: {cbsItem.ItemID}");
        var mappingResult = MapItemToPartAndSprite(cbsItem);
        EquipmentPart targetPart = mappingResult.Part;

        Debug.Log($"[EQUIP MAPPING] Item ID: {cbsItem.ItemID} được ánh xạ tới Part: {targetPart}, ItemSprite found: {mappingResult.ItemSprite != null}");

        if (mappingResult.ItemSprite != null)
        {
            // --- B1: LOGIC KIỂM TRA VÀ GỠ BỎ XUNG ĐỘT VŨ KHÍ (Cross-slot conflicts) ---
            if (targetPart == EquipmentPart.MeleeWeapon2H || targetPart == EquipmentPart.Bow || targetPart == EquipmentPart.Crossbow || targetPart == EquipmentPart.Firearm2H)
            {
                // Nếu trang bị 2H (hoặc Bow/Crossbow/Firearm2H), phải gỡ bỏ 1H và Shield
                AttemptUnequipFromCache(EquipmentPart.MeleeWeapon1H);
                AttemptUnequipFromCache(EquipmentPart.Shield);
                AttemptUnequipFromCache(EquipmentPart.Firearm1H); // Gỡ bỏ vũ khí 1 tay khác
            }
            else if (targetPart == EquipmentPart.MeleeWeapon1H || targetPart == EquipmentPart.Shield || targetPart == EquipmentPart.Firearm1H)
            {
                // Nếu trang bị 1H hoặc Shield, phải gỡ bỏ 2H
                AttemptUnequipFromCache(EquipmentPart.MeleeWeapon2H);
                AttemptUnequipFromCache(EquipmentPart.Bow);
                AttemptUnequipFromCache(EquipmentPart.Crossbow);
                AttemptUnequipFromCache(EquipmentPart.Firearm2H);
            }
            // Logic này sẽ không ảnh hưởng đến các slot giáp (Armor, Helmet, Vest...)

            // --- B2: LOGIC GỠ BỎ XUNG ĐỘT CÙNG SLOT (One-to-one conflict: Helmet vs Helmet, Vest vs Vest) ---
            if (_equippedCache.TryGetValue(targetPart, out string equippedItemId))
            {
                if (equippedItemId != cbsItem.ItemID)
                {
                    Debug.Log($"[CACHE CONFLICT] Slot {targetPart} đang bị Item {equippedItemId} chiếm giữ. Tự động gỡ bỏ item cũ.");
                    Character4D.UnEquip(targetPart);
                    _equippedCache.Remove(targetPart);
                    Debug.Log($"[CACHE UPDATE] Đã gỡ bỏ item cũ khỏi cache và Hero4D.");
                }
                else
                {
                    Debug.Log($"[CACHE CHECK] Item {cbsItem.ItemID} đã trang bị. Bỏ qua lệnh EQUIP trùng lặp.");
                    return;
                }
            }

            // --- B3: THỰC HIỆN TRANG BỊ MỚI ---
            Color? customColor = null;

            Debug.Log($"[EQUIP CALL] Gọi Character4D.Equip(Part: {targetPart})");
            Character4D.Equip(mappingResult.ItemSprite, targetPart, customColor);

            // CẬP NHẬT CACHE
            _equippedCache[targetPart] = cbsItem.ItemID;

            Debug.Log($"[EQUIP SUCCESS] Trang bị thành công: {cbsItem.ItemID} -> {targetPart}");
        }
        else
        {
            Debug.LogWarning($"[EQUIP FAIL] Không tìm thấy Hero4D Sprite ID cho CBS ItemID: {cbsItem.ItemID}");
        }
    }

    private void UnEquipItemOnHero(CBSInventoryItem cbsItem)
    {
        Debug.Log($"[UNEQUIP START] Đang xử lý gỡ Item ID: {cbsItem.ItemID}");
        var part = GetEquipmentPartFromItemID(cbsItem.ItemID);
        Debug.Log($"[UNEQUIP MAPPING] Item ID: {cbsItem.ItemID} được ánh xạ tới Part: {part}");

        // Kiểm tra cache trước khi gỡ
        if (part != EquipmentPart.Cape)
        {
            if (_equippedCache.ContainsKey(part) && _equippedCache[part] == cbsItem.ItemID)
            {
                 Debug.Log($"[UNEQUIP CALL] Gọi Character4D.UnEquip(Part: {part})");
                 Character4D.UnEquip(part);
                 _equippedCache.Remove(part); // Xóa khỏi cache
                 Debug.Log($"[UNEQUIP SUCCESS] Gỡ trang bị thành công: {cbsItem.ItemID} từ slot {part}. Đã cập nhật cache.");
            }
            else
            {
                Debug.LogWarning($"[CACHE MISMATCH] Item {cbsItem.ItemID} không có trong cache hoặc không khớp slot {part}. Bỏ qua lệnh gỡ bỏ Hero4D.");
            }
        }
        else
        {
             Debug.LogWarning($"[UNEQUIP FAIL] Gỡ trang bị bỏ qua cho item {cbsItem.ItemID} do không thể xác định rõ ràng EquipmentPart.");
        }
    }

    /// <summary>
    /// Ánh xạ CBS Item ID sang Hero4D ItemSprite và EquipmentPart.
    /// </summary>
    private (ItemSprite ItemSprite, EquipmentPart Part) MapItemToPartAndSprite(CBSInventoryItem cbsItem)
    {
        var spriteId = cbsItem.ItemID;
        var sc = Character4D.SpriteCollection;

        EquipmentPart determinedPart = EquipmentPart.Cape;

        // 1. XÁC ĐỊNH EQUIPMENT PART DỰA TRÊN CHUỖI ITEM ID
        // Giáp và các bộ phận giáp nhỏ
        if (spriteId.Contains(".Helmet.")) determinedPart = EquipmentPart.Helmet;
        else if (spriteId.Contains(".Vest.")) determinedPart = EquipmentPart.Vest;
        else if (spriteId.Contains(".Leggings.")) determinedPart = EquipmentPart.Leggings;
        else if (spriteId.Contains(".Bracers.")) determinedPart = EquipmentPart.Bracers;
        else if (spriteId.Contains(".Armor.")) determinedPart = EquipmentPart.Armor;

        // Vũ khí 1H
        else if (spriteId.Contains(".MeleeWeapon1H.")) determinedPart = EquipmentPart.MeleeWeapon1H;
        else if (spriteId.Contains(".Firearm1H.")) determinedPart = EquipmentPart.Firearm1H;
        else if (spriteId.Contains(".Shield.")) determinedPart = EquipmentPart.Shield;

        // Vũ khí 2H
        else if (spriteId.Contains(".MeleeWeapon2H.")) determinedPart = EquipmentPart.MeleeWeapon2H;
        else if (spriteId.Contains(".Bow.")) determinedPart = EquipmentPart.Bow;
        else if (spriteId.Contains(".Crossbow.")) determinedPart = EquipmentPart.Crossbow;
        else if (spriteId.Contains(".Firearm2H.")) determinedPart = EquipmentPart.Firearm2H;

        // Phụ kiện
        else if (spriteId.Contains(".Back.")) determinedPart = EquipmentPart.Back;
        else if (spriteId.Contains(".Wings.")) determinedPart = EquipmentPart.Wings;
        else if (spriteId.Contains(".Mask.")) determinedPart = EquipmentPart.Mask;


        ItemSprite itemSprite = null;

        // 2. TÌM ITEM SPRITE
        if (determinedPart != EquipmentPart.Cape)
        {
            string searchId = spriteId;

            // Nếu là Giáp nhỏ (Helmet, Vest, Leggings, Bracers), CHUYỂN ID SANG DẠNG ARMOR để tìm Sprite.
            if (determinedPart == EquipmentPart.Helmet || determinedPart == EquipmentPart.Vest ||
                determinedPart == EquipmentPart.Leggings || determinedPart == EquipmentPart.Bracers || determinedPart == EquipmentPart.Armor)
            {
                // Chỉ xử lý Giáp nếu nó không phải là Giáp toàn thân gốc (có .Armor. trong ID)
                if (determinedPart != EquipmentPart.Armor && !spriteId.Contains(".Armor."))
                {
                    string partName = determinedPart.ToString();
                    int startIndex = spriteId.LastIndexOf(partName) + partName.Length;

                    // Giả định tên Giáp cơ sở là phần còn lại của chuỗi sau Part (ví dụ: .BanditArmor2)
                    string baseName = spriteId.Substring(startIndex).TrimStart('.');

                    // Tái tạo lại ID Giáp toàn thân (FantasyHeroes.Basic.Armor.BanditArmor2)
                    searchId = $"FantasyHeroes.Basic.Armor.{baseName}";

                    Debug.Log($"[MAPPING WORKAROUND] Item {spriteId} (Part: {determinedPart}) không có Sprite riêng. Đang tìm ItemSprite bằng ID Giáp toàn thân: {searchId}");
                }
            }

            // Tìm ItemSprite trong list tương ứng
            if (determinedPart == EquipmentPart.Shield)
            {
                 itemSprite = sc.Shield.FirstOrDefault(i => i.Id == spriteId);
            }
            else if (determinedPart == EquipmentPart.MeleeWeapon1H)
            {
                itemSprite = sc.MeleeWeapon1H.FirstOrDefault(i => i.Id == spriteId);
            }
            else if (determinedPart == EquipmentPart.MeleeWeapon2H)
            {
                itemSprite = sc.MeleeWeapon2H.FirstOrDefault(i => i.Id == spriteId);
            }
            else if (determinedPart == EquipmentPart.Bow)
            {
                itemSprite = sc.Bow.FirstOrDefault(i => i.Id == spriteId);
            }
            else if (determinedPart == EquipmentPart.Crossbow)
            {
                itemSprite = sc.Crossbow.FirstOrDefault(i => i.Id == spriteId);
            }
            else if (determinedPart == EquipmentPart.Firearm1H)
            {
                itemSprite = sc.Firearm1H.FirstOrDefault(i => i.Id == spriteId);
            }
            else if (determinedPart == EquipmentPart.Firearm2H)
            {
                itemSprite = sc.Firearm2H.FirstOrDefault(i => i.Id == spriteId);
            }
            // Các part giáp (Armor, Helmet, Vest, Leggings, Bracers) sử dụng searchId (có thể là ID giáp toàn thân)
            else if (determinedPart == EquipmentPart.Armor || determinedPart == EquipmentPart.Helmet ||
                     determinedPart == EquipmentPart.Vest || determinedPart == EquipmentPart.Leggings || determinedPart == EquipmentPart.Bracers)
            {
                itemSprite = sc.Armor.FirstOrDefault(i => i.Id == searchId);
            }
            // ... các trường hợp khác

            if (itemSprite != null)
            {
                return (itemSprite, determinedPart);
            }
        }

        // --- FALLBACK cho trường hợp ItemSprite không tìm thấy bằng logic chính (chủ yếu là phụ kiện) ---

        // Nếu Item là phụ kiện và không được xử lý ở trên (ví dụ: Mask, Back, Wings, Earrings)
        if (determinedPart == EquipmentPart.Back)
        {
            itemSprite = sc.Back.FirstOrDefault(i => i.Id == spriteId);
            if (itemSprite != null) return (itemSprite, determinedPart);
        }
        if (determinedPart == EquipmentPart.Wings)
        {
            itemSprite = sc.Wings.FirstOrDefault(i => i.Id == spriteId);
            if (itemSprite != null) return (itemSprite, determinedPart);
        }
        if (determinedPart == EquipmentPart.Mask)
        {
            itemSprite = sc.Mask.FirstOrDefault(i => i.Id == spriteId);
            if (itemSprite != null) return (itemSprite, determinedPart);
        }
        if (determinedPart == EquipmentPart.Earrings)
        {
            itemSprite = sc.Earrings.FirstOrDefault(i => i.Id == spriteId);
            if (itemSprite != null) return (itemSprite, determinedPart);
        }

        // Không tìm thấy
        Debug.LogWarning($"[MAPPING FAIL] Không tìm thấy sprite phù hợp cho item {spriteId} trong bất kỳ list nào.");
        return (null, EquipmentPart.Cape);
    }

    private EquipmentPart GetEquipmentPartFromItemID(string itemId)
    {
         // Logic này vẫn chính xác để xác định Part từ Item ID
        if (itemId.Contains(".Helmet.")) return EquipmentPart.Helmet;
        if (itemId.Contains(".Vest.")) return EquipmentPart.Vest;
        if (itemId.Contains(".Leggings.")) return EquipmentPart.Leggings;
        if (itemId.Contains(".Bracers.")) return EquipmentPart.Bracers;
        if (itemId.Contains(".Armor.")) return EquipmentPart.Armor;

        if (itemId.Contains(".Shield.")) return EquipmentPart.Shield;
        if (itemId.Contains(".MeleeWeapon1H.")) return EquipmentPart.MeleeWeapon1H;
        if (itemId.Contains(".MeleeWeapon2H.")) return EquipmentPart.MeleeWeapon2H;
        if (itemId.Contains(".Bow.")) return EquipmentPart.Bow;
        if (itemId.Contains(".Crossbow.")) return EquipmentPart.Crossbow;
        if (itemId.Contains(".Firearm1H.")) return EquipmentPart.Firearm1H;
        if (itemId.Contains(".Firearm2H.")) return EquipmentPart.Firearm2H;
        if (itemId.Contains(".Back.")) return EquipmentPart.Back;
        if (itemId.Contains(".Wings.")) return EquipmentPart.Wings;
        if (itemId.Contains(".Mask.")) return EquipmentPart.Mask;
        if (itemId.Contains(".Earrings.")) return EquipmentPart.Earrings;

        return EquipmentPart.Cape;
    }
}