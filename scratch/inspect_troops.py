import json, sys
sys.stdout.reconfigure(encoding='utf-8')

with open('data/Jewelry.json', 'r', encoding='utf-8-sig') as f:
    items = json.load(f)

print("=== Items in Jewelry.json (Sequence 1..13: 兵符) ===")
for it in items:
    if it['Sequence'] <= 15:
        print(f"{it['Sequence']:2d} | Name: {it['Name']} | Type: {it['Type']} | Desc: {it['Description']}")
