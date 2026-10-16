using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockBlastClone
{
    // Plain DTO for Unity JsonUtility. Never persist Unity objects or display-only state.
    [Serializable]
    public sealed class BlockBlastSnapshot
    {
        public int version = 1;
        public int[] board;
        public BlockPieceSnapshot[] hand;
        public int score;
        public int combo;
        public int misses;
        public int moves;
        public int handNumber;
        public bool gameOver;
    }

    [Serializable]
    public sealed class BlockPieceSnapshot
    {
        public bool active;
        public int color;
        public int[] coordinates;
    }

    public sealed partial class BlockBlastModel
    {
        public BlockBlastSnapshot ExportSnapshot()
        {
            BlockBlastSnapshot snapshot = new BlockBlastSnapshot
            {
                version = 1,
                board = new int[BoardSize * BoardSize],
                hand = new BlockPieceSnapshot[Hand.Length],
                score = Score,
                combo = Combo,
                misses = PlacementsWithoutClear,
                moves = Moves,
                handNumber = HandNumber,
                gameOver = GameOver
            };

            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    snapshot.board[r * BoardSize + c] = Board[r, c];
                }
            }

            for (int i = 0; i < Hand.Length; i++)
            {
                BlockPiece piece = Hand[i];
                if (piece == null)
                {
                    // Explicit marker instead of null for robust Unity JsonUtility round-trips.
                    snapshot.hand[i] = new BlockPieceSnapshot { active = false };
                    continue;
                }

                BlockPieceSnapshot savedPiece = new BlockPieceSnapshot
                {
                    active = true,
                    color = piece.Color,
                    coordinates = new int[piece.Cells.Length * 2]
                };
                for (int j = 0; j < piece.Cells.Length; j++)
                {
                    savedPiece.coordinates[j * 2] = piece.Cells[j].x;
                    savedPiece.coordinates[j * 2 + 1] = piece.Cells[j].y;
                }
                snapshot.hand[i] = savedPiece;
            }

            return snapshot;
        }

        // Fail closed on incomplete/malformed saves, without modifying the current game.
        public bool TryRestore(BlockBlastSnapshot snapshot)
        {
            if (snapshot == null || snapshot.version != 1 ||
                snapshot.board == null || snapshot.board.Length != BoardSize * BoardSize ||
                snapshot.hand == null || snapshot.hand.Length != Hand.Length ||
                snapshot.score < 0 || snapshot.combo < 0 || snapshot.misses < 0 ||
                snapshot.moves < 0 || snapshot.handNumber < 1 || snapshot.combo > snapshot.moves ||
                snapshot.misses > snapshot.moves || snapshot.moves < snapshot.handNumber - 1)
            {
                return false;
            }

            int[,] restoredBoard = new int[BoardSize, BoardSize];
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    int color = snapshot.board[r * BoardSize + c];
                    if (color < 0 || color > 7)
                    {
                        return false;
                    }
                    restoredBoard[r, c] = color;
                }
            }

            BlockPiece[] restoredHand = new BlockPiece[Hand.Length];
            int activePieces = 0;
            for (int i = 0; i < Hand.Length; i++)
            {
                BlockPieceSnapshot saved = snapshot.hand[i];
                if (saved == null || !saved.active)
                {
                    if (saved != null && (saved.color != 0 || (saved.coordinates != null && saved.coordinates.Length != 0)))
                    {
                        return false;
                    }
                    continue;
                }

                if (saved.color < 1 || saved.color > 7 || saved.coordinates == null ||
                    saved.coordinates.Length < 2 || saved.coordinates.Length > 18 ||
                    saved.coordinates.Length % 2 != 0)
                {
                    return false;
                }

                Vector2Int[] cells = new Vector2Int[saved.coordinates.Length / 2];
                HashSet<int> visited = new HashSet<int>();
                for (int j = 0; j < cells.Length; j++)
                {
                    int x = saved.coordinates[j * 2];
                    int y = saved.coordinates[j * 2 + 1];
                    if (x < 0 || x >= BoardSize || y < 0 || y >= BoardSize ||
                        !visited.Add(y * BoardSize + x))
                    {
                        return false;
                    }
                    cells[j] = new Vector2Int(x, y);
                }

                // Only pieces from our actual catalogue can appear in a valid hand.
                bool validShape = false;
                foreach (Vector2Int[] shape in shapes)
                {
                    if (shape.Length != cells.Length)
                    {
                        continue;
                    }
                    bool matching = true;
                    foreach (Vector2Int cell in shape)
                    {
                        if (!visited.Contains(cell.y * BoardSize + cell.x))
                        {
                            matching = false;
                            break;
                        }
                    }
                    if (matching)
                    {
                        validShape = true;
                        break;
                    }
                }
                if (!validShape)
                {
                    return false;
                }
                restoredHand[i] = new BlockPiece(cells, saved.color);
                activePieces++;
            }

            if (activePieces == 0)
            {
                return false;
            }

            // A cleared game board can never contain an already completed row/column.
            for (int i = 0; i < BoardSize; i++)
            {
                bool fullRow = true;
                bool fullColumn = true;
                for (int j = 0; j < BoardSize; j++)
                {
                    if (restoredBoard[i, j] == 0) fullRow = false;
                    if (restoredBoard[j, i] == 0) fullColumn = false;
                }
                if (fullRow || fullColumn)
                {
                    return false;
                }
            }

            bool anyMove = false;
            foreach (BlockPiece piece in restoredHand)
            {
                if (piece == null)
                {
                    continue;
                }
                for (int r = 0; r <= BoardSize - piece.Height && !anyMove; r++)
                {
                    for (int c = 0; c <= BoardSize - piece.Width && !anyMove; c++)
                    {
                        bool free = true;
                        foreach (Vector2Int cell in piece.Cells)
                        {
                            if (restoredBoard[r + cell.y, c + cell.x] != 0)
                            {
                                free = false;
                                break;
                            }
                        }
                        if (free)
                        {
                            anyMove = true;
                        }
                    }
                }
            }
            if (snapshot.gameOver == anyMove)
            {
                return false;
            }

            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    Board[r, c] = restoredBoard[r, c];
                }
            }
            for (int i = 0; i < Hand.Length; i++)
            {
                Hand[i] = restoredHand[i];
            }
            Score = snapshot.score;
            Combo = snapshot.combo;
            PlacementsWithoutClear = snapshot.misses;
            Moves = snapshot.moves;
            HandNumber = snapshot.handNumber;
            GameOver = snapshot.gameOver;
            return true;
        }
    }
}
