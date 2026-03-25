namespace GCE.Simulation;

/// <summary>
/// Defines the contract for running a galvanic corrosion simulation.
/// </summary>
/// <remarks>
/// Implementations should support synchronous execution (<see cref="Run"/>),
/// asynchronous execution with progress reporting and cooperative cancellation
/// (<see cref="RunAsync"/>), and resumption from a previously captured
/// <see cref="SimulationState"/> (<see cref="Resume"/>).
/// </remarks>
public interface ISimulationRunner
{
    /// <summary>
    /// Runs the simulation synchronously and returns the results.
    /// </summary>
    /// <param name="parameters">Simulation configuration.</param>
    /// <returns>A <see cref="SimulationResult"/> containing all time-series data.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="parameters"/> is <see langword="null"/>.
    /// </exception>
    SimulationResult Run(SimulationParameters parameters);

    /// <summary>
    /// Runs the simulation asynchronously, reporting progress and honouring cancellation.
    /// </summary>
    /// <remarks>
    /// Progress is reported after every integration step.  When the
    /// <paramref name="cancellationToken"/> is cancelled the engine captures the current
    /// <see cref="SimulationState"/> via <paramref name="checkpoint"/> and terminates
    /// cleanly; the caller can later pass that state to <see cref="Resume"/> to continue.
    /// </remarks>
    /// <param name="parameters">Simulation configuration.</param>
    /// <param name="progress">
    /// Optional receiver for per-step progress updates.  May be <see langword="null"/>.
    /// </param>
    /// <param name="checkpoint">
    /// When the run is cancelled this output parameter receives the
    /// <see cref="SimulationState"/> at the cancellation point, enabling later resumption.
    /// Set to <see langword="null"/> when the run completes normally.
    /// </param>
    /// <param name="cancellationToken">
    /// Token that can be used to pause (cancel) the simulation.
    /// </param>
    /// <returns>
    /// A task that resolves to a (possibly partial) <see cref="SimulationResult"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="parameters"/> is <see langword="null"/>.
    /// </exception>
    Task<SimulationResult> RunAsync(
        SimulationParameters parameters,
        IProgress<SimulationProgress>? progress,
        out SimulationState? checkpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a previously paused simulation from a captured <see cref="SimulationState"/>.
    /// </summary>
    /// <remarks>
    /// The results from the resumed run are appended to the data already present in
    /// <paramref name="checkpoint"/>, producing a single <see cref="SimulationResult"/>
    /// that spans the full simulation window.
    /// </remarks>
    /// <param name="checkpoint">
    /// The state snapshot captured when the simulation was paused.
    /// </param>
    /// <param name="parameters">
    /// The original simulation parameters (duration and steps must match the initial run).
    /// </param>
    /// <param name="progress">Optional receiver for per-step progress updates.</param>
    /// <param name="cancellationToken">
    /// Token that can be used to pause the resumed simulation again.
    /// </param>
    /// <returns>
    /// A task that resolves to a (possibly partial) <see cref="SimulationResult"/> covering
    /// the resumed portion of the simulation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="checkpoint"/> or <paramref name="parameters"/> is
    /// <see langword="null"/>.
    /// </exception>
    Task<SimulationResult> Resume(
        SimulationState checkpoint,
        SimulationParameters parameters,
        IProgress<SimulationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
