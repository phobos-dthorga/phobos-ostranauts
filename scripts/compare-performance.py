#!/usr/bin/env python3
"""Compare opt-in Scope captures without treating inclusive timings as CPU percentages."""
import argparse
import json
import math
import statistics
from pathlib import Path


def percentile(values, fraction):
    return sorted(values)[max(0, math.ceil(len(values) * fraction) - 1)] if values else None


def analyse(data):
    if data.get('format_version') != 1:
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
    complete = data['dropped_records'] == 0 and data['rejected_measurements'] == 0
    operations = {}
    for row in data['aggregates']:
        name = definitions[row['metric']]['name']
        operations[name] = {
            'calls': row['calls'], 'calls_per_second': row['calls'] / seconds,
            'inclusive_ms_per_second': row['total_ticks'] / frequency * 1000 / seconds,
            'max_ms': row['max_ticks'] / frequency * 1000, 'incomplete': row['incomplete']}
    increments = {}
    for sample in data['counters']:
        definition = definitions[sample['metric']]
        if definition['kind'] == 'increment':
            increments[definition['name']] = increments.get(definition['name'], 0) + sample['value']
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
                    'rejected': data['rejected_measurements'], 'frame_samples': len(frames)},
        'frames': {'p50_ms': percentile(frames, .5), 'p95_ms': percentile(frames, .95),
                   'p99_ms': percentile(frames, .99), 'max_ms': max(frames) if frames else None,
                   'over_33_3_ms': sum(v > 1000 / 30 for v in frames),
                   'over_50_ms': sum(v > 50 for v in frames), 'over_100_ms': sum(v > 100 for v in frames)},
        'increments': increments, 'operations': operations}


def compare(before, after):
    warnings = []
    runs = before + after
    if len(before) != 3 or len(after) != 3:
        warnings.append('Acceptance requires three matched runs on each side.')
    if any(not r['quality']['complete_records'] or r['quality']['frame_samples'] == 0 for r in runs):
        warnings.append('Missing or dropped samples: frame acceptance is unverified; retained percentiles are descriptive only.')
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
    for key in ('p50_ms', 'p95_ms', 'p99_ms', 'over_50_ms', 'over_100_ms'):
        old, new = [r['frames'][key] for r in before], [r['frames'][key] for r in after]
        if old and new and None not in old + new:
            frame_changes[key] = change(old, new)
    names = set.intersection(*(set(r['operations']) for r in runs)) if before and after else set()
    operation_changes = {name: change([r['operations'][name]['inclusive_ms_per_second'] for r in before],
                                      [r['operations'][name]['inclusive_ms_per_second'] for r in after]) for name in sorted(names)}
    panel = operation_changes.get('autonav.panel.refresh', {}).get('change_percent')
    return {'warnings': warnings, 'before': before, 'after': after, 'frame_changes': frame_changes,
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
