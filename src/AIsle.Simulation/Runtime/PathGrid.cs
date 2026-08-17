using System;
using System.Collections.Generic;
using AIsle.Contracts.Simulation;

namespace AIsle.Simulation.Runtime
{
    public sealed class PathGrid
    {
        private readonly LayoutDefinition _layout; private readonly double _cell; private readonly int _cols; private readonly int _rows; private readonly bool[] _blocked;
        public PathGrid(LayoutDefinition layout, SimulationConfig config)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout)); _cell = config.PathCellSize;
            _cols = (int)Math.Ceiling(layout.Width / _cell); _rows = (int)Math.Ceiling(layout.Height / _cell); _blocked = new bool[_cols * _rows]; Mark(config.ObstacleMargin);
        }
        private int Key(int column, int row) => row * _cols + column;
        private bool Ok(int column, int row) => column >= 0 && row >= 0 && column < _cols && row < _rows && !_blocked[Key(column, row)];
        private Position2D Center(int column, int row) => new Position2D((column + 0.5) * _cell, (row + 0.5) * _cell);
        private void CellAt(Position2D point, out int column, out int row) { column = (int)Math.Floor(point.X / _cell); row = (int)Math.Floor(point.Y / _cell); }
        public bool IsPointWalkable(Position2D point) { CellAt(point, out var column, out var row); return Ok(column, row); }

        private void Mark(double margin)
        {
            for (var row = 0; row < _rows; row++) for (var column = 0; column < _cols; column++)
            {
                var point = Center(column, row); var blocked = false;
                var shelves = _layout.Shelves ?? Array.Empty<ShelfDefinition>();
                for (var index = 0; index < shelves.Length && !blocked; index++)
                {
                    var shelf = shelves[index]; blocked = point.X >= shelf.X - margin && point.X <= shelf.X + shelf.Width + margin && point.Y >= shelf.Y - margin && point.Y <= shelf.Y + shelf.Height + margin;
                }
                var walls = _layout.Walls ?? Array.Empty<WallDefinition>();
                for (var index = 0; index < walls.Length && !blocked; index++)
                {
                    var wall = walls[index]; blocked = PointSegmentDistance(point, new Position2D(wall.X1, wall.Y1), new Position2D(wall.X2, wall.Y2)) <= margin + 0.06;
                }
                _blocked[Key(column, row)] = blocked;
            }
        }

        public List<Position2D> FindPath(Position2D from, Position2D to)
        {
            CellAt(from, out var sourceColumn, out var sourceRow); CellAt(to, out var targetColumn, out var targetRow);
            var start = Nearest(sourceColumn, sourceRow); var end = Nearest(targetColumn, targetRow); if (start == null || end == null) return null;
            var total = _cols * _rows; var costs = new double[total]; var came = new int[total]; var closed = new bool[total];
            for (var index = 0; index < total; index++) { costs[index] = double.PositiveInfinity; came[index] = -1; }
            var startKey = Key(start.Column, start.Row); var endKey = Key(end.Column, end.Row); costs[startKey] = 0.0;
            var open = new MinHeap(); open.Push(new GridNode(start.Column, start.Row, 0.0));
            var directions = new[] { -1,0, 1,0, 0,-1, 0,1, -1,-1, 1,-1, -1,1, 1,1 };
            while (open.Count > 0)
            {
                var current = open.Pop(); var key = Key(current.Column, current.Row); if (closed[key]) continue;
                if (key == endKey)
                {
                    var points = new List<Position2D>(); var cursor = key;
                    while (cursor != -1) { points.Insert(0, Center(cursor % _cols, cursor / _cols)); cursor = came[cursor]; }
                    if (IsPointWalkable(from) && LineIsWalkable(from, points[0])) points[0] = new Position2D(from.X, from.Y);
                    if (IsPointWalkable(to) && LineIsWalkable(points[points.Count - 1], to)) points[points.Count - 1] = new Position2D(to.X, to.Y);
                    return Smooth(points);
                }
                closed[key] = true;
                for (var direction = 0; direction < directions.Length; direction += 2)
                {
                    var dc = directions[direction]; var dr = directions[direction + 1]; var column = current.Column + dc; var row = current.Row + dr;
                    if (!Ok(column, row) || (dc != 0 && dr != 0 && (!Ok(current.Column + dc, current.Row) || !Ok(current.Column, current.Row + dr)))) continue;
                    var nextKey = Key(column, row); var nextCost = costs[key] + (dc != 0 && dr != 0 ? 1.414 : 1.0);
                    if (nextCost < costs[nextKey]) { costs[nextKey] = nextCost; came[nextKey] = key; open.Push(new GridNode(column, row, nextCost + Math.Sqrt(((column - end.Column) * (column - end.Column)) + ((row - end.Row) * (row - end.Row))))); }
                }
            }
            return null;
        }

        public bool LineIsWalkable(Position2D from, Position2D to)
        {
            var steps = Math.Max(1, (int)Math.Ceiling(SimulationMath.Distance(from, to) / (_cell * 0.3)));
            for (var index = 0; index <= steps; index++) { var t = (double)index / steps; if (!IsPointWalkable(new Position2D(from.X + ((to.X - from.X) * t), from.Y + ((to.Y - from.Y) * t)))) return false; }
            return true;
        }
        public double PathLength(IList<Position2D> path) { var total = 0.0; for (var index = 1; index < path.Count; index++) total += SimulationMath.Distance(path[index - 1], path[index]); return total; }

        public List<PathAccess> ShelfAccessPaths(ShelfDefinition shelf, Position2D from)
        {
            var gap = Math.Max(0.42, _cell * 2.0); var points = new[] { new Position2D(shelf.X-gap,shelf.Y+shelf.Height/2), new Position2D(shelf.X+shelf.Width+gap,shelf.Y+shelf.Height/2), new Position2D(shelf.X+shelf.Width/2,shelf.Y-gap), new Position2D(shelf.X+shelf.Width/2,shelf.Y+shelf.Height+gap) };
            var result = new List<PathAccess>();
            for (var index = 0; index < points.Length; index++)
            {
                points[index].X = SimulationMath.Clamp(points[index].X, 0.2, _layout.Width - 0.2); points[index].Y = SimulationMath.Clamp(points[index].Y, 0.2, _layout.Height - 0.2);
                if (!IsPointWalkable(points[index])) continue; var path = FindPath(from, points[index]); if (path != null) result.Add(new PathAccess { Point = points[index], Path = path, Length = PathLength(path) });
            }
            result.Sort((left, right) => left.Length.CompareTo(right.Length)); return result;
        }

        private List<Position2D> Smooth(List<Position2D> points)
        {
            if (points.Count < 3) return points; var result = new List<Position2D> { points[0] }; var index = 0;
            while (index < points.Count - 1) { var far = index + 1; for (var candidate = points.Count - 1; candidate > index + 1; candidate--) if (LineIsWalkable(points[index], points[candidate])) { far = candidate; break; } result.Add(points[far]); index = far; }
            return result;
        }
        private Cell Nearest(int column, int row)
        {
            if (Ok(column, row)) return new Cell(column, row); var limit = Math.Max(_cols, _rows);
            for (var radius = 1; radius < limit; radius++) for (var y = row - radius; y <= row + radius; y++) for (var x = column - radius; x <= column + radius; x++) if (Ok(x, y)) return new Cell(x, y);
            return null;
        }
        private static double PointSegmentDistance(Position2D point, Position2D a, Position2D b)
        {
            var dx = b.X-a.X; var dy = b.Y-a.Y; var length = dx*dx+dy*dy; var t = length == 0 ? 0 : SimulationMath.Clamp(((point.X-a.X)*dx+(point.Y-a.Y)*dy)/length,0,1);
            return SimulationMath.Distance(point,new Position2D(a.X+t*dx,a.Y+t*dy));
        }
        private sealed class Cell { public readonly int Column; public readonly int Row; public Cell(int column,int row){Column=column;Row=row;} }
        private sealed class GridNode { public readonly int Column; public readonly int Row; public readonly double Score; public GridNode(int column,int row,double score){Column=column;Row=row;Score=score;} }
        private sealed class MinHeap
        {
            private readonly List<GridNode> _items=new List<GridNode>(); public int Count=>_items.Count;
            public void Push(GridNode node){_items.Add(node);var index=_items.Count-1;while(index>0){var parent=(index-1)/2;if(_items[parent].Score<=node.Score)break;_items[index]=_items[parent];index=parent;}_items[index]=node;}
            public GridNode Pop(){var root=_items[0];var last=_items[_items.Count-1];_items.RemoveAt(_items.Count-1);if(_items.Count>0){var index=0;while(true){var left=index*2+1;var right=left+1;if(left>=_items.Count)break;var child=right<_items.Count&&_items[right].Score<_items[left].Score?right:left;if(_items[child].Score>=last.Score)break;_items[index]=_items[child];index=child;}_items[index]=last;}return root;}
        }
    }
    public sealed class PathAccess { public Position2D Point; public List<Position2D> Path; public double Length; }
}
