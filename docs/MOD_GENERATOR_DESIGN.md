# StS2 Portrait Mod Generator 设计方案

## 1. 文档目的

本文档用于把当前 `StS2_Portrait_Mod_Generator` 仓库，从“一个已经写死资源与配置的卡图替换 Mod”，演进为“可基于 `.pck` 资源半自动生成新 Mod 的模板 + 生成器体系”。

目标不是直接在现有运行时逻辑上继续堆功能，而是把问题拆成两部分：

1. 可复用的 Portrait Replacement Mod Template
2. 消费模板并生成新 Mod 的 Generator Tool

这样做的原因很直接：

- 当前仓库里的 `RegentFemCards` 是一个具体成品，它能运行，但不适合直接承载导入、解包、识别、筛选、生成这些“工具链职责”。
- `.pck` 导入、图片分析、文件名 normalize、用户选择、JSON 生成、编译输出，本质上是一条“资产处理与脚手架生成流水线”，而不是游戏运行时逻辑。
- 如果不拆层，未来一改模板就容易把现有 Mod、生成器、构建链路绑死在一起，维护成本会很高。

## 2. 当前仓库现状

基于当前代码结构，这个仓库已经具备一个“卡图替换模板”的雏形。

### 2.1 当前关键结构

- 运行时代码位于 `RegentFemCardsCode/`
- 资源位于 `RegentFemCards/`
- 配置文件位于 `RegentFemCards/config/card_replacements.json`
- 构建与导出入口位于根目录：
  - `RegentFemCards.csproj`
  - `RegentFemCards.json`
  - `project.godot`
  - `Directory.Build.props`
  - `Sts2PathDiscovery.props`

### 2.2 当前配置机制

当前 Mod 的核心配置入口是 `RegentFemCardsCode/CardReplacementConfig.cs`。它会从固定路径加载：

`res://RegentFemCards/config/card_replacements.json`

配置中的一条记录目前支持以下字段：

- `cardId`
- `portrait`
- `uiMode`
- `frame`
- `frameMaterial`
- `bannerTexture`
- `bannerMaterial`
- `portraitBorder`
- `portraitBorderMaterial`
- `ancientTextBg`
- `textBackgroundMaterial`
- `energyIcon`
- `highlight`
- `highlightMaterial`

这说明运行时层已经不是“只能换 portrait”，而是已经具备“卡面资源替换入口”的扩展结构。生成器第一版虽然可以只聚焦 `cardId + portrait`，但输出模型应直接兼容这套字段。

### 2.3 当前路径与命名耦合点

当前仓库存在几组强耦合关系：

- `MainFile.ModId = "RegentFemCards"`
- `RegentFemCards.json` 中的 `id/name`
- `project.godot` 中的 `config/name` 与 `project/assembly_name`
- `RegentFemCards.csproj` 的项目名与输出目录
- `res://RegentFemCards/...` 资源根路径
- `RegentFemCards/` 目录名本身

这意味着未来如果生成器要输出一个新 Mod，不能只替换 JSON 里的名称，必须把“项目名、程序集名、manifest、资源根目录、配置路径”整体一起模板化。

## 3. 目标与边界

## 3.1 目标

希望最终形成一套工具链，让用户可以完成下面这条流程：

1. 选择 GDRETools headless CLI
2. 导入一个或多个 `.pck`
3. 解包资源到临时目录
4. 扫描其中的图片文件
5. 根据文件名和路径规则分析候选卡图
6. 对图片名做 normalize，推测 `cardId`
7. 在界面中预览、筛选、修正、确认映射
8. 将选中的图片复制到新 Mod 的 `CardPortraits` 目录
9. 按规则生成 `card_replacements.json`
10. 基于模板生成一个新的 Mod 项目
11. 调用 `dotnet build` / `dotnet publish` 编译
12. 输出最终的新 Mod

## 3.2 第一版非目标

第一版建议明确不做下面这些内容，避免把范围拉爆：

