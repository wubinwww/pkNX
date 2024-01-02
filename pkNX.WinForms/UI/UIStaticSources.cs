using pkNX.Game;
using pkNX.Structures;
using System;

namespace pkNX.WinForms
{
    public static class UIStaticSources
    {
        public static readonly string[] EvolutionMethods = Enum.GetNames(typeof(EvolutionType));
        public static readonly string[] EggGroups = Enum.GetNames(typeof(EggGroup));
        public static readonly string[] PokeColors = Enum.GetNames(typeof(PokeColor));
        public static readonly string[] EXPGroups = Enum.GetNames(typeof(EXPGroup));
        public static string[] SpeciesList = [];
        public static string[] FormsList = [];
        public static string[] SpeciesClassificationsList = [];
        public static string[] ItemsList = [];
        public static string[] AbilitiesList = [];
        public static string[] MovesList = [];
        public static string[] TypesList = [];

        public static void SetupForGame(GameManager ROM)
        {
            if (ROM.Game == GameVersion.SV)
                return; // SV doesn't support these file mappings yet

            SpeciesList = ROM.GetStrings(TextName.SpeciesNames);
            FormsList = ROM.GetStrings(TextName.Forms);
            ItemsList = ROM.GetStrings(TextName.ItemNames);
            MovesList = ROM.GetStrings(TextName.MoveNames);
            MovesList = EditorUtil.SanitizeMoveList(MovesList);
            TypesList = ROM.GetStrings(TextName.TypeNames);
            AbilitiesList = ROM.GetStrings(TextName.AbilityNames);
            SpeciesClassificationsList = ROM.GetStrings(TextName.SpeciesClassifications);
        }
    }
}
