using UnityEngine;

public enum HexDirection
{
    NE, E, SE, SW, W, NW
}

public static class HexDirectionExtensions // HexDirection的扩展方法
{
    public static HexDirection Opposite(this HexDirection direction)
    {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }
    public static HexDirection Previous(this HexDirection direction)
    {
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }
    public static HexDirection Next(this HexDirection direction)
    {
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }
    public static HexDirection Previous2(this HexDirection direction)
    {
        direction -= 2;
        return direction >= HexDirection.NE ? direction : (direction + 6);
    }
    public static HexDirection Next2(this HexDirection direction)
    {
        direction += 2;
        return direction <= HexDirection.NW ? direction : (direction - 6);
    }
}

public enum HexEdgeType
{
    Flat, Slope, Cliff
}

public struct HexHash
{
    public float a, b, c, d, e;
    public static HexHash Create()
    {
        HexHash hash;
        hash.a = Random.value * 0.999f;
        hash.b = Random.value * 0.999f;
        hash.c = Random.value * 0.999f;
        hash.d = Random.value * 0.999f;
        hash.e = Random.value * 0.999f;
        return hash;
    }
}

public static class HexMetrics
{
    public const float outerToInner = 0.866025404f;
    public const float innerToOuter = 1f / outerToInner;
    public const float outerRadius = 10f;
    public const float innerRadius = outerRadius * outerToInner;

    public const float solidFactor = 0.8f;
    public const float blendFactor = 1f - solidFactor;

    public const float elevationStep = 0.3f * outerRadius; // 地形抬升的步长