- 不做复杂图像内容识别，先只基于文件名、路径、尺寸做启发式分析
- 不做全自动“100% 正确识别 cardId”，必须保留人工修正入口
- 不做运行时热更新，不要求导入后立刻在游戏里预览
- 不做云端资源库或在线卡牌数据库依赖
- 不把生成器直接塞进现有 Mod 运行时代码中

## 4. 总体架构

建议采用三层结构：

1. `Base Template`
2. `Portrait Replacement Template`
3. `Generator Tool`

### 4.1 Base Template

职责：只负责“这是一个可编译、可安装、可导出的 StS2 Mod”。

建议包含：

- `.csproj`
- mod manifest `.json`
- `MainFile.cs`
- `project.godot`
- `Directory.Build.props`
- `Sts2PathDiscovery.props`
- 必要的解决方案或构建辅助文件

这个层不负责卡图替换业务。

### 4.2 Portrait Replacement Template

职责：只负责“这是一个支持卡图替换的 Mod 模板”。

建议在 Base Template 上增加：

- `CardReplacementConfig.cs`
- `PortraitReplacementRegistry.cs`
- `FrameReplacementRegistry.cs`
- `CardPortraitReplacementPatch.cs`
- `FramePatch.cs`
- `CardUiModeSpoofPatch.cs`
- `config/card_replacements.json`
- `CardPortraits/`

这个层是你当前项目真正有价值的复用资产。

### 4.3 Generator Tool

职责：把模板消费成“新项目”和“最终产物”。

它负责：

- 调 GDRETools 解包 `.pck`
- 扫描图片资源
- 分析文件名与路径
- normalize 文件名
- 让用户确认映射
- 复制模板
- 复制图片
- 生成 `card_replacements.json`
- 编译输出

Generator Tool 应是单独项目，不应直接和运行时 patch 混在同一代码层里。

## 5. 推荐落地形式

推荐路线：先做 CLI 核心，再加 GUI 壳。

### 5.1 第一阶段：CLI

先把这几个能力做通：

- `import-pck`
- `scan-assets`
- `normalize`
- `generate-config`
- `generate-mod`
- `build-mod`

CLI 阶段的目标不是用户体验，而是先验证整条流水线。

### 5.2 第二阶段：GUI

等核心流程稳定后，再补一层桌面界面。

如果只考虑 Windows 上快速落地，建议优先：

1. WinForms
2. WPF
3. Avalonia

其中第一版最务实的是 WinForms，因为重点不在炫技，而在把“选择资源、预览图片、修正映射、点击生成”这条流程稳定跑通。

## 6. 建议目录结构

建议未来调整为类似下面的结构：

```text
StS2_Portrait_Mod_Generator/
  docs/
    MOD_GENERATOR_DESIGN.md
  templates/
    BaseModTemplate/
      template.json
      src/
        __MOD_ID__.csproj
        __MOD_ID__.json
        project.godot
        Directory.Build.props
        Sts2PathDiscovery.props
        __MOD_ID__Code/
          MainFile.cs
    PortraitReplacementTemplate/
      template.json
      src/
        __MOD_ID__.csproj
        __MOD_ID__.json
        project.godot
        Directory.Build.props
        Sts2PathDiscovery.props
        __MOD_ID__/
          CardPortraits/
          config/
            card_replacements.json
        __MOD_ID__Code/
          MainFile.cs
          CardReplacementConfig.cs
          PortraitReplacementRegistry.cs
          FrameReplacementRegistry.cs
          CardPortraitReplacementPatch.cs
          FramePatch.cs
          CardUiModeSpoofPatch.cs
  tools/
    PortraitModGenerator.Cli/
    PortraitModGenerator.Core/
    PortraitModGenerator.Gui/
  output/
  temp/
```

这里的重点不是目录名必须完全照抄，而是要把“模板”和“生成器工具”在仓库结构上明确拆开。

## 7. 模板改造方案

