import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def module(name, file):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'scripts' / file)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


saved = module('saved_actions', 'audit-saved-item-actions.py')
ledger = module('action_ledger', 'audit-item-handling.py')


class ItemActionAuditTests(unittest.TestCase):
    def test_generated_action_must_reach_its_object(self):
        data = json.loads((ROOT / 'docs/item-reference-data.json').read_text(encoding='utf-8'))
        ledger.validate(data)
        job = next(j for m in data['mods'] for i in m['items'] for j in i['jobs'])
        job['attached'] = False
        with self.assertRaisesRegex(ValueError, 'maintenance registration'):
            ledger.validate(data)

    def test_wear_and_pristine_are_different_contexts(self):
        data = json.loads((ROOT / 'docs/item-reference-data.json').read_text(encoding='utf-8'))
        job = next(j for m in data['mods'] for i in m['items'] for j in i['jobs'] if j['kind'] == 'restore')
        job['targetPassOnFreshDefinition'] = True
        with self.assertRaisesRegex(ValueError, 'pristine'):
            ledger.validate(data)

    def test_missing_direct_action_cannot_be_reported_as_registered(self):
        data = json.loads((ROOT / 'docs/item-reference-data.json').read_text(encoding='utf-8'))
        data['mods'][0]['items'][0]['missingDirectActions'] = ['MissingAction']
        with self.assertRaisesRegex(ValueError, 'Missing direct action'):
            ledger.validate(data)

    def test_hidden_container_detection_distinguishes_internal_feed(self):
        item = dict(container=dict(trigger='FitsCargo', inventoryAction=False),
                    handlingFlags=[], stackLimit=1, jobs=[])
        self.assertEqual(ledger.findings(item), ['H01: inaccessible container'])
        item['handlingFlags'] = ['IsSystem']
        self.assertEqual(ledger.findings(item), [])
        item['handlingFlags'] = []
        item['container']['trigger'] = None
        self.assertEqual(ledger.findings(item), [])

    def test_save_audit_preserves_archive_resolves_aliases_and_stack_members(self):
        definition = dict(id='Machine', aliases=['SavedAlias'], handlingFlags=['IsInstalled'],
                          container=dict(trigger='FitsCargo', inventoryAction=False))
        data = dict(mods=[dict(items=[definition])])
        ship = dict(aCOs=[dict(strID='private-id', strCODef='SavedAlias', aConds=['DEFAULT']),
                         dict(strID='cargo', strCODef='Supply', aConds=[], aStack=['unit', 'unit']),
                         dict(strID='unit', strCODef='Supply', aConds=[])],
                    aItems=[dict(strID='cargo', strParentID='private-id')])
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'owner.zip'
            with zipfile.ZipFile(path, 'w') as archive:
                archive.writestr('ships/private-name.json', json.dumps([ship]))
                archive.writestr('player.json', 'must not read this')
            before = path.read_bytes()
            result = saved.audit(path, data)
            self.assertEqual(before, path.read_bytes())
            self.assertEqual(result['savedObjects'], 1)
            self.assertEqual(result['missingHandlingFlags'], {})
            self.assertEqual(result['inaccessibleCargo'][0]['unitsIncludingStacks'], 2)
            self.assertNotIn('private-id', json.dumps(result))
            self.assertNotIn('private-name', json.dumps(result))

    def test_recovery_exemption_requires_both_blocked_deposits_and_access(self):
        item = dict(container=dict(trigger='PhobosCoolingNoNewCargo', inventoryAction=False),
                    actions=['PhobosCoolingRecoverCargo'], handlingFlags=[], stackLimit=1, jobs=[])
        self.assertEqual(ledger.findings(item), [])
        item['actions'] = []
        self.assertEqual(ledger.findings(item), ['H01: inaccessible container'])
        item['actions'] = ['PhobosCoolingRecoverCargo']
        item['container']['trigger'] = 'FitsCargo'
        self.assertEqual(ledger.findings(item), ['H01: inaccessible container'])

    def test_saved_explicit_zero_overrides_default_handling(self):
        definition = dict(id='Machine', aliases=[], handlingFlags=['IsCumbersome'],
                          container=dict(trigger=None, inventoryAction=False))
        for extra in (dict(aCondZeroes=['IsCumbersome']), dict(aConds=['DEFAULT', 'IsCumbersome=1x0'])):
            with self.subTest(extra=extra), tempfile.TemporaryDirectory() as tmp:
                owner = dict(strID='item', strCODef='Machine', aConds=['DEFAULT'])
                owner.update(extra)
                path = Path(tmp) / 'owner.zip'
                with zipfile.ZipFile(path, 'w') as archive:
                    archive.writestr('ships/example.json', json.dumps([dict(aCOs=[owner])]))
                result = saved.audit(path, dict(mods=[dict(items=[copy.deepcopy(definition)])]))
                self.assertEqual(result['missingHandlingFlags'], {'Machine:IsCumbersome': 1})


if __name__ == '__main__':
    unittest.main()
