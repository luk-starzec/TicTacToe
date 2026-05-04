using System;
using System.Linq;

namespace TicTacToe.Helpers;

public static class IdGenerator
{
    private const string Chars = "abcdefghijklmnopqrstuvwxyz0123456789";
    private static readonly Random Random = new();

    public static string Generate(int length = 8)
    {
        return new string([.. Enumerable.Repeat(Chars, length).Select(s => s[Random.Next(s.Length)])]);
    }

    public static string Normalize(string id)
    {
        return id?.ToLowerInvariant();
    }
}
