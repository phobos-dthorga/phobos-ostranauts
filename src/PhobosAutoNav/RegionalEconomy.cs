using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosAutoNav;

/// <summary>Regional availability across the vanilla solar system, from the economy data pack's region factors and
/// regional section. Native supply/demand, condition, merchant margins and negotiation still set prices.</summary>
internal static class RegionalEconomy
{
    internal static (string Region, double Factor)[] Profiles => AutoNavEconomy.Pack.regions.Select(p => (p.Key, p.Value)).ToArray();

    internal static void Apply(NativeDefinitions d) => EconomyStock.ApplyRegional(d, AutoNavEconomy.Pack, AutoNavEconomy.OwnerTag, AutoNavEconomy.Sales);
}
