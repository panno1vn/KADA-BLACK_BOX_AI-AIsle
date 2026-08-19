using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AIsle.Contracts.Simulation;
using AIsle.Simulation.Runtime;

namespace AIsle.DesktopApp.Application
{
    public sealed class SimulationApplicationService : IDisposable
    {
        private readonly object _gate = new object();
        private readonly bool _backgroundLoop;
        private Timer? _timer;
        private SimulationHost? _host;
        private SimulationStartInput? _input;
        private bool _running;
        private double _speedMultiplier = 1.0;
        private double _accumulatorSeconds;
        private long _lastTimerTimestamp;

        private static readonly double[] SupportedSpeeds = { 1.0, 2.0, 3.0, 5.0, 15.0, 30.0 };

        public SimulationApplicationService(bool backgroundLoop = true)
        {
            _backgroundLoop = backgroundLoop;
        }

        public SimulationStateProjection Start(SimulationStartInput? input = null)
        {
            lock (_gate)
            {
                if (input != null)
                {
                    SimulationConfigValidator.ThrowIfInvalid(input.Config);
                    _input = input;
                    _host = CreateHost(input);
                }
                else if (_host == null)
                {
                    throw new InvalidOperationException("simulation.start requires input for a new session.");
                }

                _running = !_host.Completed;
                if (_running && _host.Time == 0.0)
                {
                    _host.Step(_input!.Config.TickSeconds);
                    _running = !_host.Completed;
                }
                ConfigureTimer();
                return _host.ProjectState(_running);
            }
        }

        public SimulationStateProjection Pause()
        {
            lock (_gate)
            {
                var host = RequireHost();
                _running = false;
                ConfigureTimer();
                return host.ProjectState(false);
            }
        }

        public SimulationStateProjection Step()
        {
            lock (_gate)
            {
                var host = RequireHost();
                _running = false;
                ConfigureTimer();
                host.Step(_input!.Config.TickSeconds);
                return host.ProjectState(false);
            }
        }

        public SimulationStateProjection Reset()
        {
            lock (_gate)
            {
                if (_input == null) throw new InvalidOperationException("No simulation session has been started.");
                _running = false;
                ConfigureTimer();
                _host = CreateHost(_input);
                return _host.ProjectState(false);
            }
        }

        public SimulationStateProjection State()
        {
            lock (_gate) return RequireHost().ProjectState(_running);
        }

        public SimulationSessionSnapshot Snapshot()
        {
            lock (_gate)
            {
                var host = RequireHost();
                return new SimulationSessionSnapshot
                {
                    RunId = host.RunId,
                    SpeedMultiplier = _speedMultiplier,
                    State = host.ProjectState(_running),
                    Summary = new SimulationSummary
                    {
                        DurationSeconds = host.Time,
                        Revenue = host.Revenue,
                        Purchases = host.Purchases.Count,
                        Spawned = host.Spawned,
                        Converted = host.Converted,
                        MainBuyers = host.MainBuyers,
                        ImpulseBuyers = host.ImpulseBuyers,
                        NotFound = host.NotFound,
                        Unreachable = host.Unreachable,
                        StuckRecoveries = host.StuckRecoveries,
                        Completed = host.Completed
                    },
                    Events = host.Events.ToArray(),
                    Purchases = host.Purchases.ToArray()
                };
            }
        }

        public SimulationSessionSnapshot SetSpeed(double multiplier)
        {
            lock (_gate)
            {
                if (!SupportedSpeeds.Contains(multiplier))
                    throw new ArgumentException("Simulation speed must be one of: 1, 2, 3, 5, 15, 30.", nameof(multiplier));
                RequireHost();
                _speedMultiplier = multiplier;
                return Snapshot();
            }
        }

        public SimResult Result(string? name = null)
        {
            lock (_gate)
            {
                var host = RequireHost();
                return host.BuildResult(string.IsNullOrWhiteSpace(name) ? _input!.Name : name);
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                _running = false;
                _timer?.Dispose();
                _timer = null;
            }
        }

        private static SimulationHost CreateHost(SimulationStartInput input) =>
            new SimulationHost(input.Layout, input.Catalog, input.Population, input.Config);

        private SimulationHost RequireHost() =>
            _host ?? throw new InvalidOperationException("No simulation session has been started.");

        private void ConfigureTimer()
        {
            _timer?.Dispose();
            _timer = null;
            _accumulatorSeconds = 0.0;
            _lastTimerTimestamp = Stopwatch.GetTimestamp();
            if (!_backgroundLoop || !_running || _host == null || _input == null) return;
            const int milliseconds = 20;
            _timer = new Timer(Tick, null, milliseconds, milliseconds);
        }

        private void Tick(object? state)
        {
            lock (_gate)
            {
                if (!_running || _host == null || _input == null) return;
                var now = Stopwatch.GetTimestamp();
                var realSeconds = Math.Min(0.25, (now - _lastTimerTimestamp) / (double)Stopwatch.Frequency);
                _lastTimerTimestamp = now;
                _accumulatorSeconds += realSeconds * _speedMultiplier;
                var steps = 0;
                while (_accumulatorSeconds + 1e-9 >= _input.Config.TickSeconds && steps++ < 200 && !_host.Completed)
                {
                    _host.Step(_input.Config.TickSeconds);
                    _accumulatorSeconds -= _input.Config.TickSeconds;
                }
                if (_host.Completed)
                {
                    _running = false;
                    ConfigureTimer();
                }
            }
        }
    }
}
