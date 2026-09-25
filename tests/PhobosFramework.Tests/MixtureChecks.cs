using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Liquids;

internal static class MixtureChecks
{
    internal static void Run(Action<bool,string> check)
    {
        void Near(double a, double b, string message) => check(Math.Abs(a-b) < 1e-8, message);
        var source = new Tank("a", new(10, .1)); var target = new Tank("b", default);
        MixtureReceipt Move(double kg) => LiquidTransferGuard.Commit(source,target,kg,source.Guard,target.Guard);
        var receipt = Move(2.02);
        Near(receipt.Received.CarrierKg,2,"Carrier transferred proportionally"); Near(receipt.Received.SoluteKg,.02,"Nutrients transferred proportionally");
        check(receipt.Reconciled,"Components reconcile independently");
        target.Cap = new(20,.025); receipt = Move(20);
        Near(receipt.Received.CarrierKg,.5,"Solute capacity bounds water too"); Near(target.Quantity.SoluteKg,.025,"No excess nutrient discarded");
        Near(Move(10).Received.TotalKg,0,"Full nutrient compartment consumes no source water");
        source = new("a",new(10,.1)); target = new("b", default) { TotalCap = 1.01 };
        Near(Move(20).Received.TotalKg,1.01,"Combined liquid mass capacity applies");
        source = new("a",new(10,.1)); target = new("b",default) { Accept = .25 };
        receipt = Move(4.04); Near(receipt.Received.TotalKg,1.01,"Uniform partial acceptance refunded proportionally");
        Near(source.Quantity.CarrierKg + target.Quantity.CarrierKg,10,"Partial receipt conserves carrier");
        Near(source.Quantity.SoluteKg + target.Quantity.SoluteKg,.1,"Partial receipt conserves solute");
        target.FailAfter = true; bool threw = false; try { Move(1); } catch { threw = true; }
        check(threw && source.Guard.Protected && target.Guard.Protected,"Interrupted mixture write protects both journals");
        var saved = source.Quantity; try { Move(1); } catch { }
        check(MixtureTransfer.Same(saved,source.Quantity),"Reloaded mixture journal prevents retry");
        source = new("a",new(10,.1)); target = new("b",default) { Corrupt = true };
        threw = false; try { Move(1); } catch { threw = true; }
        check(threw && target.Guard.Protected,"Equal total mass with wrong composition is rejected");
        foreach (bool foreignShip in new[]{true,false})
        {
            source = new("a",new(10,.1)); target = new("b",default);
            if (foreignShip) target.Ship = "neighbour"; else target.Kind = "other-formula";
            threw = false; try { Move(1); } catch { threw = true; }
            check(threw && !source.Guard.Protected && !target.Guard.Protected,"Scope/profile rejects before any debit or journal");
        }
        for(int n=1;n<=100;n++)
        {
            double carrier=n*.137, solute=n*.0013;
            source=new("a",new(carrier,solute)); target=new("b",default) { Accept=(n%9+1)/10d };
            receipt=Move(n*.073);
            Near(source.Quantity.CarrierKg+target.Quantity.CarrierKg,carrier,"Varying-size transfer conserves carrier");
            Near(source.Quantity.SoluteKg+target.Quantity.SoluteKg,solute,"Varying-size transfer conserves nutrient");
        }
        source=new("",new(1,.01)); target=new("b",default);
        threw=false; try { Move(1); } catch { threw=true; }
        check(threw && !source.Guard.Protected && !target.Guard.Protected,"Missing full object identity rejects before journaling");
    }
    private sealed class Tank : IMixtureReservoir
    {
        private readonly Dictionary<string,Dictionary<string,string>> maps=new();
        public LiquidTransferGuard Guard => new(maps,"mixture","test"); // New wrapper simulates reload.
        public Tank(string id,LiquidMixture q) { Identity=id; Quantity=q; }
        public string Identity {get;}
        public string Ship="ship",Kind="test-formula";
        public string ShipId=>Ship; public string Profile=>Kind;
        public LiquidMixture Quantity {get;private set;}
        public LiquidMixture Cap=new(20,1); public double TotalCap=20,Accept=1;
        public bool FailAfter,Corrupt;
        public LiquidMixture ComponentCapacity=>Cap; public double TotalCapacityKg=>TotalCap;
        public void SetQuantity(LiquidMixture q)
        {
            var delta=q-Quantity;
            Quantity=delta.TotalKg>0 ? Quantity+delta.Scale(Accept) : q;
            if(Corrupt) Quantity=new(Quantity.CarrierKg+Quantity.SoluteKg,0);
            if(FailAfter) throw new InvalidOperationException("Interrupted after both component writes");
        }
    }
}
