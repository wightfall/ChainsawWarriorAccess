using System.Collections.Generic;
using UnityEngine;

namespace CWAccess.Game
{
    /// <summary>Spoken descriptions of house cards (rooms, enemies, traps) and equipment cards.</summary>
    internal static class CardText
    {
        // ---------------- House cards ----------------

        /// <summary>Back of a house card: just the room.</summary>
        public static string Room(StatsCardBase card)
        {
            if (card == null) return string.Empty;
            return Loc.Get("card_room", RoomName(card.CardBack));
        }

        public static string RoomName(string cardBack)
        {
            if (string.IsNullOrEmpty(cardBack)) return Loc.Get("card_room_unknown");
            return Loc.TryGet("room_" + cardBack) ?? TextUtil.FromObjectName(cardBack);
        }

        /// <summary>Short line: name and kind.</summary>
        public static string Short(StatsCardBase card)
        {
            if (card == null) return string.Empty;
            string name = G.Text(card.Name);
            if (name.Length == 0) name = TextUtil.FromObjectName(card.Identifier);
            switch (card.Class)
            {
                case "Enemy":
                    return Loc.Get("card_enemy_short", NameAndType(name, card.Type), card.Rating);
                case "Trap":
                    return Loc.Get("card_trap_short", name);
                default:
                    return name;
            }
        }

        /// <summary>Everything printed on the front of the card.</summary>
        public static string Full(StatsCardBase card, bool escapable)
        {
            if (card == null) return string.Empty;
            var parts = new List<string>();
            string name = G.Text(card.Name);
            switch (card.Class)
            {
                case "Enemy":
                    parts.Add(NameAndType(name, card.Type));
                    if (card.Rating != 0) parts.Add(Loc.Get("card_rating", card.Rating));
                    parts.Add(Loc.Get(escapable ? "card_can_escape" : "card_no_escape"));
                    break;
                case "Trap":
                    parts.Add(Loc.Get("card_trap", name));
                    break;
                default:
                    parts.Add(name.Length > 0 ? name : Loc.Get("card_class_" + card.Class));
                    break;
            }
            parts.Add(G.Text(card.CardText));
            return TextUtil.Join(parts);
        }

        /// <summary>"Chaos Agent, enemy" rather than "Chaos Agent, chaos agent"; "Zombie Dog, zombie" keeps the kind.</summary>
        private static string NameAndType(string name, string type)
        {
            string t = TypeName(type);
            bool redundant = t.Length == 0 || name.ToLowerInvariant().Contains(t.ToLowerInvariant());
            return redundant ? Loc.Get("card_enemy_plain", name) : Loc.Get("card_enemy", name, t);
        }

        public static string TypeName(string type)
        {
            if (string.IsNullOrEmpty(type)) return string.Empty;
            return Loc.TryGet("type_" + type) ?? TextUtil.FromObjectName(type).ToLowerInvariant();
        }

        // ---------------- Equipment cards ----------------

        public static string CategoryName(string code)
        {
            StatsEquipmentType t = CStatsManager.GetEquipmentTypeFromCode(code);
            string own = Loc.TryGet("category_" + code);
            if (own != null) return own;
            return t != null ? TextUtil.Clean(t.Name) : code;
        }

        public static int Cost(StatsEquipmentBase e)
        {
            StatsEquipmentType t = CStatsManager.GetEquipmentTypeFromCode(e.Code);
            return t != null ? t.Cost : 0;
        }

        /// <summary>Name, category and current state (uses, clips, CATS, scrambled).</summary>
        public static string EquipmentShort(WeaponInfo w)
        {
            if (w == null || w.mpEquipment == null) return string.Empty;
            StatsEquipmentBase e = w.mpEquipment;
            var parts = new List<string> { G.Text(e.Name) + ", " + CategoryName(e.Code) };
            parts.AddRange(State(w));
            return TextUtil.Join(parts);
        }

        public static IEnumerable<string> State(WeaponInfo w)
        {
            StatsEquipmentBase e = w.mpEquipment;
            if (e.Uses > 0) yield return Loc.Get("eq_uses", w.mnWeaponUses, e.Uses);
            if (w.mnClips > 0) yield return Loc.Get("eq_clips", w.mnClips);
            if (e.CATS && w.mbCats) yield return Loc.Get("eq_cats_on");
            if (e.Electrical && w.mnRoundsDisabled > 0) yield return Loc.Get("eq_scrambled", w.mnRoundsDisabled);
        }

        /// <summary>The whole card: name, category, cost, state, text and the numbers to hit.</summary>
        public static string EquipmentFull(WeaponInfo w, bool showCost)
        {
            if (w == null || w.mpEquipment == null) return string.Empty;
            StatsEquipmentBase e = w.mpEquipment;
            var parts = new List<string> { G.Text(e.Name) + ", " + CategoryName(e.Code) };
            if (showCost) parts.Add(Loc.Get("eq_cost", Cost(e)));
            parts.AddRange(State(w));
            parts.Add(G.Text(e.Text));
            string hits = HitNumbers(w);
            if (hits.Length > 0) parts.Add(hits);
            var flags = new List<string>();
            if (e.Electrical) flags.Add(Loc.Get("eq_electrical"));
            if (e.CATS) flags.Add(Loc.Get(w.mbCats ? "eq_cats_attached" : "eq_cats_possible"));
            if (e.Discard) flags.Add(Loc.Get("eq_discard"));
            if (flags.Count > 0) parts.Add(string.Join(", ", flags.ToArray()));
            return TextUtil.Join(parts);
        }

        /// <summary>For guns: the 2-dice score needed against each kind of target, as printed on the card.</summary>
        public static string HitNumbers(WeaponInfo w)
        {
            StatsWeapon weapon = w.mpEquipment as StatsWeapon;
            if (weapon == null) return string.Empty;
            string code = w.mpEquipment.Code;
            if (code != "S" && code != "W" && code != "G") return string.Empty;
            int mod = 0;
            if (G.InGame) mod = G.Game.mnMarksCombat + G.Game.Marksman;
            if (w.mbCats) mod += 2;
            var list = new List<string>
            {
                Loc.Get("hit_entry", Loc.Get("hits_Zombie"), Clamp(weapon.HitZombie - mod)),
                Loc.Get("hit_entry", Loc.Get("hits_ChaosAgent"), Clamp(weapon.HitChaosAgent - mod)),
                Loc.Get("hit_entry", Loc.Get("hits_Door"), Clamp(weapon.HitDoor - mod)),
                Loc.Get("hit_entry", Loc.Get("hits_MeatMachine"), Clamp(weapon.HitMeatMachine - mod)),
                Loc.Get("hit_entry", Loc.Get("hits_Mutant"), Clamp(weapon.HitMutant - mod)),
                Loc.Get("hit_entry", Loc.Get("hits_RatPack"), Clamp(weapon.HitRat - mod)),
            };
            return Loc.Get("hit_numbers", string.Join(", ", list.ToArray()));
        }

        private static int Clamp(int v)
        {
            return Mathf.Clamp(v, 0, 100);
        }
    }
}