## 7.1 模板化原则

不要把当前可运行的 `RegentFemCards` 直接改成满地占位符。

更稳的做法是：

1. 保留当前项目继续可编译、可运行
2. 复制出一份模板目录
3. 只在模板目录中引入占位符

### 7.2 建议模板参数

模板层至少需要支持这些参数：

- `ModId`
- `ModName`
- `Author`
- `Description`
- `Version`
- `Namespace`
- `AssemblyName`
- `TemplateVersion`
- `PortraitRootFolderName`

建议使用清晰占位符，例如：

- `__MOD_ID__`
- `__MOD_NAME__`
- `__AUTHOR__`
- `__DESCRIPTION__`
- `__VERSION__`
- `__NAMESPACE__`

### 7.3 必须模板化的文件点位

以下内容必须被模板引擎显式处理：

- `*.csproj` 文件名
- manifest 文件名
- manifest 内容中的 `id/name/author/description/version`
- `project.godot` 中的 `config/name`
- `project.godot` 中的 `project/assembly_name`
- `MainFile.ModId`
- 命名空间
- 资源目录名
- `CardReplacementConfig` 中的默认配置路径
- `card_replacements.json` 中所有 `res://{ModId}/...` 路径

### 7.4 template.json

建议每个模板目录都提供 `template.json`，最少包含：

```json
{
  "templateId": "portrait-replacement",
  "templateName": "Portrait Replacement Template",
  "templateVersion": "1.0.0",
  "entryProject": "__MOD_ID__.csproj",
  "manifestFile": "__MOD_ID__.json",
  "defaultOutputStructure": {
    "portraitRoot": "__MOD_ID__/CardPortraits",
    "configPath": "__MOD_ID__/config/card_replacements.json"
  },
  "supportedFields": [
    "cardId",
    "portrait",
    "uiMode",
    "frame",
    "frameMaterial",
    "bannerTexture",
    "bannerMaterial",
    "portraitBorder",
    "portraitBorderMaterial",
    "ancientTextBg",
    "textBackgroundMaterial",
    "energyIcon",
    "highlight",
    "highlightMaterial"
  ]
}
```

这样模板升级后，生成器能按版本校验，而不是靠硬编码猜目录结构。

## 8. 生成器内部模块设计

不论外层是 CLI 还是 GUI，核心逻辑建议稳定拆成以下模块。

### 8.1 Template Engine

职责：

- 读取模板元数据
- 复制模板到目标目录
- 重命名目录与文件
- 替换允许替换的内容
- 生成最终项目结构

注意点：

- 不建议对所有文本文件做全局替换
- 要维护一份“允许替换的文件列表”和“允许替换的 token 列表”
- 二进制文件、图片、`.godot` 导出相关文件不要盲替换

输入：

- 模板路径
- 用户输入的 Mod 元数据
- 输出目录

输出：

- 已生成但尚未填充资源的新 Mod 项目目录

### 8.2 Pck Importer

职责：

- 校验 GDRETools CLI 路径
- 执行 headless 解包命令
- 将 `.pck` 解包到临时目录
- 记录 stdout/stderr
- 产出标准化导入结果

建议输出模型：

```json
{
  "sourcePck": "D:/input/foo.pck",
  "extractRoot": "D:/temp/session_001/extracted/foo",
  "startedAt": "2026-04-15T22:00:00Z",
  "endedAt": "2026-04-15T22:00:12Z",
  "success": true,
  "exitCode": 0,
  "logPath": "D:/temp/session_001/logs/foo_extract.log"
}
```

建议：

- 每个 `.pck` 单独一个解包目录
- 每次生成任务单独一个 session 目录
- 所有命令日志都落盘，便于排错

### 8.3 Asset Scanner

职责：

- 在解包目录递归扫描图片
- 收集图片元数据
- 过滤明显无关资源

第一版建议支持格式：

- `.png`
- `.jpg`
- `.jpeg`
- `.webp`

建议扫描结果字段：

