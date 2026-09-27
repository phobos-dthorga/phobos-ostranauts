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
