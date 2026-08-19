using System;
using System.Collections.Generic;
using System.Linq;
using AIsle.Contracts.Simulation;

namespace AIsle.Simulation.Runtime
{
    internal enum ShelfSlotState { Free, Reserved, Occupied }
    internal enum ShelfAccessPhase { None, ApproachSlot, ApproachQueue, WaitingQueue, Interacting }

    internal sealed class ShelfInteractionSlot
    {
        public string Id = string.Empty;
        public string ShelfId = string.Empty;
        public ShelfSide Side;
        public int Index;
        public Position2D Position = new Position2D();
        public Position2D Facing = new Position2D();
        public ShelfSlotState State;
        public string OwnerNpcId = string.Empty;
    }

    internal sealed class ShelfSlotCandidate
    {
        public ShelfInteractionSlot Slot = null!;
        public List<Position2D> Path = new List<Position2D>();
        public double Length;
    }

    internal sealed class ShelfQueueAssignment
    {
        public string NpcId = string.Empty;
        public string ShelfId = string.Empty;
        public ShelfSide Side;
        public int Index;
        public Position2D Position = new Position2D();
        public List<Position2D> Path = new List<Position2D>();
        public double Length;
    }

    internal sealed class ShelfPromotion
    {
        public string NpcId = string.Empty;
        public ShelfInteractionSlot Slot = null!;
        public List<Position2D> Path = new List<Position2D>();
        public string[] IneligibleNpcIds = Array.Empty<string>();
    }

    internal sealed class ShelfInteractionRuntime
    {
        private readonly LayoutDefinition _layout;
        private readonly PathGrid _grid;
        private readonly double _queueSpacing;
        private readonly Dictionary<string, ShelfInteractionSlot> _slots = new Dictionary<string, ShelfInteractionSlot>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> _queues = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        public ShelfInteractionRuntime(LayoutDefinition layout, PathGrid grid, SimulationConfig config)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            var stopTolerance = Math.Max(0.01, Math.Min(0.05, config.PathCellSize * 0.2));
            _queueSpacing = Math.Max(config.CollisionRadius + stopTolerance, config.PathCellSize);
            var shelves = layout.Shelves ?? Array.Empty<ShelfDefinition>();
            for (var shelfIndex = 0; shelfIndex < shelves.Length; shelfIndex++)
            {
                var geometries = grid.ShelfInteractionSlots(shelves[shelfIndex]);
                for (var index = 0; index < geometries.Count; index++)
                {
                    var geometry = geometries[index];
                    _slots[geometry.Key] = new ShelfInteractionSlot
                    {
                        Id = geometry.Key,
                        ShelfId = geometry.ShelfId,
                        Side = geometry.Side,
                        Index = geometry.Index,
                        Position = new Position2D(geometry.Position.X, geometry.Position.Y),
                        Facing = new Position2D(geometry.Facing.X, geometry.Facing.Y)
                    };
                    _queues.TryAdd(QueueKey(geometry.ShelfId, geometry.Side), new List<string>());
                }
            }
        }

        internal IReadOnlyCollection<ShelfInteractionSlot> Slots => _slots.Values;
        internal int QueueLength(string shelfId, ShelfSide side) => Queue(shelfId, side).Count;
        internal int TotalQueueLength => _queues.Values.Sum(queue => queue.Count);
        internal int MaxQueueLength => _queues.Values.Count == 0 ? 0 : _queues.Values.Max(queue => queue.Count);

        internal List<ShelfSlotCandidate> Preview(string shelfId, Position2D from) =>
            Reachable(shelfId, from, slot => true, 4);

        internal List<ShelfSlotCandidate> Free(string shelfId, Position2D from) =>
            Reachable(shelfId, from, slot => slot.State == ShelfSlotState.Free && Queue(slot.ShelfId, slot.Side).Count == 0, 6);

        internal bool TryReserve(ShelfInteractionSlot slot, string npcId)
        {
            if (slot == null || string.IsNullOrWhiteSpace(npcId) || slot.State != ShelfSlotState.Free) return false;
            slot.State = ShelfSlotState.Reserved;
            slot.OwnerNpcId = npcId;
            return true;
        }

        internal bool MarkOccupied(string npcId)
        {
            var slot = SlotOwnedBy(npcId);
            if (slot == null || slot.State != ShelfSlotState.Reserved) return false;
            slot.State = ShelfSlotState.Occupied;
            return true;
        }

        internal ShelfInteractionSlot ReleaseSlot(string npcId)
        {
            var slot = SlotOwnedBy(npcId);
            if (slot == null) return null;
            slot.State = ShelfSlotState.Free;
            slot.OwnerNpcId = string.Empty;
            return slot;
        }

        internal ShelfQueueAssignment TryJoinQueue(string shelfId, string npcId, Position2D from)
        {
            if (IsQueued(npcId)) return null;
            var sides = _slots.Values.Where(slot => slot.ShelfId == shelfId).Select(slot => slot.Side).Distinct().OrderBy(side => side).ToArray();
            ShelfQueueAssignment best = null;
            for (var index = 0; index < sides.Length; index++)
            {
                var side = sides[index];
                var queue = Queue(shelfId, side);
                var position = QueuePosition(shelfId, side, queue.Count);
                if (position == null) continue;
                var path = _grid.FindPath(from, position);
                if (path == null) continue;
                var length = _grid.PathLength(path);
                var score = length + (queue.Count * _queueSpacing);
                if (best == null || score < best.Length - 1e-9 || (Math.Abs(score - best.Length) <= 1e-9 && side < best.Side))
                    best = new ShelfQueueAssignment { ShelfId = shelfId, Side = side, Index = queue.Count, Position = position, Path = path, Length = score };
            }
            if (best == null) return null;
            Queue(best.ShelfId, best.Side).Add(npcId);
            best.NpcId = npcId;
            return best;
        }

