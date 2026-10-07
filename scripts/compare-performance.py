#!/usr/bin/env python3
"""Compare opt-in Scope captures without treating inclusive timings as CPU percentages."""
import argparse
import json
import math
import statistics
from pathlib import Path


# Framework 0.104.0 frame-time buckets (FrameMeasurements.Buckets): name and upper bound in ms, the last open-ended.
FRAME_BUCKETS = [('game.frame.upto_8_3ms', 1000 / 120), ('game.frame.upto_11_1ms', 1000 / 90), ('game.frame.upto_16_7ms', 1000 / 60),
                 ('game.frame.upto_20ms', 20), ('game.frame.upto_25ms', 25), ('game.frame.upto_33_3ms', 1000 / 30),
                 ('game.frame.upto_50ms', 50), ('game.frame.upto_66_7ms', 1000 / 15), ('game.frame.upto_100ms', 100),
                 ('game.frame.upto_200ms', 200), ('game.frame.upto_500ms', 500), ('game.frame.over_500ms', math.inf)]


def percentile(values, fraction):
    return sorted(values)[max(0, math.ceil(len(values) * fraction) - 1)] if values else None


def bucket_percentile(counts, fraction, largest):
    """The upper bound of the bucket holding the rank, capped by the largest frame: a bound, never an interpolation."""
    total = sum(counts)
    if not total:
        return None
    rank, seen = max(1, math.ceil(total * fraction)), 0
    for (_, upper), count in zip(FRAME_BUCKETS, counts):
        seen += count
        if seen >= rank:
            if largest is not None:
                return min(upper, largest)
            return None if math.isinf(upper) else upper
    return largest


def frames_from_samples(frames):
    return {'basis': 'samples', 'p50_ms': percentile(frames, .5), 'p95_ms': percentile(frames, .95),
            'p99_ms': percentile(frames, .99), 'max_ms': max(frames) if frames else None,
            'mean_ms': sum(frames) / len(frames) if frames else None,
            'over_33_3_ms': sum(v > 1000 / 30 for v in frames),
            'over_50_ms': sum(v > 50 for v in frames), 'over_100_ms': sum(v > 100 for v in frames)}


def frames_from_totals(totals):
    """Format 2: the frame interval's complete total and the bucket counts; percentiles are bucket upper bounds."""
    interval = totals.get('game.frame.interval')
    samples = interval['samples'] if interval else 0
    largest = interval['max'] if samples else None
    have_buckets = all(name in totals for name, _ in FRAME_BUCKETS)
    counts = [totals[name]['sum'] if name in totals else 0 for name, _ in FRAME_BUCKETS]

    def above(limit):
        if not have_buckets:
            return None
        return sum(c for (_, upper), c in zip(FRAME_BUCKETS, counts) if upper > limit + 1e-9)

    def rank(fraction):
        return bucket_percentile(counts, fraction, largest) if have_buckets else None

    return samples, {'basis': 'bucket_upper_bound', 'p50_ms': rank(.5), 'p95_ms': rank(.95), 'p99_ms': rank(.99),
                     'max_ms': largest, 'mean_ms': interval['sum'] / samples if samples else None,
                     'over_33_3_ms': above(1000 / 30), 'over_50_ms': above(50), 'over_100_ms': above(100)}


