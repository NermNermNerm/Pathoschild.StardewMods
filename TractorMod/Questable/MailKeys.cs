using System;
using System.Collections.Generic;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    public static class MailKeys
    {
        public const string BuildTheGarage = "QuestableTractorMod.BuildTheGarage";
        public const string FixTheEngine = "QuestableTractorMod.FixTheEngine";
        public const string WatererRepaired = "QuestableTractorMod.FixTheEngine";
        public const string GeorgeSeederMail = "QuestableTractorMod.GeorgeSeederMail";
        public const string EvelynPointsToAlex = "QuestableTractorMod.EvelynPointsToAlex";
        public const string HaleysWorkIsDone = "QuestableTractorMod.HaleysWorkIsDone";
        public const string AlexThankYouMail = "QuestableTractorMod.AlexThankYouMail";

        public static void EditAssets(IDictionary<string, string> mailItems)
        {
            mailItems[MailKeys.BuildTheGarage] = "Hey there!^I talked with Sebastian about your tractor and he has agreed to work on it, but only if he's got a decent place to work.  I understand that you're just starting out here and don't have a lot of money laying around, so I'm willing to do it at-cost, providing you can come up with the materials.  Come by my shop for a full list of materials.  See you soon!^  - Robin";
            mailItems[MailKeys.FixTheEngine] = "I got everything working except this engine.  I've never seen anything like it.  I mean, it's like it doesn't even need gas!^I don't know what you're gonna need to do to make it work, but I know I'm out of my area here.^If you manage to figure it out, bring it back up to my place and I'll see about getting it installed.^  - Sebastian"
                                              + $"%item object {ObjectIds.BustedEngine} 1%%";
            mailItems[MailKeys.WatererRepaired] = "Thanks for letting me work on this!  I even let my Dad do some of the work on it so that he got to feel like maybe he finally did make good on his promise to your Granddad all those years ago.  But me, well, I just like gadgets!  If it ever breaks down, let me know, I have a 10-year warranty on all my work :)"
                                                + $"%item object {ObjectIds.WorkingWaterer} 1%%";
            mailItems[MailKeys.GeorgeSeederMail] = "I hear you're restoring that old tractor.  Here's the seeder - it spreads fertilizer and seeds.  Your Grandpa gave it to me to fix after he decided to try going organic and put chicken droppings in it. "
                                                 + "I got it cleaned up, but it needed some iron bars.  Your Grandpa took his time rounding those up, and, well, my accident and his decline put an end to it.  But you're a young man, you can find somebody to fix it. "
                                                 + "I'm too old to be of any use anymore."
                                                 + $"%item object {ObjectIds.BustedSeeder} 1%%";
            mailItems[MailKeys.EvelynPointsToAlex] = "Hello Dear,^I gave what you said a lot of thought and I think I have a way.  George would be really uncomfortable with you doing work he would want to do.^But George has had to get used to letting Alex do a lot of things for him already, so it might be easier.^I talked to him already, but he seems uneasy about it.  Maybe someone nearer his own age who he trusted talked to him he would come around.^Good luck!^ - Granny";
            mailItems[MailKeys.HaleysWorkIsDone] = "@ - I think you'll find Alex is a little more... inspired... today.^ - Haley";
            mailItems[MailKeys.AlexThankYouMail] = "Hey!^Grandpa and I got the seeder working last night!  You can pick it up from Grandpa whenever.  Now that it's done, I'm really happy you rooked me into doing this. "
                                                 + "I really love my grandpa, and well, sometimes it's hard to find things to do together.  And hey, maybe if the Gridball thing doesn't work out I can get a job repairing "
                                                 + "farm equipment!^ - Your friend,^   Alex";
        }
    }
}
