using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockBlastClone
{
    // Coordinates: row 0 is the top row; shape cell Y increases downward.
    public sealed class BlockPiece
    {
        public readonly Vector2Int[] Cells;
        public readonly int Color;
        public readonly int Width;
        public readonly int Height;

        public BlockPiece(Vector2Int[] cells, int color)
        {
            Cells = cells;
            Color = color;
            int width = 0;
            int height = 0;
            foreach (Vector2Int cell in cells)
            {
                width = Mathf.Max(width, cell.x + 1);
                height = Mathf.Max(height, cell.y + 1);
            }
            Width = width;
            Height = height;
        }
    }

    public struct PlacementResult
    {
        public int ScoreAdded;
        public int LinesCleared;
        public int Combo;
        public Vector2Int[] ClearedCells;
        public bool NewHand;
        public bool GameOver;
    }

    public sealed partial class BlockBlastModel
    {
        public const int BoardSize = 8;
        public readonly int[,] Board = new int[BoardSize, BoardSize];
        public readonly BlockPiece[] Hand = new BlockPiece[3];
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int PlacementsWithoutClear { get; private set; }
        public bool GameOver { get; private set; }
        public int Moves { get; private set; }
        public int HandNumber { get; private set; }

        private readonly System.Random random;
        private readonly List<Vector2Int[]> shapes = new List<Vector2Int[]>();

        public BlockBlastModel(int? seed = null)
        {
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            BuildShapes();
            Reset();
        }

        public void Reset()
        {
            Array.Clear(Board, 0, Board.Length);
            Array.Clear(Hand, 0, Hand.Length);
            Score = 0;
            Combo = 0;
            PlacementsWithoutClear = 0;
            GameOver = false;
            Moves = 0;
            HandNumber = 0;
            DealHand();
        }

        public bool CanPlace(BlockPiece piece, int row, int col)
        {
            if (piece == null || row < 0 || col < 0 || row + piece.Height > BoardSize || col + piece.Width > BoardSize)
            {
                return false;
            }
            foreach (Vector2Int cell in piece.Cells)
            {
                if (Board[row + cell.y, col + cell.x] != 0)
                {
                    return false;
                }
            }
            return true;
        }

        public bool CanFitAnywhere(BlockPiece piece)
        {
            if (piece == null)
            {
                return false;
            }
            for (int row = 0; row <= BoardSize - piece.Height; row++)
            {
                for (int col = 0; col <= BoardSize - piece.Width; col++)
                {
                    if (CanPlace(piece, row, col))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool HasAnyMove()
        {
            foreach (BlockPiece piece in Hand)
            {
                if (CanFitAnywhere(piece))
                {
                    return true;
                }
            }
            return false;
        }

        public bool TryPlace(int slot, int row, int col, out PlacementResult result)
        {
            result = default;
            if (GameOver || slot < 0 || slot >= Hand.Length || !CanPlace(Hand[slot], row, col))
            {
                return false;
            }

            BlockPiece piece = Hand[slot];
            foreach (Vector2Int cell in piece.Cells)
            {
                Board[row + cell.y, col + cell.x] = piece.Color;
            }
            Hand[slot] = null;
            Moves++;

            bool[] fullRows = new bool[BoardSize];
            bool[] fullCols = new bool[BoardSize];
            for (int r = 0; r < BoardSize; r++)
            {
                fullRows[r] = true;
                fullCols[r] = true;
                for (int c = 0; c < BoardSize; c++)
                {
                    if (Board[r, c] == 0)
                    {
                        fullRows[r] = false;
                    }
                    if (Board[c, r] == 0)
                    {
                        fullCols[r] = false;
                    }
                }
            }

            int lineCount = 0;
            for (int i = 0; i < BoardSize; i++)
            {
                if (fullRows[i]) lineCount++;
                if (fullCols[i]) lineCount++;
            }

            List<Vector2Int> cleared = new List<Vector2Int>();
            if (lineCount > 0)
            {
                Combo = PlacementsWithoutClear <= 2 ? Combo + 1 : 1;
                PlacementsWithoutClear = 0;
                for (int r = 0; r < BoardSize; r++)
                {
                    for (int c = 0; c < BoardSize; c++)
                    {
                        if (fullRows[r] || fullCols[c])
                        {
                            cleared.Add(new Vector2Int(c, r));
                            Board[r, c] = 0;
                        }
                    }
                }
            }
            else
            {
                PlacementsWithoutClear++;
                if (PlacementsWithoutClear >= 3)
                {
                    Combo = 0;
                }
            }

            // Baseline clone scoring; source site's exact scoring formula is not published.
            // One point per placed square, ten per unique cleared square, plus multi-line bonus.
            int lineBonus = lineCount > 1 ? lineCount * (lineCount - 1) * 10 : 0;
            int points = piece.Cells.Length + (cleared.Count * 10 + lineBonus) * Mathf.Max(1, Combo);
            Score += points;

            bool emptyHand = true;
            foreach (BlockPiece remaining in Hand)
            {
                if (remaining != null)
                {
                    emptyHand = false;
                    break;
                }
            }
            if (emptyHand)
            {
                DealHand();
            }

            GameOver = !HasAnyMove();
            result = new PlacementResult
            {
                ScoreAdded = points,
                LinesCleared = lineCount,
                Combo = Combo,
                ClearedCells = cleared.ToArray(),
                NewHand = emptyHand,
                GameOver = GameOver
            };
            return true;
        }

        private void DealHand()
        {
            HandNumber++;
            for (int i = 0; i < Hand.Length; i++)
            {
                int shape = random.Next(shapes.Count);
                Hand[i] = new BlockPiece(shapes[shape], random.Next(1, 8));
            }

            // A playable hand is preferred when at least one shape in the library fits.
            // Other pieces remain random; a board with no legal moves still ends the game.
            if (!HasAnyMove())
            {
                List<int> fitting = new List<int>();
                for (int i = 0; i < shapes.Count; i++)
                {
                    if (CanFitAnywhere(new BlockPiece(shapes[i], 1)))
                    {
                        fitting.Add(i);
                    }
                }
                if (fitting.Count > 0)
                {
                    Hand[random.Next(Hand.Length)] = new BlockPiece(shapes[fitting[random.Next(fitting.Count)]], random.Next(1, 8));
                }
            }
        }

        private void AddShape(params string[] rows)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < rows[r].Length; c++)
                {
                    if (rows[r][c] == '#')
                    {
                        cells.Add(new Vector2Int(c, r));
                    }
                }
            }
            shapes.Add(cells.ToArray());
        }

        private void BuildShapes()
        {
            AddShape("#");
            AddShape("##"); AddShape("#", "#");
            AddShape("###"); AddShape("#", "#", "#");
            AddShape("####"); AddShape("#", "#", "#", "#");
            AddShape("#####"); AddShape("#", "#", "#", "#", "#");
            AddShape("##", "##");
            AddShape("###", "###");
            AddShape("###", "###", "###");
            AddShape("##", "#."); AddShape("##", ".#");
            AddShape("#.", "##"); AddShape(".#", "##");
            AddShape("###", "#.."); AddShape("###", "..#");
            AddShape("#..", "###"); AddShape("..#", "###");
            AddShape("#.", "#.", "##"); AddShape(".#", ".#", "##");
            AddShape("##", "#.", "#."); AddShape("##", ".#", ".#");
            AddShape("###", ".#."); AddShape(".#.", "###");
            AddShape("#.", "##", "#."); AddShape(".#", "##", ".#");
            AddShape(".##", "##."); AddShape("##.", ".##");
            AddShape(".#", "##", "#."); AddShape("#.", "##", ".#");
            AddShape("##.", ".##", "..#"); AddShape("..#", ".##", "##.");
        }
    }
}
