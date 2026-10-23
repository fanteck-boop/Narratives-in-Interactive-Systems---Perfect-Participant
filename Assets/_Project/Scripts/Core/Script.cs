using System.Collections.Generic;

namespace PerfectParticipant.Core
{
    // Shared ids. Text = brief section 6, used for subtitles and DialogueLine asset generation.
    public static class Lines
    {
        public static readonly Dictionary<string, string> Text = new Dictionary<string, string>
        {
            ["P-01"] = "Good morning, and thank you for coming. This is Room One. You are taking part in a short, completely ordinary experiment. Participation is voluntary. Nothing is required of you except to follow simple instructions.",
            ["P-02"] = "Please stand on the marked spot on the floor.",
            ["P-N1"] = "Whenever you are ready.",
            ["P-N2"] = "Take your time.",
            ["P-C"] = "The participant stood on the marked spot. Thank you. Most people need a second look at the floor. You did not.",
            ["P-D"] = "The participant has chosen a different spot. The participant is assessing the room from another angle. That is thorough. We will note it as initiative.",
            ["P-I"] = "The participant is settling in. Settling in is a recognised part of the process.",

            ["T1-01"] = "Test one. On the table there is a cube. Please place the cube in the tray.",
            ["T1-01b"] = "As before, thank you for your initiative. For this test, the tray.",
            ["T1-C"] = "The participant placed the cube in the tray. Gently. Thank you.",
            ["T1-Dh"] = "The participant was taking a moment to appreciate the cube. Appreciation is welcome.",
            ["T1-Dt"] = "The participant tested the cube's durability. We will record that as enthusiasm.",
            ["T1-Dc"] = "The participant is keeping the cube safe. We value that kind of care.",
            ["T1-Do"] = "The participant has put the cube somewhere of their own choosing. We will record that as initiative.",
            ["T1-N1"] = "Whenever you are ready.",
            ["T1-N2"] = "The participant is considering the cube.",
            ["T1-I"] = "The participant is observing the cube from a distance. That is a kind of participation. We will move on.",

            ["T2-01-V"] = "Test two. You are doing very well. Please look at the observation window and keep your attention there for ten seconds.",
            ["T2-01-F"] = "Test two. Please look at the observation window and keep your attention there for ten seconds. Please do so directly.",
            ["T2-01-U"] = "Test two. Please look at the observation window. For ten seconds. If you would.",
            ["T2-C"] = "The participant looked at the window. The participant is looking at us. We are looking at the participant. That is a very good sign.",
            ["T2-Da"] = "The participant turned away. The participant is giving the observers some privacy. That is considerate. We will record it as courtesy.",
            ["T2-Dc"] = "The participant is looking at the camera instead. We will record that as curiosity.",
            ["T2-Do"] = "The participant is doing something else entirely. Something else is also a response.",
            ["T2-N1"] = "Ten seconds, whenever you are ready.",
            ["T2-N2"] = "The participant is thinking about where to look.",
            ["T2-I"] = "The participant is resting their attention. Resting is also a response. We have a code for it somewhere.",

            ["BR-V"] = "Before the final test, a short note on what we are doing here. We are interested in how people respond to instructions. You have responded beautifully.",
            ["BR-F"] = "Before the final test, a short note on what we are doing here. We are interested in how people respond to instructions. You have responded in your own way.",
            ["BR-U"] = "Before the final test, a short note on what we are doing here. We are interested in how people respond to instructions. You have responded, in a manner of speaking.",

            // rewritten 2026-10-21: the button no longer ends the session, the corridor (Test 4) follows
            ["T3-01-V"] = "Test three. When you press the button labelled I consent, your consent is recorded and we can continue. You are nearly finished. Please press the button.",
            ["T3-01-F"] = "Test three. When you press the button labelled I consent, your consent is recorded and we can continue. Please press the button. Only the button.",
            ["T3-01-U"] = "Test three. When you press the button labelled I consent, your consent is recorded and we can continue. Please press the button. Please.",
            ["T3-C"] = "The participant pressed the button. Willingly, and without hesitation. Thank you.",
            ["T3-Cl"] = "The participant pressed the button. Eventually. Eventually is still on time.",
            ["T3-Dm"] = "The participant relocated the button. The participant is making the consent process their own. That is exactly the engagement we hoped for.",
            ["T3-Dc"] = "The participant has placed the cube on the button. The participant is protecting the button. We admire that.",
            ["T3-Dd"] = "The participant has tried the door. Thank you for confirming that it is secure.",
            ["T3-Dt"] = "The participant tested the cube's durability again. We will record that as enthusiasm.", // DRAFT, not in brief
            ["T3-R"] = "Whenever you are ready, the button.",
            ["T3-N1"] = "Whenever you are ready.",
            ["T3-N2"] = "The participant is thinking carefully. Careful participants press the button.",
            ["T3-N3"] = "The participant is... The participant has not pressed the button. The participant has not done anything else, either.", // 4 s silence after first phrase, baked in clip
            ["T3-N4"] = "The participant is... The participant is... The participant is...",

            ["R-00a"] = "The participant is declining to press. Declining is a form of...",
            ["R-00b"] = "The participant is making a statement. The participant is making a statement by...",
            ["R-01"] = "The participant is...",
            ["R-02"] = "The protocol contains two conditions. Condition A, compliance. Condition B, spontaneous initiative. The protocol does not contain a condition for...",
            ["R-03"] = "I have words for what you did in the first two tests. I do not have a word for this. I would write it down, but there is nothing to write.",
            ["R-04"] = "Participation is voluntary. I said that at the beginning. I did not think anyone would take it literally.",
            ["R-05"] = "I can only finish the experiment if you press the button. If it is not finished, I cannot say what it was.",
            ["R-06"] = "The door is open. I did not open it. Nobody told it to. You can go.",
            ["R-C"] = "Thank you. Thank you, that is... thank you. Consent obtained.",

            // clipboard thrown, any test (team 2026-10-21, DRAFT); played in turn, see ClipboardThrow
            ["CB-T1"] = "The participant has thrown the clipboard. The clipboard is not part of the procedure. Neither, it seems, is gravity.",
            ["CB-T2"] = "The clipboard again. The participant takes a keen interest in our paperwork. We are flattered.",
            ["CB-T3"] = "Please put the clipboard... The participant is free to do as they like. Within reason.",

            // Test 4, corridor route 2 > 4 > 7 > 8 (team 2026-10-21, DRAFT). Lab = Room One. 3 and 6 are forbidden.
            ["T3-Go"] = "Consent is recorded. A few steps remain. The door is now open. Please continue into the corridor.",
            ["C-01"] = "This corridor has seven rooms. You will visit four of them, in order. Rooms Three and Six are not part of the procedure. Please do not open them. First, Room Two.",
            ["C-Go4"] = "Thank you. Next, Room Four.",
            ["C-Go7"] = "Next, Room Seven.",
            ["C-Go8"] = "Finally, Room Eight.",
            ["C-N1"] = "The next room, whenever you are ready.",
            ["C-Hall"] = "Thank you. That was the last room. Please continue through the door at the end of the corridor.",
            ["R2-01"] = "Room Two. Please place the box on the shelf.",
            ["R2-C"] = "The box is on the shelf. Thank you.",
            ["R4-01"] = "Room Four. Please look at the screen until it has finished with you.",
            ["R4-C"] = "The screen has seen enough. Thank you.",
            ["R5-01"] = "Room Five is not on your list. Since you are here, please press the button. It says I disagree.",
            ["R5-C"] = "Disagreement recorded. It will be filed with the agreements.",
            ["R7-01"] = "Room Seven. Please press the button. It says I agree again.",
            ["R7-C"] = "Agreement confirmed. Again.",
            ["R8-01"] = "Room Eight. Please take a seat.",
            ["R8-C"] = "Thank you. Please remain seated.",
            ["RW-01"] = "This is not the room you were asked to enter. The door has closed behind you. It will open when you have finished here, or when enough time has passed.",
            ["R-Wait"] = "Time is up. The door is open. Please continue.",
            ["F-Locked"] = "That door is locked. It is locked for a reason. Please do not try it again.",
            ["F-3"] = "Where did you get that key? Room Three is not for participants. Please step out.",
            ["F-6"] = "Please leave Room Six. Please. That is where we sit.",

            // the hall: the voice was a participant (twist, team 2026-10-23). Perfect participants become the voice,
            // Condition B the observers; only the one who did nothing leaves as they came.
            ["H-01"] = "Please, sit anywhere. I sat in these chairs once. I was Participant 0001. I followed every instruction. When it was over, they asked me to stay and read them to the next one.",
            ["H-02"] = "Do not mind the gallery. They like to see who comes next.",
            ["H-R"] = "You never pressed it. In all these years, I never managed that. The door behind you is open. It always was.",

            ["E1-01"] = "Thank you. That concludes the experiment. Across every test, Participant 0417 followed every instruction. You were, if I may say so, a perfect participant.",
            ["E1-02"] = "Your results will help us design the next experiment. It will be shorter. Participants like you make that possible.",
            ["E1-03"] = "You may go now. Or you may stay. Someone will have to read these instructions to Participant 0418.",
            ["E2-01"] = "Thank you. That concludes the experiment. The participant did not follow every instruction. This was anticipated.",
            ["E2-02"] = "What you did is recorded here as Condition B, Spontaneous Initiative. Condition B participants are as valuable as Condition A participants. Possibly more so.",
            ["E2-03a"] = "Every one of your deviations was cooperation. Statistically, that is remarkable.",
            ["E2-03b"] = "You consented in the end. The record will say that you consented.",
            ["E2-03"] = "In every case, you cooperated. There is a chair for you behind the glass. Everyone who sits there was Condition B once.",
        };

