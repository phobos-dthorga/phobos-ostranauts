using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Story;

/// <summary>One letter or reply as the Letters window shows it.</summary>
public sealed class LetterView
{
    public string Date = "", From = "", Text = "";
    public bool Reply;
}

/// <summary>One reply the player may send, and why it is locked, if it is.</summary>
public sealed class ReplyView
{
    public string Id = "", Label = "";
    public string? Locked;
}

/// <summary>One correspondence (an arc the player has begun) as the Letters window shows it.</summary>
public sealed class CorrespondenceView
{
    public string ArcId = "", Correspondent = "", Title = "";
    public ArcState State;
    /// <summary>The correspondent's face picture name, or null.</summary>
    public string? Face;
    public List<LetterView> Letters = new();
    public string? GoalTitle, GoalDescription;
    public List<ReplyView> Replies = new();
    public double LastEpoch;
}

/// <summary>The Letters window's service (Framework 0.122.0): what each correspondence holds, and the player's replies.
/// The window only reads these and delegates its buttons to <see cref="Answer"/>, as the F3 command does.</summary>
public static partial class StoryArcs
{
    /// <summary>Every correspondence the player has begun, open ones first, newest first within each state.</summary>
    internal static List<CorrespondenceView> Correspondence()
    {
        var views = new List<CorrespondenceView>();
        if (!Ready) return views;
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        var library = StoryContent.Library;
        var facts = new GameFacts(player!);
        foreach (var pair in record.Arcs)
            if (library.Arcs.TryGetValue(pair.Key, out var arc) && View(arc, pair.Value, facts) is CorrespondenceView view) views.Add(view);
        return views.OrderBy(v => v.State == ArcState.Active ? 0 : v.State == ArcState.Done ? 1 : 2).ThenByDescending(v => v.LastEpoch).ThenBy(v => v.ArcId, StringComparer.Ordinal).ToList();
    }

    private static CorrespondenceView? View(StoryEntry<StoryArc> arc, ArcProgress progress, IStoryFacts facts)
    {
        int index = StoryRules.Resolve(arc.Value, progress);
        int shown = index >= 0 ? index : arc.Value.steps.Count - 1;
        var letters = StoryRules.Letters(arc.Value, progress, record.Letters.TryGetValue(arc.Id, out var kept) ? kept : null);
        if (letters.Count == 0 && arc.Value.steps.All(s => s.objective == null)) return null;
        string? place = PlaceOf(arc.Value.thread, arc.Value.place);
        var (person, free) = StoryRules.GoalSender(arc.Value, shown);
        var view = new CorrespondenceView
        {
            ArcId = arc.Id, State = progress.State,
            Correspondent = person != null && StoryContent.Library.PersonName(person, StoryContent.Words) is string name ? name
                : free?.from != null ? Fill(free.from, place) : Text.Get("Story.letters_someone"),
            Face = person != null ? Face(person) : null
        };
        foreach (var letter in letters) Add(view, arc, progress, letter, place);
        view.LastEpoch = letters.Select(l => l.Epoch ?? 0).DefaultIfEmpty(progress.StepStart).Max();
        // The goal the correspondence is at, or the last one it had.
        var goalStep = arc.Value.steps.Take(shown + 1).LastOrDefault(s => s.objective != null);
        if (goalStep?.objective != null)
        {
            view.Title = Fill(StoryContent.Words(arc.Owner, arc.Id + "." + goalStep.id + ".title", goalStep.objective.title), place);
            if (progress.State == ArcState.Active && goalStep == arc.Value.steps[shown])
            {
                view.GoalTitle = view.Title;
                view.GoalDescription = Fill(StoryContent.Words(arc.Owner, arc.Id + "." + goalStep.id + ".description", goalStep.objective.description), place);
            }
        }
        if (view.Title.Length == 0) view.Title = view.Correspondent;
        if (progress.State == ArcState.Active && index >= 0 && arc.Value.steps[index].choices is List<StoryChoice> choices)
            foreach (var choice in choices)
                view.Replies.Add(new ReplyView
                {
                    Id = choice.id,
                    Label = Fill(StoryContent.Words(arc.Owner, ChoiceKey(arc.Id, arc.Value.steps[index].id, choice.id) + ".label", choice.label), place),
                    Locked = StoryRules.ChoiceBlocked(choice, facts, progress.StepStart) is StoryTest t ? Need(t, place) : null
                });
        return view;
    }

    private static string ChoiceKey(string arc, string step, string choice) => arc + "." + step + ".choice." + choice;

