using System;
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
            if (!_backgroundLoop || !_running || _host == null || _input == null) return;
            var milliseconds = Math.Max(20, (int)Math.Round(_input.Config.TickSeconds * 1000.0));
            _timer = new Timer(Tick, null, milliseconds, milliseconds);
        }

        private void Tick(object? state)
        {
            lock (_gate)
            {
                if (!_running || _host == null || _input == null) return;
                _host.Step(_input.Config.TickSeconds);
                if (_host.Completed)
                {
                    _running = false;
                    ConfigureTimer();
                }
            }
        }
    }
}
