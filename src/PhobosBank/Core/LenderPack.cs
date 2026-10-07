using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Story;

namespace PhobosBank.Core;

/// <summary>The <c>lenders</c> schema (Phobos Banking 0.2.0; owner direction, 7 October 2026: lenders are data players
/// and add-ons can add to). Who lends, where, to whom and on what terms. The rules that act on them (interest, limits,
/// the ledger lines) stay in code. A lender's terms are copied into each loan when it is taken, so a later change to
/// the pack never changes a loan already running.</summary>
public sealed class LenderPack : DataPack
{
    /// <summary>Lenders by id: lowercase words joined by dashes. Story flags use the id.</summary>
    public Dictionary<string, LenderEntry> lenders = new(StringComparer.Ordinal);
    /// <summary>System-wide credit lines by id (Phobos Banking 0.6.0): opened and drawn on from the PDA anywhere, one
    /// revolving balance each. Ids share the lenders' namespace, because story arcs and flags are named by them.</summary>
    public Dictionary<string, CreditLineEntry> creditLines = new(StringComparer.Ordinal);
}

/// <summary>A credit line (Phobos Banking 0.6.0; owner choices, 7 October 2026: a system-wide service, opened from
/// anywhere, one revolving balance, a draw fee plus a higher rate). The balance is one of the game's own mortgage lines:
/// its shift instalment is the minimum payment, the Finances window's Prepay pays more, and each draw adds to it and
/// spreads it over a fresh term, as the game's own Prepay does.</summary>
public sealed class CreditLineEntry
{
    public string? notes;
    public string name = "";
    public string pitch = "";
    /// <summary>The story person who speaks for the service; optional.</summary>
    public string? person;
    /// <summary>Who may open the line: a story requirement block; none means anyone.</summary>
    public StoryRequires? requires;
    /// <summary>The most that may be owed on the line, fee included, in credits.</summary>
    public double limit;
    /// <summary>Interest per shift on the balance, as a share.</summary>
    public double ratePerShift;
    /// <summary>The fee on each draw, as a share of the amount drawn, added to the balance.</summary>
    public double drawFee;
    /// <summary>The smallest draw, in credits.</summary>
    public double minDraw = 500;
}

public sealed class LenderEntry
{
    public string? notes;
    /// <summary>The name shown in the app and on the ledger (the creditor).</summary>
    public string name = "";
    /// <summary>The lender's own words to a customer, in their sales voice.</summary>
    public string pitch = "";
    /// <summary>A registered lender (lower rates, asks for standing) or a quick-money one.</summary>
    public bool accredited = true;
    /// <summary>The story place the lender trades from: a regional place lends anywhere in its region, a part of one
    /// only to a player docked there.</summary>
    public string home = "";
    /// <summary>The story person who speaks for the lender, for letters and goals; optional.</summary>
    public string? person;
    /// <summary>Who may borrow: the story requirement block, checked as story content is.</summary>
    public StoryRequires? requires;
    /// <summary>Interest per shift on the balance still owed, as a share (0.0003 is 0.03%).</summary>
    public double ratePerShift;
    /// <summary>The smallest loan, and the most the player may owe this lender at once, in credits.</summary>
    public double minPrincipal, maxPrincipal;
    /// <summary>How many loans from this lender may run at once.</summary>
    public int maxLoans = 1;
    /// <summary>What it lends for: <c>cash</c> (paid into the player's account), <c>ship</c> (a ship broker purchase),
    /// <c>home</c> (an apartment from a real-estate broker). At least one; there is no default, because the file
    /// reader adds a file's list to a default one rather than replacing it.</summary>
    public List<string> offers = new();
    /// <summary>For ship and home loans: the least the player pays down, as a share of the price.</summary>
    public double minDownShare = 0.5;
}

public static class LenderSchema
{
    public const string Name = "lenders";
    public const string Cash = "cash", Ship = "ship", Home = "home";
    public static readonly IReadOnlyList<string> Offers = new[] { Cash, Ship, Home };
    /// <summary>What a loan in the book is for: a lender's three, and a credit line's balance (0.6.0).</summary>
    public const string Line = "line";
    public static readonly IReadOnlyList<string> LoanKinds = new[] { Cash, Ship, Home, Line };
    /// <summary>Lender ids stay short enough for the story flags built from them (<c>bank-&lt;id&gt;-borrowed</c>); credit
    /// line ids one shorter, for <c>bank-&lt;id&gt;-line-opened</c>.</summary>
    public const int MaxIdLength = 32, MaxLineIdLength = 31, MaxName = 40, MaxPitch = 400, MaxLoans = 5;
    public const double MinLineLimit = 1000, MaxLineLimit = 1000000, MaxDrawFee = 0.2, MinDraw = 100;
    public const double MaxRatePerShift = 0.01, MinPrincipal = 1000, MaxPrincipal = 5000000, MinDownShare = 0.1;

