using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio
{
    [System.Serializable]
    public class CharacterData
    {
        [SerializeField] string name;
        public string Name => name;

        [SerializeField] int cost;
        public int Cost => cost;

        [SerializeField] Sprite icon;
        public Sprite Icon => icon;

        [SerializeField] GameObject prefab;
        public GameObject Prefab => prefab;

        [Space]
        [SerializeField] bool hasStartingAbility = false;
        public bool HasStartingAbility => hasStartingAbility;

        //[SerializeField] AbilityType startingAbility;
        //public AbilityType StartingAbility => startingAbility;

        [SerializeField] AbilityType[] startingAbilities = new AbilityType[2]; // 2skill
        public AbilityType[] StartingAbilities => startingAbilities;
        public bool HasStartingAbilities => startingAbilities != null && startingAbilities.Length > 0;

         [Space]
          // TRƯỜNG BỔ SUNG: Khả năng Tấn công Đặc trưng của Hero
         [SerializeField] AbilityType designatedAttackAbility;
         public AbilityType DesignatedAttackAbility => designatedAttackAbility; // <--- DÒNG BỔ SUNG


        [Space]
        [SerializeField, Min(1)] float baseHP;
        public float BaseHP => baseHP;

        [SerializeField, Min(1f)] float baseDamage;
        public float BaseDamage => baseDamage;
    }
}