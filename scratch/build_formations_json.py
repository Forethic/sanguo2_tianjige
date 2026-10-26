import json

# 13x13 matrix
# Positive number = Row beats Column
troops_order = [
    "朴刀兵", "长枪兵", "大刀兵", "弓箭兵", "链锤兵", "飞刀兵", 
    "武斗兵", "蛮族兵", "铁锤兵", "藤甲兵", "黄巾兵", "弩兵", "女兵"
]

# Raw matrix from Sango 2:
# In the original table:
# negative was disadvantage in some tables, or let's verify signs:
# In Sango 2:
# 朴刀 vs 弓箭: 朴刀 overcomes 弓箭 (+44)
# 朴刀 vs 长枪: 长枪 overcomes 朴刀 (-42)
# 长枪 vs 朴刀: +42
matrix_data = {
    "朴刀兵": {
        "朴刀兵": 0, "长枪兵": -42, "大刀兵": -58, "弓箭兵": 44, "链锤兵": -47, "飞刀兵": 47,
        "武斗兵": 21, "蛮族兵": 14, "铁锤兵": -51, "藤甲兵": -31, "黄巾兵": 22, "弩兵": -8, "女兵": 10
    },
    "长枪兵": {
        "朴刀兵": 42, "长枪兵": 0, "大刀兵": -12, "弓箭兵": -58, "链锤兵": -23, "飞刀兵": -53,
        "武斗兵": 60, "蛮族兵": 49, "铁锤兵": 27, "藤甲兵": 66, "黄巾兵": 44, "弩兵": -52, "女兵": 39
    },
    "大刀兵": {
        "朴刀兵": 58, "长枪兵": 12, "大刀兵": 0, "弓箭兵": -66, "链锤兵": 22, "飞刀兵": -64,
        "武斗兵": 63, "蛮族兵": 53, "铁锤兵": -14, "藤甲兵": 68, "黄巾兵": 68, "弩兵": -18, "女兵": -43
    },
    "弓箭兵": {
        "朴刀兵": -44, "长枪兵": 58, "大刀兵": 66, "弓箭兵": 0, "链锤兵": 54, "飞刀兵": 8,
        "武斗兵": -56, "蛮族兵": -36, "铁锤兵": 47, "藤甲兵": -54, "黄巾兵": 28, "弩兵": 32, "女兵": -29
    },
    "链锤兵": {
        "朴刀兵": 47, "长枪兵": 23, "大刀兵": -22, "弓箭兵": -54, "链锤兵": 0, "飞刀兵": -48,
        "武斗兵": -26, "蛮族兵": 36, "铁锤兵": 13, "藤甲兵": 65, "黄巾兵": 37, "弩兵": -45, "女兵": 32
    },
    "飞刀兵": {
        "朴刀兵": -47, "长枪兵": 53, "大刀兵": 64, "弓箭兵": -8, "链锤兵": 48, "飞刀兵": 0,
        "武斗兵": -55, "蛮族兵": -39, "铁锤兵": 39, "藤甲兵": -67, "黄巾兵": -24, "弩兵": 56, "女兵": -32
    },
    "武斗兵": {
        "朴刀兵": -21, "长枪兵": -60, "大刀兵": -63, "弓箭兵": 56, "链锤兵": 26, "飞刀兵": 55,
        "武斗兵": 0, "蛮族兵": 8, "铁锤兵": 42, "藤甲兵": 16, "黄巾兵": 33, "弩兵": -18, "女兵": 6
    },
    "蛮族兵": {
        "朴刀兵": -14, "长枪兵": -49, "大刀兵": -53, "弓箭兵": 36, "链锤兵": -36, "飞刀兵": 39,
        "武斗兵": -8, "蛮族兵": 0, "铁锤兵": -41, "藤甲兵": 17, "黄巾兵": -62, "弩兵": -10, "女兵": 30
    },
    "铁锤兵": {
        "朴刀兵": 51, "长枪兵": -27, "大刀兵": 14, "弓箭兵": -47, "链锤兵": -13, "飞刀兵": -39,
        "武斗兵": -42, "蛮族兵": 41, "铁锤兵": 0, "藤甲兵": 64, "黄巾兵": 41, "弩兵": -28, "女兵": 36
    },
    "藤甲兵": {
        "朴刀兵": 31, "长枪兵": -66, "大刀兵": -68, "弓箭兵": 54, "链锤兵": -65, "飞刀兵": 67,
        "武斗兵": -16, "蛮族兵": -17, "铁锤兵": -64, "藤甲兵": 0, "黄巾兵": 21, "弩兵": 8, "女兵": 26
    },
    "黄巾兵": {
        "朴刀兵": -22, "长枪兵": -44, "大刀兵": -68, "弓箭兵": -28, "链锤兵": -37, "飞刀兵": 24,
        "武斗兵": -33, "蛮族兵": 62, "铁锤兵": -41, "藤甲兵": -21, "黄巾兵": 0, "弩兵": -24, "女兵": 56
    },
    "弩兵": {
        "朴刀兵": 8, "长枪兵": 52, "大刀兵": 18, "弓箭兵": -32, "链锤兵": 45, "飞刀兵": -56,
        "武斗兵": 18, "蛮族兵": 10, "铁锤兵": 28, "藤甲兵": -8, "黄巾兵": 24, "弩兵": 0, "女兵": 12
    },
    "女兵": {
        "朴刀兵": -10, "长枪兵": -39, "大刀兵": 43, "弓箭兵": 29, "链锤兵": -32, "飞刀兵": 32,
        "武斗兵": -6, "蛮族兵": -30, "铁锤兵": -36, "藤甲兵": -26, "黄巾兵": -56, "弩兵": -12, "女兵": 0
    }
}