    /// <summary>Adds one recorded letter, or a reply and its answer, in the words of the packs now.</summary>
    private static void Add(CorrespondenceView view, StoryEntry<StoryArc> arc, ArcProgress progress, StoryLetter letter, string? place)
    {
        var step = arc.Value.steps.FirstOrDefault(s => s.id == letter.Step);
        if (step == null) return;
        string date = letter.Epoch is double at ? Text.Get("Story.date", MathUtils.GetYearFromS(at), MathUtils.GetMonthFromS(at).ToString("00"), MathUtils.GetDayOfMonthFromS(at).ToString("00")) : "";
        string stepKey = arc.Id + "." + step.id;
        // The variant the crew log showed (Framework 0.132.0), worked out again from the letter's kind and the arc's run.
        void Message(StoryMessage? message, string fromKey, string textKey, string? choice = null)
        {
            if (message == null) return;
            int variant = LetterVariant(arc, progress, step.id, letter.Kind, choice, message.text);
            view.Letters.Add(new LetterView { Date = date, From = Fill(Sender(arc.Owner, fromKey, message), place), Text = Fill(StoryContent.Words(arc.Owner, textKey, message.text, variant), place) });
        }
        switch (letter.Kind)
        {
            case StoryLetter.Opening: Message(step.delivery?.message, stepKey + ".from", stepKey + ".message"); break;
            case StoryLetter.Completion: Message(step.onComplete?.message, stepKey + ".doneFrom", stepKey + ".done"); break;
            case StoryLetter.Reply:
                var choice = step.choices?.FirstOrDefault(c => c.id == letter.Choice);
                if (choice == null) return;
                string key = ChoiceKey(arc.Id, step.id, choice.id);
                view.Letters.Add(new LetterView { Date = date, From = Text.Get("Story.letters_you"), Text = Fill(StoryContent.Words(arc.Owner, key + ".label", choice.label), place), Reply = true });
                Message(choice.onComplete?.message, key + ".doneFrom", key + ".done", choice.id);
                break;
            default:
                if (int.TryParse(letter.Kind.Substring(1), out int b) && step.branches != null && b >= 0 && b < step.branches.Count)
                    Message(step.branches[b].onComplete?.message, stepKey + ".b" + b + ".doneFrom", stepKey + ".b" + b + ".done");
                break;
        }
    }

    /// <summary>What a locked reply still needs, in the player's words.</summary>
    private static string Need(StoryTest test, string? place) => test.kind switch
    {
        StorySchema.DockAt => Text.Get("Story.need_dock", test.station == null || test.station == StorySchema.DockedAnywhere
            ? test.station == null ? StoryContent.Library.Places.Name(place) ?? Text.Get("Story.the_station") : Text.Get("Story.need_any_station")
            : StationLabel(test.station)),
        StorySchema.HaveItem => Text.Get("Story.need_item", test.count, ItemName(test.item!)),
        StorySchema.Install => Text.Get("Story.need_install", test.count, ItemName(test.item!)),
        StorySchema.Wait => Text.Get("Story.need_wait", test.hours.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)),
        StorySchema.Credits => Text.Get("Story.need_credits", test.amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)),
        StorySchema.Condition => Text.Get("Story.need_condition", DataHandler.dictConds != null && DataHandler.dictConds.TryGetValue(test.condition!, out var c) && !string.IsNullOrEmpty(c.strNameFriendly) ? c.strNameFriendly : test.condition!),
        _ => test.kind
    };
    private static string ItemName(string id) => DataHandler.dictCOs != null && DataHandler.dictCOs.TryGetValue(id, out var co) && !string.IsNullOrEmpty(co.strNameFriendly) ? co.strNameFriendly : id;
    private static string StationLabel(string station) => CrewSim.system?.GetShipByRegID(station)?.publicName ?? station;

    /// <summary>Sends the player's reply (Framework 0.122.0): finishes the step with that reply's outcome, as a test or
    /// branch would. Says what it did, or why it refused.</summary>
    internal static string Answer(string arcId, string choiceId)
    {
        if (!Ready) return Text.Get("Story.not_in_game");
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        if (!StoryContent.Library.Arcs.TryGetValue(arcId, out var arc) || !record.Arcs.TryGetValue(arcId, out var progress) || progress.State != ArcState.Active)
            return Text.Get("Story.answer_not_open", arcId);
        int index = StoryRules.Resolve(arc.Value, progress);
        if (index < 0) return Text.Get("Story.answer_not_open", arcId);
        var step = arc.Value.steps[index];
        if (step.choices == null) return Text.Get("Story.answer_no_choice");
        var choice = step.choices.FirstOrDefault(c => c.id == choiceId);
        if (choice == null) return Text.Get("Story.answer_unknown", choiceId, string.Join(", ", step.choices.Select(c => c.id)));
        var facts = new GameFacts(player!);
        string? place = PlaceOf(arc.Value.thread, arc.Value.place);
        if (StoryRules.ChoiceBlocked(choice, facts, progress.StepStart) is StoryTest locked) return Text.Get("Story.answer_locked", Need(locked, place));
        string key = ChoiceKey(arc.Id, step.id, choice.id);
        string label = Fill(StoryContent.Words(arc.Owner, key + ".label", choice.label), place);
        var (person, free) = StoryRules.GoalSender(arc.Value, index);
        string to = person != null && StoryContent.Library.PersonName(person, StoryContent.Words) is string name ? name : free?.from != null ? Fill(free.from, place) : Text.Get("Story.letters_someone");
        Log(null, Text.Get("Story.replied", to, label));
        if (!Finish(arc, progress, index, choice.tests, choice.onComplete, choice.next, key, StoryLetter.Reply, choice.id, facts))
            return Text.Get("Story.answer_failed");
        Save();
        return Text.Get("Story.answered", to, label);
    }

    /// <summary>F3 lines for a choice step: each reply's id and label, and what a locked one needs.</summary>
    private static IEnumerable<string> ChoiceLines(StoryEntry<StoryArc> arc, int index, ArcProgress progress, IStoryFacts facts)
    {
        var step = arc.Value.steps[index];
        string? place = PlaceOf(arc.Value.thread, arc.Value.place);
        foreach (var choice in step.choices ?? new List<StoryChoice>())
            yield return "    " + Text.Get("Story.choice_line", choice.id, Fill(StoryContent.Words(arc.Owner, ChoiceKey(arc.Id, step.id, choice.id) + ".label", choice.label), place),
                StoryRules.ChoiceBlocked(choice, facts, progress.StepStart) is StoryTest t ? Text.Get("Story.choice_locked", Need(t, place)) : Text.Get("Story.choice_open"));
    }
}