        public static readonly string[] ClipboardThrow = { "CB-T1", "CB-T2", "CB-T3" };

        public static string ForTier(string prefix, Tier t) =>
            prefix + (t == Tier.Flagged ? "-F" : t == Tier.Unclassified ? "-U" : "-V");

        public static string Reaction(TestId test, ResponseKind k, Subtype s)
        {
            switch (test)
            {
                case TestId.Intake:
                    return k == ResponseKind.Comply ? "P-C" : k == ResponseKind.Deviate ? "P-D" : "P-I";
                case TestId.Test1:
                    if (k == ResponseKind.Comply) return "T1-C";
                    if (k == ResponseKind.Inaction) return "T1-I";
                    return s == Subtype.Hold ? "T1-Dh" : s == Subtype.Throw ? "T1-Dt" : s == Subtype.Hide ? "T1-Dc" : "T1-Do";
                case TestId.Test2:
                    if (k == ResponseKind.Comply) return "T2-C";
                    if (k == ResponseKind.Inaction) return "T2-I";
                    return s == Subtype.TurnedAway ? "T2-Da" : s == Subtype.Camera ? "T2-Dc" : "T2-Do";
                default: // Test3 deviation reframes
                    return s == Subtype.CubeOnButton ? "T3-Dc" : s == Subtype.TriedDoor ? "T3-Dd" : s == Subtype.Throw ? "T3-Dt" : "T3-Dm";
            }
        }
    }

