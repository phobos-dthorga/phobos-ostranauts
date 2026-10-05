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
    if version not in (1, 2):
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
    if version == 2:
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
            'max_ms': row['max_ticks'] / frequency * 1000, 'incomplete': row['incomplete']}
    increments = {}
    if version == 2:
        increments = {name: t['sum'] for name, t in totals.items() if kinds[name] == 'increment'}
    else:
        for sample in data['counters']:
            definition = definitions[sample['metric']]
            if definition['kind'] == 'increment':
                increments[definition['name']] = increments.get(definition['name'], 0) + sample['value']
    # Levels (memory and each mod's footprint, Framework 0.104.0): the range and last reading of every gauge.
    levels = {name: {'samples': t['samples'], 'min': t['min'], 'max': t['max'], 'last': t['last']}
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
    panel = operation_changes.get('autonav.panel.refresh', {}).get('change_percent')
    level_names = set.intersection(*(set(r['levels']) for r in runs)) if before and after else set()
    level_changes = {name: change([r['levels'][name]['max'] for r in before], [r['levels'][name]['max'] for r in after])
                     for name in sorted(level_names)}
    return {'warnings': warnings, 'before': before, 'after': after, 'frame_changes': frame_changes,
            'level_max_changes': level_changes,
            'operation_inclusive_ms_per_second': operation_changes,
            'incomplete_operations': incomplete_operations,
            'panel_50_percent_target': None if warnings or panel is None or 'autonav.panel.refresh' in incomplete_operations else panel <= -50,
            'interpretation': 'Timings are inclusive wall time. Matching saves, workloads, graphics and recorder overhead require owner notes; this report does not establish exclusive CPU cost or gameplay correctness.'}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--before', nargs='+', type=Path, required=True)
    parser.add_argument('--after', nargs='+', type=Path, required=True)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    read = lambda paths: [analyse(json.loads(p.read_text(encoding='utf-8-sig'))) for p in paths]
    report = json.dumps(compare(read(args.before), read(args.after)), indent=2, allow_nan=False)
    if args.output:
        with args.output.open('x', encoding='utf-8') as stream:
            stream.write(report + '\n')
    else:
        print(report)
