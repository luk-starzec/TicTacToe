using System.ComponentModel;
using System.Linq;

namespace TicTacToe.Models
{
    public enum EnumGameResult
    {
        None = 1,
        [Description("it' a tie")]
        Tie = 2,
        [Description("X won")]
        WinX = 3,
        [Description("O won")]
        WinO = 4,
        [Description("X timeout")]
        TimeOutX = 5,
        [Description("O timeout")]
        TimeOutO = 6,
    }

    public static class EnumGameResultExtensions
    {
        public static string GetDescription(this EnumGameResult value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            return attributes?.Any() == true ? attributes.First().Description : value.ToString();
        }
    }
}
