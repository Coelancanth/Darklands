好的，我将以严格、中立的视角，对这份 ADR-024 进行详细的审查。

-----

### ADR-024 Review 报告

**审查员**: 专业知识指导
**日期**: 2025-09-19
**总体评价**: **优秀 (Excellent)**. 这是一份高质量的架构决策记录。它清晰地阐述了问题背景，提出了一个经过深思熟虑且符合行业最佳实践的解决方案。文档结构清晰，与现有ADR的集成度高，代码示例具体，对关键原则的强调也非常到位。然而，在实现细节、并发处理和性能方面存在一个**严重的技术缺陷**和几个需要澄清的**潜在风险点**。

-----

### 1\. 核心优势 (Key Strengths)

这份ADR的决策和文档编写展现了高度的专业性，其主要优点如下：

1.  **问题定义精准**: ADR准确地识别了帧同步循环（Frame-Based Loop）对于回合制战术游戏的根本性缺陷（非确定性、存档/回放困难、逻辑与渲染耦合），为后续决策提供了坚实的理论基础。
2.  **架构解耦清晰**: 核心决策——将游戏逻辑循环（GameLoop）与渲染循环（Godot `_process`）彻底分离——是完全正确的。这确保了游戏逻辑的确定性和可测试性，是构建此类游戏的基石。
3.  **与现有架构高度整合**: 文档明确引用了多达8个相关的ADR，并详细说明了新架构如何遵循或实现这些既有决策（如`IGameClock`抽象、DI生命周期、项目分层等）。这表明了决策者对整个项目架构有通盘的考虑，保证了架构的一致性。
4.  **强调确定性 (Determinism)**: ADR反复强调了确定性的重要性，并给出了正确与错误的范例（`_gameTime += TimeUnit.CreateUnsafe(1)` vs. `_gameTime += elapsed.TotalSeconds`)。这种对核心原则的不断重申，能有效防止团队成员在实现中犯下严重错误。
5.  **生命周期管理明确**: 通过 `IHostedService` 和 `GameStrapper` 中的宿主（Host）启动/停止逻辑，清晰地定义了 `GameLoop` 的生命周期，使其独立于 Godot 的场景树（Scene Tree），这是一个非常健壮的设计。

-----

### 2\. 严重问题与风险 (Critical Issues & Risks)

尽管总体设计优秀，但实现细节中存在一个严重的技术错误，必须在实施前纠正。

#### **严重缺陷 (Critical Flaw): `CombatScheduler` 的性能声明错误**

  - **问题描述**: ADR中的 `CombatScheduler` 类宣称其 `GetNextActor` 方法是 `O(1)` 操作。这是**完全错误**的。
  - **技术分析**: 该实现使用了 `List<ISchedulable>`，而 `_timeline.RemoveAt(0)` 操作的复杂度是 **`O(n)`**，其中 `n` 是列表中的元素数量。因为移除第一个元素后，列表需要将后续所有 `n-1` 个元素向前移动一位。ADR中声称的 `O(1)` 严重误导了实现者和未来的维护者。
  - **潜在影响**: 在战斗单元数量较少时（例如少于10个），性能影响可以忽略不计。但随着单元数量增多（例如20个以上，或包含大量需要调度的临时效果），每次获取下一个行动者都会产生线性时间复杂度的消耗，这会累积成可观的性能瓶颈，尤其是在快速游戏模式下。
  - **修正建议**:
    1.  **首选方案**: 使用 .NET 6+ 中内置的 `PriorityQueue<TElement, TPriority>`。它天生就是为这种场景设计的，入队（Enqueue）操作是 `O(log n)`，出队（Dequeue）操作（获取优先级最高的元素）是 `O(log n)`，比当前实现的 `O(n)` 高效得多。
    2.  **备选方案**: 如果由于某些原因不能使用 `PriorityQueue`，可以使用 `LinkedList<T>`。在其头部添加节点是 `O(1)`，移除头部节点也是 `O(1)`。但插入时需要手动遍历找到正确位置，复杂度是 `O(n)`。这虽然解决了出队的性能问题，但入队的性能比 `BinarySearch` 的 `O(log n)` 要差。
    3.  **结论**: 必须将 `List<T>` 替换为 `PriorityQueue<TElement, TPriority>`，并将ADR中的性能声明更正为 `O(log n)`。

-----

### 3\. 需要澄清与改进的要点 (Areas for Clarification & Suggestions)

以下几点虽然不是严重缺陷，但缺乏明确定义，可能在未来导致不一致或难以预料的行为。

