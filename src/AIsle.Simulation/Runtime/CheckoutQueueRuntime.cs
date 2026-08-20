using System;
using System.Collections.Generic;
using AIsle.Contracts.Simulation;

namespace AIsle.Simulation.Runtime
{
    internal enum CheckoutPhase { None, ApproachQueue, WaitingQueue, ApproachService, Serving }

    internal sealed class CheckoutAssignment
    {
        public string NpcId = string.Empty;
        public int QueueIndex = -1;
        public bool IsService;
        public Position2D Position = new Position2D();
        public List<Position2D> Path = new List<Position2D>();
    }

    // Task 11 checkout geometry is intentionally specialized: one service point
    // and one FIFO line on the left side of the vertically oriented fixture.
    internal sealed class CheckoutQueueRuntime
    {
        internal const double FixtureWidth = 1.0;
        internal const double FixtureHeight = 2.4;

        private readonly PathGrid _grid;
        private readonly Position2D _service;
        private readonly double _spacing;
        private readonly int _direction;
        private readonly int _capacity;
        private readonly List<string> _queue = new List<string>();
        private string _serviceOwner = string.Empty;

        internal CheckoutQueueRuntime(LayoutDefinition layout, PathGrid grid, SimulationConfig config)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            var stopTolerance = Math.Max(0.01, Math.Min(0.05, config.PathCellSize * 0.2));
            // RVO uses half CollisionRadius as the physical agent radius.
            // Keep the service point outside the fixture without consuming the
            // extra wall/shelf raster margin, which is not part of checkout.
            var offset = (config.CollisionRadius * 0.5) + stopTolerance;
            _spacing = Math.Max(config.CollisionRadius + stopTolerance, config.PathCellSize);
            _service = new Position2D(layout.Checkout.X - (FixtureWidth * 0.5) - offset, layout.Checkout.Y);

            var upward = ComputeCapacity(layout, -1);
            var downward = ComputeCapacity(layout, 1);
            _direction = upward >= downward ? -1 : 1;
            _capacity = Math.Max(upward, downward);
        }

        internal Position2D ServicePosition => new Position2D(_service.X, _service.Y);
        internal int Direction => _direction;
        internal int Capacity => _capacity;
        internal int QueueLength => _queue.Count;
        internal string ServiceOwner => _serviceOwner;
        internal IReadOnlyList<string> Queue => _queue;

        internal Position2D QueuePosition(int index) =>
            index < 0 || index >= _capacity ? null : new Position2D(_service.X, _service.Y + (_direction * _spacing * (index + 1)));

        internal CheckoutAssignment TryEnter(string npcId, Position2D from)
        {
            if (string.IsNullOrWhiteSpace(npcId) || from == null || OwnsOrQueues(npcId)) return null;
            if (string.IsNullOrEmpty(_serviceOwner) && _grid.IsPointWalkable(_service))
            {
                var path = _grid.FindPath(from, _service);
                if (path == null) return null;
                _serviceOwner = npcId;
                return Assignment(npcId, -1, true, _service, path);
            }

            if (_queue.Count >= _capacity) return null;
            var position = QueuePosition(_queue.Count);
            var queuePath = position == null ? null : _grid.FindPath(from, position);
            if (queuePath == null) return null;
            _queue.Add(npcId);
            return Assignment(npcId, _queue.Count - 1, false, position, queuePath);
        }

        internal bool MarkServing(string npcId) => string.Equals(_serviceOwner, npcId, StringComparison.Ordinal);

        internal bool ReleaseService(string npcId)
        {
            if (!string.Equals(_serviceOwner, npcId, StringComparison.Ordinal)) return false;
            _serviceOwner = string.Empty;
            return true;
        }

        internal bool LeaveQueue(string npcId)
        {
            var index = _queue.IndexOf(npcId);
            if (index < 0) return false;
            _queue.RemoveAt(index);
            return true;
        }

        internal CheckoutAssignment TryPromote(Func<string, Position2D> positionOf)
        {
            if (!string.IsNullOrEmpty(_serviceOwner) || _queue.Count == 0) return null;
            var npcId = _queue[0];
            var current = positionOf(npcId);
            if (current == null) return null;
            var path = _grid.FindPath(current, _service);
            if (path == null) return null;
            _queue.RemoveAt(0);
            _serviceOwner = npcId;
            return Assignment(npcId, -1, true, _service, path);
        }

        internal IReadOnlyList<CheckoutAssignment> Reflow(Func<string, Position2D> positionOf)
        {
            var result = new List<CheckoutAssignment>();
            for (var index = 0; index < _queue.Count; index++)
            {
                var current = positionOf(_queue[index]);
                var position = QueuePosition(index);
                if (current == null || position == null) continue;
                var path = _grid.FindPath(current, position);
                if (path != null) result.Add(Assignment(_queue[index], index, false, position, path));
            }
            return result;
        }

        private int ComputeCapacity(LayoutDefinition layout, int direction)
        {
            if (!_grid.IsPointWalkable(_service) || _service.X < 0 || _service.X > layout.Width) return 0;
            var capacity = 0;
            var limit = Math.Max(0, (int)Math.Ceiling(layout.Height / _spacing) + 1);
            for (var index = 0; index < limit; index++)
            {
                var position = new Position2D(_service.X, _service.Y + (direction * _spacing * (index + 1)));
                if (!double.IsFinite(position.X) || !double.IsFinite(position.Y) || position.Y < 0 || position.Y > layout.Height) break;
                if (!_grid.IsPointWalkable(position) || !_grid.LineIsWalkable(index == 0 ? _service : new Position2D(_service.X, _service.Y + (direction * _spacing * index)), position)) break;
                capacity++;
            }
            return capacity;
        }

        private bool OwnsOrQueues(string npcId) =>
            string.Equals(_serviceOwner, npcId, StringComparison.Ordinal) || _queue.Contains(npcId);

        private static CheckoutAssignment Assignment(string npcId, int index, bool service, Position2D position, List<Position2D> path) =>
            new CheckoutAssignment { NpcId = npcId, QueueIndex = index, IsService = service, Position = new Position2D(position.X, position.Y), Path = path };
    }
}
