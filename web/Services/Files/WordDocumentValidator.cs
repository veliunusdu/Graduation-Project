using System.Buffers.Binary;
using System.Text;
namespace HomeworkPlatform.Web.Services.Files;

internal static class WordDocumentValidator
{
    // Check the CFB directory rather than accepting every OLE file (for example, renamed XLS files).
    // Layout: https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-cfb/05060311-bfce-4b12-874d-71fd4ce63aea
    public static void ValidateDoc(string path)
    {
        var bytes = File.ReadAllBytes(path); // The caller has already enforced the upload size limit.
        try
        {
            if (!IsWordCompoundFile(bytes)) throw new SubmissionFileException("Choose a valid DOC document.");
        }
        catch (Exception error) when (error is ArgumentException or OverflowException)
        {
            throw new SubmissionFileException("Choose a valid DOC document.");
        }
    }

    private static bool IsWordCompoundFile(byte[] data)
    {
        const uint Free = 0xFFFFFFFF, End = 0xFFFFFFFE;
        if (data.Length < 512 || !data.AsSpan(0, 8).SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 })) return false;
        ushort Read16(int offset) => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));
        uint Read32(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
        var major = Read16(26);
        var shift = Read16(30);
        if (Read16(28) != 0xFFFE || Read16(32) != 6 || (major != 3 && major != 4) || shift != (major == 3 ? 9 : 12)) return false;
        var sectorSize = 1 << shift;
        var sectorCount = data.Length / sectorSize - 1;
        if (sectorCount < 2 || data.Length % sectorSize != 0) return false;
        bool IsSector(uint sector) => sector < sectorCount;
        int Offset(uint sector) => checked(((int)sector + 1) * sectorSize);
        var fatCount = Read32(44);
        if (fatCount == 0 || fatCount > sectorCount) return false;
        var fat = new List<uint>();
        bool AddFat(uint sector)
        {
            if (sector == Free) return true;
            if (!IsSector(sector) || fat.Count >= fatCount) return false;
            fat.Add(sector); return true;
        }
        for (var index = 0; index < 109; index++) if (!AddFat(Read32(76 + index * 4))) return false;
        var difat = Read32(68);
        var difatCount = Read32(72);
        if (difatCount > sectorCount) return false;
        var seenDifat = new HashSet<uint>();
        for (var count = 0u; count < difatCount; count++)
        {
            if (!IsSector(difat) || !seenDifat.Add(difat)) return false;
            var offset = Offset(difat);
            for (var index = 0; index < sectorSize / 4 - 1; index++) if (!AddFat(Read32(offset + index * 4))) return false;
            difat = Read32(offset + sectorSize - 4);
        }
        if (fat.Count != fatCount || fat.Distinct().Count() != fat.Count) return false;
        var sector = Read32(48);
        var seenDirectory = new HashSet<uint>();
        var wordDocument = false;
        var first = true;
        while (sector != End)
        {
            if (!IsSector(sector) || !seenDirectory.Add(sector)) return false;
            var offset = Offset(sector);
            if (first && data[offset + 66] != 5) return false; // Root storage is directory entry zero.
            first = false;
            for (var entry = offset; entry < offset + sectorSize; entry += 128)
            {
                if (data[entry + 66] != 2 || Read16(entry + 64) != 26) continue;
                var name = Encoding.Unicode.GetString(data, entry, 24);
                if (name.Equals("WordDocument", StringComparison.OrdinalIgnoreCase) && Read32(entry + 120) > 0)
                    wordDocument = true;
            }
            var fatIndex = sector / (uint)(sectorSize / 4);
            if (fatIndex >= fat.Count) return false;
            sector = Read32(Offset(fat[(int)fatIndex]) + (int)(sector % (sectorSize / 4)) * 4);
        }
        return wordDocument;
    }
}
