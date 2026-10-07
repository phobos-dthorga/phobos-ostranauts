"""Offline data-pack checks: the shipped packs validate, the recipe freeze is current, and the Python hash
agrees with the C# RecipeFreeze canonical form on a fixed sample (the same sample and digest are asserted in
tests/PhobosFramework.Tests/DataPackChecks.cs)."""
import importlib.util
import json
import math
from pathlib import Path
import subprocess
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]


def load(name):
    spec = importlib.util.spec_from_file_location(name.replace('-', '_'), ROOT / 'scripts' / f'{name}.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


validate = load('validate-data-packs')
freeze = load('freeze-recipes')
schemas = load('check-json-schemas')
writer = load('write-json-schemas')

SAMPLE = {"notes": "x", "machine": "test", "revision": 1, "inputs": [{"id": "A", "count": 2, "kg": 1.5}],
          "products": [{"id": "B", "count": 1, "kg": 3}], "melt": False}
SAMPLE_CANONICAL = '{"inputs":[{"count":2,"id":"A","kg":1.5}],"machine":"test","melt":false,"products":[{"count":1,"id":"B","kg":3}],"revision":1}'
SAMPLE_HASH = '49f8679fceadf55956c05d1013dad41f13c41b907a56bf53add8faee176b5381'


class DataPackTests(unittest.TestCase):
    def test_shipped_packs_validate(self):
        for path in sorted((ROOT / 'mods').glob('*/framework/*.json')):
            with self.subTest(path=path.name):
                validate.check_file(path)

    def test_shipped_packs_match_json_schemas(self):
        for path in sorted((ROOT / 'mods').glob('*/framework/*.json')):
            found = schemas.check_file(path)
            if found is None:
                continue
            with self.subTest(path=path.name):
                self.assertEqual(found, [])

    def test_json_schemas_are_current_and_refuse_drift(self):
        for name in writer.SCHEMAS:
            with self.subTest(schema=name):
                self.assertEqual((ROOT / 'schemas' / f'{name}.schema.json').read_text(encoding='utf-8'), writer.render(name))
        bad = {'schemaVersion': 1, 'schema': 'materials', 'materials': {'m': {'kg': 1, 'price': 1, 'colour': 'red'}}}
        self.assertTrue(schemas.problems(json.loads(writer.render('materials')), bad))
        bad_economy = {'schemaVersion': 1, 'schema': 'economy', 'equipment': {'PhobosX': {'price': 1, 'work': {'dismantle': 1}, 'kind': 'furniture'}}}
        self.assertTrue(schemas.problems(json.loads(writer.render('economy')), bad_economy))

    def test_freeze_is_current(self):
        result = subprocess.run([sys.executable, str(ROOT / 'scripts/freeze-recipes.py'), '--check', '--format', 'json'], capture_output=True, text=True, cwd=ROOT)
        report = json.loads(result.stdout)
        self.assertEqual(report['status'], 'valid', report)
        self.assertTrue(report['packs'], 'no process-recipe packs found')

    def test_canonical_hash_matches_csharp_sample(self):
        self.assertEqual(freeze.canonical(SAMPLE), SAMPLE_CANONICAL)
        self.assertEqual(freeze.entry_hash(SAMPLE), SAMPLE_HASH)

    def test_validator_refuses_mass_creation(self):
        pack = {'schemaVersion': 1, 'schema': 'process-recipes', 'recipes': {'x': {'machine': 'm', 'revision': 1,
                'inputs': [{'id': 'a', 'count': 1, 'kg': 1}], 'products': [{'id': 'b', 'count': 1, 'kg': 2}]}}}
        with self.assertRaises(validate.Problem):
            validate.process_recipes(pack, 'test')
        pack['recipes']['x']['products'][0]['kg'] = 1
        validate.process_recipes(pack, 'test')
        pack['recipes']['x']['offGas'] = {'H2': 0.1}
        with self.assertRaises(validate.Problem):
            validate.process_recipes(pack, 'test')

    def test_outcome_tables(self):
        # Framework 0.88.0: chance tables over ordinary recipes with the same charge; odds are tunable, the charge is not.
        unit = {'machine': 'm', 'inputs': [{'id': 'a', 'count': 4, 'kg': 3}], 'products': [{'id': 'b', 'count': 12, 'kg': 1}], 'seconds': 1200}
        recipes = {'wash': {**unit, 'revision': 1}, 'wash-steel': {**unit, 'revision': 2}, 'other': {**unit, 'revision': 3, 'seconds': 600}}
        def pack(outcomes, key='wash'):
            return {'schemaVersion': 1, 'schema': 'outcomes', 'tables': {key: {'outcomes': outcomes}}}
        validate.outcomes(pack({'wash': 50, 'wash-steel': 30}), 'test', recipes)
        validate.outcomes(pack({'wash': 0, 'wash-steel': 1}), 'test', recipes)
        for bad in (pack({'wash-steel': 30}), pack({'wash': 1, 'missing': 1}), pack({'wash': 1, 'other': 1}), pack({'wash': 0, 'wash-steel': 0}),
                    pack({'wash': 1, 'wash-steel': -1}), pack({'wash': 1, 'wash-steel': 10001}), pack({'wash': 1, 'wash-steel': 1.5}), pack({'nowhere': 1}, 'nowhere')):
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.outcomes(bad, 'test', recipes)
        shipped = json.loads((ROOT / 'mods/PhobosManufacturing/framework/outcomes.json').read_text(encoding='utf-8'))
        self.assertEqual(sum(shipped['tables']['gangue-wash']['outcomes'].values()), 100)
        # Manufacturing 0.50.0: the regolith leach, held to the bought-stock rule by its lean odds.
        self.assertEqual(shipped['tables']['regolith-leach']['outcomes'],
                         {'regolith-leach': 72, 'regolith-leach-steel': 22, 'regolith-leach-silicates': 4, 'regolith-leach-nickel-iron': 2})

    def test_line_placement(self):
        # Framework 0.100.0: where a pipe segment counts is data. The shipped rule lets one count in a wall.
        def pack(rule, key='default'):
            return {'schemaVersion': 1, 'schema': 'lines', 'placement': {key: rule}}
        good = {'forbiddenTiles': ['IsEVATile'], 'supports': [{'tile': 'IsFloor', 'object': 'floor'}]}
        validate.lines(pack(good), 'test')
        for bad in (pack(good, 'mine'), pack({'supports': []}), pack({'supports': [{'tile': 'Is Floor', 'object': 'floor'}]}),
                    pack({'supports': [{'tile': 'IsFloor'}]}), pack({'forbiddenTiles': ['IsFloor'], 'supports': [{'tile': 'IsFloor', 'object': 'floor'}]}),
                    pack({'forbiddenTiles': ['IsEVATile', 'IsEVATile'], 'supports': [{'tile': 'IsFloor', 'object': 'floor'}]}),
                    pack({'supports': [{'tile': 'IsFloor', 'object': 'floor', 'extra': 1}]})):
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.lines(bad, 'test')
        shipped = json.loads((ROOT / 'mods/PhobosFramework/framework/lines.json').read_text(encoding='utf-8'))
        rule = shipped['placement']['default']
        self.assertEqual([(s['tile'], s['object']) for s in rule['supports']], [('IsFloor', 'floor'), ('IsWall', 'IsWall')])
        self.assertEqual(rule['forbiddenTiles'], ['IsFloorFlex', 'IsEVATile'])

    def test_upkeep_pack(self):
        # Framework 0.111.0: crew upkeep figures. The shipped pack is valid; each broken copy is refused.
        shipped = json.loads((ROOT / 'mods/PhobosFramework/framework/upkeep.json').read_text(encoding='utf-8'))
        validate.upkeep(shipped, 'test')
        self.assertEqual((shipped['tuneStep'], shipped['tuneStepSkilled'], shipped['inspectionValidHours'], shipped['inspectedFadeShare'], shipped['practiceMinutes']), (0.2, 0.3, 24, 0.5, 10))
        validate.upkeep({**shipped, 'families': {'manufacturing.charge': {'gainShare': 0.5}}}, 'test')
        for bad in ({**shipped, 'tuneStep': 0}, {**shipped, 'tuneStepSkilled': 0.1}, {**shipped, 'inspectionValidHours': 0}, {**shipped, 'inspectedFadeShare': 1.5},
                    {**shipped, 'families': {'manufacturing.charge': {'gainShare': 2}}}, {**shipped, 'families': {'': {}}},
                    {**shipped, 'families': {'x': {'colour': 1}}}, {**shipped, 'colour': 1}, {**shipped, 'tuneStep': True}, {**shipped, 'practiceMinutes': 1}, {**shipped, 'practiceMinutes': 61}):
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.upkeep(bad, 'test')

    def test_store_rules(self):
        # Framework 0.116.0: which of the game's containers are not stores. The shipped pack leaves out every ship
        # weapon and ammunition rule, and keeps requiring the game's own Inventory entry.
        shipped = json.loads((ROOT / 'mods/PhobosFramework/framework/stores.json').read_text(encoding='utf-8'))
        validate.stores(shipped, 'test')
        self.assertEqual(shipped['requireInteraction'], 'Inventory')
        self.assertIn('IsShipWeapon', shipped['excludeConditions'])
        self.assertIn('TIsFitAmmo*', shipped['excludeContainers'])
        validate.stores({'schemaVersion': 1, 'schema': 'stores', 'requireInteraction': '', 'excludeConditions': []}, 'test')
        for bad in ({**shipped, 'requireInteraction': 'Open Me'}, {**shipped, 'excludeConditions': ['IsShipWeapon', 'IsShipWeapon']},
                    {**shipped, 'excludeConditions': ['Is Weapon']}, {**shipped, 'excludeContainers': ['T*']},
                    {**shipped, 'excludeContainers': ['TIsFit*Ammo']}, {**shipped, 'excludeConditions': [f'IsThing{n}' for n in range(49)]},
                    {**shipped, 'colour': 1}, {**shipped, 'requireInteraction': None}):
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.stores(bad, 'test')

    def test_story_faces(self):
        # Framework 0.121.0: a person's face look and a goal's own person, mirrored with the C# schema and the JSON Schema.
        import copy
        pack = {'schemaVersion': 1, 'schema': 'story',
                'people': {'orra-pell': {'name': 'Orra Pell', 'role': 'broker', 'home': 'oklg', 'face': 'feminine'}},
                'arcs': {'leaflets': {'title': 'Leaflets', 'steps': [
                    {'id': 'list', 'objective': {'title': 'Wait for the list', 'person': 'orra-pell'}, 'tests': [{'kind': 'wait', 'hours': 2}]}]}}}
        validate.story(pack, 'test', framework=False)
        self.assertEqual(schemas.problems(json.loads(writer.render('story')), pack), [])
        for change in (lambda p: p['people']['orra-pell'].update(face='female'),
                       lambda p: p['people']['orra-pell'].update(face=1),
                       lambda p: p['arcs']['leaflets']['steps'][0]['objective'].update(person='Orra Pell')):
            bad = copy.deepcopy(pack)
            change(bad)
            with self.subTest(bad=bad):
                with self.assertRaises(validate.Problem):
                    validate.story(bad, 'test', framework=False)
                self.assertNotEqual(schemas.problems(json.loads(writer.render('story')), bad), [])

    def test_story_replies(self):
        # Framework 0.122.0: a step the player answers in the Letters window, mirrored with the C# schema.
        import copy
        pack = {'schemaVersion': 1, 'schema': 'story', 'arcs': {'offer': {'title': 'An offer', 'steps': [
            {'id': 'letter', 'objective': {'title': 'Answer Dara'}, 'choices': [
                {'id': 'accept', 'label': "I'll carry it.", 'tests': [{'kind': 'credits', 'amount': 100}], 'next': 'carry'},
                {'id': 'refuse', 'label': 'Not this time.', 'next': 'end'}]},
            {'id': 'carry', 'tests': [{'kind': 'wait', 'hours': 1}]}]}}}
        validate.story(pack, 'test', framework=False)
        self.assertEqual(schemas.problems(json.loads(writer.render('story')), pack), [])
        step = lambda p: p['arcs']['offer']['steps'][0]
        for change in (lambda p: step(p)['choices'].pop(),
                       lambda p: step(p).update(tests=[{'kind': 'wait', 'hours': 1}]),
                       lambda p: step(p)['choices'][1].update(id='accept'),
                       lambda p: step(p)['choices'][0].update(next='elsewhere'),
                       lambda p: step(p)['choices'][1].pop('next'),
                       lambda p: step(p)['choices'][1].update(label='x' * 61),
                       lambda p: step(p)['choices'][1].update(colour='red')):
            bad = copy.deepcopy(pack)
            change(bad)
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.story(bad, 'test', framework=False)

    def test_story_packs(self):
        # Framework 0.107.0: story packs. The shipped seed is valid; each broken copy is refused, as the game's loader does.
        import copy
        shipped = json.loads((ROOT / 'mods/PhobosAgriculture/framework/story.json').read_text(encoding='utf-8'))
        validate.story(shipped, 'test', framework=False)
        self.assertEqual(schemas.problems(json.loads(writer.render('story')), shipped), [])
        arc = shipped['arcs']['verdemorrow-grain-sample']
        self.assertEqual([s['id'] for s in arc['steps']], ['grow', 'deliver'])
        self.assertIn(arc['steps'][0]['delivery']['bulletin'], shipped['broadcasts'])

        def broken(change):
            pack = copy.deepcopy(shipped)
            change(pack)
            return pack
        first = lambda p: p['arcs']['verdemorrow-grain-sample']['steps'][0]
        for bad in (broken(lambda p: p.update(settings={'checkSeconds': 30})),
                    broken(lambda p: p['broadcasts'].update({'Bad_Id': {'region': 'Tharsis', 'text': 'x'}})),
                    broken(lambda p: p['broadcasts']['galley-survey'].update(text='Hello [captain].')),
                    broken(lambda p: p['broadcasts']['galley-survey'].update(text='<b>Bold</b> news.')),
                    broken(lambda p: p['broadcasts']['galley-survey'].update(text='x' * 701)),
                    broken(lambda p: p['broadcasts']['galley-survey'].pop('region')),
                    broken(lambda p: p['adverts']['firstlight-advert'].update(colour='green')),
                    broken(lambda p: p['arcs']['verdemorrow-grain-sample'].update(chance=2)),
                    broken(lambda p: first(p)['tests'].clear()),
                    broken(lambda p: first(p)['tests'][0].update(kind='sleep')),
                    broken(lambda p: first(p)['tests'][0].update(station='any')),
                    broken(lambda p: first(p).update(id='deliver')),
                    broken(lambda p: first(p)['objective'].update(title='x' * 61)),
                    broken(lambda p: p['arcs']['verdemorrow-grain-sample']['requires'].update(mods=['Agri culture']))):
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.story(bad, 'test', framework=False)
        framework = json.loads((ROOT / 'mods/PhobosFramework/framework/story.json').read_text(encoding='utf-8'))
        validate.story(framework, 'test', framework=True)
        self.assertEqual(framework['settings'], {'broadcastShare': 0.3, 'advertShare': 0.3, 'checkSeconds': 30, 'maxActiveArcs': 2,
                                                 'chatterShare': 0.4, 'tipShare': 0.3, 'localWeight': 4, 'farWeight': 1, 'mentionDays': 10})
        self.assertEqual(sorted(framework['sections']), ['phobos-makers', 'phobos-operations', 'phobos-spacer-life'])
        # Framework 0.117.0: the About buttons' articles, all under Phobos operations, plain text within the limits.
        self.assertEqual(sorted(framework['articles']), ['operations-maintenance', 'operations-standing-orders', 'operations-store-links',
                                                         'operations-time-skips', 'operations-upkeep'])
        for key, article in framework['articles'].items():
            with self.subTest(article=key):
                self.assertEqual(article['section'], 'phobos-operations')
                self.assertLessEqual(len(article['body']), 4000)
                self.assertNotIn('requires', article)
        with self.assertRaises(validate.Problem):
            validate.story(broken(lambda p: p.update(settings={'broadcastShare': 0.3})), 'test', framework=False)

        # Framework 0.108.0: small talk, loading tips and encyclopedia articles.
        self.assertEqual({c['moment'] for c in shipped['chatter'].values()} - set(validate.STORY_MOMENTS), set())
        self.assertTrue(all(a['section'] in framework['sections'] for a in shipped['articles'].values()))
        self.assertTrue(all('mention' in b for b in shipped['broadcasts'].values()))
        chat = lambda p: p['chatter']['rack-hum']
        for bad in (broken(lambda p: chat(p).update(moment='gossip')),
                    broken(lambda p: chat(p).update(speakers='everyone')),
                    broken(lambda p: chat(p).update(line='x' * 201)),
                    broken(lambda p: chat(p).update(line='Look at [captain].')),
                    broken(lambda p: p['broadcasts']['galley-survey'].update(mention='<i>Hot</i> meals.')),
                    broken(lambda p: p['tips']['first-lettuce'].update(text='Hello [player].')),
                    broken(lambda p: p['tips']['first-lettuce'].update(requires={'owns': ['PhobosVerdemorrowFirstlight4Installed']})),
                    broken(lambda p: p['tips']['first-lettuce'].update(text='x' * 451)),
                    broken(lambda p: p['articles']['growing-food-aboard'].update(section='Not An Id')),
                    broken(lambda p: p['articles']['growing-food-aboard'].pop('body')),
                    broken(lambda p: p['articles']['growing-food-aboard'].update(label='x' * 41)),
                    broken(lambda p: p['articles']['growing-food-aboard'].update(requires={'arcsDone': ['verdemorrow-grain-sample']}))):
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.story(bad, 'test', framework=False)
        validate.story(broken(lambda p: p['tips']['first-lettuce'].update(requires={'mods': ['PhobosAgriculture']})), 'test', framework=False)

        # Framework 0.109.0: credits and condition tests, branches, next steps, credit rewards and story time.
        branching = {'schemaVersion': 1, 'schema': 'story', 'arcs': {'debt-run': {
            'title': 'Debt', 'requires': {'afterDays': 2, 'beforeDays': 30},
            'steps': [{'id': 'offer', 'tests': [{'kind': 'credits', 'amount': 500, 'consume': True}], 'onComplete': {'credits': 50}, 'next': 'thanks',
                       'branches': [{'tests': [{'kind': 'condition', 'condition': 'SkillHacking'}], 'next': 'end', 'onComplete': {'credits': 900}}]},
                      {'id': 'thanks', 'tests': [{'kind': 'wait', 'hours': 1}]}]}}}
        validate.story(branching, 'test', framework=False)
        self.assertEqual(schemas.problems(json.loads(writer.render('story')), branching), [])
        step = lambda p: p['arcs']['debt-run']['steps'][0]

        def bent(change):
            pack = copy.deepcopy(branching)
            change(pack)
            return pack
        for bad in (bent(lambda p: step(p).update(next='nowhere')),
                    bent(lambda p: step(p)['branches'][0].update(next='elsewhere')),
                    bent(lambda p: step(p)['branches'][0].pop('next')),
                    bent(lambda p: step(p)['tests'][0].update(amount=0)),
                    bent(lambda p: step(p)['tests'][0].update(item='Seed')),
                    bent(lambda p: step(p)['branches'][0]['tests'][0].update(consume=True)),
                    bent(lambda p: step(p)['onComplete'].update(credits=50001)),
                    bent(lambda p: p['arcs']['debt-run']['requires'].update(beforeDays=1)),
                    bent(lambda p: step(p).update(branches=[step(p)['branches'][0]] * 5))):
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.story(bad, 'test', framework=False)
        with self.assertRaises(validate.Problem):
            validate.story(broken(lambda p: p['tips']['first-lettuce'].update(requires={'afterDays': 3})), 'test', framework=False)

        # Framework 0.110.0: data files, files given by outcomes, filesRead, encyclopedia pictures.
        self.assertEqual(shipped['files']['trial-notes']['name'], 'TRIAL_NOTES.TXT')
        self.assertEqual(arc['steps'][1]['onComplete']['files'], ['trial-notes'])
        validate.story(broken(lambda p: p['articles']['growing-food-aboard'].update(image='phobos/agriculture/Counter')), 'test', framework=False)
        for bad in (broken(lambda p: p['files']['trial-notes'].update(name='TRIAL NOTES.TXT')),
                    broken(lambda p: p['files']['trial-notes'].update(name='X' * 33)),
                    broken(lambda p: p['files']['trial-notes'].update(text='x' * 3001)),
                    broken(lambda p: p['files']['trial-notes'].update(startsArc='Not An Id')),
                    broken(lambda p: p['files']['trial-notes'].update(colour='green')),
                    broken(lambda p: p['arcs']['verdemorrow-grain-sample']['steps'][1]['onComplete'].update(files=['trial-notes', 'trial-notes'])),
                    broken(lambda p: p['articles']['growing-food-aboard'].update(image='phobos/agriculture/Counter.png')),
                    broken(lambda p: p['tips']['first-lettuce'].update(requires={'filesRead': ['trial-notes']}))):
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.story(bad, 'test', framework=False)

    def test_story_grounding(self):
        # Framework 0.114.0: places, people, threads, flags and the gates on them. Framework's shipped places are valid.
        framework = json.loads((ROOT / 'mods/PhobosFramework/framework/story.json').read_text(encoding='utf-8'))
        validate.story(framework, 'test', framework=True)
        places = framework['places']
        regional = {k for k, p in places.items() if 'within' not in p}
        self.assertEqual(len(regional), 12)
        self.assertTrue(all(places[p]['within'] in regional for p in places if 'within' in places[p]))
        self.assertEqual({places[p]['region'] for p in regional}, {'Shipping & Inner System', 'Tharsis', 'Outer System'})
        self.assertEqual((framework['settings']['localWeight'], framework['settings']['farWeight'], framework['settings']['mentionDays']), (4, 1, 10))
        good = {
            'schemaVersion': 1, 'schema': 'story',
            'places': {'oklg': {'station': 'OKLG', 'name': 'OKLG', 'region': 'Outer System'}, 'oklg-res': {'station': 'OKLG_RES', 'name': 'the residential level', 'within': 'oklg'}},
            'people': {'neri': {'name': 'Neri Vale', 'role': 'receiving clerk', 'home': 'oklg-res'}},
            'threads': {'ledger': {'title': 'The ledger', 'place': 'oklg', 'people': ['neri'], 'requires': {'mods': ['PhobosShipbreaker']}}},
            'broadcasts': {'ledger-news': {'thread': 'ledger', 'text': 'News at [place], says [person:neri].', 'mention': 'That ledger.'}},
            'chatter': {'dock-talk': {'thread': 'ledger', 'moment': 'complaint', 'speakers': 'locals', 'line': 'Paperwork again.'}},
            'arcs': {'misfiled-can': {'title': 'The misfiled can', 'thread': 'ledger', 'chance': 0.1,
                     'steps': [{'id': 'letter', 'delivery': {'message': {'person': 'neri', 'text': 'Come by.'}}, 'tests': [{'kind': 'dock-at'}],
                                'onComplete': {'setFlags': ['ledger-open']}},
                               {'id': 'close', 'tests': [{'kind': 'wait', 'hours': 1}], 'onComplete': {'setFlags': ['ledger-closed'], 'clearFlags': ['ledger-open']}}]}},
            'adverts': {'ad': {'text': 'Buy.', 'requires': {'flags': ['ledger-closed'], 'arcsAtStep': ['misfiled-can.close'], 'regions': ['oklg'], 'newsSeen': ['ledger-news']}}},
            'tips': {'t': {'text': 'Lore.', 'thread': 'ledger'}},
            'files': {'note': {'name': 'NOTE.TXT', 'text': 'Written at [place] on [date].', 'thread': 'ledger', 'person': 'neri'}},
        }
        validate.story(good, 'test', framework=False)

        def broken(change):
            copy = json.loads(json.dumps(good))
            change(copy)
            return copy
        for bad in (broken(lambda p: p['places']['oklg'].update(station='any')),
                    broken(lambda p: p['places']['oklg'].pop('region')),
                    broken(lambda p: p['places']['oklg'].update(name='')),
                    broken(lambda p: p['places']['oklg'].update(factions=['Not a faction'])),
                    broken(lambda p: p['people']['neri'].update(home='Venus Orbital')),
                    broken(lambda p: p['people']['neri'].pop('name')),
                    broken(lambda p: p['threads']['ledger'].update(people=['neri', 'neri'])),
                    broken(lambda p: p['threads']['ledger'].pop('title')),
                    broken(lambda p: p['broadcasts']['ledger-news'].pop('thread')),
                    broken(lambda p: p['broadcasts']['ledger-news'].update(text='[person:Neri Vale]')),
                    broken(lambda p: p['chatter']['dock-talk'].update(speakers='neighbours')),
                    broken(lambda p: p['arcs']['misfiled-can']['steps'][0]['delivery']['message'].pop('person')),
                    broken(lambda p: p['arcs']['misfiled-can'].pop('thread')),
                    broken(lambda p: p['arcs']['misfiled-can']['steps'][1]['onComplete'].update(setFlags=['ledger-open'])),
                    broken(lambda p: p['arcs']['misfiled-can']['steps'][1]['onComplete'].update(setFlags=['a', 'b', 'c', 'd', 'e'])),
                    broken(lambda p: p['adverts']['ad']['requires'].update(arcsAtStep=['misfiled-can'])),
                    broken(lambda p: p['adverts']['ad']['requires'].update(places=['Not A Key'])),
                    broken(lambda p: p['tips']['t'].update(text='Lore at [place].')),
                    broken(lambda p: p['tips']['t'].update(requires={'flags': ['x']})),
                    broken(lambda p: p['files']['note'].update(person='Neri Vale')),
                    broken(lambda p: p.update(places={f'p{i}': {'station': f'S{i}', 'name': 'x', 'region': 'r'} for i in range(65)}))):
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.story(bad, 'test', framework=False)
        with self.assertRaises(validate.Problem):
            validate.story({**good, 'settings': {'localWeight': 101}}, 'test', framework=True)
        self.assertTrue(validate.story_arc_step('misfiled-can.letter') and not validate.story_arc_step('a.b.c') and not validate.story_arc_step('nodot'))

        # Framework 0.115.0: standing, crew and clock gates; standing changes.
        gated = broken(lambda p: p['adverts']['ad']['requires'].update(
            standing=[{'faction': 'OKLGCorp', 'atLeast': 'warm'}, {'faction': 'OKLGLEO', 'atMost': 'friendly'}], crewWith=['SkillBotany'],
            crewCount={'atLeast': 1, 'atMost': 4}, running=['PhobosVerdemorrowFirstlight4Installed'], months=[1, 12], hours={'from': 22, 'to': 5}))
        gated['chatter']['dock-talk']['speakerFactions'] = ['OKLGLEO']
        gated['arcs']['misfiled-can']['steps'][1]['onComplete']['standing'] = [{'faction': 'OKLGCorp', 'change': 5}]
        validate.story(gated, 'test', framework=False)
        for bad in (broken(lambda p: p['adverts']['ad']['requires'].update(standing=[{'faction': 'OKLGCorp'}])),
                    broken(lambda p: p['adverts']['ad']['requires'].update(standing=[{'faction': 'OKLGCorp', 'atLeast': 'liked'}])),
                    broken(lambda p: p['adverts']['ad']['requires'].update(standing=[{'faction': 'OKLGCorp', 'atLeast': 'trusted', 'atMost': 'warm'}])),
                    broken(lambda p: p['adverts']['ad']['requires'].update(crewCount={'atLeast': 3, 'atMost': 2})),
                    broken(lambda p: p['adverts']['ad']['requires'].update(months=[0])),
                    broken(lambda p: p['adverts']['ad']['requires'].update(months=[3, 3])),
                    broken(lambda p: p['adverts']['ad']['requires'].update(hours={'from': 24, 'to': 3})),
                    broken(lambda p: p['adverts']['ad']['requires'].update(running=['not a machine'])),
                    broken(lambda p: p['chatter']['dock-talk'].update(speakerFactions=['a', 'b', 'c', 'd', 'e'])),
                    broken(lambda p: p['arcs']['misfiled-can']['steps'][1]['onComplete'].update(standing=[{'faction': 'OKLGCorp', 'change': 11}])),
                    broken(lambda p: p['arcs']['misfiled-can']['steps'][1]['onComplete'].update(standing=[{'faction': 'OKLGCorp', 'change': 0}])),
                    broken(lambda p: p['arcs']['misfiled-can']['steps'][1]['onComplete'].update(standing=[{'faction': 'OKLGCorp', 'change': 1}, {'faction': 'OKLGCorp', 'change': 2}])),
                    broken(lambda p: p['tips']['t'].update(requires={'hours': {'from': 1, 'to': 2}}))):
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.story(bad, 'test', framework=False)
        self.assertIsNone(validate.story_plain('See [person:neri] at [place] on [date]'))
        self.assertEqual(validate.story_plain('[person:]'), '[person:]')

    def test_addon_checker(self):
        # Framework 0.90.0: the worked example is valid; broken copies are refused for the reason the game gives.
        import shutil, tempfile
        example = ROOT / 'examples/addons/PhobosExampleRicherGangue'
        manifest, checked = validate.check_addon(example)
        self.assertEqual(manifest['id'], 'example-richer-gangue')
        self.assertEqual(len(checked), 5)
        self.assertEqual(validate.check_addon(ROOT / 'examples/addons/PhobosAddOnTemplate')[1], [])
        # The same numbers Framework derives (tests/PhobosFramework.Tests/AddOnChecks.cs).
        self.assertEqual(validate.stable_hash(['a1', 'b2']), 1848931155)
        self.assertEqual(validate.derived_revision('richergangue-steel-seam'), 843336921)
        def broken(change):
            with tempfile.TemporaryDirectory() as temp:
                copy = Path(temp) / 'addon'
                shutil.copytree(example, copy)
                change(copy)
                with self.assertRaises(validate.Problem):
                    validate.check_addon(copy)
        recipes = 'phobos/PhobosManufacturing/process-recipes/richer-gangue.json'
        tables = 'phobos/PhobosManufacturing/outcomes/richer-gangue.json'
        def rewrite(rel, old, new):
            def change(copy):
                path = copy / rel
                text = path.read_text(encoding='utf-8')
                self.assertIn(old, text)
                path.write_text(text.replace(old, new), encoding='utf-8')
            return change
        broken(lambda copy: (copy / 'phobos-addon.json').unlink())
        broken(rewrite('phobos-addon.json', '"idPrefix": "richergangue"', '"idPrefix": "PhobosGangue"'))
        broken(rewrite(recipes, '"richergangue-steel-seam"', '"steel-seam"'))                      # an added id without the prefix
        broken(rewrite(recipes, '"count": 4, "kg": 1', '"count": 5, "kg": 1'))                       # creates a kilogram
        broken(rewrite(recipes, '"seconds": 1200', '"seconds": 600'))                              # another charge than its base
        broken(rewrite(tables, '"richergangue-steel-seam": 10', '"richergangue-missing": 10'))      # names no recipe
        broken(rewrite(tables, '"schema": "outcomes",', '"schema": "outcomes", "priority": 500,'))  # out of range
        broken(rewrite('phobos/translations/PhobosManufacturing/en.json', '"Recipe.richergangue-steel-seam"', '"Recipe.someone-elses"'))  # a new key without the prefix
        items = 'phobos/PhobosManufacturing/materials/richer-gangue.json'
        broken(rewrite(items, '"RichergangueSeamChunk"', '"SeamChunk"'))                                # an added item without the prefix
        broken(rewrite(items, '"image": "richergangue/SeamChunk"', '"image": "richergangue/Missing"'))   # no such picture
        broken(rewrite(items, '"category": "IsCategoryMetals"', '"category": "IsCategoryTrash"'))        # trash with no consumer
        broken(lambda copy: (copy / 'images/richergangue/SeamChunkNormal.png').unlink())
        # Shipbreaker 0.75.0 and Agriculture 0.48.0 take added items too: the second worked example.
        example = ROOT / 'examples/addons/PhobosExampleDocksideExtras'
        manifest, checked = validate.check_addon(example)
        self.assertEqual(manifest['id'], 'example-dockside-extras')
        self.assertEqual(len(checked), 6)
        cleat = 'phobos/PhobosShipbreaker/materials/dockside.json'
        broken(rewrite(cleat, '"kind": "stock"', '"kind": "furnace-packet"'))                          # Shipbreaker's added items are plain stock
        broken(rewrite(cleat, '"DocksideMooringCleat"', '"MooringCleat"'))                              # an added item without the prefix
        broken(rewrite('phobos/PhobosShipbreaker/process-recipes/dockside.json', '"count": 3, "kg": 5', '"count": 4, "kg": 5'))  # creates five kilograms
        broken(rewrite('phobos/PhobosAgriculture/crops/dockside.json', '"hunger": 2,', '"text": "meal", "hunger": 2,'))          # an added item is named in its materials entry
        broken(rewrite('phobos/PhobosAgriculture/crops/dockside.json', '"hunger": 2,', '"hunger": 40,'))                        # beyond what food gives
        broken(lambda copy: (copy / 'images/dockside/StewedTomatoes.png').unlink())
        for bad in ({'id': 'x'}, {'schemaVersion': 1, 'id': 'my-add-on', 'name': 'N', 'author': 'A', 'version': 'one', 'idPrefix': 'myaddon'}):
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.addon_manifest(bad)

    def test_validator_checks_supersession(self):
        # Framework 0.68.0: a later revision may replace one earlier revision of its own machine, and only one recipe may.
        def pack(*supersedes):
            unit = {'inputs': [{'id': 'a', 'count': 1, 'kg': 1}], 'products': [{'id': 'b', 'count': 1, 'kg': 1}]}
            recipes = {'old': {'machine': 'm', 'revision': 1, **unit}}
            for i, earlier in enumerate(supersedes):
                recipes[f'new{i}'] = {'machine': 'm', 'revision': 2 + i, 'supersedes': earlier, **unit}
            return {'schemaVersion': 1, 'schema': 'process-recipes', 'recipes': recipes}
        validate.process_recipes(pack([1]), 'test')
        for bad in (pack([2]), pack([3]), pack([0]), pack([1], [1]), pack('1')):
            with self.subTest(bad=bad), self.assertRaises(validate.Problem):
                validate.process_recipes(bad, 'test')

    def test_faction_kiosks_sell_everything_below_honored(self):
        # Owner direction (30 September 2026): every sold Phobos item is also at the faction kiosks, never at Honored.
        for path in sorted((ROOT / 'mods').glob('*/framework/economy.json')):
            pack = json.loads(path.read_text(encoding='utf-8'))
            with self.subTest(pack=path.parent.parent.name):
                kiosks = pack.get('factionKiosks')
                self.assertIsNotNone(kiosks, 'no faction-kiosk section')
                tiers = kiosks['tiers']
                self.assertNotIn('Honored', tiers.values())
                equipment = pack.get('equipment', {})

                def family(item):
                    # An exact key first, then the longest machine prefix (PhobosShipbreaker must not claim its section).
                    if item in equipment:
                        return item
                    found = [k for k, e in equipment.items() if (e.get('forms', 'machine') == 'machine' and item.startswith(k))
                             or (e.get('forms') == 'item' and item == k + 'Dmg')]
                    return max(found, key=len) if found else item
                sold = {k for k, e in equipment.items() if e.get('offers', True)} | set(pack.get('supplies', {}))
                sold |= set((pack.get('regional') or {}).get('items', {}))
                sold |= {family(o['item']) for o in pack.get('offers', {}).values()}
                self.assertEqual(sorted(sold - set(tiers)), [], 'sold elsewhere but not at the faction kiosks')

    def test_validator_refuses_bad_faction_tiers(self):
        pack = {'schemaVersion': 1, 'schema': 'economy', 'equipment': {}, 'factionKiosks': {'merchants': ['ItmFactionKioskBCRSInv'], 'tiers': {'PhobosX': 'Revered'}}}
        with self.assertRaises(validate.Problem):
            validate.economy(pack, 'test')
        bad = {'schemaVersion': 1, 'schema': 'economy', 'equipment': {}, 'factionKiosks': {'merchants': [], 'tiers': {}}}
        self.assertTrue(schemas.problems(json.loads(writer.render('economy')), bad))

    def test_crops_must_conserve_mass_and_stay_frozen(self):
        # Agriculture 0.40.0: crops are a data pack. Every file balances, and a published crop never changes.
        path = ROOT / 'mods/PhobosAgriculture/framework/crops.json'
        pack = json.loads(path.read_text(encoding='utf-8'))
        validate.crops(pack, 'test')
        frozen = json.loads((path.with_name('frozen-crops.json')).read_text(encoding='utf-8'))['revisions']
        self.assertEqual(sorted(frozen), sorted(pack['crops']))
        for crop_id, entry in pack['crops'].items():
            self.assertEqual(freeze.entry_hash(entry), frozen[crop_id], crop_id)
        for field, value in (('waterKg', 9), ('edibleKg', 99), ('feed', 'lettuce-v1'), ('produce', 'ItmScrapSteel'), ('keptStockKg', 0.1)):
            bad = json.loads(json.dumps(pack))
            bad['crops']['potato'][field] = value
            with self.subTest(field=field), self.assertRaises(validate.Problem):
                validate.crops(bad, 'test')
        bad = json.loads(json.dumps(pack))
        bad['crops']['potato']['yield'] = 1
        with self.assertRaises(validate.Problem):
            validate.crops(bad, 'test')
        self.assertTrue(schemas.problems(json.loads(writer.render('crops')), bad))

    def test_crop_growth_section_is_optional_and_bounded(self):
        # Agriculture 0.55.0: the growing room, stress rules and W2 feeding figures are data, with a room per crop.
        path = ROOT / 'mods/PhobosAgriculture/framework/crops.json'
        pack = json.loads(path.read_text(encoding='utf-8'))
        schema = json.loads(writer.render('crops'))
        self.assertEqual(pack['growth']['room'], {**pack['growth']['room'], 'minC': 18, 'maxC': 31, 'minKPa': 70, 'maxKPa': 110})
        self.assertEqual(sorted(pack['growth']['crops']), sorted(pack['crops']))
        self.assertEqual(schemas.problems(schema, pack), [])

        def changed(change):
            copy = json.loads(json.dumps(pack))
            change(copy)
            return copy

        fine = [lambda p: p.pop('growth'), lambda p: p['growth'].pop('room'), lambda p: p['growth']['crops']['potato'].update(room={'minC': 10, 'maxKPa': 120}),
                lambda p: p['growth']['room'].update(maxC=35), lambda p: p['growth'].update(nutrientTargetKg=0.5),
                # Agriculture 0.59.0: misting, shared and per crop; all of it optional.
                lambda p: p['growth'].pop('misting'), lambda p: p['growth']['misting'].update(maxCoolingC=0),
                lambda p: p['growth']['crops']['lettuce'].update(misting={'maxCoolingC': 3})]
        for index, change in enumerate(fine):
            with self.subTest(fine=index):
                validate.crops(changed(change), 'test')
        # A new crop needs no old feed names.
        added = changed(lambda p: p['crops'].update({'fast-lettuce': {k: v for k, v in p['crops']['lettuce'].items() if k not in ('feed', 'feedCommodity')}}))
        validate.crops(added, 'test')
        self.assertEqual(schemas.problems(schema, added), [])
        bad = [lambda p: p['growth']['room'].update(maxC=10), lambda p: p['growth']['room'].update(maxC=140), lambda p: p['growth']['room'].update(minKPa=0),
               lambda p: p['growth']['crops']['potato'].update(room={'maxC': 12}), lambda p: p['growth']['crops'].update(rye={}),
               lambda p: p['growth']['stress'].update(healthLossPerHour=2), lambda p: p['growth']['stress'].update(graceHours=-1),
               lambda p: p['growth'].update(nutrientTargetKg=0), lambda p: p['growth'].update(nutrientTargetKg=0.6), lambda p: p['growth'].update(feedStrengthKgPerKg=0),
               lambda p: p['growth'].update(speed=2), lambda p: p['growth']['room'].update(maxF=90),
               lambda p: p['growth']['misting'].update(maxCoolingC=16), lambda p: p['growth']['misting'].update(waterKgPerHourPerC=0),
               lambda p: p['growth']['misting'].update(damageShareBeyond=1.5), lambda p: p['growth']['misting'].update(reserveKg=21),
               lambda p: p['growth']['misting'].update(sprayers=4),
               lambda p: p['growth']['crops']['lettuce'].update(misting={'maxCoolingC': -1}),
               lambda p: p['growth']['crops']['lettuce'].update(misting={'reserveKg': 1})]
        for index, change in enumerate(bad):
            with self.subTest(bad=index), self.assertRaises(validate.Problem):
                validate.crops(changed(change), 'test')

    def test_care_pack_keeps_thresholds_below_the_game_limits(self):
        # Phobos Medical 0.1.0: the care pack holds station power and admission thresholds; the healing stays the game's.
        path = ROOT / 'mods/PhobosMedical/framework/care.json'
        pack = json.loads(path.read_text(encoding='utf-8'))
        validate.care(pack, 'test')
        self.assertEqual(schemas.problems(json.loads(writer.render('care')), pack), [])
        for section, field, value in (('admission', 'bloodLost', 40), ('admission', 'pain', 0), ('admission', 'dischargeShare', 1),
                                      ('admission', 'wound', 1.5), ('stations', 'bed', {'idleKW': 0.5, 'workingKW': 0.1}),
                                      ('stations', 'bed', {'idleKW': 0, 'workingKW': 3}), ('stations', 'autodoc', {'idleKW': 0, 'workingKW': 0.1})):
            bad = json.loads(json.dumps(pack))
            bad[section][field] = value
            with self.subTest(field=field, value=value), self.assertRaises(validate.Problem):
                validate.care(bad, 'test')
        for value in (0.01, 1.5):
            bad = json.loads(json.dumps(pack))
            bad['levels']['bed']['weightlessHealing'] = value
            with self.subTest(weightless=value), self.assertRaises(validate.Problem):
                validate.care(bad, 'test')
        bad = json.loads(json.dumps(pack)); bad['alerts']['pain'] = 75
        with self.assertRaises(validate.Problem):
            validate.care(bad, 'test')
        legacy = json.loads(json.dumps(pack)); del legacy['levels']; del legacy['alerts']; del legacy['treatments']
        validate.care(legacy, 'test')
        # Medical 0.4.0: treatments name a fixed test and effect, one item and a bounded time; players may add one.
        added = json.loads(json.dumps(pack))
        added['treatments']['dress-with-dirty'] = {'test': 'bleeding', 'item': 'ItmScrapClothDirty', 'medicSeconds': 30}
        validate.care(added, 'test')
        self.assertEqual(schemas.problems(json.loads(writer.render('care')), added), [])
        for field, value in (('test', 'amputation'), ('effect', 'apply-condition'), ('item', ''), ('medicSeconds', 0),
                             ('medicSeconds', 4000), ('order', 1.5), ('skill', 'Skill Medical'), ('heal', 1)):
            bad = json.loads(json.dumps(pack))
            bad['treatments']['dress-bleeding'][field] = value
            with self.subTest(treatment=field, value=value), self.assertRaises(validate.Problem):
                validate.care(bad, 'test')
        bad = json.loads(json.dumps(pack)); bad['treatments']['bad name'] = pack['treatments']['dress-bleeding']
        with self.assertRaises(validate.Problem):
            validate.care(bad, 'test')
        bad = json.loads(json.dumps(pack))
        bad['admission']['heal'] = 1
        with self.assertRaises(validate.Problem):
            validate.care(bad, 'test')
        self.assertTrue(schemas.problems(json.loads(writer.render('care')), bad))

    def test_validator_refuses_unknown_fields(self):
        pack = {'schemaVersion': 1, 'schema': 'materials', 'materials': {'m': {'kg': 1, 'price': 1, 'colour': 'red'}}}
        with self.assertRaises(validate.Problem):
            validate.materials(pack, 'test')


    def test_lenders_pack_mirrors_the_game_rules(self):
        # Phobos Banking 0.2.0: the shipped lenders pass, and the Python mirror and the JSON Schema refuse the same mistakes.
        path = ROOT / 'mods/PhobosBank/framework/lenders.json'
        pack = json.loads(path.read_text(encoding='utf-8'))
        validate.lenders(pack, 'lenders')
        self.assertEqual(schemas.problems(json.loads(writer.render('lenders')), pack), [])
        lender = pack['lenders']['corvane-mutual']
        for field, value in (('ratePerShift', 0), ('ratePerShift', 0.5), ('name', 'Corvane, Mutual'), ('offers', ['gold']),
                             ('offers', ['cash', 'cash']), ('home', 'OKLG'), ('maxLoans', 9), ('minDownShare', 0.05)):
            bad = json.loads(json.dumps(pack))
            bad['lenders']['corvane-mutual'][field] = value
            with self.subTest(field=field, value=value):
                with self.assertRaises(validate.Problem):
                    validate.lenders(bad, 'lenders')
        swapped = json.loads(json.dumps(pack))
        swapped['lenders']['corvane-mutual']['minPrincipal'] = lender['maxPrincipal']
        with self.assertRaises(validate.Problem):
            validate.lenders(swapped, 'lenders')
        unknown = json.loads(json.dumps(pack))
        unknown['lenders']['corvane-mutual']['colour'] = 'blue'
        with self.assertRaises(validate.Problem):
            validate.lenders(unknown, 'lenders')
        self.assertNotEqual(schemas.problems(json.loads(writer.render('lenders')), unknown), [])
        # Phobos Banking 0.6.0: credit lines.
        self.assertIn('orrery-credit', pack['creditLines'])
        for field, value in (('drawFee', 0.5), ('limit', 10), ('ratePerShift', 0), ('minDraw', 50), ('minDraw', 30000), ('name', 'Orrery|Credit')):
            bad = json.loads(json.dumps(pack))
            bad['creditLines']['orrery-credit'][field] = value
            with self.subTest(line_field=field, value=value):
                with self.assertRaises(validate.Problem):
                    validate.lenders(bad, 'lenders')
        shared = json.loads(json.dumps(pack))
        shared['creditLines']['corvane-mutual'] = shared['creditLines']['orrery-credit']
        with self.assertRaises(validate.Problem):
            validate.lenders(shared, 'lenders')

    def test_event_namespaces_match_the_mods(self):
        # Framework 0.129.0: the Python mirror's namespaces are exactly those the mods register.
        import re
        registered = set()
        for path in (ROOT / 'src').rglob('*.cs'):
            registered.update(re.findall(r'AddOns\.RegisterNamespace\("([a-z]+)"\)', path.read_text(encoding='utf-8')))
        self.assertEqual(registered, set(validate.EVENT_NAMESPACES))
        self.assertTrue(validate.owns('exchange-keelhaul-freight-bought', 'keelhaul'))
        self.assertFalse(validate.owns('exchange-smartlink-surge', 'keelhaul'))

    def test_exchange_listing_example(self):
        # Phobos Exchange 0.2.0 and Framework 0.129.0: the worked example lists a company with news and answers the
        # exchange's own bought event, which the exchange namespace lets an add-on name.
        example = ROOT / 'examples/addons/PhobosExampleKeelhaulListing'
        manifest, checked = validate.check_addon(example)
        self.assertEqual(manifest['id'], 'example-keelhaul-listing')
        self.assertEqual(len(checked), 2)
        saved = validate.EVENT_NAMESPACES
        try:
            validate.EVENT_NAMESPACES = ()
            with self.assertRaises(validate.Problem):
                validate.check_addon(example)
        finally:
            validate.EVENT_NAMESPACES = saved

    def test_exchange_histories_mirror_the_game_rules(self):
        # Phobos Exchange 0.3.0: founding and listing years, listing prices and history entries. The mirror's year range
        # is the game code's, and the mirror and the JSON Schema refuse the same mistakes.
        import re
        rules = (ROOT / 'src/PhobosExchange/Core/ExchangeRules.cs').read_text(encoding='utf-8')
        first, age = re.search(r'FirstSaveYear = (\d+), MaxAgeYears = (\d+)', rules).groups()
        self.assertEqual((int(first), int(age)), (validate.EXCHANGE_FIRST_SAVE_YEAR, validate.EXCHANGE_MAX_AGE_YEARS))
        self.assertIn('LastMoveYear = FirstSaveYear - 3', rules)
        path = ROOT / 'mods/PhobosExchange/framework/exchange.json'
        pack = json.loads(path.read_text(encoding='utf-8'))
        self.assertIsNotNone(pack['market'].get('opened'))
        self.assertTrue(all('founded' in c and 'listed' in c for c in pack['companies'].values()))
        good = json.loads(json.dumps(pack))
        good['market']['history'] = {'ceres-panic': {'year': 2061, 'month': 4, 'move': -0.3, 'line': 'Panic selling after the Ceres yard fire.'}}
        good['sectors']['industry']['history'] = {'hull-boom': {'year': 2050, 'move': 0.25, 'line': 'Hull orders double across the yards.'}}
        smartlink = good['companies']['smartlink']
        smartlink['listingPrice'] = 30
        smartlink['history'] = {'first-contract': {'year': 1960, 'line': 'Its first defence contract.'},
                                'titan-move': {'year': 2045, 'move': 0.2, 'line': 'Moves its headquarters to Titan.'}}
        validate.exchange(good, 'exchange')
        self.assertEqual(schemas.problems(json.loads(writer.render('exchange')), good), [])
        expected = (math.log(184 / 30) - (math.log(0.7) + math.log(1.25) + math.log(1.2))) / (2079 - 2036)
        self.assertAlmostEqual(validate.exchange_listing_growth(good, smartlink, 2036), expected, places=12)

        def refused(change, schema_too=False):
            bad = json.loads(json.dumps(good))
            change(bad)
            with self.assertRaises(validate.Problem):
                validate.exchange(bad, 'exchange')
            if schema_too:
                self.assertNotEqual(schemas.problems(json.loads(writer.render('exchange')), bad), [])

        cases = {
            'opened before the 200 years': (lambda p: p['market'].update(opened=1878), True),
            'a year from the game start': (lambda p: p['companies']['smartlink'].update(founded=2079), True),
            'listed before founded': (lambda p: p['companies']['testudo'].update(listed=2018), False),
            'listed before the exchange opened': (lambda p: p['companies']['testudo'].update(founded=2020, listed=2030), False),
            'a move in the drawn years': (lambda p: p['companies']['smartlink']['history']['titan-move'].update(year=2077), False),
            'a move before the listing': (lambda p: p['companies']['smartlink']['history']['titan-move'].update(year=2035), False),
            'lore before the founding': (lambda p: p['companies']['smartlink']['history']['first-contract'].update(year=1950), False),
            'month 13': (lambda p: p['market']['history']['ceres-panic'].update(month=13), True),
            'a fall too deep': (lambda p: p['market']['history']['ceres-panic'].update(move=-0.7), True),
            'a rise too steep': (lambda p: p['sectors']['industry']['history']['hull-boom'].update(move=1.5), True),
            'a move too small': (lambda p: p['sectors']['industry']['history']['hull-boom'].update(move=0.005), False),
            'a placeholder in a line': (lambda p: p['companies']['smartlink']['history']['first-contract'].update(line='Its [first] contract.'), False),
            'an unknown field': (lambda p: p['companies']['smartlink']['history']['first-contract'].update(colour='red'), True),
            'a cliff of a listing price': (lambda p: p['companies']['verdemorrow'].update(listingPrice=0.1), False),
            'a collapse of a listing price': (lambda p: p['companies']['smartlink'].update(listingPrice=100000), False),
            'a listing price for a late listing': (lambda p: p['companies']['verdemorrow'].update(listed=2077, listingPrice=27), False),
            'a history without years': (lambda p: p['companies']['halewright'].update(founded=None, listed=None, history={'x': {'year': 2060, 'line': 'Something happened.'}}) or
                                        [p['companies']['halewright'].pop(k) for k in ('founded', 'listed')], False),
            'too many entries': (lambda p: p['sectors']['industry']['history'].update({f'e{i}': {'year': 2050, 'line': 'An event.'} for i in range(9)}), False),
        }
        for label, (change, schema_too) in cases.items():
            with self.subTest(label):
                refused(change, schema_too)

    def test_exchange_pack_mirrors_the_game_rules(self):
        # Phobos Exchange 0.1.0: the shipped exchange passes; the Python mirror and the JSON Schema refuse the same mistakes,
        # and the mirror applies the owner's expected-return guard exactly as the game does.
        path = ROOT / 'mods/PhobosExchange/framework/exchange.json'
        pack = json.loads(path.read_text(encoding='utf-8'))
        validate.exchange(pack, 'exchange')
        self.assertEqual(schemas.problems(json.loads(writer.render('exchange')), pack), [])
        for key, company in pack['companies'].items():
            expected = validate.exchange_expected_return(company, pack['sectors'][company['sector']]['trend'], pack['market']['trend'])
            with self.subTest(company=key):
                self.assertGreater(expected, 0)
                self.assertLessEqual(expected, validate.EXCHANGE_MAX_RETURN)
        for field, value in (('ticker', 'smlk'), ('ticker', 'TOOLONG'), ('drift', 0.2), ('volatility', 0), ('sector', 'farming'), ('name', 'Smart;link'),
                             ('spread', 0.2), ('price', 0)):
            bad = json.loads(json.dumps(pack))
            bad['companies']['smartlink'][field] = value
            with self.subTest(field=field, value=value):
                with self.assertRaises(validate.Problem):
                    validate.exchange(bad, 'exchange')
        repeated = json.loads(json.dumps(pack))
        repeated['companies']['testudo']['ticker'] = repeated['companies']['smartlink']['ticker']
        with self.assertRaises(validate.Problem):
            validate.exchange(repeated, 'exchange')
        wild = json.loads(json.dumps(pack))
        wild['companies']['smartlink']['trend']['sd'] = 0.5
        with self.assertRaises(validate.Problem):
            validate.exchange(wild, 'exchange')
        for driver_field, value in (('station', 'mtrs'), ('category', 'Weapons'), ('weight', 0), ('limit', 0.5)):
            bad = json.loads(json.dumps(pack))
            bad['companies']['smartlink']['drivers'][0][driver_field] = value
            with self.subTest(driver_field=driver_field, value=value):
                with self.assertRaises(validate.Problem):
                    validate.exchange(bad, 'exchange')
        unknown = json.loads(json.dumps(pack))
        unknown['companies']['smartlink']['colour'] = 'blue'
        with self.assertRaises(validate.Problem):
            validate.exchange(unknown, 'exchange')
        # Phobos Exchange 0.2.0: story news.
        good = json.loads(json.dumps(pack))
        good['companies']['smartlink']['news'] = [{'flag': 'smlk-test-contract', 'move': 0.08, 'wire': 'Smartlink wins a contract.'}]
        validate.exchange(good, 'exchange')
        self.assertEqual(schemas.problems(json.loads(writer.render('exchange')), good), [])
        for entry in ({'flag': 'Bad Flag', 'move': 0.08}, {'flag': 'smlk-a', 'move': 0.5}, {'flag': 'smlk-a', 'move': 0.001},
                      {'flag': 'smlk-a', 'move': 0.05, 'wire': '[player] did it'}, {'flag': 'smlk-a', 'move': 0.05, 'odds': 1}):
            bad = json.loads(json.dumps(pack))
            bad['companies']['smartlink']['news'] = [entry]
            with self.subTest(news=entry):
                with self.assertRaises(validate.Problem):
                    validate.exchange(bad, 'exchange')
        twice = json.loads(json.dumps(pack))
        twice['companies']['smartlink']['news'] = [{'flag': 'smlk-a', 'move': 0.05}, {'flag': 'smlk-a', 'move': 0.06}]
        with self.assertRaises(validate.Problem):
            validate.exchange(twice, 'exchange')
        self.assertNotEqual(schemas.problems(json.loads(writer.render('exchange')), unknown), [])

if __name__ == '__main__':
    unittest.main()
