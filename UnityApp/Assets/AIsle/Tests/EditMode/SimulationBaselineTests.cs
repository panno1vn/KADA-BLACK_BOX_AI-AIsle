using System.Linq;
using AIsle.Contracts.Population;
using AIsle.Contracts.Simulation;
using AIsle.Simulation.Runtime;
using NUnit.Framework;

namespace AIsle.Tests.Simulation
{
    public sealed class SimulationBaselineTests
    {
        [Test]
        public void AStarDoesNotCrossSealedWall()
        {
            var layout = new LayoutDefinition
            {
                Width = 6, Height = 4, Entrance = new Position2D(1, 2), Checkout = new Position2D(1.5, 2),
                Walls = new[] { new WallDefinition { Id = "barrier", X1 = 3, Y1 = 0, X2 = 3, Y2 = 4 } }
            };
            var grid = new PathGrid(layout, new SimulationConfig { PathCellSize = 0.2, ObstacleMargin = 0.2 });
            Assert.That(grid.FindPath(new Position2D(1, 2), new Position2D(5, 2)), Is.Null);
        }

        [Test]
        public void FullJourneyProducesReplayableSimResult()
        {
            var layout = new LayoutDefinition
            {
                Width = 8, Height = 4, Entrance = new Position2D(1, 1.7), Checkout = new Position2D(1, 2.7),
                Shelves = new[] { new ShelfDefinition { Id = "s1", Label = "Drink", X = 3, Y = 1.2, Width = 1, Height = 1, Valence = 0.5 } },
                SpawnRateCurve = new[] { new SpawnRatePoint { Minute = 0, Rate = 600 } }
            };
            var profile = new NPCProfile
            {
                Id = "buyer", TargetCategory = "drink", WalkingSpeed = 1.5, InitialNeed = 1, InitialExplorationNeed = 0, DwellSeconds = 0.2,
                CategoryPreferences = new[] { new CategoryPreference("drink", 1) }
            };
            var population = new PopulationDefinition { PopulationId = "test", NPCProfiles = new[] { profile }, Metadata = new PopulationMetadata { GeneratorName = "test", GeneratorVersion = "1" } };
            var catalog = new[] { new ProductDefinition { Id = "drink", Name = "Drink", Category = "drink", ShelfId = "s1", Price = 12.5 } };
            var config = new SimulationConfig { DurationMinutes = 1, TickSeconds = 0.1, TopKChoices = 1, DecisionNoise = 0, PurchaseNeedA = 10, PurchaseValenceB = 0, PurchaseBiasC = 10, TrajectorySampleSeconds = 0.2 };
            var host = new SimulationHost(layout, catalog, population, config); host.Agents[0].Spawn = 0; host.RunToCompletion(5000); var result = host.BuildResult("unity");
            Assert.That(result.SchemaVersion, Is.EqualTo("aisle.sim-result.v1"));
            Assert.That(result.Summary.Completed, Is.True);
            Assert.That(result.Events.Any(item => item.Type == "purchase"), Is.True);
            Assert.That(result.Events.Any(item => item.Type == "left"), Is.True);
            Assert.That(result.Replay.Agents[0].Samples.Length, Is.GreaterThan(2));
        }
    }
}