- `sourcePck`
- `absolutePath`
- `relativePath`
- `fileName`
- `extension`
- `width`
- `height`
- `fileSize`
- `pathTokens`
- `fileNameTokens`

建议内置基础过滤能力：

- 路径关键词过滤
- 文件名关键词过滤
- 尺寸过滤
- 去重过滤

### 8.4 Name Normalizer

这是整个系统的关键模块之一。

职责：

- 从原始文件名中提取候选 token
- 清理噪音词
- 转换成标准命名
- 生成候选 `cardId`
- 给出置信度与原因说明

建议把 normalize 拆成四步：

1. `RawName`
2. `SanitizedName`
3. `NormalizedName`
4. `CandidateCardIds`

例子：

```text
RawName: regent_attack_01_portrait
SanitizedName: regent attack 01
NormalizedName: RegentAttack01
CandidateCardIds:
  - Attack01
  - RegentAttack01
```

建议规则：

- 去掉扩展名
- 将 `_`、`-`、空格统一视为分隔符
- 去掉 `portrait`、`art`、`card`、`illustration` 等噪音词
- 支持角色名前缀字典，例如 `regent`
- 转 PascalCase
- 输出多个候选，不只一个

同时建议引入两个配置表：

- `alias-rules.json`
- `ignore-tokens.json`

这样后续不需要改代码就能修正规则。

### 8.5 Mapping Analyzer

职责：

- 结合文件名、路径、尺寸推测资源是否像“卡图”
- 输出候选映射记录
- 为 UI/CLI 审核步骤准备数据

建议输出数据模型：

```json
{
  "assetId": "asset_001",
  "sourcePck": "foo.pck",
  "relativePath": "images/cards/regent/regent_attack_01.png",
  "originalFileName": "regent_attack_01.png",
  "normalizedFileName": "Attack01",
  "candidateCardIds": ["Attack01", "RegentAttack01"],
  "recommendedCardId": "Attack01",
  "confidence": 0.78,
  "reasons": [
    "matched filename tokens",
    "path contains cards keyword"
  ],
  "outputSubfolder": "Regent",
  "selected": false
}
```

### 8.6 Mapping Editor

职责：

- 呈现候选映射
- 允许勾选/取消
- 允许修正 `cardId`
- 允许修改输出文件名
- 允许填写高级字段

GUI 里每条记录建议至少展示：

- 是否选中
- 缩略图预览
- 原始文件名
- 原始路径
- normalize 后名称
- 推荐 `cardId`
- 手动修正 `cardId`
- 输出文件名
- 置信度

CLI 模式下，这一层可以退化成：

- 导出 CSV/JSON 审核文件
- 用户手动修改
- 再导回生成器继续执行

### 8.7 Config Generator

职责：

- 根据最终确认的映射生成 `card_replacements.json`
- 统一生成 `res://` 路径
- 保证 JSON 结构稳定、排序可预测

建议始终输出完整 schema，但允许字段为空。

建议单条记录模型：

```json
{
  "cardId": "Arsenal",
  "portrait": "res://MyMod/CardPortraits/Regent/Arsenal.png",
  "uiMode": "",
  "frame": "",
  "frameMaterial": "",
  "bannerTexture": "",
  "bannerMaterial": "",
  "portraitBorder": "",
  "portraitBorderMaterial": "",
  "ancientTextBg": "",
  "textBackgroundMaterial": "",
  "energyIcon": "",
  "highlight": "",
  "highlightMaterial": ""
}
```

生成规则建议：

- 按 `cardId` 排序
- 保持缩进固定
- 统一使用 `/` 路径分隔符
- 所有资源路径都由生成器统一拼装，用户不直接手敲 `res://`

### 8.8 Asset Copier

职责：

- 把选中的图片复制到目标模板项目中
- 根据最终文件名重命名
- 处理重名冲突

建议规则：

