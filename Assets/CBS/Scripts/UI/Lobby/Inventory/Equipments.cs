using CBS.Core;
using CBS.Models;
using CBS.Scriptable;
using System.Collections.Generic;
using UnityEngine;
using Assets.HeroEditor4D.Common.Scripts.Enums;

namespace CBS.UI
{
    public class Equipments : MonoBehaviour
    {
        // 1. Khai báo 4 slot cố định cho Giáp (không có Giáp toàn thân)
        [Header("Fixed Equipment Slots")]
        [SerializeField]
        private InventorySlot HelmetSlot;
        [SerializeField]
        private InventorySlot VestSlot;
        [SerializeField]
        private InventorySlot BracersSlot;
        [SerializeField]
        private InventorySlot LeggingsSlot;

        // Slot cho Vũ khí và Phụ kiện (Giữ nguyên)
        [SerializeField]
        private InventorySlot Weapon1HSlot;
        [SerializeField]
        private InventorySlot ShieldSlot;
        [SerializeField]
        private InventorySlot Weapon2HSlot;
        [SerializeField]
        private InventorySlot BackSlot;
        // Thêm các slot khác nếu cần (Mask, Wings, v.v.)

        private List<CBSInventoryItem> CurrentItems { get; set; }
        private ICBSInventory CBSInventory { get; set; }
        private InventoryPrefabs Prefabs { get; set; }

        // Dictionary để ánh xạ EquipmentPart tới InventorySlot UI cố định
        private Dictionary<EquipmentPart, InventorySlot> PartToSlotMap;

        private void Awake()
        {
            CBSInventory = CBSModule.Get<CBSInventoryModule>();
            Prefabs = CBSScriptable.Get<InventoryPrefabs>();

            // Khởi tạo Map, loại bỏ EquipmentPart.Armor
            PartToSlotMap = new Dictionary<EquipmentPart, InventorySlot>
            {
                // Giáp (Chỉ giữ lại các phần nhỏ)
                { EquipmentPart.Helmet, HelmetSlot },
                { EquipmentPart.Vest, VestSlot },
                { EquipmentPart.Bracers, BracersSlot },
                { EquipmentPart.Leggings, LeggingsSlot },

                // Vũ khí 1H và Shield
                { EquipmentPart.MeleeWeapon1H, Weapon1HSlot },
                { EquipmentPart.Firearm1H, Weapon1HSlot },
                { EquipmentPart.SecondaryFirearm1H, Weapon1HSlot },
                { EquipmentPart.Shield, ShieldSlot },

                // Vũ khí 2H
                { EquipmentPart.MeleeWeapon2H, Weapon2HSlot },
                { EquipmentPart.Bow, Weapon2HSlot },
                { EquipmentPart.Crossbow, Weapon2HSlot },
                { EquipmentPart.Firearm2H, Weapon2HSlot },

                // Phụ kiện
                { EquipmentPart.Back, BackSlot },
                // ...
            };

            ClearAllSlots();
        }

        private void OnEnable()
        {
            CBSInventory.OnItemEquiped += OnItemEquipmentChange;
            CBSInventory.OnItemUnEquiped += OnItemEquipmentChange;
            DisplayEquipments();
        }

        private void OnDisable()
        {
            CBSInventory.OnItemEquiped -= OnItemEquipmentChange;
            CBSInventory.OnItemUnEquiped -= OnItemEquipmentChange;
        }

        private void DisplayEquipments()
        {
            CBSInventory.GetInventory(OnGetInvertory);
        }

        private void OnGetInvertory(CBSGetInventoryResult result)
        {
            if (result.IsSuccess)
            {
                CurrentItems = result.EquippedItems;
                DrawItems();
            }
        }

        private void ClearAllSlots()
        {
            foreach(var pair in PartToSlotMap)
            {
                if (pair.Value != null)
                {
                    pair.Value.gameObject.SetActive(false);
                }
            }
        }

        private void DrawItems()
        {
            ClearAllSlots();

            if (CurrentItems == null || CurrentItems.Count == 0)
                return;

            foreach (var item in CurrentItems)
            {
                EquipmentPart part = GetEquipmentPartFromItemID(item.ItemID);

                if (PartToSlotMap.TryGetValue(part, out InventorySlot targetSlot) && targetSlot != null)
                {
                    targetSlot.Init(item, OnItemClicked);
                    targetSlot.gameObject.SetActive(true);
                }
                // Nếu item là EquipmentPart.Armor (Giáp toàn thân) thì nó sẽ bị bỏ qua ở đây.
            }
        }

        private void OnItemEquipmentChange(CBSInventoryItem item)
        {
            DisplayEquipments();
        }

        private void OnItemClicked(CBSInventoryItem item)
        {
            var uiPrefab = Prefabs.ItemInfo;
            var uiObject = UIView.ShowWindow(uiPrefab);
            var itemInfo = uiObject.GetComponent<InventoryItemInfo>();
            itemInfo.Draw(item);
        }

        /// <summary>
        /// Ánh xạ CBS Item ID sang Hero4D EquipmentPart
        /// </summary>
        private EquipmentPart GetEquipmentPartFromItemID(string itemId)
        {
            // Các phần giáp nhỏ vẫn được xử lý bình thường
            if (itemId.Contains(".Helmet.")) return EquipmentPart.Helmet;
            if (itemId.Contains(".Vest.")) return EquipmentPart.Vest;
            if (itemId.Contains(".Leggings.")) return EquipmentPart.Leggings;
            if (itemId.Contains(".Bracers.")) return EquipmentPart.Bracers;

            // Giữ lại Armor trong logic ánh xạ, nhưng nó sẽ bị bỏ qua khi hiển thị
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
}