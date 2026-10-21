using System.IO;

namespace Tianjige.Data.Repositories;

/// <summary>
/// 武将头像直接索引解析器
/// 支持直接按武将名称（简体/繁体）索引 data/avatars/[武将名].bmp，并支持自动繁简转换与多种格式降级
/// </summary>
public class AvatarResolver
{
    private static readonly Dictionary<char, char> TradToSimpMap = new()
    {
        ['喬'] = '乔', ['孫'] = '孙', ['瓚'] = '瓒', ['欽'] = '钦', ['鴦'] = '鸯', ['醜'] = '丑',
        ['儉'] = '俭', ['渾'] = '浑', ['頎'] = '颀', ['濬'] = '浚', ['雙'] = '双', ['師'] = '师',
        ['賢'] = '贤', ['寧'] = '宁', ['豐'] = '丰', ['疇'] = '畴', ['寵'] = '宠', ['長'] = '长',
        ['異'] = '异', ['雋'] = '隽', ['綱'] = '纲', ['輿'] = '舆', ['顏'] = '颜', ['進'] = '进',
        ['儀'] = '仪', ['嬰'] = '婴', ['幹'] = '干', ['興'] = '兴', ['鳳'] = '凤', ['韋'] = '韦',
        ['習'] = '习', ['統'] = '统', ['備'] = '备', ['曄'] = '晔', ['禪'] = '禅', ['義'] = '义',
        ['曠'] = '旷', ['範'] = '范', ['蒙'] = '蒙', ['蘭'] = '兰', ['倉'] = '仓', ['魴'] = '鲂',
        ['淵'] = '渊', ['維'] = '维', ['堅'] = '坚', ['權'] = '权', ['禮'] = '礼', ['靜'] = '静',
        ['優'] = '优', ['獲'] = '获', ['達'] = '达', ['憲'] = '宪', ['勳'] = '勋', ['衛'] = '卫',
        ['寶'] = '宝', ['橫'] = '横', ['濟'] = '济', ['紘'] = '纮', ['紹'] = '绍', ['繡'] = '绣',
        ['遼'] = '辽', ['飛'] = '飞', ['榮'] = '荣', ['質'] = '质', ['純'] = '纯', ['羲'] = '羲',
        ['嚴'] = '严', ['肅'] = '肃', ['鵬'] = '鹏', ['遠'] = '远', ['懷'] = '怀', ['國'] = '国',
        ['訶'] = '诃', ['鵠'] = '鹄', ['瓊'] = '琼', ['誌'] = '志', ['銀'] = '银', ['輅'] = '辂',
        ['兒'] = '儿', ['襲'] = '袭', ['渠'] = '渠', ['術'] = '术', ['熙'] = '熙', ['將'] = '将',
        ['褚'] = '禇', ['誕'] = '诞', ['蟬'] = '蝉', ['褘'] = '祎', ['雲'] = '云', ['廣'] = '广',
        ['評'] = '评', ['結'] = '结', ['會'] = '会', ['澤'] = '泽', ['凱'] = '凯', ['績'] = '绩',
        ['遜'] = '逊', ['宮'] = '宫', ['矯'] = '矫', ['謙'] = '谦', ['銅'] = '铜', ['當'] = '当',
        ['暹'] = '暹', ['騰'] = '腾', ['謖'] = '谡', ['鐵'] = '铁', ['覽'] = '览', ['順'] = '顺',
        ['續'] = '续', ['蓋'] = '盖', ['諸'] = '诸', ['劉'] = '刘', ['關'] = '关', ['張'] = '张',
        ['馬'] = '马', ['黃'] = '黄', ['趙'] = '赵', ['陸'] = '陆', ['陳'] = '陈', ['賈'] = '贾',
        ['許'] = '许', ['袁'] = '袁', ['蔣'] = '蒋', ['韓'] = '韩', ['楊'] = '杨', ['吳'] = '吴',
        ['呂'] = '吕', ['龔'] = '龚', ['鄧'] = '邓', ['車'] = '车', ['鄒'] = '邹', ['鍾'] = '钟',
        ['閻'] = '阎', ['闞'] = '阚', ['環'] = '环', ['帶'] = '带', ['華'] = '华', ['馮'] = '冯',
        ['徹'] = '彻', ['滿'] = '满', ['審'] = '审', ['樂'] = '乐', ['盧'] = '卢', ['簡'] = '简',
        ['龐'] = '庞', ['逢'] = '逄', ['鷺'] = '鹭',
        ['靈'] = '灵', ['譚'] = '谭', ['來'] = '来', ['溫'] = '温', ['魯'] = '鲁', ['闓'] = '闿',
        ['紀'] = '纪', ['圖'] = '图', ['費'] = '费', ['軻'] = '轲', ['鋒'] = '锋', ['詡'] = '诩',
        ['駱'] = '骆', ['鮑'] = '鲍', ['綜'] = '综', ['譙'] = '谯', ['禰'] = '祢', ['蘇'] = '苏',
        ['顧'] = '顾',
        ['號'] = '号', ['斬'] = '斩', ['氣'] = '气', ['衝'] = '冲', ['陣'] = '阵', ['砲'] = '炮',
        ['亂'] = '乱', ['擊'] = '击', ['閃'] = '闪', ['連'] = '连', ['燈'] = '灯', ['門'] = '门',
        ['鎖'] = '锁', ['體'] = '体', ['緩'] = '缓', ['嘯'] = '啸', ['奪'] = '夺', ['點'] = '点',
        ['劍'] = '剑', ['繩'] = '绳', ['縛'] = '缚', ['術'] = '术', ['輪'] = '轮', ['槍'] = '枪',
        ['絕'] = '绝', ['歸'] = '归', ['隱'] = '隐', ['離'] = '离', ['間'] = '间', ['補'] = '补',
        ['減'] = '减', ['動'] = '动', ['濤'] = '涛', ['聖'] = '圣', ['滅'] = '灭', ['殺'] = '杀',
        ['無'] = '无', ['風'] = '风', ['電'] = '电', ['龍'] = '龙', ['熱'] = '热', ['選'] = '选',
        ['驗'] = '验', ['脫'] = '脱', ['殼'] = '壳', ['計'] = '计', ['敵'] = '敌', ['軍'] = '军',
        ['縣'] = '县', ['晉'] = '晋', ['鄴'] = '邺', ['陽'] = '阳', ['農'] = '农', ['涼'] = '凉',
        ['壽'] = '寿', ['業'] = '业', ['廬'] = '庐', ['漢'] = '汉', ['橋'] = '桥', ['壺'] = '壶',
        ['關'] = '关'
    };

