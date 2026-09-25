"""Reproducible DESIGN calculations, not a runtime furnace or native energy adapter.

All equipment parameters are authored candidates. Aluminium uses a documented
piecewise surrogate (see docs/furnace-first-cycle.md), not an alloy assay.
The time integration uses one equilibrated charge/lining node and a finite sink.
"""
import argparse
import json
import math
from pathlib import Path

T0 = 298.15
TM = 933.45
TARGET = 973.15
RELEASE = 323.15
MASS = 20.0
SOLID_CP = 1.05                 # kJ/(kg K), representative effective average
LIQUID_CP = 1.177
LATENT = 397.0                  # kJ/kg, rounded design surrogate
LINING_CAPACITY = 30.0          # kJ/K: 30 kg effective lining at 1 kJ/(kg K)
SINK_CAPACITY = 80.0            # kJ/K: effective cooling assembly, 100 kg
SINK_MAX = 523.15
AREA = 12.0                    # effective emitting m2, includes view factor
EMISSIVITY = 0.85
SIGMA = 5.670374419e-11          # kW/(m2 K4)
BACKGROUND = 200.0             # bounded cold-sky equivalent, not a solar model
COUPLING = 0.90
HOLD_SECONDS = 60.0


def sensible_capacity(mass=MASS):
    return mass * SOLID_CP + LINING_CAPACITY


def enthalpy(temp, fraction=0.0, mass=MASS):
    sensible = sensible_capacity(mass)
    if temp < TM:
        return sensible * (temp - T0)
    base = sensible * (TM - T0)
    if temp == TM:
        return base + mass * LATENT * fraction
    return base + mass * LATENT + (mass * LIQUID_CP + LINING_CAPACITY) * (temp - TM)


def temperature(energy, mass=MASS):
    solid = enthalpy(TM, 0, mass)
    liquid = enthalpy(TM, 1, mass)
    if energy < solid:
        return T0 + energy / sensible_capacity(mass), 0.0
    if energy <= liquid:
        return TM, (energy - solid) / (mass * LATENT)
    return TM + (energy - liquid) / (mass * LIQUID_CP + LINING_CAPACITY), 1.0


def radiation(temp, area=AREA):
    # Signed exchange: a colder surface absorbs background heat, not zero heat.
    return area * EMISSIVITY * SIGMA * (temp**4 - BACKGROUND**4)


def cycle(power=250.0, warm_lining=T0, warm_sink=T0, area=AREA, dt=0.25, cut_at=None, restart_at=None):
    hot = LINING_CAPACITY * (warm_lining - T0)  # cold charge mixes without deleting lining heat
    sink = SINK_CAPACITY * (warm_sink - T0)
    initial = hot + sink
    source = aux = rejected = room = 0.0
    hold = 0.0
    phase = 'HEAT'
    peak_sink = T0
    elapsed = 0.0
    heat_end = None
    samples = []
    goal = enthalpy(TARGET)
    step_count = math.ceil(6 * 3600 / dt)
    for step in range(step_count):
        temp, fraction = temperature(hot)
        ts = T0 + sink / SINK_CAPACITY
        peak_sink = max(peak_sink, ts)
        supply = power if cut_at is None or elapsed < cut_at or (restart_at is not None and elapsed >= restart_at) else 0.0
        leak = max(0.0, 0.001 * (temp - T0))  # 1 W/K room transfer, finite accepting room assumed
        # Fully mixed hot node; no claimed finite internal charge/lining conductance.
        cool = max(0.0, min(100.0, 0.30 * (temp - ts))) if phase == 'COOL' else 0.0
        cool = min(cool, max(0.0, (hot - enthalpy(ts)) / dt))
        need = max(0.0, (goal - hot) / dt + leak)
        heat = min(supply, need, 2.0 * sensible_capacity() if temp < TM - 0.1 else supply) if phase != 'COOL' else 0.0
        auxiliary = 2.0 if phase != 'COOL' else 1.0  # delivered electricity -> sink heat
        radiate = radiation(ts, area)
        auxiliary = min(auxiliary, max(0.0, (SINK_CAPACITY * (SINK_MAX - T0) - sink) / dt + radiate))
        # Reserve headroom BEFORE accepting coupling losses or hot-node transfer.
        headroom = max(0.0, (SINK_CAPACITY * (SINK_MAX - T0) - sink) / dt + radiate - auxiliary)
        cool = min(cool, headroom)
        headroom -= cool
        heat = min(heat, headroom * COUPLING / (1.0 - COUPLING))
        debit = heat / COUPLING
        loss = debit - heat
        hot += (heat - cool - leak) * dt
        sink += (cool + loss + auxiliary - radiate) * dt
        source += debit * dt
        aux += auxiliary * dt
        rejected += radiate * dt
        room += leak * dt
        elapsed += dt
        temp, fraction = temperature(hot)
        if phase != 'COOL':
            if TARGET - 0.5 <= temp <= TARGET + 0.5:
                phase = 'HOLD'
                hold += dt
                if hold >= HOLD_SECONDS:
                    phase = 'COOL'
                    heat_end = elapsed
            else:
                # Continuous in-band hold; interruptions reset qualifying time.
                hold = 0.0
                phase = 'HEAT'
        if step % max(1, round(60 / dt)) == 0:
            samples.append([round(elapsed, 2), round(temp - 273.15, 2), round(T0 + sink / SINK_CAPACITY - 273.15, 2), phase])
        residual = initial + source + aux - hot - sink - rejected - room
        if abs(residual) > 1e-5 or hot < -1e-5 or T0 + sink / SINK_CAPACITY > SINK_MAX + 1e-6:
            raise AssertionError('Energy budget or sink limit violated')
        if phase == 'COOL' and temp <= RELEASE:
            break
    return dict(completed=phase == 'COOL' and temp <= RELEASE,
                seconds=round(elapsed, 2), heating_and_hold_seconds=heat_end,
                source_MJ=round(source/1000, 6), auxiliary_MJ=round(aux/1000, 6),
                radiated_MJ=round(rejected/1000, 6), room_MJ=round(room/1000, 6),
                retained_hot_MJ=round(hot/1000, 6), retained_sink_MJ=round(sink/1000, 6),
                initial_MJ=round(initial/1000, 6), balance_error_kJ=residual,
                peak_sink_C=round(peak_sink - 273.15, 2), samples=samples)


