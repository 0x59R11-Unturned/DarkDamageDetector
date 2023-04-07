using System;
using System.Xml.Serialization;

namespace DarkDamageDetector
{
    [Serializable]
    public class UIEffect
    {
        [XmlAttribute("enabled")] public bool Enabled { get; set; }
        [XmlAttribute("id")] public string Id { get; set; }
        [XmlAttribute("effectId")] public ushort EffectId { get; set; }
        [XmlAttribute("effectKey")] public short EffectKey { get; set; }
        [XmlAttribute("text")] public string Text { get; set; }

        public bool HasText => !string.IsNullOrEmpty(Text);

        public UIEffect(bool enabled, string id, ushort effectId, short effectKey)
        {
            Enabled = enabled;
            Id = id;
            EffectId = effectId;
            EffectKey = effectKey;
        }
        public UIEffect(bool enabled, string id, ushort effectId, short effectKey, string text) : this(enabled, id, effectId, effectKey)
        {
            Text = text;
        }
        public UIEffect() { }


        public string Translate(params object[] parameters)
        {
            return (HasText && parameters != null) ? string.Format(Text, parameters) : null;
        }
    }
}
