using System;
using HarmonyLib;
using Ostranauts.Events.DTOs;
using Ostranauts.ShipGUIs.ShipBroker;
using UnityEngine.UI;
using PhobosBank.Core;

namespace PhobosBank;

/// <summary>The broker's purchase window (Phobos Banking 0.3.0): with a lender's pre-approval standing here, its lowest
/// down payment follows the lender instead of the broker's 50%, within the approved amount, and the crew log states the
/// terms. Without one, or for a derelict or a special offer, the window is the game's own.</summary>
[HarmonyPatch(typeof(ConfirmBuyShipPopup), nameof(ConfirmBuyShipPopup.ShowPanel), new[] { typeof(ShipPurchaseDTO), typeof(double) })]
internal static class BrokerWindowPatch
{
    private static readonly AccessTools.FieldRef<ConfirmBuyShipPopup, Slider> Slider = AccessTools.FieldRefAccess<ConfirmBuyShipPopup, Slider>("sldrMortgage");

    private static void Postfix(ConfirmBuyShipPopup __instance, ShipPurchaseDTO shipDto)
    {
        try
        {
            if (!Plugin.Ready || shipDto == null || shipDto.TransactionType != TransactionTypes.Mortgage || shipDto.ShipValue <= 0) { Financing.Reset(); return; }
            var broker = __instance.GetComponentInParent<GUIShipBroker>();
            bool realEstate = broker?.COSelf?.strCODef?.IndexOf(BankRules.RealEstateKioskMark, StringComparison.Ordinal) >= 0;
            var offer = Financing.Open(shipDto.RegId, shipDto.ShipValue, shipDto.IsSpecialOffer, realEstate, out var note);
            if (offer != null)
            {
                var slider = Slider(__instance);
                slider.minValue = (float)offer.MinShare;
                // Setting the value runs the window's own update of the price and the payment line; a value already
                // above the new minimum stays, and the player can now slide lower.
                if (slider.value < slider.minValue) slider.value = slider.minValue;
            }
            if (note != null && CrewSim.coPlayer != null) CrewSim.coPlayer.LogMessage(note, offer != null ? "Neutral" : "Bad", CrewSim.coPlayer.strID);
        }
        catch (Exception ex) { Financing.Reset(); Plugin.Log(Text.Get("Financing.failed", ex.ToString())); }
    }
}

/// <summary>The broker wrote its mortgage for a purchase: a financed one passes to the lender.</summary>
[HarmonyPatch(typeof(GUIShipBroker), "UpdateCash", new[] { typeof(ShipPurchaseDTO) })]
internal static class BrokerPurchasePatch
{
    private static readonly AccessTools.FieldRef<GUIShipBroker, CondOwner> User = AccessTools.FieldRefAccess<GUIShipBroker, CondOwner>("_coUser");

    private static void Postfix(GUIShipBroker __instance, ShipPurchaseDTO shipDto)
    {
        try
        {
            if (!Plugin.Ready || shipDto == null || shipDto.TransactionType != TransactionTypes.Mortgage) return;
            var user = User(__instance);
            if (user == null || __instance.COSelf == null) return;
            Financing.Purchased(shipDto.RegId, user.strID, __instance.COSelf.FriendlyName, DataHandler.GetString("GUI_FINANCE_MORTGAGE01") + shipDto.RegId, out var message);
            if (message != null) user.LogMessage(message, "Neutral", user.strID);
        }
        catch (Exception ex) { Financing.Reset(); Plugin.Log(Text.Get("Financing.failed", ex.ToString())); }
    }
}