def gas_budget():
    # p in kPa, V in m3, ideal-gas R in kPa m3/(mol K).
    r = 0.008314462618
    chamber, receiver, pressure, target = 0.08, 0.05, 100.0, 0.1
    total = pressure * chamber / (r * T0)
    remaining = target * chamber / (r * T0)
    moved = total - remaining
    return dict(initial_mol=total, retained_mol=remaining, receiver_mol=moved,
                receiver_kPa=moved*r*T0/receiver,
                receiver_hot_kPa=moved*r*333.15/receiver,
                hot_chamber_kPa=target*TARGET/T0,
                conservative_evacuation_seconds=math.ceil(moved/0.05),
                residual_mol=total-remaining-moved)


def check():
    for t, f in [(T0,0),(600,0),(TM,0),(TM,0.5),(TM,1),(TARGET,1)]:
        restored, fraction = temperature(enthalpy(t,f))
        assert abs(restored-t) < 1e-9 and abs(fraction-f) < 1e-9
    assert enthalpy(TM,1)-enthalpy(TM,0) == MASS*LATENT
    assert abs(gas_budget()['residual_mol']) < 1e-12
    assert 2 * gas_budget()['receiver_kPa'] > 200  # uncleared receiver cannot take another batch
    assert 20 == 19 + 1 and 19 == 18 + 1
    a = cycle()
    b = cycle(power=125)
    c = cycle(warm_lining=373.15)
    d = cycle(cut_at=100,restart_at=280)
    # Previous casting removed at 50 C; only lining and sink energy remain.
    repeat = cycle(warm_lining=RELEASE, warm_sink=T0+a['retained_sink_MJ']*1000/SINK_CAPACITY)
    assert all(x['completed'] for x in (a,b,c,d,repeat))
    assert abs(repeat['initial_MJ']-(LINING_CAPACITY*(RELEASE-T0)/1000+a['retained_sink_MJ'])) < 1e-6
    assert b['heating_and_hold_seconds'] > a['heating_and_hold_seconds']
    assert c['source_MJ'] < a['source_MJ']
    assert d['heating_and_hold_seconds'] > a['heating_and_hold_seconds']
    fine = cycle(dt=0.125)
    assert abs(a['seconds']-fine['seconds']) < 2
    assert not cycle(area=0)['completed']  # cannot complete within the six-hour horizon
    return {"cold_250kW": a, "partial_125kW": b, "warm_lining_100C": c, "next_batch_after_50C_release": repeat,
            "source_interruption_100_to_280s": d, "half_radiator": cycle(area=AREA/2)}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--output', type=Path)
    args = p.parse_args()
    scenarios = check()
    result = {'status':'Authored offline design estimates; no native energy source simulated',
              'mass_kg': MASS, 'cold_hot_node_MJ': enthalpy(TARGET)/1000,
              'gas':gas_budget(), 'scenarios':scenarios,
              'radiator_kW_at_C':{str(t):radiation(t+273.15) for t in (25,50,100,150,200,250)}}
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:{f:v for f,v in val.items() if f != 'samples'} for k,val in scenarios.items()},indent=2))
    print('Conservation, phase change, interruption, partial supply, timestep and no-radiator checks passed.')


if __name__ == '__main__':
    main()