        internal (string ShelfId, ShelfSide Side)? LeaveQueue(string npcId)
        {
            foreach (var pair in _queues)
            {
                var index = pair.Value.IndexOf(npcId);
                if (index < 0) continue;
                pair.Value.RemoveAt(index);
                ParseQueueKey(pair.Key, out var shelfId, out var side);
                return (shelfId, side);
            }
            return null;
        }

        internal ShelfPromotion TryPromote(string shelfId, ShelfSide side, Func<string, Position2D> positionOf)
        {
            var queue = Queue(shelfId, side);
            var skipped = new List<string>();
            while (queue.Count > 0)
            {
                var npcId = queue[0];
                var position = positionOf(npcId);
                if (position == null) { queue.RemoveAt(0); skipped.Add(npcId); continue; }
                var candidates = Reachable(shelfId, position, slot => slot.Side == side && slot.State == ShelfSlotState.Free, 3);
                if (candidates.Count == 0) { queue.RemoveAt(0); skipped.Add(npcId); continue; }
                var selected = candidates[0];
                if (!TryReserve(selected.Slot, npcId)) continue;
                queue.RemoveAt(0);
                return new ShelfPromotion { NpcId = npcId, Slot = selected.Slot, Path = selected.Path, IneligibleNpcIds = skipped.ToArray() };
            }
            return skipped.Count == 0 ? null : new ShelfPromotion { IneligibleNpcIds = skipped.ToArray() };
        }

        internal IReadOnlyList<ShelfQueueAssignment> Reflow(string shelfId, ShelfSide side, Func<string, Position2D> positionOf)
        {
            var queue = Queue(shelfId, side);
            var result = new List<ShelfQueueAssignment>();
            for (var index = 0; index < queue.Count; index++)
            {
                var position = QueuePosition(shelfId, side, index);
                var current = positionOf(queue[index]);
                if (position == null || current == null) continue;
                var path = _grid.FindPath(current, position);
                if (path != null) result.Add(new ShelfQueueAssignment { NpcId = queue[index], ShelfId = shelfId, Side = side, Index = index, Position = position, Path = path, Length = _grid.PathLength(path) });
            }
            return result;
        }

        internal string NpcAt(string shelfId, ShelfSide side, int queueIndex)
        {
            var queue = Queue(shelfId, side);
            return queueIndex >= 0 && queueIndex < queue.Count ? queue[queueIndex] : string.Empty;
        }

        internal bool IsQueued(string npcId) => _queues.Values.Any(queue => queue.Contains(npcId));
        internal ShelfInteractionSlot SlotOwnedBy(string npcId) => _slots.Values.FirstOrDefault(slot => slot.OwnerNpcId == npcId);

        private List<ShelfSlotCandidate> Reachable(string shelfId, Position2D from, Func<ShelfInteractionSlot, bool> predicate, int desiredCount)
        {
            var result = new List<ShelfSlotCandidate>();
            var candidates = _slots.Values.Where(slot => slot.ShelfId == shelfId && predicate(slot))
                .OrderBy(slot => DistanceSquared(from, slot.Position)).ThenBy(slot => slot.Id, StringComparer.Ordinal);
            foreach (var slot in candidates)
            {
                var path = _grid.FindPath(from, slot.Position);
                if (path != null) result.Add(new ShelfSlotCandidate { Slot = slot, Path = path, Length = _grid.PathLength(path) });
                if (result.Count >= desiredCount) break;
            }
            result.Sort((left, right) => left.Length.CompareTo(right.Length) != 0 ? left.Length.CompareTo(right.Length) : string.CompareOrdinal(left.Slot.Id, right.Slot.Id));
            return result;
        }

        private Position2D QueuePosition(string shelfId, ShelfSide side, int index)
        {
            var slots = _slots.Values.Where(slot => slot.ShelfId == shelfId && slot.Side == side).OrderBy(slot => slot.Index).ToArray();
            if (slots.Length == 0 || index < 0) return null;
            var centerX = slots.Average(slot => slot.Position.X);
            var centerY = slots.Average(slot => slot.Position.Y);
            var normalX = -slots[0].Facing.X;
            var normalY = -slots[0].Facing.Y;
            var distance = (index + 1) * _queueSpacing;
            var position = new Position2D(centerX + (normalX * distance), centerY + (normalY * distance));
            if (!double.IsFinite(position.X) || !double.IsFinite(position.Y) || position.X < 0 || position.Y < 0 || position.X > _layout.Width || position.Y > _layout.Height) return null;
            return _grid.IsPointWalkable(position) ? position : null;
        }

        private List<string> Queue(string shelfId, ShelfSide side)
        {
            var key = QueueKey(shelfId, side);
            if (!_queues.TryGetValue(key, out var queue)) { queue = new List<string>(); _queues[key] = queue; }
            return queue;
        }

        private static string QueueKey(string shelfId, ShelfSide side) => shelfId + "\u001f" + side;
        private static double DistanceSquared(Position2D first, Position2D second) { var dx = first.X - second.X; var dy = first.Y - second.Y; return (dx * dx) + (dy * dy); }
        private static void ParseQueueKey(string key, out string shelfId, out ShelfSide side)
        {
            var separator = key.LastIndexOf('\u001f');
            shelfId = separator < 0 ? key : key.Substring(0, separator);
            side = separator < 0 || !Enum.TryParse(key.Substring(separator + 1), out ShelfSide parsed) ? ShelfSide.North : parsed;
        }
    }
}
