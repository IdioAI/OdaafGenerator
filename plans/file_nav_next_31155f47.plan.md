---
name: File nav next
overview: 按 CodeSliceIndex/plans/03-file-nav-next.md 的顺序，在 OdaafGenerator 的文件菜单上逐项做完五步。试点仍是半自动前两个菜单。每做完一项再开始下一项。
todos:
  - id: tree-search
    content: 文件菜单左树模糊搜索：子序列匹配标题、方法名、中文短名和说明
    status: completed
  - id: layer-summary
    content: 可拖走的本层摘要窗：上下文、三类输入、DNA 一句、执行列短表
    status: completed
  - id: execute-lanes
    content: 执行列默认收成段；参数写入七段，加载配对另一套
    status: completed
  - id: markdown-export
    content: 从摘要表和段导出 Markdown 与 mermaid，摘要窗只读预览
    status: completed
  - id: local-propose
    content: 本地模型按直接调用提议中文名，人接受后才写入 FlowNotes
    status: completed
isProject: false
---

# 文件菜单五项，逐项做

权威排期是 [g:\myfuture\CodeSliceIndex\plans\03-file-nav-next.md](g:\myfuture\CodeSliceIndex\plans\03-file-nav-next.md)。试点仍是「加载PCB与XML对」（`ExecuteSemiAutoAddGroupingInfo`）和「参数写入PCB」（`ExecuteWriteParamsToPcb`）。不改 BHEditor，不把说明写入结构因，不把 mermaid 解析成调用节点。文件菜单仍停在 PCB文件。

现有壳是三列网格：[MainWindow.xaml](g:\myfuture\OdaafGenerator\src\OdaafGenerator.App\MainWindow.xaml)。左树来自 [MenuNav.cs](g:\myfuture\OdaafGenerator\src\OdaafGenerator.Core\MenuNav.cs) 的 `NavRoots`。中图是 [CallLayer.cs](g:\myfuture\OdaafGenerator\src\OdaafGenerator.Core\CallLayer.cs) 的一层直接调用。右栏是单个 `FunctionCard`。中文短名在 [FlowNotes.cs](g:\myfuture\OdaafGenerator\src\OdaafGenerator.Core\FlowNotes.cs)。解析入库的 `Search()` 留在原处，文件菜单不调用它。

```mermaid
flowchart LR
  search[1_树搜索] --> summary[2_本层摘要窗]
  summary --> lanes[3_执行分段]
  lanes --> markdown[4_导出Markdown]
  markdown --> propose[5_本地提议中文名]
```

## 1. 左树模糊搜索

在文件菜单左树标题下加输入框，绑定 `MainViewModel` 的新属性。每打一字过滤，清空恢复全树。

匹配在 Core 里做，不重建目录。对 `NavNode` 做子序列匹配，不区分大小写。参与字段：`Title`、`MethodName`、`FlowNotes` 的中文短名和一句说明。命中节点保留祖先。过滤结果是另一棵浅拷贝，展开到命中项；原树上已展开的调用层不丢。点节点仍走现在的 `SelectNav`。

测试：`配盘` 命中 `PerformLandMatchingAndGrouping`；`WriteParams` 命中参数写入；空串返回原树。

## 2. 可拖走的本层摘要

右栏继续只绑定当前选中的一个函数。新窗口跟当前菜单处理函数走：点执行列里的 `TryArchiveSemiAutoAssets` 时，枝图换这个函数，摘要仍概括 `ExecuteWriteParamsToPcb`。

先做独立 `Window`，可拖到第二块屏幕。关掉窗口不丢内容，菜单里可再打开。三列网格和 AvalonDock 这一步不动。

摘要四块用手写表，键是两个处理函数名，不从 mermaid 抽：

- 上下文：`SemiAuto`，一块板一份文档；参数写入必须先加载配对；中途不再选文件。
- 输入三格：本机 `semi_auto_process_config.json` 只是对话框默认值；`.prodat4semi` 与融合结果是这一对板；`ProcessInputOptions` 是这一次勾选。
- DNA：本层直接调用写「未在本层直接读取」。不把 `properties.csv` 行写入结构因。
- 执行汇总：只列当前层 `CallKind.Domain` 且阶段为执行的调用。中文短名、方法名、一句说明。未收入索引保持「未收入本期索引」。点一行，中间流程图滚到对应框（已有 `FlowCanvas.Reveal`）。

## 3. 执行列收成简图

一层调用的规则不变。默认视图先画段，展开一段才看到现在的逐框。段名写在与 `FlowNotes` 同一张键（方法名）上。没有段名的领域调用归到「其余」。

参数写入七段，按源码顺序：归档与顺序、清洗、背钻、Z 抽出、ISU、Z 补正、写回。加载配对另用：配对窗、文档、导入、融合匹配、配盘分组、清洗、刷新。

简图的线只连接段。[CallLayer.Cards](g:\myfuture\OdaafGenerator\src\OdaafGenerator.Core\CallLayer.cs) 增加一种分组结果；逐框模式仍只把执行框串起来。系统列和修饰阶段不进简图。

## 4. 导出 Markdown

源是第 2 步的摘要表和第 3 步的段。每个试点菜单一份，放在 OdaafGenerator 旁，例如 `docs/semi-auto/`。内容是四块文字加一段 mermaid `flowchart`，节点是段名。摘要窗口里只读预览同一份文本。改短名或段名后重新导出覆盖。不覆盖 skill `pcb-semi-auto`，也不读 mermaid 来建节点。

## 5. 本地模型提议中文名

试点两个菜单的手写表保持权威。其余菜单的短名、一句说明、参数含义先进待接受列表。语料是 `bheditor.db` 的方法路径与行号、现读的源码正文、已接受的 `FlowNotes`、挂在该菜单上的 skill。一次只问该处理函数的直接调用。系统调用不送。未收入索引不编造签名。人勾选后才写入说明表。默认只走本地模型；这一步不改 BHEditor 的 `///`。

本地模型的具体入口在做第 5 步时再定。前四步不依赖它。
