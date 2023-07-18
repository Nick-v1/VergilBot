namespace VergilBot.Models.Misc
{
    public class VergilQuotes
    {
        private readonly List<string> _quotes = new List<string>();

        public VergilQuotes()
        {
            _quotes.Add("Foolishness, Dante. Foolishness. Might controls everything. And without strength you can not protect anything. Let alone yourself.");
            _quotes.Add("Why isn't this working? Is there something missing? Must more blood be shed?");
            _quotes.Add("Insane buffoon! I don't know where you came from but you don't belong here. Now leave!");
            _quotes.Add("I've come to retrieve my power. You can't handle it.");
            _quotes.Add("Unfortunately, our souls are at odds, brother. I need more power!");
            _quotes.Add("No one can have this Dante. It's mine. It belongs to a son of Sparda. Leave me and go, if you don't want to be trapped in the Demon World. I'm staying. This place, was our father's home.");
            _quotes.Add("It'll be fun to fight with the Prince of Darkness. If my father did it... I should be able to do it too!");
            _quotes.Add("The Order of the Sword huh? They worship a demon as a god? Just what are your true intentions?");
            _quotes.Add("Well, I can't exactly call them misguided. But soon they shall know this devil's power. A power greater than they ever imagined. The power of a son of Sparda.");
            _quotes.Add("I'm taking this back. I'm running out of time...");
            _quotes.Add("It's nearly...time... At last...I will...");
            _quotes.Add("Defeating you like this has no meaning. Heal your wounds, Dante. Get strong. After that, we'll settle the matter.");
            _quotes.Add("That day, if our positions were switched... Would our fates be different? Would I have your life, and you mine? Let's settle this... Dante.");
            _quotes.Add("If you want it... then you'll have to take it. But you already knew that. How many times have we fought?");
            _quotes.Add("I won't lose to the likes of you...little brother.");
            _quotes.Add("Farewell, Dante.");
            _quotes.Add("All things end, Dante. Even us...");
            _quotes.Add("Foolishness....is rushing in blind all you can do?");
            _quotes.Add("Save that until you win, if you can.");
            _quotes.Add("My son... means nothing to me!");
            _quotes.Add("Nero is my son? Well, well... That was a long time ago. Ended this.");
            _quotes.Add("If I beat Nero... Then by default, I beat you. Agreed, Dante?");
            _quotes.Add("This has nothing to do with you. Stand down.");
            _quotes.Add("Nero...");
            _quotes.Add("Of your existence? Or your strength?");
            _quotes.Add("Interesting...");
            _quotes.Add("I can still fight. But if those roots continue to spread through town, it'll just interfere with our business.");
            _quotes.Add("Make haste, Dante.");
            _quotes.Add("I won't lose next time. Hold onto that until then.");
            _quotes.Add("That's enough. I don't want to hear it. Now don't get in my way.");
            _quotes.Add("That's it. Time to die!");
            _quotes.Add("Maybe. We got plenty of time.");
            _quotes.Add("Hmph, as if there was any doubt. Admit it, Dante. I'm just better than you.");
            _quotes.Add("Get lost!");
            _quotes.Add("So slow.");
            _quotes.Add("Pointless!");
            _quotes.Add("You're just gonna stand there?");
            _quotes.Add("Not bad!");
            _quotes.Add("Such a fool!");

            var s = new string[] { "Placeholder for new quotes", "new quote 1" };
        }

        public string getQuote()
        {
            //generating true randomness
            var ran = ThreadLocalRandom.Next(0, _quotes.Count + 1);

            return _quotes[ran];
        }

    }
}