    /// <summary>The checks every file passes, shipped or player. Whether the home, person and requirement entries exist
    /// is checked against the story library once it is built (<see cref="Unknown"/>).</summary>
    public static void Validate(LenderPack pack)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (pack.lenders == null) throw new ArgumentException("lenders: expected id to lender");
        foreach (var pair in pack.lenders)
        {
            string where = "lenders." + pair.Key;
            var l = pair.Value ?? throw new ArgumentException(where + ": empty");
            if (!StorySchema.IsId(pair.Key, MaxIdLength)) throw new ArgumentException(where + ": an id is lowercase letters and digits joined by dashes, at most " + MaxIdLength);
            if (l.notes != null && l.notes.Length > 2000) throw new ArgumentException(where + ".notes: at most 2000 characters");
            if (!LedgerSafe(l.name) || l.name.Length > MaxName) throw new ArgumentException(where + ".name: 1 to " + MaxName + " characters, without | , = # [ ] < > or line breaks");
            StorySchema.Words(l.pitch, MaxPitch, where + ".pitch");
            if (l.pitch.IndexOf('[') >= 0) throw new ArgumentException(where + ".pitch: no placeholders");
            if (!StorySchema.IsId(l.home)) throw new ArgumentException(where + ".home: a story place key");
            if (l.person != null && !StorySchema.IsId(l.person)) throw new ArgumentException(where + ".person: a story person key");
            StorySchema.ValidateRequires(l.requires, where);
            if (!Finite(l.ratePerShift) || l.ratePerShift <= 0 || l.ratePerShift > MaxRatePerShift) throw new ArgumentException(where + ".ratePerShift: above 0 and at most " + MaxRatePerShift);
            if (!Finite(l.minPrincipal) || !Finite(l.maxPrincipal) || l.minPrincipal < MinPrincipal || l.maxPrincipal > MaxPrincipal || l.minPrincipal >= l.maxPrincipal)
                throw new ArgumentException(where + ": minPrincipal from " + MinPrincipal + ", below maxPrincipal, at most " + MaxPrincipal);
            if (l.maxLoans < 1 || l.maxLoans > MaxLoans) throw new ArgumentException(where + ".maxLoans: 1 to " + MaxLoans);
            if (l.offers == null || l.offers.Count == 0 || l.offers.Any(o => !Offers.Contains(o)) || l.offers.Distinct().Count() != l.offers.Count)
                throw new ArgumentException(where + ".offers: one or more of " + string.Join(", ", Offers) + ", each once");
            if (!Finite(l.minDownShare) || l.minDownShare < MinDownShare || l.minDownShare > 1) throw new ArgumentException(where + ".minDownShare: from " + MinDownShare + " to 1");
        }
        if (pack.creditLines == null) throw new ArgumentException("creditLines: expected id to credit line");
        foreach (var pair in pack.creditLines)
        {
            string where = "creditLines." + pair.Key;
            var c = pair.Value ?? throw new ArgumentException(where + ": empty");
            if (!StorySchema.IsId(pair.Key, MaxLineIdLength)) throw new ArgumentException(where + ": an id is lowercase letters and digits joined by dashes, at most " + MaxLineIdLength);
            if (pack.lenders.ContainsKey(pair.Key)) throw new ArgumentException(where + ": a lender already has this id; story arcs and flags are named by it");
            if (c.notes != null && c.notes.Length > 2000) throw new ArgumentException(where + ".notes: at most 2000 characters");
            if (!LedgerSafe(c.name) || c.name.Length > MaxName) throw new ArgumentException(where + ".name: 1 to " + MaxName + " characters, without | , = # [ ] < > or line breaks");
            StorySchema.Words(c.pitch, MaxPitch, where + ".pitch");
            if (c.pitch.IndexOf('[') >= 0) throw new ArgumentException(where + ".pitch: no placeholders");
            if (c.person != null && !StorySchema.IsId(c.person)) throw new ArgumentException(where + ".person: a story person key");
            StorySchema.ValidateRequires(c.requires, where);
            if (!Finite(c.limit) || c.limit < MinLineLimit || c.limit > MaxLineLimit) throw new ArgumentException(where + ".limit: from " + MinLineLimit + " to " + MaxLineLimit);
            if (!Finite(c.ratePerShift) || c.ratePerShift <= 0 || c.ratePerShift > MaxRatePerShift) throw new ArgumentException(where + ".ratePerShift: above 0 and at most " + MaxRatePerShift);
            if (!Finite(c.drawFee) || c.drawFee < 0 || c.drawFee > MaxDrawFee) throw new ArgumentException(where + ".drawFee: from 0 to " + MaxDrawFee);
            if (!Finite(c.minDraw) || c.minDraw < MinDraw || c.minDraw * (1 + c.drawFee) > c.limit) throw new ArgumentException(where + ".minDraw: at least " + MinDraw + ", and with its fee within the limit");
        }
    }

    /// <summary>The first story entry a credit line names that the library does not know, or null.</summary>
    public static string? Unknown(CreditLineEntry line, StoryLibrary library) =>
        library.UnknownPerson(line.person) ?? library.UnknownReference(line.requires);

    /// <summary>The first story entry a lender names that the library does not know, or null.</summary>
    public static string? Unknown(LenderEntry lender, StoryLibrary library) =>
        library.UnknownPlace(lender.home) ?? library.UnknownPerson(lender.person) ?? library.UnknownReference(lender.requires);

    /// <summary>Text that can sit in a ledger line and in the saved loan book: no separators the save store or our
    /// encoding use, no rich-text or reference marks.</summary>
    public static bool LedgerSafe(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text!.IndexOfAny(new[] { '|', ',', '=', '#', '[', ']', '<', '>' }) < 0 && !text.Any(char.IsControl);

    internal static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
}