def analyse(data):
    version = data.get('format_version')
    if version not in (1, 2, 3):
        raise ValueError('Unsupported Scope capture format')
    definitions = {d['id']: d for d in data['definitions']}
    frequency = data['clock_frequency_hz']
    if not math.isfinite(frequency) or frequency <= 0:
        raise ValueError('Capture must have a positive clock and duration')
    seconds = data['end_tick'] / frequency
    if not math.isfinite(seconds) or seconds <= 0:
        raise ValueError('Capture must have a positive clock and duration')
    frames = [s['value'] for s in data['counters']
              if definitions[s['metric']]['name'] == 'game.frame.interval']
    if any(not math.isfinite(v) or v <= 0 for v in frames):
        raise ValueError('Frame intervals must be finite and positive')
    # Format 2 (recorder 0.2.0) keeps one complete total per counter; summary captures keep no samples at all.
    kinds = {d['name']: d['kind'] for d in definitions.values()}
    totals = {definitions[a['metric']]['name']: a for a in data.get('counter_aggregates', [])}
    if version >= 2:
        frame_samples, frame_report = frames_from_totals(totals)
    else:
        frame_samples, frame_report = len(frames), frames_from_samples(frames)
    complete = data['dropped_records'] == 0 and data['rejected_measurements'] == 0
    operations = {}
    for row in data['aggregates']:
        name = definitions[row['metric']]['name']
        operations[name] = {
            'calls': row['calls'], 'calls_per_second': row['calls'] / seconds,
            'inclusive_ms_per_second': row['total_ticks'] / frequency * 1000 / seconds,
            'max_ms': row['max_ticks'] / frequency * 1000, 'incomplete': row['incomplete'],
            # Format 3 (recorder 0.3.0): time outside the operation's own measured children; these do not overlap.
            'self_ms_per_second': row['self_ticks'] / frequency * 1000 / seconds if 'self_ticks' in row else None}
    increments = {}
    if version >= 2:
        increments = {name: t['sum'] for name, t in totals.items() if kinds[name] == 'increment'}
    else:
        for sample in data['counters']:
            definition = definitions[sample['metric']]
            if definition['kind'] == 'increment':
                increments[definition['name']] = increments.get(definition['name'], 0) + sample['value']
    # Levels (memory and each mod's footprint, Framework 0.104.0): the range and last reading of every gauge.
    levels = {name: {'samples': t['samples'], 'min': t['min'], 'max': t['max'], 'last': t['last'], 'sum': t['sum']}
              for name, t in totals.items() if t['samples'] and kinds[name] == 'gauge' and name != 'game.frame.interval'}
    contexts = {}
    for sample in data['contexts']:
        contexts.setdefault(definitions[sample['metric']]['name'], set()).add(sample['value'])
    metadata = {r['key']: r['value'] for r in data['metadata']}
    # Early Mono captures exposed a stub that returned zero. Only calibrated
    # metadata plus a continuously available reader establish support.
    allocation_supported = metadata.get('allocation_measurement') == 'main_thread_bytes' and contexts.get('game.allocations.available') == {'true'}
    if not allocation_supported:
        increments.pop('game.allocations.main_thread', None)
    return {
        'capture_id': data['capture_id'], 'seconds': seconds, 'mode': data['mode'],
        'metadata': metadata, 'allocation_supported': allocation_supported,
        'contexts': {k: sorted(v) for k, v in contexts.items()},
        'quality': {'complete_records': complete, 'dropped': data['dropped_records'],
                    'rejected': data['rejected_measurements'], 'frame_samples': frame_samples},
        'frames': frame_report, 'levels': levels,
        'increments': increments, 'operations': operations}


