using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Construction;

internal static class ConstructionChecks
{
    internal static void Run(Action<bool,string> check)
    {
        Recipe Recipe() => new Recipe { id = "ExamplePress", name = "Press", stationIds = new[] { "ExampleTable" },
            workSeconds = 60, range = 2, ingredients = new[] { new Ingredient { item = "ExampleMetal", trigger = "TExampleMetal", count = 2, unitMassKg = 1 } },
            outputs = new[] { new Product { item = "ExamplePlate", count = 1, unitMassKg = 2 } } };
        void Invalid(Action<Recipe> edit, string why)
        {
            var recipe = Recipe(); edit(recipe); bool threw = false;
            try { RecipeRules.Validate(recipe); } catch (ArgumentException) { threw = true; }
            check(threw, why);
        }
        RecipeRules.Validate(Recipe());
        Invalid(r => r.outputs[0].unitMassKg = 1.5, "Lost mass must be accounted for as output");
        Invalid(r => r.outputs[0].unitMassKg = 3, "No mass created");
        foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, 0, -1 })
        {
            Invalid(r => r.ingredients[0].unitMassKg = value, "Invalid material mass rejected");
            Invalid(r => r.workSeconds = value, "Invalid work duration rejected");
        }
        Invalid(r => r.ingredients[0].count = 101, "Unbounded material count rejected");
        Invalid(r => r.stationIds = Array.Empty<string>(), "No required work surface rejected");
        Invalid(r => r.optionalStationIds = r.stationIds, "Duplicate required/optional station rejected");
        Invalid(r => r.legacyActionIds = new[] { "PhobosCraft_ExamplePress" }, "Cannot alias another current provider action");
        Invalid(r => r.id = "../Example", "Invalid identifier rejected");
        var recipe = Recipe();
        var input = Enumerable.Repeat(new InputUnit("ExampleMetal", 1, true, false), 2).ToArray();
        check(RecipeRules.MatchesInputs(recipe, input), "Individual units from ordinary stacks are accepted");
        check(!RecipeRules.MatchesInputs(recipe, input.Take(1).ToArray()), "A promised but undelivered input is not enough");
        check(!RecipeRules.MatchesInputs(recipe, input.Append(input[0]).ToArray()), "Extra consumption rejected");
        input[0] = new InputUnit("OtherMetal", 1, true, true);
        check(!RecipeRules.MatchesInputs(recipe, input), "Wrong identity despite same mass rejected");
        input[0] = new InputUnit("ExampleMetal", 0.5, true, true);
        check(!RecipeRules.MatchesInputs(recipe, input), "Changed runtime mass rejected");
        input[0] = new InputUnit("ExampleMetal", 1, false, true);
        check(!RecipeRules.MatchesInputs(recipe, input), "Hidden contents must not be destroyed");
        recipe.ingredients[0].requireEmpty = true;
        input[0] = new InputUnit("ExampleMetal", 1, true, true);
        check(!RecipeRules.MatchesInputs(recipe, input), "Separate assembly pieces cannot be replaced by a stack");
        input[1] = input[0];
        check(RecipeRules.MatchesInputs(recipe, input), "Separate empty pieces accepted");
        var gate = new CompletionGate(); int calls = 0;
        check(!gate.TryBegin(true, () => { calls++; return true; }) && calls == 0 && !gate.Attempted, "Cancelled work does not evaluate or apply effects");
        check(!gate.TryBegin(false, () => false) && !gate.Attempted, "Missing inputs do not mark completion");
        check(gate.TryBegin(false, () => true) && gate.Attempted, "Validated effects may run once");
        check(!gate.TryBegin(false, () => throw new Exception()) && gate.Attempted, "No replay even after an effect failure");
    }
}
