using System.Text;

namespace LeveLEO.Helpers;

public static class Base32Helper
{
    private static readonly char[] Digits = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    public static string ToBase32(byte[] data)
    {
        int i = 0, index = 0;
        int currByte, nextByte;
        StringBuilder result = new((data.Length + 7) * 8 / 5);

        while (i < data.Length)
        {
            currByte = data[i] >= 0 ? data[i] : (data[i] + 256); // unsigned
            if (index > 3)
            {
                if (i + 1 < data.Length)
                    nextByte = data[i + 1] >= 0 ? data[i + 1] : (data[i + 1] + 256);
                else
                    nextByte = 0;

                int digit = currByte & (0xFF >> index);
                index = (index + 5) % 8;
                digit <<= index;
                digit |= nextByte >> (8 - index);
                i++;
                result.Append(Digits[digit]);
            }
            else
            {
                int digit = (currByte >> (8 - (index + 5))) & 0x1F;
                index = (index + 5) % 8;
                if (index == 0)
                    i++;
                result.Append(Digits[digit]);
            }
        }

        return result.ToString();
    }

    public static byte[] FromBase32(string base32)
    {
        if (string.IsNullOrEmpty(base32))
            return [];

        base32 = base32.TrimEnd('=').ToUpperInvariant();
        int byteCount = base32.Length * 5 / 8;
        byte[] bytes = new byte[byteCount];

        int buffer = 0, bitsLeft = 0, index = 0;

        foreach (var c in base32)
        {
            int val = Array.IndexOf(Digits, c);
            if (val < 0)
                throw new ArgumentException("Invalid Base32 character", nameof(base32));

            buffer <<= 5;
            buffer |= val & 0x1F;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bytes[index++] = (byte)(buffer >> (bitsLeft - 8));
                bitsLeft -= 8;
            }
        }

        return bytes;
    }
}