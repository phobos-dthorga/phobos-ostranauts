using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

public static class ProcessRecipes
{
    // Keep the exact revision-1 identity, counts and masses for every old job.
    // Revision 2 is enabled together with its usable reclaimer; revision 1 remains immutable.
    public static readonly ProcessRecipeCatalog WallPanels = new ProcessRecipeCatalog(2, new[] {
        new ProcessRecipe(1, ProcessRules.InputKg, new[] {
            new ProductSpec("ItmPartsMechSmall01", 2, 0.5),
            new ProductSpec("ItmScrapAluminum", 2, 1),
            new ProductSpec("ItmScrapCarbonFiber", 2, 1),
            new ProductSpec("ItmScrapSteel", 6, 1),
            new ProductSpec(ProcessRules.Residue, 1, 13)
        }, legacySeconds: 60),
        new ProcessRecipe(2, ProcessRules.InputKg, new[] {
            new ProductSpec("ItmPartsMechSmall01", 2, .5), new ProductSpec("ItmScrapAluminum", 2, 1),
            new ProductSpec("ItmScrapCarbonFiber", 2, 1), new ProductSpec("ItmScrapSteel", 6, 1),
            new ProductSpec(ReclaimerRules.Feedstock, 1, ReclaimerRules.InputKg)
        })
    });
}
