"""Read-only native repair audit. Redirect output to ignored .local/ for research.

No game definitions, images, saves or assemblies are copied. Prices are native
base values, not merchant quotes. The proposed casting budget is authored.
"""
import argparse
import hashlib
import json
from pathlib import Path


def records(directory):
    for path in sorted(directory.glob('*.json')):
        for row in json.loads(path.read_text(encoding='utf-8-sig'), strict=False):
            yield row


def stats(row):
    result = {}
    for field in row.get('aStartingConds', []):
        name, _, expression = field.partition('=')
        if name.startswith('Stat'):
            factors = expression.split('x')
            value = 1.0
            for factor in factors:
                value *= float(factor)
            result[name] = value
    return result


def audit(game, workshop=None):
    data = game / 'Ostranauts_Data/StreamingAssets/data'
    owners = {r['strName']: r for r in records(data / 'condowners')}
    triggers = {r['strName']: r for r in records(data / 'condtrigs')}
    repairs = list(records(data / 'installables'))
    # Native Restore work also uses strJobType=repair. Count broken-item Repair
    # templates separately; do not mislabel maintenance as a parts-consuming repair.
    repairs = [r for r in repairs if r.get('strInteractionTemplate') in
               ('ACTRepairTEMP', 'ACTRepairNoSparksTEMP')]
    material_ids = {
        'TIsHeatSink': 'ItmHeatSink01', 'TIsMotor': 'ItmComponentMotor01',
        'TIsMobo': 'ItmComponentMobo01', 'TIsPartsMechSmall': 'ItmPartsMechSmall01',
        'TIsPartsElecSmall': 'ItmPartsElecSmall01', 'TIsScrapSteel': 'ItmScrapSteel',
        'TIsScrapAluminum': 'ItmScrapAluminum',
    }
    materials = {key: {'id': identity, 'mass_kg': stats(owners[identity])['StatMass'],
                       'base_price': stats(owners[identity])['StatBasePrice']}
                 for key, identity in material_ids.items()}
    selected = {'AtmoScrubber01DmgRepair', 'AtmoScrubber02DmgRepair',
                'Cooler01DmgRepair', 'Heater01DmgRepair',
                'AirPump02DmgLooseRepair', 'Battery02LooseRepair',
                'Antenna01DmgLooseRepair'}
    rows = []
    for repair in repairs:
        if repair['strName'] not in selected:
            continue
        bill, kg, price = {}, 0.0, 0.0
        for token in repair.get('aInputs', []):
            trigger, quantity = token.split('=')
            chance, count = map(float, quantity.split('x'))
            if chance != 1 or trigger not in materials:
                raise ValueError('Selected bill needs manual review: ' + token)
            bill[trigger] = count
            kg += count * materials[trigger]['mass_kg']
            price += count * materials[trigger]['base_price']
        target = owners[repair['strActionCO']]
        rows.append({'repair': repair['strName'], 'target': target['strName'],
                     'inputs': bill, 'input_kg': kg, 'base_input_value': round(price, 2),
                     'tools': repair.get('aToolCTsUse', []),
                     'repair_progress': stats(target).get('StatRepairProgressMax'),
                     'result_ids': repair.get('aLootCOs', [])})
    sink_repairs = [r['strName'] for r in repairs
                    if any(t.startswith('TIsHeatSink=') for t in r.get('aInputs', []))]
    sink_mass = materials['TIsHeatSink']['mass_kg']
    budget = {'feed_kg': 20, 'rough_cluster_kg': 19, 'terminal_remainder_kg': 1,
              'finished_count': 12, 'finished_unit_kg': sink_mass, 'finishing_offcut_kg': 1}
    assert budget['feed_kg'] == budget['rough_cluster_kg'] + budget['terminal_remainder_kg']
    assert budget['rough_cluster_kg'] == budget['finished_count'] * sink_mass + budget['finishing_offcut_kg']
    assembly = game / 'Ostranauts_Data/Managed/Assembly-CSharp.dll'
    result = {'assembly_sha256': hashlib.sha256(assembly.read_bytes()).hexdigest(),
              'repair_definition_count': len(repairs), 'materials': materials,
              'heat_sink_trigger': {k: triggers['TIsHeatSink'].get(k) for k in
                                    ('aReqs', 'aForbids', 'aTriggers', 'bAND')},
              'heat_sink_repair_count': len(sink_repairs), 'heat_sink_repairs': sink_repairs,
              'selected_repairs': rows, 'authored_casting_budget': budget,
              'native_equivalent_output_base_value': 12 * materials['TIsHeatSink']['base_price']}
    if workshop:
        recipe_file = workshop / '3798573453/crafting/recipes.json'
        recipes = json.loads(recipe_file.read_text(encoding='utf-8-sig'))['recipes']
        result['salvage_workshop_heat_sink_recipes'] = [
            {'id': r['id'], 'work_seconds': r.get('workSeconds'),
             'consumes_sink': any(i.get('item') == 'ItmHeatSink01' for i in r.get('ingredients', [])),
             'guaranteed_sink_output': any(i.get('item') == 'ItmHeatSink01' for i in r.get('outputs', [])),
             'chance_sink_output': 'ItmHeatSink01' in json.dumps(r.get('chanceGroups', []))}
            for r in recipes if 'ItmHeatSink01' in json.dumps(r)]
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-root', type=Path, required=True)
    parser.add_argument('--workshop-root', type=Path)
    args = parser.parse_args()
    print(json.dumps(audit(args.game_root, args.workshop_root), indent=2))