- 默认输出到 `__MOD_ID__/CardPortraits/{CharacterOrGroup}/`
- 如果存在同名文件，先检查内容哈希
- 同内容可跳过复制
- 不同内容则自动加后缀，或要求用户确认

### 8.9 Builder

职责：

- 调用 `dotnet build`
- 按需调用 `dotnet publish`
- 收集编译日志
- 判断产物位置

构建链路应明确区分两类输出：

1. 项目源码输出
2. 编译产物输出

建议输出记录：

- build start/end
- exitCode
- success/failure
- stdout/stderr 日志路径
- 最终产物目录

## 9. 数据持久化与会话模型

建议给生成器定义一个“任务会话”目录，避免多次操作互相污染。

推荐结构：

```text
temp/
  sessions/
    20260415_223000_my_mod/
      inputs/
      extracted/
      scan/
      review/
      logs/
      generated/
```

建议每次任务保留这些中间文件：

- 用户输入参数快照
- 每个 `.pck` 的解包结果
- 扫描结果 JSON
- normalize 结果 JSON
- 用户最终选择结果 JSON
- 生成出的 `card_replacements.json`
- 构建日志

这样有三个好处：

- 失败时可恢复
- 规则调整后可重放
- 便于问题排查和后续自动化测试

## 10. 用户交互流程设计

建议最终 GUI 流程固定为 6 步。

### 第 1 步：选择模板类型

字段：

- 模板选择
- 模板版本
- 模板说明

第一版其实可以只有一个模板：`Portrait Replacement Template`

### 第 2 步：填写 Mod 基本信息

字段建议：

- `ModId`
- `ModName`
- `Author`
- `Description`
- `Version`
- 输出目录

建议当场做校验：

- `ModId` 只能包含字母数字和少量安全符号
- `ModId` 需同时满足程序集名、目录名、manifest id 的可用要求

### 第 3 步：配置工具路径

字段：

- GDRETools CLI 路径
- 可选的 Godot 路径
- 可选的 `dotnet` 路径

建议提供“检测”按钮，分别校验：

- 文件是否存在
- 是否可执行
- 版本是否兼容

### 第 4 步：导入 `.pck`

支持：

- 单文件导入
- 多文件导入
- 拖拽导入

执行：

- 调用 GDRETools 解包
- 显示进度和日志

### 第 5 步：审核图片映射

界面建议分三栏：

- 左侧：资源列表与筛选器
- 中间：图片预览
- 右侧：映射编辑表单

筛选器建议支持：

- 仅显示高置信度
- 仅显示已选中
- 按路径关键词过滤
- 按文件名搜索
- 按尺寸过滤

### 第 6 步：生成与编译

点击“生成 Mod”后顺序执行：

1. 复制模板
2. 写入图片资源
3. 生成 `card_replacements.json`
4. 写入 manifest 和项目元数据
5. 调用 `dotnet build`
6. 可选调用 `dotnet publish`
7. 显示输出路径

成功后应明确展示：

- 新 Mod 项目目录
- 编译产物目录
- 是否已复制到游戏 `mods` 目录
- 失败时的日志位置

## 11. 规则系统设计

文件名分析不可靠，所以规则系统不能写死在 UI 层。

建议把规则拆成几类配置文件：

### 11.1 ignore tokens

用于清理噪音词：

```json
[
  "portrait",
  "art",
  "card",
  "illustration",
  "full",
  "final"
]
```

### 11.2 character prefixes

用于移除角色前缀：

```json
[
  "regent",
  "ironclad",
  "silent",
  "defect"
]
```

### 11.3 aliases

用于把别名映射到规范 token：

```json
{
  "defend_regent": "DefendRegent",
  "royal_gamble_alt": "RoyalGamble"
}
```

### 11.4 path heuristics

用于基于路径增加或降低置信度：

- 路径包含 `cards`、`portrait`、`ui/card`
- 路径包含 `icons`、`fx`、`vfx` 时降权

规则系统应尽量配置化，避免每次遇到特殊命名都要改代码。

