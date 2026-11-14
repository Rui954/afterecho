# 《余响 / Afterecho》泰拉瑞亚原创模组

> 世界不记得你杀了谁，只记得你怎么杀的。

一个**不改写主世界、不堆数值**的泰拉瑞亚内容模组：世界记录你的打法，并在「回响庭」里用你自己的习惯回应你。

**你会喜欢它，如果**：你厌倦了「更强的怪、更高的数值」，想要一场和自己习惯的博弈。

- 📖 [设计案（Markdown）](余响_模组设计案.md) · [手机版设计案](afterecho_Design.html)
- 🎮 [玩家指南（流程 / 触发 / 常见问题）](<afterecho_Player Guide.html>)
- 🧩 源码：[`Afterecho/`](Afterecho)

---

## 1. 这是什么

四个原创系统，全部围绕「世界在记录你」：

| 系统 | 一句话 |
| --- | --- |
| **谱系 The Ledger** | 你命中的伤害类型、距离、位移、来向被记成四维战术指纹；主导战术决定世界怎么回应 |
| **回响庭 The Reverie** | 用「相位镜」切入一层叠加空间：地形与主世界完全一致，零 tile 写入，退出后逐字节原样 |
| **反谱 Counterform** | 敌人只改 AI、弹幕轨迹与站位，**不改生命 / 伤害 / 防御**；破解方式永远是换打法 |
| **四幕 Boss** | 描摹者重演你的走位、对页翻你的战术页、缄口收录你的武器、余响本身抽出你最高频的 5 个动作 |

附带 12 件**横向**装备（每件写死一条代价）、4 个可选回响事件、深谱模式（只改弹幕编排，乘数恒 1.00）。

---

## 2. 下载与安装

### 玩家