troop_meta = {
    "长枪兵": {
        "category": 1,
        "characteristics": "【长兵主力】阵战长枪如林，完克大部分短兵器，但极为畏惧弓弩远程箭雨齐射。",
        "chantQuote": "长枪刺步勇无双，远避飞矢莫当弓。"
    },
    "朴刀兵": {
        "category": 2,
        "characteristics": "【短兵之王】重盾护体，完克所有远程弓箭飞刀，但面对长枪、大刀等长器械极为吃力。",
        "chantQuote": "朴刀坚盾逼弓弩，最惧长枪大刀劈。"
    },
    "大刀兵": {
        "category": 1,
        "characteristics": "【重斩长兵】大开大合破阵斩将，对长枪短兵压制力惊人，唯独面对女兵与弓矢处于劣势。",
        "chantQuote": "大刀对女是短板，飞刀爆的链锤惨。"
    },
    "弓箭兵": {
        "category": 3,
        "characteristics": "【常规远兵】超远距离精准齐射，长兵器克星，但一旦被朴刀或武斗兵近身则毫无还手之力。",
        "chantQuote": "远克长，长克短，短兵又克远程兵。"
    },
    "链锤兵": {
        "category": 1,
        "characteristics": "【回旋重器】回旋铁锤范围杀伤，碾压朴刀与藤甲，但在飞刀手与武斗兵面前破绽明显。",
        "chantQuote": "飞刀爆的链锤惨，回旋长兵莫轻敌。"
    },
    "铁锤兵": {
        "category": 1,
        "characteristics": "【破甲重兵】势大力沉重击破盾，重创藤甲与朴刀，受长枪、飞刀与灵敏武斗克制。",
        "chantQuote": "铁锤重碾破藤甲，难敌长枪疾风刺。"
    },
    "飞刀兵": {
        "category": 3,
        "characteristics": "【疾速投掷】中程致命飞刃，对长枪、大刀、链锤有毁灭性杀伤，但被朴刀铁盾完美克制。",
        "chantQuote": "飞刀爆的链锤惨，遇到朴刀盾成空。"
    },
    "武斗兵": {
        "category": 2,
        "characteristics": "【高敏近搏】近身肉搏格斗大师，不惧各类重锤武器，克制弓矢，但难以正面硬撼长刀枪。",
        "chantQuote": "武斗不惧锤和缒，弓矢难防贴身拳。"
    },
    "蛮族兵": {
        "category": 2,
        "characteristics": "【蛮勇强魄】南蛮死士肉身勇悍，强攻弓手与飞刀，但在正规军阵前破绽较大。",
        "chantQuote": "蛮族嗜血破弓矢，正规枪刀破蛮勇。"
    },
    "藤甲兵": {
        "category": 2,
        "characteristics": "【油浸坚藤】强防韧甲，刀箭难透，对远程和朴刀占优，但被长兵器正面捅穿。",
        "chantQuote": "藤甲坚固拒弓矢，长枪大刀穿心透。"
    },
    "女兵": {
        "category": 2,
        "characteristics": "【轻盈奇袭】动作敏捷，出奇制胜，对大刀兵与弓手有特异奇效，但综合防御单薄。",
        "chantQuote": "大刀对女是短板，灵巧翩若惊鸿影。"
    },
    "弩兵": {
        "category": 3,
        "characteristics": "【劲弩齐发】穿透力极强的多面手，打击面宽广，但在飞刀与弓箭互射对攻中处于下风。",
        "chantQuote": "弩兵全能很能干，碰到飞弓就完蛋。"
    },
    "黄巾兵": {
        "category": 4,
        "characteristics": "【狂热民兵】黄巾义众，单兵战力孱弱，但专克女兵、飞刀与蛮族，容易被正规兵平推。",
        "chantQuote": "黄巾只克女飞蛮，正规军阵势如破竹。"
    }
}

