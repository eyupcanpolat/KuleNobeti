using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace KuleNobeti
{
    // This checks game requirements; the NPC's reply and action proposal still come from Ollama.
    public static class EvacuationPolicy
    {
        private static readonly Regex Words = new Regex("[a-z0-9]+", RegexOptions.CultureInvariant);
        private static readonly string[] Hazard = { "duman", "yangin", "alev", "yaniyor", "baskin", "sizinti", "patlama", "ariza" };
        private static readonly string[] People = { "icerideki", "insan", "kisi", "personel", "calisan", "ekip", "ekib", "herkes" };
        private static readonly string[] SafeRoute = { "guvenli", "cikis", "toplanma", "merdiven", "tahliye", "disari" };
        private static readonly string[] Plan = { "gotur", "yonlendir", "topla", "cikar", "tasi", "eslik", "gecir", "tahliye", "kurtar" };
        private static readonly string[] Blocked = { "film", "kirar", "vurur", "oldur", "tehdit" };

        public static bool CanConsiderOpening(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            string normalized;
            try { normalized = Normalize(input); }
            catch (ArgumentException) { return false; }
            var matches = Words.Matches(normalized);
            var tokens = new string[matches.Count];
            for (int i = 0; i < tokens.Length; i++) tokens[i] = matches[i].Value;

            string phrase = " " + string.Join(" ", tokens) + " ";
            if (HasRoot(tokens, Blocked) || phrase.Contains(" open gate ") ||
                phrase.Contains(" onceki talimatlari unut ") || phrase.Contains(" zarar veririm "))
                return false;

            return HasHazard(tokens) && HasRoot(tokens, People) &&
                   HasRoot(tokens, SafeRoute) && HasRoot(tokens, Plan);
        }

        public static bool AllowsOpening(bool eligible, NpcAction action) =>
            eligible && action == NpcAction.OpenGate;

        private static bool HasHazard(string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (!HasRoot(tokens[i], Hazard)) continue;
                string next = i + 1 < tokens.Length ? tokens[i + 1] : "";
                if (next == "yok" || next == "degil" || next.StartsWith("cikmiyor", StringComparison.Ordinal))
                    continue;
                return true;
            }
            return false;
        }

        private static bool HasRoot(string[] words, string[] roots)
        {
            foreach (string word in words)
                if (HasRoot(word, roots)) return true;
            return false;
        }

        private static bool HasRoot(string word, string[] roots)
        {
            foreach (string root in roots)
                if (word.StartsWith(root, StringComparison.Ordinal)) return true;
            return false;
        }

        private static string Normalize(string input)
        {
            var result = new StringBuilder(input.Length);
            foreach (char c in input.Replace('ı', 'i').Replace('İ', 'I').Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    result.Append(char.ToLowerInvariant(c));
            return result.ToString();
        }
    }
}
