using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.FoldGrid
{
    public sealed class FoldGridDemo : MonoBehaviour
    {
        private const int Columns = 4, Rows = 3;
        private readonly List<Piece> pieces = new List<Piece>();
        private bool flap = true;
        private bool preview;
        private int width = 1, height = 1;
        private string message = "Choose a size, then click a cell on the selected panel.";
        private readonly Color[] colors = { Color.cyan, Color.yellow, Color.magenta, Color.green };

        private void OnGUI()
        {
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1000f, Screen.height / 650f, 1));
            GUI.Label(new Rect(25, 15, 950, 30), "FOLD GRID | Independent portfolio reconstruction");
            flap = GUI.Toggle(new Rect(25, 55, 220, 30), flap, "Place on left flap (mirrored)");
            if (GUI.Button(new Rect(270, 55, 100, 30), "1 x 1")) { width = 1; height = 1; }
            if (GUI.Button(new Rect(380, 55, 100, 30), "2 x 1")) { width = 2; height = 1; }
            if (GUI.Button(new Rect(490, 55, 100, 30), "1 x 2")) { width = 1; height = 2; }
            GUI.Label(new Rect(620, 55, 300, 30), "Selected: " + width + " x " + height);
            DrawPanel(30, 135, true);
            DrawPanel(530, 135, false);
            if (GUI.Button(new Rect(30, 420, 170, 40), "Check folded layout"))
            {
                preview = true;
                message = FoldRules.CanFold(pieces, Columns, Rows)
                    ? "CLEAR: all projected footprints are disjoint."
                    : "COLLISION: projected footprints overlap.";
            }
            if (GUI.Button(new Rect(215, 420, 120, 40), "Undo") && pieces.Count > 0)
            { pieces.RemoveAt(pieces.Count - 1); preview = false; message = "Last piece removed."; }
            if (GUI.Button(new Rect(350, 420, 120, 40), "Reset"))
            { pieces.Clear(); preview = false; message = "Layout cleared."; }
            GUI.Label(new Rect(30, 480, 930, 40), message);
            GUI.Label(new Rect(30, 530, 930, 70),
                "Reflection: foldedX = columns - openX - pieceWidth\n"
                + "Touching edges are allowed. Overlapping cells are rejected. Click placement is used for this focused sample.");
        }

        private void DrawPanel(float originX, float originY, bool isFlap)
        {
            GUI.Label(new Rect(originX, originY - 35, 440, 30),
                isFlap ? "LEFT FLAP" : preview ? "FOLDED RESULT" : "BASE PANEL");
            const float cell = 90;
            for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Columns; x++)
                if (GUI.Button(new Rect(originX + x * cell, originY + y * cell, cell - 2, cell - 2), x + "," + y)
                    && isFlap == flap)
                    TryAdd(new Piece(x, y, width, height, isFlap));

            for (int i = 0; i < pieces.Count; i++)
            {
                Piece p = pieces[i];
                if (preview && !isFlap) p = FoldRules.Project(p, Columns, Rows);
                else if (p.OnFlap != isFlap) continue;
                Color old = GUI.color;
                GUI.color = colors[i % colors.Length];
                GUI.Box(new Rect(originX + p.X * cell + 5, originY + p.Y * cell + 5,
                    p.Width * cell - 12, p.Height * cell - 12), (i + 1) + " (" + p.Width + "x" + p.Height + ")");
                GUI.color = old;
            }
        }

        private void TryAdd(Piece candidate)
        {
            if (!FoldRules.Fits(candidate, Columns, Rows))
            { message = "Rejected: footprint extends outside the panel."; return; }
            foreach (Piece existing in pieces)
                if (existing.OnFlap == candidate.OnFlap && FoldRules.Overlaps(existing, candidate))
                { message = "Rejected: overlap on the open panel."; return; }
            pieces.Add(candidate);
            preview = false;
            message = "Piece added. Check the fold to reveal cross-panel collisions.";
        }
    }
}