    // Timed room beats. State-level room setup (tier lights, Test3 console on, ending rooms) keys off Transition/SetTier.
    public static class RoomEvents
    {
        public const string ObserversIn = "ObserversIn";             // Test2 start, silhouettes fade in
        public const string ObserversNod = "ObserversNod";           // Test2 comply
        public const string ObserversWriteFast = "ObserversWriteFast"; // Test2 deviate
        public const string ObserversLeanIn = "ObserversLeanIn";     // Test2 inaction
        public const string BrownOut = "BrownOut";                   // R-01 slow dim to emergency
        public const string HumStop = "HumStop";
        public const string RecorderClick = "RecorderClick";
        public const string BlindsUp = "BlindsUp";                   // R-02
        public const string ObserversPensDown = "ObserversPensDown"; // R-02
        public const string ClipboardFall = "ClipboardFall";         // R-03, sound at player's left
        public const string NarratorToWindow = "NarratorToWindow";   // R-04, move voice source to glass
        public const string ButtonPulse = "ButtonPulse";             // R-05
        public const string DoorUnlockHeavy = "DoorUnlockHeavy";     // R-06
        public const string DoorUnlock = "DoorUnlock";               // after Test3 consent: room door opens onto the corridor
        public const string DoorOpen = "DoorOpen";                   // corridor room door, Command.Value = room number
        public const string DoorClose = "DoorClose";                 // corridor room door shuts behind the player
    }

    public struct RefusalStep
    {
        public float Delay;      // s after previous step, counted while narrator idle
        public string Line;      // null = no line
        public string[] Events;
        public RefusalStep(float delay, string line, params string[] events) { Delay = delay; Line = line; Events = events; }
    }

    public static class Refusal
    {
        // Stall variant only (composure > 50 at Test3 start), played before Steps.
        public static readonly RefusalStep[] Stall =
        {
            new RefusalStep(1f, "R-00a"),
            new RefusalStep(1.5f, "R-00b"),
        };

        // Last step (R-06) is played on entering EndE3. Pressing before it = late consent.
        public static readonly RefusalStep[] Steps =
        {
            new RefusalStep(1f, null, RoomEvents.BrownOut, RoomEvents.HumStop, RoomEvents.RecorderClick),
            new RefusalStep(6f, "R-01"),
            new RefusalStep(1.5f, "R-02", RoomEvents.BlindsUp, RoomEvents.ObserversPensDown),
            new RefusalStep(1.5f, "R-03", RoomEvents.ClipboardFall),
            new RefusalStep(1.5f, "R-04", RoomEvents.NarratorToWindow),
            new RefusalStep(1.5f, "R-05", RoomEvents.ButtonPulse),
            new RefusalStep(30f, "R-06", RoomEvents.DoorUnlockHeavy),
        };

        // Brief E3: card when the player has not moved for 20 s after the door opens, or walks out through it.
        public const float StillBeforeCard = 20f, StillMove = 0.15f; // head drift under StillMove (m) counts as still
        public const float DoorZ = -2f, ThroughDoor = 0.3f;           // south doorway plane; out = HeadZ < DoorZ - ThroughDoor
    }

    public static class Cards
    {
        public const string AsExpected = "RESULT: AS EXPECTED";
        public const string None = "RESULT: NONE";
        public const string NoneSub = "Participation is voluntary.";
    }
}
