using Application.Common.Interfaces;

namespace Infrastructure.Common
{
    public class AdjectiveNounUsernameGenerator : IUsernameGenerator
    {
        public async Task<string> GenerateUsername()
        {
            string[] adjectives = [
                "Nice", "Buff", "Funny", "Cute", "Soft", "Cool", "Sweet", "Kind"
            ];

            string[] nouns = [
                "Worm", "Snail", "Bee", "Chicken", "Bunny", "Dragonfly", "Cow", "Fox", "Caribou", "Turtle", "Goat", "Butterfly", "Capybara"
            ];

            Random rnd = new();
            int adjIndex = rnd.Next(0, adjectives.Length);
            int nounIndex = rnd.Next(0, nouns.Length);
            int numEnd = rnd.Next(7, 100);
            string username = adjectives[adjIndex] + nouns[nounIndex] + numEnd;
            return username;
        }
    }
}