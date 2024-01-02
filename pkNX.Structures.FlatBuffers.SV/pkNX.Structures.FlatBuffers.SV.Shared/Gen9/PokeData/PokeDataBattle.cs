using System;
using System.ComponentModel;
using System.IO;
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedType.Global

namespace pkNX.Structures.FlatBuffers.SV;

[TypeConverter(typeof(ExpandableObjectConverter))]
public partial class PokeDataBattle
{
    public void SerializePKHeX(BinaryWriter bw, sbyte captureLv, RaidSerializationFormat format)
    {
        if (format == RaidSerializationFormat.BaseROM)
            AssertRegularFormat();

        // If any PointUp for a move is nonzero, throw an exception.
        if (Waza1.PointUp != 0 || Waza2.PointUp != 0 || Waza3.PointUp != 0 || Waza4.PointUp != 0)
            throw new ArgumentOutOfRangeException(nameof(WazaSet.PointUp), $"No {nameof(WazaSet.PointUp)} allowed!");

        // flag BallId if not none
        if (BallId != BallType.NONE)
            throw new ArgumentOutOfRangeException(nameof(BallId), BallId, $"No {nameof(BallId)} allowed!");

        ushort species = SpeciesConverterSV.GetNational9((ushort)DevId);
        byte form = species switch
        {
            //(ushort)Species.Vivillon or (ushort)Species.Spewpa or (ushort)Species.Scatterbug => 30,
            (ushort)Species.Minior when FormId < 7 => (byte)(FormId + 7),
            _ => (byte)FormId,
        };

        bw.Write(species);
        bw.Write(form);
        bw.Write((byte)Sex);

        bw.Write((byte)Tokusei);
        bw.Write((byte)(TalentType == TalentType.V_NUM ? TalentVnum : 0));
        bw.Write((byte)RareType);
        bw.Write((byte)captureLv);

        // raid_enemy_03_array: Dreepy is "Default", but the Manual moves are correct.
        // su2_raid_enemy_03: Beldum is "Default", fill in the moves manually.
        if (WazaType == WazaType.MANUAL || (DevId == DevID.DEV_DORAMESIYA && Level == 35)) // Dreepy
        {
            // Write moves
            bw.Write((ushort)Waza1.WazaId);
            bw.Write((ushort)Waza2.WazaId);
            bw.Write((ushort)Waza3.WazaId);
            bw.Write((ushort)Waza4.WazaId);
        }
        else if (WazaType == WazaType.DEFAULT && DevId == DevID.DEV_DANBARU && Level == 35) // Beldum
        {
            // Level 35 moves
            bw.Write((ushort)Move.Tackle);
            bw.Write((ushort)0);
            bw.Write((ushort)0);
            bw.Write((ushort)0);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(WazaType), WazaType, $"No {nameof(WazaType)} allowed!");
        }

        // ROM raids with 5 stars have a few entries that are defined as DEFAULT
        // If the type is not {specified}, the game will assume it is RANDOM.
        // Thus, DEFAULT behaves like RANDOM.
        // Let's clean up this mistake and make it explicit, so we don't have to program this workaround in other tools.
        var gem = GemType is GemType.DEFAULT ? GemType.RANDOM : GemType;
        bw.Write((byte)gem);
    }

    private void AssertRegularFormat()
    {
        if (TalentType != TalentType.V_NUM)
            throw new ArgumentOutOfRangeException(nameof(TalentType), TalentType, "No min flawless IVs?");
        if (TalentVnum == 0 && DevId != DevID.DEV_PATIRISU && Level != 35) // nice mistake gamefreak -- 3star Pachirisu is 0 IVs.
            throw new ArgumentOutOfRangeException(nameof(TalentVnum), TalentVnum, "No min flawless IVs?");

        if (Seikaku != SeikakuType.DEFAULT)
            throw new ArgumentOutOfRangeException(nameof(Seikaku), Seikaku, $"No {nameof(Seikaku)} allowed!");
    }
}

public enum RaidSerializationFormat
{
    /// <summary>
    /// Base ROM Raids
    /// </summary>
    BaseROM,

    /// <summary>
    /// Regular Distribution Raids
    /// </summary>
    Distribution,

    /// <summary>
    /// 7-Star Distribution Raids
    /// </summary>
    Might,
}
