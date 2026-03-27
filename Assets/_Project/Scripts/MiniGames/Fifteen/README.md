# Fifteen Puzzle MiniGame

Классические пятнашки (с текущими правилами проекта), управляются через интеракцию игрока.

## Как работает

1. `FifteenPuzzlePanel` собирает плитки и определяет пустую клетку.
2. При старте выполняется `Shuffle()`.
3. Игрок нажимает плитку через интеракцию.
4. Если плитка соседняя с пустой клеткой (в текущей версии допускаются и диагонали), она двигается.
5. После каждого хода проверяется состояние “собрано”.
6. На успех:
   - опционально блокируется интеракция с плитками,
   - проигрывается анимация `successCover`,
   - вызывается `onSuccess`.

## Скрипты

- `FifteenPuzzlePanel.cs` — основная логика поля, shuffle, проверка решения, success sequence.
- `FifteenPuzzleTile.cs` — плитка: значение, solved-cell, визуал, анимация перемещения.

## Ключевые настройки (`FifteenPuzzlePanel`)

- `gridSize`, `cellSize`, `boardOriginLocal`, `centerBoard` — геометрия.
- `autoShuffleOnEnable`, `shuffleMoves` — перемешивание.
- `useImageSlices`, `puzzleImage`, `mirrorImageSlicesHorizontally` — работа с картинкой.
- `successCover` и related-параметры — поведение закрывающей плитки на успех.
- `onSuccess` — действие при прохождении.

## Ключевые настройки (`FifteenPuzzleTile`)

- `tileValue`, `solvedCell` — логическая позиция плитки.
- `moveAnimationDuration` — скорость перемещения.
- `allowMouseClick` — опциональный mouse fallback.
- Цвета/автоцвет и image-slice настраиваются автоматически панелью.