## 12. 关键技术决策

### 12.1 为什么生成器要独立项目

因为它的依赖和职责不同：

- 运行时 Mod 依赖 Godot / StS2 / Harmony
- 生成器依赖文件系统、外部进程、图片扫描、UI 组件

混在一个项目里会导致：

- 依赖膨胀
- 构建链路复杂
- 模板与工具彼此牵连

### 12.2 为什么先 CLI 再 GUI

因为这条流水线里最难的不是界面，而是：

- 解包是否稳定
- 图片扫描是否可靠
- normalize 规则是否够用
- JSON 生成是否正确
- 编译链路是否可复用

先把核心流程做成无界面的命令行管道，后续 GUI 才不会变成“把问题藏在按钮后面”。

### 12.3 为什么保留高级字段但第一版不强推

当前运行时已经支持 frame/uiMode 等字段。

如果第一版文档和工具只按 `cardId + portrait` 建模，后续补 frame 功能时会造成：

- JSON schema 变更
- UI 模型重做
- 导入/导出逻辑重做

所以正确做法是：

- 结构上保留完整字段
- 默认 UI 只暴露 portrait 主流程
- 把高级字段放到高级模式或二期功能中

## 13. 与当前项目的对应改造建议

下面是基于现有仓库的具体建议。

### 13.1 当前项目保留为参考实现

`RegentFemCards` 当前已经是一个可工作的参考样板，应保留：

- 作为运行时验证项目
- 作为模板抽取来源
- 作为生成器回归测试样本

### 13.2 新增模板目录

建议新增：

- `templates/PortraitReplacementTemplate/`

并把现有项目中的可复用内容复制过去，再做占位符化。

### 13.3 新增生成器项目

建议新增三个项目：

- `tools/PortraitModGenerator.Core/`
- `tools/PortraitModGenerator.Cli/`
- `tools/PortraitModGenerator.Gui/`

其中：

- `Core` 放所有业务逻辑
- `Cli` 负责脚本化和调试
- `Gui` 负责最终用户入口

### 13.4 当前仓库中的关键复用点

这些文件几乎可以直接成为模板运行时层的一部分：

- `RegentFemCardsCode/CardReplacementConfig.cs`
- `RegentFemCardsCode/PortraitReplacementRegistry.cs`
- `RegentFemCardsCode/FrameReplacementRegistry.cs`
- `RegentFemCardsCode/CardPortraitReplacementPatch.cs`
- `RegentFemCardsCode/FramePatch.cs`
- `RegentFemCardsCode/CardUiModeSpoofPatch.cs`

需要注意的是它们里面当前写死了 `RegentFemCards` 命名空间和资源路径，模板化时必须改为参数驱动。

## 14. 失败场景与容错设计

这个项目最容易失败的地方，应该在设计里提前承认。

### 14.1 GDRETools 不可用

场景：

- 路径错误
- 版本不兼容
- 命令参数变更
- 解包失败

处理：

- 启动前做版本探测
- 记录完整命令行
- 保存 stdout/stderr
- 给用户可理解的错误信息

### 14.2 `.pck` 中有大量无关图片

场景：

- UI 图标
- 特效贴图
- 场景背景
- 小尺寸缩略图

处理：

- 先按路径/尺寸/关键词降噪
- UI 中支持搜索、批量勾选、批量排除
- 保留手工修正入口

### 14.3 文件名无法映射 cardId

场景：

- 作者命名混乱
- 使用编号命名
- 使用内部代号

处理：

- 允许多个候选
- 允许手工填写 `cardId`
- 把本次修正沉淀到 alias 规则库

### 14.4 构建失败

场景：

- `Sts2Path` 未找到
- `GodotPath` 未配置
- `dotnet restore` 缺依赖
- 模板 token 未替换干净

处理：

- 在“生成”前做环境预检
- 把错误定位到具体环节
- 输出日志文件位置

## 15. 测试策略