1. 到 [Releases](https://github.com/Rui954/afterecho/releases) 下载 `Afterecho.tmod`；
2. 放进 `Documents/My Games/Terraria/tModLoader/Mods/`；
3. 启动 tModLoader → **Mods** → 启用《余响》→ **Reload Mods**；
4. 进游戏，按 [玩家指南](<afterecho_Player Guide.html>) 走：**玻璃 10 + 透镜 1 + 落星 3** 在工作台做「相位镜」，击败克苏鲁之眼后即可展开回响庭。

> 模组不写世界存档、不新增矿物、不改原版配方与掉落，卸载后存档零残留。

### 开发者

需要 .NET 8 SDK 与 tModLoader（1.4.4.9 / TML 2026.9.3.1，`TML_2026_09`）。

```powershell
# 把 Afterecho/ 复制进 tModLoader 的 ModSources/Afterecho/
Copy-Item -Recurse .\Afterecho "$env:USERPROFILE\Documents\My Games\Terraria\tModLoader\ModSources\Afterecho" -Force

# 在 ModSources/Afterecho/ 里编译 + 打包（产物会复制进 Mods/）
dotnet build
```

也可以在游戏里点 **Workspace → Build + Reload**。

> **本地化格式（踩过的坑）**：文件名已经带了前缀，内容就**不要再套一层前缀**。
> `Localization/zh-Hans_Mods.Afterecho.hjson` 的根节点直接是 `Items: / NPCs: / Configs: / Messages: / Events: ...`；
> 写成 `Mods: { Afterecho: { Items: ... } }` 会让整份文件不生效、游戏内全部回退英文。
> 想让键名完全限定，也可以把文件命名为 `zh-Hans.hjson` 再用 `Mods.Afterecho.Items.X.DisplayName: ...` 的完整写法。

---

## 3. 当前状态

| 项目 | 结果 |
| --- | --- |
| 目标版本 | Terraria 1.4.4.9 / tModLoader 2026.9.3.1（`TML_2026_09`） |
| 编译 | `dotnet build` 通过，0 错误 |
| 打包 | tModLoader 构建管线打包出 `Afterecho.tmod` |
| 无头加载 | 服务端 Finding → Adding → Configuring → Finalizing Content → Adding Recipes 全部通过 |
| 本地化 | 中英双语实测：`zh-Hans` 与 `en-US` 的物品名、事件名、聊天消息、配置标签均能正确解析 |
| 世界验证 | 小世界完整跑过「生成 → 保存 → 读取 → 开服」，连续 tick 无异常 |
| 实机游玩 | **未验证**（开发机没有可自动操作的图形客户端） |
| 多人联机 | **未验证**（共享实例是服务端推进、客户端表现的简化实现） |

已验证的边界写在 [设计案第 8 节](余响_模组设计案.md) 的「本版验收记录」，不夸大。

---

## 4. 仓库结构

```
afterecho/
├── README.md
├── 余响_模组设计案.md              # 完整设计案（第 8 节含工程与验收记录）
├── afterecho_Design.html           # 同一份内容的移动端网页版
├── afterecho_Player Guide.html     # 玩家向：流程 / 触发 / 常见问题 / 完整配方表
├── LICENSE                         # AGPL-3.0
└── Afterecho/                      # tModLoader 1.4.4 工程（可直接复制进 ModSources）
    ├── Afterecho.cs                # Mod 主类：Balance.Validate + Boss Checklist 弱登记
    ├── Afterecho.csproj / build.txt / description.txt / icon.png / icon_small.png
    ├── Common/                     # 公共层，统一 namespace Afterecho
    │   ├── Balance.cs              # ★ 平衡宪法常量 + 加载期断言 + 弹幕预算 + Boss 锚点
    │   ├── AfterechoConfig.cs      # 服务端配置：一键关闭 / 深谱 / 姿态 / 生成 / 回响事件
    │   ├── AfterechoKeybinds.cs    # 「回滚谱系快照」键位（默认 P）
    │   ├── LedgerPlayer.cs         # ★ 谱系 + 稳定度 + 轨迹 + 封印 + 装备效果
    │   ├── LedgerText.cs           # 谱系维度的本地化显示名
    │   ├── EchoEvents.cs           # ★ 4 个可选回响事件（空走 / 换手 / 逆谱 / 留白）
    │   ├── AfterechoGlobalItem.cs  # 物品命中钩子 + 折光刃回响斩 + 封印零伤
    │   ├── AfterechoGlobalProjectile.cs  # 弹幕命中钩子（召唤物节流、按弹幕取伤害类型）
    │   ├── ReverieGlobalNPC.cs     # 庭属标记 + ≤12% 姿态软抗性
    │   └── ReverieSystem.cs        # ★ 共享实例：稳定度 / 队伍姿态 / 生成 / 清理 / 相位浮尘
    ├── Content/
    │   ├── Biomes/ReverieBiome.cs  # 纯场景效果生物群系，零世界写入
    │   ├── Items/                  # 相位镜 / 回声砂 / 残谱 / 全谱拓本
    │   ├── Items/Weapons/          # 折光刃 / 错位弦 / 残页
    │   ├── Items/Accessories/      # 静默坠 / 双向镜 / 未言坠 / 折页靴 / 留白戒
    │   ├── Items/Summons/          # 四幕召唤物 + SummonGate 进度门控
    │   ├── NPCs/                   # 描摹者 / 对页 / 缄口 / 余响本身 + 残影 / 拓影
    │   └── Projectiles/            # 武器弹幕 5 种 + Boss 弹幕 4 种
    └── Localization/               # 中 / 英 本地化（物品、NPC、配置、事件、聊天提示）
```

---

## 5. 已实现清单

| 设计 | 代码位置 | 状态 |
| --- | --- | --- |
| 平衡宪法（数值锁死） | `Balance.cs` | 常量 + 加载期断言；每条装备代价也会被断言 |
| Boss 数值锚点 | `Balance.AssertBossStats` / `ScaleBossLife` | 四幕全部按 0.95 倍锚点，超区间直接拒绝加载 |
| 四维战术指纹、400 次滑动窗口 | `LedgerPlayer.cs` | 真实命中取距离 / 来向 / 位移 / 伤害类型 |
| 主导战术响应速度 | `Balance.DominanceWindow` | 姿态读最近 120 次命中，兑现「换打法约 20 秒见效」 |
| 姿态（只改行为） | `LedgerPlayer.DescribePosture` | 掠影者 / 间壁者 / 折页者 / 常态 |
| 多人姿态均值 | `ReverieSystem.ComputeSquadPosture` | 逐维众数 |
| 共享稳定度、退出保留材料 | `ReverieSystem` | 单一真源；静默坠的 +20% 消耗只乘一次 |
| 零侵入生成 / 退出清理 | `ReverieSystem` + `ReverieGlobalNPC` | 只有庭内才生成；退出清 NPC **和敌对弹幕** |
| 庭内可感知 | `ReverieSystem.SpawnAmbience` + `ReverieBiome` | 相位浮尘 + 进出音效，零世界写入 |
| 不越阶 | `LedgerPlayer.ReverieTier` + `SummonGate` | 未打 EoC 连第一层都不开放 |
| 描摹者重演上一场轨迹 | `Tracer.AI` + `LedgerPlayer.LastFightTrace` | 有历史就重演，没有则退化为实时描摹 |
| 对页第三战术空白页 | `Verso.AI` + `TopSchools` / `LastStrikeSchool` | 硬直由玩家行为触发（已排除 Other 误触发） |
| 缄口收录 → 未言 → 切武器释放 | `Hush.AI` + `LedgerPlayer.TickSilence` | 封印期伤害归零但仍可挥击蓄力，换武器爆 `PageBurst` |
| 终局 5 残影 | `AfterechoBoss` + `EchoShade` | 残影全灭前本体无敌；本体死亡时清理残影防死锁 |
| 4 个可选回响事件 | `EchoEvents.cs` | 空走 / 换手 / 逆谱 / 留白；Boss 战自动打断，全部可跳过 |
| 全谱回响（擦除记录） | `FullScoreCodex.UseItem` | 只给外观 / 称号，无属性 |
| 12 件横向装备 | `Content/Items/**` | 每件都带写死的代价（Balance 里断言） |
| 一键关闭 / 深谱 | `AfterechoConfig` | 深谱生命伤害倍率恒 1.00 |
| Boss Checklist | `Afterecho.RegisterBossChecklist` | 弱引用 5 参数 `LogBoss`，逐条 try/catch |

**平衡约束全部成立**：不改任何 tile / 配方 / 掉落表 / Boss 顺序 / NPC 商店；主世界零自然刷怪；装备系数 ≤ 1.05；深谱弹幕 `条数 × 单发伤害 ≤ 原预算`；软抗性 ≤ 12% 且换打法即归零；谱系不写存档。

---

## 6. 实机自测清单

1. **未打克苏鲁之眼**时做出「相位镜」并使用 → 应提示「世界还没有开始记录你」，不展开庭。
2. 击败 EoC 后使用 → 提示「第 1 层」；看一眼周围建筑、箱子、NPC，退出后**逐格原样**。
3. 在庭内待到稳定度归零 → 自动弹出，材料保留，庭内「拓影」和敌对弹幕全部消失，主世界干净。
4. 用「初痕之页」召唤描摹者 → 它会重演你上一场的走位；刻意换一条路线，伤害带应该落空。
5. 打开配置的「深谱模式」再打一次 → 弹幕变多但**Boss 血条与单次承伤不变**。
6. 装备「双向镜」按 `P` → 稳定度 −15%，姿态重新洗牌。
7. 打到缄口 → 当前武器被收录 6 秒；期间反复挥击蓄力，切换武器时爆出范围回响。
8. 在庭内等到第一次回响事件 → 完成「空走」应掉落回声砂；忽略它会自然消散且不扣东西。
9. 关掉配置里的「启用模组」→ 主世界零影响；重新开启后谱系从空开始（零存档残留）。
10. 多人：两人同时在庭内，稳定度是同一条；姿态取队伍均值。

---

## 7. 仍未完成（诚实清单）

- **美术 / 音频**：贴图是程序化占位（31 张内容贴图 + 2 张图标），音效与音乐借用原版 ID；原创像素素材、四层色调、声纹环境音、Boss 血条样式都没做。
- **实机游玩验证**：只做了编译、打包、无头加载与世界读写；数值手感与四幕节奏必须实际开一局校准。
- **多人联机**：共享稳定度与队伍姿态已有代码，但没有独立联机回归；谱系按玩家本地记录。
- **Boss Checklist**：按 5 参数 `LogBoss` 调用并逐条 try/catch，不同版本可能静默跳过。
- **数值校准**：Boss 生命已按 0.90–1.05 锚点写死并断言，但伤害、防御与弹幕密度仍是占位值。

---

## 8. 设计约束速查

零改写 · 零侵入生成 · 数值天花板 1.05 · 横向装备必有代价 · 难度只改行为 · 可一键关闭且零残留 · 不越阶。

任何试图给敌人加血加伤害、给装备做数值上位替代、或让回响庭扩散到主世界的改动，都与本模组的核心立意相冲突。`Balance.Validate()` 会在加载期拦下大部分这类改动。

---

## 参考与差异化说明

设计案第 1 节列明了两个参考对象的实际特征与明确规避项。借鉴的只是「行为驱动威胁」与「分幕大体量编排」两个抽象概念；感染 / 同化 / 进化阶段 / 巢穴、新职业 / 数值难度 / 连战 / 改原版进度等具体机制一条都不用。资料来源：Calamity Mod Wiki、Calamity 代码结构概览、SRP 机制与进化系统资料、tModLoader 项目结构与教程。