    public const int terracesPerSlope = 2; //平地数
    public const int terraceSteps = terracesPerSlope * 2 + 1; //平地+斜坡总步数
    public const float horizontalTerraceStepSize = 1f / terraceSteps; // 步长 占 总坡长 比例
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1); // 坡高 占 总坡高 的比例

    public static Texture2D noiseSource; // 纹理图片的引用
    public const float cellPerturbStrength = 0.2f * outerRadius; // cell扰动强度
    public const float noiseScale = 0.03f / outerRadius; // 采样缩放比例
    public const float elevationPerturbStrength = 0.07f * outerRadius; // 高度扰动强度

    public const int chunkSizeX = 5, chunkSizeZ = 5; // 每个mesh块的长和宽
    public const int borderCellCountX = 10, borderCellCountZ = 10; // 边界补充的cell数目
    public const int AC_offsetX = 9, AC_offsetZ = 17;

    public static int[][] abyssCoords = {
        new int[] {-8, -6, 14}, new int[] {-7, -7, 14}, new int[] {-6, -8, 14},
        new int[] {-8, -5, 13}, new int[] {-7, -6, 13}, new int[] {-6, -7, 13}, new int[] {-5, -8, 13},
        new int[] {-6, -5, 11}, new int[] {-5, -5, 10}, new int[] {-5, -4, 9}, new int[] {-4, -5, 9}, new int[] {-4, -4, 8},
        new int[] {-3, -2, 5}, new int[] {-2, -2, 4}, new int[] {-2, -1, 3}, new int[] {-1, -2, 3},
        new int[] {-1, 0, 1}, new int[] {0, -1, 1}, new int[] {-1, 1, 0}, new int[] {0, 0, 0}, new int[]{1, -1, 0},
        new int[] {0, 1, -1}, new int[] {1, 0, -1},
        new int[] {1, 2, -3}, new int[] {2, 1, -3}, new int[] {2, 2, -4}, new int[] {3, 2, -5},
        new int[] {4, 4, -8}, new int[] {4, 5, -9}, new int[] {5, 4, -9}, new int[] {5, 5, -10}, new int[] {6, 5, -11},
        new int[] {5, 8, -13}, new int[] {6, 7, -13}, new int[] {7, 6, -13}, new int[] {8, 5, -13},
        new int[] {6, 8, -14}, new int[] {7, 7, -14}, new int[] {8, 6, -14}
    };
    public static int[][][] stationCoords = {
        new int[][] { new int[] { -6, -6, 12 }, new int[] { -7, -5, 12 }, new int[] { -5, -7, 12 }, new int[] { -5, -6, 11 } },
        new int[][] { new int[] { 0, -5, 5 }, new int[] { -1, -5, 6 }, new int[] { -1, -4, 5 }, new int[] { 0, -4, 4 } },
        new int[][] { new int[] { 0, 5, -5 }, new int[] { 0, 4, -4 }, new int[] { 1, 4, -5 }, new int[] { 1, 5, -6 } },
        new int[][] { new int[] { 6, 6, -12 }, new int[] { 5, 6, -11 }, new int[] { 5, 7, -12 }, new int[] { 7, 5, -12 } }
    };
    public static int[][] summonPointCoords = {
        new int[] { -8, 6, 2 }, new int[] { -7, 6, 1 }, new int[] { -6, 6, 0 }, new int[] { -6, 7, -1 },
        new int[] { -6, 8, -2 }, new int[] { 6, -8, 2 }, new int[] { 6, -7, 1 }, new int[] { 6, -6, 0 },
        new int[] { 7, -6, -1 }, new int[] { 8, -6, -2 }, new int[] { -7, -5, 12 }, new int[] { -5, -7, 12 },
        new int[] { -5, -6, 11 }, new int[] { -1, -5, 6 }, new int[] { -1, -4, 5 }, new int[] { 0, -4, 4 },
        new int[] { 0, 4, -4 }, new int[] { 1, 4, -5 }, new int[] { 1, 5, -6 }, new int[] { 5, 6, -11 },
        new int[] { 5, 7, -12 }, new int[] { 7, 5, -12 }
    };

    public static string[] mapName = {
        "standard004", "standard005"
    };
    public static int[] matIndex = {
        0, 0
    };

    /// <summary>
    /// 生物的八个属性
    /// </summary>
    // 费用/攻击/最大生命/攻击最小范围-最大范围/行动力/冷却/生物槽容量（三等级分别代表到达初始/51回合/76回合）
    public static int[][][] unitProperty = {
        new int[][] { // 神迹
            new int[] { 0, 0, 30, 0, 0, 0, 0, 1 },
            new int[] { 0, 0, 30, 0, 0, 0, 0, 1 },
            new int[] { 0, 0, 30, 0, 0, 0, 0, 1 }
        },
        new int[][] { // 1 剑士
            new int[] { 2, 2, 2, 1, 1, 3, 2, 6 },
            new int[] { 4, 4, 4, 1, 1, 3, 2, 7 },
            new int[] { 6, 6, 6, 1, 1, 3, 3, 8 }
        },
        new int[][] { // 2 弓箭手
            new int[] { 2, 1, 2, 3, 4, 3, 4, 3 },
            new int[] { 4, 2, 3, 3, 4, 3, 4, 4 },
            new int[] { 6, 3, 4, 3, 4, 3, 4, 5 }
        },
        new int[][] { // 3 黑蝙蝠
            new int[] { 2, 1, 1, 0, 1, 4, 3, 3 },
            new int[] { 3, 2, 1, 0, 1, 4, 3, 4 },
            new int[] { 6, 4, 2, 0, 1, 5, 4, 5 }
        },
        new int[][] { // 4 牧师
            new int[] { 2, 0, 3, 0, 1, 5, 3, 3 },
            new int[] { 4, 0, 4, 0, 1, 5, 3, 4 },
            new int[] { 7, 0, 6, 0, 2, 5, 5, 4 }
        },
        new int[][] { // 5 火山之龙
            new int[] { 5, 3, 5, 1, 2, 2, 5, 3 },
            new int[] { 7, 4, 7, 1, 2, 2, 5, 4 },
            new int[] { 9, 5, 9, 1, 2, 2, 5, 5 }
        },
        new int[][] { // 6 冰霜之龙
            new int[] { 5, 3, 4, 0, 2, 2, 4, 3 },
            new int[] { 7, 4, 6, 0, 2, 2, 4, 4 },
            new int[] { 9, 5, 8, 0, 2, 2, 5, 5 }
        },
        new int[][] { // 7 地狱火焰兽
            new int[] { 0, 8, 12, 0, 1, 3, 0, 0 },
            new int[] { 0, 8, 12, 0, 1, 3, 0, 0 },
            new int[] { 0, 8, 12, 0, 1, 3, 0, 0 }
        },
        new int[][] { // ??
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 }
        },
        new int[][] { // ??
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 }
        },
        new int[][] { // ??
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 }
        },
        new int[][] { // 11 剑士
            new int[] { 2, 2, 2, 1, 1, 3, 2, 6 },
            new int[] { 4, 4, 4, 1, 1, 3, 2, 7 },
            new int[] { 6, 6, 6, 1, 1, 3, 3, 8 }
        },
        new int[][] { // 12 弓箭手
            new int[] { 2, 1, 2, 3, 4, 3, 4, 3 },
            new int[] { 4, 2, 3, 3, 4, 3, 4, 4 },
            new int[] { 6, 3, 4, 3, 4, 3, 4, 5 }
        },
        new int[][] { // 13 黑蝙蝠
            new int[] { 2, 1, 1, 0, 1, 4, 3, 3 },
            new int[] { 3, 2, 1, 0, 1, 4, 3, 4 },
            new int[] { 6, 4, 2, 0, 1, 5, 4, 5 }
        },
        new int[][] { // 14 牧师
            new int[] { 2, 0, 3, 0, 1, 5, 3, 3 },
            new int[] { 4, 0, 4, 0, 1, 5, 3, 4 },
            new int[] { 7, 0, 6, 0, 2, 5, 5, 4 }
        },
        new int[][] { // 15 火山之龙
            new int[] { 5, 3, 5, 1, 2, 2, 5, 3 },
            new int[] { 7, 4, 7, 1, 2, 2, 5, 4 },
            new int[] { 9, 5, 9, 1, 2, 2, 5, 5 }
        },
        new int[][] { // 16 冰霜之龙
            new int[] { 5, 3, 4, 0, 2, 2, 4, 3 },
            new int[] { 7, 4, 6, 0, 2, 2, 4, 4 },
            new int[] { 9, 5, 8, 0, 2, 2, 5, 5 }
        },
        new int[][] { // 17 地狱火焰兽
            new int[] { 0, 8, 12, 0, 1, 3, 0, 0 },
            new int[] { 0, 8, 12, 0, 1, 3, 0, 0 },
            new int[] { 0, 8, 12, 0, 1, 3, 0, 0 }
        },
        new int[][] { // ??
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 }
        },
        new int[][] { // ??
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 }
        },
        new int[][] { // ??
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 0, 0, 0, 0, 0 }
        },
    };

    // 生物的名字
    public static string[] unitName = new string[] {
        "Miracle",
        "Swordsman",
        "Archer",
        "BlackBat",
        "Priest",
        "VolcanoDragon",
        "FrostDragon",
        "???",
        "???",
        "???",
        "???",
        "Swordsman",
        "Archer",
        "BlackBat",
        "Priest",
        "VolcanoDragon",
        "FrostDragon",
        "???",
        "???",
        "???",
        "???"
    };

    // 生物中文名字
    public static string[] unitNameZH_CN = new string[] {
        "神迹",
        "剑士",
        "弓箭手",
        "黑蝙蝠",
        "牧师",
        "火山之龙",
        "冰霜之龙",
        "衍生的地狱火",
        "???",
        "???",
        "???",
        "剑士",
        "弓箭手",
        "黑蝙蝠",
        "牧师",
        "火山之龙",
        "冰霜之龙",
        "衍生的地狱火",
        "???",
        "???",
        "???"
    };

    public static string[][] unitEntry = new string[][] {
        new string[] { "", "", "" }, // 0 Miracle
        new string[] { // 1 剑士
            "",
            "",
            ""
        },
        new string[] { // 2 弓兵
            "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>对空</b>：可攻击地面生物、飞行生物"
        },
        new string[] { // 3 黑蝙蝠
            "<b>飞行</b>：是飞行生物，可攻击地面生物、飞行生物",
            "<b>飞行</b>：是飞行生物，可攻击地面生物、飞行生物",
            "<b>飞行</b>：是飞行生物，可攻击地面生物、飞行生物"
        },
        new string[] { // 4 牧师
            "<b>触发</b>：己方回合结束时，范围2内的友方生物回复1生命；\n" +
                "<b>光环</b>：范围2内的友方生物攻击力+1\n" +
                "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>触发</b>：己方回合结束时，范围3内的友方生物回复1生命；\n" +
                "<b>光环</b>：范围3内的友方生物攻击力+1\n" +
                "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>触发</b>：己方回合结束时，范围3内的友方生物回复1生命；\n" +
                "<b>光环</b>：范围3内的友方生物攻击力+1\n" +
                "<b>对空</b>：可攻击地面生物、飞行生物"
        },
        new string[] { // 5 火山之龙
            "<b>触发</b>：攻击后，溅射处于与自身距离为2且与攻击目标距离为1的生物，造成3点伤害",
            "<b>触发</b>：攻击后，溅射处于与自身距离为2且与攻击目标距离为1的生物，造成4点伤害",
            "<b>触发</b>：攻击后，溅射处于与自身距离为2且与攻击目标距离为1的生物，造成5点伤害"
        },
        new string[] { // 6 冰霜之龙
            "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>对空</b>：可攻击地面生物、飞行生物"
        },
        new string[] { // 7 衍生的地狱火
            "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>对空</b>：可攻击地面生物、飞行生物"
        },
        new string[] { // ???
            "???", "???", "???"
        },
        new string[] { // ???
            "???", "???", "???"
        },
        new string[] { // ???
            "???", "???", "???"
        },
        new string[] { // 11 剑士
            "",
            "",
            ""
        },
        new string[] { // 12 弓兵
            "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>对空</b>：可攻击地面生物、飞行生物"
        },
        new string[] { // 13 黑蝙蝠
            "<b>飞行</b>：是飞行生物，可攻击地面生物、飞行生物",
            "<b>飞行</b>：是飞行生物，可攻击地面生物、飞行生物",
            "<b>飞行</b>：是飞行生物，可攻击地面生物、飞行生物"
        },
        new string[] { // 14 牧师
            "<b>触发</b>：己方回合结束时，范围2内的友方生物回复1生命；\n" +
                "<b>光环</b>：范围2内的友方生物攻击力+1\n" +
                "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>触发</b>：己方回合结束时，范围3内的友方生物回复1生命；\n" +
                "<b>光环</b>：范围3内的友方生物攻击力+1\n" +
                "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>触发</b>：己方回合结束时，范围3内的友方生物回复1生命；\n" +
                "<b>光环</b>：范围3内的友方生物攻击力+1\n" +
                "<b>对空</b>：可攻击地面生物、飞行生物"
        },
        new string[] { // 15 火山之龙
            "<b>触发</b>：攻击后，溅射处于与自身距离为2且与攻击目标距离为1的生物，造成3点伤害",
            "<b>触发</b>：攻击后，溅射处于与自身距离为2且与攻击目标距离为1的生物，造成4点伤害",
            "<b>触发</b>：攻击后，溅射处于与自身距离为2且与攻击目标距离为1的生物，造成5点伤害"
        },
        new string[] { // 16 冰霜之龙
            "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>对空</b>：可攻击地面生物、飞行生物"
        },
        new string[] { // 17 衍生的地狱火
            "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>对空</b>：可攻击地面生物、飞行生物",
            "<b>对空</b>：可攻击地面生物、飞行生物"
        },
        new string[] { // ???
            "???", "???", "???"
        },
        new string[] { // ???
            "???", "???", "???"
        },
        new string[] { // ???
            "???", "???", "???"
        }
    };

    public static bool isFlying(int unitType)
    {
        if (unitType == 3 || unitType == 13) {
            return true;
        }
        else return false;
    }

    public static string[][] unitDiscription = new string[][] {
        new string[] { "", "", "" }, // 0 Miracle
        new string[] { // 1 剑士
            "新兵们，拿好你们的剑盾！",
            "忠诚的勇士，一切为了国王！",
            "身经百战的勇士，不惧一切敌人！"
        },
        new string[] { // 2 弓兵
            "老实说，呃，你能不能乖乖当个靶子？",
            "我射中了！那个，是不是有点疼？",
            "哦，我看到了你的弱点。小心，箭马上就到！"
        },
        new string[] { // 3 黑蝙蝠
            "这是军用蝙蝠，禁止食用！",
            "它是怎么被养到这么大的？",
            "它锋利的爪子能轻易撕下你的血肉。"
        },
        new string[] { // 4 牧师
            "这是主的恩泽，不是医术！",
            "这是主的祝福，你将更有力量！",
            "在主的光辉下，你已经无坚不摧了！"
        },
        new string[] { // 5 火山之龙
            "熔岩之力，焚烧一切！",
            "熔岩之力，熔化一切！",
            "熔岩之力，毁灭一切！"
        },
        new string[] { // 6 冰霜之龙
            "即使站在身边，也有刺骨寒意",
            "至柔之水，至坚之冰",
            "别靠近，它在“烧”"
        },
        new string[] { // 7 衍生的地狱火
            "%#$@%%!!",
            "%#$@%%!!",
            "%#$@%%!!"
        },
        new string[] { // ???
            "???", "???", "???"
        },
        new string[] { // ???
            "???", "???", "???"
        },
        new string[] { // ???
            "???", "???", "???"
        },
        new string[] { // 11 剑士
            "新兵们，拿好你们的剑盾！",
            "忠诚的勇士，一切为了国王！",
            "身经百战的勇士，不惧一切敌人！"
        },
        new string[] { // 12 弓兵
            "老实说，呃，你能不能乖乖当个靶子？",
            "我射中了！那个，是不是有点疼？",
            "哦，我看到了你的弱点。小心，箭马上就到！"
        },
        new string[] { // 13 黑蝙蝠
            "这是军用蝙蝠，禁止食用！",
            "它是怎么被养到这么大的？",
            "它锋利的爪子能轻易撕下你的血肉。"
        },
        new string[] { // 14 牧师
            "这是主的恩泽，不是医术！",
            "这是主的祝福，你将更有力量！",
            "在主的光辉下，你已经无坚不摧了！"
        },
        new string[] { // 15 火山之龙
            "熔岩之力，焚烧一切！",
            "熔岩之力，熔化一切！",
            "熔岩之力，毁灭一切！"
        },
        new string[] { // 16 冰霜之龙
            "即使站在身边，也有刺骨寒意",
            "至柔之水，至坚之冰",
            "别靠近，它在“烧”"
        },
        new string[] { // 17 衍生的地狱火
            "%#$@%%!!",
            "%#$@%%!!",
            "%#$@%%!!"
        },
        new string[] { // ???
            "???", "???", "???"
        },
        new string[] { // ???
            "???", "???", "???"
        },
        new string[] { // ???
            "???", "???", "???"
        }
    };

    /// <summary>
    /// 神器的属性（从 1 开始）
    /// </summary>
    /// 费用/冷却
    public static int[][] atfProperty = {
        new int[] {-1, -1},
        new int[] {6, 5},
        new int[] {6, 4},
        new int[] {8, 6},
        new int[] {8, 12}
    };

    // 神器的名字
    public static string[] atfName = new string[] {
        "???",
        "HolyLight",
        "SalamanderShield",
        "InfernoFlame",
        "WindBlessing"
    };

    // 神器的名字
    public static string[] atfNameZH_CN = new string[] {
        "???",
        "塞瓦哈拉的圣光之耀",
        "马尔瑞恩的阳炎之盾",
        "洛古萨斯的地狱之火",
        "塞浦洛斯的风神之佑"
    };

    public static string[] atfNameZH_CN_Lite = new string[] {
        "???",
        "圣光之耀",
        "阳炎之盾",
        "地狱之火",
        "风神之佑"
    };

    // 神器的功能描述
    public static string[] atfEntry = new string[] {
        "没有什么卵用的神器",
        "范围2内的友方生物回复所有生命，并获得攻击+2，持续到下下回合开始",
        "赋予一个生物最大生命+3，在之后的该生物的每个回合开始时获得“圣盾”（免除一次伤害）",
        "生成一个地狱生物，并对范围2内的敌方生物造成2点伤害",
        "范围1内的友方生物重置行动次数，让已行动过的生物可以再次行动"
    };

    // 神器的描述
    public static string[] atfDiscription = new string[] {
        "你发现了神秘的0号神器",
        "牧师们把祝福灌注进了这块水晶，小心点，别摔碎了！",
        "拿好这面盾牌，它能吸收太阳的能量为你提供保护。什么？你还嫌它烫手？",
        "从天而降的陨石，听从我的召唤而来，请化身巨大的恶魔，将战场化为地狱吧！",
        "风神！风神！聆听我的祈祷！聆听我的祈祷！如狂风一般迅捷！如狂风一般迅捷！"
    };

    public static string[] campNameZH_CN = new string[] {
        "神迹-威斯克",
        "神迹-伊玛克斯"
    };

    public static Color[] unitInfoColor = new Color[] {
        new Color(0.6f, 1f, 1f),
        new Color(1f, 0.9f, 0.6f),
        new Color(0.5f, 1f, 0.5f),
        new Color(1f, 0.5f, 0.5f)
    };
    public static Color[] unitPanelColor = new Color[] {
        new Color(0f, 0.4f, 1f),
        new Color(1f, 0f, 0f)
    };

    public const float streamBedElevationOffset = -1.75f; // 河床降低，为了防止瀑布消失，要尽量深
    public const float waterElevationOffset = -0.5f; // 河流表面

    public const float waterFactor = 0.6f; // 在水体上对solidFactor的修正
    public const float waterBlendFactor = 1f - waterFactor; // 在水体上对blendFactor的修正

    public const int hashGridSize = 256; // 存储随机取样的哈希序列
    static HexHash[] hashGrid;
    public const float hashGridScale = 0.25f;

    static float[][] featureThresholds = {
        new float[] {0.0f, 0.0f, 0.4f},
        new float[] {0.0f, 0.4f, 0.6f},
        new float[] {0.4f, 0.6f, 0.8f}
    };
    public static float[] GetFeatureThresholds(int level)
    {
        return featureThresholds[level];
    }

    public static void InitializeHashGrid(int seed)
    {
        hashGrid = new HexHash[hashGridSize * hashGridSize];
        Random.State currentState = Random.state; // 记下原来的state？为什么要这么做
        Random.InitState(seed);
        for (int i = 0; i < hashGrid.Length; i++) {
            hashGrid[i] = HexHash.Create();
        }
        Random.state = currentState;
    }

    static Vector3[] corners = { // 私有的，通过下面两个接口访问
		new Vector3(0f, 0f, outerRadius),
        new Vector3(innerRadius, 0f, 0.5f * outerRadius),
        new Vector3(innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(0f, 0f, -outerRadius),
        new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
        new Vector3(0f, 0f, outerRadius) // 这是复制第一个点，方便循环赋值
	}; // 用direction访问实际上访问到 direction方向上 逆时针最近的点

    public static Vector3 GetFirstCorner(HexDirection direction) // 得到中心到direction方向的第一个顶点 的位移量
    {
        return corners[(int)direction];
    }
    public static Vector3 GetSecondCorner(HexDirection direction) // 得到中心到direction方向的第二个顶点 的位移量
    {
        return corners[(int)direction + 1];
    }
    public static Vector3 GetFirstSolidCorner(HexDirection direction) // 得到中心到direction方向的第一个缓冲点 的位移量
    {
        return corners[(int)direction] * solidFactor;
    }
    public static Vector3 GetSecondSolidCorner(HexDirection direction) // 得到中心到direction方向的第二个缓冲点 的位移量
    {
        return corners[(int)direction + 1] * solidFactor;
    }
    public static Vector3 GetBridge(HexDirection direction) // 得到direction方向的桥位移量
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) * blendFactor;
    }
    public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) * (0.5f * solidFactor);
    }
    public static Vector3 GetFirstWaterCorner(HexDirection direction)
    {
        return corners[(int)direction] * waterFactor;
    }
    public static Vector3 GetSecondWaterCorner(HexDirection direction)
    {
        return corners[(int)direction + 1] * waterFactor;
    }
    public static Vector3 GetWaterBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) * waterBlendFactor;
    }
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step) // 得到坡上 第step步 相应的点
    {
        float h = step * HexMetrics.horizontalTerraceStepSize; // 当前水平步长的比例
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }
    public static EdgeVertices TerraceLerp(EdgeVertices e1, EdgeVertices e2, int step)
    {
        EdgeVertices result;
        result.v1 = HexMetrics.TerraceLerp(e1.v1, e2.v1, step);
        result.v2 = HexMetrics.TerraceLerp(e1.v2, e2.v2, step);
        result.v3 = HexMetrics.TerraceLerp(e1.v3, e2.v3, step);
        result.v4 = HexMetrics.TerraceLerp(e1.v4, e2.v4, step);
        result.v5 = HexMetrics.TerraceLerp(e1.v5, e2.v5, step);
        return result;
    }
    public static Color TerraceLerp(Color c1, Color c2, int step) // 得到坡上相应的颜色
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        return Color.Lerp(c1, c2, h); // 线性插值：
    }
    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2) {
            return HexEdgeType.Flat;
        }
        int delta = elevation2 - elevation1;
        if (delta == 1 || delta == -1) {
            return HexEdgeType.Slope;
        }
        return HexEdgeType.Cliff;
    }
    public static Vector4 SampleNoise(Vector3 position) // 对纹理进行采样
    {
        return noiseSource.GetPixelBilinear(position.x * noiseScale, position.z * noiseScale);
    }
    public static HexHash SampleHashGrid(Vector3 position) // 根据位置，对hashGrid进行采样，并将结果限制在0~255
    {
        int x = (int)(position.x * hashGridScale) % hashGridSize;
        if (x < 0) x += hashGridSize;
        int z = (int)(position.z * hashGridScale) % hashGridSize;
        if (z < 0) z += hashGridSize;
        return hashGrid[x + z * hashGridSize];
    }
    public static Vector3 Perturb(Vector3 position) // 水平方向扰动
    {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
        return position;
    }
}

/*
 * 蓝：
 * 1, 3, 6
 * 红：
 * 2, 4, 7
 * 紫：
 * 3, 8
 * 
 (-6, -11 17)
 (-5, -10, 15), (-5, -11, 16), (-5, -12. 17), (-6, -12, 18), (-7, -12, 19)
     */
