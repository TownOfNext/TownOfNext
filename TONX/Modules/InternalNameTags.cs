using UnityEngine;
using static TONX.NameTagManager;

namespace TONX;

public static class InternalNameTags
{
    public static IReadOnlyDictionary<string, NameTag> Get()
    {
        var tags = GetAll();
        tags.Do(t => t.Value.Isinternal = true);
        return tags;
    }
    private static Dictionary<string, NameTag> GetAll() => new()
    {
        {
            "actorour#0029", //咔哥
            new()
            {
                UpperText = new()
                {
                    Text = $"∞ {GetString("Creater")} ∞",
                    Gradient = new(new Color32(198, 255, 221, 255), new Color32(251, 215, 134, 255), new Color32(247, 121, 125, 255)),
                    SizePercentage = 80
                },
                Prefix = new()
                {
                    Text = "✿",
                    TextColor = new Color32(246, 79, 89, 255)
                },
                Suffix = new()
                {
                    Text = "✿",
                    TextColor = new Color32(18, 194, 233, 255)
                },
                Name = new()
                {
                    Gradient = new(new Color32(18, 194, 233, 255), new Color32(196, 113, 237, 255), new Color32(246, 79, 89, 255)),
                    SizePercentage = 90
                }
            }
        },
        {
            "pinklaze#1776", //NCM
            new()
            {
                UpperText = new()
                {
                    Text = GetString("PullRequester"),
                    TextColor = new Color32(255, 192, 203, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "keepchirpy#6354", //Tommy-XL
            new()
            {
                UpperText = new()
                {
                    Text = $"Переводчик",
                    TextColor = new(31, 243, 198, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "taskunsold#2701", //Tem
            new()
            {
                UpperText = new()
                {
                    Text = $"Temmie",
                    Gradient = new(new Color32(66, 103, 152, 255), new Color32(246, 229, 9, 255)),
                    SizePercentage = 80
                }
            }
        },
        {
            "timedapper#9496", //阿龍
            new()
            {
                UpperText = new()
                {
                    Text = $"阿龍",
                    TextColor = new Color32(72, 255, 255, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "keyscreech#2151", //Endrmen40409
            new()
            {
                UpperText = new()
                {
                    Text = $"美術NotKomi",
                    Gradient = new(new Color32(211, 164, 255, 255), new Color32(90, 90, 173, 255)),
                    SizePercentage = 80
                }
            }
        },
        {
            "neatnet#5851", //Gurge44
            new()
            {
                UpperText = new()
                {
                    Text = $"The 200IQ guy",
                    TextColor = new Color32(255, 255, 0, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "contenthue#0404", //Gurge44
            new()
            {
                UpperText = new()
                {
                    Text = $"The 200IQ guy",
                    TextColor = new Color32(255, 255, 0, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "heavyclod#2286", //小叨
            new()
            {
                UpperText = new()
                {
                    Text = $"小叨.exe已停止运行",
                    TextColor = new Color32(255, 255, 0, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "storeroan#0331", //Night_瓜
            new()
            {
                UpperText = new()
                {
                    Text = $"Night_瓜",
                    TextColor = new Color32(255, 0, 102, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "teamelder#5856", //Slok7565
            new()
            {
                UpperText = new()
                {
                    Text = $"屑Slok（没信誉的鸽子）",
                    TextColor = new Color32(19, 121, 191, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "recentduct#6068", //法师
            new()
            {
                UpperText = new()
                {
                    Text = $"高冷男模法师",
                    TextColor = new Color32(255, 0, 255, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "canneddrum#2370", //喜
            new()
            {
                UpperText = new()
                {
                    Text = $"我是喜唉awa",
                    TextColor = new Color32(255, 252, 190, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "dovefitted#5329", //
            new()
            {
                UpperText = new()
                {
                    Text = $"不要首刀我",
                    TextColor = new Color32(19, 121, 191, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "luckylogo#7352", //林@林
            new()
            {
                UpperText = new()
                {
                    Text = $"林@林",
                    TextColor = new Color32(243, 0, 0, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "axefitful#8788", //罗寄
            new()
            {
                UpperText = new()
                {
                    Text = $"寄才是真理",
                    TextColor = new Color32(142, 129, 113, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "raftzonal#8893", //罗寄
            new()
            {
                UpperText = new()
                {
                    Text = $"寄才是真理",
                    TextColor = new Color32(142, 129, 113, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "twainrobin#8089", //
            new()
            {
                UpperText = new()
                {
                    Text = $"啊哈修maker",
                    TextColor = new Color32(0, 0, 255, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "mallcasual#6075", //波奇
            new()
            {
                UpperText = new()
                {
                    Text = $"波奇酱",
                    TextColor = new Color32(248, 156, 203, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "beamelfin#9478", // 1 1 1 1
            new()
            {
                UpperText = new()
                {
                    Text = $"Amaster-1111",
                    TextColor = new Color32(100, 149, 237, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "lordcosy#8966", //K
            new()
            {
                UpperText = new()
                {
                    Text = $"HostTONX",
                    TextColor = new Color32(255, 214, 236, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "honestsofa#2870", //SolarFlare
            new()
            {
                UpperText = new()
                {
                    Text = $"Sylveon",
                    TextColor = new Color32(211, 129, 217, 255),
                    SizePercentage = 80
                }
            }
        },
        {
            "caseeast#7194", //laikrai
            new()
            {
                UpperText = new()
                {
                    Text = $"disc.gg/maul",
                    TextColor = new Color32(28, 36, 81, 255),
                    SizePercentage = 80
                }
            }
        },
    };
}