def compare(before, after):
    warnings = []
    runs = before + after
    if len(before) != 3 or len(after) != 3:
        warnings.append('Acceptance requires three matched runs on each side.')
    if any(not r['quality']['complete_records'] or r['quality']['frame_samples'] == 0 for r in runs):
        warnings.append('Missing or dropped samples: frame acceptance is unverified; retained percentiles are descriptive only.')
    if len({r['frames']['basis'] for r in runs}) > 1:
        warnings.append('Frame percentiles mix exact samples (format 1) with bucket upper bounds (format 2); compare long-frame counts instead.')
    if any(not 29 <= r['seconds'] <= 31 for r in runs):
        warnings.append('Acceptance comparisons require 30-second captures.')
    for key in ('game.speed_multiplier', 'game.paused', 'game.navigation_console_visible'):
        values = [r['contexts'].get(key, []) for r in runs]
        if not values or any(len(v) != 1 or v != values[0] for v in values):
            warnings.append('Capture conditions differ or changed: ' + key)
    incomplete_operations = sorted({name for r in runs for name, op in r['operations'].items() if op['incomplete']})
    def change(old, new):
        a, b = statistics.median(old), statistics.median(new)
        return {'before_median': a, 'after_median': b,
                'change_percent': (b - a) / a * 100 if a else None,
                'before_range': [min(old), max(old)], 'after_range': [min(new), max(new)]}
    frame_changes = {}
    for key in ('p50_ms', 'p95_ms', 'p99_ms', 'mean_ms', 'max_ms', 'over_50_ms', 'over_100_ms'):
        old, new = [r['frames'][key] for r in before], [r['frames'][key] for r in after]
        if old and new and None not in old + new:
            frame_changes[key] = change(old, new)
    names = set.intersection(*(set(r['operations']) for r in runs)) if before and after else set()
    operation_changes = {name: change([r['operations'][name]['inclusive_ms_per_second'] for r in before],
                                      [r['operations'][name]['inclusive_ms_per_second'] for r in after]) for name in sorted(names)}
    self_changes = {name: change([r['operations'][name]['self_ms_per_second'] for r in before],
                                 [r['operations'][name]['self_ms_per_second'] for r in after])
                    for name in sorted(names)
                    if all(r['operations'][name]['self_ms_per_second'] is not None for r in runs)}
    panel = operation_changes.get('autonav.panel.refresh', {}).get('change_percent')
    level_names = set.intersection(*(set(r['levels']) for r in runs)) if before and after else set()
    level_changes = {name: change([r['levels'][name]['max'] for r in before], [r['levels'][name]['max'] for r in after])
                     for name in sorted(level_names)}
    return {'warnings': warnings, 'before': before, 'after': after, 'frame_changes': frame_changes,
            'level_max_changes': level_changes,
            'operation_inclusive_ms_per_second': operation_changes,
            'operation_self_ms_per_second': self_changes,
            'incomplete_operations': incomplete_operations,
            'panel_50_percent_target': None if warnings or panel is None or 'autonav.panel.refresh' in incomplete_operations else panel <= -50,
            'interpretation': 'Timings are inclusive wall time. Matching saves, workloads, graphics and recorder overhead require owner notes; this report does not establish exclusive CPU cost or gameplay correctness.'}


# Framework 0.133.0: who an operation belongs to, by the prefix of its stable name. game.* are the capture probes'
# timings of the game's own methods, with every mod's patches on them inside, so they are kept apart from the mods.
OWNERS = [('framework.', 'Framework'), ('agriculture.', 'Agriculture'), ('manufacturing.', 'Manufacturing'),
          ('shipbreaker.', 'Shipbreaker'), ('autonav.', 'Auto Nav'), ('medical.', 'Medical'),
          ('war.', 'War Has Been Declared'), ('bank.', 'Banking'), ('exchange.', 'Exchange'), ('game.', 'Game')]
# Increments in ms holding the time every mod's patches take on one game method (ours and any other mod's).
HOOK_SPANS = {'game.interaction.offer_postfixes': 'crew offer check', 'game.interaction.effect_hooks': 'interaction effects'}


def owner(name):
    return next((label for prefix, label in OWNERS if name.startswith(prefix)), 'Other')


