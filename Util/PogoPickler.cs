﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static PKHeX.Core.Species;

namespace PoGoEncTool
{
    public static class PogoPickler
    {
        private const string identifier = "go";

        public static byte[] WritePickle(PogoEncounterList entries)
        {
            var data = GetEntries(entries.Data);

            return BinLinker.Pack(data, identifier);
        }

        private static ReadOnlyMemory<byte>[] GetEntries(IReadOnlyList<PogoPoke> entries)
        {
            var result = new ReadOnlyMemory<byte>[entries.Count];
            for (int i = 0; i < entries.Count; i++)
                result[i] = GetBinary(entries[i]);
            return result;
        }

        private static byte[] GetBinary(PogoPoke entry)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            var sf = entry.Species | (entry.Form << 11);
            bw.Write((ushort)sf);
            foreach (var a in entry.Data)
                Write(a, bw);

            return ms.ToArray();
        }

        private static void Write(PogoEntry entry, BinaryWriter bw)
        {
            bw.Write(entry.Start?.Write(entry.LocalizedStart ? -1 : 0) ?? 0);
            bw.Write(entry.End?.Write(!entry.NoEndTolerance ? 1 : 0) ?? 0);

            bw.Write((byte)PogoToHex(entry.Shiny));
            bw.Write((byte)entry.Type);
        }

        private static int PogoToHex(PogoShiny type)
        {
            return type switch
            {
                PogoShiny.Random => 1,
                PogoShiny.Always => 2,
                PogoShiny.Never => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public static byte[] WritePickleLGPE(PogoEncounterList entries)
        {
            var data = GetPickleLGPE(entries);
            return BinLinker.Pack(data, identifier);
        }

        private static ReadOnlyMemory<byte>[] GetPickleLGPE(PogoEncounterList entries)
        {
            var noForm = Enumerable.Range(1, 150).Concat(Enumerable.Range(808, 2)); // count : 152
            var forms = new[]
            {
                (byte)Rattata,
                (byte)Raticate,
                (byte)Raichu,
                (byte)Sandshrew,
                (byte)Sandslash,
                (byte)Vulpix,
                (byte)Ninetales,
                (byte)Diglett,
                (byte)Dugtrio,
                (byte)Meowth,
                (byte)Persian,
                (byte)Geodude,
                (byte)Graveler,
                (byte)Golem,
                (byte)Grimer,
                (byte)Muk,
                (byte)Exeggutor,
                (byte)Marowak,
            };

            var regular = noForm.Select(z => entries.GetDetails(z, 0));
            var alolan = forms.Select(z => entries.GetDetails(z, 1));

            var all = regular.Concat(alolan).ToArray();
            var result = new ReadOnlyMemory<byte>[all.Length];
            for (var i = 0; i < all.Length; i++)
            {
                var entry = all[i];
                entry.Data.RemoveAll(z => z.Type.IsShadow());
                result[i] = GetBinary(entry);
            }

            return result;
        }
    }
}