#### **3.1. 并发与重入风险 (Concurrency and Re-entrancy Risk)**

  - **问题描述**: `GameLoop` 使用 `System.Threading.Timer`，其回调函数在线程池线程上执行。`SafeGameAdvancementAsync` 方法是 `async` 的。如果某一次tick的处理时间（例如由于复杂的AI计算或大量的事件发布）超过了 `Timer` 的间隔时间（`period`），下一次tick的回调可能会在当前tick完成前被触发。
  - **潜在影响**: 这会导致 `SafeGameAdvancementAsync` 方法的重入（Re-entrancy），即多个tick的处理逻辑并发执行，可能会引发竞态条件，破坏游戏状态的确定性。例如，`_currentGameTime` 可能会被多个线程同时修改。
  - **修正建议**:
    1.  **使用互斥锁**: 在 `CheckForGameAdvancement` 中使用一个简单的 `bool` 锁或 `SemaphoreSlim(1, 1)` 来确保同一时间只有一个tick在处理。

        ```csharp
        private readonly SemaphoreSlim _tickLock = new SemaphoreSlim(1, 1);

        private async void CheckForGameAdvancement(object? state)
        {
            // 如果上一个tick还没处理完，就直接跳过本次tick
            if (!await _tickLock.WaitAsync(0)) return; 
            
            try
            {
                await SafeGameAdvancementAsync();
            }
            finally
            {
                _tickLock.Release();
            }
        }
        ```

    2.  **修改Timer逻辑**: 或者，不使用带有 `period` 的重复计时器，而是在 `SafeGameAdvancementAsync` 的 `finally` 块中重新设置下一次 `Timer` 的触发。这能确保两次tick之间总是有固定的时间间隔。

#### **3.2. 调度器平局处理 (Scheduler Tie-Breaking Logic)**

  - **问题描述**: ADR提到“多个actors在同一个TimeUnit上会在多个tick中被处理”，但没有定义当多个actor在完全相同的 `TimeUnit` 准备就绪时，**处理的顺序**是什么。
  - **技术分析**: `List.BinarySearch` 的行为对于重复元素是不确定的。如果两个actor的 `NextTurn` 完全相同，它们插入列表的相对顺序取决于`IComparer`的实现。如果比较器只比较时间，那么它们的顺序将依赖于插入时的微妙差异。
  - **潜在影响**: 如果平局处理规则不明确，游戏的回放（Replay）可能会出现不一致。例如，在T=100时，Actor A和Actor B都准备好了。第一次运行时A先动，第二次运行时B先动，这可能会导致完全不同的游戏结果，破坏了确定性。
  - **修正建议**:
    1.  **定义明确的排序规则**: `TimeComparer` 不应只比较 `NextTurn`。它应该有一个次要（甚至第三）排序标准，例如：
          * **Actor ID**: 一个稳定且唯一的ID。
          * **主动性/敏捷属性 (Initiative/Agility)**: 游戏规则的一部分。
          * **调度顺序**: 为每个调度请求附加一个唯一的、递增的序列号。
    2.  在ADR中明确记录这个平局处理规则。例如：“当多个单位在同一TimeUnit准备就绪时，将根据其‘主动性’属性进行二次排序；若主动性也相同，则根据其唯一的Actor ID升序排序。”

#### **3.3. 玩家输入与GameLoop阻塞 (Player Input and GameLoop Blocking)**

  - **问题描述**: `ProcessActor` 方法中，当轮到玩家时，它调用 `await _gameState.EnablePlayerInput(actor)`。
  - **潜在风险**: 这个 `await` 的实现细节至关重要。如果 `EnablePlayerInput` 是一个真正阻塞（或长时间等待）的方法，直到玩家做出选择才返回，那么整个 `GameLoop` 的 `Timer` 会被卡住。这意味着游戏内的所有其他活动（例如持续性法术效果的计时、环境动画等）都会完全停止。
  - **澄清要求**:
    1.  ADR应明确指出 `GameLoop` 在等待玩家输入时**是否应该暂停**。
    2.  推荐的设计模式是：`EnablePlayerInput` 应该立即返回，它仅仅是改变游戏状态（例如，`GameState` 变为 `WaitingForPlayerInput`）并激活UI。`GameLoop` 的 `_gameState.ShouldAdvanceTime()` 方法此时应返回 `false`，从而自然地“暂停”时间流逝。当玩家通过UI执行一个动作后，该动作的处理器会改变游戏状态，并让 `ShouldAdvanceTime()` 重新返回 `true`。
    3.  这确保了 `GameLoop` 本身的线程永远不会被用户输入阻塞。

-----

### 4\. 结论与最终建议

**ADR-024 整体上是一个高质量、高可行性的架构决策。** 它为构建一个可维护、可测试、确定性的回合制游戏奠定了坚实的基础。

**强制性修改 (Mandatory Actions):**

1.  **修正 `CombatScheduler`**: 必须将 `List<T>` 的实现替换为 `PriorityQueue<TElement, TPriority>`，并更新文档中关于性能复杂度的错误声明。**此项为最高优先级。**

**强烈建议的修改 (Strongly Recommended Actions):**

1.  **增加并发控制**: 在 `GameLoop` 的计时器回调中加入锁（如 `SemaphoreSlim`）或改变计时器模式，以防止tick处理的重入问题。
2.  **明确平局规则**: 在ADR中详细定义 `Scheduler` 在处理同一 `TimeUnit` 上的多个actor时的排序规则，确保100%的确定性。
3.  **澄清玩家输入模型**: 阐明等待玩家输入时 `GameLoop` 的行为模式，确保它不会被I/O操作阻塞。

在采纳上述修改后，这份ADR将成为项目核心循环的一个极其稳固的指导性文档。