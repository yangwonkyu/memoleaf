using System;
using System.Collections.Generic;

namespace Portfolio.FoldGrid
{
    // Independent educational rewrite, not production source.
    public struct Piece
    {
        public readonly int X, Y, Width, Height;
        public readonly bool OnFlap;
        public Piece(int x, int y, int width, int height, bool onFlap)
        { X = x; Y = y; Width = width; Height = height; OnFlap = onFlap; }
    }

    public static class FoldRules
    {
        public static bool Fits(Piece piece, int columns, int rows)
        {
            return columns > 0 && rows > 0 && piece.Width > 0 && piece.Height > 0
                && piece.X >= 0 && piece.Y >= 0
                && piece.Width <= columns && piece.Height <= rows
                && piece.X <= columns - piece.Width && piece.Y <= rows - piece.Height;
        }

        public static Piece Project(Piece piece, int columns, int rows)
        {
            if (!Fits(piece, columns, rows))
                throw new ArgumentOutOfRangeException("piece", "Footprint must fit the open panel.");
            // Reflect the entire rectangle, not just its top-left cell.
            int x = piece.OnFlap ? columns - piece.X - piece.Width : piece.X;
            return new Piece(x, piece.Y, piece.Width, piece.Height, false);
        }

        public static bool Overlaps(Piece a, Piece b)
        {
            return a.X < b.X + b.Width && b.X < a.X + a.Width
                && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
        }

        public static bool CanFold(IReadOnlyList<Piece> pieces, int columns, int rows)
        {
            if (pieces == null) throw new ArgumentNullException("pieces");
            if (columns <= 0 || rows <= 0) return false;
            for (int i = 0; i < pieces.Count; i++)
            {
                if (!Fits(pieces[i], columns, rows)) return false;
                Piece projected = Project(pieces[i], columns, rows);
                for (int j = 0; j < i; j++)
                    if (Overlaps(projected, Project(pieces[j], columns, rows))) return false;
            }
            return true;
        }
    }
}
