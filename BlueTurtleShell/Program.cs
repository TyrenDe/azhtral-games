using MixItUp.API;
using MixItUp.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueTurtleShell
{
    class Program
    {
        private const string CurrencyName = "Credits";
        private static readonly string[] ProtectedUserNames = new string[]
        {
            "Azhtral",
            "SNOTR_BOT",
        };

        private static Random m_Random = new Random();

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("You must call this program with: $username $targetusername");
            }

            Task runTask = RunAsync(args[0], args[1]);
            runTask.Wait();
        }

        static async Task RunAsync(string callingUserName, string targetUserName)
        {
            int amount = m_Random.Next(3000, 5000);
            int dropped = m_Random.Next(1, 250);

            Currency[] currencies = await Currencies.GetAllCurrenciesAsync();
            Currency credits = currencies.SingleOrDefault(c => c.Name.Equals(CurrencyName, StringComparison.InvariantCultureIgnoreCase));
            if (credits == null)
            {
                Console.WriteLine("Cannot find `Credits` currency!");
                return;
            }

            List<User> users = new List<User>(await Chat.GetAllUsersAsync());
            User targetUser = users.SingleOrDefault(u => u.UserName.Equals(targetUserName, StringComparison.InvariantCultureIgnoreCase));
            if (targetUser == null)
            {
                Console.WriteLine($"Cannot find `{targetUserName}` in chat!");
                return;
            }

            User callingUser = users.SingleOrDefault(u => u.UserName.Equals(callingUserName, StringComparison.InvariantCultureIgnoreCase));
            if (targetUser == null)
            {
                Console.WriteLine($"Cannot find `{callingUserName}` in chat!");
                return;
            }

            // Remove ineligible users
            users.RemoveAll(u => u.UserName.Equals(targetUserName, StringComparison.InvariantCultureIgnoreCase));
            users.RemoveAll(u => u.UserName.Equals(callingUserName, StringComparison.InvariantCultureIgnoreCase));
            foreach (string protectedUserName in ProtectedUserNames)
            {
                users.RemoveAll(u => u.UserName.Equals(protectedUserName, StringComparison.InvariantCultureIgnoreCase));
            }

            if (users.Count < 2)
            {
                Console.WriteLine($"Not enough users to perform this action!");
                return;
            }

            User alpha = users[m_Random.Next(0, users.Count)];
            users.Remove(alpha);

            User bravo = users[m_Random.Next(0, users.Count)];
            users.Remove(bravo);

            int actualAmount = await SubtractFromUser(targetUser, credits, amount);
            int alphaDropped = await SubtractFromUser(alpha, credits, dropped);
            int bravoDropped = await SubtractFromUser(bravo, credits, dropped);
            int total = actualAmount + alphaDropped + bravoDropped;

            await Users.AdjustUserCurrencyAsync(callingUser.ID, credits.ID, total);

            await Chat.SendMessageAsync($"/me @{callingUserName} throws their Blue Turtle Shell at @{targetUserName}! The shell hits @{alpha.UserName} making them drop {alphaDropped} credits and @{bravo.UserName} forcing them to drop {bravoDropped} credits! The shell smashes into {targetUserName}, knocking {actualAmount} credits out of their pockets! {callingUserName} quickly gathers up their {total} credits and scampers off!", false);
        }

        static async Task<int> SubtractFromUser(User user, Currency currency, int amount)
        {
            int quantity = 0;
            CurrencyAmount userCurrency = user.CurrencyAmounts.SingleOrDefault(c => c.ID == currency.ID);
            if (userCurrency != null)
            {
                quantity = userCurrency.Amount;
            }

            if (amount > quantity)
            {
                // They don't have enough, steal what you can
                amount = quantity;
            }

            if (amount > 0)
            {
                await Users.AdjustUserCurrencyAsync(user.ID, currency.ID, -amount);
            }

            return amount;
        }
    }
}
