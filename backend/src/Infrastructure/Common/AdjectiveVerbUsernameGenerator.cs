using Application.Common.Interfaces;

namespace Infrastructure.Common
{
    public class AdjectiveNounUsernameGenerator : IUsernameGenerator
    {
        public async Task<string> GenerateUsername()
        {
            string[] adjectives = [
                "Nice", "Buff", "Funny", "Cute", "Soft", "Cool", "Sweet", "Kind", "Gentle", "Creative", "Happy", "Curious", "Glad", "Warm", "Mighty", "Bright"
            ];

            string[] nouns = [
                "Worm", "Snail", "Bee", "Chicken", "Bunny", "Dragonfly", "Cow", "Fox", "Caribou", "Turtle", "Goat", "Butterfly", "Capybara", "Chipmunk",
                "Giraffe", "Alpaca", "Frog", "Deer", "Sparrow", "Bear", "Elephant", "Ostrich", "Meerkat", "Penguin", "Dog", "Cat", "Kitten", "Puppy", "Hamster",
                "Dolphin", "Gorilla", "Rhino", "Octopus", "Eagle", "Manatee", "Snake", "Gopher", "Ant", "Sheep", "Goose", "Duck"
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