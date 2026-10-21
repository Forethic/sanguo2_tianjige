import struct, json, os, re

pak_path = r'D:\Software\Steam\steamapps\common\Sango2\Sango2.PAK'
with open(pak_path, 'rb') as f:
    pak = f.read()

scenarios = [
    (1, 'Things.ini', 0x1e7cb7, 0x328e1),
    (2, 'Times1.ini', 0x21a598, 0x32f05),
    (3, 'Times2.ini', 0x24d49d, 0x330dd),
    (4, 'Times3.ini', 0x28057a, 0x33892),
    (5, 'Times4.ini', 0x2b3e0c, 0x33c2d),
]

# Load existing Generals.json to map trad/simp names
with open('data/Generals.json', 'r', encoding='utf-8-sig') as f:
    gen_list = json.load(f)
name_map = {}
for g in gen_list:
    name_map[g['Name']] = g['Name']

out_summary = []

for s_id, iname, off, sz in scenarios:
    raw = pak[off: off+sz]
    text = raw.decode('big5', errors='ignore')
    
    # 1. Parse Kings
    king_sections = re.findall(r'\[KING([0-9]+)\](.*?)(?=\n\[|\Z)', text, re.DOTALL)
    kings = []
    for k_num, k_body in king_sections:
        nm_m = re.search(r'NAME=([^\r\n;]+)', k_body)
        flag_m = re.search(r'FLAGNUM=([^\r\n;]+)', k_body)
        type_m = re.search(r'TYPE=([^\r\n;]+)', k_body)
        sel_m = re.search(r'SELECT=([^\r\n;]+)', k_body)
        
        name = nm_m.group(1).strip() if nm_m else ''
        kings.append({
            'Section': f'KING{k_num}',
            'Name': name,
            'FlagNum': int(flag_m.group(1).strip()) if flag_m and flag_m.group(1).strip().isdigit() else 0,
            'Type': int(type_m.group(1).strip()) if type_m and type_m.group(1).strip().isdigit() else 0,
            'Select': int(sel_m.group(1).strip()) if sel_m and sel_m.group(1).strip().isdigit() else 0
        })

    # 2. Parse Cities
    city_sections = re.findall(r'\[CITY([0-9]+)\](.*?)(?=\n\[|\Z)', text, re.DOTALL)
    cities = []
    for c_num, c_body in city_sections:
        nm_m = re.search(r'NAME=([^\r\n;]+)', c_body)
        lord_m = re.search(r'KING=([^\r\n;]+)', c_body) or re.search(r'LORD=([^\r\n;]+)', c_body)
        chief_m = re.search(r'MAJOR=([^\r\n;]+)', c_body) or re.search(r'CHIEF=([^\r\n;]+)', c_body)
        adv_m = re.search(r'WISE=([^\r\n;]+)', c_body) or re.search(r'ADVISER=([^\r\n;]+)', c_body)
        money_m = re.search(r'MONEY=([^\r\n;]+)', c_body)
        people_m = re.search(r'POPULATION=([^\r\n;]+)', c_body) or re.search(r'PEOPLE=([^\r\n;]+)', c_body)
        def_m = re.search(r'DEFENSE=([^\r\n;]+)', c_body)
        reserve_m = re.search(r'RESERVE=([^\r\n;]+)', c_body) or re.search(r'RESERVESOLDIERNUMCUR=([^\r\n;]+)', c_body)

        cid = int(c_num)
        cname = nm_m.group(1).strip() if nm_m else ''
        
        # Check lord
        lord_val = lord_m.group(1).strip() if lord_m else '0'
        lord_id = 0
        if lord_val.isdigit():
            lord_id = int(lord_val)
        else:
            # find king index
            for kidx, k in enumerate(kings, 1):
                if k['Name'] == lord_val:
                    lord_id = kidx
                    break

        cities.append({
            'Id': cid,
            'Name': cname,
            'Lord': lord_id,
            'Chief': 0,
            'Adviser': 0,
            'Money': int(money_m.group(1).strip()) if money_m and money_m.group(1).strip().isdigit() else 0,
            'People': int(people_m.group(1).strip()) if people_m and people_m.group(1).strip().isdigit() else 0,
            'Defend': int(def_m.group(1).strip()) if def_m and def_m.group(1).strip().isdigit() else 0,
            'Economics': 0,
            'ReserveSoldier': int(reserve_m.group(1).strip()) if reserve_m and reserve_m.group(1).strip().isdigit() else 0
        })

    # Write JSON files for scenario
    with open(f'data/Monarchs_Scenario{s_id}.json', 'w', encoding='utf-8') as f:
        json.dump(kings, f, ensure_ascii=False, indent=2)
    
    with open(f'data/Cities_Scenario{s_id}.json', 'w', encoding='utf-8') as f:
        json.dump(cities, f, ensure_ascii=False, indent=2)

    out_summary.append({
        'scenario': s_id,
        'kings_count': len(kings),
        'kings': [k['Name'] for k in kings],
        'cities_count': len(cities)
    })

with open('scratch/extraction_summary.json', 'w', encoding='utf-8') as f:
    json.dump(out_summary, f, ensure_ascii=False, indent=2)

print('Extracted successfully!')
