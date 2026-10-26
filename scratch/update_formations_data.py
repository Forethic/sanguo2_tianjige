import json

formations = [
    {
        "id": 1,
        "name": "方形阵",
        "centerGravity": "四平八稳，中央均衡紧缩（平头阵）",
        "advantages": "克制钩形(+23)、雁形(+19)、玄襄(+13)、圆形(+7)",
        "disadvantages": "受制冲锋(-23)、锥形(-18)、箭矢(-15)、鱼丽(-6)",
        "description": "基础稳健之阵，前后军阵列紧密，侧翼防护严密，完克两翼包抄的凹头阵；但中路单薄，易被尖锋突击阵凿穿。"
    },
    {
        "id": 2,
        "name": "圆形阵",
        "centerGravity": "层层环形护卫，武将深居阵眼（平头阵）",
        "advantages": "克制雁形(+22)、玄襄(+21)、钩形(+12)、鱼丽(+6)",
        "disadvantages": "受制冲锋(-24)、箭矢(-18)、锥形(-12)、方形(-7)",
        "description": "铁桶护卫之阵，兵众层层外扩环绕，对主将保护极佳，全方位克制凹头阵；但面对锋锐突进阵易被直刺阵眼。"
    },
    {
        "id": 3,
        "name": "锥形阵",
        "centerGravity": "前锐后宽，锐角突刺，中军破袭（尖头阵）",
        "advantages": "克制鱼丽(+25)、方形(+18)、圆形(+12)、箭矢(+6)",
        "disadvantages": "受制钩形(-25)、雁形(-19)、玄襄(-11)、冲锋(-7)",
        "description": "尖刀突击阵，前锋集中精锐高速突入，专克平头方圆之阵；但两翼暴露，极易陷入凹头阵的两翼合围与侧切。"
    },
    {
        "id": 4,
        "name": "雁形阵",
        "centerGravity": "人字排开，两翼大角度前突包夹（凹头阵）",
        "advantages": "克制箭矢(+20)、锥形(+19)、冲锋(+15)、钩形(+3)",
        "disadvantages": "受制圆形(-22)、方形(-19)、鱼丽(-11)、玄襄(-5)",
        "description": "两翼大开之合围阵，适合两翼火力覆盖与交叉合围，专克锥形、箭矢等尖头突击；但阵线过长易被平阵正面推散。"
    },
    {
        "id": 5,
        "name": "玄襄阵",
        "centerGravity": "多疑奇谋，错落掩护，虚实诱敌（凹头阵）",
        "advantages": "克制冲锋(+24)、箭矢(+19)、锥形(+11)、雁形(+5)",
        "disadvantages": "受制圆形(-21)、鱼丽(-18)、方形(-13)、钩形(-6)",
        "description": "兵书疑兵之阵，旗幡交错虚张声势，令冲锋、箭矢等无脑直线突进阵迷失受挫；但缺乏正面厚度，受平头阵碾压。"
    },
    {
        "id": 6,
        "name": "鱼丽阵",
        "centerGravity": "车步相掩，前后梯次，连绵波次（平头阵）",
        "advantages": "克制钩形(+23)、玄襄(+18)、雁形(+11)、方形(+6)",
        "disadvantages": "受制锥形(-25)、箭矢(-17)、冲锋(-10)、圆形(-6)",
        "description": "古典战车步兵梯次阵，前后军波次掩护，攻势绵密持久，压制凹头各阵；但前线平宽，受锥形强力突破克制。"
    },
    {
        "id": 7,
        "name": "钩形阵",
        "centerGravity": "正面列盾，两翼内钩，口袋陷阱（凹头阵）",
        "advantages": "克制锥形(+25)、箭矢(+15)、冲锋(+14)、玄襄(+6)",
        "disadvantages": "受制方形(-23)、鱼丽(-23)、圆形(-12)、雁形(-3)",
        "description": "倒钩口袋阵，专门为诱捕尖锋突击而设，对锥形有最高+25克制加成；但面对平头正规军正面推进时极为被动。"
    },
    {
        "id": 8,
        "name": "冲锋阵",
        "centerGravity": "全军冲锋，兵分两路两翼直奔敌将（尖头阵）",
        "advantages": "克制圆形(+24)、方形(+23)、鱼丽(+10)、锥形(+7)",
        "disadvantages": "受制玄襄(-24)、雁形(-15)、钩形(-14)、箭矢(-12)",
        "description": "孤注一掷之奔袭阵，两翼兵锋直插敌将阵眼，极大克制平头防守阵；但一旦撞入凹头陷阱阵容易被侧翼围剿斩断。"
    },
    {
        "id": 9,
        "name": "箭矢阵",
        "centerGravity": "长矢贯日，箭头聚集高杀伤精锐（尖头阵）",
        "advantages": "克制圆形(+18)、鱼丽(+17)、方形(+15)、冲锋(+12)",
        "disadvantages": "受制雁形(-20)、玄襄(-19)、钩形(-15)、锥形(-6)",
        "description": "疾风迅雷突杀阵，单点施压极大，擅长以点破面击溃平直阵线；但阵尾狭长，极易被雁形与玄襄两翼断后。"
    }
]

# Read data/Formations.json
with open('data/Formations.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

data['formations'] = formations

# Fix chantQuote and characteristics for troopCounters
for tc in data['troopCounters']:
    name = tc['troopName']
    if name == '藤甲兵':
        tc['strongAgainst'] = ["飞刀兵", "弓箭兵", "朴刀兵", "女兵", "黄巾兵", "弩兵"]
        tc['weakAgainst'] = ["大刀兵", "长枪兵", "链锤兵", "铁锤兵", "蛮族兵", "武斗兵"]
        tc['chantQuote'] = "藤甲坚韧箭难透，生克飞弓莫对枪。"
        tc['characteristics'] = "【高防短兵】刀枪难入、极抗远程飞箭与飞刀投掷；但受长枪、大刀与链锤等长重兵器严重穿刺劈斩克制。"
    elif name == '飞刀兵':
        tc['strongAgainst'] = ["大刀兵", "弩兵", "长枪兵", "链锤兵", "铁锤兵"]
        tc['weakAgainst'] = ["藤甲兵", "武斗兵", "朴刀兵", "蛮族兵", "女兵", "黄巾兵"]
        tc['chantQuote'] = "飞刀爆的链锤惨，远避藤甲近身斩。"
        tc['characteristics'] = "【特种远程】中距离飞刃强破长重兵器；但面对高防贴身短兵（尤其是藤甲兵与武斗兵）极度劣势。"
    elif name == '大刀兵':
        tc['chantQuote'] = "大刀对女是短板，重劈藤甲与长枪。"
    elif name == '黄巾兵':
        tc['chantQuote'] = "黄巾只克女飞蛮，正规军阵势如破竹。"
    elif name == '武斗兵':
        tc['chantQuote'] = "武斗不惧锤和缒，近破飞弓震敌胆。"
    elif name == '弩兵':
        tc['chantQuote'] = "弩兵全能很能干，碰到飞弓就完蛋。"

# Write to data/Formations.json and src/Tianjige.Data/Resources/Formations.json
for target_path in ['data/Formations.json', 'src/Tianjige.Data/Resources/Formations.json']:
    with open(target_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

print("Formations and troop data updated successfully to 9 formations!")
