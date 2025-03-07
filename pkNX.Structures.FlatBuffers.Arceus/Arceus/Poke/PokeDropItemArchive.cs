using System;
using System.ComponentModel;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace pkNX.Structures.FlatBuffers.Arceus;

[TypeConverter(typeof(ExpandableObjectConverter))]
public partial class PokeDropItemArchive { }

[TypeConverter(typeof(ExpandableObjectConverter))]
public partial class PokeDropItem
{
    public string Dump(string[] itemNames) => $"{Hash:X16}\t{itemNames[RegularItem]}\t{RegularItemProbability}\t{itemNames[RareItem]}\t{RareItemProbability}";
}
