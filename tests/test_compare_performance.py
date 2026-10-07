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

    def test_report_per_mod(self):
        # Framework 0.133.0: one recording's windows in order, own time per mod, hook spans and the trigger estimate.
        def window(number, sweep_self):
            raw = dict(capture(), format_version=3, end_tick=10000, counters=[],
                       metadata=[dict(key='recording_id', value='r1'), dict(key='window', value=str(number)),
                                 dict(key='timer_overhead_ns', value='100.0')])
            raw['definitions'] = [dict(id=1, name='game.frame.interval', kind='gauge'),
                                  dict(id=2, name='framework.world.sweep', kind='operation'),
                                  dict(id=3, name='agriculture.machine.update', kind='operation'),
                                  dict(id=4, name='game.sim.advance', kind='operation'),
                                  dict(id=5, name='game.interaction.effect_hooks', kind='increment'),
                                  dict(id=6, name='game.condtrigger.calls', kind='increment'),
                                  dict(id=7, name='game.condtrigger.sampled_calls', kind='increment'),
                                  dict(id=8, name='game.condtrigger.postfix_sampled_ms', kind='increment'),
                                  dict(id=9, name='game.gc.frame_ms', kind='gauge'),
                                  dict(id=10, name='game.save.begin', kind='operation')]
            raw['aggregates'] = [dict(metric=2, calls=10, total_ticks=sweep_self, max_ticks=5, self_ticks=sweep_self, incomplete=0),
                                 dict(metric=3, calls=10, total_ticks=30, max_ticks=5, self_ticks=20, incomplete=0),
                                 dict(metric=4, calls=10, total_ticks=4000, max_ticks=900, self_ticks=4000, incomplete=0),
                                 dict(metric=10, calls=1, total_ticks=700, max_ticks=700, self_ticks=700, incomplete=0)]
            raw['counter_aggregates'] = [dict(metric=1, samples=100, sum=10000, min=50, max=900, last=50),
                                         dict(metric=5, samples=10, sum=20, min=1, max=3, last=2),
                                         dict(metric=6, samples=100, sum=1600, min=1, max=50, last=16),
                                         dict(metric=7, samples=100, sum=100, min=1, max=3, last=1),
                                         dict(metric=8, samples=100, sum=0.03, min=0, max=0.001, last=0),
                                         dict(metric=9, samples=2, sum=1500, min=600, max=900, last=600)]
            return perf.analyse(raw)
        result = perf.report([window(2, 50), window(1, 100)])
        first, second = result['windows']
        self.assertEqual((first['window'], second['window']), ('1', '2'))
        self.assertAlmostEqual(first['own_ms_per_second']['Framework'], 10)
        self.assertAlmostEqual(first['own_ms_per_second']['Agriculture'], 2)
        self.assertAlmostEqual(first['own_ms_per_second']['Game'], 470)
        self.assertAlmostEqual(first['hook_ms_per_second']['interaction effects'], 2)
        trigger = first['trigger_check_hooks']
        # 100 timed calls read 0.03 ms, of which 100 x 100 ns is the timer: 0.02 ms over 100 calls, scaled to 1,600 calls in 10 s.
        self.assertEqual(trigger['basis'], 'estimate')
        self.assertAlmostEqual(trigger['ms_per_second'], 0.02 * 16 / 10)
        self.assertAlmostEqual(first['phobos_ms_per_second'], 10 + 2 + 2 + 0.032)
        self.assertEqual(first['collections'], dict(frames=2, total_ms=1500, worst_ms=900, share_of_frame_time_percent=15))
        self.assertEqual(first['saves']['game.save.begin']['calls'], 1)
        self.assertAlmostEqual(result['phobos_share_percent'], (1.4032 + 0.9032) / 2)