即使第一版先做工具，也建议同步规划测试。

### 15.1 核心单元测试

建议覆盖：

- 文件名 normalize
- alias 规则
- ignore token 规则
- 路径到 `res://` 的拼装
- `card_replacements.json` 生成排序

### 15.2 集成测试

建议准备 2 到 3 组样本：

- 命名规范的 `.pck`
- 命名混乱但可修正的 `.pck`
- 含大量无关图片的 `.pck`

验证输出：

- 扫描结果数量
- 可识别比例
- 最终生成的 JSON
- 新 Mod 是否能编译通过

### 15.3 模板回归测试

每次模板升级都要验证：

- token 是否替换完整
- 生成后的项目是否还能 `dotnet build`
- 资源路径是否仍正确

## 16. 分阶段实施计划

推荐按下面顺序推进。

### 阶段 1：模板抽离

目标：

- 从当前 `RegentFemCards` 提取 `PortraitReplacementTemplate`
- 定义 `template.json`
- 跑通“复制模板并替换基本信息”

完成标准：

- 可生成一个改名后的新 Mod 项目
- 新项目能编译

### 阶段 2：CLI 核心流程

目标：

- 接入 GDRETools CLI
- 解包 `.pck`
- 扫描图片
- 生成候选映射
- 生成 `card_replacements.json`

完成标准：

- 不依赖 GUI，也能完整跑一遍生成流程

### 阶段 3：构建链路接通

目标：

- 生成后自动 `dotnet build`
- 可选 `publish`
- 输出最终产物目录

完成标准：

- 用户执行一次命令即可得到可编译项目与构建结果

### 阶段 4：GUI

目标：

- 补资源筛选、预览、修正、生成入口

完成标准：

- 用户可以通过图形界面完成整个流程

### 阶段 5：高级字段与规则增强

目标：

- 增加 `uiMode/frame/highlight` 等高级替换编辑
- 引入可配置规则库
- 支持映射模板保存与复用

## 17. 第一版的最小可行产品定义

如果要控制开发量，第一版 MVP 建议限定为：

- 一个独立的生成器项目
- 能调用 GDRETools 解包单个或多个 `.pck`
- 能扫描常见图片格式
- 能基于文件名和路径规则给出候选 `cardId`
- 用户能手动修正与勾选
- 能把选中的图片复制到 `CardPortraits`
- 能生成兼容当前 schema 的 `card_replacements.json`
- 能基于模板生成新 Mod 项目
- 能执行 `dotnet build`

第一版可以暂缓：

- 自动推断 frame/banner 资源
- 复杂图像识别
- 一键安装到游戏目录
- 跨平台完整支持

## 18. 结论

这件事最合适的定义，不是“继续给现有 mod 加功能”，而是“把现有 mod 提炼成模板，再做一个面向资源导入的脚手架生成器”。

对当前仓库来说，最稳的方向是：

1. 保留 `RegentFemCards` 作为参考实现
2. 新建 `PortraitReplacementTemplate`
3. 新建独立的 `Generator Tool`
4. 先做 CLI 核心流程
5. 再补 GUI 和一键编译体验

这样拆完之后，模板负责“生成出来的 Mod 长什么样”，生成器负责“用户如何从 `.pck` 走到新 Mod”，两边职责清晰，后续扩展也不会互相拖累。

## 19. 下一步建议

基于这份方案，下一步最值得落地的不是直接开 GUI，而是先做下面三件事：

1. 把当前仓库整理出 `templates/PortraitReplacementTemplate/`
2. 定义 `template.json` 和模板参数替换规则
3. 搭一个 `PortraitModGenerator.Core + Cli` 的最小骨架，把“复制模板 + 生成 card_replacements.json”先跑通

如果继续往下推进，下一份文档建议写成更工程化的内容：

- 模块级目录结构草案
- C# 数据模型定义草案
- CLI 命令设计
- GUI 页面草图与交互稿
- 生成器与模板之间的接口契约
