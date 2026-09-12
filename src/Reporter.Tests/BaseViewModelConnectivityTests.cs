using Reporter.Core.Interfaces;
using Reporter.Core.ViewModels;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the connectivity tracking encapsulated in <see cref="BaseViewModel"/>.
/// </summary>
public class BaseViewModelConnectivityTests
{
    /// <summary>
    /// Verifies that InitConnectivity initializes IsOnline from the service
    /// without subscribing to change events.
    /// </summary>
    [Fact]
    public void InitConnectivity_SetsIsOnline_WithoutTracking()
    {
        var service = new FakeNetworkStatusService { IsOnline = false };
        var viewModel = new TestViewModel();

        viewModel.Init(service);

        Assert.False(viewModel.IsOnline);

        service.IsOnline = true;
        service.RaiseConnectivityChanged();

        Assert.False(viewModel.IsOnline);
        Assert.Equal(0, viewModel.ConnectivityChangeCount);
    }

    /// <summary>
    /// Verifies that TrackConnectivity updates IsOnline and invokes the hook
    /// on connectivity change events.
    /// </summary>
    [Fact]
    public void TrackConnectivity_UpdatesIsOnline_AndInvokesHook()
    {
        var service = new FakeNetworkStatusService();
        var viewModel = new TestViewModel();
        viewModel.Track(service);

        Assert.True(viewModel.IsOnline);

        service.IsOnline = false;
        service.RaiseConnectivityChanged();

        Assert.False(viewModel.IsOnline);
        Assert.Equal(1, viewModel.ConnectivityChangeCount);
        Assert.False(viewModel.LastReportedIsOnline);
    }

    /// <summary>
    /// Verifies that UntrackConnectivity stops further updates.
    /// </summary>
    [Fact]
    public void UntrackConnectivity_StopsUpdates()
    {
        var service = new FakeNetworkStatusService();
        var viewModel = new TestViewModel();
        viewModel.Track(service);

        viewModel.Untrack();
        viewModel.Untrack();

        service.IsOnline = false;
        service.RaiseConnectivityChanged();

        Assert.True(viewModel.IsOnline);
        Assert.Equal(0, viewModel.ConnectivityChangeCount);
    }

    /// <summary>
    /// Verifies that RefreshConnectivityStatus invokes the hook only when the
    /// status actually changed.
    /// </summary>
    [Fact]
    public void RefreshConnectivityStatus_InvokesHookOnlyOnChange()
    {
        var service = new FakeNetworkStatusService();
        var viewModel = new TestViewModel();
        viewModel.Init(service);

        viewModel.Refresh();

        Assert.Equal(0, viewModel.ConnectivityChangeCount);

        service.IsOnline = false;
        viewModel.Refresh();

        Assert.False(viewModel.IsOnline);
        Assert.Equal(1, viewModel.ConnectivityChangeCount);
    }

    /// <summary>
    /// Verifies that tracking can be re-attached after a detach.
    /// </summary>
    [Fact]
    public void TrackConnectivity_CanBeReattached()
    {
        var service = new FakeNetworkStatusService();
        var viewModel = new TestViewModel();
        viewModel.Init(service);

        viewModel.Track();
        viewModel.Untrack();
        viewModel.Track();

        service.IsOnline = false;
        service.RaiseConnectivityChanged();

        Assert.False(viewModel.IsOnline);
        Assert.Equal(1, viewModel.ConnectivityChangeCount);
    }

    private sealed class TestViewModel : BaseViewModel
    {
        /// <summary>
        /// Gets the number of times the connectivity change hook was invoked.
        /// </summary>
        public int ConnectivityChangeCount { get; private set; }

        /// <summary>
        /// Gets the online state last reported to the connectivity change hook.
        /// </summary>
        public bool LastReportedIsOnline { get; private set; }

        /// <summary>
        /// Exposes <see cref="BaseViewModel.InitConnectivity"/> for testing.
        /// </summary>
        /// <param name="service">The network connectivity status service.</param>
        public void Init(INetworkStatusService service) => InitConnectivity(service);

        /// <summary>
        /// Exposes <see cref="BaseViewModel.TrackConnectivity(INetworkStatusService)"/> for testing.
        /// </summary>
        /// <param name="service">The network connectivity status service.</param>
        public void Track(INetworkStatusService service) => TrackConnectivity(service);

        /// <summary>
        /// Exposes <see cref="BaseViewModel.TrackConnectivity()"/> for testing.
        /// </summary>
        public void Track() => TrackConnectivity();

        /// <summary>
        /// Exposes <see cref="BaseViewModel.UntrackConnectivity"/> for testing.
        /// </summary>
        public void Untrack() => UntrackConnectivity();

        /// <summary>
        /// Exposes <see cref="BaseViewModel.RefreshConnectivityStatus"/> for testing.
        /// </summary>
        public void Refresh() => RefreshConnectivityStatus();

        /// <inheritdoc />
        protected override void OnConnectivityChanged(bool isOnline)
        {
            ConnectivityChangeCount++;
            LastReportedIsOnline = isOnline;
        }
    }
}
