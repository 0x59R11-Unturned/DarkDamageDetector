using Rocket.API;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace DarkDamageDetector
{
    public class DarkDamageDetectorConfiguration : IRocketPluginConfiguration
    {
        public short DamageMinKeyRange;
        public short DamageMaxKeyRange;

        [XmlArrayItem("Effect")] public List<UIEffect> Effects;

        public bool VehicleDamageViewValue;
        public bool StructureDamageViewValue;
        public bool BarricadeDamageViewValue;
        public bool ResourceDamageViewValue;


        #region ids
        // 6660 : 660 - damage

        // 6661 : 661 - player_kill
        // 6662 : 661 - player_kill_skull

        // 6663 : 662 - vehicle_kill

        // 6664 : 663 - structure_kill

        // 6665 : 664 - barricade_kill 

        // 6666 : 665 - resource_kill
        #endregion

        public void LoadDefaults()
        {
            DamageMinKeyRange = 700;
            DamageMaxKeyRange = 750;


            Effects = new List<UIEffect>()
            {
                new UIEffect(true, "player_damage", 6660, 660, "<color=#FAFAFA>-{0}</color>"),
                new UIEffect(true, "player_damage_skull", 6660, 660, "<color=#FF3232>-{0}</color>"),

                new UIEffect(true, "player_kill", 6661, 661),
                new UIEffect(true, "player_kill_skull", 6662, 661),

                new UIEffect(true, "vehicle_damage", 6660, 660, "<color=#FFCD00>-{0}</color>"),
                new UIEffect(true, "vehicle_kill", 6663, 662),

                new UIEffect(true, "structure_damage", 6660, 660, "<color=#17E79B>-{0}</color>"),
                new UIEffect(true, "structure_kill", 6664, 663),

                new UIEffect(true, "barricade_damage", 6660, 660, "<color=#17E79B>-{0}</color>"),
                new UIEffect(true, "barricade_kill", 6665, 664),

                new UIEffect(true, "zombie_damage", 6660, 660, "<color=#36763C>-{0}</color>"),

                new UIEffect(true, "resource_damage", 6660, 660, "<color=#FF8000>-{0}</color>"),
                new UIEffect(true, "resource_kill", 6666, 665)
            };

            VehicleDamageViewValue = false;
            StructureDamageViewValue = false;
            BarricadeDamageViewValue = false;
            ResourceDamageViewValue = false;
        }

        
        public bool TryGetEffectById(EDetectType detectType, string key, out UIEffect effect)
        {
            string id = detectType.ToString().ToLower() + (key == null ? string.Empty : "_" + key);
            effect = Effects.FirstOrDefault(e => e.Id == id && e.Enabled);
            if (effect == null)
            {
                effect = Effects.FirstOrDefault(e => e.Id == detectType.ToString().ToLower());
            }
            return effect != null;
        }
    }
}
