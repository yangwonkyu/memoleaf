using System;
using UnityEngine;
using Portfolio.FoldGrid;

public static class ValidateFoldGrid
{
    [UnityEditor.MenuItem("Portfolio/Validate Fold Rules")]
    public static void Run()
    {
        int checkedCases = 0;
        Action<bool, string> check = (condition, label) =>
        { if (!condition) throw new Exception(label); checkedCases++; };
        for (int columns = 1; columns <= 6; columns++)
        for (int rows = 1; rows <= 4; rows++)
        for (int width = 1; width <= columns; width++)
        for (int height = 1; height <= rows; height++)
        for (int y = 0; y <= rows - height; y++)
        for (int x = 0; x <= columns - width; x++)
        {
            var original = new Piece(x, y, width, height, true);
            var projected = FoldRules.Project(original, columns, rows);
            check(FoldRules.Fits(projected, columns, rows), "Projection left board");
            var twice = FoldRules.Project(new Piece(projected.X, projected.Y, width, height, true), columns, rows);
            check(twice.X == x && twice.Y == y, "Reflection is not involutive");
            // Compare every occupied cell to an independently reflected cell set.
            for (int dx = 0; dx < width; dx++)
                check(columns - 1 - (x + dx) >= projected.X
                    && columns - 1 - (x + dx) < projected.X + width, "Footprint mismatch");
        }
        check(!FoldRules.CanFold(new[] { new Piece(0,0,2,1,true), new Piece(2,0,1,1,false) },4,3), "Missed fold collision");
        check(FoldRules.CanFold(new[] { new Piece(0,0,2,1,true), new Piece(1,0,1,1,false) },4,3), "Edge contact rejected");
        check(!FoldRules.CanFold(new[] { new Piece(3,0,2,1,true) },4,3), "Out of bounds accepted");
        check(!FoldRules.CanFold(new[] { new Piece(0,0,0,1,false) },4,3), "Zero width accepted");
        check(!FoldRules.CanFold(new[] { new Piece(-1,0,1,1,false) },4,3), "Negative anchor accepted");
        check(FoldRules.CanFold(new Piece[0],4,3), "Empty layout should be clear");
        check(!FoldRules.CanFold(new Piece[0],0,3), "Invalid board accepted");
        Debug.Log("FOLD_GRID_VALIDATION_PASSED checks=" + checkedCases);
    }

    public static void ValidateAndCreate()
    {
        Run();
        CreateFoldGridScene.Create();
    }
}