troop_counters = []
for name, meta in troop_meta.items():
    matrix = matrix_data[name]
    strong = [opp for opp, score in matrix.items() if score > 0]
    # sort strong by score descending
    strong.sort(key=lambda opp: matrix[opp], reverse=True)
    weak = [opp for opp, score in matrix.items() if score < 0]
    # sort weak by score ascending (most negative first)
    weak.sort(key=lambda opp: matrix[opp])
    
    troop_counters.append({
        "troopName": name,
        "category": meta["category"],
        "strongAgainst": strong,
        "weakAgainst": weak,
        "advantageMatrix": matrix,
        "characteristics": meta["characteristics"],
        "chantQuote": meta["chantQuote"]
    })

# Existing 8 formations
formations = [
    {
        "id": 1,
        "name": "方形阵",
        "centerGravity": "四平八稳，中央均衡紧缩",
        "advantages": "克制冲阵、锥形阵，侧翼防护严密，不易被分割",
        "disadvantages": "对抗圆形阵、鱼丽阵较平庸，推进速度慢",
        "description": "基础稳健之阵，前后军阵列紧密，适合防守反击与步步为营。"
    },
    {
        "id": 2,
        "name": "圆形阵",
        "centerGravity": "层层环形护卫，武将深居阵眼",
        "advantages": "对武将护卫极佳，极大克制冲阵，防箭射击极优",
        "disadvantages": "攻击面分散，缺乏集中攻坚穿透力，易被一字阵拉扯",
        "description": "铁桶防护之阵，兵众层层外扩环绕，有效抵御四面奔袭包抄。"
    },
    {
        "id": 3,
        "name": "锥形阵",
        "centerGravity": "前锐后宽，锐角突刺，中军破袭",
        "advantages": "克制圆形阵、一字阵，正中央突破撕扯敌方中路",
        "disadvantages": "被方形阵、雁形阵两翼包夹克制，两翼兵力薄弱",
        "description": "冲锋突破型锋芒阵，集中尖刀力量快速突入敌将本阵。"
    },
    {
        "id": 4,
        "name": "雁形阵",
        "centerGravity": "人字排开，两翼大角度前突包夹",
        "advantages": "克制锥形阵、箭形阵，两翼弓弩交叉火力极猛",
        "disadvantages": "中军接敌较早，被一字阵与冲阵中路突入克制",
        "description": "宽幅大翼阵，适合远程齐射与合围切割敌军两翼。"
    },
    {
        "id": 5,
        "name": "鱼丽阵",
        "centerGravity": "梯次梯阵，前后梯次相掩，车步相配",
        "advantages": "克制方形阵、圆形阵，前后梯次连续波次攻击",
        "disadvantages": "机动变阵迟缓，被两翼快速兵力包抄时易脱节",
        "description": "古典战车步兵梯次阵，前后军彼此掩护递进，攻势绵密持久。"
    },
    {
        "id": 6,
        "name": "箭形阵",
        "centerGravity": "箭杆长驱，箭头聚集高杀伤精锐",
        "advantages": "克制雁形阵，直线突进势如破竹，斩将速度快",
        "disadvantages": "被方形阵、冲阵正面截断，阵线过长易溃散",
        "description": "疾风迅雷之突杀阵，单点施压极大，擅长直取中军上将首级。"
    },
    {
        "id": 7,
        "name": "一字阵",
        "centerGravity": "横向一字排开，阵面极为宽阔",
        "advantages": "克制雁形阵、冲阵，阵线拉伸极大，极易绕后偷袭敌将",
        "disadvantages": "纵深单薄，被锥形阵、箭形阵中央击穿则两截不顾",
        "description": "大开大合之平推阵，全军一线并进，拉扯战线制造单挑空隙。"
    },
    {
        "id": 8,
        "name": "冲阵",
        "centerGravity": "全军冲锋，兵分两路两翼直奔敌将",
        "advantages": "克制一字阵、箭形阵，快速越过敌方小兵强杀主将",
        "disadvantages": "极大被圆形阵、方形阵克制，正面极易撞上密集群撞死",
        "description": "孤注一掷之奔袭阵，兵锋直指敌将，适合敌将体弱残血时强斩。"
    }
]

out = {
    "formations": formations,
    "troopCounters": troop_counters
}

for target in ['src/Tianjige.Data/Resources/Formations.json', 'data/Formations.json']:
    with open(target, 'w', encoding='utf-8') as f:
        json.dump(out, f, ensure_ascii=False, indent=2)
    print(f'Wrote {target} with {len(troop_counters)} troops.')
