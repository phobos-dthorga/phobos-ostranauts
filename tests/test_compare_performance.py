import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('compare_performance', Path(__file__).resolve().parents[1] / 'scripts/compare-performance.py')
perf = importlib.util.module_from_spec(spec)
spec.loader.exec_module(perf)


def capture():
    return dict(format_version=1, capture_id='fixture', clock_frequency_hz=1000, end_tick=30000, mode='summary',
                dropped_records=0, rejected_measurements=0, metadata=[], contexts=[], aggregates=[],
                definitions=[dict(id=1, name='game.frame.interval', kind='gauge'), dict(id=2, name='game.allocations.main_thread', kind='increment')],
                counters=[dict(metric=1, value=v) for v in [10, 20, 50, 100, 200]] + [dict(metric=2, value=0)])


class ComparisonTests(unittest.TestCase):
    def test_frames_and_allocation_support(self):
        row = perf.analyse(capture())
        self.assertEqual(row['frames']['p50_ms'], 50)
        self.assertEqual(row['frames']['over_100_ms'], 1)
        self.assertFalse(row['allocation_supported'])
        self.assertNotIn('game.allocations.main_thread', row['increments'])

    def test_bad_clock_and_frames(self):
        for field, value in [('clock_frequency_hz', 0), ('clock_frequency_hz', float('inf')), ('end_tick', -1)]:
            raw = capture(); raw[field] = value
            with self.assertRaises(ValueError): perf.analyse(raw)
        raw = capture(); raw['counters'][0]['value'] = float('nan')
        with self.assertRaises(ValueError): perf.analyse(raw)

    def test_quality_and_incomplete_scopes(self):
        raw = capture()
        for index, name, value in [(3, 'game.speed_multiplier', '1'), (4, 'game.paused', 'false'), (5, 'game.navigation_console_visible', 'true')]:
            raw['definitions'].append(dict(id=index, name=name, kind='context'))
            raw['contexts'].append(dict(metric=index, value=value))
        raw['definitions'].append(dict(id=6, name='autonav.panel.refresh', kind='duration'))
        raw['aggregates'] = [dict(metric=6, calls=100, total_ticks=10000, max_ticks=100, incomplete=0)]
        old = perf.analyse(raw); better = copy.deepcopy(raw); better['aggregates'][0]['total_ticks'] = 4000
        new = perf.analyse(better)
        self.assertTrue(perf.compare([old]*3, [new]*3)['panel_50_percent_target'])
        better['aggregates'][0]['incomplete'] = 1
        report = perf.compare([old]*3, [perf.analyse(better)]*3)
        self.assertIsNone(report['panel_50_percent_target'])
        self.assertEqual(report['warnings'], [])  # A stopped scope does not invalidate frame samples.
        raw['contexts'].append(dict(metric=4, value='true'))
        self.assertTrue(perf.compare([perf.analyse(raw)]*3, [new]*3)['warnings'])
        self.assertIsNone(perf.compare([old], [])['panel_50_percent_target'])

    def test_format_two_totals_and_buckets(self):
        names = [n for n, _ in perf.FRAME_BUCKETS]
        definitions = [dict(id=1, name='game.frame.interval', kind='gauge'), dict(id=2, name='memory.managed_heap', kind='gauge'),
                       dict(id=3, name='framework.crew.path_checks', kind='increment')]
        definitions += [dict(id=10 + i, name=n, kind='increment') for i, n in enumerate(names)]
        # 100 frames: 90 at 16 ms, 6 at 40 ms, 3 at 90 ms and one of 700 ms.
        counts = {'game.frame.upto_16_7ms': 90, 'game.frame.upto_50ms': 6, 'game.frame.upto_100ms': 3, 'game.frame.over_500ms': 1}
        totals = [dict(metric=1, samples=100, sum=90 * 16 + 6 * 40 + 3 * 90 + 700, min=16, max=700, last=16),
                  dict(metric=2, samples=30, sum=3e9, min=9e7, max=1.1e8, last=1e8),
                  dict(metric=3, samples=4, sum=12, min=1, max=5, last=2)]
        totals += [dict(metric=10 + i, samples=1, sum=counts.get(n, 0), min=counts.get(n, 0), max=counts.get(n, 0), last=counts.get(n, 0))
                   for i, n in enumerate(names)]
        raw = dict(capture(), format_version=2, definitions=definitions, counters=[], counter_aggregates=totals)
        row = perf.analyse(raw)
        frames = row['frames']
        self.assertEqual(frames['basis'], 'bucket_upper_bound')
        self.assertAlmostEqual(frames['p50_ms'], 1000 / 60)
        self.assertEqual((frames['p95_ms'], frames['p99_ms'], frames['max_ms']), (50, 100, 700))
        self.assertEqual((frames['over_33_3_ms'], frames['over_50_ms'], frames['over_100_ms']), (10, 4, 1))
        self.assertEqual(frames['mean_ms'], 26.5)
        self.assertEqual(row['quality']['frame_samples'], 100)
        self.assertEqual(row['increments']['framework.crew.path_checks'], 12)
        self.assertEqual(row['levels']['memory.managed_heap']['max'], 1.1e8)
        self.assertNotIn('game.frame.interval', row['levels'])
        report = perf.compare([row] * 3, [perf.analyse(capture())] * 3)
        self.assertTrue(any('bucket upper bounds' in w for w in report['warnings']))
        self.assertEqual(perf.compare([row] * 3, [row] * 3)['level_max_changes']['memory.managed_heap']['change_percent'], 0)

    def test_format_three_self_time(self):
        raw = dict(capture(), format_version=3, counter_aggregates=[])
        raw['definitions'].append(dict(id=6, name='autonav.panel.refresh', kind='operation'))
        raw['aggregates'] = [dict(metric=6, calls=10, total_ticks=3000, max_ticks=600, self_ticks=1500, incomplete=0)]
        row = perf.analyse(raw)
        self.assertEqual(row['operations']['autonav.panel.refresh']['self_ms_per_second'], 50)
        self.assertEqual(perf.compare([row] * 3, [row] * 3)['operation_self_ms_per_second']['autonav.panel.refresh']['change_percent'], 0)
        self.assertIsNone(perf.analyse(capture())['operations'].get('autonav.panel.refresh'))

