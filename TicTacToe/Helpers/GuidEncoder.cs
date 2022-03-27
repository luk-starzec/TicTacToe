using System;

namespace TicTacToe.Helpers
{
    public static class GuidEncoder
    {
        public static string Encode(Guid guid)
        {
            string base64 = Convert.ToBase64String(guid.ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-");

            // Remove the trailing ==
            var encoded = base64[..22];

            return encoded;
        }

        public static Guid Decode(string encoded)
        {
            encoded = encoded
                .Replace("_", "/")
                .Replace("-", "+");

            byte[] buffer = Convert.FromBase64String(encoded + "==");
            return new Guid(buffer);
        }
    }
}