    private readonly string _avatarsDirectory;
    private readonly HashSet<string> _availableFiles = new(StringComparer.OrdinalIgnoreCase);

    public AvatarResolver(string avatarsDirectory)
    {
        _avatarsDirectory = avatarsDirectory;
        Refresh();
    }

    public void Refresh()
    {
        _availableFiles.Clear();
        if (Directory.Exists(_avatarsDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(_avatarsDirectory))
            {
                _availableFiles.Add(Path.GetFileName(file));
            }
        }
    }

    /// <summary>
    /// 将武将名称转换为简体
    /// </summary>
    public static string ToSimplified(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        var chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (TradToSimpMap.TryGetValue(chars[i], out var simp))
            {
                chars[i] = simp;
            }
        }
        return new string(chars);
    }

    /// <summary>
    /// 按武将名称直接解析武将头像物理路径
    /// </summary>
    /// <param name="generalName">武将姓名（支持繁体或简体）</param>
    /// <returns>若头像存在返回完整绝对路径，不存在返回 null</returns>
    public string? ResolveByName(string generalName)
    {
        if (string.IsNullOrWhiteSpace(generalName)) return null;

        var cleanName = generalName.Trim();
        var simpName = ToSimplified(cleanName);

        // 优先级：原名.bmp -> 简体.bmp -> 原名.png/jpg -> 简体.png/jpg
        var candidateNames = new[] { cleanName, simpName };
        var extensions = new[] { ".bmp", ".png", ".jpg", ".jpeg" };

        foreach (var ext in extensions)
        {
            foreach (var n in candidateNames)
            {
                var fileName = n + ext;
                if (_availableFiles.Contains(fileName))
                {
                    return Path.Combine(_avatarsDirectory, fileName);
                }
            }
        }

        return null;
    }
}