def window_report(run):
    """One window: own time per mod, the hook spans, the trigger-check estimate, collections and saves.

    Own (self) times do not overlap, so they add up per mod. A hook span holds whatever ran inside the patches, which can
    include a measured section already listed under its mod; the overlap is small and is not subtracted. The trigger
    check is timed on one call in N (Framework 0.133.0): the estimate takes off what an empty timed span reads
    (timer_overhead_ns) and scales the timed share up to every call, so it is an estimate and says so."""
    seconds, inc, ops = run['seconds'], run['increments'], run['operations']
    mods = {}
    for name, op in ops.items():
        if op['self_ms_per_second'] is None:
            continue
        mods[owner(name)] = mods.get(owner(name), 0.0) + op['self_ms_per_second']
    hooks = {label: inc[name] / seconds for name, label in HOOK_SPANS.items() if name in inc}
    trigger = None
    calls = inc.get('game.condtrigger.calls')
    sampled = inc.get('game.condtrigger.sampled_calls')
    sampled_ms = inc.get('game.condtrigger.postfix_sampled_ms')
    if calls and sampled and sampled_ms is not None:
        try:
            overhead_ns = float(run['metadata'].get('timer_overhead_ns', 'nan'))
        except ValueError:
            overhead_ns = math.nan
        corrected = sampled_ms - sampled * overhead_ns / 1e6 if math.isfinite(overhead_ns) else sampled_ms
        trigger = {'basis': 'estimate', 'calls_per_second': calls / seconds, 'timed_calls': sampled,
                   'timer_overhead_ns': overhead_ns if math.isfinite(overhead_ns) else None,
                   'ms_per_second': max(0.0, corrected) * calls / sampled / seconds}
    frames = run['frames']
    frame_total_ms = frames['mean_ms'] * run['quality']['frame_samples'] if frames['mean_ms'] is not None else None
    gc = run['levels'].get('game.gc.frame_ms')
    collections = None
    if gc:
        collections = {'frames': gc['samples'], 'total_ms': gc['sum'], 'worst_ms': gc['max'],
                       'share_of_frame_time_percent': gc['sum'] / frame_total_ms * 100 if frame_total_ms else None}
    saves = {name: {'calls': ops[name]['calls'], 'total_ms': ops[name]['inclusive_ms_per_second'] * seconds, 'max_ms': ops[name]['max_ms']}
             for name in ('game.save.begin', 'game.save.serialise') if name in ops and ops[name]['calls']}
    phobos = sum(v for k, v in mods.items() if k not in ('Game', 'Other')) + sum(hooks.values()) + (trigger['ms_per_second'] if trigger else 0)
    ranked = sorted(mods.items(), key=lambda p: -p[1])
    return {'capture_id': run['capture_id'], 'window': run['metadata'].get('window'), 'seconds': seconds,
            'contexts': run['contexts'], 'frames': frames,
            'own_ms_per_second': dict(ranked), 'own_share_percent': {k: v / 10 for k, v in ranked},
            'hook_ms_per_second': hooks, 'trigger_check_hooks': trigger,
            'phobos_ms_per_second': phobos, 'phobos_share_percent': phobos / 10,
            'collections': collections, 'saves': saves}


def report(runs):
    """A recording's windows in order (recording_id, then window), each with its per-mod breakdown."""
    def order(r):
        try:
            return (r['metadata'].get('recording_id', ''), int(r['metadata'].get('window', 0)))
        except ValueError:
            return (r['metadata'].get('recording_id', ''), 0)
    runs = sorted(runs, key=order)
    total = sum(r['seconds'] for r in runs)
    windows = [window_report(r) for r in runs]
    share = sum(w['phobos_share_percent'] * w['seconds'] for w in windows) / total if total else None
    return {'windows': windows, 'recordings': sorted({r['metadata'].get('recording_id', '') for r in runs}),
            'seconds': total, 'phobos_share_percent': share,
            'interpretation': "Own time is main-thread elapsed time outside each section's measured children, per real second; "
                              "shares are of real time. Hook spans include any mod's patches on that method. The trigger-check "
                              "figure is an estimate from timed samples. Nothing here measures the graphics card or memory per mod."}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--before', nargs='+', type=Path)
    parser.add_argument('--after', nargs='+', type=Path)
    parser.add_argument('--report', nargs='+', type=Path, help="per-mod breakdown of one recording's windows")
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    read = lambda paths: [analyse(json.loads(p.read_text(encoding='utf-8-sig'))) for p in paths]
    if args.report:
        if args.before or args.after:
            parser.error('--report is used on its own')
        result = report(read(args.report))
    elif args.before and args.after:
        result = compare(read(args.before), read(args.after))
    else:
        parser.error('give --before and --after, or --report')
    text = json.dumps(result, indent=2, allow_nan=False)
    if args.output:
        with args.output.open('x', encoding='utf-8') as stream:
            stream.write(text + '\n')
    else:
        print(text)
